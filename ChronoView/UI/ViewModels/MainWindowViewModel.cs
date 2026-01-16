using ChronoView.Models;
using ChronoView.Core.ImageProcessing;
using ChronoView.Core.Analytics;
using ChronoView.Core.ProgramLaunching;
using ChronoView.Helpers;
using ChronoView.Core.Configuration;
using ChronoView.Core.FileWatching;
using ChronoView.Core.FileOperations;
using ChronoView.UI.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using WpfApplication = System.Windows.Application;

namespace ChronoView.UI.ViewModels;

public class MainWindowViewModel : ViewModelBase, IDisposable
{
    public IDashboardViewModel Dashboard { get; }
    public IFileOperationViewModel Operations { get; }
    public ISystemControlViewModel Control { get; }


    private string _statusMessage = "Ready"; public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
    private double _progressValue; public double ProgressValue { get => _progressValue; set => SetProperty(ref _progressValue, value); }
    public bool IsMonitoring => Control.IsMonitoring;

    // UI Display Properties (Used by MainWindow.xaml.cs)
    private int _displayImageWidth = 120; public int DisplayImageWidth { get => _displayImageWidth; set => SetProperty(ref _displayImageWidth, value); }
    private int _displayImageHeight = 90; public int DisplayImageHeight { get => _displayImageHeight; set => SetProperty(ref _displayImageHeight, value); }
    private int _nirDisplayWidth = 120; public int NirDisplayWidth { get => _nirDisplayWidth; set => SetProperty(ref _nirDisplayWidth, value); }
    private int _nirDisplayHeight = 90; public int NirDisplayHeight { get => _nirDisplayHeight; set => SetProperty(ref _nirDisplayHeight, value); }
    private double _displayFontSize = 10.0; public double DisplayFontSize { get => _displayFontSize; set => SetProperty(ref _displayFontSize, value); }

    // ============================================================
    // Line 1 Sample Move Settings
    // ============================================================
    private string _line1SampleName = ""; 
    public string Line1SampleName 
    { 
        get => _line1SampleName; 
        set 
        { 
            if (SetProperty(ref _line1SampleName, value) && !_isLoadingSettings)
            {
                _ = SaveLineSettingsAsync(1);
            }
        }
    }

    private string _line1MoveNir = ""; 
    public string Line1MoveNir 
    { 
        get => _line1MoveNir; 
        set 
        { 
            if (SetProperty(ref _line1MoveNir, value) && !_isLoadingSettings)
            {
                _ = SaveLineSettingsAsync(1);
            }
        }
    }

    private string _line1MoveAllData = ""; 
    public string Line1MoveAllData 
    { 
        get => _line1MoveAllData; 
        set 
        { 
            if (SetProperty(ref _line1MoveAllData, value) && !_isLoadingSettings)
            {
                _ = SaveLineSettingsAsync(1);
            }
        }
    }

    // ============================================================
    // Line 2 Sample Move Settings
    // ============================================================
    private string _line2SampleName = ""; 
    public string Line2SampleName 
    { 
        get => _line2SampleName; 
        set 
        { 
            if (SetProperty(ref _line2SampleName, value) && !_isLoadingSettings)
            {
                _ = SaveLineSettingsAsync(2);
            }
        }
    }

    private string _line2MoveNir = ""; 
    public string Line2MoveNir 
    { 
        get => _line2MoveNir; 
        set 
        { 
            if (SetProperty(ref _line2MoveNir, value) && !_isLoadingSettings)
            {
                _ = SaveLineSettingsAsync(2);
            }
        }
    }

    private string _line2MoveAllData = ""; 
    public string Line2MoveAllData 
    { 
        get => _line2MoveAllData; 
        set 
        { 
            if (SetProperty(ref _line2MoveAllData, value) && !_isLoadingSettings)
            {
                _ = SaveLineSettingsAsync(2);
            }
        }
    }

