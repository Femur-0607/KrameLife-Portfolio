using System.Collections.Generic;

namespace TownLife.Dialogue
{
    /// <summary>
    /// CSV 한 행의 값을 헤더 이름으로 조회합니다.
    /// </summary>
    public sealed class DialogueCsvRow
    {
        private readonly Dictionary<string, string> values;

        public DialogueCsvRow(Dictionary<string, string> values)
        {
            this.values = values;
        }

        /// <summary>
        /// 헤더 이름에 해당하는 셀 값을 반환합니다. 없으면 빈 문자열을 반환합니다.
        /// </summary>
        public string Get(string headerName)
        {
            if (string.IsNullOrWhiteSpace(headerName))
            {
                return string.Empty;
            }

            return values.TryGetValue(headerName, out string value) ? value : string.Empty;
        }
    }
}
