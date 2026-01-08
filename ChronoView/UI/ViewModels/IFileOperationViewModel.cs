using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using ChronoView.Models;

namespace ChronoView.UI.ViewModels;

/// <summary>
/// Sub-ViewModel for managing move/delete operations.
/// </summary>
public interface IFileOperationViewModel : IDisposable
{
    ICommand MoveCommand { get; }
    ICommand DeleteCommand { get; }
    ICommand RefreshCommand { get; }
    
    // Operation status
    bool IsOperationInProgress { get; }
    int ProgressValue { get; }

    Task RefreshDataAsync();
    
    // These methods are needed for delegating execution from MainWindow with params
    Task ExecuteMoveAsync(IEnumerable<FileGroupViewModel> selectedGroups, string moveNirCount, string moveAllDataCount, string? subject = null);
    Task ExecuteDeleteAsync(IEnumerable<FileGroupViewModel> selectedGroups, string? subject = null);

    event Action<LogSeverity, string, string> LogRequested;
    event Action<string> StatusChanged;
}