    // ============================================================
    // Tab Helper Properties (for dynamic UI)
    // ============================================================
    private int _activeTabIndex; 
    public int ActiveTabIndex 
    { 
        get => _activeTabIndex; 
        set 
        { 
            if (SetProperty(ref _activeTabIndex, value))
            {
                OnPropertyChanged(nameof(IsLine1Tab));
                OnPropertyChanged(nameof(IsLine2Tab));
                OnPropertyChanged(nameof(IsCombinedTab));
                OnPropertyChanged(nameof(SampleMoveSettingsHeader));
            }
        }
    }

    public bool IsLine1Tab => ActiveTabIndex == 0;
    public bool IsLine2Tab => ActiveTabIndex == 1;
    public bool IsCombinedTab => ActiveTabIndex == 2;
    public string SampleMoveSettingsHeader => ActiveTabIndex switch
    {
        0 => "Line 1 설정",
        1 => "Line 2 설정",
        2 => "통합 설정 (Line 1 & 2)",
        _ => "설정"
    };
    
    private double _dataGridRowHeight = 100; public double DataGridRowHeight { get => _dataGridRowHeight; set => SetProperty(ref _dataGridRowHeight, value); }
    private DateTime _currentTime = DateTime.Now; public DateTime CurrentTime { get => _currentTime; set => SetProperty(ref _currentTime, value); }
    
    // Log Panel Visibility
    private const double DefaultLogPanelHeight = 200;
    private const double DefaultLogSplitterHeight = 5;

    private GridLength _logPanelHeight = new GridLength(DefaultLogPanelHeight);
    public GridLength LogPanelHeight
    {
        get => _logPanelHeight;
        set
        {
            if (SetProperty(ref _logPanelHeight, value))
            {
                if (_isLogPanelVisible && value.Value > 0)
                {
                    _lastLogPanelHeight = value;
                }
            }
        }
    }

    private GridLength _logSplitterHeight = new GridLength(DefaultLogSplitterHeight);
    public GridLength LogSplitterHeight { get => _logSplitterHeight; set => SetProperty(ref _logSplitterHeight, value); }

    private GridLength _lastLogPanelHeight = new GridLength(DefaultLogPanelHeight);

    private bool _isLogPanelVisible = true; 
    public bool IsLogPanelVisible
    {
        get => _isLogPanelVisible;
        set
        {
            if (SetProperty(ref _isLogPanelVisible, value))
            {
                UpdateLogPanelLayout();
                OnPropertyChanged(nameof(LogPanelButtonText));
            }
        }
    }
    public string LogPanelButtonText => IsLogPanelVisible ? "📋 로그 ▼" : "📋 로그 ▲";
    public ICommand ToggleLogPanelCommand { get; private set; }
    public ICommand CloseLogPanelCommand { get; private set; }

    public ObservableCollection<LogMessage> LogMessages { get; } = new();

    public ICommand StartCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand MoveCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand SelectAllCommand { get; }
    public ICommand DeselectAllCommand { get; }
    public ICommand OpenImagePreviewCommand { get; }


    public event EventHandler? RequestOpenSettings;

    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly CancellationTokenSource _windowCts = new();
    private readonly IConfigurationManager _configManager;
    private System.Windows.Threading.DispatcherTimer? _saveDebounceTimer;
    private readonly object _saveLock = new object();
    private bool _isLoadingSettings = false;

