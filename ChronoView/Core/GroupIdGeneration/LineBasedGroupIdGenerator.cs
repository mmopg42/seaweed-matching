using System.Collections.Generic;

namespace ChronoView.Core.GroupIdGeneration
{
    /// <summary>
    /// 라인별 그룹 ID 생성기 (New Requirement).
    /// 각 라인마다 독립적인 ID 시퀀스를 유지합니다.
    /// 포맷: line1_001, line1_002, ... / line2_001, line2_002, ...
    /// </summary>
    public class LineBasedGroupIdGenerator : IGroupIdGenerator
    {
        private readonly object _lock = new object();
        private readonly Dictionary<int, int> _counters = new Dictionary<int, int>();

        /// <summary>
        /// 다음 그룹 ID를 생성합니다.
        /// </summary>
        /// <param name="lineNumber">라인 번호 (1 또는 2)</param>
        /// <returns>lineX_XXX 형태의 ID (1-based, 3자리 패딩)</returns>
        public string GenerateNextId(int lineNumber)
        {
            lock (_lock)
            {
                if (!_counters.ContainsKey(lineNumber))
                {
                    _counters[lineNumber] = 0;
                }

                var id = ++_counters[lineNumber];
                return $"line{lineNumber}_{id:D3}";
            }
        }

        /// <summary>
        /// 모든 라인의 카운터를 초기화합니다.
        /// </summary>
        public void Reset()
        {
            lock (_lock)
            {
                _counters.Clear();
            }
        }
    }
}



