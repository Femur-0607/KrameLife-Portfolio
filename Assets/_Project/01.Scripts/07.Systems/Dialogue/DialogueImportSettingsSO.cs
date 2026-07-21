using System;
using System.Collections.Generic;
using UnityEngine;

namespace TownLife.Dialogue
{
    /// <summary>
    /// Google Sheets 대화 데이터 Import에 필요한 스프레드시트 ID와 탭 gid 목록입니다.
    /// </summary>
    [CreateAssetMenu(menuName = "TownLife/Dialogue Import Settings")]
    public sealed class DialogueImportSettingsSO : ScriptableObject
    {
        [SerializeField, Tooltip("Google Sheets 문서 ID입니다. URL의 /d/와 /edit 사이 문자열을 입력합니다.")]
        private string spreadsheetId = "10G_kXjpCMg4VJkWdOj2c0SADnlS125QmelBQ1WTakAM";

        [SerializeField, Tooltip("CSV를 저장할 프로젝트 상대 경로입니다.")]
        private string generatedFolderPath = "Assets/_Project/Dialogue/Generated";

        [SerializeField, Tooltip("가져올 시트 이름과 gid 목록입니다.")]
        private List<DialogueImportSheetConfig> sheets = new()
        {
            new(DialogueSheetNames.DialogueNodes, "890339469"),
            new(DialogueSheetNames.DialogueChoices, "432874479"),
            new(DialogueSheetNames.Conditions, "372552812"),
            new(DialogueSheetNames.Effects, "1654524666"),
            new(DialogueSheetNames.QuestDefinitions, "360940923"),
            new(DialogueSheetNames.QuestSteps, "833609434"),
        };

        public string SpreadsheetId => spreadsheetId;
        public string GeneratedFolderPath => generatedFolderPath;
        public IReadOnlyList<DialogueImportSheetConfig> Sheets => sheets;
    }

    [Serializable]
    public sealed class DialogueImportSheetConfig
    {
        [SerializeField, Tooltip("Google Sheets 탭 이름입니다.")]
        private string sheetName = string.Empty;

        [SerializeField, Tooltip("Google Sheets 탭 gid입니다. 탭을 열었을 때 URL의 gid 값입니다.")]
        private string gid = string.Empty;

        public DialogueImportSheetConfig(string sheetName, string gid)
        {
            this.sheetName = sheetName;
            this.gid = gid;
        }

        public string SheetName => sheetName;
        public string Gid => gid;
    }
}
