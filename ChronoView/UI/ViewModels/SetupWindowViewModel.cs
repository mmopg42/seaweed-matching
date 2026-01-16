using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using ChronoView.Models;
using ChronoView.Core.Configuration;
using ChronoView.Core.ProgramLaunching;
using ChronoView.Core.Nir;
using ChronoView.Core.Localization;

namespace ChronoView.UI.ViewModels;

/// <summary>
/// ViewModel for the Setup Window.
/// Handles launching external programs and navigation to main window.
/// </summary>
public class SetupWindowViewModel : ViewModelBase
{
    private readonly ILogger<SetupWindowViewModel>? _logger;
    private readonly ApplicationConfiguration? _config;
    private readonly IServiceProvider? _serviceProvider;
    private readonly GeneralCameraLauncher? _generalCameraLauncher;
    private readonly NirCameraLauncher? _nirCameraLauncher;
    private readonly Nir2CameraLauncher? _nir2CameraLauncher;
    private readonly NirFilteringService? _nirFilteringService;
    
    private string _nir2FilteringStatus = LocalizationManager.GetString("Status_Deactivated");
    private bool _isNir2FilteringBusy;

    public SetupWindowViewModel()
    {
        // Parameterless constructor for XAML designer
        LaunchGeneralCameraCommand = new RelayCommand(async () => await ExecuteGeneralCameraLaunchAsync());
        LaunchNir1CameraCommand = new RelayCommand(async () => await ExecuteNir1CameraLaunchAsync());
        LaunchNir2CameraCommand = new RelayCommand(async () => await ExecuteNir2CameraLaunchAsync());
        ToggleNirFilteringCommand = new RelayCommand(async () => await ExecuteToggleNirFilteringAsync(), () => !IsNir2FilteringBusy);
        StartCommand = new RelayCommand(OnStart);
        OpenSettingsCommand = new RelayCommand(OnOpenSettings);

        // Design-time friendly default
        UpdateNir2FilteringStatus();
    }

    public SetupWindowViewModel(
        ILogger<SetupWindowViewModel> logger,
        ApplicationConfiguration config,
        IServiceProvider serviceProvider,
        GeneralCameraLauncher generalCameraLauncher,
        NirCameraLauncher nirCameraLauncher,
        Nir2CameraLauncher nir2CameraLauncher,
        NirFilteringService nirFilteringService)
    {
        _logger = logger;
        _config = config;
        _serviceProvider = serviceProvider;
        _generalCameraLauncher = generalCameraLauncher;
        _nirCameraLauncher = nirCameraLauncher;
        _nir2CameraLauncher = nir2CameraLauncher;
        _nirFilteringService = nirFilteringService;

        _logger.LogInformation("SetupWindowViewModel constructor called");

        LaunchGeneralCameraCommand = new RelayCommand(async () => await ExecuteGeneralCameraLaunchAsync());
        LaunchNir1CameraCommand = new RelayCommand(async () => await ExecuteNir1CameraLaunchAsync());
        LaunchNir2CameraCommand = new RelayCommand(async () => await ExecuteNir2CameraLaunchAsync());
        ToggleNirFilteringCommand = new RelayCommand(async () => await ExecuteToggleNirFilteringAsync(), () => !IsNir2FilteringBusy);
        StartCommand = new RelayCommand(OnStart);
        OpenSettingsCommand = new RelayCommand(OnOpenSettings);


        _logger.LogInformation("SetupWindowViewModel initialized successfully");

        // Keep UI in sync with service state (and clear busy flag on external state changes)
        _nirFilteringService.StatusChanged += (_, __) =>
        {
            IsNir2FilteringBusy = false;
            UpdateNir2FilteringStatus();
        };

        // Sync initial UI state with service
        UpdateNir2FilteringStatus();
    }



    public ICommand LaunchGeneralCameraCommand { get; }
    public ICommand LaunchNir1CameraCommand { get; }
    public ICommand LaunchNir2CameraCommand { get; }
    public ICommand ToggleNirFilteringCommand { get; }
    public ICommand StartCommand { get; }
    public ICommand OpenSettingsCommand { get; }

    public bool StartClicked { get; private set; }

    public string Nir2FilteringStatus
    {
        get => _nir2FilteringStatus;
        set => SetProperty(ref _nir2FilteringStatus, value);
    }

