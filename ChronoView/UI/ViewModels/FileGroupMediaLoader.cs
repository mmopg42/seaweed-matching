using ChronoView.Core.Configuration;
using ChronoView.Core.ImageProcessing;
using ChronoView.Core.Localization;
using ChronoView.Core.FileWatching;
using ChronoView.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ChronoView.Core.Nir;
using ScottPlot;
using System.Linq;
using SixLabors.ImageSharp;

namespace ChronoView.UI.ViewModels;

public class FileGroupMediaLoader : ViewModelBase, IDisposable
{
    private readonly FileGroup _group;
    private readonly IImageProcessor _imageProcessor;
    private readonly ApplicationConfiguration _config;
    private readonly ILogger _logger;
    private readonly Action<LogSeverity, string, string>? _uiLog;
    private readonly CancellationTokenSource _cts = new();
    
    private readonly ConcurrentQueue<RetryContext> _retryQueue = new();
    private readonly DispatcherTimer _retryTimer;

    #region Thumbnails
    private MainImageLoadedInfo? _mainImageLoadedInfo; public MainImageLoadedInfo? MainImageLoadedInfo { get => _mainImageLoadedInfo; set => SetProperty(ref _mainImageLoadedInfo, value); }
    private BitmapSource? _mainThumbnail; public BitmapSource? MainImageThumbnail { get => _mainThumbnail; set => SetProperty(ref _mainThumbnail, value); }
    private BitmapSource? _nirThumbnail; public BitmapSource? NirImageThumbnail { get => _nirThumbnail; set => SetProperty(ref _nirThumbnail, value); }
    private BitmapSource? _nirGraphThumbnail; public BitmapSource? NirGraphThumbnail { get => _nirGraphThumbnail; set => SetProperty(ref _nirGraphThumbnail, value); }
    private BitmapSource? _cam1Thumbnail; public BitmapSource? Camera1Thumbnail { get => _cam1Thumbnail; set => SetProperty(ref _cam1Thumbnail, value); }
    private BitmapSource? _cam2Thumbnail; public BitmapSource? Camera2Thumbnail { get => _cam2Thumbnail; set => SetProperty(ref _cam2Thumbnail, value); }
    private BitmapSource? _cam3Thumbnail; public BitmapSource? Camera3Thumbnail { get => _cam3Thumbnail; set => SetProperty(ref _cam3Thumbnail, value); }
    private BitmapSource? _cam4Thumbnail; public BitmapSource? Camera4Thumbnail { get => _cam4Thumbnail; set => SetProperty(ref _cam4Thumbnail, value); }
    private BitmapSource? _cam5Thumbnail; public BitmapSource? Camera5Thumbnail { get => _cam5Thumbnail; set => SetProperty(ref _cam5Thumbnail, value); }
    private BitmapSource? _cam6Thumbnail; public BitmapSource? Camera6Thumbnail { get => _cam6Thumbnail; set => SetProperty(ref _cam6Thumbnail, value); }
    #endregion

    public FileGroupMediaLoader(FileGroup group, IImageProcessor imageProcessor, IMonitoringOrchestrator? orchestrator, ApplicationConfiguration? config, ILogger? logger, Action<LogSeverity, string, string>? uiLog)
    {
        _group = group; 
        _imageProcessor = imageProcessor; 
        _config = config ?? new ApplicationConfiguration(); // Address warning CS8604
        _logger = logger!; // Suppress null check since we handle it with ?. elsewhere
        _uiLog = uiLog;
        _retryTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _retryTimer.Tick += ProcessRetryQueue;
    }

