namespace TownLife.Dialogue
{
    /// <summary>
    /// Google Sheets CSV export URL을 생성합니다.
    /// </summary>
    public static class DialogueSheetCsvUrlBuilder
    {
        /// <summary>
        /// 스프레드시트 ID와 시트 gid로 CSV export URL을 만듭니다.
        /// </summary>
        public static string BuildGoogleCsvExportUrl(string spreadsheetId, string gid)
        {
            return $"https://docs.google.com/spreadsheets/d/{spreadsheetId}/export?format=csv&gid={gid}";
        }
    }
}
