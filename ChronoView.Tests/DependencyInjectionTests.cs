using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ChronoView.Core.Configuration;
using ChronoView.Core.FileWatching;
using ChronoView.Core.FileMatching;
using ChronoView.Core.ImageProcessing;
using ChronoView.Core.Analytics;
using ChronoView.Core.FileOperations;
using ChronoView.Core.GroupIdGeneration;
using ChronoView.Core.Nir;
using System.Windows.Threading;
using ChronoView.UI.ViewModels;
using ChronoView.UI.Views;
using ChronoView.Models;
using Xunit;

namespace ChronoView.Tests;

/// <summary>
/// Tests for dependency injection container configuration.
/// Validates Requirements 1.1, 1.3, 1.4
/// </summary>
public class DependencyInjectionTests : IDisposable
{
    private ServiceProvider? _serviceProvider;

    /// <summary>
    /// Creates a service provider with the same configuration as the application.
    /// </summary>
    private ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        
        // Configure logging
        services.AddLogging(configure =>
        {
            configure.AddConsole();
            configure.AddDebug();
            configure.SetMinimumLevel(LogLevel.Information);
        });

        // WPF Dispatcher (테스트에서는 Application.Current가 없을 수 있으므로 CurrentDispatcher 사용)
        services.AddSingleton(sp => Dispatcher.CurrentDispatcher);

        // Configuration: 테스트는 파일 I/O에 의존하지 않도록 기본 인스턴스를 사용
        services.AddSingleton<IConfigurationManager>(sp => new ConfigurationManager("ChronoViewTest", "Test"));
        services.AddSingleton<ApplicationConfiguration>(_ => new ApplicationConfiguration());

        // Configuration (Singleton - loaded once and shared)
        // Core Services (Singleton) - App.ConfigureServices() 구성과 동기화
        services.AddSingleton<ITimestampCache>(sp =>
        {
            var config = sp.GetRequiredService<ApplicationConfiguration>();
            var ttl = config.WorkflowSettings?.FolderTimestampCacheTTL ?? 300;
            return new FolderTimestampCache(ttl, sp.GetService<ILogger<FolderTimestampCache>>());
        });

        services.AddSingleton<IGroupIdGenerator>(sp =>
        {
            var config = sp.GetRequiredService<ApplicationConfiguration>();
            return config.WorkflowSettings.UseLineSpecificGroupId
                ? new LineBasedGroupIdGenerator()
                : new GlobalGroupIdGenerator();
        });

        services.AddSingleton<IGroupManager, GroupManager>();
        services.AddSingleton<IFileWatcher, FileWatcherService>();
        services.AddSingleton<IFileGroupMatcher>(sp =>
            new FileGroupMatcherService(
                sp.GetRequiredService<IGroupIdGenerator>(),
                sp.GetService<ILogger<FileGroupMatcherService>>()));
        services.AddSingleton<INirFileResolver, SpcTxtNirFileResolver>();
        services.AddSingleton<IInitialScanner, InitialScanner>();
        services.AddSingleton<IImageProcessor, ImageProcessingService>();
        services.AddSingleton<IImageCaptureService, ImageCaptureService>();
        services.AddSingleton<IEventProcessor, EventProcessor>();
        services.AddSingleton<IStatisticsService, StatisticsService>();

