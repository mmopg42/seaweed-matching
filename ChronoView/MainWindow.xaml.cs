using System.Text;
using System.Text.Json;
using System.Windows;
using ChronoView.Models;
using ChronoView.UI.ViewModels;
using ChronoView.UI.Views;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

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
        var app = (App)Application.Current;
        var settingsDialog = app.Services.GetRequiredService<SettingsDialog>();
        settingsDialog.Owner = this;
        
        var result = settingsDialog.ShowDialog();
        
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
}
