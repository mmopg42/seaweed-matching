using ChronoView.Models;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using System.Collections.Concurrent;
using System.IO;

namespace ChronoView.Core.ImageProcessing;

/// <summary>
/// Service for image processing operations with LRU caching.
/// Implements background image processing with proper threading for WPF UI updates.
/// </summary>
public class ImageProcessingService : IImageProcessor
{
    private readonly ILogger<ImageProcessingService> _logger;
    private readonly ImageSettings _settings;
    private readonly LruCache<string, byte[]> _thumbnailCache;
    private readonly LruCache<string, (int Width, int Height)> _thumbnailDimensionsCache;
    private readonly LruCache<string, ImageMetadata> _metadataCache;
    // Limit concurrent processing to prevent thread pool starvation
    // Use ProcessorCount * 2 as a reasonable baseline for mixed I/O and CPU work
    private readonly SemaphoreSlim _processingSemaphore = new(Math.Max(4, Environment.ProcessorCount * 2));

    public ImageProcessingService(ILogger<ImageProcessingService> logger, ApplicationConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = configuration?.ImageSettings ?? throw new ArgumentNullException(nameof(configuration));

        // Initialize LRU caches with configured size
        var maxCacheSize = _settings.MaxCacheSizeMB * 1024 * 1024; // Convert MB to bytes
        _thumbnailCache = new LruCache<string, byte[]>(maxCacheSize);
        _thumbnailDimensionsCache = new LruCache<string, (int Width, int Height)>(maxCacheSize / 50); // Very small overhead
        _metadataCache = new LruCache<string, ImageMetadata>(maxCacheSize / 10); // Metadata is much smaller

        _logger.LogInformation("ImageProcessingService initialized with cache size: {CacheSizeMB}MB, Concurrency: {Limit}", 
            _settings.MaxCacheSizeMB, _processingSemaphore.CurrentCount);
    }

    /// <summary>
    /// Generates a thumbnail image asynchronously using Task.Run for CPU-intensive operations.
    /// </summary>
    public async Task<byte[]> GenerateThumbnailAsync(string imagePath, int width, int height, CancellationToken cancellationToken = default, bool throwOnError = false)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
            throw new ArgumentException("Image path cannot be null or empty", nameof(imagePath));

        if (width <= 0 || height <= 0)
            throw new ArgumentException("Width and height must be positive values");

        if (!File.Exists(imagePath))
        {
            if (throwOnError) throw new FileNotFoundException("Image file not found", imagePath);
            _logger.LogWarning("Image file not found: {ImagePath}", imagePath);
            return GetPlaceholderImage(width, height);
        }

        // Check cache first
        var cacheKey = $"{imagePath}_{width}x{height}";
        if (_settings.EnableCaching && _thumbnailCache.TryGet(cacheKey, out var cachedThumbnail))
        {
            _logger.LogDebug("Thumbnail cache hit for: {ImagePath}", imagePath);
            return cachedThumbnail ?? GetPlaceholderImage(width, height);
        }

