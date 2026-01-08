using ChronoView.Models;
using ChronoView.Core.ImageProcessing;
using ChronoView.Core.Analytics;
using ChronoView.Core.Nir;
using ChronoView.Helpers;
using ChronoView.Core.Configuration;
using ChronoView.Core.FileWatching;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Logging;

namespace ChronoView.UI.ViewModels;

public class FileGroupViewModel : ViewModelBase, IDisposable
{
    private readonly FileGroup _fileGroup;
    private readonly FileGroupMediaLoader _mediaLoader;
    private readonly IAbnormalDetector? _abnormalDetector;
    private readonly ILogger<FileGroupViewModel>? _logger;
    
    private bool _isSelected;
    private bool _isAbnormal;
    private string? _abnormalReason;
    private bool _disposed;

    #region Partial Selection Properties
    private bool _isNormalSelected; public bool IsNormalSelected { get => _isNormalSelected; set => SetProperty(ref _isNormalSelected, value); }
    private bool _isNirSelected; public bool IsNirSelected { get => _isNirSelected; set => SetProperty(ref _isNirSelected, value); }
    private bool _isCam1Selected; public bool IsCam1Selected { get => _isCam1Selected; set => SetProperty(ref _isCam1Selected, value); }
    private bool _isCam2Selected; public bool IsCam2Selected { get => _isCam2Selected; set => SetProperty(ref _isCam2Selected, value); }
    private bool _isCam3Selected; public bool IsCam3Selected { get => _isCam3Selected; set => SetProperty(ref _isCam3Selected, value); }
    private bool _isCam4Selected; public bool IsCam4Selected { get => _isCam4Selected; set => SetProperty(ref _isCam4Selected, value); }
    private bool _isCam5Selected; public bool IsCam5Selected { get => _isCam5Selected; set => SetProperty(ref _isCam5Selected, value); }
    private bool _isCam6Selected; public bool IsCam6Selected { get => _isCam6Selected; set => SetProperty(ref _isCam6Selected, value); }
    public bool IsAnyPartialSelected => IsNormalSelected || IsNirSelected || IsCam1Selected || IsCam2Selected || IsCam3Selected || IsCam4Selected || IsCam5Selected || IsCam6Selected;
    #endregion

    public string? MainImagePath { get; private set; }
    public string? NirImagePath { get; private set; }
    public string? Camera1ImagePath => GetCameraImagePath(1);
    public string? Camera2ImagePath => GetCameraImagePath(2);
    public string? Camera3ImagePath => GetCameraImagePath(3);
    public string? Camera4ImagePath => GetCameraImagePath(4);
    public string? Camera5ImagePath => GetCameraImagePath(5);
    public string? Camera6ImagePath => GetCameraImagePath(6);

    #region UI Bindings - Thumbnails (Proxied)
    public BitmapSource? MainImageThumbnail => _mediaLoader.MainImageThumbnail;
    public BitmapSource? NirImageThumbnail => _mediaLoader.NirImageThumbnail;
    public BitmapSource? NirGraphThumbnail => _mediaLoader.NirGraphThumbnail;
    public BitmapSource? Camera1Thumbnail => _mediaLoader.Camera1Thumbnail;
    public BitmapSource? Camera2Thumbnail => _mediaLoader.Camera2Thumbnail;
    public BitmapSource? Camera3Thumbnail => _mediaLoader.Camera3Thumbnail;
    public BitmapSource? Camera4Thumbnail => _mediaLoader.Camera4Thumbnail;
    public BitmapSource? Camera5Thumbnail => _mediaLoader.Camera5Thumbnail;
    public BitmapSource? Camera6Thumbnail => _mediaLoader.Camera6Thumbnail;
    #endregion

    #region Label Properties
    public string NormalLabel => !string.IsNullOrEmpty(NormalFolder) ? Path.GetFileName(NormalFolder) : "";
    public string NormalImageSizeLabel => GetNormalImageSize();
    public string NirLabel => !string.IsNullOrEmpty(NirKey) ? NirKey : "";
    public string Camera1Label => GetCameraLabel(1);
    public string Camera2Label => GetCameraLabel(2);
    public string Camera3Label => GetCameraLabel(3);
    public string Camera4Label => GetCameraLabel(4);
    public string Camera5Label => GetCameraLabel(5);
    public string Camera6Label => GetCameraLabel(6);
    private string GetCameraLabel(int num) => !string.IsNullOrEmpty(GetCameraImagePath(num)) ? Path.GetFileName(GetCameraImagePath(num)) ?? "" : "";
    
