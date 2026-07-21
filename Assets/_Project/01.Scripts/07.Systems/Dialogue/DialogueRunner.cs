using System;
using System.Collections.Generic;
using TownLife.Core;
using TownLife.Gameplay;
using TownLife.Quests;
using UnityEngine;

namespace TownLife.Dialogue
{
    /// <summary>
    /// DialogueDatabase의 노드와 선택지를 따라가며 현재 대화 흐름을 진행합니다.
    /// </summary>
    public sealed class DialogueRunner : MonoBehaviour
    {
        [SerializeField, Tooltip("대화 노드와 선택지를 조회할 런타임 대화 데이터베이스입니다.")]
        private DialogueDatabaseSO database;

        private DialogueNodeData currentNode;
        private List<DialogueChoiceData> currentChoices = new();
        private QuestManager questManager;
        private IInventory inventory;
        private IGold gold;
        private ShopReputationManager reputationManager;

        public event Action<DialogueNodeData, IReadOnlyList<DialogueChoiceData>> DialogueChanged;
        public event Action DialogueEnded;

        public bool IsDialogueActive => currentNode != null;
        public string CurrentNodeId => currentNode != null ? currentNode.Id : string.Empty;
        public DialogueNodeData CurrentNode => currentNode;
        public IReadOnlyList<DialogueChoiceData> CurrentChoices => currentChoices;

        /// <summary>
        /// 코드나 테스트에서 사용할 대화 데이터베이스를 주입합니다.
        /// </summary>
        public void Initialize(DialogueDatabaseSO dialogueDatabase)
        {
            database = dialogueDatabase;
            currentNode = null;
            currentChoices.Clear();
        }

        /// <summary>
        /// 지정한 노드 ID에서 대화를 시작하고 현재 대사를 출력합니다.
        /// </summary>
        public bool StartDialogue(string startNodeId)
        {
            if (database == null)
            {
                Debug.LogWarning("[Dialogue] DialogueDatabase가 없습니다.", this);
                return false;
            }

            if (string.IsNullOrWhiteSpace(startNodeId))
            {
                Debug.LogWarning("[Dialogue] 시작 노드 ID가 비어 있습니다.", this);
                return false;
            }

            return MoveToNode(startNodeId);
        }

        /// <summary>
        /// 선택지가 없는 현재 노드에서 nextNodeId로 진행합니다.
        /// </summary>
        public bool Advance()
        {
            if (currentNode == null)
            {
                return false;
            }

            if (currentChoices.Count > 0)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(currentNode.NextNodeId))
            {
                EndDialogue();
                return false;
            }

            return MoveToNode(currentNode.NextNodeId);
        }

        /// <summary>
        /// 현재 노드의 선택지 번호를 골라 targetNodeId로 이동합니다.
        /// </summary>
        public bool SelectChoice(int choiceIndex)
        {
            if (currentNode == null)
            {
                return false;
            }

            if (choiceIndex < 0 || choiceIndex >= currentChoices.Count)
            {
                return false;
            }

            DialogueChoiceData choice = currentChoices[choiceIndex];
            if (!TryApplyEffectGroup(choice.EffectGroupId))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(choice.TargetNodeId))
            {
                EndDialogue();
                return false;
            }

