namespace ChronoView.Core.GroupIdGeneration
{
    /// <summary>
    /// 그룹 ID 생성을 위한 전략 인터페이스.
    /// Strategy Pattern을 사용하여 다양한 ID 생성 방식을 지원합니다.
    /// </summary>
    public interface IGroupIdGenerator
    {
        /// <summary>
        /// 다음 그룹 ID를 생성합니다.
        /// </summary>
        /// <param name="lineNumber">라인 번호 (1 또는 2). 전역 모드에서는 무시됨.</param>
        /// <returns>생성된 그룹 ID (예: "line1_001" 또는 "group_001")</returns>
        string GenerateNextId(int lineNumber);

        /// <summary>
        /// ID 카운터를 초기화합니다.
        /// 새로운 스캔이나 모니터링 세션 시작 시 호출됩니다.
        /// </summary>
        void Reset();
    }
}





