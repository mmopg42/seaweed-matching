using ChronoView.Core.Configuration;
using ChronoView.Core.ImageProcessing;
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

            if (force || MainImageThumbnail == null) MainImageThumbnail = await LoadWithRetryAsync(_group.MainImagePath, w, h, (t) => MainImageThumbnail = t);
            if (_group.HasNir && (force || NirImageThumbnail == null)) NirImageThumbnail = await LoadWithRetryAsync(_group.NirFilePath, w, h, (t) => NirImageThumbnail = t);
            if (_group.HasNir && (force || NirGraphThumbnail == null)) await ReloadNirGraphAsync(cfg);

            foreach (var cam in _group.CameraFiles) {
                var propName = $"Camera{cam.Key.Substring(3)}Thumbnail";
                var prop = GetType().GetProperty(propName);
                if (prop != null && prop.GetValue(this) == null) {
                    await LoadWithRetryAsync(cam.Value, w, h, (t) => prop.SetValue(this, t));
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

    private async Task<BitmapSource?> LoadWithRetryAsync(string path, int w, int h, Action<BitmapSource>? onSuccess)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
        try {
            // Check for empty file (common race condition during copy)
            if (new FileInfo(path).Length == 0) throw new IOException("File is empty");

            var bytes = await _imageProcessor.GenerateThumbnailAsync(path, w, h, _cts.Token, throwOnError: true);
            var thumb = ToBitmapSource(bytes);
            if (thumb != null) onSuccess?.Invoke(thumb);
            return thumb;
        } catch (Exception) {
            // Queue for retry on any error (including 0-byte, locked, or partial files)
            _retryQueue.Enqueue(new RetryContext(path, w, h, onSuccess));
            _retryTimer.Start();
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
            
            var bytes = await _imageProcessor.GenerateThumbnailAsync(ctx.Path, ctx.W, ctx.H, _cts.Token, throwOnError: true);
            var thumb = ToBitmapSource(bytes);
            if (thumb != null) { ctx.Callback?.Invoke(thumb); }
        } catch { 
            // Keep retrying indefinitely while file exists
            if (File.Exists(ctx.Path)) {
                _retryQueue.Enqueue(ctx); 
            }
        }
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
        } catch { return null; }
    }

    public void Dispose() { _cts.Cancel(); _retryTimer.Stop(); }

    private class RetryContext { 
        public string Path { get; set; }
        public int W { get; set; }
        public int H { get; set; }
        public int RetryCount { get; set; }
        public Action<BitmapSource>? Callback { get; set; }

        public RetryContext(string path, int w, int h, Action<BitmapSource>? callback)
        {
            Path = path; W = w; H = h; Callback = callback;
        }
    }
}
