using System.Collections.Generic;

namespace TownLife.Dialogue
{
    /// <summary>
    /// 시트별 CSV 텍스트를 검증 가능한 대화 시트 데이터로 변환합니다.
    /// </summary>
    public static class DialogueSheetCsvMapper
    {
        /// <summary>
        /// 시트 이름을 키로 가진 CSV 텍스트 묶음을 대화 시트 데이터로 변환합니다.
        /// </summary>
        public static DialogueSheetData FromCsvTexts(IReadOnlyDictionary<string, string> csvTexts)
        {
            DialogueSheetData data = new();

            MapNodes(Parse(csvTexts, DialogueSheetNames.DialogueNodes), data);
            MapChoices(Parse(csvTexts, DialogueSheetNames.DialogueChoices), data);
            MapConditions(Parse(csvTexts, DialogueSheetNames.Conditions), data);
            MapEffects(Parse(csvTexts, DialogueSheetNames.Effects), data);
            MapQuests(Parse(csvTexts, DialogueSheetNames.QuestDefinitions), data);
            MapQuestSteps(Parse(csvTexts, DialogueSheetNames.QuestSteps), data);

            return data;
        }

        private static DialogueCsvTable Parse(IReadOnlyDictionary<string, string> csvTexts, string sheetName)
        {
            string csvText = csvTexts.TryGetValue(sheetName, out string value) ? value : string.Empty;
            return DialogueCsvParser.Parse(csvText);
        }

        private static void MapNodes(DialogueCsvTable table, DialogueSheetData data)
        {
            foreach (DialogueCsvRow row in table.Rows)
            {
                data.Nodes.Add(new DialogueNodeRow(
                    row.Get("id"),
                    row.Get("characterId"),
                    row.Get("text"),
                    row.Get("nextNodeId"),
                    row.Get("conditionGroupId"),
                    row.Get("effectGroupId"),
                    row.Get("memo")));
            }
        }

        private static void MapChoices(DialogueCsvTable table, DialogueSheetData data)
        {
            foreach (DialogueCsvRow row in table.Rows)
            {
                data.Choices.Add(new DialogueChoiceRow(
                    row.Get("nodeId"),
                    row.Get("choiceId"),
                    row.Get("text"),
                    row.Get("targetNodeId"),
                    row.Get("conditionGroupId"),
                    row.Get("effectGroupId"),
                    ParseInt(row.Get("sortOrder")),
                    row.Get("memo")));
            }
        }

        private static void MapConditions(DialogueCsvTable table, DialogueSheetData data)
        {
            foreach (DialogueCsvRow row in table.Rows)
            {
                data.Conditions.Add(new DialogueConditionRow(
                    row.Get("groupId"),
                    row.Get("type"),
                    row.Get("key"),
                    row.Get("operator"),
                    row.Get("value"),
                    row.Get("memo")));
            }
        }

        private static void MapEffects(DialogueCsvTable table, DialogueSheetData data)
        {
            foreach (DialogueCsvRow row in table.Rows)
            {
                data.Effects.Add(new DialogueEffectRow(
                    row.Get("groupId"),
                    row.Get("type"),
                    row.Get("key"),
                    row.Get("operation"),
                    row.Get("value"),
                    row.Get("memo")));
            }
        }

        private static void MapQuests(DialogueCsvTable table, DialogueSheetData data)
        {
            foreach (DialogueCsvRow row in table.Rows)
            {
                data.Quests.Add(new QuestDefinitionRow(
                    row.Get("questId"),
                    row.Get("displayName"),
                    row.Get("description"),
                    row.Get("startStepId"),
                    row.Get("memo")));
            }
        }

        private static void MapQuestSteps(DialogueCsvTable table, DialogueSheetData data)
        {
            foreach (DialogueCsvRow row in table.Rows)
            {
                data.QuestSteps.Add(new QuestStepRow(
                    row.Get("questId"),
                    row.Get("stepId"),
                    row.Get("description"),
                    row.Get("nextStepId"),
                    row.Get("completeConditionGroupId"),
                    row.Get("rewardEffectGroupId"),
                    row.Get("memo")));
            }
        }

        private static int ParseInt(string value)
        {
            return int.TryParse(value, out int parsedValue) ? parsedValue : 0;
        }
    }
}