        try
        {
            // Throttle concurrent processing
            await _processingSemaphore.WaitAsync(cancellationToken);

            try
            {
                // Use Task.Run for CPU-intensive image processing on background thread
                var thumbnail = await Task.Run(async () =>
                {
                    // Open file with FileShare.Read to allow other processes to access it
                    using var fileStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    using var image = await SixLabors.ImageSharp.Image.LoadAsync(fileStream, cancellationToken);

                    // Resize image maintaining aspect ratio
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = new SixLabors.ImageSharp.Size(width, height),
                        Mode = ResizeMode.Max
                    }));

                    // Encode to JPEG with configured quality
                    using var ms = new MemoryStream();
                    var encoder = new JpegEncoder { Quality = _settings.ThumbnailQuality };
                    await image.SaveAsync(ms, encoder, cancellationToken);
                    return ms.ToArray();
                }, cancellationToken);

                // Cache the result
                if (_settings.EnableCaching)
                {
                    _thumbnailCache.Add(cacheKey, thumbnail);
                }

                _logger.LogDebug("Generated thumbnail for: {ImagePath} ({Size} bytes)", imagePath, thumbnail.Length);
                return thumbnail;
            }
            finally
            {
                _processingSemaphore.Release();
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            if (throwOnError) throw; // Re-throw to caller to handle retry
            _logger.LogError(ex, "Error generating thumbnail for: {ImagePath}", imagePath);
            return GetPlaceholderImage(width, height);
        }
    }

    /// <summary>
    /// Generates a thumbnail image and returns original dimensions in a single pass.
    /// </summary>
    public async Task<ThumbnailWithDimensionsResult> GenerateThumbnailWithDimensionsAsync(
        string imagePath, int width, int height, CancellationToken cancellationToken = default, bool throwOnError = false)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
            throw new ArgumentException("Image path cannot be null or empty", nameof(imagePath));

        if (width <= 0 || height <= 0)
            throw new ArgumentException("Width and height must be positive values");

        if (!File.Exists(imagePath))
        {
            if (throwOnError) throw new FileNotFoundException("Image file not found", imagePath);
            _logger.LogWarning("Image file not found: {ImagePath}", imagePath);
            return new ThumbnailWithDimensionsResult(GetPlaceholderImage(width, height), 0, 0);
        }

        // Check cache first
        var cacheKey = $"{imagePath}_{width}x{height}";
        if (_settings.EnableCaching && 
            _thumbnailCache.TryGet(cacheKey, out var cachedThumbnail) &&
            _thumbnailDimensionsCache.TryGet(cacheKey, out var cachedDimensions))
        {
            _logger.LogDebug("Thumbnail+Dimensions cache hit for: {ImagePath}", imagePath);
            var thumb = cachedThumbnail ?? GetPlaceholderImage(width, height);
            return new ThumbnailWithDimensionsResult(thumb, cachedDimensions.Width, cachedDimensions.Height);
        }

        try
        {
            await _processingSemaphore.WaitAsync(cancellationToken);

            try
            {
                var result = await Task.Run(async () =>
                {
                    using var fileStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    using var image = await SixLabors.ImageSharp.Image.LoadAsync(fileStream, cancellationToken);
                    
                    var originalWidth = image.Width;
                    var originalHeight = image.Height;

                    // Resize image
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = new SixLabors.ImageSharp.Size(width, height),
                        Mode = ResizeMode.Max
                    }));

                    using var ms = new MemoryStream();
                    var encoder = new JpegEncoder { Quality = _settings.ThumbnailQuality };
                    await image.SaveAsync(ms, encoder, cancellationToken);
                    var thumbnailBytes = ms.ToArray();

                    return new ThumbnailWithDimensionsResult(thumbnailBytes, originalWidth, originalHeight);
                }, cancellationToken);

                // Cache the result
                if (_settings.EnableCaching)
                {
                    _thumbnailCache.Add(cacheKey, result.ThumbnailBytes);
                    _thumbnailDimensionsCache.Add(cacheKey, (result.OriginalWidth, result.OriginalHeight));
                }

                _logger.LogDebug("Generated thumbnail with dimensions for: {ImagePath} ({OriginalW}x{OriginalH})", 
                    imagePath, result.OriginalWidth, result.OriginalHeight);
                
                return result;
            }
            finally
            {
                _processingSemaphore.Release();
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            if (throwOnError) throw;
            _logger.LogError(ex, "Error generating thumbnail with dimensions for: {ImagePath}", imagePath);
            return new ThumbnailWithDimensionsResult(GetPlaceholderImage(width, height), 0, 0);
        }
    }

    /// <summary>
    /// Extracts image metadata asynchronously.
    /// </summary>
    public async Task<ImageMetadata> GetImageMetadataAsync(string imagePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
            throw new ArgumentException("Image path cannot be null or empty", nameof(imagePath));

        if (!File.Exists(imagePath))
        {
            _logger.LogWarning("Image file not found for metadata extraction: {ImagePath}", imagePath);
            return CreateErrorMetadata(imagePath);
        }

        // Check cache first
        if (_settings.EnableCaching && _metadataCache.TryGet(imagePath, out var cachedMetadata))
        {
            _logger.LogDebug("Metadata cache hit for: {ImagePath}", imagePath);
            return cachedMetadata ?? CreateErrorMetadata(imagePath);
        }

        try
        {
            // Throttle concurrent processing
            await _processingSemaphore.WaitAsync(cancellationToken);

            try
            {
                // Use Task.Run for file I/O and image processing on background thread
                var metadata = await Task.Run(async () =>
                {
                    var fileInfo = new FileInfo(imagePath);

                    // Open file with FileShare.Read to allow other processes to access it
                    using var fileStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    using var image = await SixLabors.ImageSharp.Image.LoadAsync(fileStream, cancellationToken);

                    return new ImageMetadata
                    {
                        Width = image.Width,
                        Height = image.Height,
                        CreatedAt = fileInfo.CreationTime,
                        FileSize = fileInfo.Length,
                        Format = image.Metadata.DecodedImageFormat?.Name ?? "Unknown",
                        FilePath = imagePath,
                        IsAbnormal = false
                    };
                }, cancellationToken);

                // Cache the result
                if (_settings.EnableCaching)
                {
                    _metadataCache.Add(imagePath, metadata);
                }

                _logger.LogDebug("Extracted metadata for: {ImagePath} ({Width}x{Height})", imagePath, metadata.Width, metadata.Height);
                return metadata;
            }
            finally
            {
                _processingSemaphore.Release();
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error extracting metadata for: {ImagePath}", imagePath);
            return CreateErrorMetadata(imagePath);
        }
    }

    /// <summary>
    /// Gets a placeholder image for missing or error states.
    /// </summary>
    public byte[] GetPlaceholderImage(int width, int height)
    {
        try
        {
            // Create a simple gray placeholder image with an X
            using var image = new Image<SixLabors.ImageSharp.PixelFormats.Rgb24>(width, height);
            image.Mutate(x =>
            {
                // Fill with gray background
                x.BackgroundColor(SixLabors.ImageSharp.Color.Gray);
                
                // Draw an X using DrawLine (singular)
                x.DrawLine(SixLabors.ImageSharp.Color.DarkGray, 2,
                    new SixLabors.ImageSharp.PointF(0, 0), new SixLabors.ImageSharp.PointF(width, height));
                x.DrawLine(SixLabors.ImageSharp.Color.DarkGray, 2,
                    new SixLabors.ImageSharp.PointF(width, 0), new SixLabors.ImageSharp.PointF(0, height));
            });

            using var ms = new MemoryStream();
            var encoder = new JpegEncoder { Quality = 75 };
            image.Save(ms, encoder);
            return ms.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating placeholder image");
            // Return minimal valid JPEG if placeholder creation fails
            return Array.Empty<byte>();
        }
    }

    /// <summary>
    /// Clears all caches to free memory.
    /// </summary>
    public void ClearCache()
    {
        _thumbnailCache.Clear();
        _thumbnailDimensionsCache.Clear();
        _metadataCache.Clear();
        _logger.LogInformation("Image caches cleared");
    }

    /// <summary>
    /// Gets the current cache size in bytes.
    /// </summary>
    public long GetCacheSizeBytes()
    {
        return _thumbnailCache.GetSizeBytes() + _thumbnailDimensionsCache.GetSizeBytes() + _metadataCache.GetSizeBytes();
    }

    private ImageMetadata CreateErrorMetadata(string imagePath)
    {
        return new ImageMetadata
        {
            Width = 0,
            Height = 0,
            CreatedAt = DateTime.MinValue,
            FileSize = 0,
            Format = "Error",
            FilePath = imagePath,
            IsAbnormal = false
        };
    }

    /// <summary>
    /// Gets image dimensions without loading the full image (header-only read).
    /// </summary>
    /// <param name="imagePath">Path to the image file.</param>
    /// <returns>Tuple of (Width, Height). Returns (0, 0) if file not found or error.</returns>
    public (int Width, int Height) GetImageDimensions(string imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
        {
            return (0, 0);
        }

        try
        {
            using var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var info = SixLabors.ImageSharp.Image.Identify(stream);
            return (info.Width, info.Height);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to identify image dimensions for: {ImagePath}", imagePath);
            return (0, 0);
        }
    }
}
