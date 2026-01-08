using ChronoView.Models;
using ChronoView.Core.ImageProcessing;
using ChronoView.Core.Analytics;
using ChronoView.Core.ProgramLaunching;
using ChronoView.Helpers;
using ChronoView.Core.Configuration;
using ChronoView.Core.FileWatching;
using ChronoView.Core.FileOperations;
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
using Microsoft.Extensions.Logging;
using WpfApplication = System.Windows.Application;

namespace ChronoView.UI.ViewModels;

public class MainWindowViewModel : ViewModelBase, IDisposable
{
    public IDashboardViewModel Dashboard { get; }
    public IFileOperationViewModel Operations { get; }
    public ISystemControlViewModel Control { get; }
    public DetailPreviewViewModel DetailPreviewVM { get; } = new();

    private string _statusMessage = "Ready"; public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
    private double _progressValue; public double ProgressValue { get => _progressValue; set => SetProperty(ref _progressValue, value); }
    public bool IsMonitoring => Control.IsMonitoring;

    // UI Display Properties (Used by MainWindow.xaml.cs)
    private int _displayImageWidth = 120; public int DisplayImageWidth { get => _displayImageWidth; set => SetProperty(ref _displayImageWidth, value); }
    private int _displayImageHeight = 90; public int DisplayImageHeight { get => _displayImageHeight; set => SetProperty(ref _displayImageHeight, value); }
    private int _nirDisplayWidth = 120; public int NirDisplayWidth { get => _nirDisplayWidth; set => SetProperty(ref _nirDisplayWidth, value); }
    private int _nirDisplayHeight = 90; public int NirDisplayHeight { get => _nirDisplayHeight; set => SetProperty(ref _nirDisplayHeight, value); }
    private double _displayFontSize = 10.0; public double DisplayFontSize { get => _displayFontSize; set => SetProperty(ref _displayFontSize, value); }

    private string _sampleName = ""; 
    public string SampleName 
    { 
        get => _sampleName; 
        set 
        { 
            if (SetProperty(ref _sampleName, value) && !_isLoadingSettings)
            {
                _ = SaveSampleMoveSettingsAsync();
            }
        }
    }

    private string _moveNir = ""; 
    public string MoveNir 
    { 
        get => _moveNir; 
        set 
        { 
            if (SetProperty(ref _moveNir, value) && !_isLoadingSettings)
            {
                _ = SaveSampleMoveSettingsAsync();
            }
        }
    }

    private string _moveAllData = ""; 
    public string MoveAllData 
    { 
        get => _moveAllData; 
        set 
        { 
            if (SetProperty(ref _moveAllData, value) && !_isLoadingSettings)
            {
                _ = SaveSampleMoveSettingsAsync();
            }
        }
    }
    private int _activeTabIndex; public int ActiveTabIndex { get => _activeTabIndex; set => SetProperty(ref _activeTabIndex, value); }
    
    private double _dataGridRowHeight = 100; public double DataGridRowHeight { get => _dataGridRowHeight; set => SetProperty(ref _dataGridRowHeight, value); }
    private DateTime _currentTime = DateTime.Now; public DateTime CurrentTime { get => _currentTime; set => SetProperty(ref _currentTime, value); }
    
    // Log Panel Visibility
    private bool _isLogPanelVisible = true; 
    public bool IsLogPanelVisible { get => _isLogPanelVisible; set => SetProperty(ref _isLogPanelVisible, value); }
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
    public ICommand OpenDetailViewCommand { get; }

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
        Control = new SystemControlViewModel(orchestrator, stats, config, gen, nir, nir2, factory.CreateLogger<SystemControlViewModel>());

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
        OpenDetailViewCommand = new RelayCommand<FileGroupViewModel>(ExecuteOpenDetailView);
        ToggleLogPanelCommand = new RelayCommand(() => { IsLogPanelVisible = !IsLogPanelVisible; OnPropertyChanged(nameof(LogPanelButtonText)); });
        CloseLogPanelCommand = new RelayCommand(() => { IsLogPanelVisible = false; OnPropertyChanged(nameof(LogPanelButtonText)); });
        
