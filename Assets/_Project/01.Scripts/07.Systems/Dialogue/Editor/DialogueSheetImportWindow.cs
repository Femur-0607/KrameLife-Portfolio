#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace TownLife.Dialogue.Editor
{
    /// <summary>
    /// Google Sheets 대화 데이터를 수동으로 내려받고 검증하는 에디터 창입니다.
    /// </summary>
    public sealed class DialogueSheetImportWindow : EditorWindow
    {
        private const string DefaultSettingsPath = "Assets/_Project/Dialogue/DialogueImportSettings.asset";
        private const string DatabasePath = "Assets/_Project/Dialogue/Generated/DialogueDatabase.asset";

        [SerializeField, Tooltip("대화 데이터 Import 설정 에셋입니다.")]
        private DialogueImportSettingsSO settings;

        private Vector2 scrollPosition;
        private string lastResult = "아직 Import하지 않았습니다.";
        private bool isImporting;

        /// <summary>
        /// 대화 데이터 Importer 에디터 창을 열고 기본 설정 에셋을 불러옵니다.
        /// </summary>
        [MenuItem("Tools/TownLife/Dialogue Data Importer")]
        private static void Open()
        {
            DialogueSheetImportWindow window = GetWindow<DialogueSheetImportWindow>("Dialogue Importer");
            window.minSize = new Vector2(520f, 360f);
            window.LoadDefaultSettings();
            window.Show();
        }

        /// <summary>
        /// Import 설정 선택, 기본 설정 생성, Import 실행 버튼과 결과 메시지를 그립니다.
        /// </summary>
        private void OnGUI()
        {
            EditorGUILayout.LabelField("TownLife Dialogue Data Importer", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            settings = (DialogueImportSettingsSO)EditorGUILayout.ObjectField(
                "Import Settings",
                settings,
                typeof(DialogueImportSettingsSO),
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

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(settings == null || isImporting))
            {
                if (GUILayout.Button(isImporting ? "Importing..." : "Import Dialogue Data"))
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
            settings = AssetDatabase.LoadAssetAtPath<DialogueImportSettingsSO>(DefaultSettingsPath);
        }

        /// <summary>
        /// 기본 Import 설정 에셋이 없으면 생성하고, 있으면 기존 에셋을 불러옵니다.
        /// </summary>
        private void CreateDefaultSettings()
        {
            DialogueImportSettingsSO existingSettings = AssetDatabase.LoadAssetAtPath<DialogueImportSettingsSO>(DefaultSettingsPath);
            if (existingSettings != null)
            {
                settings = existingSettings;
                lastResult = "기존 기본 설정 에셋을 불러왔습니다.";
                return;
            }

            EnsureFolder("Assets/_Project/Dialogue");

            DialogueImportSettingsSO newSettings = CreateInstance<DialogueImportSettingsSO>();
            AssetDatabase.CreateAsset(newSettings, DefaultSettingsPath);
            AssetDatabase.SaveAssets();
            settings = newSettings;
            lastResult = "기본 설정 에셋을 생성했습니다.";
        }

        /// <summary>
        /// Google Sheets CSV 다운로드, 데이터 매핑, 검증, CSV 캐시 저장, DialogueDatabase 갱신을 순서대로 실행합니다.
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
                Dictionary<string, string> csvTexts = await DownloadAllCsvAsync(settings);
                DialogueSheetData data = DialogueSheetCsvMapper.FromCsvTexts(csvTexts);
                IReadOnlyList<DialogueSheetValidationError> errors = DialogueSheetValidator.Validate(data);

                if (errors.Count > 0)
                {
                    lastResult = BuildErrorReport(errors);
                    return;
                }

                SaveCsvCache(settings.GeneratedFolderPath, csvTexts);

                DialogueDatabaseSO database = LoadOrCreateDatabase();
                database.SetGeneratedData(
                    BuildNodeData(data.Nodes),
                    BuildChoiceData(data.Choices),
                    BuildConditionData(data.Conditions),
                    BuildQuestData(data.Quests),
                    BuildQuestStepData(data.QuestSteps),
                    BuildEffectData(data.Effects));

                EditorUtility.SetDirty(database);
                AssetDatabase.SaveAssets();

                lastResult = $"Import 성공. CSV 캐시와 DialogueDatabase 저장 완료: {settings.GeneratedFolderPath}";
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
        /// Import 설정에 등록된 모든 시트를 CSV 텍스트로 다운로드합니다.
        /// </summary>
        private static async Task<Dictionary<string, string>> DownloadAllCsvAsync(DialogueImportSettingsSO settings)
        {
            Dictionary<string, string> csvTexts = new();

            foreach (DialogueImportSheetConfig sheet in settings.Sheets)
            {
                string url = DialogueSheetCsvUrlBuilder.BuildGoogleCsvExportUrl(settings.SpreadsheetId, sheet.Gid);
                csvTexts[sheet.SheetName] = await DownloadTextAsync(url);
            }

            return csvTexts;
        }

        /// <summary>
        /// 지정한 URL에서 텍스트를 다운로드하고 실패하면 UnityException을 발생시킵니다.
        /// </summary>
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

        /// <summary>
        /// 검증을 통과한 시트 CSV 원본을 프로젝트 폴더에 저장합니다.
        /// </summary>
        private static void SaveCsvCache(string folderPath, IReadOnlyDictionary<string, string> csvTexts)
        {
            EnsureFolder(folderPath);

            foreach (KeyValuePair<string, string> pair in csvTexts)
            {
                string filePath = Path.Combine(folderPath, $"{pair.Key}.csv").Replace('\\', '/');
                File.WriteAllText(filePath, pair.Value, System.Text.Encoding.UTF8);
            }

            AssetDatabase.Refresh();
        }

        /// <summary>
        /// DialogueDatabase 에셋을 불러오거나 없으면 Generated 폴더에 새로 생성합니다.
        /// </summary>
        private static DialogueDatabaseSO LoadOrCreateDatabase()
        {
            DialogueDatabaseSO database = AssetDatabase.LoadAssetAtPath<DialogueDatabaseSO>(DatabasePath);
            if (database != null)
            {
                return database;
            }

            EnsureFolder(Path.GetDirectoryName(DatabasePath));

            database = CreateInstance<DialogueDatabaseSO>();
            AssetDatabase.CreateAsset(database, DatabasePath);
            return database;
        }

        /// <summary>
        /// 검증된 DialogueNodes 행을 런타임 대화 노드 데이터로 변환합니다.
        /// </summary>
        private static List<DialogueNodeData> BuildNodeData(IEnumerable<DialogueNodeRow> rows)
        {
            List<DialogueNodeData> result = new();

            foreach (DialogueNodeRow row in rows)
            {
                result.Add(new DialogueNodeData
                {
                    Id = row.Id,
                    CharacterId = row.CharacterId,
                    Text = row.Text,
                    NextNodeId = row.NextNodeId,
                    ConditionGroupId = row.ConditionGroupId,
                    EffectGroupId = row.EffectGroupId,
                });
            }

            return result;
        }

        /// <summary>
        /// 검증된 DialogueChoices 행을 런타임 선택지 데이터로 변환합니다.
        /// </summary>
        private static List<DialogueChoiceData> BuildChoiceData(IEnumerable<DialogueChoiceRow> rows)
        {
            List<DialogueChoiceData> result = new();

            foreach (DialogueChoiceRow row in rows)
            {
                result.Add(new DialogueChoiceData
                {
                    NodeId = row.NodeId,
                    ChoiceId = row.ChoiceId,
                    Text = row.Text,
                    TargetNodeId = row.TargetNodeId,
                    ConditionGroupId = row.ConditionGroupId,
                    EffectGroupId = row.EffectGroupId,
                    SortOrder = row.SortOrder,
                });
            }

            return result;
        }

        /// <summary>
        /// 검증된 Conditions 행을 런타임 조건 데이터로 변환합니다.
        /// </summary>
        private static List<DialogueConditionData> BuildConditionData(IEnumerable<DialogueConditionRow> rows)
        {
            List<DialogueConditionData> result = new();

            foreach (DialogueConditionRow row in rows)
            {
                result.Add(new DialogueConditionData
                {
                    ConditionGroupId = row.GroupId,
                    ConditionType = row.Type,
                    Key = row.Key,
                    Operator = row.Operator,
                    Value = row.Value,
                    Memo = row.Memo,
                });
            }

            return result;
        }

        /// <summary>
        /// 검증된 QuestDefinitions 행을 런타임 퀘스트 데이터로 변환합니다.
        /// </summary>
        private static List<QuestDefinitionData> BuildQuestData(IEnumerable<QuestDefinitionRow> rows)
        {
            List<QuestDefinitionData> result = new();

            foreach (QuestDefinitionRow row in rows)
            {
                result.Add(new QuestDefinitionData
                {
                    QuestId = row.QuestId,
                    DisplayName = row.DisplayName,
                    Description = row.Description,
                    StartStepId = row.StartStepId,
                    Memo = row.Memo,
                });
            }

            return result;
        }

        /// <summary>
        /// 검증된 QuestSteps 행을 런타임 퀘스트 단계 데이터로 변환합니다.
        /// </summary>
        private static List<QuestStepData> BuildQuestStepData(IEnumerable<QuestStepRow> rows)
        {
            List<QuestStepData> result = new();

            foreach (QuestStepRow row in rows)
            {
                result.Add(new QuestStepData
                {
                    QuestId = row.QuestId,
                    StepId = row.StepId,
                    Description = row.Description,
                    NextStepId = row.NextStepId,
                    CompleteConditionGroupId = row.CompleteConditionGroupId,
                    RewardEffectGroupId = row.RewardEffectGroupId,
                    Memo = row.Memo,
                });
            }

            return result;
        }

        /// <summary>
        /// 검증된 Effects 행을 런타임 효과 데이터로 변환합니다.
        /// </summary>
        private static List<DialogueEffectData> BuildEffectData(IEnumerable<DialogueEffectRow> rows)
        {
            List<DialogueEffectData> result = new();

            foreach (DialogueEffectRow row in rows)
            {
                result.Add(new DialogueEffectData
                {
                    EffectGroupId = row.GroupId,
                    EffectType = row.Type,
                    TargetId = row.Key,
                    Operation = row.Operation,
                    Value = row.Value,
                    Memo = row.Memo,
                });
            }

            return result;
        }

        /// <summary>
        /// 검증 오류 목록을 에디터 창에 표시할 텍스트 보고서로 변환합니다.
        /// </summary>
        private static string BuildErrorReport(IReadOnlyList<DialogueSheetValidationError> errors)
        {
            System.Text.StringBuilder builder = new();
            builder.AppendLine($"Import 중단. 검증 오류 {errors.Count}개:");

            foreach (DialogueSheetValidationError error in errors)
            {
                builder.AppendLine($"- {error}");
            }

            return builder.ToString();
        }

        /// <summary>
        /// 지정한 프로젝트 상대 폴더 경로가 없으면 단계별로 생성합니다.
        /// </summary>
        private static void EnsureFolder(string folderPath)
        {
            string normalizedPath = folderPath.Replace('\\', '/').TrimEnd('/');
            if (AssetDatabase.IsValidFolder(normalizedPath))
            {
                return;
            }

            string[] parts = normalizedPath.Split('/');
            string currentPath = parts[0];

            for (int index = 1; index < parts.Length; index++)
            {
                string nextPath = $"{currentPath}/{parts[index]}";
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, parts[index]);
                }

                currentPath = nextPath;
            }
        }
    }
}
#endif
