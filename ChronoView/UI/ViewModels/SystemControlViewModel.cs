using ChronoView.Core.Configuration;
using ChronoView.Core.Localization;
using ChronoView.Core.FileWatching;
using ChronoView.Core.Analytics;
using ChronoView.Core.ProgramLaunching;
using ChronoView.Core.NIR.Line2;
using ChronoView.Models;
using ChronoView.Helpers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Input;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes; // Also alias Brushes to be safe/explicit

namespace ChronoView.UI.ViewModels;

public class SystemControlViewModel : ViewModelBase, ISystemControlViewModel
{
    private readonly IMonitoringOrchestrator _orchestrator;
    private readonly IStatisticsService _statsService;
    private readonly IConfigurationManager _configManager;
    private readonly GeneralCameraLauncher _generalCameraLauncher;
    private readonly NirCameraLauncher _nirCameraLauncher;
    private readonly NirFilteringService _nirFilteringService;
    private readonly Nir2DataCollector _nir2DataCollector;
    private readonly ApiBasedNirProvider _nir2DataProvider;
    private readonly ILogger<SystemControlViewModel> _logger;

    public event Action<LogSeverity, string, string>? LogRequested;
    public event Action<string>? StatusChanged;

    private bool _isMonitoring; public bool IsMonitoring { get => _isMonitoring; private set { if (SetProperty(ref _isMonitoring, value)) { (StartCommand as RelayCommand)?.RaiseCanExecuteChanged(); (StopCommand as RelayCommand)?.RaiseCanExecuteChanged(); } } }
    
    private CameraState _genCamState = CameraState.Stopped; public CameraState GeneralCameraState { get => _genCamState; private set => SetProperty(ref _genCamState, value); }
    
    private CameraState _nirCamState = CameraState.Stopped; public CameraState NirCameraState { get => _nirCamState; private set => SetProperty(ref _nirCamState, value); }
    

    private CameraState _nir2FiltState = CameraState.Stopped; public CameraState Nir2FilteringState { get => _nir2FiltState; private set => SetProperty(ref _nir2FiltState, value); }

    private CameraState _nir2ParsingState = CameraState.Stopped; public CameraState Nir2ParsingState { get => _nir2ParsingState; private set => SetProperty(ref _nir2ParsingState, value); }

    public ICommand StartCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand ToggleNir2FilteringCommand { get; }
    public ICommand ToggleNir2ParsingCommand { get; }
    public ICommand LaunchGeneralCameraCommand { get; }
    public ICommand LaunchNir1CameraCommand { get; }

    public SystemControlViewModel(IMonitoringOrchestrator orchestrator, IStatisticsService statsService, IConfigurationManager configManager, GeneralCameraLauncher genCam, NirCameraLauncher nirCam, NirFilteringService nirFilter, Nir2DataCollector nir2Collector, ApiBasedNirProvider nir2Provider, ILogger<SystemControlViewModel> logger)
    {
        _orchestrator = orchestrator; _statsService = statsService; _configManager = configManager; _generalCameraLauncher = genCam; _nirCameraLauncher = nirCam; _nirFilteringService = nirFilter; _nir2DataCollector = nir2Collector; _nir2DataProvider = nir2Provider; _logger = logger;

        StartCommand = new RelayCommand(() => _ = StartMonitoringAsync(), () => !IsMonitoring);
        StopCommand = new RelayCommand(() => _ = StopMonitoringAsync(), () => IsMonitoring);
        ToggleNir2FilteringCommand = new RelayCommand(() => ExecuteToggleNir2Filtering());
        ToggleNir2ParsingCommand = new RelayCommand(() => ExecuteToggleNir2Parsing());
        LaunchGeneralCameraCommand = new RelayCommand(async () => await ExecuteLaunchAsync(_generalCameraLauncher, "General Camera", s => GeneralCameraState = s, () => GeneralCameraState));
        LaunchNir1CameraCommand = new RelayCommand(async () => await ExecuteLaunchAsync(_nirCameraLauncher, "NIR Camera 1", s => NirCameraState = s, () => NirCameraState));

        _generalCameraLauncher.StatusChanged += (s, active) => UpdateCameraStatus(active, () => GeneralCameraState, s => GeneralCameraState = s);
        _nirCameraLauncher.StatusChanged += (s, active) => UpdateCameraStatus(active, () => NirCameraState, s => NirCameraState = s);

        Nir2FilteringState = _nirFilteringService.IsFilteringActive ? CameraState.Running : CameraState.Stopped;
        _nirFilteringService.StatusChanged += (s, active) =>
        {
            UpdateCameraStatus(active, () => Nir2FilteringState, s => Nir2FilteringState = s);
            var stateText = active ? "Activated" : "Deactivated";
            var message = LocalizationManager.GetString("Log_Info_NirFiltering_Toggled", stateText);
            LogRequested?.Invoke(LogSeverity.Info, "System", message);
        };

        // Wire up NIR2 data collector events
        _nir2DataCollector.StateChanged += OnNir2CollectorStateChanged;
        _nir2DataCollector.ChunkDetected += OnNir2ChunkDetected;

        // Initialize NIR2 parsing state from collector
        UpdateNir2ParsingState(_nir2DataCollector.State);

        // Initialize launcher status
        _ = InitializeLaunchersAsync();
    }