        LoadSettings();
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

                // 샘플 이동 설정 로드 (setter 트리거 방지)
                _sampleName = config.MatchingSettings.SampleName ?? "";
                _moveNir = config.MatchingSettings.MoveNir?.ToString() ?? "";
                _moveAllData = config.MatchingSettings.MoveAllData?.ToString() ?? "";
                OnPropertyChanged(nameof(SampleName));
                OnPropertyChanged(nameof(MoveNir));
                OnPropertyChanged(nameof(MoveAllData));
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

    private async Task SaveSampleMoveSettingsAsync()
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
                        
                        // SampleName 저장
                        config.MatchingSettings.SampleName = string.IsNullOrWhiteSpace(SampleName) ? null : SampleName;
                        
                        // MoveNir 저장 (빈 값 또는 파싱 실패 시 null)
                        config.MatchingSettings.MoveNir = string.IsNullOrWhiteSpace(MoveNir) 
                            ? null 
                            : (int.TryParse(MoveNir, out int nirValue) ? nirValue : null);
                        
                        // MoveAllData 저장 (빈 값 또는 파싱 실패 시 null)
                        config.MatchingSettings.MoveAllData = string.IsNullOrWhiteSpace(MoveAllData) 
                            ? null 
                            : (int.TryParse(MoveAllData, out int allValue) ? allValue : null);
                        
