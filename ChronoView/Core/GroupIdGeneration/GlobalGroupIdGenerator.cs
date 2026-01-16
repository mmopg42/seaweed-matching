using System.Threading;

namespace ChronoView.Core.GroupIdGeneration
{
    /// <summary>
    /// 전역 그룹 ID 생성기 (Legacy Support).
    /// 라인 구분 없이 전역적으로 순차 증가하는 ID를 생성합니다.
    /// 포맷: group_001, group_002, group_003, ...
    /// </summary>
    public class GlobalGroupIdGenerator : IGroupIdGenerator
    {
        private int _nextGroupId = 0;

        /// <summary>
        /// 다음 그룹 ID를 생성합니다.
        /// </summary>
        /// <param name="lineNumber">라인 번호 (이 구현에서는 무시됨)</param>
        /// <returns>group_XXX 형태의 ID (1-based, 3자리 패딩)</returns>
        public string GenerateNextId(int lineNumber)
        {
            var id = Interlocked.Increment(ref _nextGroupId);
            return $"group_{id:D3}";
        }

        /// <summary>
        /// ID 카운터를 0으로 초기화합니다.
        /// </summary>
        public void Reset()
        {
            Interlocked.Exchange(ref _nextGroupId, 0);
        }
    }
}