    public MainWindowViewModel(
        IMonitoringOrchestrator orchestrator, 
        IStatisticsService stats, 
        IFileOperationService fileOp, 
        IConfigurationManager config, 
        IImageProcessor proc, 
        IAbnormalDetector detector, 
        GeneralCameraLauncher gen, 
        NirCameraLauncher nir, 
        Nir2CameraLauncher nir2, 
        NirFilteringService nirFilter,
        IMoveService move, 
        IDeleteService delete, 
        ILogger<MainWindowViewModel> logger, 
        ILoggerFactory factory, 
        ILogger<FileGroupViewModel> fgLogger)
    {
        _logger = logger;
        _configManager = config;
        Dashboard = new DashboardViewModel(orchestrator, stats, proc, detector, config, factory.CreateLogger<DashboardViewModel>(), fgLogger);
        Operations = new FileOperationViewModel(move, delete, config, Dashboard, factory.CreateLogger<FileOperationViewModel>());
        Control = new SystemControlViewModel(orchestrator, stats, config, gen, nir, nir2, nirFilter, factory.CreateLogger<SystemControlViewModel>());

        // Connect Orchestrator logs to the UI log panel
        orchestrator.SetUILog((sev, src, msg) => AddLogMessage(sev, src, msg));

        Dashboard.LogRequested += AddLogMessage;
        Operations.LogRequested += AddLogMessage;
        Control.LogRequested += AddLogMessage;
        Dashboard.StatusChanged += s => StatusMessage = s;
        Operations.StatusChanged += s => StatusMessage = s;
        Control.StatusChanged += s => StatusMessage = s;

        StartCommand = Control.StartCommand;
        StopCommand = Control.StopCommand;
        MoveCommand = new RelayCommand(() => ExecuteMoveWithConfirmation(), () => Operations.MoveCommand.CanExecute(null));
        DeleteCommand = new RelayCommand(() => ExecuteDeleteWithConfirmation(), () => Operations.DeleteCommand.CanExecute(null));
        RefreshCommand = new RelayCommand(async () => {
            await Operations.RefreshDataAsync();
            await Control.RefreshMonitoringAsync();
        });
        SelectAllCommand = new RelayCommand(() => Dashboard.FileGroups.ToList().ForEach(g => g.IsSelected = true));
        DeselectAllCommand = new RelayCommand(() => Dashboard.FileGroups.ToList().ForEach(g => g.IsSelected = false));
        OpenImagePreviewCommand = new RelayCommand<string>(ExecuteOpenImagePreview);

        ToggleLogPanelCommand = new RelayCommand(() => IsLogPanelVisible = !IsLogPanelVisible);
        CloseLogPanelCommand = new RelayCommand(() => IsLogPanelVisible = false);

        UpdateLogPanelLayout();
        
        LoadSettings();
    }

    private void UpdateLogPanelLayout()
    {
        if (_isLogPanelVisible)
        {
            if (_lastLogPanelHeight.Value <= 0)
            {
                _lastLogPanelHeight = new GridLength(DefaultLogPanelHeight);
            }

            LogPanelHeight = _lastLogPanelHeight;
            LogSplitterHeight = new GridLength(DefaultLogSplitterHeight);
        }
        else
        {
            if (LogPanelHeight.Value > 0)
            {
                _lastLogPanelHeight = LogPanelHeight;
            }

            LogPanelHeight = new GridLength(0);
            LogSplitterHeight = new GridLength(0);
        }
    }

    public void LoadSettings()
    {
        _isLoadingSettings = true;
        try
        {
            var appConfig = (WpfApplication.Current as App)?.Services.GetService(typeof(IConfigurationManager)) as IConfigurationManager;
            var config = appConfig?.LoadConfiguration<ApplicationConfiguration>();
            if (config != null)
            {
                // Automatically update paths containing date strings to today's date
                config.UpdatePathsWithCurrentDate();
                
                // Persist the updated configuration so the changes are visible in the config file and survive reloads
                appConfig.SaveConfiguration(config);

                DisplayImageWidth = config.UISettings.DisplayImageWidth;
                DisplayImageHeight = config.UISettings.DisplayImageHeight;
                NirDisplayWidth = config.UISettings.NirThumbnailWidth;
                NirDisplayHeight = config.UISettings.NirThumbnailHeight;
                DisplayFontSize = config.UISettings.DisplayFontSize;
                // Enforce minimum row height based on image height + label/text space + margins
                // Image height + label line (approx 2 lines * 1.5 spacing) + StackPanel margins (10px top/bottom) + extra padding
                var imageHeight = Math.Max(DisplayImageHeight, NirDisplayHeight);
                // Label space: 2 lines of text (Label + Size info) * 1.5 line height * font size + 25px padding
                var labelSpace = (int)(DisplayFontSize * 3.5) + 25; 
                DataGridRowHeight = Math.Max(config.UISettings.DataGridRowHeight, imageHeight + labelSpace);
                // Trigger reloading of media for all existing groups to reflect new settings
                // This ensures NIR graphs are regenerated with new dimensions/options
                if (Dashboard?.FileGroups != null)
                {
                    foreach (var group in Dashboard.FileGroups)
                    {
                        _ = group.ReloadMediaAsync(config);
                    }
                }

                // Line 1 샘플 이동 설정 로드 (setter 트리거 방지)
                _line1SampleName = config.MatchingSettings.Line1Settings.SampleName ?? "";
                _line1MoveNir = config.MatchingSettings.Line1Settings.MoveNir?.ToString() ?? "";
                _line1MoveAllData = config.MatchingSettings.Line1Settings.MoveAllData?.ToString() ?? "";
                OnPropertyChanged(nameof(Line1SampleName));
                OnPropertyChanged(nameof(Line1MoveNir));
                OnPropertyChanged(nameof(Line1MoveAllData));

                // Line 2 샘플 이동 설정 로드
                _line2SampleName = config.MatchingSettings.Line2Settings.SampleName ?? "";
                _line2MoveNir = config.MatchingSettings.Line2Settings.MoveNir?.ToString() ?? "";
                _line2MoveAllData = config.MatchingSettings.Line2Settings.MoveAllData?.ToString() ?? "";
                OnPropertyChanged(nameof(Line2SampleName));
                OnPropertyChanged(nameof(Line2MoveNir));
                OnPropertyChanged(nameof(Line2MoveAllData));
            }
        }
        finally
        {
            _isLoadingSettings = false;
        }

        var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += (s, e) => CurrentTime = DateTime.Now;
        timer.Start();
    }

