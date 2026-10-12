namespace TownLife.Core
{
    /// <summary>
    /// Google Sheets 레시피 시트를 CSV export URL로 바꿔주는 유틸리티입니다.
    /// </summary>
    public static class RecipeSheetCsvUrlBuilder
    {
        public static string BuildGoogleCsvExportUrl(string spreadsheetId, string gid)
        {
            return $"https://docs.google.com/spreadsheets/d/{spreadsheetId}/export?format=csv&gid={gid}";
        }
    }
}
