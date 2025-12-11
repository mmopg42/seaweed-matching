using System.Text;
using System.Text.Json;
using System.Windows;
using ChronoView.Models;
using ChronoView.UI.ViewModels;
using ChronoView.UI.Views;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using WpfApplication = System.Windows.Application;

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
        
        _logger.LogInformation("MainWindow initialized with dependency injection");
    }



    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _logger?.LogInformation("MainWindow loaded");
        // Restore window state from configuration
        // This will be implemented when window state manager is available
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        _logger?.LogInformation("MainWindow closing");
        // Save window state to configuration
        // This will be implemented when window state manager is available
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Setup_Click(object sender, RoutedEventArgs e)
    {
        OpenSettingsDialog();
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
        
        // Reload UI display settings from configuration
        var app = (App)WpfApplication.Current;
        var configManager = app.Services.GetRequiredService<Core.Configuration.IConfigurationManager>();
        var config = configManager.LoadConfiguration<ApplicationConfiguration>();
        
        _viewModel.DisplayImageWidth = config.UISettings.DisplayImageWidth;
        _viewModel.DisplayImageHeight = config.UISettings.DisplayImageHeight;
        _viewModel.DataGridRowHeight = config.UISettings.DataGridRowHeight;
        _viewModel.NirDisplayWidth = config.UISettings.NirDisplayWidth;
        _viewModel.NirDisplayHeight = config.UISettings.NirDisplayHeight;
        
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
    
    private void DataGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        // Force command re-evaluation for CanExecute
        System.Windows.Input.CommandManager.InvalidateRequerySuggested();
    }
}
