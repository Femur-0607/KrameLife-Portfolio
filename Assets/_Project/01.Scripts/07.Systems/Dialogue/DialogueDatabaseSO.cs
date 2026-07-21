using System;
using System.Collections.Generic;
using UnityEngine;

namespace TownLife.Dialogue
{
    [CreateAssetMenu(menuName = "TownLife/Dialogue Database")]
    public sealed class DialogueDatabaseSO : ScriptableObject
    {
        [SerializeField, Tooltip("Importer가 생성한 대화 노드 목록입니다. 직접 수정하지 않고 Google Sheets에서 관리합니다.")]
        private List<DialogueNodeData> nodes = new();

        [SerializeField, Tooltip("Importer가 생성한 선택지 목록입니다. 직접 수정하지 않고 Google Sheets에서 관리합니다.")]
        private List<DialogueChoiceData> choices = new();

        [SerializeField, Tooltip("Importer가 생성한 조건 목록입니다. 직접 수정하지 않고 Google Sheets에서 관리합니다.")]
        private List<DialogueConditionData> conditions = new();

        [SerializeField, Tooltip("Importer가 생성한 퀘스트 정의 목록입니다. 직접 수정하지 않고 Google Sheets에서 관리합니다.")]
        private List<QuestDefinitionData> quests = new();

        [SerializeField, Tooltip("Importer가 생성한 퀘스트 단계 목록입니다. 직접 수정하지 않고 Google Sheets에서 관리합니다.")]
        private List<QuestStepData> questSteps = new();

        [SerializeField, Tooltip("Importer가 생성한 효과 목록입니다. 직접 수정하지 않고 Google Sheets에서 관리합니다.")]
        private List<DialogueEffectData> effects = new();

        private Dictionary<string, DialogueNodeData> nodeMap;
        private Dictionary<string, List<DialogueConditionData>> conditionMap;
        private Dictionary<string, QuestDefinitionData> questMap;
        private Dictionary<string, QuestStepData> questStepMap;
        private Dictionary<string, List<DialogueEffectData>> effectMap;

        public IReadOnlyList<DialogueNodeData> Nodes => nodes;
        public IReadOnlyList<DialogueChoiceData> Choices => choices;
        /// <summary>
        /// Importer가 생성한 런타임 조건 목록입니다.
        /// </summary>
        public IReadOnlyList<DialogueConditionData> Conditions => conditions;
        public IReadOnlyList<QuestDefinitionData> Quests => quests;
        public IReadOnlyList<QuestStepData> QuestSteps => questSteps;
        public IReadOnlyList<DialogueEffectData> Effects => effects;

        public bool TryGetNode(string id, out DialogueNodeData node)
        {
            EnsureNodeMap();
            return nodeMap.TryGetValue(id, out node);
        }

        public List<DialogueChoiceData> GetChoices(string nodeId)
        {
            List<DialogueChoiceData> result = new();

            foreach (DialogueChoiceData choice in choices)
            {
                if (choice.NodeId == nodeId)
                {
                    result.Add(choice);
                }
            }

            result.Sort((left, right) => left.SortOrder.CompareTo(right.SortOrder));
            return result;
        }

        /// <summary>
        /// 지정한 조건 그룹에 속한 런타임 조건 목록을 반환합니다.
        /// </summary>
        public List<DialogueConditionData> GetConditions(string conditionGroupId)
        {
            EnsureConditionMap();

            if (string.IsNullOrWhiteSpace(conditionGroupId)
                || !conditionMap.TryGetValue(conditionGroupId, out List<DialogueConditionData> groupConditions))
            {
                return new List<DialogueConditionData>();
            }

            return new List<DialogueConditionData>(groupConditions);
        }

        public bool TryGetQuest(string questId, out QuestDefinitionData quest)
        {
            EnsureQuestMap();
            return questMap.TryGetValue(questId, out quest);
        }

        public bool TryGetQuestStep(string stepId, out QuestStepData step)
        {
            EnsureQuestStepMap();
            return questStepMap.TryGetValue(stepId, out step);
        }

        public List<QuestStepData> GetQuestSteps(string questId)
        {
            List<QuestStepData> result = new();

            foreach (QuestStepData step in questSteps)
            {
                if (step.QuestId == questId)
                {
                    result.Add(step);
                }
            }

            return result;
        }

        public List<DialogueEffectData> GetEffects(string effectGroupId)
        {
            EnsureEffectMap();

            if (string.IsNullOrWhiteSpace(effectGroupId)
                || !effectMap.TryGetValue(effectGroupId, out List<DialogueEffectData> groupEffects))
            {
                return new List<DialogueEffectData>();
            }

            return new List<DialogueEffectData>(groupEffects);
        }

