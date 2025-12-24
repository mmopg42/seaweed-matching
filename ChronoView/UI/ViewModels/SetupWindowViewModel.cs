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
    
    private string _statusMessage = string.Empty;
    private Visibility _statusVisibility = Visibility.Collapsed;
    private string _nir2FilteringStatus = "Deactivated";
    private System.Windows.Media.Brush _nir2FilteringForeground = new SolidColorBrush(Colors.Red);

    public SetupWindowViewModel()
    {
        // Parameterless constructor for XAML designer
        LaunchGeneralCameraCommand = new RelayCommand(async () => await ExecuteGeneralCameraLaunchAsync());
        LaunchNir1CameraCommand = new RelayCommand(async () => await ExecuteNir1CameraLaunchAsync());
        LaunchNir2CameraCommand = new RelayCommand(async () => await ExecuteNir2CameraLaunchAsync());
        ToggleNirFilteringCommand = new RelayCommand(async () => await ExecuteToggleNirFilteringAsync());
        StartCommand = new RelayCommand(OnStart);
        OpenSettingsCommand = new RelayCommand(OnOpenSettings);
    }

    public SetupWindowViewModel(
        ILogger<SetupWindowViewModel> logger,
        ApplicationConfiguration config,
        IServiceProvider serviceProvider,
        GeneralCameraLauncher generalCameraLauncher,
        NirCameraLauncher nirCameraLauncher,
        Nir2CameraLauncher nir2CameraLauncher)
    {
        _logger = logger;
        _config = config;
        _serviceProvider = serviceProvider;
        _generalCameraLauncher = generalCameraLauncher;
        _nirCameraLauncher = nirCameraLauncher;
        _nir2CameraLauncher = nir2CameraLauncher;

        _logger.LogInformation("SetupWindowViewModel constructor called");

        LaunchGeneralCameraCommand = new RelayCommand(async () => await ExecuteGeneralCameraLaunchAsync());
        LaunchNir1CameraCommand = new RelayCommand(async () => await ExecuteNir1CameraLaunchAsync());
        LaunchNir2CameraCommand = new RelayCommand(async () => await ExecuteNir2CameraLaunchAsync());
        ToggleNirFilteringCommand = new RelayCommand(async () => await ExecuteToggleNirFilteringAsync());
        StartCommand = new RelayCommand(OnStart);
        OpenSettingsCommand = new RelayCommand(OnOpenSettings);

        _logger.LogInformation("SetupWindowViewModel initialized successfully");
    }



    public ICommand LaunchGeneralCameraCommand { get; }
    public ICommand LaunchNir1CameraCommand { get; }
    public ICommand LaunchNir2CameraCommand { get; }
    public ICommand ToggleNirFilteringCommand { get; }
    public ICommand StartCommand { get; }
    public ICommand OpenSettingsCommand { get; }

    public bool StartClicked { get; private set; }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public Visibility StatusVisibility
    {
        get => _statusVisibility;
        set => SetProperty(ref _statusVisibility, value);
    }

    public string Nir2FilteringStatus
    {
        get => _nir2FilteringStatus;
        set => SetProperty(ref _nir2FilteringStatus, value);
    }

    public System.Windows.Media.Brush Nir2FilteringForeground
    {
        get => _nir2FilteringForeground;
        set => SetProperty(ref _nir2FilteringForeground, value);
    }

    private async Task ExecuteGeneralCameraLaunchAsync()
    {
        if (_generalCameraLauncher == null)
        {
            ShowStatus("General Camera launcher not initialized");
            return;
        }

        ShowStatus("Launching General Camera...");
        
        var (success, message) = await _generalCameraLauncher.LaunchAsync();
        
        ShowStatus(message);
        
        // 3초 후 자동 숨김 (성공한 경우만)
        if (success)
        {
            await Task.Delay(3000);
            if (StatusMessage == message)
            {
                StatusVisibility = Visibility.Collapsed;
            }
        }
    }

    private async Task ExecuteNir1CameraLaunchAsync()
    {
        if (_nirCameraLauncher == null)
        {
            ShowStatus("NIR Camera 1 launcher not initialized");
            return;
        }

        ShowStatus("Launching NIR Camera 1...");
        
        var (success, message) = await _nirCameraLauncher.LaunchAsync();
        
        ShowStatus(message);
        
        // 3초 후 자동 숨김 (성공한 경우만)
        if (success)
        {
            await Task.Delay(3000);
            if (StatusMessage == message)
            {
                StatusVisibility = Visibility.Collapsed;
            }
        }
    }

    private async Task ExecuteNir2CameraLaunchAsync()
    {
        if (_nir2CameraLauncher == null)
        {
            ShowStatus("NIR Camera 2 launcher not initialized");
            return;
        }

        ShowStatus("Launching NIR Camera 2...");
        
        var (success, message) = await _nir2CameraLauncher.LaunchAsync();
        
        ShowStatus(message);
        
        // 3초 후 자동 숨김 (성공한 경우만)
        if (success)
        {
            await Task.Delay(3000);
            if (StatusMessage == message)
            {
                StatusVisibility = Visibility.Collapsed;
            }
        }
    }



    private void ShowStatus(string message)
    {
        StatusMessage = message;
        StatusVisibility = Visibility.Visible;
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
        if (_nir2CameraLauncher == null)
        {
            ShowStatus("NIR2 Camera launcher not initialized");
            return;
        }

        try
        {
            if (_nir2CameraLauncher.IsFilteringActive)
            {
                // 필터링 중지
                _nir2CameraLauncher.StopFiltering();
                ShowStatus("NIR filtering stopped");
            }
            else
            {
                // 필터링 시작
                ShowStatus("Starting NIR filtering...");
                var (success, message) = await _nir2CameraLauncher.StartFilteringAsync();
                ShowStatus(message);
                
                if (success)
                {
                    await Task.Delay(2000);
                    if (StatusMessage == message)
                    {
                        StatusVisibility = Visibility.Collapsed;
                    }
                }
            }
            
            // 상태 업데이트
            UpdateNir2FilteringStatus();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to toggle NIR filtering");
            ShowStatus($"Error toggling NIR filtering: {ex.Message}");
        }
    }

    private void UpdateNir2FilteringStatus()
    {
        if (_nir2CameraLauncher == null)
            return;

        var greenBrush = new SolidColorBrush(Colors.Green);
        var redBrush = new SolidColorBrush(Colors.Red);

        if (_nir2CameraLauncher.IsFilteringActive)
        {
            Nir2FilteringStatus = "Activated";
            Nir2FilteringForeground = greenBrush;
        }
        else
        {
            Nir2FilteringStatus = "Deactivated";
            Nir2FilteringForeground = redBrush;
        }
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
            var viewModel = new SettingsDialogViewModel(configManager, settingsLogger);

            var settingsDialog = new Views.SettingsDialog(viewModel);
            settingsDialog.Owner = System.Windows.Application.Current.Windows.OfType<Window>()
                .FirstOrDefault(w => w.DataContext == this);
            settingsDialog.ShowDialog();

            _logger?.LogInformation("Settings dialog closed");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to open settings dialog");
            ShowStatus($"Error opening settings: {ex.Message}");
        }
    }
}
