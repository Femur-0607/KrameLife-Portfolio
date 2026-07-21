namespace TownLife.Core
{
    /// <summary>
    /// Google Sheets 아이템 시트를 CSV export URL로 바꿔주는 유틸리티입니다.
    /// </summary>
    public static class ItemSheetCsvUrlBuilder
    {
        /// <summary>
        /// 스프레드시트 ID와 시트 gid로 Google Sheets CSV 다운로드 URL을 만듭니다.
        /// </summary>
        public static string BuildGoogleCsvExportUrl(string spreadsheetId, string gid)
        {
            // Google Sheets의 공개 CSV export 규칙에 맞춰 다운로드 URL을 조립합니다.
            return $"https://docs.google.com/spreadsheets/d/{spreadsheetId}/export?format=csv&gid={gid}";
        }
    }
}
