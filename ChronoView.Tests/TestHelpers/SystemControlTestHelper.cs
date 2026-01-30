using ChronoView.Core.Analytics;
using ChronoView.Core.Configuration;
using ChronoView.Core.FileWatching;
using ChronoView.Core.ProgramLaunching;
using ChronoView.Models;
using ChronoView.UI.ViewModels;
using Microsoft.Extensions.Logging;
using Moq;

namespace ChronoView.Tests.TestHelpers;

public static class SystemControlTestHelper
{
    public static (SystemControlViewModel Vm, Mock<IMonitoringOrchestrator> Orchestrator, Mock<IStatisticsService> Stats, Mock<IConfigurationManager> ConfigManager)
        CreateSystemControlViewModel()
    {
        var orchestrator = new Mock<IMonitoringOrchestrator>();
        var stats = new Mock<IStatisticsService>();
        var configManager = new Mock<IConfigurationManager>();

        var config = new ApplicationConfiguration
        {
            ExternalProgramSettings = new ExternalProgramSettings(),
            MatchingSettings = new MatchingSettings(),
            WorkflowSettings = new WorkflowSettings(),
            ImageSettings = new ImageSettings()
        };

        configManager.Setup(m => m.LoadConfiguration<ApplicationConfiguration>()).Returns(config);
        configManager.Setup(m => m.LoadConfigurationAsync<ApplicationConfiguration>()).ReturnsAsync(config);

        orchestrator.Setup(m => m.StartAsync(It.IsAny<ApplicationConfiguration>())).Returns(Task.CompletedTask);
        orchestrator.Setup(m => m.StopAsync()).Returns(Task.CompletedTask);
        orchestrator.Setup(m => m.RefreshAsync()).Returns(Task.CompletedTask);

        stats.Setup(m => m.StartMonitoringAsync(It.IsAny<ApplicationConfiguration>())).Returns(Task.CompletedTask);
        stats.Setup(m => m.StopMonitoringAsync()).Returns(Task.CompletedTask);
        stats.Setup(m => m.ReloadStatsAsync(It.IsAny<ApplicationConfiguration>())).Returns(Task.CompletedTask);

        var genLauncher = new GeneralCameraLauncher(Mock.Of<ILogger<GeneralCameraLauncher>>(), configManager.Object);
        var nir1Launcher = new NirCameraLauncher(Mock.Of<ILogger<NirCameraLauncher>>(), configManager.Object);
        var nir2Launcher = new Nir2CameraLauncher(Mock.Of<ILogger<Nir2CameraLauncher>>(), configManager.Object);
        var nirFilter = new NirFilteringService(Mock.Of<ILogger<NirFilteringService>>(), configManager.Object);

        var vm = new SystemControlViewModel(
            orchestrator.Object,
            stats.Object,
            configManager.Object,
            genLauncher,
            nir1Launcher,
            nir2Launcher,
            nirFilter,
            Mock.Of<ILogger<SystemControlViewModel>>());

        return (vm, orchestrator, stats, configManager);
    }
}