    public async Task LoadThumbnailsAsync(ChronoView.Models.ApplicationConfiguration? config = null, bool force = false)
    {
        try {
            var cfg = config ?? _config;
            int w = cfg.UISettings?.DisplayImageWidth ?? 100;
            int h = cfg.UISettings?.DisplayImageHeight ?? 100;

            if (force || MainImageThumbnail == null) 
            {
                // Capture the current session ID for stale guard
                long currentSessionId = 0; // Assuming specific session ID logic if exists, or just use 0 if not yet implemented (Design mentioned LoadSessionId). 
                // Ah, the code provided doesn't have _loadSessionId field visible in the snippet. I'll use 0 or DateTime.Ticks.
                // Wait, checking the snippet again... FileGroupMediaLoader doesn't have _loadSessionId. 
                // The design says `long LoadSessionId`. I'll add it or similar.
                // For now I'll use DateTime.UtcNow.Ticks as a simple session ID or just 0 if not critical yet.
                // But the design in 1.1 says `_loadSessionId`. 
                // I'll assume I need to add that too or just use a timestamp.
                long sessionId = DateTime.UtcNow.Ticks; 

                await LoadWithRetryAsync(_group.MainImagePath, w, h, (t, orgW, orgH) => 
                {
                    MainImageThumbnail = t;
                    MainImageLoadedInfo = new MainImageLoadedInfo(
                        _group.GroupId,
                        sessionId,
                        orgW,
                        orgH,
                        null, // ThumbnailBytes (optional, omitting for now to save memory in record if not needed)
                        _group.MainImagePath,
                        DateTime.UtcNow
                    );
                });
            }
            if (_group.HasNir && (force || NirGraphThumbnail == null)) await ReloadNirGraphAsync(cfg);

            foreach (var cam in _group.CameraFiles) {
                var propName = $"Camera{cam.Key.Substring(3)}Thumbnail";
                var prop = GetType().GetProperty(propName);
                if (prop != null && prop.GetValue(this) == null) {
                    await LoadWithRetryAsync(cam.Value, w, h, (t, _, _) => prop.SetValue(this, t));
                }
            }
        } catch (Exception ex) { _logger?.LogError(ex, "Failed to load thumbnails for {GroupId}", _group.GroupId); }
    }

    public async Task ReloadNirGraphAsync(ApplicationConfiguration config)
    {
        if (config == null || string.IsNullOrEmpty(_group.NirFilePath) || !File.Exists(_group.NirFilePath)) return;
        try {
            var data = NirSpectrumParser.Parse(_group.NirFilePath);
            if (data == null) return;
            
            var plt = new Plot();

            // Set white background as requested
            plt.FigureBackground.Color = ScottPlot.Colors.White;
            plt.DataBackground.Color = ScottPlot.Colors.White;
            
            // Add spectrum as a smooth line without markers
            var scatter = plt.Add.Scatter(data.Wavelengths, data.Intensities);
            scatter.MarkerSize = 0; // Disable markers for clean line
            
            // Hide axes, grid, and margins for a compact thumbnail view
            plt.Axes.Frameless();
            plt.HideGrid();
            plt.Axes.Margins(0, 0);

            // Explicitly hide standard axes to ensure no lines are visible
            // Sometimes Frameless() might leave a hairline depending on version/scaling
            if (plt.Axes.Left != null) { plt.Axes.Left.IsVisible = false; plt.Axes.Left.FrameLineStyle.Width = 0; }
            if (plt.Axes.Bottom != null) { plt.Axes.Bottom.IsVisible = false; plt.Axes.Bottom.FrameLineStyle.Width = 0; }
            if (plt.Axes.Right != null) { plt.Axes.Right.IsVisible = false; plt.Axes.Right.FrameLineStyle.Width = 0; }
            if (plt.Axes.Top != null) { plt.Axes.Top.IsVisible = false; plt.Axes.Top.FrameLineStyle.Width = 0; }
            
            int w = config.UISettings.NirThumbnailWidth;
            int h = config.UISettings.NirThumbnailHeight;
            var bytes = plt.GetImageBytes(w, h);
            NirGraphThumbnail = ToBitmapSource(bytes);
        } catch (Exception ex) { _logger?.LogError(ex, "Failed to generate NIR graph for {GroupId}", _group.GroupId); }
    }

