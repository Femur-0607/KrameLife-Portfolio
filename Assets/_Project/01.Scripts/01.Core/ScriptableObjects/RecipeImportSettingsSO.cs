using System;
using System.Collections.Generic;
using UnityEngine;

namespace TownLife.Core
{
    /// <summary>
    /// Google Sheets 레시피 데이터 Import에 필요한 설정입니다.
    /// 제작/요리 탭 gid를 Inspector에서 관리합니다.
    /// </summary>
    [CreateAssetMenu(menuName = "TownLife/Recipe Import Settings")]
    public sealed class RecipeImportSettingsSO : ScriptableObject
    {
        [SerializeField, Tooltip("Google Sheets 문서 ID입니다. URL의 /d/와 /edit 사이 문자열을 입력합니다.")]
        private string spreadsheetId = "1WfbjF-7WrHyE55lCVk40GJSpU38-wTF84b1sxLLCeaU";

        [SerializeField, Tooltip("다운로드한 CSV 캐시와 생성된 레시피 SO를 저장할 프로젝트 상대 경로입니다.")]
        private string generatedFolderPath = "Assets/_Project/03.ScriptableObjects/Recipes";

        [SerializeField, Tooltip("가져올 레시피 Google Sheets 탭 목록입니다. gid가 비어 있는 탭은 Import하지 않습니다.")]
        private List<RecipeImportSheetConfig> sheets = CreateDefaultSheets();

        public string SpreadsheetId => spreadsheetId;
        public string GeneratedFolderPath => generatedFolderPath;
        public IReadOnlyList<RecipeImportSheetConfig> Sheets => sheets;

        public void ResetSheetsToDefaults()
        {
            sheets = CreateDefaultSheets();
        }

        private static List<RecipeImportSheetConfig> CreateDefaultSheets()
        {
            return new List<RecipeImportSheetConfig>
            {
                new(RecipeKind.Craft, "0"),
                new(RecipeKind.Cooking, "332108293"),
            };
        }
    }

    [Serializable]
    public sealed class RecipeImportSheetConfig
    {
        [SerializeField, Tooltip("이 탭에서 생성할 레시피 계열입니다.")]
        private RecipeKind recipeKind = RecipeKind.Craft;

        [SerializeField, Tooltip("Google Sheets 탭 gid입니다. 비워두면 이 탭은 Import하지 않습니다.")]
        private string gid = string.Empty;

        public RecipeImportSheetConfig(RecipeKind recipeKind, string gid)
        {
            this.recipeKind = recipeKind;
            this.gid = gid;
        }

        public RecipeKind RecipeKind => recipeKind;
        public string SheetName => recipeKind == RecipeKind.Craft ? "Recipes_Craft" : "Recipes_Cooking";
        public string Gid => gid;
        public bool IsEnabled => !string.IsNullOrWhiteSpace(gid);
    }
}