    private string GetNormalImageSize()
    {
        if (string.IsNullOrEmpty(NormalFolder) || !Directory.Exists(NormalFolder)) return "";
        var stitchedPath = Path.Combine(NormalFolder, "stitched_original.png");
        if (!File.Exists(stitchedPath)) return "";
        try {
            using (var stream = new FileStream(stitchedPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var frame = BitmapFrame.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
                return $"{frame.PixelWidth}x{frame.PixelHeight}";
            }
        } catch { return ""; }
    }
    #endregion

    public FileGroupViewModel(FileGroup fileGroup, IImageProcessor imageProcessor, IMonitoringOrchestrator? orchestrator, IAbnormalDetector? abnormalDetector, ChronoView.Models.ApplicationConfiguration? configuration, ILogger<FileGroupViewModel>? logger, Action<LogSeverity, string, string>? uiLog)
    {
        _fileGroup = fileGroup; _abnormalDetector = abnormalDetector; _logger = logger;
        _mediaLoader = new FileGroupMediaLoader(fileGroup, imageProcessor, orchestrator, configuration, logger, uiLog);
        _mediaLoader.PropertyChanged += (s, e) => OnPropertyChanged(e.PropertyName);
        InitializeImagePaths();
        CheckAbnormalStatus();
    }

    public FileGroup Model => _fileGroup;
    public string GroupId => _fileGroup.GroupId;
    public string NirKey => _fileGroup.NirKey;
    public string NormalFolder => _fileGroup.NormalFolder;
    public int LineNumber => _fileGroup.LineNumber;
    public bool HasNir => _fileGroup.HasNir;
    public DateTime CreatedAt => _fileGroup.CreatedAt;
    public GroupStatus Status => _fileGroup.Status;
    public string StatusText => IsAbnormal ? "Abnormal" : (IsGroupComplete() ? "Complete" : "Pending");

    private bool IsGroupComplete()
    {
        if (LineNumber == 1) return !string.IsNullOrEmpty(MainImagePath) && !string.IsNullOrEmpty(Camera1ImagePath) && !string.IsNullOrEmpty(Camera2ImagePath) && !string.IsNullOrEmpty(Camera3ImagePath);
        if (LineNumber == 2) return !string.IsNullOrEmpty(MainImagePath) && !string.IsNullOrEmpty(Camera4ImagePath) && !string.IsNullOrEmpty(Camera5ImagePath) && !string.IsNullOrEmpty(Camera6ImagePath);
        return false;
    }

    public bool IsAbnormal { get => _isAbnormal || Status == GroupStatus.Abnormal; private set { if (SetProperty(ref _isAbnormal, value)) OnPropertyChanged(nameof(StatusText)); } }
    public bool IsSelected { get => _isSelected; set { if (SetProperty(ref _isSelected, value)) { IsNormalSelected = IsNirSelected = IsCam1Selected = IsCam2Selected = IsCam3Selected = IsCam4Selected = IsCam5Selected = IsCam6Selected = value; } } }

    public async Task LoadThumbnailsAsync() => await _mediaLoader.LoadThumbnailsAsync();
    public async Task ReloadNirGraphAsync(ChronoView.Models.ApplicationConfiguration config) => await _mediaLoader.ReloadNirGraphAsync(config);

    public void Refresh() { InitializeImagePaths(); CheckAbnormalStatus(); OnPropertyChanged(string.Empty); }
    private void InitializeImagePaths() { if (_fileGroup.HasNir) NirImagePath = _fileGroup.NirFilePath; MainImagePath = !string.IsNullOrEmpty(_fileGroup.MainImagePath) ? _fileGroup.MainImagePath : (string.IsNullOrEmpty(_fileGroup.NormalFolder) ? null : Path.Combine(_fileGroup.NormalFolder, "stitched_original.png")); }
    private string? GetCameraImagePath(int num) => _fileGroup.CameraFiles.TryGetValue($"cam{num}", out var path) ? path : null;
    private void CheckAbnormalStatus() { if (_abnormalDetector == null) return; try { IsAbnormal = _abnormalDetector.IsGroupAbnormal(_fileGroup); _abnormalReason = IsAbnormal ? "Detected" : null; } catch { IsAbnormal = false; } }
    public void Dispose() { if (_disposed) return; _mediaLoader.Dispose(); _disposed = true; }

    public List<string> GetSelectedComponents()
    {
        var comps = new List<string>();
        if (IsNormalSelected && !string.IsNullOrEmpty(NormalFolder)) comps.Add("Normal");
        if (IsNirSelected && HasNir) comps.Add("Nir");
        if (IsCam1Selected && Camera1ImagePath != null) comps.Add("Cam1");
        if (IsCam2Selected && Camera2ImagePath != null) comps.Add("Cam2");
        if (IsCam3Selected && Camera3ImagePath != null) comps.Add("Cam3");
        if (IsCam4Selected && Camera4ImagePath != null) comps.Add("Cam4");
        if (IsCam5Selected && Camera5ImagePath != null) comps.Add("Cam5");
        if (IsCam6Selected && Camera6ImagePath != null) comps.Add("Cam6");
        return comps;
    }

    public bool HasAnyRemainingData() =>
        !string.IsNullOrEmpty(NormalFolder) || HasNir ||
        Camera1ImagePath != null || Camera2ImagePath != null || Camera3ImagePath != null ||
        Camera4ImagePath != null || Camera5ImagePath != null || Camera6ImagePath != null;
    public async Task ReloadMediaAsync(ChronoView.Models.ApplicationConfiguration? config = null)
    {
        await _mediaLoader.LoadThumbnailsAsync(config, force: true);
        // LoadThumbnailsAsync with force=true already reloads NIR graph, so no separate call needed
    }
}
