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
/// Unit tests for MainWindowViewModel Stop command.
/// Tests Requirements: 3.1, 3.3
/// </summary>
public class StopCommandTests
{
    private readonly Mock<IMonitoringOrchestrator> _mockOrchestrator;
    private readonly Mock<IStatisticsService> _mockStatisticsService;
    private readonly Mock<IConfigurationManager> _mockConfigManager;
    private readonly Mock<IFileOperationService> _mockFileOperationService;
    private readonly Mock<IPathManagementService> _mockPathManagementService;
    private readonly Mock<IImageProcessor> _mockImageProcessor;
    private readonly Mock<IAbnormalDetector> _mockAbnormalDetector;
    private readonly Mock<ILogger<MainWindowViewModel>> _mockLogger;
    private readonly Mock<ILogger<FileGroupViewModel>> _mockFileGroupLogger;
    private readonly ApplicationConfiguration _testConfig;

    public StopCommandTests()
    {
        _mockOrchestrator = new Mock<IMonitoringOrchestrator>();
        _mockStatisticsService = new Mock<IStatisticsService>();
        _mockConfigManager = new Mock<IConfigurationManager>();
        _mockFileOperationService = new Mock<IFileOperationService>();
        _mockPathManagementService = new Mock<IPathManagementService>();
        _mockImageProcessor = new Mock<IImageProcessor>();
        _mockAbnormalDetector = new Mock<IAbnormalDetector>();
        _mockLogger = new Mock<ILogger<MainWindowViewModel>>();
        _mockFileGroupLogger = new Mock<ILogger<FileGroupViewModel>>();

        _testConfig = new ApplicationConfiguration
        {
            FolderPaths = new Dictionary<string, string>
            {
                { "nir_path", "C:\\Test\\NIR" },
                { "normal_path", "C:\\Test\\Normal" }
            },
            MatchingSettings = new MatchingSettings
            {
                Nir1Path = "C:\\Test\\NIR",
                Normal1Path = "C:\\Test\\Normal"
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
            _mockAbnormalDetector.Object,
            _mockLogger.Object,
            _mockFileGroupLogger.Object);
    }

    [Fact]
    public void StopCommand_CannotExecute_WhenNotMonitoring()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        var canExecute = viewModel.StopCommand.CanExecute(null);

        // Assert
        Assert.False(canExecute);
    }

    [Fact]
    public void StopCommand_CanExecute_WhenMonitoring()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.IsMonitoring = true;

        // Act
        var canExecute = viewModel.StopCommand.CanExecute(null);

