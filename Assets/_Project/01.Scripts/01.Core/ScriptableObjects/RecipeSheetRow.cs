using System.Collections.Generic;

namespace TownLife.Core
{
    /// <summary>
    /// Google Sheets 레시피 탭의 한 행을 코드에서 다루기 쉬운 형태로 보관합니다.
    /// </summary>
    public sealed class RecipeSheetRow
    {
        private readonly List<ItemStack> inputs = new();

        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public RecipeKind RecipeKind { get; set; } = RecipeKind.Craft;
        public string StationId { get; set; } = string.Empty;
        public IReadOnlyList<ItemStack> Inputs => inputs;
        public string OutputItemId { get; set; } = string.Empty;
        public string OutputCount { get; set; } = string.Empty;
        public string GoldCost { get; set; } = string.Empty;
        public string StaminaCost { get; set; } = string.Empty;
        public string IconAddress { get; set; } = string.Empty;
        public string UnlockNodeId { get; set; } = string.Empty;
        public string Enabled { get; set; } = string.Empty;

        public bool IsEnabled => string.IsNullOrWhiteSpace(Enabled) || Enabled.Equals("TRUE", System.StringComparison.OrdinalIgnoreCase);

        public void AddInput(string itemId, string countText)
        {
            if (string.IsNullOrWhiteSpace(itemId) && string.IsNullOrWhiteSpace(countText))
            {
                return;
            }

            int count = int.TryParse(countText, out int parsedCount) ? parsedCount : 0;
            inputs.Add(new ItemStack(itemId, count));
        }
    }
}
