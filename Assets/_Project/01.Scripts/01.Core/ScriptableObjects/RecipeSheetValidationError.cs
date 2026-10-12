namespace TownLife.Core
{
    /// <summary>
    /// 레시피 시트 검증 중 발견한 오류 한 건을 표현합니다.
    /// </summary>
    public sealed class RecipeSheetValidationError
    {
        public RecipeSheetValidationError(string sheetName, string recipeId, string message)
        {
            SheetName = sheetName;
            RecipeId = recipeId;
            Message = message;
        }

        public string SheetName { get; }
        public string RecipeId { get; }
        public string Message { get; }
    }
}
