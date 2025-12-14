using System.Windows;
using System.Windows.Input;
using ChronoView.UI.ViewModels;
using Microsoft.Extensions.Logging;

namespace ChronoView.UI.Views;

/// <summary>
/// Interaction logic for SetupWindow.xaml
/// </summary>
public partial class SetupWindow : Window
{
    private readonly ILogger<SetupWindow> _logger;

    public SetupWindow(SetupWindowViewModel viewModel, ILogger<SetupWindow> logger)
    {
        _logger = logger;
        _logger.LogInformation("SetupWindow constructor called");
        InitializeComponent();
        _logger.LogInformation("InitializeComponent completed");
        DataContext = viewModel;
        _logger.LogInformation("DataContext set to viewModel");
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1)
        {
            DragMove();
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        _logger.LogInformation("Minimize button clicked");
        WindowState = WindowState.Minimized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        _logger.LogInformation("Close button clicked - shutting down application");
        System.Windows.Application.Current.Shutdown();
    }
}
