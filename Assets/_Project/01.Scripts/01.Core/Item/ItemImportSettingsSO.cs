using System;
using System.Collections.Generic;
using UnityEngine;

namespace TownLife.Core
{
    /// <summary>
    /// Google Sheets 아이템 데이터 Import에 필요한 설정입니다.
    /// 카테고리별 탭 이름과 gid를 Inspector에서 관리합니다.
    /// </summary>
    [CreateAssetMenu(menuName = "TownLife/Item Import Settings")]
    public sealed class ItemImportSettingsSO : ScriptableObject
    {
        [SerializeField, Tooltip("Google Sheets 문서 ID입니다. URL의 /d/와 /edit 사이 문자열을 입력합니다.")]
        private string spreadsheetId = "1H__RRbykZ1av5OoQr75vtkebvVFt5vHnQ6tk_ZjkLGs";

        [SerializeField, Tooltip("다운로드한 CSV 캐시와 생성된 아이템 SO를 저장할 프로젝트 상대 경로입니다.")]
        private string generatedFolderPath = "Assets/_Project/03.ScriptableObjects/Items";

        [SerializeField, Tooltip("가져올 카테고리별 Google Sheets 탭 목록입니다. gid가 비어 있는 탭은 Import하지 않습니다.")]
        private List<ItemImportSheetConfig> sheets = CreateDefaultSheets();

        /// <summary>
        /// Google Sheets 문서 ID입니다.
        /// </summary>
        public string SpreadsheetId => spreadsheetId;

        /// <summary>
        /// CSV 캐시와 생성 에셋을 저장할 폴더 경로입니다.
        /// </summary>
        public string GeneratedFolderPath => generatedFolderPath;

        /// <summary>
        /// Import할 카테고리별 시트 목록입니다.
        /// </summary>
        public IReadOnlyList<ItemImportSheetConfig> Sheets => sheets;

        /// <summary>
        /// 기존 설정 에셋의 카테고리별 gid 목록을 코드 기본값으로 되돌립니다.
        /// </summary>
        public void ResetSheetsToDefaults()
        {
            sheets = CreateDefaultSheets();
        }

        private static List<ItemImportSheetConfig> CreateDefaultSheets()
        {
            return new List<ItemImportSheetConfig>
            {
                new(ItemCategory.Ore, "0"),
                new(ItemCategory.Herb, "1755606634"),
                new(ItemCategory.MonsterMaterial, "146889907"),
                new(ItemCategory.Goods, "1737093167"),
                new(ItemCategory.Scroll, "1210151646"),
                new(ItemCategory.Crop, "2133011206"),
                new(ItemCategory.Seed, "270715383"),
                new(ItemCategory.AnimalProduct, "2115285342"),
                new(ItemCategory.Fish, "1849924014"),
                new(ItemCategory.Food, "1032934464"),
                new(ItemCategory.Potion, "1852861857"),
                new(ItemCategory.Tool, "656543851"),
                new(ItemCategory.Weapon, "1066157768"),
                new(ItemCategory.Armor, "1017265088"),
                new(ItemCategory.Furniture, "18776698"),
                new(ItemCategory.KeyItem, "1766252046"),
                new(ItemCategory.Material, "1422225078"),
                new(ItemCategory.Gem, "1690066461"),
                new(ItemCategory.Accessory, "227865999"),
                new(ItemCategory.Collectible, "280140225"),
            };
        }
    }

    /// <summary>
    /// Google Sheets에서 가져올 카테고리 시트 정보입니다.
    /// 탭 이름은 category enum 이름을 그대로 사용합니다.
    /// </summary>
    [Serializable]
    public sealed class ItemImportSheetConfig
    {
        [SerializeField, Tooltip("이 탭에서 생성할 아이템 카테고리입니다. Google Sheets 탭 이름도 이 enum 이름과 같게 맞춥니다.")]
        private ItemCategory category = ItemCategory.Ore;

        [SerializeField, Tooltip("Google Sheets 탭 gid입니다. 비워두면 이 카테고리는 Import하지 않습니다.")]
        private string gid = string.Empty;

        /// <summary>
        /// 코드에서 기본 시트 설정을 만들 때 사용합니다.
        /// </summary>
        public ItemImportSheetConfig(ItemCategory category, string gid)
        {
            // Inspector 기본값으로 표시할 카테고리와 gid를 저장합니다.
            this.category = category;
            this.gid = gid;
        }

        /// <summary>
        /// 이 시트에서 생성할 아이템 카테고리입니다.
        /// </summary>
        public ItemCategory Category => category;

        /// <summary>
        /// Google Sheets 탭 이름입니다. 카테고리 enum 이름을 그대로 사용합니다.
        /// </summary>
        public string SheetName => category.ToString();

        /// <summary>
        /// Google Sheets 탭 gid입니다.
        /// </summary>
        public string Gid => gid;

        /// <summary>
        /// gid가 입력되어 실제 Import 대상인지 확인합니다.
        /// </summary>
        public bool IsEnabled => !string.IsNullOrWhiteSpace(gid);
    }
}
