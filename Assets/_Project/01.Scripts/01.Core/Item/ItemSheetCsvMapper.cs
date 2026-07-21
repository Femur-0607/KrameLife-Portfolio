namespace TownLife.Core
{
    /// <summary>
    /// CSV 테이블을 아이템 시트 데이터로 변환합니다.
    /// category는 CSV 컬럼이 아니라 가져온 탭의 카테고리 값을 사용합니다.
    /// </summary>
    public static class ItemSheetCsvMapper
    {
        private const string IdHeader = "id";
        private const string DisplayNameHeader = "displayName";
        private const string DescriptionHeader = "description";
        private const string BaseSellPriceHeader = "baseSellPrice";
        private const string MaxStackHeader = "maxStack";
        private const string IconAddressHeader = "iconAddress";

        /// <summary>
        /// CSV 텍스트를 파싱한 뒤 지정한 카테고리의 아이템 시트 데이터로 변환합니다.
        /// </summary>
        public static ItemSheetData FromCsvText(string csvText, ItemCategory category)
        {
            // Google Sheets에서 내려받은 CSV 원문을 헤더 기반 테이블로 변환합니다.
            ItemCsvTable table = ItemCsvParser.Parse(csvText);

            // 검증과 SO 생성 단계에서 사용할 아이템 데이터 묶음을 준비합니다.
            ItemSheetData data = new();

            // 각 CSV 행을 ItemSheetRow로 옮겨 담고, 탭 category를 함께 넣습니다.
            foreach (ItemCsvRow row in table.Rows)
            {
                data.Add(MapRow(row, category));
            }

            return data;
        }

        /// <summary>
        /// CSV 한 행을 아이템 시트 행 데이터로 변환합니다.
        /// </summary>
        private static ItemSheetRow MapRow(ItemCsvRow row, ItemCategory category)
        {
            // 헤더 이름으로 셀을 읽기 때문에 시트 컬럼 순서가 바뀌어도 동작합니다.
            return new ItemSheetRow
            {
                Id = row.Get(IdHeader),
                DisplayName = row.Get(DisplayNameHeader),
                Description = row.Get(DescriptionHeader),
                Category = category,
                BaseSellPrice = row.Get(BaseSellPriceHeader),
                MaxStack = row.Get(MaxStackHeader),
                IconAddress = row.Get(IconAddressHeader),
            };
        }
    }
}
