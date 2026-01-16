using ChronoView.Tests.TestHelpers;
using Xunit;

namespace ChronoView.Tests.UI.ViewModels;

/// <summary>
/// 최신 구조에서는 MainWindowViewModel이 Dashboard/Control/Operations로 분리되어 있으므로,
/// 레거시 "단일 ViewModel 상태/통계 API" 테스트는 유지보수 비용이 커서 제거하고
/// 핵심 동작(모니터링 상태 전환)만 최소 단위로 검증한다.
/// </summary>
public class ViewModelTests
{
    [Fact]
    public async Task SystemControlViewModel_StartThenStop_TogglesIsMonitoring()
    {
        var (vm, _, _, _) = SystemControlTestHelper.CreateSystemControlViewModel();

        Assert.False(vm.IsMonitoring);
        await vm.StartMonitoringAsync();
        Assert.True(vm.IsMonitoring);
        await vm.StopMonitoringAsync();
        Assert.False(vm.IsMonitoring);
    }
}


