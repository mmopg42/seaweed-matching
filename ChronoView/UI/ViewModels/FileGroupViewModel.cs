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

using ChronoView.Core.FileWatching;


namespace ChronoView.UI.ViewModels;

/// <summary>
/// ViewModel for displaying individual file groups in the UI.
/// </summary>
public class FileGroupViewModel : ViewModelBase, IDisposable
{
    private readonly FileGroup _fileGroup;
    private readonly IImageProcessor _imageProcessor;
    private readonly IMonitoringOrchestrator? _orchestrator;
    private readonly IAbnormalDetector? _abnormalDetector;
    private readonly ApplicationConfiguration? _configuration;
    private readonly ILogger<FileGroupViewModel>? _logger;
    private readonly Action<LogSeverity, string, string>? _uiLog;
    private static readonly HashSet<string> _imageExtensions = new(StringComparer.OrdinalIgnoreCase)
    { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tif", ".tiff", ".webp" };
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    
    // Concurrency control for thumbnail loading
    private readonly HashSet<string> _activeLoadingTasks = new();
    private readonly object _loadingTasksLock = new();

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
    
    // Retry Infrastructure
    private class RetryContext
    {
        public string Key { get; set; } = ""; // e.g., "Cam1", "Main"
        public string ImagePath { get; set; } = "";
        public int Width { get; set; }
        public int Height { get; set; }
        public Action<BitmapSource> OnSuccess { get; set; } = _ => { };
        public int RetryCount { get; set; } = 0;
    }
    private readonly System.Collections.Concurrent.ConcurrentQueue<RetryContext> _retryQueue = new();
    private readonly System.Windows.Threading.DispatcherTimer _retryTimer;

    /// <summary>
    /// Creates a new FileGroupViewModel wrapping a FileGroup model.
    /// </summary>
    /// <param name="fileGroup">The FileGroup model to wrap.</param>
    /// <param name="imageProcessor">Image processor for thumbnail generation.</param>
    /// <param name="orchestrator">Orchestrator for accessing cached images.</param>
    /// <param name="abnormalDetector">Optional abnormal detector for z-score analysis.</param>
    /// <param name="configuration">Optional application configuration for NIR graph settings.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    /// <param name="uiLog">Optional UI log sink (Severity, Source, Message).</param>
    public FileGroupViewModel(
        FileGroup fileGroup,
        IImageProcessor imageProcessor,
        IMonitoringOrchestrator? orchestrator = null,
        IAbnormalDetector? abnormalDetector = null,
        ApplicationConfiguration? configuration = null,
        ILogger<FileGroupViewModel>? logger = null,
        Action<LogSeverity, string, string>? uiLog = null)
    {
        _fileGroup = fileGroup ?? throw new ArgumentNullException(nameof(fileGroup));
        _imageProcessor = imageProcessor ?? throw new ArgumentNullException(nameof(imageProcessor));
        _orchestrator = orchestrator;
        _abnormalDetector = abnormalDetector;
        _configuration = configuration;
        _logger = logger;
        _uiLog = uiLog;

        InitializeImagePaths();
        CheckAbnormalStatus();
        
        // Initialize Retry Timer (2 seconds interval)
        _retryTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _retryTimer.Tick += ProcessRetryQueue;
        // Timer starts only when items are queued

        // Removed duplicate VM creation logs - matching details are logged in FileMatchingEngine
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
    /// Status display text for UI based on completion criteria.
    /// </summary>
    public string StatusText
    {
        get
        {
            // Priority 1: Abnormal status (when implemented)
            if (IsAbnormal)
            {
                return Core.Localization.LocalizationManager.GetString("Status_Abnormal");
            }

            // Priority 2: Check completion based on line-specific requirements
            bool isComplete = IsGroupComplete();

            if (isComplete)
            {
                return Core.Localization.LocalizationManager.GetString("Status_Complete");
            }
            else
            {
                return Core.Localization.LocalizationManager.GetString("Status_Pending");
            }
        }
    }

    /// <summary>
    /// Determines if this group is complete based on line-specific data requirements.
    /// NIR presence is NOT a completion criterion.
    /// </summary>
    private bool IsGroupComplete()
    {
        if (LineNumber == 1)
        {
            // Line 1: Normal1 + Cam1 + Cam2 + Cam3 required for completion
            return !string.IsNullOrEmpty(MainImagePath) &&  // Normal1 (stitched_original.png)
                   !string.IsNullOrEmpty(Camera1ImagePath) && // Cam1
                   !string.IsNullOrEmpty(Camera2ImagePath) && // Cam2
                   !string.IsNullOrEmpty(Camera3ImagePath);   // Cam3
        }
        else if (LineNumber == 2)
        {
            // Line 2: Normal2 + Cam4 + Cam5 + Cam6 required for completion
            return !string.IsNullOrEmpty(MainImagePath) &&  // Normal2 (stitched_original.png)
                   !string.IsNullOrEmpty(Camera4ImagePath) && // Cam4
                   !string.IsNullOrEmpty(Camera5ImagePath) && // Cam5
                   !string.IsNullOrEmpty(Camera6ImagePath);   // Cam6
        }

        // Unknown line number - consider incomplete
        return false;
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
        set 
        {
            if (SetProperty(ref _mainImageThumbnail, value))
            {
                OnPropertyChanged(nameof(NormalImageSizeLabel));
            }
        }
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

        // Notify that label properties have been initialized
        OnPropertyChanged(nameof(NirLabel));
        OnPropertyChanged(nameof(NormalLabel));
        OnPropertyChanged(nameof(Camera1Label));
        OnPropertyChanged(nameof(Camera2Label));
        OnPropertyChanged(nameof(Camera3Label));
        OnPropertyChanged(nameof(Camera4Label));
        OnPropertyChanged(nameof(Camera5Label));
        OnPropertyChanged(nameof(Camera6Label));
    }

    /// <summary>
    /// Formatted label for NIR files (e.g. "file.spc\nfile.txt").
    /// </summary>
    public string NirLabel
    {
        get
        {
            if (!HasNir || string.IsNullOrEmpty(NirKey)) return string.Empty;
            
            // Expected filenames based on NirKey
            // e.g. Key="run_120251204T120000" -> .spc and A.txt
            return $"{NirKey}.spc\n{NirKey}A.txt";
        }
    }

    /// <summary>
    /// Label for Normal camera (Folder Name).
    /// </summary>
    public string NormalLabel
    {
        get
        {
            if (string.IsNullOrEmpty(NormalFolder)) return string.Empty;
            return Path.GetFileName(NormalFolder);
        }
    }

    /// <summary>
    /// Label for Normal camera image size (e.g. "640 x 480 px").
    /// Only available after thumbnail is loaded.
    /// </summary>
    public string NormalImageSizeLabel
    {
        get
        {
            if (_mainImageThumbnail == null) return string.Empty;
            // Since we are using thumbnails, the size might be small (100x100).
            // However, for correct display, we show the pixel dimensions of the loaded bitmap.
            // If the user wants ORIGINAL size, we would need to read metadata which is expensive.
            // For now, we display the bitmap size which acts as a proxy or placeholder.
            // Note: If using generated thumbnails, this will show thumbnail size.
            // To show real size, we'd need to metadata read. 
            // Given the requirement "below image size", and performance constraints, we stick to loaded image properties.
            return $"{_mainImageThumbnail.PixelWidth} x {_mainImageThumbnail.PixelHeight} px";
        }
    }

    /// <summary>
    /// Label for Camera 1 (Filename).
    /// </summary>
    public string Camera1Label => GetCameraLabel(1);

    /// <summary>
    /// Label for Camera 2 (Filename).
    /// </summary>
    public string Camera2Label => GetCameraLabel(2);

    /// <summary>
    /// Label for Camera 3 (Filename).
    /// </summary>
    public string Camera3Label => GetCameraLabel(3);

    /// <summary>
    /// Label for Camera 4 (Filename).
    /// </summary>
    public string Camera4Label => GetCameraLabel(4);

    /// <summary>
    /// Label for Camera 5 (Filename).
    /// </summary>
    public string Camera5Label => GetCameraLabel(5);

    /// <summary>
    /// Label for Camera 6 (Filename).
    /// </summary>
    public string Camera6Label => GetCameraLabel(6);

    private string GetCameraLabel(int cameraNumber)
    {
        var path = GetCameraImagePath(cameraNumber);
        return string.IsNullOrEmpty(path) ? "비어있음" : Path.GetFileName(path);
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
                AbnormalReason = Core.Localization.LocalizationManager.GetString("Status_AbnormalReason");
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

        _logger?.LogDebug("[{GroupId}] LoadThumbnailsAsync 시작", GroupId);
        _uiLog?.Invoke(LogSeverity.Debug, "Image", $"[{GroupId}] 썸네일 로딩 시작");

        const int thumbnailWidth = 100;
        const int thumbnailHeight = 100;

        try
        {
            // Create list of parallel loading tasks
            var loadingTasks = new List<Task>();

            // Load main image thumbnail (only if not already loaded)
            if (!string.IsNullOrEmpty(MainImagePath) && MainImageThumbnail == null)
            {
                _logger?.LogDebug("[{GroupId}] Main 이미지 로딩 시도: {Path}", GroupId, MainImagePath);
                if (TryEnterLoading("Main"))
                {
                    loadingTasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            var mainThumbnail = await LoadSingleThumbnailAsync(MainImagePath, thumbnailWidth, thumbnailHeight);
                            await WpfApplication.Current.Dispatcher.InvokeAsync(() =>
                            {
                                if (mainThumbnail != null)
                                {
                                    MainImageThumbnail = mainThumbnail;
                                    _logger?.LogDebug("[{GroupId}] Main 이미지 표시 완료: {Path}", GroupId, MainImagePath);
                                    _uiLog?.Invoke(LogSeverity.Debug, "Image", $"[{GroupId}] Main 이미지 표시: {Path.GetFileName(MainImagePath)}");
                                }
                                else
                                {
                                    _logger?.LogDebug("[{GroupId}] Main 이미지 로딩 실패 -> Retry Queue", GroupId, MainImagePath);
                                    QueueRetry("Main", MainImagePath, thumbnailWidth, thumbnailHeight, (bmp) => MainImageThumbnail = bmp);
                                }
                            }).Task;
                        }
                        finally { ExitLoading("Main"); }
                    }));
                }
                else
                {
                    _logger?.LogDebug("[{GroupId}] Main 이미지 로딩 건너뜀 - 이미 로딩 중", GroupId);
                }
            }
            else if (MainImageThumbnail != null)
            {
                _logger?.LogDebug("[{GroupId}] Main 이미지는 이미 로드됨", GroupId);
            }

