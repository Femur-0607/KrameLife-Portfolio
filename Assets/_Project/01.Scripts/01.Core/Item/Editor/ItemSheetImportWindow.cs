#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace TownLife.Core.Editor
{
    /// <summary>
    /// Google Sheets 카테고리별 아이템 탭을 내려받아 GameItemSO 에셋으로 생성하거나 갱신하는 에디터 창입니다.
    /// </summary>
    public sealed class ItemSheetImportWindow : EditorWindow
    {
        private const string DefaultSettingsPath = "Assets/_Project/03.ScriptableObjects/Items/ItemImportSettings.asset";
        private const string GameDatabasePath = "Assets/_Project/03.ScriptableObjects/GameDatabase.asset";

        [SerializeField, Tooltip("아이템 데이터 Import 설정 에셋입니다.")]
        private ItemImportSettingsSO settings;

        private Vector2 scrollPosition;
        private string lastResult = "아직 Import하지 않았습니다.";
        private bool isImporting;

        /// <summary>
        /// 아이템 데이터 Importer 에디터 창을 열고 기본 설정 에셋을 불러옵니다.
        /// </summary>
        [MenuItem("Tools/TownLife/Item Data Importer")]
        private static void Open()
        {
            // Unity 메뉴에서 창을 열고, 프로젝트 기본 설정 에셋을 자동으로 연결합니다.
            ItemSheetImportWindow window = GetWindow<ItemSheetImportWindow>("Item Importer");
            window.minSize = new Vector2(560f, 380f);
            window.LoadDefaultSettings();
            window.Show();
        }

        /// <summary>
        /// Import 설정 선택, 기본 설정 생성, Import 실행 버튼과 결과 메시지를 그립니다.
        /// </summary>
        private void OnGUI()
        {
            EditorGUILayout.LabelField("TownLife Item Data Importer", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // 사용자가 다른 설정 에셋을 테스트할 수 있게 ObjectField로 노출합니다.
            settings = (ItemImportSettingsSO)EditorGUILayout.ObjectField(
                "Import Settings",
                settings,
                typeof(ItemImportSettingsSO),
                false);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Load Default Settings"))
                {
                    LoadDefaultSettings();
                }

                if (GUILayout.Button("Create Default Settings"))
                {
                    CreateDefaultSettings();
                }
            }

            using (new EditorGUI.DisabledScope(settings == null || isImporting))
            {
                if (GUILayout.Button("Reset Sheet GIDs To Code Defaults"))
                {
                    ResetSettingsSheetGids();
                }
            }

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(settings == null || isImporting))
            {
                if (GUILayout.Button(isImporting ? "Importing..." : "Import Item Data"))
                {
                    _ = ImportAsync();
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Result", EditorStyles.boldLabel);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            EditorGUILayout.HelpBox(lastResult, MessageType.Info);
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 기본 경로에 있는 Import 설정 에셋을 불러옵니다.
        /// </summary>
        private void LoadDefaultSettings()
        {
            // 기본 설정 에셋이 있으면 창에 연결하고, 없으면 사용자가 생성 버튼을 누르게 둡니다.
            settings = AssetDatabase.LoadAssetAtPath<ItemImportSettingsSO>(DefaultSettingsPath);
        }

        /// <summary>
        /// 기본 Import 설정 에셋이 없으면 생성하고, 있으면 기존 에셋을 불러옵니다.
        /// </summary>
        private void CreateDefaultSettings()
        {
            ItemImportSettingsSO existingSettings = AssetDatabase.LoadAssetAtPath<ItemImportSettingsSO>(DefaultSettingsPath);
            if (existingSettings != null)
            {
                settings = existingSettings;
                lastResult = "기존 기본 설정 에셋을 불러왔습니다.";
                return;
            }

            // 설정 SO는 아이템 SO 폴더에 함께 둬서 팀원이 찾기 쉽게 합니다.
            EnsureFolder(Path.GetDirectoryName(DefaultSettingsPath));

            ItemImportSettingsSO newSettings = CreateInstance<ItemImportSettingsSO>();
            AssetDatabase.CreateAsset(newSettings, DefaultSettingsPath);
            AssetDatabase.SaveAssets();

            settings = newSettings;
            lastResult = "기본 설정 에셋을 생성했습니다.";
        }

        /// <summary>
        /// 기존 Import 설정 에셋의 카테고리별 gid 목록을 코드 기본값으로 동기화합니다.
        /// </summary>
        private void ResetSettingsSheetGids()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Reset Sheet GIDs",
                "현재 Import Settings의 카테고리별 gid 목록을 코드 기본값으로 덮어씁니다.",
                "Reset",
                "Cancel");

            if (!confirmed)
            {
                return;
            }

            Undo.RecordObject(settings, "Reset Item Import Sheet GIDs");
            settings.ResetSheetsToDefaults();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            lastResult = "ItemImportSettings의 gid 목록을 코드 기본값으로 동기화했습니다.";
        }

        /// <summary>
        /// Google Sheets CSV 다운로드, 데이터 매핑, 검증, CSV 캐시 저장, 아이템 SO 갱신을 순서대로 실행합니다.
        /// </summary>
        private async Task ImportAsync()
        {
            if (settings == null)
            {
                lastResult = "Import Settings가 없습니다.";
                return;
            }

            isImporting = true;
            lastResult = "CSV 다운로드 중...";
            Repaint();

            try
            {
                // gid가 입력된 카테고리 탭만 내려받아 하나의 아이템 데이터로 합칩니다.
                ItemSheetData data = new();
                Dictionary<string, string> csvTexts = new();
                foreach (ItemImportSheetConfig sheet in settings.Sheets)
                {
                    if (!sheet.IsEnabled)
                    {
                        continue;
                    }

                    string csvText = await DownloadItemCsvAsync(settings, sheet);
                    csvTexts[sheet.SheetName] = csvText;
                    data.AddRange(ItemSheetCsvMapper.FromCsvText(csvText, sheet.Category));
                }

                if (csvTexts.Count == 0)
                {
                    lastResult = "Import할 탭이 없습니다. ItemImportSettings의 gid를 1개 이상 입력하세요.";
                    return;
                }

                // 모든 탭을 합친 뒤 ID 중복까지 한 번에 검증합니다.
                IReadOnlyList<ItemSheetValidationError> errors = ItemSheetValidator.Validate(data);
                if (errors.Count > 0)
                {
                    lastResult = BuildErrorReport(errors);
                    return;
                }

                // 검증된 CSV 원문을 탭별로 저장해 구글 시트 변경 내용을 git diff로 확인할 수 있게 합니다.
                SaveCsvCache(settings.GeneratedFolderPath, csvTexts);

                // 각 행을 GameItemSO 에셋으로 생성하거나 기존 에셋을 갱신합니다.
                List<GameItemSO> importedItems = ImportItems(settings.GeneratedFolderPath, data.Items);

                // 생성된 아이템이 GameDatabase에서 id로 조회되도록 entries 목록에 등록합니다.
                RegisterItemsToGameDatabase(importedItems);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                lastResult = $"Import 성공. 탭 {csvTexts.Count}개, 아이템 {importedItems.Count}개 갱신 완료: {settings.GeneratedFolderPath}";
            }
            catch (System.Exception exception)
            {
                lastResult = $"Import 실패:\n{exception.Message}";
            }
            finally
            {
                isImporting = false;
                Repaint();
            }
        }

        /// <summary>
        /// Import 설정과 카테고리 시트 설정을 사용해 Google Sheets CSV 텍스트를 다운로드합니다.
        /// </summary>
        private static async Task<string> DownloadItemCsvAsync(ItemImportSettingsSO settings, ItemImportSheetConfig sheet)
        {
            // 시트 설정을 URL로 변환하고 UnityWebRequest로 텍스트를 내려받습니다.
            string url = ItemSheetCsvUrlBuilder.BuildGoogleCsvExportUrl(settings.SpreadsheetId, sheet.Gid);
            return await DownloadTextAsync(url);
        }

        /// <summary>
        /// 지정한 URL에서 텍스트를 다운로드하고 실패하면 UnityException을 발생시킵니다.
        /// </summary>
        private static async Task<string> DownloadTextAsync(string url)
        {
            using UnityWebRequest request = UnityWebRequest.Get(url);
            UnityWebRequestAsyncOperation operation = request.SendWebRequest();

            // EditorWindow가 멈추지 않도록 비동기로 다운로드 완료를 기다립니다.
            while (!operation.isDone)
            {
                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                throw new UnityException($"CSV 다운로드 실패: {url}\n{request.error}");
            }

            return request.downloadHandler.text;
        }

        /// <summary>
        /// 검증을 통과한 탭별 CSV 원본을 프로젝트 폴더에 저장합니다.
        /// </summary>
        private static void SaveCsvCache(string folderPath, IReadOnlyDictionary<string, string> csvTexts)
        {
            EnsureFolder(folderPath);

            foreach (KeyValuePair<string, string> pair in csvTexts)
            {
                // 탭 이름별 CSV를 같이 남겨두면 어떤 카테고리가 바뀌었는지 추적하기 쉽습니다.
                string filePath = Path.Combine(folderPath, $"{pair.Key}.csv").Replace('\\', '/');
                File.WriteAllText(filePath, pair.Value, Encoding.UTF8);
            }
        }

        /// <summary>
        /// 검증된 아이템 행을 GameItemSO 에셋으로 생성하거나 기존 에셋에 덮어씁니다.
        /// </summary>
        private static List<GameItemSO> ImportItems(string rootFolderPath, IReadOnlyList<ItemSheetRow> rows)
        {
            List<GameItemSO> importedItems = new();

            foreach (ItemSheetRow row in rows)
            {
                // category별 폴더를 사용해 아이템 수가 많아져도 Inspector에서 찾기 쉽게 합니다.
                string categoryFolderPath = Path.Combine(rootFolderPath, row.Category.ToString()).Replace('\\', '/');
                EnsureFolder(categoryFolderPath);

                GameItemSO item = LoadOrCreateItem(rootFolderPath, categoryFolderPath, row);
                ApplyItemData(item, row);
                importedItems.Add(item);
            }

            return importedItems;
        }

        /// <summary>
        /// 같은 ID의 기존 아이템 에셋을 찾고, 없으면 category 폴더에 새 에셋을 만듭니다.
        /// </summary>
        private static GameItemSO LoadOrCreateItem(string rootFolderPath, string categoryFolderPath, ItemSheetRow row)
        {
            // 이미 생성된 SO가 있으면 파일 위치가 달라도 기존 에셋을 갱신해 참조 깨짐을 줄입니다.
            GameItemSO existingItem = FindItemById(rootFolderPath, row.Id);
            if (existingItem != null)
            {
                return existingItem;
            }

            string assetPath = Path.Combine(categoryFolderPath, $"{SanitizeAssetName(row.Id)}.asset").Replace('\\', '/');
            GameItemSO newItem = CreateInstance<GameItemSO>();
            AssetDatabase.CreateAsset(newItem, AssetDatabase.GenerateUniqueAssetPath(assetPath));
            return newItem;
        }

        /// <summary>
        /// 지정한 루트 폴더 아래에서 같은 ID를 가진 GameItemSO를 찾습니다.
        /// </summary>
        private static GameItemSO FindItemById(string rootFolderPath, string itemId)
        {
            // 생성 폴더 내부의 GameItemSO만 검색해 다른 시스템 데이터와 섞이지 않게 합니다.
            string[] guids = AssetDatabase.FindAssets("t:GameItemSO", new[] { rootFolderPath });
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                GameItemSO item = AssetDatabase.LoadAssetAtPath<GameItemSO>(assetPath);
                if (item != null && item.Id == itemId)
                {
                    return item;
                }
            }

            return null;
        }

        /// <summary>
        /// 시트 행 데이터를 GameItemSO의 serialized field에 적용합니다.
        /// </summary>
        private static void ApplyItemData(GameItemSO item, ItemSheetRow row)
        {
            // private SerializeField 값은 SerializedObject로 수정해야 Unity 직렬화와 dirty 처리가 맞습니다.
            SerializedObject serializedItem = new(item);
            serializedItem.FindProperty("id").stringValue = row.Id;
            serializedItem.FindProperty("displayName").stringValue = row.DisplayName;
            serializedItem.FindProperty("description").stringValue = row.Description;
            serializedItem.FindProperty("iconAddress").stringValue = row.IconAddress;
            serializedItem.FindProperty("category").enumValueIndex = (int)row.Category;
            serializedItem.FindProperty("baseSellPrice").intValue = int.Parse(row.BaseSellPrice);
            serializedItem.FindProperty("maxStack").intValue = int.Parse(row.MaxStack);
            serializedItem.ApplyModifiedPropertiesWithoutUndo();

            // 에셋 이름도 ID와 맞춰 검색하기 쉽게 합니다.
            item.name = SanitizeAssetName(row.Id);
            EditorUtility.SetDirty(item);
        }

        /// <summary>
        /// 가져온 아이템들을 GameDatabase entries 목록에 등록합니다.
        /// </summary>
        private static void RegisterItemsToGameDatabase(IReadOnlyList<GameItemSO> importedItems)
        {
            GameDatabase database = AssetDatabase.LoadAssetAtPath<GameDatabase>(GameDatabasePath);
            if (database == null)
            {
                throw new UnityException($"GameDatabase를 찾을 수 없습니다: {GameDatabasePath}");
            }

            SerializedObject serializedDatabase = new(database);
            SerializedProperty entriesProperty = serializedDatabase.FindProperty("entries");
            HashSet<Object> registeredEntries = CollectRegisteredEntries(entriesProperty);

            foreach (GameItemSO item in importedItems)
            {
                if (registeredEntries.Contains(item))
                {
                    continue;
                }

                // 기존 데이터는 유지하고, 새로 생성된 아이템만 뒤에 추가합니다.
                int newIndex = entriesProperty.arraySize;
                entriesProperty.InsertArrayElementAtIndex(newIndex);
                entriesProperty.GetArrayElementAtIndex(newIndex).objectReferenceValue = item;
                registeredEntries.Add(item);
            }

            serializedDatabase.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);
        }

        /// <summary>
        /// GameDatabase entries에 이미 등록된 에셋 참조를 모읍니다.
        /// </summary>
        private static HashSet<Object> CollectRegisteredEntries(SerializedProperty entriesProperty)
        {
            HashSet<Object> entries = new();

            for (int index = 0; index < entriesProperty.arraySize; index++)
            {
                Object entry = entriesProperty.GetArrayElementAtIndex(index).objectReferenceValue;
                if (entry != null)
                {
                    entries.Add(entry);
                }
            }

            return entries;
        }

        /// <summary>
        /// 검증 오류 목록을 에디터 창에 보여줄 문자열로 변환합니다.
        /// </summary>
        private static string BuildErrorReport(IReadOnlyList<ItemSheetValidationError> errors)
        {
            StringBuilder builder = new();
            builder.AppendLine($"Import 중단. 오류 {errors.Count}개를 먼저 수정하세요.");

            foreach (ItemSheetValidationError error in errors)
            {
                // ID가 비어 있는 행도 표시될 수 있으므로 빈 ID는 별도 문구로 대체합니다.
                string itemId = string.IsNullOrWhiteSpace(error.ItemId) ? "(id 없음)" : error.ItemId;
                builder.AppendLine($"- {error.SheetName}/{itemId}: {error.Message}");
            }

            return builder.ToString();
        }

        /// <summary>
        /// Unity 에셋 파일명에 안전한 문자열로 바꿉니다.
        /// </summary>
        private static string SanitizeAssetName(string value)
        {
            // 아이템 ID의 점 구분자는 SO 이름/파일명에서도 유지하고, 나머지 특수문자만 정리합니다.
            return Regex.Replace(value, "[^a-zA-Z0-9_.가-힣-]", "_");
        }

        /// <summary>
        /// 지정한 프로젝트 상대 폴더가 없으면 생성합니다.
        /// </summary>
        private static void EnsureFolder(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                return;
            }

            // Directory.CreateDirectory는 이미 폴더가 있어도 안전하게 통과합니다.
            Directory.CreateDirectory(folderPath);
            AssetDatabase.Refresh();
        }
    }
}
#endif
