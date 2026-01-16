using ChronoView.Tests.TestHelpers;
using Moq;
using Xunit;

namespace ChronoView.Tests.UI.ViewModels;

/// <summary>
/// 최신 구조: Stop은 SystemControlViewModel.StopMonitoringAsync()를 통해 수행된다.
/// </summary>
public class StopCommandTests
{
    [Fact]
    public async Task StopMonitoringAsync_CallsDependencies_AndSetsIsMonitoringFalse()
    {
        var (vm, orchestrator, stats, _) = SystemControlTestHelper.CreateSystemControlViewModel();

        await vm.StartMonitoringAsync();
        Assert.True(vm.IsMonitoring);

        await vm.StopMonitoringAsync();

        Assert.False(vm.IsMonitoring);
        orchestrator.Verify(o => o.StopAsync(), Times.Once);
        stats.Verify(s => s.StopMonitoringAsync(), Times.Once);
    }
}