            // Load NIR image thumbnail (only if not already loaded)
            if (HasNir && !string.IsNullOrEmpty(NirImagePath))
            {
                if (NirImageThumbnail != null)
                {
                     _logger?.LogDebug("[{GroupId}] NIR 이미지는 이미 로드됨", GroupId);
                }
                else
                {
                    var ext = Path.GetExtension(NirImagePath);
                    if (!_imageExtensions.Contains(ext))
                    {
                        _logger?.LogDebug("[{GroupId}] NIR 이미지 건너김 (이미지 파일 아님): {Path}", GroupId, NirImagePath);
                    }
                    else
                    {
                        _logger?.LogDebug("[{GroupId}] NIR 이미지 로딩 시도: {Path}", GroupId, NirImagePath);
                        if (TryEnterLoading("NIR"))
                        {
                            loadingTasks.Add(Task.Run(async () =>
                            {
                                try
                                {
                                    var nirThumbnail = await LoadSingleThumbnailAsync(NirImagePath, thumbnailWidth, thumbnailHeight);
                                    await WpfApplication.Current.Dispatcher.InvokeAsync(() =>
                                    {
                                        if (nirThumbnail != null)
                                        {
                                            NirImageThumbnail = nirThumbnail;
                                            _logger?.LogDebug("[{GroupId}] NIR 이미지 표시 완료: {Path}", GroupId, NirImagePath);
                                            _uiLog?.Invoke(LogSeverity.Debug, "Image", $"[{GroupId}] NIR 이미지 표시: {Path.GetFileName(NirImagePath)}");
                                        }
                                        else
                                        {
                                            _logger?.LogDebug("[{GroupId}] NIR 이미지 로딩 실패 -> Retry Queue", GroupId, NirImagePath);
                                            QueueRetry("NIR", NirImagePath, thumbnailWidth, thumbnailHeight, (bmp) => NirImageThumbnail = bmp);
                                        }
                                    }).Task;
                                }
                                finally { ExitLoading("NIR"); }
                            }));
                        }
                        else
                        {
                             _logger?.LogDebug("[{GroupId}] NIR 이미지 로딩 건너김 - 이미 로딩 중", GroupId);
                        }
                    }
                }
            }
            else if (NirImageThumbnail != null)
            {
                 // Handle case where NIR might have been removed but thumbnail persists? (Unlikely)
            }
            else if (string.IsNullOrEmpty(NirImagePath))
            {
                _logger?.LogDebug("[{GroupId}] NIR 이미지 경로가 없음", GroupId);
            }