        /// <summary>
        /// Importer가 시트에서 생성한 런타임 데이터 목록으로 데이터베이스를 갱신합니다.
        /// </summary>
        public void SetGeneratedData(
            List<DialogueNodeData> newNodes,
            List<DialogueChoiceData> newChoices,
            List<DialogueConditionData> newConditions,
            List<QuestDefinitionData> newQuests,
            List<QuestStepData> newQuestSteps,
            List<DialogueEffectData> newEffects)
        {
            nodes = newNodes;
            choices = newChoices;
            conditions = newConditions;
            quests = newQuests;
            questSteps = newQuestSteps;
            effects = newEffects;
            nodeMap = null;
            conditionMap = null;
            questMap = null;
            questStepMap = null;
            effectMap = null;
        }

        private void EnsureNodeMap()
        {
            if (nodeMap != null)
            {
                return;
            }

            nodeMap = new Dictionary<string, DialogueNodeData>();

            foreach (DialogueNodeData node in nodes)
            {
                if (!string.IsNullOrWhiteSpace(node.Id))
                {
                    nodeMap[node.Id] = node;
                }
            }
        }

        private void EnsureQuestMap()
        {
            if (questMap != null)
            {
                return;
            }

            questMap = new Dictionary<string, QuestDefinitionData>();

            foreach (QuestDefinitionData quest in quests)
            {
                if (!string.IsNullOrWhiteSpace(quest.QuestId))
                {
                    questMap[quest.QuestId] = quest;
                }
            }
        }

        private void EnsureConditionMap()
        {
            if (conditionMap != null)
            {
                return;
            }

            conditionMap = new Dictionary<string, List<DialogueConditionData>>();

            foreach (DialogueConditionData condition in conditions)
            {
                if (string.IsNullOrWhiteSpace(condition.ConditionGroupId))
                {
                    continue;
                }

                if (!conditionMap.TryGetValue(condition.ConditionGroupId, out List<DialogueConditionData> groupConditions))
                {
                    groupConditions = new List<DialogueConditionData>();
                    conditionMap[condition.ConditionGroupId] = groupConditions;
                }

                groupConditions.Add(condition);
            }
        }

        private void EnsureQuestStepMap()
        {
            if (questStepMap != null)
            {
                return;
            }

            questStepMap = new Dictionary<string, QuestStepData>();

            foreach (QuestStepData step in questSteps)
            {
                if (!string.IsNullOrWhiteSpace(step.StepId))
                {
                    questStepMap[step.StepId] = step;
                }
            }
        }

        private void EnsureEffectMap()
        {
            if (effectMap != null)
            {
                return;
            }

            effectMap = new Dictionary<string, List<DialogueEffectData>>();

            foreach (DialogueEffectData effect in effects)
            {
                if (string.IsNullOrWhiteSpace(effect.EffectGroupId))
                {
                    continue;
                }

                if (!effectMap.TryGetValue(effect.EffectGroupId, out List<DialogueEffectData> groupEffects))
                {
                    groupEffects = new List<DialogueEffectData>();
                    effectMap[effect.EffectGroupId] = groupEffects;
                }

                groupEffects.Add(effect);
            }
        }
    }

    [Serializable]
    public sealed class DialogueNodeData
    {
        public string Id;
        public string CharacterId;
        public string Text;
        public string NextNodeId;
        public string ConditionGroupId;
        public string EffectGroupId;
    }

    [Serializable]
    public sealed class DialogueChoiceData
    {
        public string NodeId;
        public string ChoiceId;
        public string Text;
        public string TargetNodeId;
        public string ConditionGroupId;
        public string EffectGroupId;
        public int SortOrder;
    }

    [Serializable]
    public sealed class DialogueConditionData
    {
        public string ConditionGroupId;
        public string ConditionType;
        public string Key;
        public string Operator;
        public string Value;
        public string Memo;
    }

    [Serializable]
    public sealed class QuestDefinitionData
    {
        public string QuestId;
        public string DisplayName;
        public string Description;
        public string StartStepId;
        public string Memo;
    }

    [Serializable]
    public sealed class QuestStepData
    {
        public string QuestId;
        public string StepId;
        public string Description;
        public string NextStepId;
        public string CompleteConditionGroupId;
        public string RewardEffectGroupId;
        public string Memo;
    }

    [Serializable]
    public sealed class DialogueEffectData
    {
        public string EffectGroupId;
        public string EffectType;
        public string TargetId;
        public string Operation;
        public string Value;
        public string Memo;
    }
}
