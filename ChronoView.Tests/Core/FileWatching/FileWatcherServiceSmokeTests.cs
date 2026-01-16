using ChronoView.Core.FileWatching;
using ChronoView.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ChronoView.Tests.Core.FileWatching;

public class FileWatcherServiceSmokeTests
{
    [Fact]
    public async Task StartThenStop_DoesNotThrow()
    {
        var logger = new Mock<ILogger<FileWatcherService>>();
        var config = new ApplicationConfiguration { WorkflowSettings = new WorkflowSettings() };

        var watcher = new FileWatcherService(logger.Object, config);

        var dir = Path.Combine(Path.GetTempPath(), "ChronoView_WatcherSmoke_" + Guid.NewGuid());
        Directory.CreateDirectory(dir);

        try
        {
            await watcher.StartWatchingAsync(new[] { dir }, new FileWatcherOptions { EnablePolling = false });
            await watcher.StopWatchingAsync();
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch { }
        }
    }
}


