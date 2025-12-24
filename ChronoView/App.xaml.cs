using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ChronoView.Core.Configuration;
using ChronoView.Core.FileWatching;
using ChronoView.Core.FileMatching;
using ChronoView.Core.ImageProcessing;
using ChronoView.Core.Analytics;
using ChronoView.Core.Nir;
using Application = System.Windows.Application;
using WpfMessageBox = System.Windows.MessageBox;
using ChronoView.Core.FileOperations;
using ChronoView.Core.ProgramLaunching;
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

    protected override async void OnStartup(StartupEventArgs e)
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

        // 1. Show splash screen
        var splash = new SplashWindow();
        splash.Show();

        // 2. Initialize services asynchronously
        await System.Threading.Tasks.Task.Run(async () =>
        {
            // Configure dependency injection
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            // Get logger
            var logger = _serviceProvider.GetRequiredService<ILogger<App>>();
            logger.LogInformation("ChronoView application starting...");

            // Ensure splash screen is visible for at least 2 seconds
            await System.Threading.Tasks.Task.Delay(2000);
        });

        // CRITICAL: Set ShutdownMode to prevent auto-shutdown when MainWindow closes
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var logger2 = _serviceProvider!.GetRequiredService<ILogger<App>>();
        logger2.LogInformation("Showing Setup Window");

        try
        {
            logger2.LogInformation("Creating SetupWindow from DI container...");
            var setupWindow = _serviceProvider!.GetRequiredService<SetupWindow>();
            logger2.LogInformation("SetupWindow created successfully");

            // Get ViewModel to access StartClicked property
            var viewModel = (SetupWindowViewModel)setupWindow.DataContext;

            // Handle window closing
            setupWindow.Closed += (s, e) =>
            {
                logger2.LogInformation("SetupWindow closed event fired. StartClicked: {StartClicked}", viewModel.StartClicked);
                
                if (viewModel.StartClicked)
                {
                    logger2.LogInformation("Setup completed. Showing Main Window");
                    try
                    {
                        var mainWindow = _serviceProvider!.GetRequiredService<MainWindow>();
                        MainWindow = mainWindow;
                        mainWindow.Show();
                        logger2.LogInformation("MainWindow shown successfully");
                        
                        // Now switch shutdown mode back
                        ShutdownMode = ShutdownMode.OnMainWindowClose;
                    }
                    catch (Exception ex)
                    {
                        logger2.LogError(ex, "Failed to show MainWindow");
                        Shutdown();
                    }
                }
                else
                {
                    logger2.LogInformation("Setup cancelled. Shutting down application");
                    Shutdown();
                }
            };

            // CRITICAL: Set as MainWindow before showing
            MainWindow = setupWindow;
            logger2.LogInformation("SetupWindow set as Application.MainWindow");

            // Close splash now that we have a main window
            splash.Close();
            logger2.LogInformation("Splash closed");

            logger2.LogInformation("Showing SetupWindow...");
            setupWindow.Show();
        }
        catch (Exception ex)
        {
            logger2.LogCritical(ex, "CRITICAL: Exception while showing SetupWindow");
            splash.Close();
            LogAndShowError(ex, "Setup Window Error");
            Shutdown();
        }
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

        WpfMessageBox.Show(
            Core.Localization.LocalizationManager.GetString("Error_CriticalMessage", source, ex?.Message ?? "Unknown error"),
            Core.Localization.LocalizationManager.GetString("Error_Critical"),
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Configure logging
        services.AddLogging(configure =>
        {
            configure.AddConsole();
            configure.AddDebug();
            configure.SetMinimumLevel(LogLevel.Debug);  // Changed from Information to Debug for detailed logging
        });

        // Register WPF Dispatcher (Singleton - UI thread dispatcher)
        services.AddSingleton(System.Windows.Threading.Dispatcher.CurrentDispatcher);

        // Configuration (Singleton - loaded once and shared)
        services.AddSingleton<ApplicationConfiguration>(sp =>
        {
            var configManager = sp.GetRequiredService<IConfigurationManager>();
            return configManager.LoadConfiguration<ApplicationConfiguration>();
        });

        // Core Services (Singleton - maintain state across application lifetime)
        services.AddSingleton<IConfigurationManager, ConfigurationManager>();
        services.AddSingleton<FolderTimestampCache>(sp =>
        {
            var config = sp.GetRequiredService<ApplicationConfiguration>();
            int ttl = config.WorkflowSettings?.FolderTimestampCacheTTL ?? 300;
            return new FolderTimestampCache(ttl, sp.GetService<ILogger<FolderTimestampCache>>());
        });
        services.AddSingleton<IFileWatcher, FileWatcherService>();
        services.AddSingleton<IFileGroupMatcher>(sp =>
            new FileGroupMatcherService(
                sp.GetService<ILogger<FileGroupMatcherService>>()));
        services.AddSingleton<INirFileResolver, SpcTxtNirFileResolver>();
        services.AddSingleton<IImageProcessor, ImageProcessingService>();
        services.AddSingleton<IStatisticsService, StatisticsService>();
        services.AddSingleton<IMonitoringOrchestrator, MonitoringOrchestrator>(sp =>
            new MonitoringOrchestrator(
                sp.GetRequiredService<IFileGroupMatcher>(),
                sp.GetRequiredService<IFileWatcher>(),
                sp.GetRequiredService<ILogger<MonitoringOrchestrator>>(),
                sp.GetRequiredService<INirFileResolver>(),
                sp.GetRequiredService<FolderTimestampCache>()));
        services.AddSingleton<IAbnormalDetector, AbnormalDetectorService>();

        // File Operation Services (Transient - new instance per operation)
        services.AddTransient<IFileOperationService, FileOperationService>();
        services.AddTransient<IPathManagementService, PathManagementService>();

        // Program Launchers (Singleton - shared state for program status tracking)
        services.AddSingleton<GeneralCameraLauncher>();
        services.AddSingleton<NirCameraLauncher>();
        services.AddSingleton<Nir2CameraLauncher>();

        // ViewModels (Transient - new instance per view)
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<SettingsDialogViewModel>();
        services.AddTransient<SetupWindowViewModel>();

        // Views (Transient - new instance per dialog/window)
        services.AddTransient<MainWindow>();
        services.AddTransient<SettingsDialog>();
        services.AddTransient<SetupWindow>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        var logger = _serviceProvider?.GetService<ILogger<App>>();
        logger?.LogInformation("Application exiting with code {ExitCode}", e.ApplicationExitCode);
        
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}

