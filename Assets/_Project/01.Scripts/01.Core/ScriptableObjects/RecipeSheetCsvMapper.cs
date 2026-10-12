namespace TownLife.Core
{
    /// <summary>
    /// CSV 테이블을 레시피 시트 데이터로 변환합니다.
    /// recipeKind는 CSV 컬럼이 아니라 가져온 탭 설정값을 사용합니다.
    /// </summary>
    public static class RecipeSheetCsvMapper
    {
        private const string IdHeader = "id";
        private const string DisplayNameHeader = "displayName";
        private const string DescriptionHeader = "description";
        private const string StationIdHeader = "stationId";
        private const string OutputItemIdHeader = "outputItemId";
        private const string OutputCountHeader = "outputCount";
        private const string GoldCostHeader = "goldCost";
        private const string StaminaCostHeader = "staminaCost";
        private const string IconAddressHeader = "iconAddress";
        private const string UnlockNodeIdHeader = "unlockNodeId";
        private const string EnabledHeader = "enabled";

        public static RecipeSheetData FromCsvText(string csvText, RecipeKind recipeKind)
        {
            ItemCsvTable table = ItemCsvParser.Parse(csvText);
            RecipeSheetData data = new();

            foreach (ItemCsvRow row in table.Rows)
            {
                if (IsBlankRow(row))
                {
                    continue;
                }

                data.Add(MapRow(row, recipeKind));
            }

            return data;
        }

        private static RecipeSheetRow MapRow(ItemCsvRow row, RecipeKind recipeKind)
        {
            RecipeSheetRow recipe = new()
            {
                Id = row.Get(IdHeader),
                DisplayName = row.Get(DisplayNameHeader),
                Description = row.Get(DescriptionHeader),
                RecipeKind = recipeKind,
                StationId = row.Get(StationIdHeader),
                OutputItemId = row.Get(OutputItemIdHeader),
                OutputCount = row.Get(OutputCountHeader),
                GoldCost = recipeKind == RecipeKind.Craft ? row.Get(GoldCostHeader) : "0",
                StaminaCost = recipeKind == RecipeKind.Craft ? row.Get(StaminaCostHeader) : "0",
                IconAddress = row.Get(IconAddressHeader),
                UnlockNodeId = row.Get(UnlockNodeIdHeader),
                Enabled = row.Get(EnabledHeader),
            };

            recipe.AddInput(row.Get("input1ItemId"), row.Get("input1Count"));
            recipe.AddInput(row.Get("input2ItemId"), row.Get("input2Count"));

            if (recipeKind == RecipeKind.Cooking)
            {
                recipe.AddInput(row.Get("input3ItemId"), row.Get("input3Count"));
            }

            return recipe;
        }

        private static bool IsBlankRow(ItemCsvRow row)
        {
            return string.IsNullOrWhiteSpace(row.Get(IdHeader))
                && string.IsNullOrWhiteSpace(row.Get(DisplayNameHeader))
                && string.IsNullOrWhiteSpace(row.Get(OutputItemIdHeader));
        }
    }
}
