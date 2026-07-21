namespace TownLife.Dialogue
{
    /// <summary>
    /// DialogueNodes 시트의 한 행입니다.
    /// </summary>
    public sealed class DialogueNodeRow
    {
        public DialogueNodeRow(string id, string characterId, string text, string nextNodeId, string conditionGroupId, string effectGroupId, string memo)
        {
            Id = id;
            CharacterId = characterId;
            Text = text;
            NextNodeId = nextNodeId;
            ConditionGroupId = conditionGroupId;
            EffectGroupId = effectGroupId;
            Memo = memo;
        }

        public string Id { get; }
        public string CharacterId { get; }
        public string Text { get; }
        public string NextNodeId { get; }
        public string ConditionGroupId { get; }
        public string EffectGroupId { get; }
        public string Memo { get; }
    }

    /// <summary>
    /// DialogueChoices 시트의 한 행입니다.
    /// </summary>
    public sealed class DialogueChoiceRow
    {
        public DialogueChoiceRow(string nodeId, string choiceId, string text, string targetNodeId, string conditionGroupId, string effectGroupId, int sortOrder, string memo)
        {
            NodeId = nodeId;
            ChoiceId = choiceId;
            Text = text;
            TargetNodeId = targetNodeId;
            ConditionGroupId = conditionGroupId;
            EffectGroupId = effectGroupId;
            SortOrder = sortOrder;
            Memo = memo;
        }

        public string NodeId { get; }
        public string ChoiceId { get; }
        public string Text { get; }
        public string TargetNodeId { get; }
        public string ConditionGroupId { get; }
        public string EffectGroupId { get; }
        public int SortOrder { get; }
        public string Memo { get; }
    }

    /// <summary>
    /// Conditions 시트의 한 행입니다.
    /// </summary>
    public sealed class DialogueConditionRow
    {
        public DialogueConditionRow(string groupId, string type, string key, string operatorText, string value, string memo)
        {
            GroupId = groupId;
            Type = type;
            Key = key;
            Operator = operatorText;
            Value = value;
            Memo = memo;
        }

        public string GroupId { get; }
        public string Type { get; }
        public string Key { get; }
        public string Operator { get; }
        public string Value { get; }
        public string Memo { get; }
    }

    /// <summary>
    /// Effects 시트의 한 행입니다.
    /// </summary>
    public sealed class DialogueEffectRow
    {
        public DialogueEffectRow(string groupId, string type, string key, string operation, string value, string memo)
        {
            GroupId = groupId;
            Type = type;
            Key = key;
            Operation = operation;
            Value = value;
            Memo = memo;
        }

        public string GroupId { get; }
        public string Type { get; }
        public string Key { get; }
        public string Operation { get; }
        public string Value { get; }
        public string Memo { get; }
    }

    /// <summary>
    /// QuestDefinitions 시트의 한 행입니다.
    /// </summary>
    public sealed class QuestDefinitionRow
    {
        public QuestDefinitionRow(string questId, string displayName, string description, string startStepId, string memo)
        {
            QuestId = questId;
            DisplayName = displayName;
            Description = description;
            StartStepId = startStepId;
            Memo = memo;
        }

        public string QuestId { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public string StartStepId { get; }
        public string Memo { get; }
    }

    /// <summary>
    /// QuestSteps 시트의 한 행입니다.
    /// </summary>
    public sealed class QuestStepRow
    {
        public QuestStepRow(string questId, string stepId, string description, string nextStepId, string completeConditionGroupId, string rewardEffectGroupId, string memo)
        {
            QuestId = questId;
            StepId = stepId;
            Description = description;
            NextStepId = nextStepId;
            CompleteConditionGroupId = completeConditionGroupId;
            RewardEffectGroupId = rewardEffectGroupId;
            Memo = memo;
        }

        public string QuestId { get; }
        public string StepId { get; }
        public string Description { get; }
        public string NextStepId { get; }
        public string CompleteConditionGroupId { get; }
        public string RewardEffectGroupId { get; }
        public string Memo { get; }
    }
}
