using ChronoView.Tests.TestHelpers;
using Moq;
using Xunit;

namespace ChronoView.Tests.UI.ViewModels;

/// <summary>
/// 최신 구조: Start/Stop은 MainWindowViewModel이 아니라 SystemControlViewModel이 담당한다.
/// </summary>
public class StartCommandTests
{
    [Fact]
    public void StartCommand_CanExecute_Initially()
    {
        var (vm, _, _, _) = SystemControlTestHelper.CreateSystemControlViewModel();
        Assert.True(vm.StartCommand.CanExecute(null));
        Assert.False(vm.StopCommand.CanExecute(null));
    }

    [Fact]
    public async Task StartMonitoringAsync_CallsDependencies_AndSetsIsMonitoring()
    {
        var (vm, orchestrator, stats, _) = SystemControlTestHelper.CreateSystemControlViewModel();

        await vm.StartMonitoringAsync();

        Assert.True(vm.IsMonitoring);
        orchestrator.Verify(o => o.StartAsync(It.IsAny<ChronoView.Models.ApplicationConfiguration>()), Times.Once);
        stats.Verify(s => s.StartMonitoringAsync(It.IsAny<ChronoView.Models.ApplicationConfiguration>()), Times.Once);
    }
}




