using ChronoView.UI.ViewModels;
using ChronoView.Core.FileWatching;
using ChronoView.Core.Analytics;
using ChronoView.Core.Configuration;
using ChronoView.Core.FileOperations;
using ChronoView.Core.ImageProcessing;
using ChronoView.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ChronoView.Tests.UI.ViewModels;

/// <summary>
/// Unit tests for MainWindowViewModel Start command.
/// Tests Requirements: 2.1, 2.3
/// </summary>
public class StartCommandTests
{
    private readonly Mock<IMonitoringOrchestrator> _mockOrchestrator;
    private readonly Mock<IStatisticsService> _mockStatisticsService;
    private readonly Mock<IConfigurationManager> _mockConfigManager;
    private readonly Mock<IFileOperationService> _mockFileOperationService;
    private readonly Mock<IPathManagementService> _mockPathManagementService;
    private readonly Mock<IImageProcessor> _mockImageProcessor;
    private readonly Mock<ILogger<MainWindowViewModel>> _mockLogger;
    private readonly ApplicationConfiguration _testConfig;

    public StartCommandTests()
    {
        _mockOrchestrator = new Mock<IMonitoringOrchestrator>();
        _mockStatisticsService = new Mock<IStatisticsService>();
        _mockConfigManager = new Mock<IConfigurationManager>();
        _mockFileOperationService = new Mock<IFileOperationService>();
        _mockPathManagementService = new Mock<IPathManagementService>();
        _mockImageProcessor = new Mock<IImageProcessor>();
        _mockLogger = new Mock<ILogger<MainWindowViewModel>>();

        _testConfig = new ApplicationConfiguration
        {
            FolderPaths = new Dictionary<string, string>
            {
                { "nir_path", "C:\\Test\\NIR" },
                { "normal_path", "C:\\Test\\Normal" }
            },
            MatchingSettings = new MatchingSettings
            {
                NirPath = "C:\\Test\\NIR",
                NormalPath = "C:\\Test\\Normal"
            }
        };
    }

    private MainWindowViewModel CreateViewModel()
    {
        return new MainWindowViewModel(
            _mockOrchestrator.Object,
            _mockStatisticsService.Object,
            _mockConfigManager.Object,
            _mockFileOperationService.Object,
            _mockPathManagementService.Object,
            _mockImageProcessor.Object,
            _mockLogger.Object);
    }

    [Fact]
    public void StartCommand_CanExecute_WhenNotMonitoring()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        var canExecute = viewModel.StartCommand.CanExecute(null);

