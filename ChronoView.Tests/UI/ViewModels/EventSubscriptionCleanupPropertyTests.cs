using ChronoView.Tests.TestHelpers;
using Xunit;

namespace ChronoView.Tests.UI.ViewModels;

/// <summary>
/// WPF(Application.Current) 의존이 있는 레거시 MainWindowViewModel 테스트 대신,
/// WPF 없이도 검증 가능한 SystemControlViewModel의 기본 수명주기를 확인한다.
/// </summary>
public class EventSubscriptionCleanupPropertyTests
{
    [Fact]
    public void DisposingSystemControlViewModel_DoesNotThrow()
    {
        var (vm, _, _, _) = SystemControlTestHelper.CreateSystemControlViewModel();
        vm.Dispose();
    }
}


