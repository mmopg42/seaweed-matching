using ChronoView.Core.Configuration;
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
    private readonly ILogger<SystemControlViewModel> _logger;

    public event Action<LogSeverity, string, string>? LogRequested;
    public event Action<string>? StatusChanged;

    private bool _isMonitoring; public bool IsMonitoring { get => _isMonitoring; private set { if (SetProperty(ref _isMonitoring, value)) { (StartCommand as RelayCommand)?.RaiseCanExecuteChanged(); (StopCommand as RelayCommand)?.RaiseCanExecuteChanged(); } } }
    
    private string _genCamStatus = "Deactivated"; public string GeneralCameraStatus { get => _genCamStatus; private set => SetProperty(ref _genCamStatus, value); }
    private Brush _genCamForeground = Brushes.Gray; public Brush GeneralCameraForeground { get => _genCamForeground; private set => SetProperty(ref _genCamForeground, value); }
    
    private string _nirCamStatus = "Deactivated"; public string NirCameraStatus { get => _nirCamStatus; private set => SetProperty(ref _nirCamStatus, value); }
    private Brush _nirCamForeground = Brushes.Gray; public Brush NirCameraForeground { get => _nirCamForeground; private set => SetProperty(ref _nirCamForeground, value); }

    private string _nir2FiltStatus = "Deactivated"; public string Nir2FilteringStatus { get => _nir2FiltStatus; private set => SetProperty(ref _nir2FiltStatus, value); }
    private Brush _nir2FiltForeground = Brushes.Gray; public Brush Nir2FilteringForeground { get => _nir2FiltForeground; private set => SetProperty(ref _nir2FiltForeground, value); }
    private string _nir2ButtonText = "ON"; public string Nir2FilteringButtonText { get => _nir2ButtonText; private set => SetProperty(ref _nir2ButtonText, value); }
    private Brush _nir2ButtonBg = Brushes.Gray; public Brush Nir2FilteringButtonBackground { get => _nir2ButtonBg; private set => SetProperty(ref _nir2ButtonBg, value); }

    public ICommand StartCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand ToggleNir2FilteringCommand { get; }

    public SystemControlViewModel(IMonitoringOrchestrator orchestrator, IStatisticsService statsService, IConfigurationManager configManager, GeneralCameraLauncher genCam, NirCameraLauncher nirCam, Nir2CameraLauncher nir2Cam, ILogger<SystemControlViewModel> logger)
    {
        _orchestrator = orchestrator; _statsService = statsService; _configManager = configManager; _generalCameraLauncher = genCam; _nirCameraLauncher = nirCam; _nir2CameraLauncher = nir2Cam; _logger = logger;

        StartCommand = new RelayCommand(() => _ = StartMonitoringAsync(), () => !IsMonitoring);
        StopCommand = new RelayCommand(() => _ = StopMonitoringAsync(), () => IsMonitoring);
        ToggleNir2FilteringCommand = new RelayCommand(() => ExecuteToggleNir2Filtering());

        _generalCameraLauncher.StatusChanged += (s, active) => { GeneralCameraStatus = active ? "Activated" : "Deactivated"; GeneralCameraForeground = active ? Brushes.Green : Brushes.Gray; };
        _nirCameraLauncher.StatusChanged += (s, active) => { NirCameraStatus = active ? "Activated" : "Deactivated"; NirCameraForeground = active ? Brushes.Green : Brushes.Gray; };
        
        UpdateNir2FilteringStatus();
        _nir2CameraLauncher.StatusChanged += (s, active) => UpdateNir2FilteringStatus();
    }

    public async Task StartMonitoringAsync()
    {
        try {
            IsMonitoring = true;
            LogRequested?.Invoke(LogSeverity.Info, "System", "Starting monitoring...");
            var config = await _configManager.LoadConfigurationAsync<ApplicationConfiguration>();
            
            await _statsService.StartMonitoringAsync(config);
            await _orchestrator.StartAsync(config);
            
            _logger.LogInformation("Diagnostic: Monitoring started.");
            StatusChanged?.Invoke("Monitoring...");
        } catch (Exception ex) {
            IsMonitoring = false;
            LogRequested?.Invoke(LogSeverity.Error, "System", $"Start failed: {ex.Message}");
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
        LogRequested?.Invoke(LogSeverity.Info, "System", "Refreshing data...");
        var config = await _configManager.LoadConfigurationAsync<ApplicationConfiguration>();
        
        // Reload file statistics
        await _statsService.ReloadStatsAsync(config);

        // If monitoring is active, we might need to stop/start or use a dedicated refresh on orchestrator
        if (IsMonitoring)
        {
            // If Orchestrator supports hot-refresh:
            await _orchestrator.RefreshAsync(); 
        }
        else
        {
            // Just scan
            await _orchestrator.PerformInitialScanAsync();
        }
        StatusChanged?.Invoke("Refreshed");
    }

    private async void ExecuteToggleNir2Filtering()
    {
        if (_nir2CameraLauncher.IsFilteringActive) _nir2CameraLauncher.StopFiltering();
        else await _nir2CameraLauncher.StartFilteringAsync();
    }

    private void UpdateNir2FilteringStatus()
    {
        bool isActive = _nir2CameraLauncher.IsFilteringActive;
        Nir2FilteringStatus = isActive ? "Activated" : "Deactivated";
        Nir2FilteringForeground = isActive ? Brushes.Green : Brushes.Gray;
        Nir2FilteringButtonText = isActive ? "OFF" : "ON";
        Nir2FilteringButtonBackground = isActive ? Brushes.Red : Brushes.Green;
    }

    public void Dispose() { }
}
