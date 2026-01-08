using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using ChronoView.Models;
using Microsoft.Extensions.Logging;

namespace ChronoView.Core.FileWatching
{
    /// <summary>
    /// Service that handles image capture and caching, especially for race condition handling with ML programs
    /// </summary>
    public class ImageCaptureService : IImageCaptureService
    {
        private readonly ILogger<ImageCaptureService> _logger;
        private readonly IGroupManager _groupManager;
        private readonly ConcurrentDictionary<string, BitmapImage> _imageCaptureCache = new();

        public ImageCaptureService(ILogger<ImageCaptureService> logger, IGroupManager groupManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _groupManager = groupManager ?? throw new ArgumentNullException(nameof(groupManager));
        }

        public async Task<BitmapImage?> LoadImageIntoMemoryAsync(string imagePath)
        {
            BitmapImage? capturedImage = null;
            var stopwatch = Stopwatch.StartNew();
            
            // Retry up to 3 times (handle file locks from other processes)
            for (int attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    // Open file with read sharing allowed
                    using var stream = new FileStream(imagePath, 
                        FileMode.Open, 
                        FileAccess.Read, 
                        FileShare.Read | FileShare.Delete, // Allow ML program to access concurrently
                        bufferSize: 81920, // 80KB buffer for faster reading
                        useAsync: true);
                    
                    // Load into memory
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad; // Load into memory immediately!
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                    bitmap.Freeze(); // Thread-safe + memory efficient
                    
                    capturedImage = bitmap;
                    stopwatch.Stop();
                    
                    _logger.LogDebug("Image loaded in {Ms}ms: {Path}", 
                        stopwatch.ElapsedMilliseconds, Path.GetFileName(imagePath));
                    break;
                }
                catch (IOException ex) when (attempt < 2)
                {
                    _logger.LogWarning("File locked (attempt {Attempt}/3), retrying: {Message}", 
                        attempt + 1, ex.Message);
                    await Task.Delay(10); // 10ms delay before retry
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load image: {Path}", Path.GetFileName(imagePath));
                    break;
                }
            }
            
            return capturedImage;
        }

        public async Task HandleStitchedImageCaptureAsync(string imagePath, int workerId, Action<FileGroup> onGroupUpdated)
        {
            _logger.LogInformation("🏁 Worker {WorkerId} racing to capture image: {Path}", workerId, imagePath);
            
            try
            {
                // Get parent folder path (this is the Normal folder = group key)
                string? folderPath = Path.GetDirectoryName(imagePath);
                if (string.IsNullOrEmpty(folderPath))
                {
                    _logger.LogWarning("Could not determine parent folder for: {Path}", imagePath);
                    return;
                }

                // ⚡ Load image into memory IMMEDIATELY (race condition critical!)
                var capturedImage = await LoadImageIntoMemoryAsync(imagePath);
                
                if (capturedImage == null)
                {
                    _logger.LogError("❌ Failed to capture image after 3 attempts: {Path}", imagePath);
                    return;
                }
                
                _logger.LogInformation("✅ Image captured for group matching: {Path}", Path.GetFileName(imagePath));

                // Find existing group by folder path (via GroupManager)
                FileGroup? existingGroup = _groupManager.ActiveGroups
                    .FirstOrDefault(g => g.NormalFolder?.Equals(folderPath, StringComparison.OrdinalIgnoreCase) == true);

                if (existingGroup != null)
                {
                    // Store in cache
                    _imageCaptureCache[existingGroup.GroupId] = capturedImage;
                    _imageCaptureCache[folderPath] = capturedImage;
                    
                    _logger.LogInformation("📸 Cached image for group {GroupId} (folder: {Folder})", 
                        existingGroup.GroupId, Path.GetFileName(folderPath));
                    
                    // Trigger UI update
                    onGroupUpdated?.Invoke(existingGroup);
                }
                else
                {
                    _logger.LogWarning("⚠️ No existing group found for folder: {Folder}. Image cached by folder path temporarily.", 
                        Path.GetFileName(folderPath));
                    
                    // Cache by folder path temporarily (group might be created soon)
                    _imageCaptureCache[folderPath] = capturedImage;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle stitched image capture: {Path}", imagePath);
            }
        }

        public BitmapImage? GetCapturedImage(string groupId)
        {
            if (_imageCaptureCache.TryGetValue(groupId, out var image))
            {
                _logger.LogDebug("🎯 Cache hit for group {GroupId}", groupId);
                return image;
            }
            
            _logger.LogDebug("❌ Cache miss for group {GroupId}", groupId);
            return null;
        }

        public BitmapImage? GetCapturedImageByFolderPath(string folderPath)
        {
            if (_imageCaptureCache.TryGetValue(folderPath, out var image))
            {
                _logger.LogDebug("🎯 Cache hit for folder {Folder}", Path.GetFileName(folderPath));
                return image;
            }

            return null;
        }

        public void PromoteCacheToGroupId(string folderPath, string groupId)
        {
            if (string.IsNullOrEmpty(folderPath) || string.IsNullOrEmpty(groupId))
                return;

            if (_imageCaptureCache.TryGetValue(folderPath, out var image))
            {
                _imageCaptureCache[groupId] = image;
                _logger.LogDebug("Promoted cached image from folder '{Folder}' to group '{GroupId}'", 
                    Path.GetFileName(folderPath), groupId);
            }
        }

        public void Clear()
        {
            _imageCaptureCache.Clear();
            _logger.LogInformation("Image capture cache cleared");
        }
    }
}
