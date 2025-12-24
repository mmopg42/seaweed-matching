using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Windows;
using ChronoView.Models;
using ChronoView.Core.FileWatching;
using ChronoView.Core.Analytics;
using ChronoView.Core.Configuration;
using ChronoView.Core.FileOperations;
using ChronoView.Core.ImageProcessing;
using ChronoView.Core.FileMatching;
using ChronoView.Core.ProgramLaunching;
using ChronoView.Resources;
using Microsoft.Extensions.Logging;
using System.Windows.Media;
using WpfApplication = System.Windows.Application;
using WpfMessageBox = System.Windows.MessageBox;

namespace ChronoView.UI.ViewModels;

/// <summary>
/// ViewModel for the main application window.
/// </summary>
public class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly IMonitoringOrchestrator _orchestrator;
    private readonly IStatisticsService _statisticsService;
    private readonly IConfigurationManager _configManager;
    private readonly IFileOperationService _fileOperationService;
    private readonly IPathManagementService _pathManagementService;
    private readonly IImageProcessor _imageProcessor;
    private readonly IAbnormalDetector _abnormalDetector;
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly ILogger<FileGroupViewModel> _fileGroupLogger;
    private readonly GeneralCameraLauncher _generalCameraLauncher;
    private readonly NirCameraLauncher _nirCameraLauncher;
    private readonly Nir2CameraLauncher _nir2CameraLauncher;
    private readonly Action<LogSeverity, string, string> _uiLog;

    // CancellationToken management
    private CancellationTokenSource _windowCts = new();
    private CancellationTokenSource? _operationCts;

    private FileGroupViewModel? _selectedLine1Group;
    private FileGroupViewModel? _selectedLine2Group;
    private bool _isMonitoring;
    private bool _isSeparatedMode;
    private int _totalGroups;
    private double _matchRate;
    private int _failures;
    private string _statusMessage = "Ready";
    private string _nirConnectionStatus = "OK";
    private DateTime _currentTime = DateTime.Now;
    
    // Progress and operation state
    private double _progressValue;
    private bool _isOperationInProgress;
    private int _activeTabIndex;
    
    // Toolbar input fields
    private string _dateInput = DateTime.Now.ToString("yyyyMMdd");
    private string _sampleFolderName = "Sample_001";
    private string _sampleName = "Sample_001";
    private string _moveNir = "Last 50 NIRs";
    private string _moveAllData = "";
    
    // Matching statistics display (aliases for unified mode)
    private int _withNirCount;
    private int _withoutNirCount;
    private int _failedCount;

    // File count statistics
    private int _nirCount;
    private int _nir2Count;
    private int _normalCount;
    private int _normal2Count;
    private int _cam1Count;
    private int _cam2Count;
    private int _cam3Count;
    private int _cam4Count;
    private int _cam5Count;
    private int _cam6Count;

    // Matching statistics - Unified mode
    private int _unifiedTotalGroups;
    private int _unifiedWithNir;
    private int _unifiedWithoutNir;
    private int _unifiedFailed;

    // Matching statistics - Separated mode
    private int _line1TotalGroups;
    private int _line1WithNir;
    private int _line1WithoutNir;
    private int _line1Failed;
    private int _line2TotalGroups;
    private int _line2WithNir;
    private int _line2WithoutNir;
    private int _line2Failed;

    // Window state
    private double _windowWidth = 1200;
    private double _windowHeight = 800;
    private double _windowLeft = 100;
    private double _windowTop = 100;

    // UI display settings
    private int _displayImageWidth = 120;
    private int _displayImageHeight = 90;
    private int _dataGridRowHeight = 140;
    private int _nirDisplayWidth = 120;
    private int _nirDisplayHeight = 90;

    // Program status tracking
    private string _generalCameraStatus = "Deactivated";
    private System.Windows.Media.Brush _generalCameraForeground = new SolidColorBrush(Colors.Red);
    private string _nirCameraStatus = "Deactivated";
    private System.Windows.Media.Brush _nirCameraForeground = new SolidColorBrush(Colors.Red);
    
    // NIR2 Filtering status tracking
    private string _nir2FilteringStatus = "Deactivated";
    private System.Windows.Media.Brush _nir2FilteringForeground = new SolidColorBrush(Colors.Red);
    private string _nir2FilteringButtonText = "ON";
    private System.Windows.Media.Brush _nir2FilteringButtonBackground = new SolidColorBrush(Colors.Green);

    // Cached conflict resolution for file operations (applies to all conflicts in operation)
    private ConflictResolution? _cachedConflictResolution = null;

    public MainWindowViewModel(
        IMonitoringOrchestrator orchestrator,
        IStatisticsService statisticsService,
        IConfigurationManager configManager,
        IFileOperationService fileOperationService,
        IPathManagementService pathManagementService,
        IImageProcessor imageProcessor,
        IAbnormalDetector abnormalDetector,
        IFileGroupMatcher fileGroupMatcher,
        ILogger<MainWindowViewModel> logger,
        ILogger<FileGroupViewModel> fileGroupLogger,
        GeneralCameraLauncher generalCameraLauncher,
        NirCameraLauncher nirCameraLauncher,
        Nir2CameraLauncher nir2CameraLauncher)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _statisticsService = statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));
        _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
        _fileOperationService = fileOperationService ?? throw new ArgumentNullException(nameof(fileOperationService));
        _pathManagementService = pathManagementService ?? throw new ArgumentNullException(nameof(pathManagementService));
        _imageProcessor = imageProcessor ?? throw new ArgumentNullException(nameof(imageProcessor));
        _abnormalDetector = abnormalDetector ?? throw new ArgumentNullException(nameof(abnormalDetector));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _fileGroupLogger = fileGroupLogger ?? throw new ArgumentNullException(nameof(fileGroupLogger));
        _generalCameraLauncher = generalCameraLauncher ?? throw new ArgumentNullException(nameof(generalCameraLauncher));
        _nirCameraLauncher = nirCameraLauncher ?? throw new ArgumentNullException(nameof(nirCameraLauncher));
        _nir2CameraLauncher = nir2CameraLauncher ?? throw new ArgumentNullException(nameof(nir2CameraLauncher));

        // Initialize collections FIRST before any AddLogMessage calls
        FileGroups = new ObservableCollection<FileGroupViewModel>();
        Line1Groups = new ObservableCollection<FileGroupViewModel>();
        Line2Groups = new ObservableCollection<FileGroupViewModel>();
        LogMessages = new ObservableCollection<LogMessage>();

        // NOW we can safely use AddLogMessage
        _uiLog = (severity, source, message) =>
        {
            // Marshal to UI thread since this can be called from background threads (FileWatcher)
            WpfApplication.Current.Dispatcher.BeginInvoke(() =>
            {
                AddLogMessage(severity, source, message);
            });
        };

        // Setup UI log for FileGroupMatcherService
        if (fileGroupMatcher is FileGroupMatcherService matcherService)
        {
            matcherService.SetUILog(_uiLog);
            AddLogMessage(LogSeverity.Info, "System", "UI log connected to FileGroupMatcherService");
        }
        else
        {
            AddLogMessage(LogSeverity.Warning, "System", $"FileGroupMatcher is not FileGroupMatcherService: {fileGroupMatcher?.GetType().Name ?? "null"}");
        }

        // Setup UI log for MonitoringOrchestrator
        _orchestrator.SetUILog(_uiLog);
        AddLogMessage(LogSeverity.Info, "System", "UI log connected to MonitoringOrchestrator");

        // Initialize commands
        StartCommand = new RelayCommand(ExecuteStart, CanExecuteStart);
        StopCommand = new RelayCommand(ExecuteStop, CanExecuteStop);
        MoveCommand = new RelayCommand(ExecuteMove, CanExecuteMove);
        DeleteCommand = new RelayCommand(ExecuteDelete, CanExecuteDelete);
        RefreshCommand = new RelayCommand(ExecuteRefresh, CanExecuteRefresh);
        CreateSampleFolderCommand = new RelayCommand(ExecuteCreateSampleFolder);
        CreateSampleFolderCommand = new RelayCommand(ExecuteCreateSampleFolder);
        SetupCommand = new RelayCommand(ExecuteSetup);
        OpenDetailViewCommand = new RelayCommand<FileGroupViewModel>(ExecuteOpenDetailView);
        SelectAllCommand = new RelayCommand(ExecuteSelectAll);
        DeselectAllCommand = new RelayCommand(ExecuteDeselectAll);
        ToggleNir2FilteringCommand = new RelayCommand(ExecuteToggleNir2Filtering);

        // Subscribe to orchestrator events
        _orchestrator.GroupCreated += OnGroupCreated;
        _orchestrator.GroupUpdated += OnGroupUpdated;
        _orchestrator.GroupRemoved += OnGroupRemoved;
        _orchestrator.MonitoringError += OnMonitoringError;

        // Subscribe to statistics service events
        _statisticsService.FileCountsUpdated += OnFileCountsUpdated;
        _statisticsService.MatchingStatisticsUpdated += OnMatchingStatisticsUpdated;

        // Subscribe to program launcher status events
        _generalCameraLauncher.StatusChanged += OnGeneralCameraStatusChanged;
        _nirCameraLauncher.StatusChanged += OnNirCameraStatusChanged;
        _nir2CameraLauncher.StatusChanged += OnNir2FilteringStatusChanged;

        // Initialize program status
        UpdateProgramStatus();

        // Add initial log message
        AddLogMessage(LogSeverity.Info, "System", "Application started");
        // Initialize child ViewModels
        DetailPreviewVM = new DetailPreviewViewModel();

        _logger.LogInformation("MainWindowViewModel initialized with all service dependencies and event subscriptions");
    }

    #region Properties

    /// <summary>
    /// ViewModel for the Detail Preview pane.
    /// </summary>
    public DetailPreviewViewModel DetailPreviewVM { get; }

    /// <summary>
    /// All file groups (unified view).
    /// </summary>
    public ObservableCollection<FileGroupViewModel> FileGroups { get; }

    /// <summary>
    /// File groups for Line 1.
    /// </summary>
    public ObservableCollection<FileGroupViewModel> Line1Groups { get; }

    /// <summary>
    /// File groups for Line 2.
    /// </summary>
    public ObservableCollection<FileGroupViewModel> Line2Groups { get; }

    /// <summary>
    /// Log messages for display.
    /// </summary>
    public ObservableCollection<LogMessage> LogMessages { get; }

    /// <summary>
    /// Currently selected file group (computed based on active tab).
    /// For Combined tab (index 2), returns the first non-null selection from Line1 or Line2.
    /// </summary>
    public FileGroupViewModel? SelectedGroup => ActiveTabIndex switch
    {
        0 => SelectedLine1Group,
        1 => SelectedLine2Group,
        2 => SelectedLine1Group ?? SelectedLine2Group, // Combined: use either
        _ => null
    };

    /// <summary>
    /// Whether the system is currently monitoring.
    /// </summary>
    public bool IsMonitoring
    {
        get => _isMonitoring;
        set
        {
            if (SetProperty(ref _isMonitoring, value))
            {
                ((RelayCommand)StartCommand).RaiseCanExecuteChanged();
                ((RelayCommand)StopCommand).RaiseCanExecuteChanged();
                StatusMessage = value ? "Monitoring..." : "Ready";
            }
        }
    }

    /// <summary>
    /// Whether the system is in separated line mode (vs unified mode).
    /// </summary>
    public bool IsSeparatedMode
    {
        get => _isSeparatedMode;
        set => SetProperty(ref _isSeparatedMode, value);
    }

    /// <summary>
    /// Total number of file groups.
    /// </summary>
    public int TotalGroups
    {
        get => _totalGroups;
        set => SetProperty(ref _totalGroups, value);
    }

    /// <summary>
    /// Match rate percentage.
    /// </summary>
    public double MatchRate
    {
        get => _matchRate;
        set => SetProperty(ref _matchRate, value);
    }

    /// <summary>
    /// Number of failed matches.
    /// </summary>
    public int Failures
    {
        get => _failures;
        set => SetProperty(ref _failures, value);
    }

    /// <summary>
    /// Current status message.
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>
    /// NIR connection status (OK/Error).
    /// </summary>
    public string NirConnectionStatus
    {
        get => _nirConnectionStatus;
        set => SetProperty(ref _nirConnectionStatus, value);
    }

    /// <summary>
    /// Current time for status bar display.
    /// </summary>
    public DateTime CurrentTime
    {
        get => _currentTime;
        set => SetProperty(ref _currentTime, value);
    }

    /// <summary>
    /// Date input for path auto-configuration (YYYYMMDD format).
    /// </summary>
    public string DateInput
    {
        get => _dateInput;
        set => SetProperty(ref _dateInput, value);
    }

    /// <summary>
    /// Sample folder name for creation.
    /// </summary>
    public string SampleFolderName
    {
        get => _sampleFolderName;
        set => SetProperty(ref _sampleFolderName, value);
    }

    /// <summary>
    /// Sample name for workflow control.
    /// </summary>
    public string SampleName
    {
        get => _sampleName;
        set => SetProperty(ref _sampleName, value);
    }

    /// <summary>
    /// Move NIR configuration (UI binding).
    /// Format: empty = all, "0" = skip NIR groups, "N" = max N NIR groups.
    /// </summary>
    public string MoveNir
    {
        get => _moveNir;
        set
        {
            if (SetProperty(ref _moveNir, value))
            {
                // Save to configuration on change
                _ = SaveMoveNirCountAsync();
            }
        }
    }

    /// <summary>
    /// Move all data configuration (UI binding).
    /// Format: empty = all, "0" = skip move, "N" = max N groups.
    /// </summary>
    public string MoveAllData
    {
        get => _moveAllData;
        set
        {
            if (SetProperty(ref _moveAllData, value))
            {
                // Save to configuration on change
                _ = SaveMoveAllDataCountAsync();
            }
        }
    }

    /// <summary>
    /// Count of groups with NIR (for statistics bar).
    /// </summary>
    public int WithNirCount
    {
        get => _withNirCount;
        set => SetProperty(ref _withNirCount, value);
    }

    /// <summary>
    /// Count of groups without NIR (for statistics bar).
    /// </summary>
    public int WithoutNirCount
    {
        get => _withoutNirCount;
        set => SetProperty(ref _withoutNirCount, value);
    }

    /// <summary>
    /// Count of failed groups (for statistics bar).
    /// </summary>
    public int FailedCount
    {
        get => _failedCount;
        set => SetProperty(ref _failedCount, value);
    }

    private int _abnormalCount;
    
    /// <summary>
    /// Count of abnormal groups (for statistics bar).
    /// </summary>
    public int AbnormalCount
    {
        get => _abnormalCount;
        set => SetProperty(ref _abnormalCount, value);
    }

    #endregion

    #region File Count Statistics Properties

    public int NirCount
    {
        get => _nirCount;
        set => SetProperty(ref _nirCount, value);
    }

    public int Nir2Count
    {
        get => _nir2Count;
        set => SetProperty(ref _nir2Count, value);
    }

    public int NormalCount
    {
        get => _normalCount;
        set => SetProperty(ref _normalCount, value);
    }

    public int Normal2Count
    {
        get => _normal2Count;
        set => SetProperty(ref _normal2Count, value);
    }

    public int Cam1Count
    {
        get => _cam1Count;
        set => SetProperty(ref _cam1Count, value);
    }

    public int Cam2Count
    {
        get => _cam2Count;
        set => SetProperty(ref _cam2Count, value);
    }

    public int Cam3Count
    {
        get => _cam3Count;
        set => SetProperty(ref _cam3Count, value);
    }

    public int Cam4Count
    {
        get => _cam4Count;
        set => SetProperty(ref _cam4Count, value);
    }

    public int Cam5Count
    {
        get => _cam5Count;
        set => SetProperty(ref _cam5Count, value);
    }

    public int Cam6Count
    {
        get => _cam6Count;
        set => SetProperty(ref _cam6Count, value);
    }

    #endregion

    #region Matching Statistics Properties - Unified Mode

    public int UnifiedTotalGroups
    {
        get => _unifiedTotalGroups;
        set => SetProperty(ref _unifiedTotalGroups, value);
    }

    public int UnifiedWithNir
    {
        get => _unifiedWithNir;
        set => SetProperty(ref _unifiedWithNir, value);
    }

    public int UnifiedWithoutNir
    {
        get => _unifiedWithoutNir;
        set => SetProperty(ref _unifiedWithoutNir, value);
    }

    public int UnifiedFailed
    {
        get => _unifiedFailed;
        set => SetProperty(ref _unifiedFailed, value);
    }

    #endregion

    #region Matching Statistics Properties - Separated Mode

    public int Line1TotalGroups
    {
        get => _line1TotalGroups;
        set => SetProperty(ref _line1TotalGroups, value);
    }

    public int Line1WithNir
    {
        get => _line1WithNir;
        set => SetProperty(ref _line1WithNir, value);
    }

    public int Line1WithoutNir
    {
        get => _line1WithoutNir;
        set => SetProperty(ref _line1WithoutNir, value);
    }

    public int Line1Failed
    {
        get => _line1Failed;
        set => SetProperty(ref _line1Failed, value);
    }

    public int Line2TotalGroups
    {
        get => _line2TotalGroups;
        set => SetProperty(ref _line2TotalGroups, value);
    }

    public int Line2WithNir
    {
        get => _line2WithNir;
        set => SetProperty(ref _line2WithNir, value);
    }

    public int Line2WithoutNir
    {
        get => _line2WithoutNir;
        set => SetProperty(ref _line2WithoutNir, value);
    }

    public int Line2Failed
    {
        get => _line2Failed;
        set => SetProperty(ref _line2Failed, value);
    }

    #endregion

    #region Window State Properties

    public double WindowWidth
    {
        get => _windowWidth;
        set => SetProperty(ref _windowWidth, value);
    }

    public double WindowHeight
    {
        get => _windowHeight;
        set => SetProperty(ref _windowHeight, value);
    }

    public double WindowLeft
    {
        get => _windowLeft;
        set => SetProperty(ref _windowLeft, value);
    }

    public double WindowTop
    {
        get => _windowTop;
        set => SetProperty(ref _windowTop, value);
    }

    #endregion

    #region UI Display Settings Properties

    /// <summary>
    /// Image display width in DataGrid cells (pixels).
    /// </summary>
    public int DisplayImageWidth
    {
        get => _displayImageWidth;
        set => SetProperty(ref _displayImageWidth, value);
    }

    /// <summary>
    /// Image display height in DataGrid cells (pixels).
    /// </summary>
    public int DisplayImageHeight
    {
        get => _displayImageHeight;
        set => SetProperty(ref _displayImageHeight, value);
    }

    /// <summary>
    /// DataGrid row height (pixels).
    /// </summary>
    public int DataGridRowHeight
    {
        get => _dataGridRowHeight;
        set => SetProperty(ref _dataGridRowHeight, value);
    }

    /// <summary>
    /// NIR graph display width in DataGrid cells (pixels).
    /// </summary>
    public int NirDisplayWidth
    {
        get => _nirDisplayWidth;
        set => SetProperty(ref _nirDisplayWidth, value);
    }

    /// <summary>
    /// NIR graph display height in DataGrid cells (pixels).
    /// </summary>
    public int NirDisplayHeight
    {
        get => _nirDisplayHeight;
        set => SetProperty(ref _nirDisplayHeight, value);
    }

    #endregion

    #region Progress and Operation State Properties

    /// <summary>
    /// Progress value (0-100) for current operation.
    /// </summary>
    public double ProgressValue
    {
        get => _progressValue;
        set => SetProperty(ref _progressValue, value);
    }

    /// <summary>
    /// Whether an operation is currently in progress.
    /// </summary>
    public bool IsOperationInProgress
    {
        get => _isOperationInProgress;
        set
        {
            if (SetProperty(ref _isOperationInProgress, value))
            {
                ((RelayCommand)StartCommand).RaiseCanExecuteChanged();
                ((RelayCommand)StopCommand).RaiseCanExecuteChanged();
                ((RelayCommand)MoveCommand).RaiseCanExecuteChanged();
                ((RelayCommand)DeleteCommand).RaiseCanExecuteChanged();
                ((RelayCommand)RefreshCommand).RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Index of the currently active tab (0=Line1, 1=Line2, 2=Combined).
    /// </summary>
    public int ActiveTabIndex
    {
        get => _activeTabIndex;
        set
        {
            if (SetProperty(ref _activeTabIndex, value))
            {
                OnPropertyChanged(nameof(SelectedGroup));
                ((RelayCommand)MoveCommand).RaiseCanExecuteChanged();
                ((RelayCommand)DeleteCommand).RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Selected group in Line 1 tab.
    /// </summary>
    public FileGroupViewModel? SelectedLine1Group
    {
        get => _selectedLine1Group;
        set
        {
            if (SetProperty(ref _selectedLine1Group, value))
            {
                OnPropertyChanged(nameof(SelectedGroup));
                ((RelayCommand)MoveCommand).RaiseCanExecuteChanged();
                ((RelayCommand)DeleteCommand).RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Selected group in Line 2 tab.
    /// </summary>
    public FileGroupViewModel? SelectedLine2Group
    {
        get => _selectedLine2Group;
        set
        {
            if (SetProperty(ref _selectedLine2Group, value))
            {
                OnPropertyChanged(nameof(SelectedGroup));
                ((RelayCommand)MoveCommand).RaiseCanExecuteChanged();
                ((RelayCommand)DeleteCommand).RaiseCanExecuteChanged();
            }
        }
    }

    #endregion

    #region Commands

    public ICommand StartCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand MoveCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand CreateSampleFolderCommand { get; }
    public ICommand SetupCommand { get; }
    public ICommand OpenDetailViewCommand { get; }
    public ICommand SelectAllCommand { get; }
    public ICommand DeselectAllCommand { get; }
    public ICommand ToggleNir2FilteringCommand { get; }

    #endregion

    #region Command Implementations

    private bool CanExecuteStart()
    {
        return !IsMonitoring && !IsOperationInProgress;
    }

    private void ExecuteStart()
    {
        // Synchronous wrapper for async command
        _ = ExecuteStartAsync();
    }

    /// <summary>
    /// Executes the Start command asynchronously.
    /// Loads configuration and starts monitoring services.
    /// </summary>
    public async Task ExecuteStartAsync()
    {
        // Check if already monitoring
        if (IsMonitoring)
        {
            _logger.LogWarning("Start command called but monitoring is already active");
            return;
        }

        try
        {
            _logger.LogInformation("Starting monitoring");
            AddLogMessage(LogSeverity.Info, "System", "Starting monitoring...");

            // Load configuration from ConfigurationManager
            var config = await _configManager.LoadConfigurationAsync<ApplicationConfiguration>();

            // Auto-configure paths with today's date (replace date patterns in existing paths)
            await AutoConfigurePathsAsync(config);

            // Validate configuration paths exist
            var missingPaths = new List<string>();
            if (!string.IsNullOrEmpty(config.MatchingSettings.Nir1Path) && !Directory.Exists(config.MatchingSettings.Nir1Path))
                missingPaths.Add($"NIR1: {config.MatchingSettings.Nir1Path}");
            if (!string.IsNullOrEmpty(config.MatchingSettings.Normal1Path) && !Directory.Exists(config.MatchingSettings.Normal1Path))
                missingPaths.Add($"Normal1: {config.MatchingSettings.Normal1Path}");
            if (!string.IsNullOrEmpty(config.MatchingSettings.Nir2Path) && !Directory.Exists(config.MatchingSettings.Nir2Path))
                missingPaths.Add($"NIR2: {config.MatchingSettings.Nir2Path}");
            if (!string.IsNullOrEmpty(config.MatchingSettings.Normal2Path) && !Directory.Exists(config.MatchingSettings.Normal2Path))
                missingPaths.Add($"Normal2: {config.MatchingSettings.Normal2Path}");

            if (missingPaths.Count > 0)
            {
                var errorMsg = $"Configuration paths do not exist:\n{string.Join("\n", missingPaths)}\n\nPlease configure paths in Settings.";
                _logger.LogWarning("Cannot start monitoring - invalid paths: {Paths}", string.Join(", ", missingPaths));
                AddLogMessage(LogSeverity.Warning, "System", errorMsg);
                
                await WpfApplication.Current.Dispatcher.InvokeAsync(() =>
                {
                    WpfMessageBox.Show(errorMsg, "Configuration Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                });
                
                StatusMessage = "Configuration required";
                return;
            }

            // Call StatisticsService.StartMonitoringAsync(config) FIRST
            // It doesn't need to wait for initial scan
            _logger.LogError("DEBUG: About to call StatisticsService.StartMonitoringAsync");
            await _statisticsService.StartMonitoringAsync(config);
            _logger.LogError("DEBUG: StatisticsService.StartMonitoringAsync returned");

            // Call MonitoringOrchestrator.StartAsync(config)
            // This does initial scan which may take time
            await _orchestrator.StartAsync(config);

            // Load UI display settings from configuration
            DisplayImageWidth = config.UISettings.DisplayImageWidth;
            DisplayImageHeight = config.UISettings.DisplayImageHeight;
            
            // Enforce minimum row height of 140 to accommodate labels (migration from old default of 100)
            if (config.UISettings.DataGridRowHeight < 140)
            {
                DataGridRowHeight = 140;
                // Ideally we should save this back to config, but for now we just enforce it in runtime
            }
            else
            {
                DataGridRowHeight = config.UISettings.DataGridRowHeight;
            }

            NirDisplayWidth = config.UISettings.NirDisplayWidth;
            NirDisplayHeight = config.UISettings.NirDisplayHeight;

            // Load MoveNir and MoveAllData settings from configuration
            if (config.MatchingSettings.MoveNir.HasValue)
            {
                MoveNir = config.MatchingSettings.MoveNir.Value.ToString();
            }
            else
            {
                MoveNir = ""; // Empty = all groups
            }

            if (config.MatchingSettings.MoveAllData.HasValue)
            {
                MoveAllData = config.MatchingSettings.MoveAllData.Value.ToString();
            }
            else
            {
                MoveAllData = ""; // Empty = all groups
            }

            // Set IsMonitoring = true on success
            IsMonitoring = true;

            // Add diagnostic information after initial scan
            await WpfApplication.Current.Dispatcher.InvokeAsync(() =>
            {
                var totalGroups = FileGroups.Count;
                var line1Count = Line1Groups.Count;
                var line2Count = Line2Groups.Count;
                
                var normalOnlyCount = FileGroups.Count(g => !string.IsNullOrEmpty(g.NormalFolder) && g.Model.CameraFiles.Count == 0);
                var normalWithCamCount = FileGroups.Count(g => !string.IsNullOrEmpty(g.NormalFolder) && g.Model.CameraFiles.Count > 0);
                var camOnlyCount = FileGroups.Count(g => string.IsNullOrEmpty(g.NormalFolder) && g.Model.CameraFiles.Count > 0);
                var nirOnlyCount = FileGroups.Count(g => string.IsNullOrEmpty(g.NormalFolder) && g.Model.CameraFiles.Count == 0 && g.HasNir);
                
                AddLogMessage(LogSeverity.Info, "Diagnostic", 
                    $"[DIAGNOSTIC] Final group count: Line1={line1Count}, Line2={line2Count}, Total={totalGroups}");
                AddLogMessage(LogSeverity.Info, "Diagnostic", 
                    $"[DIAGNOSTIC] Group composition: NormalOnly={normalOnlyCount}, NormalWithCam={normalWithCamCount}, CamOnly={camOnlyCount}, NirOnly={nirOnlyCount}");
            });

            // Log success
            _logger.LogInformation("Monitoring started successfully");
            AddLogMessage(LogSeverity.Info, "System", "Monitoring started successfully");
            StatusMessage = "Monitoring...";
        }
        catch (Exception ex)
        {
            // Handle exceptions and display error messages
            _logger.LogError(ex, "Failed to start monitoring");
            AddLogMessage(LogSeverity.Error, "System", $"Failed to start monitoring: {ex.Message}");
            
            await WpfApplication.Current.Dispatcher.InvokeAsync(() =>
            {
                WpfMessageBox.Show($"Failed to start monitoring:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            });
            
            // Ensure IsMonitoring remains false on error
            IsMonitoring = false;
            StatusMessage = "Error - Check logs";
        }
    }

    private bool CanExecuteStop()
    {
        return IsMonitoring && !IsOperationInProgress;
    }

    private void ExecuteStop()
    {
        // Synchronous wrapper for async command
        _ = ExecuteStopAsync();
    }

    /// <summary>
    /// Executes the Stop command asynchronously.
    /// Stops monitoring services and updates UI state.
    /// </summary>
    public async Task ExecuteStopAsync()
    {
        // Check if not monitoring
        if (!IsMonitoring)
        {
            _logger.LogWarning("Stop command called but monitoring is not active");
            return;
        }

        try
        {
            _logger.LogInformation("Stopping monitoring");
            AddLogMessage(LogSeverity.Info, "System", "Stopping monitoring...");

            // Call MonitoringOrchestrator.StopAsync()
            await _orchestrator.StopAsync();

            // Call StatisticsService.StopMonitoringAsync()
            await _statisticsService.StopMonitoringAsync();

            // Set IsMonitoring = false on success
            IsMonitoring = false;

            // Log success
            _logger.LogInformation("Monitoring stopped successfully");
            AddLogMessage(LogSeverity.Info, "System", "Monitoring stopped successfully");
        }
        catch (Exception ex)
        {
            // Handle exceptions and display error messages
            _logger.LogError(ex, "Failed to stop monitoring");
            AddLogMessage(LogSeverity.Error, "System", $"Failed to stop monitoring: {ex.Message}");
            
            // Set IsMonitoring to false even on error (best effort stop)
            IsMonitoring = false;
            StatusMessage = "Error - Check logs";
        }
    }

    private bool CanExecuteMove()
    {
        var selectedGroups = GetSelectedGroups();
        return selectedGroups.Count > 0 && !IsOperationInProgress;
    }

    private void ExecuteMove()
    {
        // Synchronous wrapper for async command
        _ = ExecuteMoveAsync();
    }

    /// <summary>
    /// Executes the Move command asynchronously.
    /// Moves selected file groups to the output path.
    /// </summary>
    public async Task ExecuteMoveAsync()
    {
        var selectedGroups = GetSelectedGroups();
        if (selectedGroups.Count == 0)
        {
            _logger.LogWarning("Move command called but no groups selected");
            return;
        }

        if (IsOperationInProgress)
        {
            _logger.LogWarning("Move command called but another operation is in progress");
            return;
        }

        try
        {
            // Load configuration to get output path
            var config = await _configManager.LoadConfigurationAsync<ApplicationConfiguration>();
            var outputPath = config.MatchingSettings.OutputPath;

            // Apply NIR and Data count limits
            var filteredGroups = ApplyFilters(selectedGroups, config);
            
            if (filteredGroups.Count == 0 && selectedGroups.Count > 0)
            {
                // If we had groups selected but filters removed all of them (or MoveAllData=0)
                // We should check if it was specifically MoveAllData=0 which logs its own message, 
                // or if filters just reduced it to zero.
                
                // If MoveAllData was 0, we already logged and we should return.
                if (config.MatchingSettings.MoveAllData == 0) return;

                // Otherwise just log that nothing to move
                _logger.LogInformation("No groups to move after applying limits");
                 AddLogMessage(LogSeverity.Info, "FileOperation", "No groups to move after applying limits");
                return;
            }
            
            // Use the filtered list for the operation
            selectedGroups = filteredGroups;

            // Validate output path
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                var errorMsg = "Output path is not configured. Please set it in Settings.";
                _logger.LogWarning("Cannot move files - output path not configured");
                AddLogMessage(LogSeverity.Warning, "FileOperation", errorMsg);

                await WpfApplication.Current.Dispatcher.InvokeAsync(() =>
                {
                    WpfMessageBox.Show(errorMsg, "Configuration Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                });
                return;
            }

            if (!Directory.Exists(outputPath))
            {
                var errorMsg = $"Output path does not exist: {outputPath}";
                _logger.LogWarning("Cannot move files - output path does not exist: {Path}", outputPath);
                AddLogMessage(LogSeverity.Warning, "FileOperation", errorMsg);

                await WpfApplication.Current.Dispatcher.InvokeAsync(() =>
                {
                    WpfMessageBox.Show(errorMsg, "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                });
                return;
            }

            // Begin operation
            var cancellationToken = BeginOperation();

            _logger.LogInformation("Starting move operation for {Count} groups", selectedGroups.Count);
            AddLogMessage(LogSeverity.Info, "FileOperation", $"Moving {selectedGroups.Count} group(s) to {outputPath}");

            // Create progress reporter
            var progress = CreateProgressReporter();

            // Track successfully moved groups for removal
            var movedGroups = new List<string>();
            int successCount = 0;
            int failedCount = 0;
            int totalFilesFailed = 0;

            // Move each selected group
            foreach (var groupViewModel in selectedGroups)
            {
                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogInformation("Moving group {GroupId}", groupViewModel.GroupId);
                AddLogMessage(LogSeverity.Info, "FileOperation", $"Moving group {groupViewModel.GroupId}");

                // Call MoveFileGroupAsync
                var result = await _fileOperationService.MoveFileGroupAsync(
                    groupViewModel.Model,
                    outputPath,
                    null,  // subject - will implement in next step
                    progress,
                    onConflict: ResolveConflict,
                    cancellationToken);

                if (result.Success)
                {
                    successCount++;
                    movedGroups.Add(groupViewModel.GroupId);
                    _logger.LogInformation("Successfully moved group {GroupId} ({Processed}/{Total} files)",
                        groupViewModel.GroupId, result.FilesProcessed, result.FilesProcessed + result.FilesFailed);
                    AddLogMessage(LogSeverity.Info, "FileOperation",
                        $"Successfully moved group {groupViewModel.GroupId} ({result.FilesProcessed} files)");
                }
                else
                {
                    failedCount++;
                    totalFilesFailed += result.FilesFailed;
                    _logger.LogError("Failed to move group {GroupId}: {Error} ({FilesFailed} files failed)",
                        groupViewModel.GroupId, result.ErrorMessage, result.FilesFailed);
                    AddLogMessage(LogSeverity.Error, "FileOperation",
                        $"Failed to move group {groupViewModel.GroupId}: {result.ErrorMessage} ({result.FilesFailed} files failed)");
                }
            }

            // Remove successfully moved groups from collections
            foreach (var groupId in movedGroups)
            {
                RemoveFileGroup(groupId);
            }

            // Show summary
            var summaryMsg = $"Move operation complete: {successCount} succeeded, {failedCount} failed";
            if (totalFilesFailed > 0)
            {
                summaryMsg += $" (Total {totalFilesFailed} files failed)";
            }
            
            _logger.LogInformation(summaryMsg);
            AddLogMessage(failedCount > 0 || totalFilesFailed > 0 ? LogSeverity.Warning : LogSeverity.Info,
                "FileOperation", summaryMsg);

            if (failedCount > 0)
            {
                await WpfApplication.Current.Dispatcher.InvokeAsync(() =>
                {
                    WpfMessageBox.Show(summaryMsg, "Move Complete", MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                });
            }

            StatusMessage = $"Moved {successCount} group(s)";
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Move operation cancelled by user");
            AddLogMessage(LogSeverity.Warning, "FileOperation", "Move operation cancelled");
            StatusMessage = "Move operation cancelled";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during move operation");
            AddLogMessage(LogSeverity.Error, "FileOperation", $"Move operation error: {ex.Message}");

            await WpfApplication.Current.Dispatcher.InvokeAsync(() =>
            {
                WpfMessageBox.Show($"Error during move operation:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            });

            StatusMessage = "Move operation failed";
        }
        finally
        {
            EndOperation();
            ((RelayCommand)MoveCommand).RaiseCanExecuteChanged();
        }
    }

    /// <summary>
    /// Applies configured limits to the list of groups to move.
    /// </summary>
    private IReadOnlyList<FileGroupViewModel> ApplyFilters(
        IReadOnlyList<FileGroupViewModel> groups, 
        ApplicationConfiguration config)
    {
        IEnumerable<FileGroupViewModel> result = groups;

        // 1. NIR Count Limit
        int? moveNirLimit = config.MatchingSettings.MoveNir;
        if (moveNirLimit.HasValue)
        {
            if (moveNirLimit.Value == 0)
            {
                // Exclude all NIR groups
                result = result.Where(g => !g.Model.HasNir);
                AddLogMessage(LogSeverity.Info, "FileOperation", "Limit: Excluded NIR groups (Count=0)");
            }
            else if (moveNirLimit.Value > 0)
            {
                // Apply NIR count limit helper
                result = ApplyNirCountLimit(result, moveNirLimit.Value);
                AddLogMessage(LogSeverity.Info, "FileOperation", $"Limit: Top {moveNirLimit.Value} NIR groups");
            }
            // If null, do nothing (keep all)
        }

        // 2. Data Count Limit
        int? moveAllDataLimit = config.MatchingSettings.MoveAllData;
        if (moveAllDataLimit.HasValue)
        {
            if (moveAllDataLimit.Value == 0)
            {
                // Skip move entirely
                _logger.LogInformation("Move All Data is 0, skipping move operation.");
                AddLogMessage(LogSeverity.Info, "FileOperation", "Limit: Move All Data is 0 (Skip)");
                return new List<FileGroupViewModel>();
            }
            else if (moveAllDataLimit.Value > 0)
            {
                // Take top N groups (sorted by filename)
                result = result
                    .OrderBy(g => g.Model.NirKey ?? g.Model.GroupId)
                    .Take(moveAllDataLimit.Value);
                AddLogMessage(LogSeverity.Info, "FileOperation", $"Limit: Top {moveAllDataLimit.Value} total groups");
            }
            // If null, do nothing (keep all)
        }

        return result.ToList();
    }

    /// <summary>
    /// Helper to limit the number of NIR groups while keeping all non-NIR groups.
    /// </summary>
    private IEnumerable<FileGroupViewModel> ApplyNirCountLimit(
        IEnumerable<FileGroupViewModel> groups, 
        int limit)
    {
        var withNir = groups.Where(g => g.Model.HasNir);
        var withoutNir = groups.Where(g => !g.Model.HasNir);

        // Sort NIR groups by filename (NirKey) and take top 'limit'
        var selectedWithNir = withNir
            .OrderBy(g => g.Model.NirKey ?? g.Model.GroupId)
            .Take(limit);

        // Combine with non-NIR groups
        return selectedWithNir.Concat(withoutNir);
    }

    private bool CanExecuteDelete()
    {
        var selectedGroups = GetSelectedGroups();
        return selectedGroups.Count > 0 && !IsOperationInProgress;
    }

    private void ExecuteDelete()
    {
        _ = ExecuteDeleteAsync();
    }

    /// <summary>
    /// Executes the Delete command asynchronously.
    /// Deletes selected file groups (soft delete to quarantine).
    /// </summary>
    public async Task ExecuteDeleteAsync()
    {
        var selectedGroups = GetSelectedGroups();
        if (selectedGroups.Count == 0) return;

        if (IsOperationInProgress) return;

        // Load configuration for quarantine path
        var config = await _configManager.LoadConfigurationAsync<ApplicationConfiguration>();
        var quarantinePath = config.WorkflowSettings.DeleteQuarantinePath;
        if (string.IsNullOrWhiteSpace(quarantinePath))
        {
            // 기본: BasePath/Trash
            var basePath = string.IsNullOrWhiteSpace(config.BasePath) ? "D:/Data" : config.BasePath;
            quarantinePath = Path.Combine(basePath, "Trash");
        }

        // Confirmation dialog (soft delete notice)
        var message = $"Are you sure you want to delete (move to quarantine) {selectedGroups.Count} selected group(s)?\n" +
                      $"Quarantine folder: {quarantinePath}";
        
        var confirmResult = WpfMessageBox.Show(message, "Confirm Delete", 
            MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);

        if (confirmResult != MessageBoxResult.Yes) return;

        // Begin operation
        var cancellationToken = BeginOperation();

        // Extract subject/sample name from toolbar input
        var subject = _sampleName;

        _logger.LogInformation("Starting delete operation for {Count} groups with subject '{Subject}'", selectedGroups.Count, subject);
        AddLogMessage(LogSeverity.Info, "FileOperation", $"Deleting {selectedGroups.Count} group(s) to subject '{subject}'");

        // Create progress reporter
        var progress = CreateProgressReporter();

        // Track successfully deleted groups for removal
        var deletedGroups = new List<string>();
        int successCount = 0;
        int failedCount = 0;
        int totalFilesFailed = 0;

        // Reset cached conflict resolution for this operation
        _cachedConflictResolution = null;

        try
        {
            foreach (var groupViewModel in selectedGroups)
            {
                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogInformation("Deleting group {GroupId}", groupViewModel.GroupId);
                AddLogMessage(LogSeverity.Info, "FileOperation", $"Deleting group {groupViewModel.GroupId}");

                // Call DeleteFileGroupAsync with subject
                var result = await _fileOperationService.DeleteFileGroupAsync(
                    groupViewModel.Model,
                    quarantinePath,
                    subject,  // ← NEW: Pass subject for structured paths
                    progress,
                    onConflict: ResolveConflict,
                    cancellationToken);

                if (result.Success)
                {
                    successCount++;
                    deletedGroups.Add(groupViewModel.GroupId);
                    _logger.LogInformation("Successfully deleted group {GroupId}", groupViewModel.GroupId);
                    AddLogMessage(LogSeverity.Info, "FileOperation", $"Successfully deleted group {groupViewModel.GroupId}");
                }
                else
                {
                    failedCount++;
                    totalFilesFailed += result.FilesFailed;
                    _logger.LogError("Failed to delete group {GroupId}: {Error} ({FilesFailed} files failed)",
                        groupViewModel.GroupId, result.ErrorMessage, result.FilesFailed);
                    AddLogMessage(LogSeverity.Error, "FileOperation",
                        $"Failed to delete group {groupViewModel.GroupId}: {result.ErrorMessage} ({result.FilesFailed} files failed)");
                }
            }

            // Remove successfully deleted groups from collections
            foreach (var groupId in deletedGroups)
            {
                RemoveFileGroup(groupId);
            }

            // Show summary
            var summaryMsg = $"Delete operation complete: {successCount} succeeded, {failedCount} failed";
            if (totalFilesFailed > 0)
            {
                summaryMsg += $" (Total {totalFilesFailed} files failed)";
            }
            
            _logger.LogInformation(summaryMsg);
            AddLogMessage(failedCount > 0 || totalFilesFailed > 0 ? LogSeverity.Warning : LogSeverity.Info,
                "FileOperation", summaryMsg);

            if (failedCount > 0)
            {
                await WpfApplication.Current.Dispatcher.InvokeAsync(() =>
                {
                    WpfMessageBox.Show(summaryMsg, "Delete Complete", MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                });
            }

            StatusMessage = $"Deleted {successCount} group(s)";
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Delete operation cancelled by user");
            AddLogMessage(LogSeverity.Warning, "FileOperation", "Delete operation cancelled");
            StatusMessage = "Delete operation cancelled";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during delete operation");
            AddLogMessage(LogSeverity.Error, "FileOperation", $"Delete operation error: {ex.Message}");
            
            await WpfApplication.Current.Dispatcher.InvokeAsync(() =>
            {
                WpfMessageBox.Show($"Error during delete operation:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            });
            
            StatusMessage = "Delete operation failed";
        }
        finally
        {
            EndOperation();
            ((RelayCommand)DeleteCommand).RaiseCanExecuteChanged();
            // Clear cached resolution after operation
            _cachedConflictResolution = null;
        }
    }

    private bool CanExecuteRefresh()
    {
        return !IsOperationInProgress;
    }

    private async void ExecuteRefresh()
    {
        await ExecuteRefreshAsync();
    }

    private async Task ExecuteRefreshAsync()
    {
        if (IsOperationInProgress) return;

        try
        {
            StatusMessage = "Refreshing...";
            AddLogMessage(LogSeverity.Info, "System", "Refreshing data...");

            // Begin operation (creates new cancellation token)
            var cancellationToken = BeginOperation();

            // CRITICAL FIX: Clear ALL UI groups BEFORE refresh
            // This prevents orphan groups when backend doesn't know about some groups
            // (e.g., when files deleted externally without triggering proper removal events)
            await WpfApplication.Current.Dispatcher.InvokeAsync(() =>
            {
                _logger.LogInformation("Clearing all UI groups before refresh");
                FileGroups.Clear();
                Line1Groups.Clear();
                Line2Groups.Clear();
                UpdateStatistics();
            });

            // Request orchestrator refresh with token
            // This will trigger GroupCreated events to repopulate UI with only existing files
            await _orchestrator.RefreshAsync(cancellationToken);

            // Add diagnostic information after refresh
            await WpfApplication.Current.Dispatcher.InvokeAsync(() =>
            {
                var totalGroups = FileGroups.Count;
                var line1Count = Line1Groups.Count;
                var line2Count = Line2Groups.Count;
                
                var normalOnlyCount = FileGroups.Count(g => !string.IsNullOrEmpty(g.NormalFolder) && g.Model.CameraFiles.Count == 0);
                var normalWithCamCount = FileGroups.Count(g => !string.IsNullOrEmpty(g.NormalFolder) && g.Model.CameraFiles.Count > 0);
                var camOnlyCount = FileGroups.Count(g => string.IsNullOrEmpty(g.NormalFolder) && g.Model.CameraFiles.Count > 0);
                var nirOnlyCount = FileGroups.Count(g => string.IsNullOrEmpty(g.NormalFolder) && g.Model.CameraFiles.Count == 0 && g.HasNir);
                
                AddLogMessage(LogSeverity.Info, "Diagnostic", 
                    $"[DIAGNOSTIC] Final group count: Line1={line1Count}, Line2={line2Count}, Total={totalGroups}");
                AddLogMessage(LogSeverity.Info, "Diagnostic", 
                    $"[DIAGNOSTIC] Group composition: NormalOnly={normalOnlyCount}, NormalWithCam={normalWithCamCount}, CamOnly={camOnlyCount}, NirOnly={nirOnlyCount}");
            });

            StatusMessage = "Refresh complete";
            AddLogMessage(LogSeverity.Info, "System", "Refresh completed successfully");
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Refresh cancelled";
            AddLogMessage(LogSeverity.Info, "System", "Refresh cancelled by user");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during refresh");
            StatusMessage = "Refresh failed";
            AddLogMessage(LogSeverity.Error, "System", $"Refresh failed: {ex.Message}");
        }
        finally
        {
            EndOperation();
        }
    }

    /// <summary>
    /// Automatically configures paths by replacing date patterns in existing paths with today's date.
    /// This follows the Python reference implementation behavior.
    /// </summary>
    private async Task AutoConfigurePathsAsync(ApplicationConfiguration config)
    {
        try
        {
            // Use today's date for path replacement
            var todayDate = DateTime.Today.ToString("yyyyMMdd");
            var datePattern = new System.Text.RegularExpressions.Regex(@"\d{8}");

            var updatedPaths = new List<string>();
            var createdFolders = new List<string>();
            var failedFolders = new List<string>();

            // Helper function to update a single path
            string UpdatePath(string path)
            {
                if (string.IsNullOrEmpty(path))
                    return path;

                var newPath = datePattern.Replace(path, todayDate);
                if (newPath != path)
                {
                    updatedPaths.Add($"{Path.GetFileName(path)} → {Path.GetFileName(newPath)}");

                    // Try to create the folder
                    try
                    {
                        if (!Directory.Exists(newPath))
                        {
                            Directory.CreateDirectory(newPath);
                            createdFolders.Add(newPath);
                            _logger.LogInformation("Created folder: {Path}", newPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        failedFolders.Add($"{newPath}: {ex.Message}");
                        _logger.LogWarning(ex, "Failed to create folder: {Path}", newPath);
                    }

                    return newPath;
                }
                return path;
            }

            // Update all matching paths
            config.MatchingSettings.Nir1Path = UpdatePath(config.MatchingSettings.Nir1Path);
            config.MatchingSettings.Nir2Path = UpdatePath(config.MatchingSettings.Nir2Path);
            config.MatchingSettings.Normal1Path = UpdatePath(config.MatchingSettings.Normal1Path);
            config.MatchingSettings.Normal2Path = UpdatePath(config.MatchingSettings.Normal2Path);
            config.MatchingSettings.Camera1Path = UpdatePath(config.MatchingSettings.Camera1Path);
            config.MatchingSettings.Camera2Path = UpdatePath(config.MatchingSettings.Camera2Path);
            config.MatchingSettings.Camera3Path = UpdatePath(config.MatchingSettings.Camera3Path);
            config.MatchingSettings.Camera4Path = UpdatePath(config.MatchingSettings.Camera4Path);
            config.MatchingSettings.Camera5Path = UpdatePath(config.MatchingSettings.Camera5Path);
            config.MatchingSettings.Camera6Path = UpdatePath(config.MatchingSettings.Camera6Path);
            config.MatchingSettings.OutputPath = UpdatePath(config.MatchingSettings.OutputPath);

            // Save updated configuration
            if (updatedPaths.Count > 0)
            {
                await _configManager.SaveConfigurationAsync(config);

                AddLogMessage(LogSeverity.Info, "Configuration",
                    $"Auto-configured {updatedPaths.Count} paths for date {todayDate}");

                if (createdFolders.Count > 0)
                {
                    AddLogMessage(LogSeverity.Info, "Configuration",
                        $"Created {createdFolders.Count} folders");
                }

                if (failedFolders.Count > 0)
                {
                    AddLogMessage(LogSeverity.Warning, "Configuration",
                        $"Failed to create {failedFolders.Count} folders");
                }

                _logger.LogInformation("Auto-configured paths: {Paths}, Created: {Created}, Failed: {Failed}",
                    string.Join(", ", updatedPaths), createdFolders.Count, failedFolders.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during automatic path configuration");
            AddLogMessage(LogSeverity.Error, "Configuration", $"Path auto-configuration failed: {ex.Message}");
        }
    }

    private void ExecuteCreateSampleFolder()
    {
        _ = ExecuteCreateSampleFolderAsync();
    }

    /// <summary>
    /// Executes the Create Sample Folder command asynchronously.
    /// Creates sample folders in all configured monitoring paths.
    /// </summary>
    private async Task ExecuteCreateSampleFolderAsync()
    {
        if (IsOperationInProgress) return;

        try
        {
            // Generate sample folder name with timestamp
            var sampleName = $"Sample_{DateTime.Now:yyyyMMdd_HHmmss}";

            // Confirm with user
            var confirmMessage = $"Create sample folder '{sampleName}' in all configured paths?\n\n" +
                                 "This will create folders in NIR, Normal, and Camera paths.";
            
            var confirmResult = await WpfApplication.Current.Dispatcher.InvokeAsync(() =>
                WpfMessageBox.Show(confirmMessage, "Confirm Sample Folder Creation",
                    MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No));

            if (confirmResult != MessageBoxResult.Yes) return;

            StatusMessage = "Creating sample folders...";
            AddLogMessage(LogSeverity.Info, "FileOperation", $"Creating sample folder: {sampleName}");

            // Begin operation
            var cancellationToken = BeginOperation();

            // Load current configuration
            var config = await _configManager.LoadConfigurationAsync<ApplicationConfiguration>();

            // Create sample folders using PathManagementService
            var result = await _pathManagementService.CreateSampleFoldersAsync(
                sampleName, 
                config, 
                cancellationToken);

            if (result)
            {
                StatusMessage = "Sample folders created successfully";
                AddLogMessage(LogSeverity.Info, "FileOperation", 
                    $"Successfully created sample folder: {sampleName}");

                // Show success message
                await WpfApplication.Current.Dispatcher.InvokeAsync(() =>
                {
                    WpfMessageBox.Show($"Sample folders created successfully.\n\n" +
                                      $"Folder name: {sampleName}\n\n" +
                                      $"Created in all configured monitoring paths.",
                        "Sample Creation Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                });
            }
            else
            {
                StatusMessage = "Sample folder creation failed";
                AddLogMessage(LogSeverity.Warning, "FileOperation", 
                    $"Failed to create some or all sample folders");

                await WpfApplication.Current.Dispatcher.InvokeAsync(() =>
                {
                    WpfMessageBox.Show("Sample folder creation completed with warnings.\n" +
                                      "Check logs for details.",
                        "Sample Creation Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                });
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Sample folder creation cancelled by user");
            AddLogMessage(LogSeverity.Warning, "FileOperation", "Sample folder creation cancelled");
            StatusMessage = "Sample folder creation cancelled";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during sample folder creation");
            AddLogMessage(LogSeverity.Error, "FileOperation", $"Sample folder creation error: {ex.Message}");
            
            await WpfApplication.Current.Dispatcher.InvokeAsync(() =>
            {
                WpfMessageBox.Show($"Failed to create sample folders:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            });
            
            StatusMessage = "Sample folder creation failed";
        }
        finally
        {
            EndOperation();
        }
    }

    /// <summary>
    /// Callback for resolving file name conflicts during move operations.
    /// Must be called from a background thread and marshals to UI thread.
    /// Uses cached resolution after first conflict to avoid repeated dialogs.
    /// </summary>
    /// <param name="conflictPath">The full path of the implementation file that caused the conflict.</param>
    /// <returns>Resolution strategy chosen by the user.</returns>
    private ConflictResolution ResolveConflict(string conflictPath)
    {
        // Return cached resolution if already decided
        if (_cachedConflictResolution.HasValue)
        {
            _logger.LogDebug("Using cached conflict resolution: {Resolution} for {Path}", 
                _cachedConflictResolution.Value, conflictPath);
            return _cachedConflictResolution.Value;
        }

        ConflictResolution resolution = ConflictResolution.Skip;
        
        WpfApplication.Current.Dispatcher.Invoke(() =>
        {
            var fileName = Path.GetFileName(conflictPath);
            var message = $"File or folder already exists in destination:\n{fileName}\n\n" +
                          "How do you want to handle this conflict?\n" +
                          "Note: Your choice will be applied to ALL subsequent conflicts in this operation.\n\n" +
                          "Yes: Overwrite (Apply to All)\n" +
                          "No: Skip (Apply to All)\n" + 
                          "Cancel: Abort Operation";
            
            var result = WpfMessageBox.Show(message, "Conflict Detected", 
                MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            
            resolution = result switch
            {
                MessageBoxResult.Yes => ConflictResolution.Overwrite,
                MessageBoxResult.No => ConflictResolution.Skip,
                MessageBoxResult.Cancel => ConflictResolution.Abort,
                _ => ConflictResolution.Skip
            };

            // Cache the resolution for subsequent conflicts
            _cachedConflictResolution = resolution;
            _logger.LogInformation("Conflict resolution cached: {Resolution} (will apply to all future conflicts)", resolution);
        });
        
        return resolution;
    }

    private void ExecuteSetup()
    {
        AddLogMessage(LogSeverity.Info, "System", "Opening settings dialog");
        // TODO: Open settings dialog
    }

    private void ExecuteOpenDetailView(FileGroupViewModel? group)
    {
        _logger.LogInformation("ExecuteOpenDetailView called for group: {GroupId}", group?.GroupId ?? "null");
        if (group != null)
        {
            DetailPreviewVM.UpdateGroup(group);
            _logger.LogInformation("Detail view updated/opened for group {GroupId}", group.GroupId);
        }
        else 
        {
             _logger.LogWarning("ExecuteOpenDetailView called with null group");
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Adds a file group to the appropriate collection(s).
    /// Includes duplicate detection to prevent adding the same group twice.
    /// </summary>
    public void AddFileGroup(FileGroup fileGroup)
    {
        // GUARD: Check for duplicate before creating ViewModel
        var existingGroup = FileGroups.FirstOrDefault(g => g.GroupId == fileGroup.GroupId);
        if (existingGroup != null)
        {
            _logger.LogWarning("Duplicate AddFileGroup call for {GroupId} - skipping", fileGroup.GroupId);
            AddLogMessage(LogSeverity.Warning, "GroupManager", 
                $"Duplicate group {fileGroup.GroupId} detected - skipped");
            return;
        }

        var config = _configManager.LoadConfiguration<ApplicationConfiguration>();
        var viewModel = new FileGroupViewModel(fileGroup, _imageProcessor, _orchestrator, _abnormalDetector, config, _fileGroupLogger, _uiLog);

        FileGroups.Add(viewModel);

        if (fileGroup.LineNumber == 1)
        {
            Line1Groups.Add(viewModel);
        }
        else if (fileGroup.LineNumber == 2)
        {
            Line2Groups.Add(viewModel);
        }

        // Load thumbnails asynchronously (fire and forget - non-blocking)
        _ = viewModel.LoadThumbnailsAsync();

        UpdateStatistics();
        AddLogMessage(LogSeverity.Info, "GroupManager", $"Added group {fileGroup.GroupId}");
        _logger.LogInformation("Group {GroupId} added to UI collections", fileGroup.GroupId);
    }

    /// <summary>
    /// Removes a file group from all collections.
    /// </summary>
    public void RemoveFileGroup(string groupId)
    {
        var group = FileGroups.FirstOrDefault(g => g.GroupId == groupId);
        if (group == null) return;

        FileGroups.Remove(group);
        Line1Groups.Remove(group);
        Line2Groups.Remove(group);

        UpdateStatistics();
        AddLogMessage(LogSeverity.Info, "GroupManager", $"Removed group {groupId}");
    }

    /// <summary>
    /// Clears all file groups.
    /// </summary>
    public void ClearFileGroups()
    {
        FileGroups.Clear();
        Line1Groups.Clear();
        Line2Groups.Clear();
        UpdateStatistics();
        AddLogMessage(LogSeverity.Info, "GroupManager", "Cleared all groups");
    }

    /// <summary>
    /// Reloads NIR graph thumbnails for all file groups with updated configuration.
    /// Called when settings are applied to immediately reflect size changes.
    /// </summary>
    public void ReloadNirGraphThumbnails()
    {
        _logger.LogInformation("Reloading NIR graph thumbnails for all file groups");

        var config = _configManager.LoadConfiguration<ApplicationConfiguration>();

        // Reload for all file groups
        foreach (var viewModel in FileGroups)
        {
            _ = ReloadSingleNirGraphAsync(viewModel, config);
        }
    }

    /// <summary>
    /// Reloads NIR graph thumbnail for a single FileGroupViewModel.
    /// </summary>
    private async Task ReloadSingleNirGraphAsync(FileGroupViewModel viewModel, ApplicationConfiguration config)
    {
        try
        {
            // Reload just the NIR graph portion with updated configuration
            await viewModel.ReloadNirGraphAsync(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reload NIR graph for group {GroupId}", viewModel.GroupId);
        }
    }

    /// <summary>
    /// Adds a log message to the log panel.
    /// </summary>
    public void AddLogMessage(LogSeverity severity, string source, string message)
    {
        var logMessage = new LogMessage(severity, source, message);
        LogMessages.Add(logMessage);

        // Keep log size manageable (keep last 1000 messages)
        while (LogMessages.Count > 1000)
        {
            LogMessages.RemoveAt(0);
        }
    }

    /// <summary>
    /// Updates file count statistics.
    /// </summary>
    public void UpdateFileCountStatistics(
        int nirCount, int nir2Count,
        int normalCount, int normal2Count,
        int cam1Count, int cam2Count, int cam3Count,
        int cam4Count, int cam5Count, int cam6Count)
    {
        NirCount = nirCount;
        Nir2Count = nir2Count;
        NormalCount = normalCount;
        Normal2Count = normal2Count;
        Cam1Count = cam1Count;
        Cam2Count = cam2Count;
        Cam3Count = cam3Count;
        Cam4Count = cam4Count;
        Cam5Count = cam5Count;
        Cam6Count = cam6Count;
    }

    /// <summary>
    /// Updates matching statistics for unified mode.
    /// </summary>
    public void UpdateUnifiedMatchingStatistics(int total, int withNir, int withoutNir, int failed)
    {
        UnifiedTotalGroups = total;
        UnifiedWithNir = withNir;
        UnifiedWithoutNir = withoutNir;
        UnifiedFailed = failed;
    }

    /// <summary>
    /// Updates matching statistics for separated mode.
    /// </summary>
    public void UpdateSeparatedMatchingStatistics(
        int line1Total, int line1WithNir, int line1WithoutNir, int line1Failed,
        int line2Total, int line2WithNir, int line2WithoutNir, int line2Failed)
    {
        Line1TotalGroups = line1Total;
        Line1WithNir = line1WithNir;
        Line1WithoutNir = line1WithoutNir;
        Line1Failed = line1Failed;

        Line2TotalGroups = line2Total;
        Line2WithNir = line2WithNir;
        Line2WithoutNir = line2WithoutNir;
        Line2Failed = line2Failed;
    }

    /// <summary>
    /// Updates overall statistics based on current file groups.
    /// </summary>
    private void UpdateStatistics()
    {
        TotalGroups = FileGroups.Count;
        
        var withNir = FileGroups.Count(g => g.HasNir);
        var failed = FileGroups.Count(g => g.Status == GroupStatus.Error);
        var abnormal = FileGroups.Count(g => g.Status == GroupStatus.Abnormal);
        
        Failures = failed;
        MatchRate = TotalGroups > 0 ? (double)withNir / TotalGroups * 100 : 0;

        // Update display properties for statistics bar
        WithNirCount = withNir;
        WithoutNirCount = TotalGroups - withNir - failed - abnormal;
        FailedCount = failed;
        AbnormalCount = abnormal;

        // Update unified statistics
        UpdateUnifiedMatchingStatistics(
            TotalGroups,
            withNir,
            TotalGroups - withNir - failed,
            failed);

        // Update separated statistics
        var line1Total = Line1Groups.Count;
        var line1WithNir = Line1Groups.Count(g => g.HasNir);
        var line1Failed = Line1Groups.Count(g => g.Status == GroupStatus.Error || g.Status == GroupStatus.Abnormal);

        var line2Total = Line2Groups.Count;
        var line2WithNir = Line2Groups.Count(g => g.HasNir);
        var line2Failed = Line2Groups.Count(g => g.Status == GroupStatus.Error || g.Status == GroupStatus.Abnormal);

        UpdateSeparatedMatchingStatistics(
            line1Total, line1WithNir, line1Total - line1WithNir - line1Failed, line1Failed,
            line2Total, line2WithNir, line2Total - line2WithNir - line2Failed, line2Failed);
    }

    /// <summary>
    /// Saves the MoveNir count limit to configuration.
    /// Parses the UI string input and persists to config file.
    /// </summary>
    private async Task SaveMoveNirCountAsync()
    {
        try
        {
            var config = await _configManager.LoadConfigurationAsync<ApplicationConfiguration>();
            
            if (int.TryParse(MoveNir, out int count))
            {
                config.MatchingSettings.MoveNir = count;
                _logger.LogDebug("MoveNir set to: {Count}", count);
            }
            else if (string.IsNullOrWhiteSpace(MoveNir))
            {
                config.MatchingSettings.MoveNir = null; // null = all
                _logger.LogDebug("MoveNir set to null (all groups)");
            }
            else
            {
                // Invalid input - don't save
                _logger.LogWarning("Invalid MoveNir input: {Input}. Must be a number or empty.", MoveNir);
                return;
            }
            
            await _configManager.SaveConfigurationAsync(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save MoveNir configuration");
        }
    }

    /// <summary>
    /// Saves the MoveAllData count limit to configuration.
    /// Parses the UI string input and persists to config file.
    /// </summary>
    private async Task SaveMoveAllDataCountAsync()
    {
        try
        {
            var config = await _configManager.LoadConfigurationAsync<ApplicationConfiguration>();
            
            if (int.TryParse(MoveAllData, out int count))
            {
                config.MatchingSettings.MoveAllData = count;
                _logger.LogDebug("MoveAllData set to: {Count}", count);
            }
            else if (string.IsNullOrWhiteSpace(MoveAllData))
            {
                config.MatchingSettings.MoveAllData = null; // null = all
                _logger.LogDebug("MoveAllData set to null (all groups)");
            }
            else
            {
                // Invalid input - don't save
                _logger.LogWarning("Invalid MoveAllData input: {Input}. Must be a number or empty.", MoveAllData);
                return;
            }
            
            await _configManager.SaveConfigurationAsync(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save MoveAllData configuration");
        }
    }

    #endregion

    #region CancellationToken and Progress Helpers

    /// <summary>
    /// Begins a new operation and returns a CancellationToken linked to the window's lifetime.
    /// This cancels any previous operation that was in progress.
    /// </summary>
    private CancellationToken BeginOperation()
    {
        _operationCts?.Cancel();
        _operationCts?.Dispose();
        _operationCts = CancellationTokenSource.CreateLinkedTokenSource(_windowCts.Token);
        IsOperationInProgress = true;
        ProgressValue = 0;
        return _operationCts.Token;
    }

    /// <summary>
    /// Ends the current operation and resets progress state.
    /// </summary>
    private void EndOperation()
    {
        IsOperationInProgress = false;
        ProgressValue = 0;
    }

    /// <summary>
    /// Cancels the current operation if one is in progress.
    /// </summary>
    public void CancelCurrentOperation()
    {
        _operationCts?.Cancel();
        AddLogMessage(LogSeverity.Warning, "System", "Operation cancelled by user");
    }

    /// <summary>
    /// Gets the list of selected file groups based on the active tab.
    /// </summary>
    /// <returns>A read-only list of selected FileGroupViewModels.</returns>
    public IReadOnlyList<FileGroupViewModel> GetSelectedGroups()
    {
        var sourceCollection = ActiveTabIndex switch
        {
            0 => Line1Groups,
            1 => Line2Groups,
            2 => Line1Groups.Concat(Line2Groups), // Combined: both lines
            _ => FileGroups
        };
        return sourceCollection.Where(g => g.IsSelected).ToList();
    }

    /// <summary>
    /// Creates a progress reporter for file operations.
    /// Reports progress to the UI thread.
    /// </summary>
    /// <returns>An IProgress instance for OperationProgress.</returns>
    public IProgress<OperationProgress> CreateProgressReporter()
    {
        return new Progress<OperationProgress>(p =>
        {
            WpfApplication.Current?.Dispatcher.Invoke(() =>
            {
                ProgressValue = p.PercentComplete;
                StatusMessage = $"{p.CurrentFile} ({p.ProcessedFiles}/{p.TotalFiles})";
            });
        });
    }

    #endregion
    #region Event Handlers

    /// <summary>
    /// Handles the GroupCreated event from MonitoringOrchestrator.
    /// Marshals to UI thread to add the group to collections.
    /// </summary>
    private void OnGroupCreated(object? sender, FileGroup group)
    {
        _logger.LogDebug("GroupCreated event received for group {GroupId}", group.GroupId);
        
        // Marshal to UI thread for collection updates
        WpfApplication.Current.Dispatcher.InvokeAsync(() =>
        {
            try
            {
                // GUARD: Check for duplicate before creating ViewModel
                var existingGroup = FileGroups.FirstOrDefault(g => g.GroupId == group.GroupId);
                if (existingGroup != null)
                {
                    _logger.LogWarning("Duplicate GroupCreated event for {GroupId} - skipping", group.GroupId);
                    AddLogMessage(LogSeverity.Warning, "GroupManager", 
                        $"Duplicate group {group.GroupId} detected - skipped");
                    return;
                }

                var config = _configManager.LoadConfiguration<ApplicationConfiguration>();
                var viewModel = new FileGroupViewModel(group, _imageProcessor, _orchestrator, _abnormalDetector, config, _fileGroupLogger, _uiLog);

                FileGroups.Add(viewModel);

                if (group.LineNumber == 1)
                {
                    Line1Groups.Add(viewModel);
                }
                else if (group.LineNumber == 2)
                {
                    Line2Groups.Add(viewModel);
                }

                // Initial thumbnail loading - OnGroupUpdated will handle subsequent updates
                _ = viewModel.LoadThumbnailsAsync();

                UpdateStatistics();
                AddLogMessage(LogSeverity.Info, "GroupManager", $"Added group {group.GroupId}");
                _logger.LogInformation("Group {GroupId} added to UI collections", group.GroupId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling GroupCreated event for group {GroupId}", group.GroupId);
                AddLogMessage(LogSeverity.Error, "GroupManager", $"Error adding group {group.GroupId}: {ex.Message}");
            }
        });
    }

    /// <summary>
    /// Handles the GroupUpdated event from MonitoringOrchestrator.
    /// Marshals to UI thread to refresh the group and reload thumbnails.
    /// </summary>
    private void OnGroupUpdated(object? sender, FileGroup group)
    {
        _logger.LogDebug("GroupUpdated event received for group {GroupId}", group.GroupId);
        
        // Marshal to UI thread for collection updates
        WpfApplication.Current.Dispatcher.InvokeAsync(async () =>
        {
            try
            {
                var viewModel = FileGroups.FirstOrDefault(g => g.GroupId == group.GroupId);
                if (viewModel == null)
                {
                    _logger.LogWarning("Group {GroupId} not found in collections for update", group.GroupId);
                    return;
                }

                // ⚡ Delegated to ViewModel: LoadThumbnailsAsync now checks cache internally
                // We don't need to manually set MainImageThumbnail here anymore because
                // LoadThumbnailsAsync (called below) handles both cache lookup and disk loading.

                // Only refresh paths - thumbnails were already loaded on creation
                // LoadThumbnailsAsync checks for null thumbnails, so new images will load automatically
                viewModel.Refresh();
                
                // Trigger thumbnail loading (will check cache -> disk)
                _ = viewModel.LoadThumbnailsAsync();
                
                UpdateStatistics();
                _logger.LogDebug("Group {GroupId} updated and thumbnails reloaded", group.GroupId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling GroupUpdated event for group {GroupId}", group.GroupId);
                AddLogMessage(LogSeverity.Error, "GroupManager", $"Error updating group {group.GroupId}: {ex.Message}");
            }
        });
    }

    /// <summary>
    /// Handles the GroupRemoved event from MonitoringOrchestrator.
    /// Marshals to UI thread to remove the group from collections.
    /// </summary>
    private void OnGroupRemoved(object? sender, string groupId)
    {
        _logger.LogDebug("GroupRemoved event received for group {GroupId}", groupId);
        
        // Marshal to UI thread for collection updates
        WpfApplication.Current.Dispatcher.InvokeAsync(() =>
        {
            try
            {
                var group = FileGroups.FirstOrDefault(g => g.GroupId == groupId);
                if (group == null)
                {
                    _logger.LogWarning("Group {GroupId} not found in collections", groupId);
                    return;
                }

                FileGroups.Remove(group);
                Line1Groups.Remove(group);
                Line2Groups.Remove(group);

                UpdateStatistics();
                AddLogMessage(LogSeverity.Info, "GroupManager", $"Removed group {groupId}");
                _logger.LogInformation("Group {GroupId} removed from UI collections", groupId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling GroupRemoved event for group {GroupId}", groupId);
                AddLogMessage(LogSeverity.Error, "GroupManager", $"Error removing group {groupId}: {ex.Message}");
            }
        });
    }

    /// <summary>
    /// Handles the MonitoringError event from MonitoringOrchestrator.
    /// Marshals to UI thread to display error message.
    /// </summary>
    private void OnMonitoringError(object? sender, string errorMessage)
    {
        _logger.LogError("MonitoringError event received: {ErrorMessage}", errorMessage);
        
        // Marshal to UI thread for UI updates
        WpfApplication.Current.Dispatcher.InvokeAsync(() =>
        {
            try
            {
                AddLogMessage(LogSeverity.Error, "Monitoring", errorMessage);
                StatusMessage = "Error - Check logs";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling MonitoringError event");
            }
        });
    }

    /// <summary>
    /// Handles the FileCountsUpdated event from StatisticsService.
    /// Marshals to UI thread to update file count properties.
    /// </summary>
    private void OnFileCountsUpdated(object? sender, FileCountStatistics stats)
    {
        _logger.LogDebug("FileCountsUpdated event received");
        
        // Marshal to UI thread for property updates
        WpfApplication.Current.Dispatcher.InvokeAsync(() =>
        {
            try
            {
                NirCount = stats.NirCount;
                Nir2Count = stats.Nir2Count;
                NormalCount = stats.NormalCount;
                Normal2Count = stats.Normal2Count;
                Cam1Count = stats.Cam1Count;
                Cam2Count = stats.Cam2Count;
                Cam3Count = stats.Cam3Count;
                Cam4Count = stats.Cam4Count;
                Cam5Count = stats.Cam5Count;
                Cam6Count = stats.Cam6Count;
                
                _logger.LogDebug("File count statistics updated in UI");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling FileCountsUpdated event");
            }
        });
    }

    /// <summary>
    /// Handles the MatchingStatisticsUpdated event from StatisticsService.
    /// Marshals to UI thread to update matching statistics properties.
    /// </summary>
    private void OnMatchingStatisticsUpdated(object? sender, MatchingStatistics stats)
    {
        _logger.LogDebug("MatchingStatisticsUpdated event received");
        
        // Marshal to UI thread for property updates
        WpfApplication.Current.Dispatcher.InvokeAsync(() =>
        {
            try
            {
                // Update unified mode statistics
                UnifiedTotalGroups = stats.TotalGroups;
                UnifiedWithNir = stats.WithNir;
                UnifiedWithoutNir = stats.WithoutNir;
                UnifiedFailed = stats.Failed;

                // Update display properties for statistics bar
                TotalGroups = stats.TotalGroups;
                WithNirCount = stats.WithNir;
                WithoutNirCount = stats.WithoutNir;
                FailedCount = stats.Failed;
                MatchRate = stats.TotalGroups > 0 ? (double)stats.WithNir / stats.TotalGroups * 100 : 0;

                // Update separated mode statistics if available
                if (stats.Line1 != null)
                {
                    Line1TotalGroups = stats.Line1.TotalGroups;
                    Line1WithNir = stats.Line1.WithNir;
                    Line1WithoutNir = stats.Line1.WithoutNir;
                    Line1Failed = stats.Line1.Failed;
                }

                if (stats.Line2 != null)
                {
                    Line2TotalGroups = stats.Line2.TotalGroups;
                    Line2WithNir = stats.Line2.WithNir;
                    Line2WithoutNir = stats.Line2.WithoutNir;
                    Line2Failed = stats.Line2.Failed;
                }
                
                _logger.LogDebug("Matching statistics updated in UI");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling MatchingStatisticsUpdated event");
            }
        });
    }

    #endregion

    #region IDisposable Implementation

    private bool _disposed = false;

    /// <summary>
    /// Disposes the ViewModel and unsubscribes from all events.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            // Cancel all pending operations
            _windowCts.Cancel();
            _operationCts?.Cancel();

            // Unsubscribe from orchestrator events
            _orchestrator.GroupCreated -= OnGroupCreated;
            _orchestrator.GroupRemoved -= OnGroupRemoved;
            _orchestrator.MonitoringError -= OnMonitoringError;

            // Unsubscribe from statistics service events
            _statisticsService.FileCountsUpdated -= OnFileCountsUpdated;
            _statisticsService.MatchingStatisticsUpdated -= OnMatchingStatisticsUpdated;

            // Unsubscribe from program launcher events
            _generalCameraLauncher.StatusChanged -= OnGeneralCameraStatusChanged;
            _nirCameraLauncher.StatusChanged -= OnNirCameraStatusChanged;
            _generalCameraLauncher.Dispose();
            _nirCameraLauncher.Dispose();

            // Dispose CancellationTokenSources
            _windowCts.Dispose();
            _operationCts?.Dispose();

            _logger.LogInformation("MainWindowViewModel disposed and all event subscriptions cleaned up");
        }

        _disposed = true;
    }

    #endregion

    #region Program Status Tracking

    /// <summary>
    /// General Camera status text
    /// </summary>
    public string GeneralCameraStatus
    {
        get => _generalCameraStatus;
        set => SetProperty(ref _generalCameraStatus, value);
    }

    /// <summary>
    /// General Camera status color
    /// </summary>
    public System.Windows.Media.Brush GeneralCameraForeground
    {
        get => _generalCameraForeground;
        set => SetProperty(ref _generalCameraForeground, value);
    }

    /// <summary>
    /// NIR Camera status text
    /// </summary>
    public string NirCameraStatus
    {
        get => _nirCameraStatus;
        set => SetProperty(ref _nirCameraStatus, value);
    }

    /// <summary>
    /// NIR Camera status color
    /// </summary>
    public System.Windows.Media.Brush NirCameraForeground
    {
        get => _nirCameraForeground;
        set => SetProperty(ref _nirCameraForeground, value);
    }
    
    /// <summary>
    /// NIR2 filtering activation status ("Activated" or "Deactivated").
    /// </summary>
    public string Nir2FilteringStatus
    {
        get => _nir2FilteringStatus;
        set => SetProperty(ref _nir2FilteringStatus, value);
    }

    /// <summary>
    /// NIR2 filtering status indicator color (green when active, red when inactive).
    /// </summary>
    public System.Windows.Media.Brush Nir2FilteringForeground
    {
        get => _nir2FilteringForeground;
        set => SetProperty(ref _nir2FilteringForeground, value);
    }

    /// <summary>
    /// NIR2 filtering toggle button text ("ON" when inactive, "OFF" when active).
    /// </summary>
    public string Nir2FilteringButtonText
    {
        get => _nir2FilteringButtonText;
        set => SetProperty(ref _nir2FilteringButtonText, value);
    }

    /// <summary>
    /// NIR2 filtering toggle button background color.
    /// </summary>
    public System.Windows.Media.Brush Nir2FilteringButtonBackground
    {
        get => _nir2FilteringButtonBackground;
        set => SetProperty(ref _nir2FilteringButtonBackground, value);
    }

    private void OnGeneralCameraStatusChanged(object? sender, bool isActive)
    {
        UpdateProgramStatus();
    }

    private void OnNirCameraStatusChanged(object? sender, bool isActive)
    {
        WpfApplication.Current.Dispatcher.Invoke(() =>
        {
            NirCameraStatus = isActive ? "Activated" : "Deactivated";
            NirCameraForeground = isActive 
                ? new SolidColorBrush(Colors.Green) 
                : new SolidColorBrush(Colors.Red);
        });
    }

    private void OnNir2FilteringStatusChanged(object? sender, bool isActive)
    {
        WpfApplication.Current.Dispatcher.Invoke(() =>
        {
            Nir2FilteringStatus = isActive ? "Activated" : "Deactivated";
            Nir2FilteringForeground = isActive 
                ? new SolidColorBrush(Colors.Green) 
                : new SolidColorBrush(Colors.Red);
            
            // Button shows opposite action (what clicking will do)
            Nir2FilteringButtonText = isActive ? "OFF" : "ON";
            Nir2FilteringButtonBackground = isActive
                ? new SolidColorBrush(Colors.Red)    // Red when active (to turn OFF)
                : new SolidColorBrush(Colors.Green); // Green when inactive (to turn ON)
        });
    }

    private async void ExecuteToggleNir2Filtering()
    {
        try
        {
            if (_nir2CameraLauncher.IsFilteringActive)
            {
                _nir2CameraLauncher.StopFiltering();
                AddLogMessage(LogSeverity.Info, "NIR2", "필터링 중지됨");
            }
            else
            {
                var result = await _nir2CameraLauncher.StartFilteringAsync();
                if (result.Success)
                {
                    AddLogMessage(LogSeverity.Info, "NIR2", "필터링 시작됨");
                }
                else
                {
                    AddLogMessage(LogSeverity.Error, "NIR2", $"필터링 시작 실패: {result.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle NIR2 filtering");
            AddLogMessage(LogSeverity.Error, "NIR2", $"오류: {ex.Message}");
        }
    }
    private void UpdateProgramStatus()
    {
        // 색상 정의
        var greenBrush = new SolidColorBrush(Colors.Green);
        var redBrush = new SolidColorBrush(Colors.Red);
        
        // 상태 확인
        var nir1Active = _nirCameraLauncher.IsActive;  // NIR1만 체크
        var generalActive = _generalCameraLauncher.IsActive;
        
        // NIR Camera 상태 업데이트 (NIR1만)
        if (nir1Active)
        {
            NirCameraStatus = "Activated";
            NirCameraForeground = greenBrush;
        }
        else
        {
            NirCameraStatus = "Deactivated";
            NirCameraForeground = redBrush;
        }
        
        // General Camera 상태 업데이트
        if (generalActive)
        {
            GeneralCameraStatus = "Activated";
            GeneralCameraForeground = greenBrush;
        }
        else
        {
            GeneralCameraStatus = "Deactivated";
            GeneralCameraForeground = redBrush;
        }
    }

    /// <summary>
    /// Selects all items in the currently active tab.
    /// </summary>
    private void ExecuteSelectAll()
    {
        var targetCollection = ActiveTabIndex == 0 ? Line1Groups : Line2Groups;
        
        if (targetCollection == null)
            return;

        foreach (var group in targetCollection)
        {
            group.IsSelected = true;
        }
        
        _logger.LogInformation("Selected all {Count} groups in {Tab}", targetCollection.Count, ActiveTabIndex == 0 ? "Line 1" : "Line 2");
    }

    /// <summary>
    /// Deselects all items in the currently active tab.
    /// </summary>
    private void ExecuteDeselectAll()
    {
        var targetCollection = ActiveTabIndex == 0 ? Line1Groups : Line2Groups;
        
        if (targetCollection == null)
            return;

        foreach (var group in targetCollection)
        {
            group.IsSelected = false;
        }
        
        _logger.LogInformation("Deselected all groups in {Tab}", ActiveTabIndex == 0 ? "Line 1" : "Line 2");
    }

    #endregion
}
