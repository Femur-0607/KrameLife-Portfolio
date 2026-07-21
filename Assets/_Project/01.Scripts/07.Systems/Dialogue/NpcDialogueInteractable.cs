using TownLife.Core;
using TownLife.Quests;
using UnityEngine;

namespace TownLife.Dialogue
{
    /// <summary>
    /// NPC 오브젝트에서 공통 월드 상호작용을 받아 DialogueRunner로 대화를 시작합니다.
    /// </summary>
    public sealed class NpcDialogueInteractable : MonoBehaviour, IWorldInteractable
    {
        [SerializeField, Tooltip("상호작용 시 대화를 실행할 DialogueRunner입니다. 비워두면 씬에서 자동으로 찾습니다.")]
        private DialogueRunner dialogueRunner = null;

        [SerializeField, Tooltip("상호작용 시 시작할 대화 노드 ID입니다.")]
        private string startNodeId = string.Empty;

        [Header("Quest Branch (Optional)")]
        [SerializeField, Tooltip("대화 시작점을 진행 상태에 따라 바꿀 퀘스트 ID입니다. 비워두면 기본 대화만 사용합니다.")]
        private string branchQuestId = string.Empty;

        [SerializeField, Tooltip("퀘스트 진행 중에 시작할 대화 노드 ID입니다.")]
        private string activeNodeId = string.Empty;

        [SerializeField, Tooltip("퀘스트 보고 가능 상태에 시작할 대화 노드 ID입니다.")]
        private string readyToReportNodeId = string.Empty;

        [SerializeField, Tooltip("퀘스트 완료 후 시작할 대화 노드 ID입니다.")]
        private string completedNodeId = string.Empty;

        private QuestManager questManager;

        /// <summary>
        /// 테스트나 코드에서 대화 실행에 필요한 참조와 시작 노드 ID를 주입합니다.
        /// </summary>
        public void Initialize(DialogueRunner runner, string nodeId)
        {
            dialogueRunner = runner;
            startNodeId = nodeId;
        }

        /// <summary>
        /// 현재 설정으로 NPC 대화를 시작할 수 있는지 확인합니다.
        /// </summary>
        public bool CanInteract()
        {
            CacheReferences();
            return dialogueRunner != null && !string.IsNullOrWhiteSpace(startNodeId);
        }

        /// <summary>
        /// 공통 상호작용 입력에서 호출되어 설정된 시작 노드로 대화를 시작합니다.
        /// </summary>
        public void Interact()
        {
#if UNITY_EDITOR
            Debug.Log(
                $"NPC 대화 상호작용 호출: npc={name}, startNodeId={startNodeId}, hasRunner={dialogueRunner != null}",
                this);
#endif

            if (!CanInteract())
            {
                Debug.LogWarning("NPC 대화 상호작용에 필요한 설정이 없습니다.", this);
                return;
            }

            if (dialogueRunner.IsDialogueActive)
            {
                dialogueRunner.Advance();
                return;
            }

            dialogueRunner.StartDialogue(ResolveStartNodeId());
        }

        /// <summary>
        /// 선택적 퀘스트 분기 설정과 현재 진행 상태에 맞는 시작 노드를 반환합니다.
        /// </summary>
        private string ResolveStartNodeId()
        {
            if (string.IsNullOrWhiteSpace(branchQuestId))
            {
                return startNodeId;
            }

            if (questManager == null)
            {
                questManager = FindFirstObjectByType<QuestManager>();
            }

            if (questManager == null)
            {
                return startNodeId;
            }

            return questManager.GetQuestState(branchQuestId) switch
            {
                QuestRuntimeState.Active => GetConfiguredNodeOrDefault(activeNodeId),
                QuestRuntimeState.ReadyToReport => GetConfiguredNodeOrDefault(readyToReportNodeId),
                QuestRuntimeState.Completed => GetConfiguredNodeOrDefault(completedNodeId),
                _ => startNodeId
            };
        }

        /// <summary>
        /// 상태별 노드가 비어 있으면 기본 시작 노드를 사용합니다.
        /// </summary>
        private string GetConfiguredNodeOrDefault(string nodeId)
        {
            return string.IsNullOrWhiteSpace(nodeId) ? startNodeId : nodeId;
        }

        private void CacheReferences()
        {
            if (dialogueRunner == null)
            {
                dialogueRunner = FindFirstObjectByType<DialogueRunner>();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            CacheReferences();
        }
#endif
    }
}
