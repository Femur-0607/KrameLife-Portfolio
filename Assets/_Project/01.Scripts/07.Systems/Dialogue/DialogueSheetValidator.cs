using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TownLife.Dialogue
{
    /// <summary>
    /// Google Sheets에서 가져온 대화/퀘스트 데이터의 참조와 허용 값을 검증합니다.
    /// </summary>
    public static class DialogueSheetValidator
    {
        private static readonly Regex IdPattern = new("^[a-z0-9_]+$");
        private static readonly HashSet<string> ConditionTypes = new() { "affection", "quest", "item", "flag" };
        private static readonly HashSet<string> ConditionOperators = new() { "==", "!=", ">", ">=", "<", "<=" };
        private static readonly HashSet<string> EffectTypes = new() { "affection", "quest", "item", "flag", "gold", "reputation" };
        private static readonly HashSet<string> EffectOperations = new() { "set", "add", "remove", "start", "complete", "complete_step", "fail" };

        /// <summary>
        /// 시트 데이터 전체를 검증하고 발견된 문제 목록을 반환합니다.
        /// </summary>
        public static IReadOnlyList<DialogueSheetValidationError> Validate(DialogueSheetData data)
        {
            List<DialogueSheetValidationError> errors = new();

            HashSet<string> nodeIds = BuildIdSet(data.Nodes, node => node.Id, DialogueSheetNames.DialogueNodes, "id", errors);
            HashSet<string> conditionGroupIds = BuildIdSet(data.Conditions, condition => condition.GroupId, DialogueSheetNames.Conditions, "groupId", errors, allowDuplicates: true);
            HashSet<string> effectGroupIds = BuildIdSet(data.Effects, effect => effect.GroupId, DialogueSheetNames.Effects, "groupId", errors, allowDuplicates: true);
            HashSet<string> questIds = BuildIdSet(data.Quests, quest => quest.QuestId, DialogueSheetNames.QuestDefinitions, "questId", errors);
            HashSet<string> questStepIds = BuildIdSet(data.QuestSteps, step => step.StepId, DialogueSheetNames.QuestSteps, "stepId", errors);

            ValidateNodes(data.Nodes, nodeIds, conditionGroupIds, effectGroupIds, errors);
            ValidateChoices(data.Choices, nodeIds, conditionGroupIds, effectGroupIds, errors);
            ValidateConditions(data.Conditions, questIds, errors);
            ValidateEffects(data.Effects, questIds, errors);
            ValidateQuests(data.Quests, questStepIds, errors);
            ValidateQuestSteps(data.QuestSteps, questIds, questStepIds, conditionGroupIds, effectGroupIds, errors);

            return errors;
        }

        private static void ValidateNodes(
            IEnumerable<DialogueNodeRow> nodes,
            HashSet<string> nodeIds,
            HashSet<string> conditionGroupIds,
            HashSet<string> effectGroupIds,
            ICollection<DialogueSheetValidationError> errors)
        {
            foreach (DialogueNodeRow node in nodes)
            {
                RequireValue(node.Id, DialogueSheetNames.DialogueNodes, node.Id, "id", errors);
                RequireValue(node.CharacterId, DialogueSheetNames.DialogueNodes, node.Id, "characterId", errors);
                RequireValue(node.Text, DialogueSheetNames.DialogueNodes, node.Id, "text", errors);
                ValidateIdFormat(node.Id, DialogueSheetNames.DialogueNodes, node.Id, "id", errors);
                ValidateOptionalReference(node.NextNodeId, nodeIds, DialogueSheetNames.DialogueNodes, node.Id, "nextNodeId", errors);
                ValidateOptionalReference(node.ConditionGroupId, conditionGroupIds, DialogueSheetNames.DialogueNodes, node.Id, "conditionGroupId", errors);
                ValidateOptionalReference(node.EffectGroupId, effectGroupIds, DialogueSheetNames.DialogueNodes, node.Id, "effectGroupId", errors);
            }
        }

        private static void ValidateChoices(
            IEnumerable<DialogueChoiceRow> choices,
            HashSet<string> nodeIds,
            HashSet<string> conditionGroupIds,
            HashSet<string> effectGroupIds,
            ICollection<DialogueSheetValidationError> errors)
        {
            foreach (DialogueChoiceRow choice in choices)
            {
                RequireValue(choice.NodeId, DialogueSheetNames.DialogueChoices, choice.ChoiceId, "nodeId", errors);
                RequireValue(choice.ChoiceId, DialogueSheetNames.DialogueChoices, choice.ChoiceId, "choiceId", errors);
                RequireValue(choice.Text, DialogueSheetNames.DialogueChoices, choice.ChoiceId, "text", errors);
                RequireValue(choice.TargetNodeId, DialogueSheetNames.DialogueChoices, choice.ChoiceId, "targetNodeId", errors);
                ValidateIdFormat(choice.ChoiceId, DialogueSheetNames.DialogueChoices, choice.ChoiceId, "choiceId", errors);
                ValidateRequiredReference(choice.NodeId, nodeIds, DialogueSheetNames.DialogueChoices, choice.ChoiceId, "nodeId", errors);
                ValidateRequiredReference(choice.TargetNodeId, nodeIds, DialogueSheetNames.DialogueChoices, choice.ChoiceId, "targetNodeId", errors);
                ValidateOptionalReference(choice.ConditionGroupId, conditionGroupIds, DialogueSheetNames.DialogueChoices, choice.ChoiceId, "conditionGroupId", errors);
                ValidateOptionalReference(choice.EffectGroupId, effectGroupIds, DialogueSheetNames.DialogueChoices, choice.ChoiceId, "effectGroupId", errors);
            }
        }

        private static void ValidateConditions(
            IEnumerable<DialogueConditionRow> conditions,
            HashSet<string> questIds,
            ICollection<DialogueSheetValidationError> errors)
        {
            foreach (DialogueConditionRow condition in conditions)
            {
                RequireValue(condition.GroupId, DialogueSheetNames.Conditions, condition.GroupId, "groupId", errors);
                RequireValue(condition.Type, DialogueSheetNames.Conditions, condition.GroupId, "type", errors);
                RequireValue(condition.Key, DialogueSheetNames.Conditions, condition.GroupId, "key", errors);
                RequireValue(condition.Operator, DialogueSheetNames.Conditions, condition.GroupId, "operator", errors);
                ValidateIdFormat(condition.GroupId, DialogueSheetNames.Conditions, condition.GroupId, "groupId", errors);
                ValidateAllowedValue(condition.Type, ConditionTypes, DialogueSheetNames.Conditions, condition.GroupId, "type", errors);
                ValidateAllowedValue(condition.Operator, ConditionOperators, DialogueSheetNames.Conditions, condition.GroupId, "operator", errors);

                if (condition.Type == "quest")
                {
                    ValidateRequiredReference(condition.Key, questIds, DialogueSheetNames.Conditions, condition.GroupId, "key", errors);
                }
            }
        }

        private static void ValidateEffects(
            IEnumerable<DialogueEffectRow> effects,
            HashSet<string> questIds,
            ICollection<DialogueSheetValidationError> errors)
        {
            foreach (DialogueEffectRow effect in effects)
            {
                RequireValue(effect.GroupId, DialogueSheetNames.Effects, effect.GroupId, "groupId", errors);
                RequireValue(effect.Type, DialogueSheetNames.Effects, effect.GroupId, "type", errors);
                RequireValue(effect.Key, DialogueSheetNames.Effects, effect.GroupId, "key", errors);
                RequireValue(effect.Operation, DialogueSheetNames.Effects, effect.GroupId, "operation", errors);
                ValidateIdFormat(effect.GroupId, DialogueSheetNames.Effects, effect.GroupId, "groupId", errors);
                ValidateAllowedValue(effect.Type, EffectTypes, DialogueSheetNames.Effects, effect.GroupId, "type", errors);
                ValidateAllowedValue(effect.Operation, EffectOperations, DialogueSheetNames.Effects, effect.GroupId, "operation", errors);

                if (effect.Type == "quest")
                {
                    ValidateRequiredReference(effect.Key, questIds, DialogueSheetNames.Effects, effect.GroupId, "key", errors);
                }
            }
        }

        private static void ValidateQuests(
            IEnumerable<QuestDefinitionRow> quests,
            HashSet<string> questStepIds,
            ICollection<DialogueSheetValidationError> errors)
        {
            foreach (QuestDefinitionRow quest in quests)
            {
                RequireValue(quest.QuestId, DialogueSheetNames.QuestDefinitions, quest.QuestId, "questId", errors);
                RequireValue(quest.DisplayName, DialogueSheetNames.QuestDefinitions, quest.QuestId, "displayName", errors);
                RequireValue(quest.StartStepId, DialogueSheetNames.QuestDefinitions, quest.QuestId, "startStepId", errors);
                ValidateIdFormat(quest.QuestId, DialogueSheetNames.QuestDefinitions, quest.QuestId, "questId", errors);
                ValidateRequiredReference(quest.StartStepId, questStepIds, DialogueSheetNames.QuestDefinitions, quest.QuestId, "startStepId", errors);
            }
        }

        private static void ValidateQuestSteps(
            IEnumerable<QuestStepRow> steps,
            HashSet<string> questIds,
            HashSet<string> questStepIds,
            HashSet<string> conditionGroupIds,
            HashSet<string> effectGroupIds,
            ICollection<DialogueSheetValidationError> errors)
        {
            foreach (QuestStepRow step in steps)
            {
                RequireValue(step.QuestId, DialogueSheetNames.QuestSteps, step.StepId, "questId", errors);
                RequireValue(step.StepId, DialogueSheetNames.QuestSteps, step.StepId, "stepId", errors);
                RequireValue(step.Description, DialogueSheetNames.QuestSteps, step.StepId, "description", errors);
                ValidateIdFormat(step.StepId, DialogueSheetNames.QuestSteps, step.StepId, "stepId", errors);
                ValidateRequiredReference(step.QuestId, questIds, DialogueSheetNames.QuestSteps, step.StepId, "questId", errors);
                ValidateOptionalReference(step.NextStepId, questStepIds, DialogueSheetNames.QuestSteps, step.StepId, "nextStepId", errors);
                ValidateOptionalReference(step.CompleteConditionGroupId, conditionGroupIds, DialogueSheetNames.QuestSteps, step.StepId, "completeConditionGroupId", errors);
                ValidateOptionalReference(step.RewardEffectGroupId, effectGroupIds, DialogueSheetNames.QuestSteps, step.StepId, "rewardEffectGroupId", errors);
            }
        }

        private static HashSet<string> BuildIdSet<T>(
            IEnumerable<T> rows,
            System.Func<T, string> getId,
            string sheetName,
            string fieldName,
            ICollection<DialogueSheetValidationError> errors,
            bool allowDuplicates = false)
        {
            HashSet<string> ids = new();

            foreach (T row in rows)
            {
                string id = getId(row);
                if (string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                if (!ids.Add(id) && !allowDuplicates)
                {
                    errors.Add(new DialogueSheetValidationError(sheetName, id, $"중복된 {fieldName}입니다."));
                }
            }

            return ids;
        }

        private static void RequireValue(string value, string sheetName, string rowId, string fieldName, ICollection<DialogueSheetValidationError> errors)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add(new DialogueSheetValidationError(sheetName, rowId, $"{fieldName} 값이 비어 있습니다."));
            }
        }

        private static void ValidateIdFormat(string value, string sheetName, string rowId, string fieldName, ICollection<DialogueSheetValidationError> errors)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            if (!IdPattern.IsMatch(value))
            {
                errors.Add(new DialogueSheetValidationError(sheetName, rowId, $"{fieldName}는 소문자 영어, 숫자, 언더스코어만 사용할 수 있습니다."));
            }
        }

        private static void ValidateAllowedValue(string value, HashSet<string> allowedValues, string sheetName, string rowId, string fieldName, ICollection<DialogueSheetValidationError> errors)
        {
            if (string.IsNullOrWhiteSpace(value) || allowedValues.Contains(value))
            {
                return;
            }

            errors.Add(new DialogueSheetValidationError(sheetName, rowId, $"지원하지 않는 {fieldName} 값입니다. value: {value}"));
        }

        private static void ValidateRequiredReference(string value, HashSet<string> validIds, string sheetName, string rowId, string fieldName, ICollection<DialogueSheetValidationError> errors)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            if (!validIds.Contains(value))
            {
                errors.Add(new DialogueSheetValidationError(sheetName, rowId, $"{fieldName}가 존재하지 않는 ID를 참조합니다. value: {value}"));
            }
        }

        private static void ValidateOptionalReference(string value, HashSet<string> validIds, string sheetName, string rowId, string fieldName, ICollection<DialogueSheetValidationError> errors)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            ValidateRequiredReference(value, validIds, sheetName, rowId, fieldName, errors);
        }
    }
}