            return MoveToNode(choice.TargetNodeId);
        }

        private bool MoveToNode(string nodeId)
        {
            if (!database.TryGetNode(nodeId, out DialogueNodeData node))
            {
                Debug.LogWarning($"[Dialogue] 노드를 찾을 수 없습니다. id: {nodeId}", this);
                return false;
            }

            if (!EvaluateConditionGroup(node.ConditionGroupId))
            {
                Debug.LogWarning($"[Dialogue] 노드 조건을 충족하지 못했습니다. id: {nodeId}", this);
                return false;
            }

            currentNode = node;
            currentChoices = GetAvailableChoices(node.Id);
            DialogueChanged?.Invoke(currentNode, currentChoices);
            TryApplyEffectGroup(currentNode.EffectGroupId);
            return true;
        }

        /// <summary>
        /// 현재 상태에서 조건을 만족하는 선택지만 반환합니다.
        /// </summary>
        private List<DialogueChoiceData> GetAvailableChoices(string nodeId)
        {
            List<DialogueChoiceData> choices = database.GetChoices(nodeId);
            choices.RemoveAll(choice => !EvaluateConditionGroup(choice.ConditionGroupId));
            return choices;
        }

        /// <summary>
        /// 조건 그룹의 모든 행을 AND 방식으로 평가합니다.
        /// </summary>
        private bool EvaluateConditionGroup(string conditionGroupId)
        {
            if (string.IsNullOrWhiteSpace(conditionGroupId))
            {
                return true;
            }

            List<DialogueConditionData> conditions = database.GetConditions(conditionGroupId);
            if (conditions.Count == 0)
            {
                return false;
            }

            foreach (DialogueConditionData condition in conditions)
            {
                if (!EvaluateCondition(condition))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 아이템 수량 또는 퀘스트 상태 조건 한 행을 평가합니다.
        /// </summary>
        private bool EvaluateCondition(DialogueConditionData condition)
        {
            if (string.Equals(condition.ConditionType, "item", StringComparison.OrdinalIgnoreCase))
            {
                IInventory resolvedInventory = ResolveInventory();
                return resolvedInventory != null
                    && int.TryParse(condition.Value, out int requiredCount)
                    && Compare(resolvedInventory.Count(condition.Key), condition.Operator, requiredCount);
            }

            if (string.Equals(condition.ConditionType, "quest", StringComparison.OrdinalIgnoreCase))
            {
                QuestManager manager = ResolveQuestManager();
                string state = manager != null
                    ? FormatQuestState(manager.GetQuestState(condition.Key))
                    : string.Empty;
                return Compare(state, condition.Operator, condition.Value);
            }

            return false;
        }

        /// <summary>
        /// 효과 그룹을 검증한 뒤 퀘스트, 아이템, 보상 순서로 적용합니다.
        /// </summary>
        private bool TryApplyEffectGroup(string effectGroupId)
        {
            if (database == null || string.IsNullOrWhiteSpace(effectGroupId))
            {
                return true;
            }

            List<DialogueEffectData> effects = database.GetEffects(effectGroupId);
            if (!CanApplyEffects(effects))
            {
                return false;
            }

            return ApplyEffectsByType(effects, "quest")
                && ApplyEffectsByType(effects, "item")
                && ApplyEffectsByType(effects, "gold")
                && ApplyEffectsByType(effects, "reputation")
                && ApplyEffectsByType(effects, "affection")
                && ApplyEffectsByType(effects, "flag");
        }

        /// <summary>
        /// 효과 적용에 필요한 서비스와 정수 값을 미리 검증합니다.
        /// </summary>
        private bool CanApplyEffects(IReadOnlyList<DialogueEffectData> effects)
        {
            Dictionary<string, int> requiredItems = new();

            foreach (DialogueEffectData effect in effects)
            {
                bool needsAmount = string.Equals(effect.EffectType, "item", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(effect.EffectType, "gold", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(effect.EffectType, "reputation", StringComparison.OrdinalIgnoreCase);
                int amount = 0;

                if (needsAmount && (!int.TryParse(effect.Value, out amount) || amount <= 0))
                {
                    return false;
                }

                if (string.Equals(effect.EffectType, "item", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(effect.Operation, "remove", StringComparison.OrdinalIgnoreCase))
                {
                    requiredItems.TryGetValue(effect.TargetId, out int currentRequired);
                    requiredItems[effect.TargetId] = currentRequired + amount;
                }

                if (string.Equals(effect.EffectType, "quest", StringComparison.OrdinalIgnoreCase)
                    && ResolveQuestManager() == null)
                {
                    return false;
                }

                if (string.Equals(effect.EffectType, "item", StringComparison.OrdinalIgnoreCase)
                    && ResolveInventory() == null)
                {
                    return false;
                }

                if (string.Equals(effect.EffectType, "gold", StringComparison.OrdinalIgnoreCase)
                    && ResolveGold() == null)
                {
                    return false;
                }

                if (string.Equals(effect.EffectType, "reputation", StringComparison.OrdinalIgnoreCase)
                    && ResolveReputationManager() == null)
                {
                    return false;
                }
            }

            IInventory resolvedInventory = requiredItems.Count > 0 ? ResolveInventory() : null;
            foreach (KeyValuePair<string, int> requiredItem in requiredItems)
            {
                if (resolvedInventory == null || resolvedInventory.Count(requiredItem.Key) < requiredItem.Value)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 지정한 타입의 효과를 시트 행 순서대로 적용합니다.
        /// </summary>
        private bool ApplyEffectsByType(IEnumerable<DialogueEffectData> effects, string effectType)
        {
            foreach (DialogueEffectData effect in effects)
            {
                if (string.Equals(effect.EffectType, effectType, StringComparison.OrdinalIgnoreCase)
                    && !ApplyEffect(effect))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 효과 한 행을 대상 런타임 시스템에 적용합니다.
        /// </summary>
        private bool ApplyEffect(DialogueEffectData effect)
        {
            if (string.Equals(effect.EffectType, "quest", StringComparison.OrdinalIgnoreCase))
            {
                QuestManager manager = ResolveQuestManager();
                if (string.Equals(effect.Operation, "start", StringComparison.OrdinalIgnoreCase))
                {
                    return manager.StartQuest(effect.TargetId);
                }

                if (string.Equals(effect.Operation, "complete_step", StringComparison.OrdinalIgnoreCase))
                {
                    return manager.CompleteStep(effect.TargetId, effect.Value);
                }

                if (string.Equals(effect.Operation, "complete", StringComparison.OrdinalIgnoreCase))
                {
                    return manager.CompleteQuestReport(effect.TargetId);
                }
            }

            if (string.Equals(effect.EffectType, "item", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(effect.Value, out int itemCount))
            {
                IInventory resolvedInventory = ResolveInventory();
                if (string.Equals(effect.Operation, "remove", StringComparison.OrdinalIgnoreCase))
                {
                    return resolvedInventory.Remove(effect.TargetId, itemCount);
                }

                if (string.Equals(effect.Operation, "add", StringComparison.OrdinalIgnoreCase))
                {
                    return resolvedInventory.Add(effect.TargetId, itemCount);
                }
            }

            if (string.Equals(effect.EffectType, "gold", StringComparison.OrdinalIgnoreCase)
                && string.Equals(effect.Operation, "add", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(effect.Value, out int goldAmount))
            {
                ResolveGold().Add(goldAmount);
                return true;
            }

            if (string.Equals(effect.EffectType, "reputation", StringComparison.OrdinalIgnoreCase)
                && string.Equals(effect.Operation, "add", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(effect.Value, out int reputationAmount))
            {
                ResolveReputationManager().Add(reputationAmount);
                return true;
            }

            return string.Equals(effect.EffectType, "affection", StringComparison.OrdinalIgnoreCase)
                || string.Equals(effect.EffectType, "flag", StringComparison.OrdinalIgnoreCase);
        }

        private QuestManager ResolveQuestManager()
        {
            if (questManager == null)
            {
                questManager = FindFirstObjectByType<QuestManager>();
            }

            if (questManager == null)
            {
                questManager = gameObject.AddComponent<QuestManager>();
                questManager.Initialize(database);
            }
            else if (!questManager.HasDatabase)
            {
                questManager.Initialize(database);
            }

            return questManager;
        }

        /// <summary>
        /// 전역 컨텍스트 또는 씬에서 일반 인벤토리를 찾습니다.
        /// </summary>
        private IInventory ResolveInventory()
        {
            inventory ??= GameContext.Instance?.ItemInventory;
            inventory ??= FindFirstObjectByType<ItemInventoryManager>();
            return inventory;
        }

        /// <summary>
        /// 전역 컨텍스트 또는 씬에서 골드 저장소를 찾습니다.
        /// </summary>
        private IGold ResolveGold()
        {
            gold ??= GameContext.Instance?.Gold;
            gold ??= FindFirstObjectByType<GoldManager>();
            return gold;
        }

        /// <summary>
        /// 씬에서 상점 명성 매니저를 찾습니다.
        /// </summary>
        private ShopReputationManager ResolveReputationManager()
        {
            reputationManager ??= FindFirstObjectByType<ShopReputationManager>();
            return reputationManager;
        }

        /// <summary>
        /// 정수 조건 연산자를 비교합니다.
        /// </summary>
        private static bool Compare(int left, string operatorText, int right)
        {
            return operatorText switch
            {
                "==" => left == right,
                "!=" => left != right,
                ">" => left > right,
                ">=" => left >= right,
                "<" => left < right,
                "<=" => left <= right,
                _ => false
            };
        }

        /// <summary>
        /// 문자열 조건의 동등 또는 비동등 연산자를 비교합니다.
        /// </summary>
        private static bool Compare(string left, string operatorText, string right)
        {
            bool equals = string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
            return operatorText == "==" ? equals : operatorText == "!=" && !equals;
        }

        /// <summary>
        /// 퀘스트 상태를 Conditions 시트에서 사용할 문자열로 변환합니다.
        /// </summary>
        private static string FormatQuestState(QuestRuntimeState state)
        {
            return state switch
            {
                QuestRuntimeState.Active => "active",
                QuestRuntimeState.ReadyToReport => "ready_to_report",
                QuestRuntimeState.Completed => "completed",
                _ => "inactive"
            };
        }

        private void EndDialogue()
        {
            currentNode = null;
            currentChoices.Clear();
            DialogueEnded?.Invoke();
        }
    }
}
