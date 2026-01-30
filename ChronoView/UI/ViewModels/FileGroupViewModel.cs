using ChronoView.Models;
using ChronoView.Core.ImageProcessing;
using ChronoView.Core.Localization;
using ChronoView.Core.Analytics;
using ChronoView.Core.NIR.Shared;
using ChronoView.Core.NIR.Interfaces;
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
    private readonly IImageProcessor? _imageProcessor;
    private readonly IAbnormalDetector? _abnormalDetector;
    private readonly ILogger<FileGroupViewModel>? _logger;
    private readonly Action<LogSeverity, string, string>? _uiLog;
    private readonly INirDataProvider? _nirDataProvider;

    // NIR2 cached data (lazy loaded from ApiBasedNirProvider)
    private NirSpectrum? _nir2Data;
    
    private bool _isSelected;
    private bool _isAbnormal;
    private string? _abnormalReason;
    private bool _disposed;

    #region Partial Selection Properties
    private bool _isNormalSelected; 
    public bool IsNormalSelected { get => _isNormalSelected; set { if (SetProperty(ref _isNormalSelected, value)) NotifySelectionComputed(); } }
    private bool _isNirSelected; 
    public bool IsNirSelected { get => _isNirSelected; set { if (SetProperty(ref _isNirSelected, value)) NotifySelectionComputed(); } }
    private bool _isCam1Selected; 
    public bool IsCam1Selected { get => _isCam1Selected; set { if (SetProperty(ref _isCam1Selected, value)) NotifySelectionComputed(); } }
    private bool _isCam2Selected; 
    public bool IsCam2Selected { get => _isCam2Selected; set { if (SetProperty(ref _isCam2Selected, value)) NotifySelectionComputed(); } }
    private bool _isCam3Selected; 
    public bool IsCam3Selected { get => _isCam3Selected; set { if (SetProperty(ref _isCam3Selected, value)) NotifySelectionComputed(); } }
    private bool _isCam4Selected; 
    public bool IsCam4Selected { get => _isCam4Selected; set { if (SetProperty(ref _isCam4Selected, value)) NotifySelectionComputed(); } }
    private bool _isCam5Selected; 
    public bool IsCam5Selected { get => _isCam5Selected; set { if (SetProperty(ref _isCam5Selected, value)) NotifySelectionComputed(); } }
    private bool _isCam6Selected; 
    public bool IsCam6Selected { get => _isCam6Selected; set { if (SetProperty(ref _isCam6Selected, value)) NotifySelectionComputed(); } }

    // UI state: any checkbox checked (for button enable/disable)
    public bool IsAnyPartialSelected => IsNormalSelected || IsNirSelected || IsCam1Selected || IsCam2Selected || IsCam3Selected || IsCam4Selected || IsCam5Selected || IsCam6Selected;

    // Delete logic: at least one component with actual data is selected
    public bool HasAnySelectedComponent =>
        (IsNormalSelected && !string.IsNullOrEmpty(NormalFolder)) ||
        (IsNirSelected && HasNir) ||
        (IsCam1Selected && Camera1ImagePath != null) ||
        (IsCam2Selected && Camera2ImagePath != null) ||
        (IsCam3Selected && Camera3ImagePath != null) ||
        (IsCam4Selected && Camera4ImagePath != null) ||
        (IsCam5Selected && Camera5ImagePath != null) ||
        (IsCam6Selected && Camera6ImagePath != null);

    // Classification: all available components are selected
    public bool IsFullySelected =>
        (string.IsNullOrEmpty(NormalFolder) || IsNormalSelected) &&
        (!HasNir || IsNirSelected) &&
        (Camera1ImagePath == null || IsCam1Selected) &&
        (Camera2ImagePath == null || IsCam2Selected) &&
        (Camera3ImagePath == null || IsCam3Selected) &&
        (Camera4ImagePath == null || IsCam4Selected) &&
        (Camera5ImagePath == null || IsCam5Selected) &&
        (Camera6ImagePath == null || IsCam6Selected);

    private void NotifySelectionComputed()
    {
        OnPropertyChanged(nameof(IsAnyPartialSelected));
        OnPropertyChanged(nameof(HasAnySelectedComponent));
        OnPropertyChanged(nameof(IsFullySelected));
    }
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
    
    private int _cachedWidth;
    private int _cachedHeight;
    private double? _cachedRatioDiff;

    public string NormalImageSizeLabel 
    {
        get
        {
            if (_cachedWidth > 0 && _cachedHeight > 0)
            {
                var baseLabel = $"{_cachedWidth}x{_cachedHeight}";
                if (_cachedRatioDiff.HasValue)
                {
                    return $"{baseLabel} (Diff: {_cachedRatioDiff.Value:F2})";
                }
                return baseLabel;
            }
            return GetNormalImageSize(); // Fallback
        }
    }

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

    public FileGroupViewModel(FileGroup fileGroup, IImageProcessor imageProcessor, IMonitoringOrchestrator? orchestrator, IAbnormalDetector? abnormalDetector, ChronoView.Models.ApplicationConfiguration? configuration, ILogger<FileGroupViewModel>? logger, Action<LogSeverity, string, string>? uiLog, INirDataProvider? nirDataProvider = null)
    {
        _fileGroup = fileGroup; _imageProcessor = imageProcessor; _abnormalDetector = abnormalDetector; _logger = logger; _uiLog = uiLog; _nirDataProvider = nirDataProvider;
        _mediaLoader = new FileGroupMediaLoader(fileGroup, imageProcessor, orchestrator, configuration, logger, uiLog);
        _mediaLoader.PropertyChanged += (s, e) => 
        {
            OnPropertyChanged(e.PropertyName);
            
            // NEW: Calculate Diff only when LoadedInfo is available (single SSoT)
            // This replaces the old CheckAbnormalStatus() ratio check
            if (e.PropertyName == nameof(_mediaLoader.MainImageLoadedInfo) &&
                _mediaLoader.MainImageLoadedInfo != null)
            {
                CalculateAbnormalStatusFromLoadedInfo(_mediaLoader.MainImageLoadedInfo);
            }
        };
        InitializeImagePaths();
        CheckAbnormalStatus(); // Checks NIR status only (fast). Ratio check is now event-driven.
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

    #region NIR2 Properties
    /// <summary>
    /// True if this group has NIR2 data (NirKey starts with "chunk:").
    /// </summary>
    public bool HasNir2 => !string.IsNullOrEmpty(NirKey) && NirKey.StartsWith("chunk:", StringComparison.Ordinal);

    /// <summary>
    /// NIR2 Protein value (cached from ApiBasedNirProvider).
    /// Returns null if not yet loaded or not NIR2.
    /// </summary>
    public double? Nir2Protein
    {
        get
        {
            if (!HasNir2 || _nir2Data == null) return null;
            if (_nir2Data.Metadata != null && _nir2Data.Metadata.TryGetValue("protein", out var proteinValue))
            {
                if (proteinValue != null && double.TryParse(proteinValue.ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var protein))
                    return protein;
            }
            return _nir2Data.Protein; // Fallback to direct property
        }
    }

    /// <summary>
    /// NIR2 Moisture value (cached from ApiBasedNirProvider).
    /// Returns null if not yet loaded or not NIR2.
    /// </summary>
    public double? Nir2Moisture
    {
        get
        {
            if (!HasNir2 || _nir2Data == null) return null;
            if (_nir2Data.Metadata != null && _nir2Data.Metadata.TryGetValue("moisture", out var moistureValue))
            {
                if (moistureValue != null && double.TryParse(moistureValue.ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var moisture))
                    return moisture;
            }
            return _nir2Data.Moisture; // Fallback to direct property
        }
    }

    /// <summary>
    /// NIR2 display text for UI (e.g., "P: 12.5% M: 8.3%").
    /// </summary>
    public string Nir2DisplayText
    {
        get
        {
            var protein = Nir2Protein;
            var moisture = Nir2Moisture;
            if (protein.HasValue && moisture.HasValue)
            {
                return $"P: {protein.Value:F1}%\nM: {moisture.Value:F1}%";
            }
            return HasNir2 ? "Loading..." : "";
        }
    }

    /// <summary>
    /// Loads NIR2 data from the provider for display.
    /// Should be called when thumbnails are loaded.
    /// </summary>
    public async Task LoadNir2DataAsync()
    {
        if (!HasNir2 || _nirDataProvider == null || _nir2Data != null) return;

        try
        {
            _nir2Data = await _nirDataProvider.LoadNirDataAsync(NirKey);
            OnPropertyChanged(nameof(Nir2DisplayText));
            OnPropertyChanged(nameof(Nir2Protein));
            OnPropertyChanged(nameof(Nir2Moisture));
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Failed to load NIR2 data for {NirKey}", NirKey);
        }
    }
    #endregion

    private bool IsGroupComplete()
    {
        if (LineNumber == 1) return !string.IsNullOrEmpty(MainImagePath) && !string.IsNullOrEmpty(Camera1ImagePath) && !string.IsNullOrEmpty(Camera2ImagePath) && !string.IsNullOrEmpty(Camera3ImagePath);
        if (LineNumber == 2) return !string.IsNullOrEmpty(MainImagePath) && !string.IsNullOrEmpty(Camera4ImagePath) && !string.IsNullOrEmpty(Camera5ImagePath) && !string.IsNullOrEmpty(Camera6ImagePath);
        return false;
    }

    public bool IsAbnormal { get => _isAbnormal || Status == GroupStatus.Abnormal; private set { if (SetProperty(ref _isAbnormal, value)) OnPropertyChanged(nameof(StatusText)); } }
    public bool IsSelected { get => _isSelected; set { if (SetProperty(ref _isSelected, value)) { IsNormalSelected = IsNirSelected = IsCam1Selected = IsCam2Selected = IsCam3Selected = IsCam4Selected = IsCam5Selected = IsCam6Selected = value; NotifySelectionComputed(); } } }

    public async Task LoadThumbnailsAsync()
    {
        await _mediaLoader.LoadThumbnailsAsync();
        TryCalculateDiffFromDimensionsFallback();
        await LoadNir2DataAsync(); // Load NIR2 data for display
    }
    public async Task ReloadNirGraphAsync(ChronoView.Models.ApplicationConfiguration config) => await _mediaLoader.ReloadNirGraphAsync(config);

    public void Refresh() { InitializeImagePaths(); CheckAbnormalStatus(); OnPropertyChanged(string.Empty); }
    private void InitializeImagePaths() 
    { 
        if (_fileGroup.HasNir) NirImagePath = _fileGroup.NirFilePath; 

        if (!string.IsNullOrEmpty(_fileGroup.MainImagePath))
        {
            MainImagePath = _fileGroup.MainImagePath;
        }
        else if (!string.IsNullOrEmpty(_fileGroup.NormalFolder))
        {
             var stitchedPath = Path.Combine(_fileGroup.NormalFolder, "stitched_original.png");
             MainImagePath = stitchedPath;
             
             if (!File.Exists(stitchedPath))
             {
                 var message = LocalizationManager.GetString("Log_Warn_ImageMissing", _fileGroup.NormalFolder);
                 _uiLog?.Invoke(LogSeverity.Warning, "ImageLoader", message);
             }
        }
        else
        {
            MainImagePath = null;
        }
    }
    private string? GetCameraImagePath(int num) => _fileGroup.CameraFiles.TryGetValue($"cam{num}", out var path) ? path : null;
    private void CheckAbnormalStatus() 
    { 
        if (_abnormalDetector == null) return;
        
        try 
        { 
            // 1. NIR-only group check (existing logic, fast metadata check)
            if (_abnormalDetector.IsGroupAbnormal(_fileGroup))
            {
                IsAbnormal = true;
                _abnormalReason = "NIR-only group";
                return;
            }

            // Ratio check is removed from here to prevent file locking/race conditions.
            // It is now performed in CalculateAbnormalStatusFromLoadedInfo triggered by media load.

            // If not NIR abnormal, reset status (unless Ratio check sets it later)
            // Note: If we had a previous ratio error, it should be cleared if we re-check? 
            // Actually, if we re-check strictly via this method, we might reset IsAbnormal.
            // But IsAbnormal logic requires coordination.
            // For now, assume this method handles static group abnormalities.
            // If Ratio Abnormality was set, this method might clear it if we just do IsAbnormal = false at end.
            // So we should be careful. 
            // However, IsAbnormal property is set by this method. 
            // If we want to preserve Ratio abnormality, we need to check _cachedRatioDiff or similar.
            // But typically CheckAbnormalStatus is called on init or refresh.
            // If we Refresh(), we reset everything.
            
            IsAbnormal = false;
            _abnormalReason = null;
        } 
        catch (Exception ex) 
        { 
            _logger?.LogError(ex, "Failed to check abnormal status for group {GroupId}", GroupId);
            IsAbnormal = false; 
        } 
    }

    private void TryCalculateDiffFromDimensionsFallback()
    {
        // If main image info was not produced (thumbnail load failure), try header-only dimensions.
        if (_mediaLoader.MainImageLoadedInfo != null) return;
        if (string.IsNullOrEmpty(MainImagePath)) return;

        try
        {
            var (width, height) = _imageProcessor.GetImageDimensions(MainImagePath);
            if (width <= 0 || height <= 0) return;

            var info = new MainImageLoadedInfo(
                GroupId,
                DateTime.UtcNow.Ticks,
                width,
                height,
                null,
                MainImagePath,
                DateTime.UtcNow);

            CalculateAbnormalStatusFromLoadedInfo(info);
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Failed to calculate diff from dimensions fallback: {Path}", MainImagePath);
        }
    }

    private void CalculateAbnormalStatusFromLoadedInfo(MainImageLoadedInfo info)
    {
        // Stale guard: verify this info belongs to this group
        if (info.GroupId != GroupId) return;

        if (_abnormalDetector == null || info.OriginalWidth <= 0 || info.OriginalHeight <= 0)
        {
            // If dimensions are invalid/missing, we can't calculate diff.
            // Just update cached dimensions for display if valid.
            if (info.OriginalWidth > 0 && info.OriginalHeight > 0)
            {
                _cachedWidth = info.OriginalWidth;
                _cachedHeight = info.OriginalHeight;
                OnPropertyChanged(nameof(NormalImageSizeLabel));
            }
            return;
        }

        // Cache dimensions (for UI display)
        _cachedWidth = info.OriginalWidth;
        _cachedHeight = info.OriginalHeight;

        try
        {
            var context = AbnormalDetectorService.ExtractContext(_fileGroup.NormalFolder);
            var (isAbnormal, ratioDiff) = _abnormalDetector.AddAndCheckImage(
                info.OriginalWidth, info.OriginalHeight, context);

            // Cache ratio diff
            _cachedRatioDiff = ratioDiff;

            // Update UI
            OnPropertyChanged(nameof(NormalImageSizeLabel));

            if (isAbnormal)
            {
                IsAbnormal = true;
                _abnormalReason = $"Abnormal Ratio (Diff: {ratioDiff:F3})";
                _logger?.LogWarning("Group {GroupId} detected as abnormal: {Reason}",
                    GroupId, _abnormalReason);
            }
            else
            {
                // If previously abnormal due to ratio, clear it?
                // But CheckAbnormalStatus might have cleared it already.
                // If checks are cumulative, we should handle it.
                // Current logic: CheckAbnormalStatus runs first (clears), then this runs (sets).
                // If CheckAbnormalStatus runs LATER (e.g. some other trigger), it clears.
                // But CheckAbnormalStatus is mainly Init/Refresh.
                // This event happens after Init.
                // So this will overwrite the IsAbnormal state correctly.
            }
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Failed to analyze image dimensions for abnormal detection: {Path}", info.FilePath);
        }
    }
    public void Dispose() { if (_disposed) return; _mediaLoader.Dispose(); _disposed = true; }

    public List<string> GetSelectedComponents()
    {
        // intersection rule: if row is not selected, return nothing
        if (!IsSelected) return new List<string>();

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

    /// <summary>
    /// Returns selected components with their file/folder names for detailed logging.
    /// Format: "ComponentName: FileName" pairs.
    /// </summary>
    public List<string> GetSelectedComponentDetails()
    {
        // intersection rule: if row is not selected, return nothing
        if (!IsSelected) return new List<string>();

        var details = new List<string>();
        if (IsNormalSelected && !string.IsNullOrEmpty(NormalFolder)) 
            details.Add($"Normal: {Path.GetFileName(NormalFolder)}");
        if (IsNirSelected && HasNir && !string.IsNullOrEmpty(NirImagePath)) 
            details.Add($"Nir: {Path.GetFileName(NirImagePath)}");
        if (IsCam1Selected && Camera1ImagePath != null) 
            details.Add($"Cam1: {Path.GetFileName(Camera1ImagePath)}");
        if (IsCam2Selected && Camera2ImagePath != null) 
            details.Add($"Cam2: {Path.GetFileName(Camera2ImagePath)}");
        if (IsCam3Selected && Camera3ImagePath != null) 
            details.Add($"Cam3: {Path.GetFileName(Camera3ImagePath)}");
        if (IsCam4Selected && Camera4ImagePath != null) 
            details.Add($"Cam4: {Path.GetFileName(Camera4ImagePath)}");
        if (IsCam5Selected && Camera5ImagePath != null) 
            details.Add($"Cam5: {Path.GetFileName(Camera5ImagePath)}");
        if (IsCam6Selected && Camera6ImagePath != null) 
            details.Add($"Cam6: {Path.GetFileName(Camera6ImagePath)}");
        return details;
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
