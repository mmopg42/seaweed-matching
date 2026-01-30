using ChronoView.Core.Analytics;
using ChronoView.Core.Configuration;
using ChronoView.Core.FileMatching;
using ChronoView.Core.FileWatching;
using ChronoView.Core.Nir;
using ChronoView.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ChronoView.Tests.Core.FileWatching;

public class MonitoringOrchestratorConstructionTests
{
    [Fact]
    public void CanConstructMonitoringOrchestrator_WithMocks()
    {
        var matcher = new Mock<IFileGroupMatcher>();
        var watcher = new Mock<IFileWatcher>();
        var logger = new Mock<ILogger<MonitoringOrchestrator>>();
        var nirResolver = new Mock<INirFileResolver>();
        var timestampCache = new Mock<ITimestampCache>();
        var initialScanner = new Mock<IInitialScanner>();
        var groupManager = new Mock<IGroupManager>();
        var imageCapture = new Mock<IImageCaptureService>();
        var eventProcessor = new Mock<IEventProcessor>();
        var configManager = new Mock<IConfigurationManager>();
        var abnormalDetector = new Mock<IAbnormalDetector>();

        var orchestrator = new MonitoringOrchestrator(
            matcher.Object,
            watcher.Object,
            logger.Object,
            nirResolver.Object,
            timestampCache.Object,
            initialScanner.Object,
            groupManager.Object,
            imageCapture.Object,
            eventProcessor.Object,
            configManager.Object,
            abnormalDetector.Object,
            uiLog: null);

        Assert.NotNull(orchestrator);
    }
}




