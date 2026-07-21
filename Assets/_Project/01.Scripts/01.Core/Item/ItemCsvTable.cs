using System.Collections.Generic;

namespace TownLife.Core
{
    /// <summary>
    /// CSV 헤더와 행 목록을 함께 보관하는 아이템 CSV 테이블입니다.
    /// </summary>
    public sealed class ItemCsvTable
    {
        /// <summary>
        /// CSV 테이블을 만듭니다.
        /// </summary>
        public ItemCsvTable(IReadOnlyList<string> headers, IReadOnlyList<ItemCsvRow> rows)
        {
            // 파서가 읽은 헤더와 행을 그대로 보관해 mapper 단계에서 사용합니다.
            Headers = headers;
            Rows = rows;
        }

        /// <summary>
        /// CSV 첫 줄에서 읽은 헤더 목록입니다.
        /// </summary>
        public IReadOnlyList<string> Headers { get; }

        /// <summary>
        /// 헤더 이름으로 값을 조회할 수 있는 CSV 데이터 행 목록입니다.
        /// </summary>
        public IReadOnlyList<ItemCsvRow> Rows { get; }
    }
}
