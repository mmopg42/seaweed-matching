using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using System.Windows;
using ChronoView.Models;
using ChronoView.Core.FileWatching;
using ChronoView.Core.Analytics;
using ChronoView.Core.Configuration;
using ChronoView.Core.FileOperations;
using ChronoView.Core.ImageProcessing;
using Microsoft.Extensions.Logging;

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
    private readonly ILogger<MainWindowViewModel> _logger;

    private FileGroupViewModel? _selectedGroup;
    private bool _isMonitoring;
    private bool _isSeparatedMode;
    private int _totalGroups;
    private double _matchRate;
    private int _failures;
    private string _statusMessage = "Ready";
    private string _nirConnectionStatus = "OK";
    private DateTime _currentTime = DateTime.Now;
    
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
    private int _dataGridRowHeight = 100;

    public MainWindowViewModel(
        IMonitoringOrchestrator orchestrator,
        IStatisticsService statisticsService,
        IConfigurationManager configManager,
        IFileOperationService fileOperationService,
        IPathManagementService pathManagementService,
        IImageProcessor imageProcessor,
        ILogger<MainWindowViewModel> logger)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _statisticsService = statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));
        _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
        _fileOperationService = fileOperationService ?? throw new ArgumentNullException(nameof(fileOperationService));
        _pathManagementService = pathManagementService ?? throw new ArgumentNullException(nameof(pathManagementService));
        _imageProcessor = imageProcessor ?? throw new ArgumentNullException(nameof(imageProcessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Initialize collections
        FileGroups = new ObservableCollection<FileGroupViewModel>();
        Line1Groups = new ObservableCollection<FileGroupViewModel>();
        Line2Groups = new ObservableCollection<FileGroupViewModel>();
        LogMessages = new ObservableCollection<LogMessage>();

        // Initialize commands
        StartCommand = new RelayCommand(ExecuteStart, CanExecuteStart);
        StopCommand = new RelayCommand(ExecuteStop, CanExecuteStop);
        MoveCommand = new RelayCommand(ExecuteMove, CanExecuteMove);
        DeleteCommand = new RelayCommand(ExecuteDelete, CanExecuteDelete);
        RefreshCommand = new RelayCommand(ExecuteRefresh);
        PathAutoConfigCommand = new RelayCommand(ExecutePathAutoConfig);
        CreateSampleFolderCommand = new RelayCommand(ExecuteCreateSampleFolder);
        SetupCommand = new RelayCommand(ExecuteSetup);

        // Subscribe to orchestrator events
        _orchestrator.GroupCreated += OnGroupCreated;
        _orchestrator.GroupRemoved += OnGroupRemoved;
        _orchestrator.MonitoringError += OnMonitoringError;

        // Subscribe to statistics service events
        _statisticsService.FileCountsUpdated += OnFileCountsUpdated;
        _statisticsService.MatchingStatisticsUpdated += OnMatchingStatisticsUpdated;

        // Add initial log message
        AddLogMessage(LogSeverity.Info, "System", "Application started");
        _logger.LogInformation("MainWindowViewModel initialized with all service dependencies and event subscriptions");
    }

    #region Properties

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
    /// Currently selected file group.
    /// </summary>
    public FileGroupViewModel? SelectedGroup
    {
        get => _selectedGroup;
        set
        {
            if (SetProperty(ref _selectedGroup, value))
            {
                ((RelayCommand)MoveCommand).RaiseCanExecuteChanged();
                ((RelayCommand)DeleteCommand).RaiseCanExecuteChanged();
            }
        }
    }

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
    /// Move NIR configuration.
    /// </summary>
    public string MoveNir
    {
        get => _moveNir;
        set => SetProperty(ref _moveNir, value);
    }

    /// <summary>
    /// Move all data configuration.
    /// </summary>
    public string MoveAllData
    {
        get => _moveAllData;
        set => SetProperty(ref _moveAllData, value);
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

    #endregion

    #region Commands

    public ICommand StartCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand MoveCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand PathAutoConfigCommand { get; }
    public ICommand CreateSampleFolderCommand { get; }
    public ICommand SetupCommand { get; }

    #endregion

    #region Command Implementations

    private bool CanExecuteStart()
    {
        return !IsMonitoring;
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

            // Validate configuration paths exist
            var missingPaths = new List<string>();
            if (!string.IsNullOrEmpty(config.MatchingSettings.NirPath) && !Directory.Exists(config.MatchingSettings.NirPath))
                missingPaths.Add($"NIR: {config.MatchingSettings.NirPath}");
            if (!string.IsNullOrEmpty(config.MatchingSettings.NormalPath) && !Directory.Exists(config.MatchingSettings.NormalPath))
                missingPaths.Add($"Normal: {config.MatchingSettings.NormalPath}");

            if (missingPaths.Count > 0)
            {
                var errorMsg = $"Configuration paths do not exist:\n{string.Join("\n", missingPaths)}\n\nPlease configure paths in Settings.";
                _logger.LogWarning("Cannot start monitoring - invalid paths: {Paths}", string.Join(", ", missingPaths));
                AddLogMessage(LogSeverity.Warning, "System", errorMsg);
                
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show(errorMsg, "Configuration Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                });
                
                StatusMessage = "Configuration required";
                return;
            }

            // Call MonitoringOrchestrator.StartAsync(config)
            await _orchestrator.StartAsync(config);

            // Call StatisticsService.StartMonitoringAsync(config)
            await _statisticsService.StartMonitoringAsync(config);

            // Load UI display settings from configuration
            DisplayImageWidth = config.UISettings.DisplayImageWidth;
            DisplayImageHeight = config.UISettings.DisplayImageHeight;
            DataGridRowHeight = config.UISettings.DataGridRowHeight;

            // Set IsMonitoring = true on success
            IsMonitoring = true;

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
            
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                MessageBox.Show($"Failed to start monitoring:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            });
            
            // Ensure IsMonitoring remains false on error
            IsMonitoring = false;
            StatusMessage = "Error - Check logs";
        }
    }

    private bool CanExecuteStop()
    {
        return IsMonitoring;
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
        return SelectedGroup != null;
    }

    private void ExecuteMove()
    {
        if (SelectedGroup == null) return;
        
        AddLogMessage(LogSeverity.Info, "FileOperation", $"Moving group {SelectedGroup.GroupId}");
        // TODO: Implement move operation
    }

    private bool CanExecuteDelete()
    {
        return SelectedGroup != null;
    }

    private void ExecuteDelete()
    {
        if (SelectedGroup == null) return;
        
        AddLogMessage(LogSeverity.Warning, "FileOperation", $"Deleting group {SelectedGroup.GroupId}");
        // TODO: Implement delete operation
    }

    private void ExecuteRefresh()
    {
        AddLogMessage(LogSeverity.Info, "System", "Refreshing data");
        // TODO: Implement refresh logic
    }

    private void ExecutePathAutoConfig()
    {
        AddLogMessage(LogSeverity.Info, "Configuration", "Auto-configuring paths");
        // TODO: Implement path auto-configuration
    }

    private void ExecuteCreateSampleFolder()
    {
        AddLogMessage(LogSeverity.Info, "FileOperation", "Creating sample folder");
        // TODO: Implement sample folder creation
    }

    private void ExecuteSetup()
    {
        AddLogMessage(LogSeverity.Info, "System", "Opening settings dialog");
        // TODO: Open settings dialog
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Adds a file group to the appropriate collection(s).
    /// </summary>
    public void AddFileGroup(FileGroup fileGroup)
    {
        var viewModel = new FileGroupViewModel(fileGroup, _imageProcessor);
        
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
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            try
            {
                var viewModel = new FileGroupViewModel(group, _imageProcessor);
                
                FileGroups.Add(viewModel);
                
                if (group.LineNumber == 1)
                {
                    Line1Groups.Add(viewModel);
                }
                else if (group.LineNumber == 2)
                {
                    Line2Groups.Add(viewModel);
                }

                // Load thumbnails asynchronously (fire and forget - non-blocking)
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
    /// Handles the GroupRemoved event from MonitoringOrchestrator.
    /// Marshals to UI thread to remove the group from collections.
    /// </summary>
    private void OnGroupRemoved(object? sender, string groupId)
    {
        _logger.LogDebug("GroupRemoved event received for group {GroupId}", groupId);
        
        // Marshal to UI thread for collection updates
        Application.Current.Dispatcher.InvokeAsync(() =>
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
        Application.Current.Dispatcher.InvokeAsync(() =>
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
        Application.Current.Dispatcher.InvokeAsync(() =>
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
        Application.Current.Dispatcher.InvokeAsync(() =>
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
            // Unsubscribe from orchestrator events
            _orchestrator.GroupCreated -= OnGroupCreated;
            _orchestrator.GroupRemoved -= OnGroupRemoved;
            _orchestrator.MonitoringError -= OnMonitoringError;

            // Unsubscribe from statistics service events
            _statisticsService.FileCountsUpdated -= OnFileCountsUpdated;
            _statisticsService.MatchingStatisticsUpdated -= OnMatchingStatisticsUpdated;

            _logger.LogInformation("MainWindowViewModel disposed and all event subscriptions cleaned up");
        }

        _disposed = true;
    }

    #endregion
}