    public bool IsNir2FilteringBusy
    {
        get => _isNir2FilteringBusy;
        private set
        {
            if (SetProperty(ref _isNir2FilteringBusy, value))
            {
                // Ensure button enabled state updates immediately
                (ToggleNirFilteringCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    private async Task ExecuteGeneralCameraLaunchAsync()
    {
        if (_generalCameraLauncher == null)
        {
            _logger?.LogWarning("General Camera launcher not initialized");
            return;
        }

        var (success, message) = await _generalCameraLauncher.LaunchAsync();

        if (success) _logger?.LogInformation("General camera launch: {Message}", message);
        else _logger?.LogWarning("General camera launch failed: {Message}", message);
    }

    private async Task ExecuteNir1CameraLaunchAsync()
    {
        if (_nirCameraLauncher == null)
        {
            _logger?.LogWarning("NIR Camera 1 launcher not initialized");
            return;
        }

        var (success, message) = await _nirCameraLauncher.LaunchAsync();

        if (success) _logger?.LogInformation("NIR Camera 1 launch: {Message}", message);
        else _logger?.LogWarning("NIR Camera 1 launch failed: {Message}", message);
    }

    private async Task ExecuteNir2CameraLaunchAsync()
    {
        if (_nir2CameraLauncher == null)
        {
            _logger?.LogWarning("NIR Camera 2 launcher not initialized");
            return;
        }

        var (success, message) = await _nir2CameraLauncher.LaunchAsync();

        if (success) _logger?.LogInformation("NIR Camera 2 launch: {Message}", message);
        else _logger?.LogWarning("NIR Camera 2 launch failed: {Message}", message);
    }

    private void OnStart()
    {
        _logger?.LogInformation("User clicked Start Monitoring button");

        StartClicked = true;

        // Find the window and close it
        var window = System.Windows.Application.Current.Windows.OfType<Window>()
            .FirstOrDefault(w => w.DataContext == this);

        if (window != null)
        {
            window.Close();
        }
    }

    private async Task ExecuteToggleNirFilteringAsync()
    {
        if (_nirFilteringService == null)
        {
            _logger?.LogWarning("NIR Filtering Service not initialized");
            return;
        }

        try
        {
            if (IsNir2FilteringBusy)
                return;

            if (_nirFilteringService.IsFilteringActive)
            {
                // 필터링 중지
                _nirFilteringService.StopFiltering();
                UpdateNir2FilteringStatus();
            }
            else
            {
                // 필터링 시작
                IsNir2FilteringBusy = true;
                UpdateNir2FilteringStatus();
                var (success, message) = await _nirFilteringService.StartFilteringAsync();

                if (success) _logger?.LogInformation("NIR filtering started: {Message}", message);
                else _logger?.LogWarning("NIR filtering start failed: {Message}", message);

                IsNir2FilteringBusy = false;
            }
            
            // 상태 업데이트
            UpdateNir2FilteringStatus();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to toggle NIR filtering");
            IsNir2FilteringBusy = false;
            UpdateNir2FilteringStatus();
        }
    }

    private void UpdateNir2FilteringStatus()
    {
        if (_nirFilteringService == null)
            return;

        // 3-state display: Deactivated / Processing / Activated (text stays white per UI design)
        if (IsNir2FilteringBusy)
        {
            Nir2FilteringStatus = LocalizationManager.GetString("Status_Processing");
            return;
        }

        Nir2FilteringStatus = _nirFilteringService.IsFilteringActive
            ? LocalizationManager.GetString("Status_Activated")
            : LocalizationManager.GetString("Status_Deactivated");
    }

    private void OnOpenSettings()
    {
        try
        {
            _logger?.LogInformation("Opening settings dialog from Setup window");

            if (_serviceProvider == null)
            {
                _logger?.LogWarning("ServiceProvider is null, cannot open settings dialog");
                return;
            }

            // Create SettingsDialogViewModel using DI
            var configManager = _serviceProvider.GetRequiredService<IConfigurationManager>();
            var settingsLogger = _serviceProvider.GetRequiredService<ILogger<SettingsDialogViewModel>>();
            var viewModel = new SettingsDialogViewModel(configManager, settingsLogger)
            {
                SelectedTabIndex = 4 // Open "External Programs" tab by default from Setup screen
            };

            var settingsDialog = new Views.SettingsDialog(viewModel);
            settingsDialog.Owner = System.Windows.Application.Current.Windows.OfType<Window>()
                .FirstOrDefault(w => w.DataContext == this);
            settingsDialog.ShowDialog();

            _logger?.LogInformation("Settings dialog closed");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to open settings dialog");
        }
    }
}