                        await _configManager.SaveConfigurationAsync(config);
                        _logger.LogDebug("Sample move settings saved: SampleName={SampleName}, MoveNir={MoveNir}, MoveAllData={MoveAllData}", 
                            SampleName, MoveNir, MoveAllData);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to save sample move settings");
                    }
                };
                _saveDebounceTimer.Start();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting up save debounce timer");
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
        var logMessage = new LogMessage(severity, source, message);
        
        WpfApplication.Current.Dispatcher.Invoke(() => {
            LogMessages.Add(logMessage);
            while (LogMessages.Count > 1000) LogMessages.RemoveAt(0);
        });
        
        // UI 로그 파일 저장 (사용자용)
        WriteToUILogFile(logMessage);
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

    private void ExecuteOpenDetailView(FileGroupViewModel? group)
    {
        if (group == null) return;
        _logger.LogInformation("Opening detail view for group {GroupId}", group.GroupId);
        DetailPreviewVM.UpdateGroup(group);
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

            // 2. 기존 이동 확인 로직
            var nirCount = int.TryParse(MoveNir, out int n) ? n : 0;
            var allCount = int.TryParse(MoveAllData, out int a) ? a : 0;
            var totalGroups = Dashboard.FileGroups.Count;

            // 값이 없는 필드 확인
            bool isNirEmpty = string.IsNullOrWhiteSpace(MoveNir);
            bool isAllDataEmpty = string.IsNullOrWhiteSpace(MoveAllData);

            // Get output path from config
            var appConfig = (WpfApplication.Current as App)?.Services.GetService(typeof(IConfigurationManager)) as IConfigurationManager;
            var config = appConfig?.LoadConfiguration<ApplicationConfiguration>();
            var outputPath = config?.MatchingSettings.OutputPath ?? "(설정되지 않음)";

            // 메시지 구성
            var message = $"다음 데이터를 이동합니다:\n\n";

            // NIR 파일 정보
            if (isNirEmpty)
            {
                message += $"• NIR 파일: 전체 이동 (값이 입력되지 않음)\n";
            }
            else
            {
                message += $"• NIR 파일: {nirCount}개\n";
            }

            // 전체 데이터 정보
            if (isAllDataEmpty)
            {
                message += $"• 전체 데이터: 전체 이동 (값이 입력되지 않음)\n";
            }
            else
            {
                message += $"• 전체 데이터: {allCount}개\n";
            }

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
                    await Operations.ExecuteMoveAsync(Dashboard.FileGroups.ToList(), MoveNir, MoveAllData, SampleName);

                    StatusMessage = "Refreshing data...";
                    await Operations.RefreshDataAsync(); // 1. UI 목록 Clear
                    await Control.RefreshMonitoringAsync(); // 2. 통계 리로드 및 1회 재스캔(PerformInitialScanAsync)

                    if (StatusMessage == "Refreshed")
                    {
                        StatusMessage = "Ready";
                    }
                    else
                    {
                        // PerformInitialScanAsync 실패 시 (예: DataSequenceSettings 미설정) 알림 보강
                        _logger.LogWarning("Refresh may be incomplete due to configuration issues.");
                        StatusMessage = "Refresh completed with warnings. Check settings.";
                    }
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ExecuteMoveWithConfirmation");
            System.Windows.MessageBox.Show($"이동 작업 중 오류 발생: {ex.Message}", "오류",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private async void ExecuteDeleteWithConfirmation()
    {
        try
        {
            var selectedGroups = Dashboard.FileGroups.Where(g => g.IsSelected || g.IsAnyPartialSelected).ToList();
            if (selectedGroups.Count == 0) return;

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

            // 2. 기존 삭제 확인 로직
            var fullDelete = selectedGroups.Where(g => g.IsSelected).ToList();
            var partialDelete = selectedGroups.Where(g => g.IsAnyPartialSelected && !g.IsSelected).ToList();

            var message = "다음 데이터를 삭제합니다:\n\n";

            if (fullDelete.Count > 0)
            {
                message += $"• 전체 삭제: {fullDelete.Count}개 그룹\n";
                foreach (var g in fullDelete.Take(5))
                    message += $"  - {g.GroupId}\n";
                if (fullDelete.Count > 5)
                    message += $"  ... 외 {fullDelete.Count - 5}개\n";
            }

            if (partialDelete.Count > 0)
            {
                message += $"• 부분 삭제: {partialDelete.Count}개 그룹\n";
            }

            message += "\n📁 지정된 삭제 폴더로 이동됩니다.\n\n진행하시겠습니까?";

            var result = System.Windows.MessageBox.Show(message, "삭제 확인", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
            if (result == System.Windows.MessageBoxResult.Yes)
            {
                try
                {
                    StatusMessage = "Processing...";
                    await Operations.ExecuteDeleteAsync(selectedGroups, SampleName);

                    StatusMessage = "Refreshing data...";
                    await Operations.RefreshDataAsync(); // 1. UI 목록 Clear
                    await Control.RefreshMonitoringAsync(); // 2. 통계 리로드 및 1회 재스캔(PerformInitialScanAsync)

                    if (StatusMessage == "Refreshed")
                    {
                        StatusMessage = "Ready";
                    }
                    else
                    {
                        // PerformInitialScanAsync 실패 시 (예: DataSequenceSettings 미설정) 알림 보강
                        _logger.LogWarning("Refresh may be incomplete due to configuration issues.");
                        StatusMessage = "Refresh completed with warnings. Check settings.";
                    }
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
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ChronoView",
                "Logs");

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
                    var startTime = DateTime.Now;
                    var dateFolder = Path.Combine(logDir, startTime.ToString("yyyyMMdd"));
                    Directory.CreateDirectory(dateFolder);
                    _uiLogFilePath = Path.Combine(dateFolder, $"ChronoView_UI_{startTime:yyyyMMdd_HHmmss}.log");
                }
                targetFile = _uiLogFilePath;

                bool shouldWriteSessionHeader = false;
                
                if (_sessionStartTime == null)
                {
                    // 첫 실행: 세션 시작 시간 설정
                    _sessionStartTime = DateTime.Now;
                    shouldWriteSessionHeader = true;
                }
                else if (!File.Exists(targetFile))
                {
                    // 파일이 존재하지 않음: 새 날짜 파일이므로 세션 시작 시간 업데이트
                    _sessionStartTime = DateTime.Now;
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
