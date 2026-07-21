namespace TownLife.Dialogue
{
    /// <summary>
    /// 대화 데이터 검증 중 발견된 문제입니다.
    /// </summary>
    public sealed class DialogueSheetValidationError
    {
        public DialogueSheetValidationError(string sheetName, string rowId, string message)
        {
            SheetName = sheetName;
            RowId = rowId;
            Message = message;
        }

        public string SheetName { get; }
        public string RowId { get; }
        public string Message { get; }

        public override string ToString()
        {
            return $"{SheetName}/{RowId}: {Message}";
        }
    }
}