    private async Task SaveLineSettingsAsync(int lineNumber)
    {
        try
        {
            lock (_saveLock)
            {
                _saveDebounceTimer?.Stop();
                _saveDebounceTimer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(500)
                };
                _saveDebounceTimer.Tick += async (s, e) =>
                {
                    _saveDebounceTimer.Stop();
                    try
                    {
                        var config = await _configManager.LoadConfigurationAsync<ApplicationConfiguration>();
                        
                        // Get the target settings based on line number
                        var settings = lineNumber == 1 
                            ? config.MatchingSettings.Line1Settings 
                            : config.MatchingSettings.Line2Settings;
                        
                        // Get the corresponding ViewModel values
                        var sampleName = lineNumber == 1 ? Line1SampleName : Line2SampleName;
                        var moveNir = lineNumber == 1 ? Line1MoveNir : Line2MoveNir;
                        var moveAllData = lineNumber == 1 ? Line1MoveAllData : Line2MoveAllData;
                        
                        // Save values
                        settings.SampleName = string.IsNullOrWhiteSpace(sampleName) ? null : sampleName;
                        settings.MoveNir = string.IsNullOrWhiteSpace(moveNir) 
                            ? null 
                            : (int.TryParse(moveNir, out int nirValue) ? nirValue : null);
                        settings.MoveAllData = string.IsNullOrWhiteSpace(moveAllData) 
                            ? null 
                            : (int.TryParse(moveAllData, out int allValue) ? allValue : null);
                        
                        await _configManager.SaveConfigurationAsync(config);
                        _logger.LogDebug("Line {Line} settings saved: SampleName={SampleName}, MoveNir={MoveNir}, MoveAllData={MoveAllData}", 
                            lineNumber, sampleName, moveNir, moveAllData);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to save Line {Line} settings", lineNumber);
                    }
                };
                _saveDebounceTimer.Start();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting up save debounce timer for Line {Line}", lineNumber);
        }
    }

    public void ReloadNirGraphThumbnails()
    {
        foreach (var group in Dashboard.FileGroups)
        {
            _ = group.LoadThumbnailsAsync(); // Changed from ReloadThumbnailsAsync
        }
    }

    public void AddLogMessage(LogSeverity severity, string source, string message)
    {
        // 1. Infer line number from source and message
        int? lineNumber = InferLineNumber(source, message);

        // 2. Format message with line prefix (if applicable)
        string displayMessage = lineNumber.HasValue
            ? $"[Line {lineNumber}] {message}"
            : message;

        // 3. Create log message with LineNumber for filtering
        var logMessage = new LogMessage(severity, source, displayMessage)
        {
            LineNumber = lineNumber
        };

        WpfApplication.Current.Dispatcher.Invoke(() => {
            LogMessages.Add(logMessage);
            while (LogMessages.Count > LogPanel.MaxLogMessages) LogMessages.RemoveAt(0);
        });

        // UI 로그 파일 저장 (사용자용)
        WriteToUILogFile(logMessage);
    }