            // Load NIR Graph thumbnail (uses LoadNirGraphThumbnailAsync which handles its own logic)
            if (NirGraphThumbnail == null && _configuration?.MatchingSettings.EnableNirGraph == true)
            {
                 if (TryEnterLoading("NirGraph"))
                 {
                    loadingTasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            var nirGraphThumbnail = await LoadNirGraphThumbnailAsync(_configuration);
                            await WpfApplication.Current.Dispatcher.InvokeAsync(() =>
                            {
                                NirGraphThumbnail = nirGraphThumbnail;
                                if (nirGraphThumbnail != null)
                                {
                                    _logger?.LogDebug("[{GroupId}] NIR 그래프 표시 완료", GroupId);
                                    _uiLog?.Invoke(LogSeverity.Debug, "Image", $"[{GroupId}] NIR 그래프 표시 완료");
                                }
                            });
                        }
                        finally { ExitLoading("NirGraph"); }
                    }));
                 }
            }
            else if (NirGraphThumbnail == null && _configuration?.MatchingSettings.EnableNirGraph == false)
            {
                 await WpfApplication.Current.Dispatcher.InvokeAsync(() => NirGraphThumbnail = ResourceHelper.GetNirPlaceholder());
            }

            // Load camera thumbnails in parallel (only if not already loaded)
            if (Camera1Thumbnail == null) loadingTasks.Add(LoadCameraThumbnailAsync(1, thumbnailWidth, thumbnailHeight));
            if (Camera2Thumbnail == null) loadingTasks.Add(LoadCameraThumbnailAsync(2, thumbnailWidth, thumbnailHeight));
            if (Camera3Thumbnail == null) loadingTasks.Add(LoadCameraThumbnailAsync(3, thumbnailWidth, thumbnailHeight));
            if (Camera4Thumbnail == null) loadingTasks.Add(LoadCameraThumbnailAsync(4, thumbnailWidth, thumbnailHeight));
            if (Camera5Thumbnail == null) loadingTasks.Add(LoadCameraThumbnailAsync(5, thumbnailWidth, thumbnailHeight));
            if (Camera6Thumbnail == null) loadingTasks.Add(LoadCameraThumbnailAsync(6, thumbnailWidth, thumbnailHeight));

            // Wait for all parallel tasks to complete
            if (loadingTasks.Count > 0)
            {
                await Task.WhenAll(loadingTasks);
            }

            // Final State Logging
            bool allNull = MainImageThumbnail == null && NirImageThumbnail == null && NirGraphThumbnail == null &&
                           Camera1Thumbnail == null && Camera2Thumbnail == null && Camera3Thumbnail == null &&
                           Camera4Thumbnail == null && Camera5Thumbnail == null && Camera6Thumbnail == null;

            if (allNull)
            {
                // Only log warning if we expected images
                bool expectedImages = !string.IsNullOrEmpty(MainImagePath) || HasNir || (_fileGroup.CameraFiles?.Count > 0);
                if (expectedImages)
                {
                    _logger?.LogWarning("[{GroupId}] 초기 로딩에서 썸네일 확보 실패 (Retry Queue에서 처리 예정)", GroupId);
                }
            }
            else
            {
                 _logger?.LogDebug("[{GroupId}] LoadThumbnailsAsync 완료 (일부 성공)", GroupId);
                 _uiLog?.Invoke(LogSeverity.Debug, "Image", $"[{GroupId}] 썸네일 로딩 완료");
            }
        }
        catch (OperationCanceledException)
        {
            _logger?.LogDebug("[{GroupId}] LoadThumbnailsAsync 취소됨", GroupId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[{GroupId}] 썸네일 로딩 중 오류 발생", GroupId);
            _uiLog?.Invoke(LogSeverity.Error, "Thumb", $"Error loading thumbnails for {GroupId}: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads a single thumbnail image. STRICT MODE: Fails fast if locked/invalid.
    /// Caller is responsible for queuing retries if needed.
    /// </summary>
    private async Task<BitmapSource?> LoadSingleThumbnailAsync(string imagePath, int width, int height)
    {
        if (_disposed || string.IsNullOrEmpty(imagePath)) return null;

        // ⚡ CACHE CHECK
        if (_orchestrator != null && Path.GetFileName(imagePath).Equals("stitched_original.png", StringComparison.OrdinalIgnoreCase))
        {
            var cachedImage = _orchestrator.GetCapturedImage(GroupId);
            if (cachedImage == null && !string.IsNullOrEmpty(NormalFolder))
                cachedImage = _orchestrator.GetCapturedImageByFolderPath(NormalFolder);

            if (cachedImage != null) return cachedImage;
        }

        try
        {
            if (!File.Exists(imagePath)) return null; // Fail fast

            // FAST LOCK CHECK
            try 
            {
                var info = new FileInfo(imagePath);
                if (info.Length == 0) return null; // Empty file
                
                using (var fs = File.Open(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    // Accessible
                }
            }
            catch (IOException) 
            { 
                // Locked
                _logger?.LogDebug("[{GroupId}] 파일 잠김 (Fast-Check): {Path}", GroupId, imagePath);
                return null; 
            }

            // Generate
            _logger?.LogDebug("[{GroupId}] 썸네일 생성 시도: {Path}", GroupId, imagePath);
            var thumbnailBytes = await _imageProcessor.GenerateThumbnailAsync(
                imagePath, width, height, _cancellationTokenSource.Token);

            if (thumbnailBytes == null || thumbnailBytes.Length == 0) return null;

            using var ms = new System.IO.MemoryStream(thumbnailBytes);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = ms;
            bitmap.EndInit();
            bitmap.Freeze();
            
            // STRICT VALIDATION
            if (bitmap.PixelWidth == 0 || bitmap.PixelHeight == 0)
            {
                _logger?.LogWarning("[{GroupId}] 유효하지 않은 이미지 (0x0): {Path}", GroupId, imagePath);
                return null;
            }
            
            return bitmap;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger?.LogDebug("[{GroupId}] 썸네일 생성 예외: {Error}", GroupId, ex.Message);
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
            
        // Safety net: check if this camera is already loading
        var key = $"Cam{cameraNumber}";
        if (!TryEnterLoading(key))
        {
            _logger?.LogDebug("[{GroupId}] Camera{Number} 로딩 건너뜀 - 이미 로딩 중", GroupId, cameraNumber);
            return;
        }

        try
        {
            var imagePath = GetCameraImagePath(cameraNumber);
            if (string.IsNullOrEmpty(imagePath))
            {
                // Only log if this camera should exist for this group's line
                // Cam 1-3 are for Line 1, Cam 4-6 are for Line 2
                bool shouldHaveCamera = (LineNumber == 1 && cameraNumber <= 3) || (LineNumber == 2 && cameraNumber >= 4);
                
                if (shouldHaveCamera)
                {
                    _logger?.LogDebug("[{GroupId}] Camera{Number} 이미지 경로가 없음 (예상됨)", GroupId, cameraNumber);
                }
                return;
            }

            _logger?.LogDebug("[{GroupId}] Camera{Number} 이미지 로딩 시도: {Path}", GroupId, cameraNumber, imagePath);
            var thumbnail = await LoadSingleThumbnailAsync(imagePath, width, height);

            await WpfApplication.Current.Dispatcher.InvokeAsync(() =>
            {
                // Update UI if successful
                if (thumbnail != null)
                {
                    UpdateCameraThumbnail(cameraNumber, thumbnail);
                    _logger?.LogDebug("[{GroupId}] Camera{Number} 이미지 표시 완료: {Path}", GroupId, cameraNumber, imagePath);
                    _uiLog?.Invoke(LogSeverity.Debug, "Image", $"[{GroupId}] Camera{cameraNumber} 이미지 표시: {Path.GetFileName(imagePath)}");
                }
                else
                {
                    // FAIL -> Queue Retry
                    _logger?.LogDebug("[{GroupId}] Camera{Number} 이미지 로딩 실패 (잠김/없음) -> Retry Queue 추가", GroupId, cameraNumber);
                    QueueRetry($"Camera{cameraNumber}", imagePath, width, height, (bmp) => UpdateCameraThumbnail(cameraNumber, bmp));
                }
            }).Task;
        }
        finally
        {
            ExitLoading(key);
        }
    }

    private void UpdateCameraThumbnail(int cameraNumber, BitmapSource thumbnail)
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
    }

    private void QueueRetry(string key, string path, int width, int height, Action<BitmapSource> onSuccess)
    {
        if (_disposed) return;

        _retryQueue.Enqueue(new RetryContext 
        { 
            Key = key, 
            ImagePath = path, 
            Width = width, 
            Height = height, 
            OnSuccess = onSuccess 
        });

        if (!_retryTimer.IsEnabled) 
        {
            _retryTimer.Start();
            _logger?.LogDebug("[{GroupId}] Retry Timer 시작 (Queue Size: {Size})", GroupId, _retryQueue.Count);
        }
    }

    private async void ProcessRetryQueue(object? sender, EventArgs e)
    {
        if (_disposed || _retryQueue.IsEmpty) 
        {
            _retryTimer.Stop();
            return;
        }

        // Dequeue UP TO current count (avoid infinite loop if retries are re-queued immediately)
        int batchSize = _retryQueue.Count;
        List<RetryContext> nextCycle = new();

        for (int i = 0; i < batchSize; i++)
        {
            if (!_retryQueue.TryDequeue(out var context)) break;

            if (context.RetryCount > 30) // Max retries (e.g. 1 min) - give up
            {
                _logger?.LogDebug("[{GroupId}] Retry 포기 (Max Attempts): {Key}", GroupId, context.Key);
                continue;
            }

            // Attempt Load
            var bitmap = await LoadSingleThumbnailAsync(context.ImagePath, context.Width, context.Height);
            if (bitmap != null)
            {
                // SUCCESS
                context.OnSuccess(bitmap);
                
                // Explicit Log
                _logger?.LogDebug("[{GroupId}] {Key} 이미지 표시 완료 (Retry 성공): {Path}", GroupId, context.Key, context.ImagePath);
                _uiLog?.Invoke(LogSeverity.Debug, "Image", $"[{GroupId}] {context.Key} 이미지 표시 완료 (Retry)");
            }
            else
            {
                // FAIL - Re-queue for next tick
                context.RetryCount++;
                nextCycle.Add(context);
            }
        }

        // Re-queue failed items
        foreach (var item in nextCycle) _retryQueue.Enqueue(item);

        if (_retryQueue.IsEmpty) _retryTimer.Stop();
    }

    private bool TryEnterLoading(string key)
    {
        lock (_loadingTasksLock)
        {
            if (_activeLoadingTasks.Contains(key))
                return false;
            
            _activeLoadingTasks.Add(key);
            return true;
        }
    }

    private void ExitLoading(string key)
    {
        lock (_loadingTasksLock)
        {
            _activeLoadingTasks.Remove(key);
        }
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
            
            _logger?.LogInformation("Generating NIR graph for {GroupId}: Width={Width}, Height={Height}", GroupId, width, height);

            // Determine NIR .txt file path
            string? nirTxtPath = null;
            var nirSpcPath = _fileGroup.NirFilePath;

            // Priority 0: If NirFilePath is already a .txt file, use it directly
            if (nirSpcPath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            {
                if (File.Exists(nirSpcPath))
                {
                    nirTxtPath = nirSpcPath;
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


            // Generate graph on background thread to prevent UI freezes
            // ScottPlot SavePng is synchronous I/O-heavy operation
            try
            {
                var graph = await Task.Run(() =>
                {
                    try
                    {
                        // Parse NIR spectrum from .txt file
                        var spectrum = NirSpectrumParser.Parse(nirTxtPath);
                        if (spectrum == null)
                        {
                            _logger?.LogWarning("NIR spectrum parsing returned null for {GroupId}: {NirTxtPath}", GroupId, nirTxtPath);
                            return null;
                        }

                        // Generate graph bitmap
                        var graphBitmap = NirGraphGenerator.GenerateGraph(spectrum, width, height);
                        return graphBitmap;
                    }
                    catch (Exception ex)
                    {
                        // Return null on any error - log exception details for debugging
                        _logger?.LogError(ex, "NIR graph generation failed for {GroupId} using {NirTxtPath}. Error: {ErrorMessage}", 
                            GroupId, nirTxtPath, ex.Message);
                        return null;
                    }
                }, _cancellationTokenSource.Token);

                if (graph != null)
                {
                    // Graph generated successfully - no logging needed
                }
                else
                {
                    _uiLog?.Invoke(LogSeverity.Error, "NIR", $"Graph generation returned null for {GroupId} txt={nirTxtPath}");
                }
                
                return graph;
            }
            catch (OperationCanceledException)
            {
                // Expected during disposal
                return null;
            }
            catch (Exception ex)
            {
                // Return null for failed loads - log exception details
                _logger?.LogError(ex, "NIR graph load failed for {GroupId}. Error: {ErrorMessage}", GroupId, ex.Message);
                _uiLog?.Invoke(LogSeverity.Error, "NIR", $"Graph load failed for {GroupId} Error: {ex.Message}");
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
