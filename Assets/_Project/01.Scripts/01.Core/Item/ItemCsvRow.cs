using System.Collections.Generic;

namespace TownLife.Core
{
    /// <summary>
    /// CSV 한 행의 값을 헤더 이름으로 조회합니다.
    /// </summary>
    public sealed class ItemCsvRow
    {
        private readonly Dictionary<string, string> values;

        /// <summary>
        /// 헤더 이름과 셀 값을 받아 CSV 행을 만듭니다.
        /// </summary>
        public ItemCsvRow(Dictionary<string, string> values)
        {
            // mapper가 헤더 이름으로 셀 값을 꺼낼 수 있도록 딕셔너리를 보관합니다.
            this.values = values;
        }

        /// <summary>
        /// 지정한 헤더의 셀 값을 가져옵니다. 값이 없으면 빈 문자열을 반환합니다.
        /// </summary>
        public string Get(string header)
        {
            // 누락된 선택 컬럼도 안전하게 처리하기 위해 빈 문자열을 기본값으로 사용합니다.
            return values.TryGetValue(header, out string value) ? value.Trim() : string.Empty;
        }
    }
}
