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
    /// Google Sheets 제작/요리 레시피 탭을 내려받아 RecipeSO 에셋으로 생성하거나 갱신하는 에디터 창입니다.
    /// </summary>
    public sealed class RecipeSheetImportWindow : EditorWindow
    {
        private const string DefaultSettingsPath = "Assets/_Project/03.ScriptableObjects/Recipes/RecipeImportSettings.asset";
        private const string GameDatabasePath = "Assets/_Project/03.ScriptableObjects/GameDatabase.asset";

        [SerializeField, Tooltip("레시피 데이터 Import 설정 에셋입니다.")]
        private RecipeImportSettingsSO settings;

        private Vector2 scrollPosition;
        private string lastResult = "아직 Import하지 않았습니다.";
        private bool isImporting;

        [MenuItem("Tools/TownLife/Recipe Data Importer")]
        private static void Open()
        {
            RecipeSheetImportWindow window = GetWindow<RecipeSheetImportWindow>("Recipe Importer");
            window.minSize = new Vector2(560f, 380f);
            window.LoadDefaultSettings();
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("TownLife Recipe Data Importer", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            settings = (RecipeImportSettingsSO)EditorGUILayout.ObjectField(
                "Import Settings",
                settings,
                typeof(RecipeImportSettingsSO),
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
                if (GUILayout.Button(isImporting ? "Importing..." : "Import Recipe Data"))
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

        private void LoadDefaultSettings()
        {
            settings = AssetDatabase.LoadAssetAtPath<RecipeImportSettingsSO>(DefaultSettingsPath);
        }

        private void CreateDefaultSettings()
        {
            RecipeImportSettingsSO existingSettings = AssetDatabase.LoadAssetAtPath<RecipeImportSettingsSO>(DefaultSettingsPath);
            if (existingSettings != null)
            {
                settings = existingSettings;
                lastResult = "기존 기본 설정 에셋을 불러왔습니다.";
                return;
            }

            EnsureFolder(Path.GetDirectoryName(DefaultSettingsPath));

            RecipeImportSettingsSO newSettings = CreateInstance<RecipeImportSettingsSO>();
            AssetDatabase.CreateAsset(newSettings, DefaultSettingsPath);
            AssetDatabase.SaveAssets();

            settings = newSettings;
            lastResult = "기본 설정 에셋을 생성했습니다.";
        }

        private void ResetSettingsSheetGids()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Reset Sheet GIDs",
                "현재 Import Settings의 레시피 gid 목록을 코드 기본값으로 덮어씁니다.",
                "Reset",
                "Cancel");

            if (!confirmed)
            {
                return;
            }

            Undo.RecordObject(settings, "Reset Recipe Import Sheet GIDs");
            settings.ResetSheetsToDefaults();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            lastResult = "RecipeImportSettings의 gid 목록을 코드 기본값으로 동기화했습니다.";
        }

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
                RecipeSheetData data = new();
                Dictionary<string, string> csvTexts = new();
                foreach (RecipeImportSheetConfig sheet in settings.Sheets)
                {
                    if (!sheet.IsEnabled)
                    {
                        continue;
                    }

                    string csvText = await DownloadRecipeCsvAsync(settings, sheet);
                    csvTexts[sheet.SheetName] = csvText;
                    data.AddRange(RecipeSheetCsvMapper.FromCsvText(csvText, sheet.RecipeKind));
                }

                if (csvTexts.Count == 0)
                {
                    lastResult = "Import할 탭이 없습니다. RecipeImportSettings의 gid를 1개 이상 입력하세요.";
                    return;
                }

                IReadOnlyList<RecipeSheetValidationError> errors = RecipeSheetValidator.Validate(data);
                if (errors.Count > 0)
                {
                    lastResult = BuildErrorReport(errors);
                    return;
                }

                SaveCsvCache(settings.GeneratedFolderPath, csvTexts);

                List<RecipeSO> importedRecipes = ImportRecipes(settings.GeneratedFolderPath, data.Recipes);
                RegisterRecipesToGameDatabase(importedRecipes);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                lastResult = $"Import 성공. 탭 {csvTexts.Count}개, 레시피 {importedRecipes.Count}개 갱신 완료: {settings.GeneratedFolderPath}";
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

        private static async Task<string> DownloadRecipeCsvAsync(RecipeImportSettingsSO settings, RecipeImportSheetConfig sheet)
        {
            string url = RecipeSheetCsvUrlBuilder.BuildGoogleCsvExportUrl(settings.SpreadsheetId, sheet.Gid);
            return await DownloadTextAsync(url);
        }

        private static async Task<string> DownloadTextAsync(string url)
        {
            using UnityWebRequest request = UnityWebRequest.Get(url);
            UnityWebRequestAsyncOperation operation = request.SendWebRequest();

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

        private static void SaveCsvCache(string folderPath, IReadOnlyDictionary<string, string> csvTexts)
        {
            EnsureFolder(folderPath);

            foreach (KeyValuePair<string, string> pair in csvTexts)
            {
                string filePath = Path.Combine(folderPath, $"{pair.Key}.csv").Replace('\\', '/');
                File.WriteAllText(filePath, pair.Value, Encoding.UTF8);
            }
        }

        private static List<RecipeSO> ImportRecipes(string rootFolderPath, IReadOnlyList<RecipeSheetRow> rows)
        {
            List<RecipeSO> importedRecipes = new();

            foreach (RecipeSheetRow row in rows)
            {
                if (!row.IsEnabled)
                {
                    continue;
                }

                string kindFolderPath = Path.Combine(rootFolderPath, row.RecipeKind.ToString()).Replace('\\', '/');
                EnsureFolder(kindFolderPath);

                RecipeSO recipe = LoadOrCreateRecipe(rootFolderPath, kindFolderPath, row);
                ApplyRecipeData(recipe, row);
                importedRecipes.Add(recipe);
            }

            return importedRecipes;
        }

        private static RecipeSO LoadOrCreateRecipe(string rootFolderPath, string kindFolderPath, RecipeSheetRow row)
        {
            RecipeSO existingRecipe = FindRecipeById(rootFolderPath, row.Id);
            if (existingRecipe != null)
            {
                return existingRecipe;
            }

            string assetPath = Path.Combine(kindFolderPath, $"{SanitizeAssetName(row.Id)}.asset").Replace('\\', '/');
            RecipeSO newRecipe = CreateInstance<RecipeSO>();
            AssetDatabase.CreateAsset(newRecipe, AssetDatabase.GenerateUniqueAssetPath(assetPath));
            return newRecipe;
        }

        private static RecipeSO FindRecipeById(string rootFolderPath, string recipeId)
        {
            string[] guids = AssetDatabase.FindAssets("t:RecipeSO", new[] { rootFolderPath });
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                RecipeSO recipe = AssetDatabase.LoadAssetAtPath<RecipeSO>(assetPath);
                if (recipe != null && recipe.Id == recipeId)
                {
                    return recipe;
                }
            }

            return null;
        }

        private static void ApplyRecipeData(RecipeSO recipe, RecipeSheetRow row)
        {
            SerializedObject serializedRecipe = new(recipe);
            serializedRecipe.FindProperty("id").stringValue = row.Id;
            serializedRecipe.FindProperty("recipeKind").enumValueIndex = (int)row.RecipeKind;
            serializedRecipe.FindProperty("displayName").stringValue = row.DisplayName;
            serializedRecipe.FindProperty("description").stringValue = row.Description;
            serializedRecipe.FindProperty("iconAddress").stringValue = row.IconAddress;
            serializedRecipe.FindProperty("requiredStationDefinition").objectReferenceValue = null;
            serializedRecipe.FindProperty("requiredStationId").stringValue = row.StationId;
            serializedRecipe.FindProperty("unlockNodeId").stringValue = row.UnlockNodeId;
            serializedRecipe.FindProperty("staminaCost").intValue = int.Parse(row.StaminaCost);
            serializedRecipe.FindProperty("goldCost").intValue = int.Parse(row.GoldCost);

            ApplyInputStacks(serializedRecipe.FindProperty("inputs"), row.Inputs);
            ApplyOutputStack(serializedRecipe.FindProperty("output"), row.OutputItemId, int.Parse(row.OutputCount));

            serializedRecipe.ApplyModifiedPropertiesWithoutUndo();

            recipe.name = SanitizeAssetName(row.Id);
            EditorUtility.SetDirty(recipe);
        }

        private static void ApplyInputStacks(SerializedProperty inputsProperty, IReadOnlyList<ItemStack> inputs)
        {
            inputsProperty.ClearArray();
            for (int index = 0; index < inputs.Count; index++)
            {
                inputsProperty.InsertArrayElementAtIndex(index);
                SerializedProperty inputProperty = inputsProperty.GetArrayElementAtIndex(index);
                ApplyItemStack(inputProperty, inputs[index].ItemId, inputs[index].Count);
            }
        }

        private static void ApplyOutputStack(SerializedProperty outputProperty, string itemId, int count)
        {
            ApplyItemStack(outputProperty, itemId, count);
        }

        private static void ApplyItemStack(SerializedProperty stackProperty, string itemId, int count)
        {
            stackProperty.FindPropertyRelative("item").objectReferenceValue = null;
            stackProperty.FindPropertyRelative("itemId").stringValue = itemId;
            stackProperty.FindPropertyRelative("count").intValue = count;
        }

        private static void RegisterRecipesToGameDatabase(IReadOnlyList<RecipeSO> importedRecipes)
        {
            GameDatabase database = AssetDatabase.LoadAssetAtPath<GameDatabase>(GameDatabasePath);
            if (database == null)
            {
                throw new UnityException($"GameDatabase를 찾을 수 없습니다: {GameDatabasePath}");
            }

            SerializedObject serializedDatabase = new(database);
            SerializedProperty entriesProperty = serializedDatabase.FindProperty("entries");
            HashSet<Object> registeredEntries = CollectRegisteredEntries(entriesProperty);

            foreach (RecipeSO recipe in importedRecipes)
            {
                if (registeredEntries.Contains(recipe))
                {
                    continue;
                }

                int newIndex = entriesProperty.arraySize;
                entriesProperty.InsertArrayElementAtIndex(newIndex);
                entriesProperty.GetArrayElementAtIndex(newIndex).objectReferenceValue = recipe;
                registeredEntries.Add(recipe);
            }

            serializedDatabase.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);
        }

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

        private static string BuildErrorReport(IReadOnlyList<RecipeSheetValidationError> errors)
        {
            StringBuilder builder = new();
            builder.AppendLine($"Import 중단. 오류 {errors.Count}개를 먼저 수정하세요.");

            foreach (RecipeSheetValidationError error in errors)
            {
                string recipeId = string.IsNullOrWhiteSpace(error.RecipeId) ? "(id 없음)" : error.RecipeId;
                builder.AppendLine($"- {error.SheetName}/{recipeId}: {error.Message}");
            }

            return builder.ToString();
        }

        private static string SanitizeAssetName(string value)
        {
            return Regex.Replace(value, "[^a-zA-Z0-9_.가-힣-]", "_");
        }

        private static void EnsureFolder(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                return;
            }

            Directory.CreateDirectory(folderPath);
            AssetDatabase.Refresh();
        }
    }
}
#endif
