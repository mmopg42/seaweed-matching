using ChronoView.Core.Configuration;
using ChronoView.Core.Localization;
using ChronoView.Core.FileWatching;
using ChronoView.Core.Analytics;
using ChronoView.Core.ProgramLaunching;
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
    private readonly Nir2CameraLauncher _nir2CameraLauncher;
    private readonly NirFilteringService _nirFilteringService;
    private readonly ILogger<SystemControlViewModel> _logger;

    public event Action<LogSeverity, string, string>? LogRequested;
    public event Action<string>? StatusChanged;

    private bool _isMonitoring; public bool IsMonitoring { get => _isMonitoring; private set { if (SetProperty(ref _isMonitoring, value)) { (StartCommand as RelayCommand)?.RaiseCanExecuteChanged(); (StopCommand as RelayCommand)?.RaiseCanExecuteChanged(); } } }
    
    private CameraState _genCamState = CameraState.Stopped; public CameraState GeneralCameraState { get => _genCamState; private set => SetProperty(ref _genCamState, value); }
    
    private CameraState _nirCamState = CameraState.Stopped; public CameraState NirCameraState { get => _nirCamState; private set => SetProperty(ref _nirCamState, value); }
    
    private CameraState _nir2CamState = CameraState.Stopped; public CameraState Nir2CameraState { get => _nir2CamState; private set => SetProperty(ref _nir2CamState, value); }

    private CameraState _nir2FiltState = CameraState.Stopped; public CameraState Nir2FilteringState { get => _nir2FiltState; private set => SetProperty(ref _nir2FiltState, value); }

    public ICommand StartCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand ToggleNir2FilteringCommand { get; }
    public ICommand LaunchGeneralCameraCommand { get; }
    public ICommand LaunchNir1CameraCommand { get; }
    public ICommand LaunchNir2CameraCommand { get; }

    public SystemControlViewModel(IMonitoringOrchestrator orchestrator, IStatisticsService statsService, IConfigurationManager configManager, GeneralCameraLauncher genCam, NirCameraLauncher nirCam, Nir2CameraLauncher nir2Cam, NirFilteringService nirFilter, ILogger<SystemControlViewModel> logger)
    {
        _orchestrator = orchestrator; _statsService = statsService; _configManager = configManager; _generalCameraLauncher = genCam; _nirCameraLauncher = nirCam; _nir2CameraLauncher = nir2Cam; _nirFilteringService = nirFilter; _logger = logger;

        StartCommand = new RelayCommand(() => _ = StartMonitoringAsync(), () => !IsMonitoring);
        StopCommand = new RelayCommand(() => _ = StopMonitoringAsync(), () => IsMonitoring);
        ToggleNir2FilteringCommand = new RelayCommand(() => ExecuteToggleNir2Filtering());
        LaunchGeneralCameraCommand = new RelayCommand(async () => await ExecuteLaunchAsync(_generalCameraLauncher, "General Camera", s => GeneralCameraState = s, () => GeneralCameraState));
        LaunchNir1CameraCommand = new RelayCommand(async () => await ExecuteLaunchAsync(_nirCameraLauncher, "NIR Camera 1", s => NirCameraState = s, () => NirCameraState));
        LaunchNir2CameraCommand = new RelayCommand(async () => await ExecuteLaunchAsync(_nir2CameraLauncher, "NIR Camera 2", s => Nir2CameraState = s, () => Nir2CameraState));

        _generalCameraLauncher.StatusChanged += (s, active) => UpdateCameraStatus(active, () => GeneralCameraState, s => GeneralCameraState = s);
        _nirCameraLauncher.StatusChanged += (s, active) => UpdateCameraStatus(active, () => NirCameraState, s => NirCameraState = s);
        _nir2CameraLauncher.StatusChanged += (s, active) => UpdateCameraStatus(active, () => Nir2CameraState, s => Nir2CameraState = s);
        
        Nir2FilteringState = _nirFilteringService.IsFilteringActive ? CameraState.Running : CameraState.Stopped;
        _nirFilteringService.StatusChanged += (s, active) =>
        {
            UpdateCameraStatus(active, () => Nir2FilteringState, s => Nir2FilteringState = s);
            var stateText = active ? "Activated" : "Deactivated";
            var message = LocalizationManager.GetString("Log_Info_NirFiltering_Toggled", stateText);
            LogRequested?.Invoke(LogSeverity.Info, "System", message);
        };

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
            _nirCameraLauncher.CheckStatusAsync(),
            _nir2CameraLauncher.CheckStatusAsync()
        );
    }

    public void Dispose() { }
}
