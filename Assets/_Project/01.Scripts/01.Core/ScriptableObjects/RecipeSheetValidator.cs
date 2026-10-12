using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TownLife.Core
{
    /// <summary>
    /// Google Sheets에서 가져온 레시피 행이 SO로 생성 가능한지 검증합니다.
    /// </summary>
    public static class RecipeSheetValidator
    {
        private const string IdPattern = "^[a-z0-9_.]+$";

        public static IReadOnlyList<RecipeSheetValidationError> Validate(RecipeSheetData data)
        {
            List<RecipeSheetValidationError> errors = new();
            HashSet<string> usedIds = new();

            foreach (RecipeSheetRow row in data.Recipes)
            {
                ValidateRow(row, usedIds, errors);
            }

            return errors;
        }

        private static void ValidateRow(
            RecipeSheetRow row,
            ISet<string> usedIds,
            ICollection<RecipeSheetValidationError> errors)
        {
            string sheetName = ToSheetName(row.RecipeKind);
            RequireValue(row.Id, sheetName, row.Id, "id", errors);
            if (string.IsNullOrWhiteSpace(row.Id))
            {
                return;
            }

            if (!usedIds.Add(row.Id))
            {
                errors.Add(new RecipeSheetValidationError(sheetName, row.Id, "중복된 id입니다."));
            }

            if (!Regex.IsMatch(row.Id, IdPattern) || !row.Id.StartsWith("recipe."))
            {
                errors.Add(new RecipeSheetValidationError(sheetName, row.Id, "id는 recipe.로 시작하고 소문자 영어, 숫자, 언더스코어, 점만 사용할 수 있습니다."));
            }

            ValidateEnabled(row.Enabled, sheetName, row.Id, errors);
            if (!row.IsEnabled)
            {
                return;
            }

            RequireValue(row.DisplayName, sheetName, row.Id, "displayName", errors);
            RequireValue(row.StationId, sheetName, row.Id, "stationId", errors);
            RequireValue(row.OutputItemId, sheetName, row.Id, "outputItemId", errors);
            ValidateItemId(row.OutputItemId, sheetName, row.Id, "outputItemId", errors);
            ValidatePositiveInt(row.OutputCount, sheetName, row.Id, "outputCount", errors);
            ValidateNonNegativeInt(row.GoldCost, sheetName, row.Id, "goldCost", errors);
            ValidateNonNegativeInt(row.StaminaCost, sheetName, row.Id, "staminaCost", errors);
            ValidateInputs(row, sheetName, errors);
        }

        private static void ValidateInputs(RecipeSheetRow row, string sheetName, ICollection<RecipeSheetValidationError> errors)
        {
            if (row.Inputs.Count == 0)
            {
                errors.Add(new RecipeSheetValidationError(sheetName, row.Id, "재료가 1개 이상 필요합니다."));
                return;
            }

            int maxInputCount = row.RecipeKind == RecipeKind.Craft ? 2 : 3;
            if (row.Inputs.Count > maxInputCount)
            {
                errors.Add(new RecipeSheetValidationError(sheetName, row.Id, $"재료는 최대 {maxInputCount}개까지 사용할 수 있습니다."));
            }

            HashSet<string> usedInputIds = new();
            for (int index = 0; index < row.Inputs.Count; index++)
            {
                ItemStack input = row.Inputs[index];
                string fieldPrefix = $"input{index + 1}";
                ValidateItemId(input.ItemId, sheetName, row.Id, $"{fieldPrefix}ItemId", errors);
                if (input.Count <= 0)
                {
                    errors.Add(new RecipeSheetValidationError(sheetName, row.Id, $"{fieldPrefix}Count는 1 이상의 정수여야 합니다."));
                }

                if (!string.IsNullOrWhiteSpace(input.ItemId) && !usedInputIds.Add(input.ItemId))
                {
                    errors.Add(new RecipeSheetValidationError(sheetName, row.Id, $"중복된 입력 아이템입니다: {input.ItemId}"));
                }
            }
        }

        private static void ValidateEnabled(
            string value,
            string sheetName,
            string recipeId,
            ICollection<RecipeSheetValidationError> errors)
        {
            if (string.IsNullOrWhiteSpace(value)
                || value.Equals("TRUE", System.StringComparison.OrdinalIgnoreCase)
                || value.Equals("FALSE", System.StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            errors.Add(new RecipeSheetValidationError(sheetName, recipeId, "enabled는 TRUE, FALSE, 빈 값만 사용할 수 있습니다."));
        }

        private static void ValidateItemId(
            string value,
            string sheetName,
            string recipeId,
            string fieldName,
            ICollection<RecipeSheetValidationError> errors)
        {
            RequireValue(value, sheetName, recipeId, fieldName, errors);
            if (!string.IsNullOrWhiteSpace(value) && (!Regex.IsMatch(value, IdPattern) || !value.StartsWith("item.")))
            {
                errors.Add(new RecipeSheetValidationError(sheetName, recipeId, $"{fieldName}는 item.으로 시작하고 소문자 영어, 숫자, 언더스코어, 점만 사용할 수 있습니다."));
            }
        }

        private static void RequireValue(
            string value,
            string sheetName,
            string recipeId,
            string fieldName,
            ICollection<RecipeSheetValidationError> errors)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add(new RecipeSheetValidationError(sheetName, recipeId, $"{fieldName} 값이 비어 있습니다."));
            }
        }

        private static void ValidateNonNegativeInt(
            string value,
            string sheetName,
            string recipeId,
            string fieldName,
            ICollection<RecipeSheetValidationError> errors)
        {
            if (!int.TryParse(value, out int parsedValue) || parsedValue < 0)
            {
                errors.Add(new RecipeSheetValidationError(sheetName, recipeId, $"{fieldName}는 0 이상의 정수여야 합니다."));
            }
        }

        private static void ValidatePositiveInt(
            string value,
            string sheetName,
            string recipeId,
            string fieldName,
            ICollection<RecipeSheetValidationError> errors)
        {
            if (!int.TryParse(value, out int parsedValue) || parsedValue < 1)
            {
                errors.Add(new RecipeSheetValidationError(sheetName, recipeId, $"{fieldName}는 1 이상의 정수여야 합니다."));
            }
        }

        private static string ToSheetName(RecipeKind recipeKind)
        {
            return recipeKind == RecipeKind.Craft ? "Recipes_Craft" : "Recipes_Cooking";
        }
    }
}
