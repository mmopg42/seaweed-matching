using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using ChronoView.Models;

namespace ChronoView.UI.ViewModels;

/// <summary>
/// ViewModel for the Setup Window.
/// Handles launching external programs and navigation to main window.
/// </summary>
public class SetupWindowViewModel : ViewModelBase
{
    private readonly ILogger<SetupWindowViewModel>? _logger;
    private readonly ApplicationConfiguration? _config;
    
    private string _statusMessage = string.Empty;
    private Visibility _statusVisibility = Visibility.Collapsed;

    public SetupWindowViewModel()
    {
        // Parameterless constructor for XAML designer
        LaunchGeneralCameraCommand = new RelayCommand(async () => await LaunchProgramAsync("General Camera", GetGeneralCameraPath()));
        LaunchNir1CameraCommand = new RelayCommand(async () => await LaunchProgramAsync("NIR Camera 1", GetNir1ProgramPath()));
        LaunchNir2CameraCommand = new RelayCommand(async () => await LaunchProgramAsync("NIR Camera 2", GetNir2ProgramPath()));
        StartCommand = new RelayCommand(OnStart);
    }

    public SetupWindowViewModel(ILogger<SetupWindowViewModel> logger, ApplicationConfiguration config)
    {
        _logger = logger;
        _config = config;

        _logger.LogInformation("SetupWindowViewModel constructor called");

        LaunchGeneralCameraCommand = new RelayCommand(async () => await LaunchProgramAsync("General Camera", GetGeneralCameraPath()));
        LaunchNir1CameraCommand = new RelayCommand(async () => await LaunchProgramAsync("NIR Camera 1", GetNir1ProgramPath()));
        LaunchNir2CameraCommand = new RelayCommand(async () => await LaunchProgramAsync("NIR Camera 2", GetNir2ProgramPath()));
        StartCommand = new RelayCommand(OnStart);

        _logger.LogInformation("SetupWindowViewModel initialized successfully");
    }

    public ICommand LaunchGeneralCameraCommand { get; }
    public ICommand LaunchNir1CameraCommand { get; }
    public ICommand LaunchNir2CameraCommand { get; }
    public ICommand StartCommand { get; }

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

    private string GetGeneralCameraPath()
    {
        // TODO: Add GeneralCameraProgramPath to ApplicationConfiguration
        // For now, return empty string
        return string.Empty;
    }

    private string GetNir1ProgramPath()
    {
        // TODO: Add Nir1ProgramPath to ApplicationConfiguration
        // For now, return empty string
        return string.Empty;
    }

    private string GetNir2ProgramPath()
    {
        // TODO: Add Nir2ProgramPath to ApplicationConfiguration
        // For now, return empty string
        return string.Empty;
    }

    private async Task LaunchProgramAsync(string programName, string programPath)
    {
        try
        {
            _logger?.LogInformation("Attempting to launch {ProgramName}", programName);

            // Check if path is configured
            if (string.IsNullOrWhiteSpace(programPath))
            {
                ShowStatus($"Path not set for {programName}");
                _logger?.LogWarning("Path not configured for {ProgramName}", programName);
                return;
            }

            // Check if file exists
            if (!File.Exists(programPath))
            {
                ShowStatus($"File not found: {programPath}");
                _logger?.LogWarning("Program file not found for {ProgramName}: {Path}", programName, programPath);
                return;
            }

            // Show launching status
            ShowStatus($"Launching {programName}...");

            // Launch the program asynchronously
            await Task.Run(() =>
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = programPath,
                    UseShellExecute = true
                };
                Process.Start(startInfo);
            });

            // Show success
            ShowStatus($"{programName} launched successfully");
            _logger?.LogInformation("{ProgramName} launched successfully from {Path}", programName, programPath);

            // Auto-hide status after 3 seconds
            await Task.Delay(3000);
            if (StatusMessage == $"{programName} launched successfully")
            {
                StatusVisibility = Visibility.Collapsed;
            }
        }
        catch (Exception ex)
        {
            ShowStatus($"Error launching {programName}: {ex.Message}");
            _logger?.LogError(ex, "Failed to launch {ProgramName}", programName);
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
}