    public async Task StartMonitoringAsync()
    {
        try {
            IsMonitoring = true;
            var message = LocalizationManager.GetString("Log_Info_Monitoring_Starting");
            LogRequested?.Invoke(LogSeverity.Info, "System", message);
            var config = await _configManager.LoadConfigurationAsync<ApplicationConfiguration>();
            
            await _statsService.StartMonitoringAsync(config);
            await _orchestrator.StartAsync(config);
            
            _logger.LogInformation("Diagnostic: Monitoring started.");
            StatusChanged?.Invoke("Monitoring...");
        } catch (Exception ex) {
            IsMonitoring = false;
            var errorMessage = LocalizationManager.GetString("Log_Error_StartFailed", ex.Message);
            LogRequested?.Invoke(LogSeverity.Error, "System", errorMessage);
        }
    }

    public async Task StopMonitoringAsync()
    {
        await _orchestrator.StopAsync();
        await _statsService.StopMonitoringAsync();
        IsMonitoring = false;
        StatusChanged?.Invoke("Stopped");
    }

    public async Task RefreshMonitoringAsync()
    {
        var refreshMessage = LocalizationManager.GetString("Log_Info_Monitoring_Refreshing");
        LogRequested?.Invoke(LogSeverity.Info, "System", refreshMessage);
        var config = await _configManager.LoadConfigurationAsync<ApplicationConfiguration>();
        
        // Reload file statistics
        await _statsService.ReloadStatsAsync(config);

        // Sync external program status
        await InitializeLaunchersAsync();

        // Always use RefreshAsync - it syncs data and STOPS monitoring
        await _orchestrator.RefreshAsync();
        
        // Sync ViewModel state (Refresh always stops monitoring)
        IsMonitoring = false;
        
        StatusChanged?.Invoke("Refreshed");
    }

    private async void ExecuteToggleNir2Filtering()
    {
        if (_nirFilteringService.IsFilteringActive)
        {
            Nir2FilteringState = CameraState.Stopping;
            _nirFilteringService.StopFiltering();
            if (Nir2FilteringState == CameraState.Stopping)
                Nir2FilteringState = CameraState.Stopped;
        }
        else
        {
            Nir2FilteringState = CameraState.Starting;
            (bool success, string message) = await _nirFilteringService.StartFilteringAsync();
            if (success)
            {
               Nir2FilteringState = CameraState.Running;
            }
            else
            {
                var failMessage = LocalizationManager.GetString("Log_Error_NirFilteringFailed", message);
                LogRequested?.Invoke(LogSeverity.Error, "System", failMessage);
                Nir2FilteringState = CameraState.Stopped;
            }
        }
    }

    /// <summary>
    /// Toggles NIR2 data parsing (collection from API).
    /// </summary>
    private async void ExecuteToggleNir2Parsing()
    {
        if (Nir2ParsingState == CameraState.Running || Nir2ParsingState == CameraState.Starting)
        {
            // Stop parsing
            Nir2ParsingState = CameraState.Stopping;
            await _nir2DataCollector.StopAsync();
            _logger.LogInformation("NIR2 parsing stopped by user");
        }
        else
        {
            // Start parsing - validate settings first
            var config = await _configManager.LoadConfigurationAsync<ApplicationConfiguration>();
            if (string.IsNullOrWhiteSpace(config.Nir2Settings?.CsvDirectory))
            {
                Nir2ParsingState = CameraState.Stopped;
                var errorMsg = LocalizationManager.GetString("Error_Nir2CsvPathNotSet") ?? "CSV 저장 경로가 설정되지 않았습니다.";

                // Show MessageBox for user visibility
                System.Windows.MessageBox.Show(
                    errorMsg + "\n\n설정에서 NIR2 CSV 저장 경로를 먼저 설정하세요.",
                    "NIR2 CSV 경로 설정 필요",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);

                LogRequested?.Invoke(LogSeverity.Error, "NIR2", errorMsg);
                _logger.LogWarning("NIR2 parsing start failed: CSV directory not set");
                return;
            }

            Nir2ParsingState = CameraState.Starting;
            await _nir2DataCollector.StartAsync();
            _logger.LogInformation("NIR2 parsing started by user");
        }
    }

