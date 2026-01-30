using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ChronoView.Core.Configuration;
using ChronoView.Core.FileWatching;
using ChronoView.Core.FileMatching;
using ChronoView.Core.GroupIdGeneration;
using ChronoView.Core.ImageProcessing;
using ChronoView.Core.Analytics;
using ChronoView.Core.NIR.Shared;
using ChronoView.Core.NIR.Line1;
using ChronoView.Core.NIR.Line2;
using ChronoView.Core.NIR.Interfaces;
using Application = System.Windows.Application;
using WpfMessageBox = System.Windows.MessageBox;
using ChronoView.Core.FileOperations;
using ChronoView.Core.FileOperations.Line1;
using ChronoView.Core.FileOperations.Line2;
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

    /// <summary>
    /// Global session start time to ensure log files share the same timestamp
    /// </summary>
    public static readonly DateTime SessionStartTime = DateTime.Now;

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
            
            // Initialize log cleanup service (run asynchronously to avoid blocking UI)
            var cleanupService = _serviceProvider.GetRequiredService<Core.Logging.LogCleanupService>();
            System.Threading.Tasks.Task.Run(() => cleanupService.CleanupOldLogFiles());

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
        
            // Get config manager for program name access
            var configManager = _serviceProvider.GetRequiredService<IConfigurationManager>();

            // Write to a separate panic log file in case regular logging fails
            try
            {
                var criticalLogFile = PathHelper.GetSessionLogFilePath(configManager.AppName + "_Critical");
                File.AppendAllText(criticalLogFile, $"{DateTime.Now}: [{source}] {errorMessage}\n\n");
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
            
            // 개발용 로그 파일 저장 추가
            var startTime = PathHelper.SessionStartTime;
                var logFile = PathHelper.GetSessionLogFilePath("ChronoView_Debug");
            
            // 세션 시작 시간 기록 (파일이 새로 생성될 때만)
            if (!File.Exists(logFile) || new FileInfo(logFile).Length == 0)
            {
                var separator = new string('=', 80);
                var sessionHeader = $"\n{separator}\n" +
                                   $"Session Started: {startTime:yyyy-MM-dd HH:mm:ss}\n" +
                                   $"{separator}\n\n";
                File.AppendAllText(logFile, sessionHeader);
            }
            
            // FileLoggerProvider에 완전한 파일 경로 전달 (날짜 폴더 포함)
            configure.AddProvider(new Infrastructure.Logging.FileLoggerProvider(logFile));
        });

        // Register WPF Dispatcher (Singleton - UI thread dispatcher)
        // Application.Current.Dispatcher를 사용하여 항상 UI Dispatcher를 주입
        services.AddSingleton(sp => System.Windows.Application.Current.Dispatcher);

        // Configuration (Singleton - loaded once and shared)
        services.AddSingleton<ApplicationConfiguration>(sp =>
        {
            var configManager = sp.GetRequiredService<IConfigurationManager>();
            return configManager.LoadConfiguration<ApplicationConfiguration>();
        });

        // Core Services (Singleton - maintain state across application lifetime)
        services.AddSingleton<IConfigurationManager>(sp => new ConfigurationManager("prische"));
        services.AddSingleton<ITimestampCache>(sp =>
        {
            var config = sp.GetRequiredService<ApplicationConfiguration>();
            int ttl = config.WorkflowSettings?.FolderTimestampCacheTTL ?? 300;
            return new FolderTimestampCache(ttl, sp.GetService<ILogger<FolderTimestampCache>>());
        });
        
        // Group ID Generator (Strategy Pattern - depends on UseLineSpecificGroupId setting)
        services.AddSingleton<IGroupIdGenerator>(sp =>
        {
            var config = sp.GetRequiredService<ApplicationConfiguration>();
            return config.WorkflowSettings.UseLineSpecificGroupId
                ? new LineBasedGroupIdGenerator()
                : new GlobalGroupIdGenerator();
        });

        services.AddSingleton<IEvictionService, EvictionService>();
        services.AddSingleton<IGroupManager, GroupManager>();
        services.AddSingleton<IFileWatcher, FileWatcherService>();
        services.AddSingleton<IFileGroupMatcher>(sp =>
            new FileGroupMatcherService(
                sp.GetRequiredService<IGroupIdGenerator>(),
                sp.GetRequiredService<INirMatcher>(),
                sp.GetService<ILogger<FileGroupMatcherService>>()));
        services.AddSingleton<INirMatcher, FileBasedNirMatcher>();
        services.AddSingleton<INirFileResolver, SpcTxtNirFileResolver>();
        services.AddSingleton<IInitialScanner, InitialScanner>();
        services.AddSingleton<IImageProcessor, ImageProcessingService>();
        services.AddSingleton<IImageCaptureService, ImageCaptureService>(); // NEW: ImageCaptureService registration
        services.AddSingleton<IEventProcessor, EventProcessor>(); // NEW: IEventProcessor registration
        services.AddSingleton<IStatisticsService, StatisticsService>();
        services.AddSingleton<IMonitoringOrchestrator, MonitoringOrchestrator>(sp =>
            new MonitoringOrchestrator(
                sp.GetRequiredService<IFileGroupMatcher>(),
                sp.GetRequiredService<IFileWatcher>(),
                sp.GetRequiredService<ILogger<MonitoringOrchestrator>>(),
                sp.GetRequiredService<INirFileResolver>(),
                sp.GetRequiredService<ITimestampCache>(),
                sp.GetRequiredService<IInitialScanner>(),
                sp.GetRequiredService<IGroupManager>(),
                sp.GetRequiredService<IImageCaptureService>(),
                sp.GetRequiredService<IEventProcessor>(),
                sp.GetRequiredService<IConfigurationManager>(),
                sp.GetRequiredService<IAbnormalDetector>(),
                (severity, category, message) => 
                {
                    var logPanel = App.Current.MainWindow?.FindName("LogPanel") as FrameworkElement;
                    // ... simplified for now, orchestrated through events is better
                }
            ));
        
        // Abnormal Detection Services
        services.AddSingleton<AbnormalHistoryManager>();
        services.AddSingleton<IAbnormalDetector, AbnormalDetectorService>(sp =>
            new AbnormalDetectorService(
                sp.GetRequiredService<IConfigurationManager>(),
                sp.GetRequiredService<AbnormalHistoryManager>(),
                sp.GetService<ILogger<AbnormalDetectorService>>()));

        // Path Builders (Singleton - shared for all operations)
        services.AddSingleton<IPathBuilder, Line2PathBuilder>();  // Register as interface for Line2CsvMoveManager
        services.AddSingleton<Line1PathBuilder>();
        services.AddSingleton<Line2PathBuilder>();

        // CSV Move Manager (Line2 only)
        services.AddSingleton<ICsvMoveManager, Line2CsvMoveManager>();

        // File Operation Services (Transient - new instance per operation)
        services.AddSingleton<IFileGroupOperator, FileGroupOperator>(sp =>
            new FileGroupOperator(
                sp.GetRequiredService<ILogger<FileGroupOperator>>(),
                sp.GetRequiredService<Line1PathBuilder>(),
                sp.GetRequiredService<Line2PathBuilder>()));
        services.AddTransient<IMoveService, MoveService>();
        services.AddTransient<IDeleteService, DeleteService>();
        services.AddTransient<IFileOperationService, FileOperationService>();
        services.AddTransient<IPathManagementService, PathManagementService>();

        // Program Launchers (Singleton - shared state for program status tracking)
        services.AddSingleton<GeneralCameraLauncher>();
        services.AddSingleton<NirCameraLauncher>();
        services.AddSingleton<NirFilteringService>();

        // NIR2 Services (Singleton - shared state for data collection)
        services.AddSingleton<INir2ChunkFileStorage, Nir2ChunkFileStorage>();
        services.AddSingleton<ApiBasedNirProvider>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<ApiBasedNirProvider>>();
            var chunkStorage = sp.GetRequiredService<INir2ChunkFileStorage>();
            return new ApiBasedNirProvider(logger, chunkStorage);
        });
        services.AddSingleton<INirDataProvider>(sp => sp.GetRequiredService<ApiBasedNirProvider>());
        services.AddSingleton<ChunkBasedNirMatcher>();
        services.AddSingleton<Nir2DataCollector>(sp =>
        {
            var config = sp.GetRequiredService<ApplicationConfiguration>();
            var logger = sp.GetRequiredService<ILogger<Nir2DataCollector>>();
            var chunkStorage = sp.GetRequiredService<INir2ChunkFileStorage>();
            var settings = config.Nir2Settings ?? new Core.Configuration.Nir2Settings();
            var chunkStoragePath = config.ExternalProgramSettings.Nir2ChunkStoragePath ?? "";
            return new Nir2DataCollector(settings, chunkStoragePath, logger, chunkStorage);
        });

        // ViewModels (Transient - new instance per view)
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<SettingsDialogViewModel>();
        services.AddTransient<SetupWindowViewModel>();

        // Views (Transient - new instance per dialog/window)
        services.AddTransient<MainWindow>();
        services.AddTransient<SettingsDialog>();
        services.AddTransient<SetupWindow>();
        
        // Logging Services
        services.AddSingleton<Core.Logging.LogCleanupService>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        var logger = _serviceProvider?.GetService<ILogger<App>>();
        logger?.LogInformation("Application exiting with code {ExitCode}", e.ApplicationExitCode);
        
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}

