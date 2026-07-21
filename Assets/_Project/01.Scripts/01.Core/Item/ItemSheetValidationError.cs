namespace TownLife.Core
{
    /// <summary>
    /// 아이템 시트 검증 중 발견한 오류 한 건을 표현합니다.
    /// </summary>
    public sealed class ItemSheetValidationError
    {
        /// <summary>
        /// 오류가 발생한 시트, 아이템 ID, 메시지를 저장합니다.
        /// </summary>
        public ItemSheetValidationError(string sheetName, string itemId, string message)
        {
            // 오류 리포트에서 어느 탭과 행이 문제인지 바로 볼 수 있도록 함께 보관합니다.
            SheetName = sheetName;
            ItemId = itemId;
            Message = message;
        }

        /// <summary>
        /// 오류가 발생한 Google Sheets 탭 이름입니다.
        /// </summary>
        public string SheetName { get; }

        /// <summary>
        /// 오류가 발생한 아이템 ID입니다. ID 자체가 비어 있으면 빈 문자열일 수 있습니다.
        /// </summary>
        public string ItemId { get; }

        /// <summary>
        /// 사용자에게 보여줄 검증 오류 메시지입니다.
        /// </summary>
        public string Message { get; }
    }
}