        // Assert
        Assert.True(canExecute);
    }

    [Fact]
    public void StartCommand_CannotExecute_WhenAlreadyMonitoring()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.IsMonitoring = true;

        // Act
        var canExecute = viewModel.StartCommand.CanExecute(null);

        // Assert
        Assert.False(canExecute);
    }

    [Fact]
    public async Task StartCommand_CallsOrchestratorStartAsync_WithConfiguration()
    {
        // Arrange
        _mockConfigManager
            .Setup(m => m.LoadConfigurationAsync<ApplicationConfiguration>())
            .ReturnsAsync(_testConfig);

        var viewModel = CreateViewModel();

        // Act
        await viewModel.ExecuteStartAsync();

        // Assert
        _mockOrchestrator.Verify(
            o => o.StartAsync(It.Is<ApplicationConfiguration>(c => c == _testConfig)),
            Times.Once);
    }

    [Fact]
    public async Task StartCommand_CallsStatisticsServiceStartMonitoringAsync_WithConfiguration()
    {
        // Arrange
        _mockConfigManager
            .Setup(m => m.LoadConfigurationAsync<ApplicationConfiguration>())
            .ReturnsAsync(_testConfig);

        var viewModel = CreateViewModel();

        // Act
        await viewModel.ExecuteStartAsync();

        // Assert
        _mockStatisticsService.Verify(
            s => s.StartMonitoringAsync(It.Is<ApplicationConfiguration>(c => c == _testConfig)),
            Times.Once);
    }

    [Fact]
    public async Task StartCommand_SetsIsMonitoringToTrue_OnSuccess()
    {
        // Arrange
        _mockConfigManager
            .Setup(m => m.LoadConfigurationAsync<ApplicationConfiguration>())
            .ReturnsAsync(_testConfig);

        var viewModel = CreateViewModel();
        Assert.False(viewModel.IsMonitoring);

        // Act
        await viewModel.ExecuteStartAsync();

        // Assert
        Assert.True(viewModel.IsMonitoring);
    }

    [Fact]
    public async Task StartCommand_UpdatesStatusMessage_OnSuccess()
    {
        // Arrange
        _mockConfigManager
            .Setup(m => m.LoadConfigurationAsync<ApplicationConfiguration>())
            .ReturnsAsync(_testConfig);

        var viewModel = CreateViewModel();
        Assert.Equal("Ready", viewModel.StatusMessage);

        // Act
        await viewModel.ExecuteStartAsync();

        // Assert
        Assert.Equal("Monitoring...", viewModel.StatusMessage);
    }

    [Fact]
    public async Task StartCommand_LogsStartOperation_OnSuccess()
    {
        // Arrange
        _mockConfigManager
            .Setup(m => m.LoadConfigurationAsync<ApplicationConfiguration>())
            .ReturnsAsync(_testConfig);

        var viewModel = CreateViewModel();

        // Act
        await viewModel.ExecuteStartAsync();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting monitoring")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StartCommand_HandlesOrchestratorException_DisplaysError()
    {
        // Arrange
        _mockConfigManager
            .Setup(m => m.LoadConfigurationAsync<ApplicationConfiguration>())
            .ReturnsAsync(_testConfig);

        _mockOrchestrator
            .Setup(o => o.StartAsync(It.IsAny<ApplicationConfiguration>()))
            .ThrowsAsync(new InvalidOperationException("Orchestrator failed"));

        var viewModel = CreateViewModel();

        // Act
        await viewModel.ExecuteStartAsync();

        // Assert
        Assert.False(viewModel.IsMonitoring); // Should remain false on error
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to start monitoring")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StartCommand_HandlesStatisticsServiceException_DisplaysError()
    {
        // Arrange
        _mockConfigManager
            .Setup(m => m.LoadConfigurationAsync<ApplicationConfiguration>())
            .ReturnsAsync(_testConfig);

        _mockStatisticsService
            .Setup(s => s.StartMonitoringAsync(It.IsAny<ApplicationConfiguration>()))
            .ThrowsAsync(new InvalidOperationException("Statistics service failed"));

        var viewModel = CreateViewModel();

        // Act
        await viewModel.ExecuteStartAsync();

        // Assert
        Assert.False(viewModel.IsMonitoring); // Should remain false on error
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to start monitoring")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StartCommand_HandlesConfigurationLoadException_DisplaysError()
    {
        // Arrange
        _mockConfigManager
            .Setup(m => m.LoadConfigurationAsync<ApplicationConfiguration>())
            .ThrowsAsync(new ConfigurationException("Failed to load configuration"));

        var viewModel = CreateViewModel();

        // Act
        await viewModel.ExecuteStartAsync();

        // Assert
        Assert.False(viewModel.IsMonitoring); // Should remain false on error
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to start monitoring")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StartCommand_UpdatesCommandState_StartDisabledStopEnabled()
    {
        // Arrange
        _mockConfigManager
            .Setup(m => m.LoadConfigurationAsync<ApplicationConfiguration>())
            .ReturnsAsync(_testConfig);

        var viewModel = CreateViewModel();

        // Initial state
        Assert.True(viewModel.StartCommand.CanExecute(null));
        Assert.False(viewModel.StopCommand.CanExecute(null));

        // Act
        await viewModel.ExecuteStartAsync();

        // Assert
        Assert.False(viewModel.StartCommand.CanExecute(null)); // Start should be disabled
        Assert.True(viewModel.StopCommand.CanExecute(null));   // Stop should be enabled
    }

    [Fact]
    public async Task StartCommand_AddsLogMessage_OnSuccess()
    {
        // Arrange
        _mockConfigManager
            .Setup(m => m.LoadConfigurationAsync<ApplicationConfiguration>())
            .ReturnsAsync(_testConfig);

        var viewModel = CreateViewModel();
        var initialLogCount = viewModel.LogMessages.Count;

        // Act
        await viewModel.ExecuteStartAsync();

        // Assert
        Assert.True(viewModel.LogMessages.Count > initialLogCount);
        Assert.Contains(viewModel.LogMessages, 
            m => m.Message.Contains("Monitoring started") && m.Severity == LogSeverity.Info);
    }

    [Fact]
    public async Task StartCommand_AddsErrorLogMessage_OnFailure()
    {
        // Arrange
        _mockConfigManager
            .Setup(m => m.LoadConfigurationAsync<ApplicationConfiguration>())
            .ThrowsAsync(new ConfigurationException("Configuration error"));

        var viewModel = CreateViewModel();

        // Act
        await viewModel.ExecuteStartAsync();

        // Assert
        Assert.Contains(viewModel.LogMessages, 
            m => m.Severity == LogSeverity.Error && m.Message.Contains("Failed to start monitoring"));
    }

    [Fact]
    public async Task StartCommand_DoesNotCallServices_WhenAlreadyMonitoring()
    {
        // Arrange
        _mockConfigManager
            .Setup(m => m.LoadConfigurationAsync<ApplicationConfiguration>())
            .ReturnsAsync(_testConfig);

        var viewModel = CreateViewModel();
        await viewModel.ExecuteStartAsync(); // Start once

        // Reset mocks to verify second call doesn't happen
        _mockOrchestrator.Reset();
        _mockStatisticsService.Reset();

        // Act - Try to start again
        await viewModel.ExecuteStartAsync();

        // Assert - Services should not be called again
        _mockOrchestrator.Verify(
            o => o.StartAsync(It.IsAny<ApplicationConfiguration>()),
            Times.Never);
        _mockStatisticsService.Verify(
            s => s.StartMonitoringAsync(It.IsAny<ApplicationConfiguration>()),
            Times.Never);
    }
}
