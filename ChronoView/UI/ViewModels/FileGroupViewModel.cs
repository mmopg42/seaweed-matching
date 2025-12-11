using ChronoView.Models;
using ChronoView.Core.ImageProcessing;
using ChronoView.Core.Analytics;
using ChronoView.Core.Nir;
using ChronoView.Helpers;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows;
using Microsoft.Extensions.Logging;
using WpfApplication = System.Windows.Application;

namespace ChronoView.UI.ViewModels;

/// <summary>
/// ViewModel for displaying individual file groups in the UI.
/// </summary>
public class FileGroupViewModel : ViewModelBase, IDisposable
{
    private readonly FileGroup _fileGroup;
    private readonly IImageProcessor _imageProcessor;
    private readonly IAbnormalDetector? _abnormalDetector;
    private readonly ApplicationConfiguration? _configuration;
    private readonly ILogger<FileGroupViewModel>? _logger;
    private readonly Action<LogSeverity, string, string>? _uiLog;
    private static readonly HashSet<string> _imageExtensions = new(StringComparer.OrdinalIgnoreCase)
    { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tif", ".tiff", ".webp" };
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    
    private bool _isSelected;
    private bool _isAbnormal;
    private string? _abnormalReason;
    private string? _mainImagePath;
    private string? _nirImagePath;
    private Dictionary<string, string?> _cameraImagePaths = new();
    private BitmapSource? _mainImageThumbnail;
    private BitmapSource? _nirImageThumbnail;
    private BitmapSource? _nirGraphThumbnail;
    private BitmapSource? _camera1Thumbnail;
    private BitmapSource? _camera2Thumbnail;
    private BitmapSource? _camera3Thumbnail;
    private BitmapSource? _camera4Thumbnail;
    private BitmapSource? _camera5Thumbnail;
    private BitmapSource? _camera6Thumbnail;
    private bool _disposed;

    /// <summary>
    /// Creates a new FileGroupViewModel wrapping a FileGroup model.
    /// </summary>
    /// <param name="fileGroup">The FileGroup model to wrap.</param>
    /// <param name="imageProcessor">Image processor for thumbnail generation.</param>
    /// <param name="abnormalDetector">Optional abnormal detector for z-score analysis.</param>
    /// <param name="configuration">Optional application configuration for NIR graph settings.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    /// <param name="uiLog">Optional UI log sink (Severity, Source, Message).</param>
    public FileGroupViewModel(
        FileGroup fileGroup,
        IImageProcessor imageProcessor,
        IAbnormalDetector? abnormalDetector = null,
        ApplicationConfiguration? configuration = null,
        ILogger<FileGroupViewModel>? logger = null,
        Action<LogSeverity, string, string>? uiLog = null)
    {
        _fileGroup = fileGroup ?? throw new ArgumentNullException(nameof(fileGroup));
        _imageProcessor = imageProcessor ?? throw new ArgumentNullException(nameof(imageProcessor));
        _abnormalDetector = abnormalDetector;
        _configuration = configuration;
        _logger = logger;
        _uiLog = uiLog;

        InitializeImagePaths();
        CheckAbnormalStatus();

        _logger?.LogInformation("FileGroupViewModel created for {GroupId} | HasNir={HasNir} NirPath={NirPath} NormalFolder={Normal} MainImage={MainImage} Cameras={CamCount}",
            GroupId, _fileGroup.HasNir, _fileGroup.NirFilePath, _fileGroup.NormalFolder, _fileGroup.MainImagePath, _fileGroup.CameraFiles?.Count ?? 0);
        _uiLog?.Invoke(LogSeverity.Debug, "Group", $"VM created {GroupId} HasNir={_fileGroup.HasNir} Normal={_fileGroup.NormalFolder} Main={_fileGroup.MainImagePath} CamCount={_fileGroup.CameraFiles?.Count ?? 0}");
    }

    /// <summary>
    /// The underlying FileGroup model.
    /// </summary>
    public FileGroup Model => _fileGroup;

    /// <summary>
    /// Unique identifier for this file group.
    /// </summary>
    public string GroupId => _fileGroup.GroupId;

    /// <summary>
    /// NIR file key/identifier.
    /// </summary>
    public string NirKey => _fileGroup.NirKey;

    /// <summary>
    /// Path to the normal folder.
    /// </summary>
    public string NormalFolder => _fileGroup.NormalFolder;

    /// <summary>
    /// Line number for multi-line monitoring.
    /// </summary>
    public int LineNumber => _fileGroup.LineNumber;

    /// <summary>
    /// Indicates whether this group has an associated NIR file.
    /// </summary>
    public bool HasNir => _fileGroup.HasNir;

    /// <summary>
    /// Timestamp when this group was created.
    /// </summary>
    public DateTime CreatedAt => _fileGroup.CreatedAt;

    /// <summary>
    /// Current status of this file group.
    /// </summary>
    public GroupStatus Status => _fileGroup.Status;

    /// <summary>
    /// Status display text for UI, including abnormal indicator.
    /// </summary>
    public string StatusText
    {
        get
        {
            var baseStatus = Status switch
            {
                GroupStatus.Complete => "✓ Complete",
                GroupStatus.Pending => "⏳ Pending",
                GroupStatus.Processing => "⚙ Processing",
                GroupStatus.Moved => "📦 Moved",
                GroupStatus.Error => "✗ Error",
                GroupStatus.Abnormal => "⚠ Abnormal",
                _ => Status.ToString()
            };
            
            // Append abnormal indicator if detected by z-score analysis
            if (IsAbnormal && Status != GroupStatus.Abnormal)
            {
                return $"{baseStatus} (⚠ Abnormal)";
            }
            
            return baseStatus;
        }
    }

    /// <summary>
    /// Indicates whether this file group is detected as abnormal.
    /// This can be set by either the model's Status or by the IAbnormalDetector.
    /// </summary>
    public bool IsAbnormal
    {
        get => _isAbnormal || Status == GroupStatus.Abnormal;
        private set
        {
            if (SetProperty(ref _isAbnormal, value))
            {
                OnPropertyChanged(nameof(StatusText));
            }
        }
    }

    /// <summary>
    /// Reason for abnormal status (if applicable).
    /// </summary>
    public string? AbnormalReason
    {
        get => _abnormalReason;
        private set => SetProperty(ref _abnormalReason, value);
    }

    /// <summary>
    /// Whether this group is selected in the UI.
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    /// <summary>
    /// Path to the main image for display.
    /// </summary>
    public string? MainImagePath
    {
        get => _mainImagePath;
        set => SetProperty(ref _mainImagePath, value);
    }

    /// <summary>
    /// Path to the NIR image for display.
    /// </summary>
    public string? NirImagePath
    {
        get => _nirImagePath;
        set => SetProperty(ref _nirImagePath, value);
    }

    /// <summary>
    /// Gets the camera image path for a specific camera number (1-6).
    /// </summary>
    public string? GetCameraImagePath(int cameraNumber)
    {
        var key = $"cam{cameraNumber}";
        return _cameraImagePaths.TryGetValue(key, out var path) ? path : null;
    }

    /// <summary>
    /// Sets the camera image path for a specific camera number (1-6).
    /// </summary>
    public void SetCameraImagePath(int cameraNumber, string? path)
    {
        var key = $"cam{cameraNumber}";
        if (_cameraImagePaths.TryGetValue(key, out var currentPath) && currentPath == path)
            return;

        _cameraImagePaths[key] = path;
        OnPropertyChanged($"Camera{cameraNumber}ImagePath");
    }

    /// <summary>
    /// Camera 1 image path (for data binding).
    /// </summary>
    public string? Camera1ImagePath
    {
        get => GetCameraImagePath(1);
        set => SetCameraImagePath(1, value);
    }

    /// <summary>
    /// Camera 2 image path (for data binding).
    /// </summary>
    public string? Camera2ImagePath
    {
        get => GetCameraImagePath(2);
        set => SetCameraImagePath(2, value);
    }

    /// <summary>
    /// Camera 3 image path (for data binding).
    /// </summary>
    public string? Camera3ImagePath
    {
        get => GetCameraImagePath(3);
        set => SetCameraImagePath(3, value);
    }

    /// <summary>
    /// Camera 4 image path (for data binding).
    /// </summary>
    public string? Camera4ImagePath
    {
        get => GetCameraImagePath(4);
        set => SetCameraImagePath(4, value);
    }

    /// <summary>
    /// Camera 5 image path (for data binding).
    /// </summary>
    public string? Camera5ImagePath
    {
        get => GetCameraImagePath(5);
        set => SetCameraImagePath(5, value);
    }

    /// <summary>
    /// Camera 6 image path (for data binding).
    /// </summary>
    public string? Camera6ImagePath
    {
        get => GetCameraImagePath(6);
        set => SetCameraImagePath(6, value);
    }

    /// <summary>
    /// Cam1ImagePath alias for XAML binding compatibility.
    /// </summary>
    public string? Cam1ImagePath => Camera1ImagePath;

    /// <summary>
    /// Cam2ImagePath alias for XAML binding compatibility.
    /// </summary>
    public string? Cam2ImagePath => Camera2ImagePath;

    /// <summary>
    /// Cam3ImagePath alias for XAML binding compatibility.
    /// </summary>
    public string? Cam3ImagePath => Camera3ImagePath;

    /// <summary>
    /// Cam4ImagePath alias for XAML binding compatibility.
    /// </summary>
    public string? Cam4ImagePath => Camera4ImagePath;

    /// <summary>
    /// Cam5ImagePath alias for XAML binding compatibility.
    /// </summary>
    public string? Cam5ImagePath => Camera5ImagePath;

    /// <summary>
    /// Cam6ImagePath alias for XAML binding compatibility.
    /// </summary>
    public string? Cam6ImagePath => Camera6ImagePath;

    /// <summary>
    /// Main image thumbnail for display in DataGrid.
    /// </summary>
    public BitmapSource? MainImageThumbnail
    {
        get => _mainImageThumbnail;
        private set => SetProperty(ref _mainImageThumbnail, value);
    }

    /// <summary>
    /// NIR image thumbnail for display in DataGrid.
    /// </summary>
    public BitmapSource? NirImageThumbnail
    {
        get => _nirImageThumbnail;
        private set => SetProperty(ref _nirImageThumbnail, value);
    }

    /// <summary>
    /// NIR graph thumbnail for display in DataGrid.
    /// </summary>
    public BitmapSource? NirGraphThumbnail
    {
        get => _nirGraphThumbnail;
        private set => SetProperty(ref _nirGraphThumbnail, value);
    }

    /// <summary>
    /// Camera 1 thumbnail for display in DataGrid.
    /// </summary>
    public BitmapSource? Camera1Thumbnail
    {
        get => _camera1Thumbnail;
        private set => SetProperty(ref _camera1Thumbnail, value);
    }

    /// <summary>
    /// Camera 2 thumbnail for display in DataGrid.
    /// </summary>
    public BitmapSource? Camera2Thumbnail
    {
        get => _camera2Thumbnail;
        private set => SetProperty(ref _camera2Thumbnail, value);
    }

    /// <summary>
    /// Camera 3 thumbnail for display in DataGrid.
    /// </summary>
    public BitmapSource? Camera3Thumbnail
    {
        get => _camera3Thumbnail;
        private set => SetProperty(ref _camera3Thumbnail, value);
    }

    /// <summary>
    /// Camera 4 thumbnail for display in DataGrid.
    /// </summary>
    public BitmapSource? Camera4Thumbnail
    {
        get => _camera4Thumbnail;
        private set => SetProperty(ref _camera4Thumbnail, value);
    }

    /// <summary>
    /// Camera 5 thumbnail for display in DataGrid.
    /// </summary>
    public BitmapSource? Camera5Thumbnail
    {
        get => _camera5Thumbnail;
        private set => SetProperty(ref _camera5Thumbnail, value);
    }

    /// <summary>
    /// Camera 6 thumbnail for display in DataGrid.
    /// </summary>
    public BitmapSource? Camera6Thumbnail
    {
        get => _camera6Thumbnail;
        private set => SetProperty(ref _camera6Thumbnail, value);
    }

    /// <summary>
    /// Initializes image paths from the FileGroup model.
    /// </summary>
    private void InitializeImagePaths()
    {
        // Initialize camera image paths from the model
        foreach (var kvp in _fileGroup.CameraFiles)
        {
            if (kvp.Key.StartsWith("cam", StringComparison.OrdinalIgnoreCase))
            {
                _cameraImagePaths[kvp.Key.ToLower()] = kvp.Value;
            }
        }

        // Set NIR image path if available
        if (_fileGroup.HasNir && !string.IsNullOrEmpty(_fileGroup.NirFilePath))
        {
            _nirImagePath = _fileGroup.NirFilePath;
        }

        // Set main image path (only use Normal folder images, NOT camera images)
        if (!string.IsNullOrEmpty(_fileGroup.MainImagePath))
        {
            _mainImagePath = _fileGroup.MainImagePath;
        }
        else if (!string.IsNullOrEmpty(_fileGroup.NormalFolder))
        {
            // Try to construct path to stitched_original.png from Normal folder
            _mainImagePath = Path.Combine(_fileGroup.NormalFolder, "stitched_original.png");
        }
        // Do NOT fallback to camera images - they belong in their own columns
    }

    /// <summary>
    /// Checks the abnormal status using the injected IAbnormalDetector.
    /// Triggers PropertyChanged notifications for IsAbnormal and StatusText.
    /// </summary>
    private void CheckAbnormalStatus()
    {
        if (_abnormalDetector == null)
        {
            // No detector available, rely on model status only
            _isAbnormal = false;
            _abnormalReason = null;
            return;
        }

        try
        {
            var isAbnormal = _abnormalDetector.IsGroupAbnormal(_fileGroup);
            if (isAbnormal)
            {
                IsAbnormal = true;
                AbnormalReason = "Detected by z-score analysis";
            }
            else
            {
                IsAbnormal = false;
                AbnormalReason = null;
            }
        }
        catch (Exception)
        {
            // If detection fails, don't mark as abnormal
            IsAbnormal = false;
            AbnormalReason = null;
        }
    }

    /// <summary>
    /// Checks the abnormal status without triggering PropertyChanged notifications.
    /// Use this in Refresh() to avoid duplicate notifications.
    /// </summary>
    private void CheckAbnormalStatusWithoutNotification()
    {
        if (_abnormalDetector == null)
        {
            _isAbnormal = false;
            _abnormalReason = null;
            return;
        }

        try
        {
            var isAbnormal = _abnormalDetector.IsGroupAbnormal(_fileGroup);
            if (isAbnormal)
            {
                _isAbnormal = true;
                _abnormalReason = "Detected by z-score analysis";
            }
            else
            {
                _isAbnormal = false;
                _abnormalReason = null;
            }
        }
        catch (Exception)
        {
            _isAbnormal = false;
            _abnormalReason = null;
        }
    }

    /// <summary>
    /// Updates the ViewModel from the underlying model (call after model changes).
    /// </summary>
    public void Refresh()
    {
        // Notify basic property changes
        OnPropertyChanged(nameof(GroupId));
        OnPropertyChanged(nameof(NirKey));
        OnPropertyChanged(nameof(NormalFolder));
        OnPropertyChanged(nameof(LineNumber));
        OnPropertyChanged(nameof(HasNir));
        OnPropertyChanged(nameof(CreatedAt));
        OnPropertyChanged(nameof(Status));
        
        // Re-initialize paths and check abnormal status
        InitializeImagePaths();
        CheckAbnormalStatusWithoutNotification();
        
        // Notify status-related properties after abnormal check is complete
        // This ensures StatusText is only notified once, after all calculations
        OnPropertyChanged(nameof(IsAbnormal));
        OnPropertyChanged(nameof(AbnormalReason));
        OnPropertyChanged(nameof(StatusText));
        
        // Notify image path changes
        OnPropertyChanged(nameof(MainImagePath));
        OnPropertyChanged(nameof(NirImagePath));
        OnPropertyChanged(nameof(Camera1ImagePath));
        OnPropertyChanged(nameof(Camera2ImagePath));
        OnPropertyChanged(nameof(Camera3ImagePath));
        OnPropertyChanged(nameof(Camera4ImagePath));
        OnPropertyChanged(nameof(Camera5ImagePath));
        OnPropertyChanged(nameof(Camera6ImagePath));
    }

    /// <summary>
    /// Loads thumbnail images asynchronously for all image paths in this file group.
    /// Uses Dispatcher.InvokeAsync to update UI properties from background thread.
    /// </summary>
    public async Task LoadThumbnailsAsync()
    {
        if (_disposed)
            return;

        const int thumbnailWidth = 100;
        const int thumbnailHeight = 100;

        try
        {
            _logger?.LogDebug("Begin thumbnail load for {GroupId} | HasNir={HasNir} Normal={NormalFolder} Main={MainImagePath} CamCount={CamCount} NirGraphEnabled={NirGraphEnabled}",
                GroupId, HasNir, _fileGroup.NormalFolder, MainImagePath, _fileGroup.CameraFiles?.Count ?? 0,
                _configuration?.MatchingSettings.EnableNirGraph);
            _uiLog?.Invoke(LogSeverity.Debug, "Thumb", $"Start load {GroupId} HasNir={HasNir} Normal={_fileGroup.NormalFolder} Main={MainImagePath} CamCount={_fileGroup.CameraFiles?.Count ?? 0} NirGraph={_configuration?.MatchingSettings.EnableNirGraph}");

            // Load main image thumbnail
            if (!string.IsNullOrEmpty(MainImagePath))
            {
                var mainThumbnail = await LoadSingleThumbnailAsync(MainImagePath, thumbnailWidth, thumbnailHeight);
                await WpfApplication.Current.Dispatcher.InvokeAsync(() => MainImageThumbnail = mainThumbnail);
            }

            // Load NIR image thumbnail
            if (!string.IsNullOrEmpty(NirImagePath))
            {
            var ext = Path.GetExtension(NirImagePath);
            // Skip non-image files (spc/txt 등)
            if (!_imageExtensions.Contains(ext))
                {
                _logger?.LogDebug("Skipping NIR image thumbnail for non-image {GroupId}: {Path}", GroupId, NirImagePath);
                _uiLog?.Invoke(LogSeverity.Debug, "Thumb", $"Skip NIR non-image thumbnail {GroupId}: {NirImagePath}");
                }
                else
                {
                    var nirThumbnail = await LoadSingleThumbnailAsync(NirImagePath, thumbnailWidth, thumbnailHeight);
                    await WpfApplication.Current.Dispatcher.InvokeAsync(() => NirImageThumbnail = nirThumbnail);
                }
            }

            // Load NIR graph thumbnail (if enabled in configuration)
            if (HasNir)
            {
                if (_configuration?.MatchingSettings.EnableNirGraph == true)
                {
                    var nirGraphThumbnail = await LoadNirGraphThumbnailAsync(_configuration);
                    await WpfApplication.Current.Dispatcher.InvokeAsync(() => NirGraphThumbnail = nirGraphThumbnail);
                }
                else
                {
                    // Show placeholder when NIR graph is disabled
                    await WpfApplication.Current.Dispatcher.InvokeAsync(() =>
                        NirGraphThumbnail = ResourceHelper.GetNirPlaceholder());
                }
            }

            // Load camera thumbnails
            await LoadCameraThumbnailAsync(1, thumbnailWidth, thumbnailHeight);
            await LoadCameraThumbnailAsync(2, thumbnailWidth, thumbnailHeight);
            await LoadCameraThumbnailAsync(3, thumbnailWidth, thumbnailHeight);
            await LoadCameraThumbnailAsync(4, thumbnailWidth, thumbnailHeight);
            await LoadCameraThumbnailAsync(5, thumbnailWidth, thumbnailHeight);
            await LoadCameraThumbnailAsync(6, thumbnailWidth, thumbnailHeight);

            // If everything is null, log warning for empty row visibility
            if (MainImageThumbnail == null &&
                NirImageThumbnail == null &&
                NirGraphThumbnail == null &&
                Camera1Thumbnail == null &&
                Camera2Thumbnail == null &&
                Camera3Thumbnail == null &&
                Camera4Thumbnail == null &&
                Camera5Thumbnail == null &&
                Camera6Thumbnail == null)
            {
                _logger?.LogWarning("All thumbnails are null for {GroupId}. Paths -> Main:{Main} Nir:{Nir} NirTxt:{NirTxtCandidate} Cams:{CamCount}",
                    GroupId, MainImagePath, NirImagePath, _fileGroup.NirFilePath, _fileGroup.CameraFiles?.Count ?? 0);
                _uiLog?.Invoke(LogSeverity.Error, "Thumb", $"All thumbnails null for {GroupId} Main={MainImagePath} Nir={NirImagePath} NirSrc={_fileGroup.NirFilePath} CamCount={_fileGroup.CameraFiles?.Count ?? 0}");
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when disposing - no action needed
        }
        catch (Exception ex)
        {
            // Log error but don't throw - thumbnails are non-critical
            _logger?.LogError(ex, "Error loading thumbnails for group {GroupId}", GroupId);
            _uiLog?.Invoke(LogSeverity.Error, "Thumb", $"Error loading thumbnails for {GroupId}: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads a single thumbnail image and converts it to BitmapSource.
    /// </summary>
    private async Task<BitmapSource?> LoadSingleThumbnailAsync(string imagePath, int width, int height)
    {
        if (_disposed || string.IsNullOrEmpty(imagePath))
            return null;

        try
        {
            var ext = Path.GetExtension(imagePath);
            if (!_imageExtensions.Contains(ext))
            {
                _logger?.LogDebug("Skipping thumbnail generation for non-image {GroupId}: {Path}", GroupId, imagePath);
                _uiLog?.Invoke(LogSeverity.Debug, "Thumb", $"Skip non-image thumbnail {GroupId}: {imagePath}");
                return null;
            }

            if (!File.Exists(imagePath))
            {
                _logger?.LogWarning("Thumbnail source missing for {GroupId}: {Path}", GroupId, imagePath);
                _uiLog?.Invoke(LogSeverity.Warning, "Thumb", $"Missing file for {GroupId}: {imagePath}");
                return null;
            }

            var thumbnailBytes = await _imageProcessor.GenerateThumbnailAsync(
                imagePath, width, height, _cancellationTokenSource.Token);

            if (thumbnailBytes == null || thumbnailBytes.Length == 0)
            {
                _logger?.LogWarning("Thumbnail generation returned empty for {GroupId}: {Path}", GroupId, imagePath);
                _uiLog?.Invoke(LogSeverity.Warning, "Thumb", $"Thumbnail empty for {GroupId}: {imagePath}");
                return null;
            }

            // Convert byte array to BitmapSource
            using var ms = new System.IO.MemoryStream(thumbnailBytes);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = ms;
            bitmap.EndInit();
            bitmap.Freeze(); // Make it thread-safe for cross-thread access
            return bitmap;
        }
        catch (OperationCanceledException)
        {
            throw; // Re-throw cancellation
        }
        catch (Exception)
        {
            // Return null for failed loads - placeholder will be shown
            return null;
        }
    }

    /// <summary>
    /// Loads a camera thumbnail by camera number.
    /// </summary>
    private async Task LoadCameraThumbnailAsync(int cameraNumber, int width, int height)
    {
        if (_disposed)
            return;

        var imagePath = GetCameraImagePath(cameraNumber);
        if (string.IsNullOrEmpty(imagePath))
        {
            _logger?.LogDebug("Camera{Cam} path missing for {GroupId}", cameraNumber, GroupId);
            _uiLog?.Invoke(LogSeverity.Debug, "Thumb", $"Cam{cameraNumber} path missing for {GroupId}");
            return;
        }

        var thumbnail = await LoadSingleThumbnailAsync(imagePath, width, height);

        await WpfApplication.Current.Dispatcher.InvokeAsync(() =>
        {
            switch (cameraNumber)
            {
                case 1: Camera1Thumbnail = thumbnail; break;
                case 2: Camera2Thumbnail = thumbnail; break;
                case 3: Camera3Thumbnail = thumbnail; break;
                case 4: Camera4Thumbnail = thumbnail; break;
                case 5: Camera5Thumbnail = thumbnail; break;
                case 6: Camera6Thumbnail = thumbnail; break;
            }
        });
    }

    /// <summary>
    /// Reloads only the NIR graph thumbnail with updated configuration.
    /// Called when settings are applied to reflect size changes immediately.
    /// </summary>
    public async Task ReloadNirGraphAsync(ApplicationConfiguration? configuration = null)
    {
        if (_disposed)
            return;

        // Use provided configuration or the instance configuration
        var config = configuration ?? _configuration;

        if (HasNir)
        {
            if (config?.MatchingSettings.EnableNirGraph == true)
            {
                var nirGraphThumbnail = await LoadNirGraphThumbnailAsync(config);
                await WpfApplication.Current.Dispatcher.InvokeAsync(() => NirGraphThumbnail = nirGraphThumbnail);
            }
            else
            {
                // Show placeholder when NIR graph is disabled
                await WpfApplication.Current.Dispatcher.InvokeAsync(() =>
                    NirGraphThumbnail = ResourceHelper.GetNirPlaceholder());
            }
        }
        else
        {
            // Clear the thumbnail if no NIR file
            await WpfApplication.Current.Dispatcher.InvokeAsync(() => NirGraphThumbnail = null);
        }
    }

    /// <summary>
    /// Loads NIR graph thumbnail by parsing NIR .txt file and generating a graph.
    /// Implements file resolution priority: .spc->A.txt suffix, then .txt fallback.
    /// </summary>
    private async Task<BitmapSource?> LoadNirGraphThumbnailAsync(ApplicationConfiguration? config = null)
    {
        if (_disposed || !HasNir || string.IsNullOrEmpty(_fileGroup.NirFilePath))
            return null;

        try
        {
            // Use provided config or instance config
            var cfg = config ?? _configuration;

            // Get configured dimensions or use defaults
            int width = cfg?.UISettings.NirThumbnailWidth ?? 250;
            int height = cfg?.UISettings.NirThumbnailHeight ?? 100;

            // Determine NIR .txt file path
            string? nirTxtPath = null;
            var nirSpcPath = _fileGroup.NirFilePath;

            // Priority 0: If NirFilePath is already a .txt file, use it directly
            if (nirSpcPath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            {
                if (File.Exists(nirSpcPath))
                {
                    nirTxtPath = nirSpcPath;
                    _logger?.LogDebug("NIR .txt file used directly for {GroupId}: {TxtPath}", GroupId, nirSpcPath);
                    _uiLog?.Invoke(LogSeverity.Debug, "NIR", $".txt used directly for {GroupId}: {nirSpcPath}");
                }
            }
            // Priority 1: Try .spc -> A.txt suffix (e.g., run_120251204T111028.spc -> run_120251204T111028A.txt)
            else if (nirSpcPath.EndsWith(".spc", StringComparison.OrdinalIgnoreCase))
            {
                var basePath = Path.GetDirectoryName(nirSpcPath);
                var fileNameWithoutExt = Path.GetFileNameWithoutExtension(nirSpcPath);
                var candidatePath = Path.Combine(basePath ?? "", $"{fileNameWithoutExt}A.txt");

                if (File.Exists(candidatePath))
                {
                    nirTxtPath = candidatePath;
                }
                else
                {
                    // Priority 2: Fallback to exact match .txt (e.g., run_120251204T111028.spc -> run_120251204T111028.txt)
                    candidatePath = Path.Combine(basePath ?? "", $"{fileNameWithoutExt}.txt");
                    if (File.Exists(candidatePath))
                    {
                        nirTxtPath = candidatePath;
                    }
                }
            }

            // If no .txt file found, return null
            if (nirTxtPath == null)
            {
                _logger?.LogWarning("NIR graph source .txt not found for {GroupId}. spc={SpcPath}", GroupId, nirSpcPath);
                _uiLog?.Invoke(LogSeverity.Warning, "NIR", $"Graph source .txt not found for {GroupId} spc={nirSpcPath}");
                return null;
            }


            // Generate graph on calling thread (already on UI thread via Dispatcher)
            // ScottPlot requires UI thread access for internal ObservableCollection updates
            try
            {
                // Parse NIR spectrum from .txt file
                var spectrum = NirSpectrumParser.Parse(nirTxtPath);
                if (spectrum == null)
                {
                    _logger?.LogWarning("NIR spectrum parsing returned null for {GroupId}: {NirTxtPath}", GroupId, nirTxtPath);
                    return null;
                }

                // Generate graph bitmap (must run on UI thread for ScottPlot)
                var graph = NirGraphGenerator.GenerateGraph(spectrum, width, height);
                if (graph != null)
                {
                    _logger?.LogDebug("NIR graph generated for {GroupId} using {NirTxtPath}", GroupId, nirTxtPath);
                    _uiLog?.Invoke(LogSeverity.Debug, "NIR", $"Graph generated for {GroupId} txt={nirTxtPath}");
                }
                return graph;
            }
            catch (Exception ex)
            {
                // Return null on any error - log exception details for debugging
                _logger?.LogError(ex, "NIR graph generation failed for {GroupId} using {NirTxtPath}. Error: {ErrorMessage}", 
                    GroupId, nirTxtPath, ex.Message);
                _uiLog?.Invoke(LogSeverity.Error, "NIR", 
                    $"Graph generation failed for {GroupId} txt={nirTxtPath} Error: {ex.Message}");
                return null;
            }
        }
        catch (Exception ex)
        {
            // Return null for failed loads - log exception details
            _logger?.LogError(ex, "NIR graph load failed for {GroupId}. Error: {ErrorMessage}", GroupId, ex.Message);
            _uiLog?.Invoke(LogSeverity.Error, "NIR", $"Graph load failed for {GroupId} Error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Disposes resources and cancels any pending thumbnail loading operations.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
    }
}
