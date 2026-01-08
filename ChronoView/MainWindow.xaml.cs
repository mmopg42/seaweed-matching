using System.Text;
using System.Text.Json;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using ChronoView.Models;
using ChronoView.UI.ViewModels;
using ChronoView.UI.Views;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using WpfApplication = System.Windows.Application;
using System.Windows.Threading;

// Using aliases to avoid ambiguous references with System.Windows.Forms
using WpfBorder = System.Windows.Controls.Border;
using WpfImage = System.Windows.Controls.Image;
using WpfGrid = System.Windows.Controls.Grid;
using WpfProgressBar = System.Windows.Controls.ProgressBar;
using WpfBinding = System.Windows.Data.Binding;

namespace ChronoView;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly ILogger<MainWindow> _logger;
    private readonly MainWindowViewModel _viewModel;

    public MainWindow(MainWindowViewModel viewModel, ILogger<MainWindow> logger)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        InitializeComponent();
        
        // Set DataContext to injected ViewModel
        DataContext = _viewModel;

        // Subscribe to ViewModel events
        _viewModel.RequestOpenSettings += OnRequestOpenSettings;
        
        _logger.LogInformation("MainWindow initialized with dependency injection");
    }



    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _logger?.LogInformation("MainWindow loaded");

        // Subscribe to collection changes for auto-scroll during monitoring
        if (_viewModel?.Dashboard != null)
        {
            _viewModel.Dashboard.Line1Groups.CollectionChanged += OnLine1GroupsChanged;
            _viewModel.Dashboard.Line2Groups.CollectionChanged += OnLine2GroupsChanged;
        }

        // Restore window state from configuration
        // This will be implemented when window state manager is available
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        _logger?.LogInformation("MainWindow closing");
        
        // Unsubscribe from collection change events
        if (_viewModel?.Dashboard != null)
        {
            _viewModel.Dashboard.Line1Groups.CollectionChanged -= OnLine1GroupsChanged;
            _viewModel.Dashboard.Line2Groups.CollectionChanged -= OnLine2GroupsChanged;
        }

        // Unsubscribe from events to prevent memory leaks
        if (_viewModel != null)
        {
            _viewModel.RequestOpenSettings -= OnRequestOpenSettings;
        }

        // Save window state to configuration
        // This will be implemented when window state manager is available
    }

    private void Setup_Click(object sender, RoutedEventArgs e)
    {
        OpenSettingsDialog();
    }

    private void OnRequestOpenSettings(object? sender, EventArgs e)
    {
        // Marshal to UI thread if needed (though usually called from UI command)
        Dispatcher.Invoke(OpenSettingsDialog);
    }

    /// <summary>
    /// Opens the settings dialog when Setup button is clicked.
    /// This is called from XAML instead of using Command binding.
    /// </summary>
    public void OpenSettingsDialog()
    {
        _logger.LogInformation("Opening settings dialog");

        // Get SettingsDialog from DI container
        var app = (App)WpfApplication.Current;
        var settingsDialog = app.Services.GetRequiredService<SettingsDialog>();
        settingsDialog.Owner = this;

        // Subscribe to SettingsApplied event
        if (settingsDialog.DataContext is SettingsDialogViewModel settingsViewModel)
        {
            settingsViewModel.SettingsApplied += OnSettingsApplied;
        }

        var result = settingsDialog.ShowDialog();

        // Unsubscribe from event
        if (settingsDialog.DataContext is SettingsDialogViewModel vm)
        {
            vm.SettingsApplied -= OnSettingsApplied;
        }

        if (result == true)
        {
            _logger.LogInformation("Settings saved");
            _viewModel.AddLogMessage(LogSeverity.Info, "System", "Settings saved successfully");
        }
        else
        {
            _logger.LogInformation("Settings cancelled");
        }
    }

    /// <summary>
    /// Handles the SettingsApplied event to reload all settings and restart monitoring.
    /// </summary>
    private void OnSettingsApplied(object? sender, EventArgs e)
    {
        _logger.LogInformation("Settings applied, reloading all settings and restarting monitoring");
        
        // Reload UI display settings by delegating to ViewModel logic
        _viewModel.LoadSettings();
        
        // Regenerate DataGrid columns based on updated settings
        _logger.LogInformation("Regenerating DataGrid columns after settings change");
        
        // Refresh columns on all DataGrid instances
        Line1DataGrid?.RefreshColumns();
        Line2DataGrid?.RefreshColumns();
        CombinedLine1DataGrid?.RefreshColumns();
        CombinedLine2DataGrid?.RefreshColumns();
        
        // Restart monitoring if currently running to apply new settings
        if (_viewModel.IsMonitoring)
        {
            _viewModel.StopCommand.Execute(null);
            System.Threading.Thread.Sleep(500); // Brief pause
            _viewModel.StartCommand.Execute(null);
            _viewModel.AddLogMessage(LogSeverity.Info, "System", "Settings applied and monitoring restarted");
        }
        else
        {
            // If not monitoring, just reload NIR graphs
            _viewModel.ReloadNirGraphThumbnails();
            _viewModel.AddLogMessage(LogSeverity.Info, "System", "Display settings applied");
        }
    }

    private void FileGroupRow_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is System.Windows.Controls.DataGridRow row)
        {
            _logger.LogInformation("Row DoubleClick detected. DataContext type: {Type}", row.DataContext?.GetType().Name ?? "null");
            
            if (row.DataContext is FileGroupViewModel group && _viewModel != null)
            {
                _logger.LogInformation("Executing OpenDetailViewCommand for group: {GroupId}", group.GroupId);
                _viewModel.OpenDetailViewCommand.Execute(group);
                e.Handled = true;
            }
            else
            {
                 _logger.LogWarning("Row DataContext is not FileGroupViewModel or ViewModel is null");
            }
        }
    }

    private void OnLine1GroupsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (!_viewModel.IsMonitoring) return;
        if (e.Action != NotifyCollectionChangedAction.Add) return;

        Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() =>
        {
            Line1DataGrid?.ScrollToBottom();
            CombinedLine1DataGrid?.ScrollToBottom();
        }));
    }

    private void OnLine2GroupsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (!_viewModel.IsMonitoring) return;
        if (e.Action != NotifyCollectionChangedAction.Add) return;

        Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() =>
        {
            Line2DataGrid?.ScrollToBottom();
            CombinedLine2DataGrid?.ScrollToBottom();
        }));
    }
}
