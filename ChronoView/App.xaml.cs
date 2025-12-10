using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ChronoView.Core.Configuration;
using ChronoView.Core.FileWatching;
using ChronoView.Core.FileMatching;
using ChronoView.Core.ImageProcessing;
using ChronoView.Core.Analytics;
using Application = System.Windows.Application;
using WpfMessageBox = System.Windows.MessageBox;
using ChronoView.Core.FileOperations;
using ChronoView.UI.ViewModels;
using ChronoView.UI.Views;
using ChronoView.Models;

namespace ChronoView;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    /// <summary>
    /// Gets the service provider for dependency injection
    /// </summary>
    public IServiceProvider Services => _serviceProvider ?? throw new InvalidOperationException("Service provider not initialized");

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // UI Thread Exception Handling
        DispatcherUnhandledException += (s, args) =>
        {
            LogAndShowError(args.Exception, "UI Thread Exception");
            args.Handled = true;
        };

        // Background Thread Exception Handling
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            LogAndShowError(ex, "AppDomain Unhandled Exception");
        };

        // Task Exception Handling
        TaskScheduler.UnobservedTaskException += (s, args) =>
        {
            LogAndShowError(args.Exception, "TaskScheduler Unobserved Exception");
            args.SetObserved();
        };

        // Configure dependency injection
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        // Get logger
        var logger = _serviceProvider.GetRequiredService<ILogger<App>>();
        logger.LogInformation("ChronoView application starting...");

        // Show main window
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private void LogAndShowError(Exception? ex, string source)
    {
        var logger = _serviceProvider?.GetService<ILogger<App>>();
        var errorMessage = ex?.ToString() ?? "Unknown error";
        
        logger?.LogCritical(ex, "CRITICAL ERROR in {Source}: {Message}", source, ex?.Message);
        
        // Write to a separate panic log file in case regular logging fails
        try 
        {
            File.AppendAllText("critical_error.log", $"{DateTime.Now}: [{source}] {errorMessage}\n\n");
        }
        catch { /* ignored as we are in a critical state */ }

        WpfMessageBox.Show($"Critical Error ({source}):\n{ex?.Message}\n\nCheck critical_error.log for details.", 
                        "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Configure logging
        services.AddLogging(configure =>
        {
            configure.AddConsole();
            configure.AddDebug();
            configure.SetMinimumLevel(LogLevel.Information);
        });

        // Register WPF Dispatcher (Singleton - UI thread dispatcher)
        services.AddSingleton(System.Windows.Threading.Dispatcher.CurrentDispatcher);

        // Configuration (Singleton - loaded once and shared)
        services.AddSingleton(sp =>
        {
            var configManager = sp.GetRequiredService<IConfigurationManager>();
            return configManager.LoadConfiguration<ApplicationConfiguration>();
        });

        // Core Services (Singleton - maintain state across application lifetime)
        services.AddSingleton<IConfigurationManager, ConfigurationManager>();
        services.AddSingleton<IFileWatcher, FileWatcherService>();
        services.AddSingleton<IFileGroupMatcher, FileGroupMatcherService>();
        services.AddSingleton<IImageProcessor, ImageProcessingService>();
        services.AddSingleton<IStatisticsService, StatisticsService>();
        services.AddSingleton<IMonitoringOrchestrator, MonitoringOrchestrator>();
        services.AddSingleton<IAbnormalDetector, AbnormalDetectorService>();

        // File Operation Services (Transient - new instance per operation)
        services.AddTransient<IFileOperationService, FileOperationService>();
        services.AddTransient<IPathManagementService, PathManagementService>();

        // ViewModels (Transient - new instance per view)
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<SettingsDialogViewModel>();

        // Views (Transient - new instance per dialog/window)
        services.AddTransient<MainWindow>();
        services.AddTransient<SettingsDialog>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        var logger = _serviceProvider?.GetService<ILogger<App>>();
        logger?.LogInformation("Application exiting with code {ExitCode}", e.ApplicationExitCode);
        
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}

