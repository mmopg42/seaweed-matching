using System;
using System.Threading.Tasks;
using System.Windows.Input;
using ChronoView.Models;

namespace ChronoView.UI.ViewModels;

/// <summary>
/// Sub-ViewModel for managing system monitoring and program status.
/// </summary>
public interface ISystemControlViewModel : IDisposable
{
    ICommand StartCommand { get; }
    ICommand StopCommand { get; }
    ICommand ToggleNir2FilteringCommand { get; }
    ICommand LaunchGeneralCameraCommand { get; }
    ICommand LaunchNir1CameraCommand { get; }

    bool IsMonitoring { get; }
    // Camera State properties
    CameraState GeneralCameraState { get; }
    CameraState NirCameraState { get; }
    CameraState Nir2FilteringState { get; }
    
    Task StartMonitoringAsync();
    Task StopMonitoringAsync();
    Task RefreshMonitoringAsync();

    event Action<LogSeverity, string, string> LogRequested;
    event Action<string> StatusChanged;
}
