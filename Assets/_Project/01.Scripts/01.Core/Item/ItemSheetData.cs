using System.Collections.Generic;

namespace TownLife.Core
{
    /// <summary>
    /// 카테고리별 시트에서 읽은 아이템 행 목록을 보관합니다.
    /// </summary>
    public sealed class ItemSheetData
    {
        private readonly List<ItemSheetRow> items = new();

        /// <summary>
        /// 검증과 SO 생성에 사용할 아이템 행 목록입니다.
        /// </summary>
        public IReadOnlyList<ItemSheetRow> Items => items;

        /// <summary>
        /// 시트에서 읽은 아이템 행을 목록에 추가합니다.
        /// </summary>
        public void Add(ItemSheetRow row)
        {
            // mapper가 만든 행을 검증 전 원본 목록에 누적합니다.
            items.Add(row);
        }

        /// <summary>
        /// 다른 시트에서 읽은 아이템 행들을 현재 데이터에 합칩니다.
        /// </summary>
        public void AddRange(ItemSheetData data)
        {
            // 여러 카테고리 탭을 하나의 Import 결과로 묶습니다.
            foreach (ItemSheetRow row in data.Items)
            {
                items.Add(row);
            }
        }
    }
}
