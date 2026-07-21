using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TownLife.Core
{
    /// <summary>
    /// Google Sheets에서 가져온 아이템 행이 SO로 생성 가능한지 검증합니다.
    /// </summary>
    public static class ItemSheetValidator
    {
        private const string ItemIdPattern = "^[a-z0-9_.]+$";

        /// <summary>
        /// 아이템 시트 데이터 전체를 검증하고 오류 목록을 반환합니다.
        /// </summary>
        public static IReadOnlyList<ItemSheetValidationError> Validate(ItemSheetData data)
        {
            // 오류는 한 번에 모아서 보여줘야 시트를 여러 번 고치지 않아도 됩니다.
            List<ItemSheetValidationError> errors = new();
            HashSet<string> usedIds = new();

            foreach (ItemSheetRow row in data.Items)
            {
                // 각 행은 독립적으로 필수값, 형식, 중복 여부를 검사합니다.
                ValidateRow(row, usedIds, errors);
            }

            return errors;
        }

        /// <summary>
        /// 아이템 시트 한 행의 필수값, ID 형식, 숫자 값을 검증합니다.
        /// </summary>
        private static void ValidateRow(
            ItemSheetRow row,
            ISet<string> usedIds,
            ICollection<ItemSheetValidationError> errors)
        {
            string sheetName = row.Category.ToString();

            // ID가 없으면 다른 오류도 위치를 알기 어려우므로 먼저 검사합니다.
            RequireValue(row.Id, sheetName, row.Id, "id", errors);
            if (string.IsNullOrWhiteSpace(row.Id))
            {
                return;
            }

            // 여러 탭 전체에서 같은 ID가 두 번 나오면 GameDatabase 조회와 저장 데이터가 충돌합니다.
            if (!usedIds.Add(row.Id))
            {
                errors.Add(new ItemSheetValidationError(sheetName, row.Id, "중복된 id입니다."));
            }

            // 코드와 저장 데이터에서 안전하게 쓰도록 ID 문자 규칙을 제한합니다.
            if (!Regex.IsMatch(row.Id, ItemIdPattern))
            {
                errors.Add(new ItemSheetValidationError(sheetName, row.Id, "id는 소문자 영어, 숫자, 언더스코어, 점만 사용할 수 있습니다."));
            }

            // UI와 게임 규칙에 필요한 필수 컬럼을 검사합니다.
            RequireValue(row.DisplayName, sheetName, row.Id, "displayName", errors);
            RequireValue(row.BaseSellPrice, sheetName, row.Id, "baseSellPrice", errors);
            RequireValue(row.MaxStack, sheetName, row.Id, "maxStack", errors);

            // 가격은 0 이상 정수만 허용합니다.
            ValidateNonNegativeInt(row.BaseSellPrice, sheetName, row.Id, "baseSellPrice", errors);

            // 스택 수량은 1 이상 정수만 허용합니다.
            ValidatePositiveInt(row.MaxStack, sheetName, row.Id, "maxStack", errors);
        }

        /// <summary>
        /// 필수 문자열 값이 비어 있는지 검사합니다.
        /// </summary>
        private static void RequireValue(
            string value,
            string sheetName,
            string itemId,
            string fieldName,
            ICollection<ItemSheetValidationError> errors)
        {
            // 빈 문자열은 생성 후 Unity Inspector에서 놓치기 쉬우므로 Import 전에 막습니다.
            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add(new ItemSheetValidationError(sheetName, itemId, $"{fieldName} 값이 비어 있습니다."));
            }
        }

        /// <summary>
        /// 문자열 값이 0 이상 정수인지 검사합니다.
        /// </summary>
        private static void ValidateNonNegativeInt(
            string value,
            string sheetName,
            string itemId,
            string fieldName,
            ICollection<ItemSheetValidationError> errors)
        {
            // 숫자가 아니거나 음수면 가격 계산에서 잘못된 값이 퍼질 수 있습니다.
            if (!int.TryParse(value, out int parsedValue) || parsedValue < 0)
            {
                errors.Add(new ItemSheetValidationError(sheetName, itemId, $"{fieldName}는 0 이상의 정수여야 합니다."));
            }
        }

        /// <summary>
        /// 문자열 값이 1 이상 정수인지 검사합니다.
        /// </summary>
        private static void ValidatePositiveInt(
            string value,
            string sheetName,
            string itemId,
            string fieldName,
            ICollection<ItemSheetValidationError> errors)
        {
            // maxStack이 0 이하면 인벤토리 묶음 계산이 불가능합니다.
            if (!int.TryParse(value, out int parsedValue) || parsedValue < 1)
            {
                errors.Add(new ItemSheetValidationError(sheetName, itemId, $"{fieldName}는 1 이상의 정수여야 합니다."));
            }
        }
    }
}