        // Abnormal Detection Services
        services.AddSingleton<AbnormalHistoryManager>();
        services.AddSingleton<IAbnormalDetector, AbnormalDetectorService>(sp =>
            new AbnormalDetectorService(
                sp.GetRequiredService<IConfigurationManager>(),
                sp.GetRequiredService<AbnormalHistoryManager>(),
                sp.GetService<ILogger<AbnormalDetectorService>>()));

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
                uiLog: null));

        // File Operation Services (Transient)
        services.AddSingleton<IFileGroupOperator, FileGroupOperator>();
        services.AddTransient<IMoveService, MoveService>();
        services.AddTransient<IDeleteService, DeleteService>();
        services.AddTransient<IFileOperationService, FileOperationService>();
        services.AddTransient<IPathManagementService, PathManagementService>();

        // ViewModels (Transient)
        services.AddTransient<SettingsDialogViewModel>();

        return services.BuildServiceProvider();
    }

    [Fact]
    public void AllCoreServices_CanBeResolved()
    {
        // Arrange
        _serviceProvider = CreateServiceProvider();

        // Act & Assert - Core services should be resolvable (excluding WPF-dependent services)
        Assert.NotNull(_serviceProvider.GetRequiredService<IConfigurationManager>());
        Assert.NotNull(_serviceProvider.GetRequiredService<IFileWatcher>());
        Assert.NotNull(_serviceProvider.GetRequiredService<IFileGroupMatcher>());
        Assert.NotNull(_serviceProvider.GetRequiredService<IImageProcessor>());
        Assert.NotNull(_serviceProvider.GetRequiredService<IMonitoringOrchestrator>());
        
        // Note: StatisticsService requires WPF Dispatcher and is tested during application startup
    }

    [Fact]
    public void AllFileOperationServices_CanBeResolved()
    {
        // Arrange
        _serviceProvider = CreateServiceProvider();

        // Act & Assert - All file operation services should be resolvable
        Assert.NotNull(_serviceProvider.GetRequiredService<IFileOperationService>());
        Assert.NotNull(_serviceProvider.GetRequiredService<IPathManagementService>());
    }

    [Fact]
    public void AllViewModels_CanBeResolved()
    {
        // Arrange
        _serviceProvider = CreateServiceProvider();

        // Act & Assert - ViewModels should be resolvable (excluding those with WPF dependencies)
        Assert.NotNull(_serviceProvider.GetRequiredService<SettingsDialogViewModel>());
        
        // Note: MainWindowViewModel requires StatisticsService which needs WPF Dispatcher
        // It is tested during application startup
    }

    // Note: WPF Window tests are skipped because they require STA thread
    // The actual Window resolution is tested during application startup

    [Fact]
    public void SingletonServices_ReturnSameInstance()
    {
        // Arrange
        _serviceProvider = CreateServiceProvider();

        // Act - Resolve singleton services twice
        var configManager1 = _serviceProvider.GetRequiredService<IConfigurationManager>();
        var configManager2 = _serviceProvider.GetRequiredService<IConfigurationManager>();

        var fileWatcher1 = _serviceProvider.GetRequiredService<IFileWatcher>();
        var fileWatcher2 = _serviceProvider.GetRequiredService<IFileWatcher>();

        var imageProcessor1 = _serviceProvider.GetRequiredService<IImageProcessor>();
        var imageProcessor2 = _serviceProvider.GetRequiredService<IImageProcessor>();

        var orchestrator1 = _serviceProvider.GetRequiredService<IMonitoringOrchestrator>();
        var orchestrator2 = _serviceProvider.GetRequiredService<IMonitoringOrchestrator>();

        // Assert - Same instances should be returned
        Assert.Same(configManager1, configManager2);
        Assert.Same(fileWatcher1, fileWatcher2);
        Assert.Same(imageProcessor1, imageProcessor2);
        Assert.Same(orchestrator1, orchestrator2);
        
        // Note: StatisticsService requires WPF Dispatcher and is tested during application startup
    }

    [Fact]
    public void TransientServices_ReturnDifferentInstances()
    {
        // Arrange
        _serviceProvider = CreateServiceProvider();

        // Act - Resolve transient services twice
        var fileOpService1 = _serviceProvider.GetRequiredService<IFileOperationService>();
        var fileOpService2 = _serviceProvider.GetRequiredService<IFileOperationService>();

        var pathMgmtService1 = _serviceProvider.GetRequiredService<IPathManagementService>();
        var pathMgmtService2 = _serviceProvider.GetRequiredService<IPathManagementService>();

        // Assert - Different instances should be returned
        Assert.NotSame(fileOpService1, fileOpService2);
        Assert.NotSame(pathMgmtService1, pathMgmtService2);
    }

    [Fact]
    public void TransientViewModels_ReturnDifferentInstances()
    {
        // Arrange
        _serviceProvider = CreateServiceProvider();

        // Act - Resolve ViewModels twice
        var settingsViewModel1 = _serviceProvider.GetRequiredService<SettingsDialogViewModel>();
        var settingsViewModel2 = _serviceProvider.GetRequiredService<SettingsDialogViewModel>();

        // Assert - Different instances should be returned
        Assert.NotSame(settingsViewModel1, settingsViewModel2);
        
        // Note: MainWindowViewModel requires StatisticsService which needs WPF Dispatcher
        // Transient behavior is tested during application startup
    }

    [Fact]
    public void ServiceProvider_DisposesServicesCorrectly()
    {
        // Arrange
        _serviceProvider = CreateServiceProvider();
        
        // Resolve some services to ensure they're created
        var configManager = _serviceProvider.GetRequiredService<IConfigurationManager>();
        var imageProcessor = _serviceProvider.GetRequiredService<IImageProcessor>();
        
        Assert.NotNull(configManager);
        Assert.NotNull(imageProcessor);

        // Act - Dispose the service provider
        _serviceProvider.Dispose();

        // Assert - Service provider should be disposed
        // Attempting to resolve services after disposal should throw
        Assert.Throws<ObjectDisposedException>(() => 
            _serviceProvider.GetRequiredService<IConfigurationManager>());
    }

    [Fact]
    public void LoggingServices_AreConfiguredCorrectly()
    {
        // Arrange
        _serviceProvider = CreateServiceProvider();

        // Act - Resolve logging services
        var loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = _serviceProvider.GetRequiredService<ILogger<DependencyInjectionTests>>();

        // Assert - Logging services should be available
        Assert.NotNull(loggerFactory);
        Assert.NotNull(logger);
    }

    // Note: MainWindowViewModel test is skipped because it requires StatisticsService
    // which needs WPF Dispatcher. This is tested during application startup.

    [Fact]
    public void SettingsDialogViewModel_ReceivesAllDependencies()
    {
        // Arrange
        _serviceProvider = CreateServiceProvider();

        // Act - Resolve SettingsDialogViewModel
        var viewModel = _serviceProvider.GetRequiredService<SettingsDialogViewModel>();

        // Assert - ViewModel should be created successfully with all dependencies
        Assert.NotNull(viewModel);
        Assert.NotNull(viewModel.SaveCommand);
        Assert.NotNull(viewModel.CancelCommand);
        Assert.NotNull(viewModel.BrowsePathCommand);
    }



    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}