    /// <summary>
    /// Infers production line number from log source and message content.
    /// </summary>
    private static int? InferLineNumber(string source, string message)
    {
        var text = $"{source} {message}".ToLower();

        // Line 2 patterns: nir2, normal2, cam4-6, line 2
        if (Regex.IsMatch(text, @"(nir2|normal2|cam[456]|line\s?2|camera[456])"))
            return 2;

        // Line 1 patterns: nir1, normal1, cam1-3, line 1
        if (Regex.IsMatch(text, @"(nir1|normal1|cam[123]|line\s?1|camera[123])"))
            return 1;

        // System/Common - no specific line
        return null;
    }

    private void ExecuteOpenImagePreview(string? path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;
        try {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(path);
            bitmap.EndInit();
            bitmap.Freeze();

            var win = new ChronoView.UI.Views.ImagePreviewWindow(bitmap, Path.GetFileName(path));
            win.Owner = WpfApplication.Current.MainWindow;
            win.ShowDialog();
        } catch (Exception ex) {
            _logger.LogError(ex, "Failed to open preview for {Path}", path);
        }
    }



    private async void ExecuteMoveWithConfirmation()
    {
        try
        {
            // 1. 실시간 감지 중인지 확인
            if (IsMonitoring)
            {
                var monitoringResult = System.Windows.MessageBox.Show(
                    "실시간 감지 중입니다.\n이동 작업을 진행하시겠습니까?\n\n" +
                    "(예를 선택하면 실시간 감지가 중지됩니다)",
                    "실시간 감지 중",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);

                if (monitoringResult != System.Windows.MessageBoxResult.Yes)
                    return;

                await Control.StopMonitoringAsync();
            }

            // Get output path from config
            var appConfig = (WpfApplication.Current as App)?.Services.GetService(typeof(IConfigurationManager)) as IConfigurationManager;
            var config = appConfig?.LoadConfiguration<ApplicationConfiguration>();
            var outputPath = config?.MatchingSettings.OutputPath ?? "(설정되지 않음)";

            // Tab-based execution
            if (IsCombinedTab)
            {
                await ExecuteCombinedMoveAsync(config, outputPath);
            }
            else
            {
                // Single line execution (Line 1 or Line 2)
                var lineNumber = IsLine1Tab ? 1 : 2;
                var sampleName = IsLine1Tab ? Line1SampleName : Line2SampleName;
                var moveNir = IsLine1Tab ? Line1MoveNir : Line2MoveNir;
                var moveAllData = IsLine1Tab ? Line1MoveAllData : Line2MoveAllData;
                var groups = IsLine1Tab ? Dashboard.Line1Groups : Dashboard.Line2Groups;

                await ExecuteSingleLineMoveAsync(lineNumber, sampleName, moveNir, moveAllData, groups, outputPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ExecuteMoveWithConfirmation");
            System.Windows.MessageBox.Show($"이동 작업 중 오류 발생: {ex.Message}", "오류",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task ExecuteSingleLineMoveAsync(int lineNumber, string sampleName, string moveNir, string moveAllData, 
        IEnumerable<FileGroupViewModel> groups, string outputPath)
    {
        var nirCount = int.TryParse(moveNir, out int n) ? n : 0;
        var allCount = int.TryParse(moveAllData, out int a) ? a : 0;
        var totalGroups = groups.Count();

        // 값이 없는 필드 확인
        bool isNirEmpty = string.IsNullOrWhiteSpace(moveNir);
        bool isAllDataEmpty = string.IsNullOrWhiteSpace(moveAllData);

        // 메시지 구성
        var message = $"[Line {lineNumber}] 다음 데이터를 이동합니다:\n\n";
        message += $"• 샘플명: {(string.IsNullOrWhiteSpace(sampleName) ? "(미지정)" : sampleName)}\n";

        // NIR 파일 정보
        if (isNirEmpty)
            message += $"• NIR 파일: 전체 이동 (값이 입력되지 않음)\n";
        else
            message += $"• NIR 파일: {nirCount}개\n";

        // 전체 데이터 정보
        if (isAllDataEmpty)
            message += $"• 전체 데이터: 전체 이동 (값이 입력되지 않음)\n";
        else
            message += $"• 전체 데이터: {allCount}개\n";

        message += "\n";

        // 경고 메시지 추가
        if (isNirEmpty || isAllDataEmpty)
        {
            message += "⚠️ 경고:\n";
            var emptyFields = new List<string>();
            if (isNirEmpty) emptyFields.Add("NIR 이동");
            if (isAllDataEmpty) emptyFields.Add("전체 데이터 이동");
            message += $"• {string.Join(", ", emptyFields)} 필드에 값이 없습니다.\n";
            message += $"• 값이 없을 경우 해당 항목은 전체가 이동됩니다.\n\n";
        }

        message += $"📁 이동 경로: {outputPath}\n\n";
        message += "진행하시겠습니까?";

        var result = System.Windows.MessageBox.Show(message, "이동 확인", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
        if (result == System.Windows.MessageBoxResult.Yes)
        {
            try
            {
                StatusMessage = "Processing...";
                await Operations.ExecuteMoveAsync(groups.ToList(), moveNir, moveAllData, sampleName);

                StatusMessage = "Refreshing data...";
                await Operations.RefreshDataAsync();
                await Control.RefreshMonitoringAsync();
                StatusMessage = StatusMessage == "Refreshed" ? "Ready" : "Refresh completed with warnings. Check settings.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during move operation or refresh");
                StatusMessage = $"Move failed: {ex.Message}";
                System.Windows.MessageBox.Show($"이동 작업 중 오류 발생: {ex.Message}", "오류",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }

    private async Task ExecuteCombinedMoveAsync(ApplicationConfiguration? config, string outputPath)
    {
        // Combined tab: execute both lines sequentially
        var message = "통합 이동을 수행합니다:\n\n";
        message += $"[Line 1]\n• 샘플명: {(string.IsNullOrWhiteSpace(Line1SampleName) ? "(미지정)" : Line1SampleName)}\n";
        message += $"• NIR: {(string.IsNullOrWhiteSpace(Line1MoveNir) ? "전체" : Line1MoveNir + "개")}\n";
        message += $"• 전체 데이터: {(string.IsNullOrWhiteSpace(Line1MoveAllData) ? "전체" : Line1MoveAllData + "개")}\n\n";
        message += $"[Line 2]\n• 샘플명: {(string.IsNullOrWhiteSpace(Line2SampleName) ? "(미지정)" : Line2SampleName)}\n";
        message += $"• NIR: {(string.IsNullOrWhiteSpace(Line2MoveNir) ? "전체" : Line2MoveNir + "개")}\n";
        message += $"• 전체 데이터: {(string.IsNullOrWhiteSpace(Line2MoveAllData) ? "전체" : Line2MoveAllData + "개")}\n\n";
        message += $"📁 이동 경로: {outputPath}\n\n";
        message += "진행하시겠습니까?";

        var result = System.Windows.MessageBox.Show(message, "통합 이동 확인", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
        if (result == System.Windows.MessageBoxResult.Yes)
        {
            try
            {
                StatusMessage = "Processing Line 1...";
                await Operations.ExecuteMoveAsync(Dashboard.Line1Groups.ToList(), Line1MoveNir, Line1MoveAllData, Line1SampleName);

                StatusMessage = "Processing Line 2...";
                await Operations.ExecuteMoveAsync(Dashboard.Line2Groups.ToList(), Line2MoveNir, Line2MoveAllData, Line2SampleName);

                StatusMessage = "Refreshing data...";
                await Operations.RefreshDataAsync();
                await Control.RefreshMonitoringAsync();
                StatusMessage = StatusMessage == "Refreshed" ? "Ready" : "Refresh completed with warnings.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during combined move operation");
                StatusMessage = $"Move failed: {ex.Message}";
                System.Windows.MessageBox.Show($"통합 이동 작업 중 오류 발생: {ex.Message}", "오류",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }

    private async void ExecuteDeleteWithConfirmation()
    {
        try
        {
            // Get selected groups based on current tab (Row selection gates deletion)
            IEnumerable<FileGroupViewModel> selectedGroups;
            if (IsCombinedTab)
            {
                selectedGroups = Dashboard.FileGroups.Where(g => g.IsSelected);
            }
            else if (IsLine1Tab)
            {
                selectedGroups = Dashboard.Line1Groups.Where(g => g.IsSelected);
            }
            else
            {
                selectedGroups = Dashboard.Line2Groups.Where(g => g.IsSelected);
            }

            var selectedList = selectedGroups.ToList();
            if (selectedList.Count == 0) return;

            // 1. 실시간 감지 중인지 확인
            if (IsMonitoring)
            {
                var monitoringResult = System.Windows.MessageBox.Show(
                    "실시간 감지 중입니다.\n삭제 작업을 진행하시겠습니까?\n\n" +
                    "(예를 선택하면 실시간 감지가 중지됩니다)",
                    "실시간 감지 중",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);

                if (monitoringResult != System.Windows.MessageBoxResult.Yes)
                    return;

                await Control.StopMonitoringAsync();
            }

            // 2. Classification using new computed properties
            var fullDelete = selectedList.Where(g => g.IsFullySelected).ToList();
            var partialDelete = selectedList.Where(g => !g.IsFullySelected && g.HasAnySelectedComponent).ToList();

            // Helper to build category message (Top 3 + summary)
            void AppendCategory(System.Text.StringBuilder sb, string title, List<FileGroupViewModel> items)
            {
                if (items.Count == 0) return;
                sb.AppendLine($"• {title}: {items.Count}개 그룹");
                foreach (var g in items.Take(3))
                {
                    var details = g.GetSelectedComponentDetails();
                    sb.AppendLine($"  - {g.GroupId} ({details.Count}개 항목: {string.Join(", ", details)})");
                }
                if (items.Count > 3)
                    sb.AppendLine($"  ... 외 {items.Count - 3}개 그룹");
                sb.AppendLine();
            }

            var msgBuilder = new System.Text.StringBuilder();
            msgBuilder.AppendLine("다음 데이터를 삭제합니다:\n");

            AppendCategory(msgBuilder, "전체 삭제", fullDelete);
            AppendCategory(msgBuilder, "부분 삭제", partialDelete);

            // Total summary
            var totalGroups = fullDelete.Count + partialDelete.Count;
            var totalItems = fullDelete.Sum(g => g.GetSelectedComponents().Count) + partialDelete.Sum(g => g.GetSelectedComponents().Count);
            msgBuilder.AppendLine($"📊 총 {totalGroups}개 그룹, {totalItems}개 항목");

            // Get quarantine path from config
            var deleteConfig = await _configManager.LoadConfigurationAsync<ApplicationConfiguration>();
            var quarantinePath = !string.IsNullOrEmpty(deleteConfig?.WorkflowSettings?.DeleteQuarantinePath)
                ? deleteConfig.WorkflowSettings.DeleteQuarantinePath
                : Path.Combine(deleteConfig?.BasePath ?? @"D:\Data", "Quarantine");
            msgBuilder.AppendLine($"📁 이동 경로: {quarantinePath}");
            msgBuilder.AppendLine("\n진행하시겠습니까?");

            var message = msgBuilder.ToString();

            var result = System.Windows.MessageBox.Show(message, "삭제 확인", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
            if (result == System.Windows.MessageBoxResult.Yes)
            {
                try
                {
                    StatusMessage = "Processing...";
                    
                    if (IsCombinedTab)
                    {
                        // Combined tab: execute delete for both lines with their respective sample names
                        var line1Selected = selectedList.Where(g => g.LineNumber == 1).ToList();
                        var line2Selected = selectedList.Where(g => g.LineNumber == 2).ToList();
                        
                        if (line1Selected.Any())
                        {
                            await Operations.ExecuteDeleteAsync(line1Selected, Line1SampleName);
                        }
                        if (line2Selected.Any())
                        {
                            await Operations.ExecuteDeleteAsync(line2Selected, Line2SampleName);
                        }
                    }
                    else
                    {
                        // Single line: use the appropriate sample name
                        var sampleName = IsLine1Tab ? Line1SampleName : Line2SampleName;
                        await Operations.ExecuteDeleteAsync(selectedList, sampleName);
                    }

                    StatusMessage = "Refreshing data...";
                    await Operations.RefreshDataAsync();
                    await Control.RefreshMonitoringAsync();
                    StatusMessage = StatusMessage == "Refreshed" ? "Ready" : "Refresh completed with warnings. Check settings.";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during delete operation or refresh");
                    StatusMessage = $"Delete failed: {ex.Message}";
                    System.Windows.MessageBox.Show($"삭제 작업 중 오류 발생: {ex.Message}", "오류",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ExecuteDeleteWithConfirmation");
            System.Windows.MessageBox.Show($"삭제 작업 중 오류 발생: {ex.Message}", "오류",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private static DateTime? _sessionStartTime = null;
    private static string? _uiLogFilePath = null;
    private static readonly object _sessionLock = new object();
    
    private void WriteToUILogFile(LogMessage message)
    {
        try
        {
            // 날짜 폴더 생성
            
            // 세션 시작 시간 기록 및 파일 경로 초기화
            // 로직 설명:
            // 1. _uiLogFilePath가 없으면 초기화
            // 2. _sessionStartTime이 null이면 첫 실행이므로 세션 시작 시간 기록
            // 3. 파일이 존재하지 않으면 새 파일이므로 세션 시작 시간 기록
            // 4. lock을 사용하여 멀티스레드 환경에서 중복 기록 방지
            string targetFile;
            lock (_sessionLock)
            {
                if (_uiLogFilePath == null)
                {
                    _uiLogFilePath = PathHelper.GetSessionLogFilePath("ChronoView_UI");
                }
                targetFile = _uiLogFilePath;

                bool shouldWriteSessionHeader = false;
                
                if (_sessionStartTime == null)
                {
                    // 첫 실행: 세션 시작 시간 설정 (Use Global Session Start Time)
                    _sessionStartTime = PathHelper.SessionStartTime;
                    shouldWriteSessionHeader = true;
                }
                else if (!File.Exists(targetFile))
                {
                    // 파일이 존재하지 않음: 새 날짜 파일이므로 세션 시작 시간 업데이트 (Keep global time or use global time? Use global for consistency in this session)
                    // If the session spans across midnight, the debug log stays in the old date folder. 
                    // To keep strictly matched, we should stick to App.SessionStartTime.
                    _sessionStartTime = PathHelper.SessionStartTime;
                    shouldWriteSessionHeader = true;
                }
                else
                {
                    // 파일이 존재: 크기 확인
                    var fileInfo = new FileInfo(targetFile);
                    if (fileInfo.Length == 0)
                    {
                        // 빈 파일: 세션 시작 시간 기록
                        shouldWriteSessionHeader = true;
                    }
                }
                
                if (shouldWriteSessionHeader && _sessionStartTime.HasValue)
                {
                    var separator = new string('=', 80);
                    var sessionHeader = $"\n{separator}\n" +
                                       $"Session Started: {_sessionStartTime.Value:yyyy-MM-dd HH:mm:ss}\n" +
                                       $"{separator}\n\n";
                    File.AppendAllText(targetFile, sessionHeader);
                }
            }

            var logEntry = $"[{message.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{message.Severity}] [{message.Source}] {message.Message}";

            File.AppendAllText(targetFile, logEntry + Environment.NewLine);
        }
        catch
        {
            // Silently fail if we can't write to log file
        }
    }

    public void Dispose() 
    { 
        _saveDebounceTimer?.Stop();
        _saveDebounceTimer = null;
        _windowCts.Cancel(); 
        Dashboard.Dispose(); 
        Control.Dispose(); 
    }
}