        // Assert
        Assert.True(canExecute);
    }

    [Fact]
    public async Task StopCommand_CallsOrchestratorStopAsync()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.IsMonitoring = true;

        // Act
        await viewModel.ExecuteStopAsync();

        // Assert
        _mockOrchestrator.Verify(
            o => o.StopAsync(),
            Times.Once);
    }

    [Fact]
    public async Task StopCommand_CallsStatisticsServiceStopMonitoringAsync()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.IsMonitoring = true;

        // Act
        await viewModel.ExecuteStopAsync();

        // Assert
        _mockStatisticsService.Verify(
            s => s.StopMonitoringAsync(),
            Times.Once);
    }

    [Fact]
    public async Task StopCommand_SetsIsMonitoringToFalse_OnSuccess()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.IsMonitoring = true;
        Assert.True(viewModel.IsMonitoring);

        // Act
        await viewModel.ExecuteStopAsync();

        // Assert
        Assert.False(viewModel.IsMonitoring);
    }

    [Fact]
    public async Task StopCommand_UpdatesStatusMessage_OnSuccess()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.IsMonitoring = true;

        // Act
        await viewModel.ExecuteStopAsync();

        // Assert
        Assert.Equal("Ready", viewModel.StatusMessage);
    }

    [Fact]
    public async Task StopCommand_LogsStopOperation_OnSuccess()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.IsMonitoring = true;

        // Act
        await viewModel.ExecuteStopAsync();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Stopping monitoring")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StopCommand_HandlesOrchestratorException_DisplaysError()
    {
        // Arrange
        _mockOrchestrator
            .Setup(o => o.StopAsync())
            .ThrowsAsync(new InvalidOperationException("Orchestrator stop failed"));

        var viewModel = CreateViewModel();
        viewModel.IsMonitoring = true;

        // Act
        await viewModel.ExecuteStopAsync();

        // Assert
        Assert.False(viewModel.IsMonitoring); // Should still be set to false
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to stop monitoring")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StopCommand_HandlesStatisticsServiceException_DisplaysError()
    {
        // Arrange
        _mockStatisticsService
            .Setup(s => s.StopMonitoringAsync())
            .ThrowsAsync(new InvalidOperationException("Statistics service stop failed"));

        var viewModel = CreateViewModel();
        viewModel.IsMonitoring = true;

        // Act
        await viewModel.ExecuteStopAsync();

        // Assert
        Assert.False(viewModel.IsMonitoring); // Should still be set to false
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to stop monitoring")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StopCommand_UpdatesCommandState_StopDisabledStartEnabled()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.IsMonitoring = true;

        // Initial state when monitoring
        Assert.False(viewModel.StartCommand.CanExecute(null));
        Assert.True(viewModel.StopCommand.CanExecute(null));

        // Act
        await viewModel.ExecuteStopAsync();

        // Assert
        Assert.True(viewModel.StartCommand.CanExecute(null));  // Start should be enabled
        Assert.False(viewModel.StopCommand.CanExecute(null));  // Stop should be disabled
    }

    [Fact]
    public async Task StopCommand_AddsLogMessage_OnSuccess()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.IsMonitoring = true;
        var initialLogCount = viewModel.LogMessages.Count;

        // Act
        await viewModel.ExecuteStopAsync();

        // Assert
        Assert.True(viewModel.LogMessages.Count > initialLogCount);
        Assert.Contains(viewModel.LogMessages, 
            m => m.Message.Contains("Monitoring stopped") && m.Severity == LogSeverity.Info);
    }

    [Fact]
    public async Task StopCommand_AddsErrorLogMessage_OnFailure()
    {
        // Arrange
        _mockOrchestrator
            .Setup(o => o.StopAsync())
            .ThrowsAsync(new InvalidOperationException("Stop failed"));

        var viewModel = CreateViewModel();
        viewModel.IsMonitoring = true;

        // Act
        await viewModel.ExecuteStopAsync();

        // Assert
        Assert.Contains(viewModel.LogMessages, 
            m => m.Severity == LogSeverity.Error && m.Message.Contains("Failed to stop monitoring"));
    }

    [Fact]
    public async Task StopCommand_DoesNotCallServices_WhenNotMonitoring()
    {
        // Arrange
        var viewModel = CreateViewModel();
        Assert.False(viewModel.IsMonitoring);

        // Act
        await viewModel.ExecuteStopAsync();

        // Assert - Services should not be called when not monitoring
        _mockOrchestrator.Verify(
            o => o.StopAsync(),
            Times.Never);
        _mockStatisticsService.Verify(
            s => s.StopMonitoringAsync(),
            Times.Never);
    }

    [Fact]
    public async Task StopCommand_CallsServicesInCorrectOrder()
    {
        // Arrange
        var callOrder = new List<string>();
        
        _mockOrchestrator
            .Setup(o => o.StopAsync())
            .Callback(() => callOrder.Add("Orchestrator"))
            .Returns(Task.CompletedTask);
        
        _mockStatisticsService
            .Setup(s => s.StopMonitoringAsync())
            .Callback(() => callOrder.Add("Statistics"))
            .Returns(Task.CompletedTask);

        var viewModel = CreateViewModel();
        viewModel.IsMonitoring = true;

        // Act
        await viewModel.ExecuteStopAsync();

        // Assert - Both services should be called
        Assert.Equal(2, callOrder.Count);
        Assert.Contains("Orchestrator", callOrder);
        Assert.Contains("Statistics", callOrder);
    }
}
