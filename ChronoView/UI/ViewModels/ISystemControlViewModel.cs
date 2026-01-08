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
    
    bool IsMonitoring { get; }
    string GeneralCameraStatus { get; }
    string NirCameraStatus { get; }
    
    Task StartMonitoringAsync();
    Task StopMonitoringAsync();
    Task RefreshMonitoringAsync();

    event Action<LogSeverity, string, string> LogRequested;
    event Action<string> StatusChanged;
}
