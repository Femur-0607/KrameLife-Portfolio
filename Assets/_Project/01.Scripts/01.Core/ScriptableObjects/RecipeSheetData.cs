using System.Collections.Generic;

namespace TownLife.Core
{
    /// <summary>
    /// 제작/요리 시트에서 읽은 레시피 행 목록을 보관합니다.
    /// </summary>
    public sealed class RecipeSheetData
    {
        private readonly List<RecipeSheetRow> recipes = new();

        public IReadOnlyList<RecipeSheetRow> Recipes => recipes;

        public void Add(RecipeSheetRow row)
        {
            recipes.Add(row);
        }

        public void AddRange(RecipeSheetData data)
        {
            foreach (RecipeSheetRow row in data.Recipes)
            {
                recipes.Add(row);
            }
        }
    }
}
