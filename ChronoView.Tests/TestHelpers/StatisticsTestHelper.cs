namespace ChronoView.Tests.TestHelpers;

public static class StatisticsTestHelper
{
    /// <summary>
    /// 테스트에서만 사용하는 MatchRate 계산(0~100).
    /// 현재 테스트 스위트에서의 의미는 "WithNir / TotalGroups * 100"이다.
    /// (메인 코드에 MatchRate 속성이 없어도 테스트가 독립적으로 계산한다.)
    /// </summary>
    public static double CalcMatchRate(int totalGroups, int withNir)
    {
        if (totalGroups <= 0) return 0.0;
        return withNir * 100.0 / totalGroups;
    }
}