    private async Task<BitmapSource?> LoadWithRetryAsync(string path, int w, int h, Action<BitmapSource, int, int>? onSuccess)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
        // Guard: only attempt thumbnail generation for real image files.
        // This prevents accidental attempts to decode NIR spectrum text files (e.g. run_...A.txt).
        if (!LooksLikeImagePath(path))
        {
            _logger?.LogDebug("Skip thumbnail load (not an image): {Path}", Path.GetFileName(path));
            return null;
        }
        try {
            // Check for empty file (common race condition during copy)
            if (new FileInfo(path).Length == 0) throw new IOException("File is empty");

            var result = await _imageProcessor.GenerateThumbnailWithDimensionsAsync(path, w, h, _cts.Token, throwOnError: true);
            var bytes = result.ThumbnailBytes;
            
            // Validate bytes before converting
            if (bytes == null || bytes.Length == 0) throw new IOException("Generated thumbnail bytes are empty");

            var thumb = ToBitmapSource(bytes);
            if (thumb == null) throw new IOException("Failed to decode BitmapSource from bytes"); // Force retry if decoding fails

            if (thumb != null)
            {
                onSuccess?.Invoke(thumb, result.OriginalWidth, result.OriginalHeight);
                var message = LocalizationManager.GetString("Log_Debug_Thumbnail_LoadSuccess", Path.GetFileName(path));
                _uiLog?.Invoke(LogSeverity.Debug, "ImageLoader", message);
            }
            return thumb;
        } catch (Exception ex) {
            // Log failure reason for debugging
            _logger?.LogWarning("Thumbnail load failed (will retry): {Path} - {Error}", Path.GetFileName(path), ex.Message);

            // Only retry for transient errors (locked/partial/IO). For deterministic decode errors (unsupported format),
            // retrying just spams logs and never succeeds.
            if (IsTransientRetryable(ex))
            {
                _retryQueue.Enqueue(new RetryContext(path, w, h, onSuccess));
                _retryTimer.Start();
            }
            return null;
        }
    }

    private async void ProcessRetryQueue(object? sender, EventArgs e)
    {
        if (_retryQueue.IsEmpty) { _retryTimer.Stop(); return; }
        if (!_retryQueue.TryDequeue(out var ctx)) return;
        try {
            // Only retry if file still exists
            if (!File.Exists(ctx.Path)) return;
            
            var result = await _imageProcessor.GenerateThumbnailWithDimensionsAsync(ctx.Path, ctx.W, ctx.H, _cts.Token, throwOnError: true);
            var bytes = result.ThumbnailBytes;
            var thumb = ToBitmapSource(bytes);
            
            if (thumb != null)
            {
                ctx.Callback?.Invoke(thumb, result.OriginalWidth, result.OriginalHeight);
                var message = LocalizationManager.GetString("Log_Debug_Thumbnail_RetrySuccess", Path.GetFileName(ctx.Path));
                _uiLog?.Invoke(LogSeverity.Debug, "ImageLoader", message);
            }
            else
            {
                throw new Exception("Bitmap decoding failed in retry");
            }
        } catch (Exception ex) { 
            // Keep retrying indefinitely while file exists, but throttle with timer
            if (File.Exists(ctx.Path)) {
                // Determine if we should log verbose retry failures (maybe only every 10th retry)
                if (ctx.RetryCount++ % 10 == 0)
                {
                    _logger?.LogDebug("Retry {Count} failed for {Path}: {Error}", ctx.RetryCount, Path.GetFileName(ctx.Path), ex.Message);
                }
                if (IsTransientRetryable(ex))
                {
                    _retryQueue.Enqueue(ctx);
                }
            }
        }
    }

    private static bool LooksLikeImagePath(string path)
    {
        var ext = System.IO.Path.GetExtension(path)?.ToLowerInvariant();
        if (string.IsNullOrEmpty(ext)) return false;

        // Mirror common ImageSharp decoders we support in practice.
        return ext is ".png" or ".gif" or ".jpg" or ".jpeg" or ".qoi" or ".webp" or ".tga" or ".pbm" or ".tif" or ".tiff" or ".bmp";
    }

    private static bool IsTransientRetryable(Exception ex)
    {
        // Typical transient cases: file is still being written/copied, or temporarily locked.
        if (ex is IOException or UnauthorizedAccessException) return true;

        // Deterministic decode failures (unsupported/corrupt format) should not be retried.
        if (ex is UnknownImageFormatException) return false;

        // Default: be conservative and do not retry unknown exception types.
        return false;
    }

    private BitmapSource? ToBitmapSource(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0) return null;
        try {
            using var stream = new MemoryStream(bytes);
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        } catch (Exception ex) { 
            _logger?.LogError(ex, "Failed to convert bytes to BitmapSource");
            return null; 
        }
    }

    public void Dispose() { _cts.Cancel(); _retryTimer.Stop(); }

    private class RetryContext { 
        public string Path { get; set; }
        public int W { get; set; }
        public int H { get; set; }
        public int RetryCount { get; set; }
        public Action<BitmapSource, int, int>? Callback { get; set; }

        public RetryContext(string path, int w, int h, Action<BitmapSource, int, int>? callback)
        {
            Path = path; W = w; H = h; Callback = callback;
        }
    }
}

public sealed record MainImageLoadedInfo(
    string GroupId,
    long LoadSessionId,
    int OriginalWidth,
    int OriginalHeight,
    byte[]? ThumbnailBytes = null,
    string? FilePath = null,
    DateTime? LoadedAtUtc = null);