    /// <summary>
    /// Handles NIR2 data collector state changes.
    /// </summary>
    private void OnNir2CollectorStateChanged(Nir2CollectorState collectorState)
    {
        UpdateNir2ParsingState(collectorState);
    }

    /// <summary>
    /// Updates the Nir2ParsingState based on the collector state.
    /// </summary>
    private void UpdateNir2ParsingState(Nir2CollectorState collectorState)
    {
        CameraState uiState = collectorState switch
        {
            Nir2CollectorState.Stopped => CameraState.Stopped,
            Nir2CollectorState.Starting => CameraState.Starting,
            Nir2CollectorState.Running => CameraState.Running,
            Nir2CollectorState.Stopping => CameraState.Stopping,
            _ => CameraState.Stopped
        };
        Nir2ParsingState = uiState;
    }

    /// <summary>
    /// Handles NIR2 chunk detection events.
    /// Registers the chunk with the provider for later matching.
    /// </summary>
    private void OnNir2ChunkDetected(Nir2Chunk chunk)
    {
        _nir2DataProvider.RegisterChunk(chunk.ChunkId, chunk);
        _logger.LogInformation("[NIR2-UI] Chunk detected and registered: {ChunkId}, Samples={SampleCount}, P={Protein:F2}%, M={Moisture:F2}%",
            chunk.ChunkId, chunk.SampleCount, chunk.AggregatedProtein, chunk.AggregatedMoisture);

        var message = LocalizationManager.GetString("Log_Info_Nir2_ChunkDetected",
            chunk.ChunkId, chunk.SampleCount, chunk.AggregatedProtein?.ToString("F2") ?? "N/A",
            chunk.AggregatedMoisture?.ToString("F2") ?? "N/A");
        if (message != null)
        {
            LogRequested?.Invoke(LogSeverity.Info, "NIR2", message);
        }
    }

    private void UpdateCameraStatus(bool isActive, Func<CameraState> getState, Action<CameraState> setState)
    {
        var currentState = getState();
        // Ignore status updates during transitional states to prevent race conditions
        if (currentState is CameraState.Starting or CameraState.Stopping)
            return;

        setState(isActive ? CameraState.Running : CameraState.Stopped);
    }

    private async Task ExecuteLaunchAsync(dynamic launcher, string programName, Action<CameraState> setState, Func<CameraState> getState)
    {
        // 1. 이미 실행 중인지 확인
        if (launcher.IsActive && launcher.CurrentProcess != null)
        {
            // 2. 종료 확인 다이얼로그
            var dialogResult = System.Windows.MessageBox.Show(
                $"{programName}을(를) 종료하시겠습니까?",
                "프로그램 종료 확인",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (dialogResult == System.Windows.MessageBoxResult.Yes)
            {
                // 3. 예 → 종료
                setState(CameraState.Stopping);
                try {
                    await launcher.TerminateAsync();
                    var termMessage = LocalizationManager.GetString("Log_Info_Camera_Terminated", programName);
                    LogRequested?.Invoke(LogSeverity.Info, "System", termMessage);
                }
                finally {
                    // UpdateCameraStatus event should handle this, but strictly ensure state is updated
                    if (getState() == CameraState.Stopping)
                        setState(CameraState.Stopped);
                }
            }
            else
            {
                // 4. 아니오 → 맨 앞으로 활성화
                WindowActivationHelper.ActivateProcessWindow(launcher.CurrentProcess);
                var actMessage = LocalizationManager.GetString("Log_Info_Camera_Activated", programName);
                LogRequested?.Invoke(LogSeverity.Info, "System", actMessage);
            }
            return;
        }

        // 5. 실행 중이 아니면 실행
        setState(CameraState.Starting);
        (bool success, string message) launchResult = await launcher.LaunchAsync();

        if (launchResult.success)
        {
            var launchMessage = LocalizationManager.GetString("Log_Info_Camera_Launched", launchResult.message);
            LogRequested?.Invoke(LogSeverity.Info, "System", launchMessage);
            setState(CameraState.Running);
        }
        else
        {
            var failMessage = LocalizationManager.GetString("Log_Error_CameraLaunchFailed", programName, launchResult.message);
            LogRequested?.Invoke(LogSeverity.Error, "System", failMessage);
            setState(CameraState.Stopped);
        }
    }

    private async Task InitializeLaunchersAsync()
    {
        await Task.WhenAll(
            _generalCameraLauncher.CheckStatusAsync(),
            _nirCameraLauncher.CheckStatusAsync()
        );
    }

    public void Dispose() { }
}
