using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using TownLife.Dialogue;
using TownLife.Quests;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TownLife.UI.Quests
{
    public sealed class QuestObjectiveListController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("퀘스트 목표 목록 전체 루트입니다.")]
        private GameObject panelRoot = null;

        [SerializeField, Tooltip("퀘스트 이름을 표시할 텍스트입니다.")]
        private TMP_Text titleText = null;

        [SerializeField, Tooltip("퀘스트 상태 안내를 표시할 텍스트입니다.")]
        private TMP_Text statusText = null;

        [SerializeField, Tooltip("목표 줄들이 생성될 부모 Transform입니다.")]
        private Transform objectiveRoot = null;

        [SerializeField, Tooltip("목표 한 줄을 표시할 TMP 텍스트 템플릿입니다.")]
        private TMP_Text objectiveTextTemplate = null;

        [SerializeField, Tooltip("진행 정보를 제공하는 QuestManager입니다. 비워두면 씬에서 찾습니다.")]
        private QuestManager questManager = null;

        [SerializeField, Tooltip("대화 종료 후 퀘스트 수락 표시를 띄우기 위한 DialogueRunner입니다. 비워두면 씬에서 찾습니다.")]
        private DialogueRunner dialogueRunner = null;

        [Header("Text")]
        [SerializeField, Tooltip("모든 목표 완료 후 표시할 안내 문구입니다.")]
        private string reportReadyText = "퀘스트 보고하러가기";

        [SerializeField, Tooltip("진행 중 목표 목록 위에 표시할 안내 문구입니다.")]
        private string activeText = "해야 할 일";

        [Header("Motion")]
        [SerializeField, Tooltip("Q 키로 퀘스트 트래커 표시 상태를 전환합니다.")]
        private bool toggleWithQKey = true;

        [SerializeField, Tooltip("트래커가 보일 때 anchoredPosition.x 값입니다.")]
        private float shownAnchoredX = -185f;

        [SerializeField, Tooltip("트래커가 숨겨질 때 anchoredPosition.x 값입니다.")]
        private float hiddenAnchoredX = 200f;

        [SerializeField, Tooltip("트래커가 나타나는 시간입니다.")]
        private float showDuration = 0.35f;

        [SerializeField, Tooltip("트래커가 사라지는 시간입니다.")]
        private float hideDuration = 0.25f;

        [SerializeField, Tooltip("퀘스트 수락 후 자동으로 보여줄 시간입니다.")]
        private float autoHideDelay = 2f;

        private readonly List<TMP_Text> objectiveTexts = new();
        private RectTransform panelRect;
        private Tween panelTween;
        private Coroutine autoHideCoroutine;
        private QuestProgressSnapshot currentVisibleQuest;
        private string lastVisibleQuestId = string.Empty;
        private bool isQuestSubscribed;
        private bool isDialogueSubscribed;
        private bool isOpen;
        private bool pendingAutoReveal;

        private void Awake()
        {
            CachePanelRect();

            if (objectiveTextTemplate != null)
            {
                objectiveTextTemplate.gameObject.SetActive(false);
            }

            SnapHidden();
        }

        private void OnEnable()
        {
            TrySubscribe();
            TrySubscribeDialogue();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
            UnsubscribeDialogue();
            StopAutoHide();
            panelTween?.Kill();
        }

        private void Update()
        {
            if (!isQuestSubscribed)
            {
                TrySubscribe();
            }

            if (!isDialogueSubscribed)
            {
                TrySubscribeDialogue();
            }

            Keyboard keyboard = Keyboard.current;
            if (toggleWithQKey
                && keyboard != null
                && keyboard.qKey.wasPressedThisFrame
                && currentVisibleQuest != null)
            {
                Toggle();
            }
        }

        private void TrySubscribe()
        {
            if (isQuestSubscribed)
            {
                return;
            }

            if (questManager == null)
            {
                questManager = FindFirstObjectByType<QuestManager>();
            }

            if (questManager == null)
            {
                return;
            }

            questManager.QuestProgressChanged += Refresh;
            isQuestSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!isQuestSubscribed || questManager == null)
            {
                return;
            }

            questManager.QuestProgressChanged -= Refresh;
            isQuestSubscribed = false;
        }

        private void TrySubscribeDialogue()
        {
            if (isDialogueSubscribed)
            {
                return;
            }

            if (dialogueRunner == null)
            {
                dialogueRunner = FindFirstObjectByType<DialogueRunner>();
            }

            if (dialogueRunner == null)
            {
                return;
            }

            dialogueRunner.DialogueEnded += HandleDialogueEnded;
            isDialogueSubscribed = true;
        }

        private void UnsubscribeDialogue()
        {
            if (!isDialogueSubscribed || dialogueRunner == null)
            {
                return;
            }

            dialogueRunner.DialogueEnded -= HandleDialogueEnded;
            isDialogueSubscribed = false;
        }

        private void Refresh()
        {
            QuestProgressSnapshot visibleQuest = FindVisibleQuest();
            bool wasVisibleQuestEmpty = currentVisibleQuest == null;
            string previousQuestId = lastVisibleQuestId;
            currentVisibleQuest = visibleQuest;

            if (visibleQuest == null)
            {
                Close();
                ClearObjectiveTexts();
                lastVisibleQuestId = string.Empty;
                return;
            }

            lastVisibleQuestId = visibleQuest.QuestId;
            SetText(titleText, visibleQuest.DisplayName);
            SetText(statusText, visibleQuest.IsReadyToReport ? reportReadyText : activeText);
            RebuildObjectives(visibleQuest);

            if (wasVisibleQuestEmpty || previousQuestId != visibleQuest.QuestId)
            {
                RequestAutoReveal();
            }
        }

        private QuestProgressSnapshot FindVisibleQuest()
        {
            if (questManager == null)
            {
                return null;
            }

            foreach (QuestProgressSnapshot snapshot in questManager.ActiveQuestSnapshots)
            {
                return snapshot;
            }

            return null;
        }

        private void RebuildObjectives(QuestProgressSnapshot quest)
        {
            ClearObjectiveTexts();

            foreach (QuestStepProgressSnapshot step in quest.Steps)
            {
                TMP_Text objectiveText = CreateObjectiveText();
                if (objectiveText == null)
                {
                    continue;
                }

                string checkText = step.IsCompleted ? "[x]" : "[ ]";
                objectiveText.text = $"{checkText} {step.Description}";
                objectiveText.gameObject.SetActive(true);
                objectiveTexts.Add(objectiveText);
            }
        }

        private TMP_Text CreateObjectiveText()
        {
            if (objectiveTextTemplate == null || objectiveRoot == null)
            {
                return null;
            }

            return Instantiate(objectiveTextTemplate, objectiveRoot);
        }

        private void ClearObjectiveTexts()
        {
            foreach (TMP_Text objectiveText in objectiveTexts)
            {
                if (objectiveText != null)
                {
                    Destroy(objectiveText.gameObject);
                }
            }

            objectiveTexts.Clear();
        }

        public void Toggle()
        {
            if (isOpen)
            {
                Close();
                return;
            }

            Open();
        }

        public void Open()
        {
            StopAutoHide();
            MovePanel(shownAnchoredX, showDuration, Ease.OutCubic, true);
        }

        public void Close()
        {
            if (!isOpen && (panelRoot == null || !panelRoot.activeSelf))
            {
                SnapHidden();
                return;
            }

            StopAutoHide();
            MovePanel(hiddenAnchoredX, hideDuration, Ease.InCubic, false);
        }

        private void RequestAutoReveal()
        {
            if (currentVisibleQuest == null)
            {
                return;
            }

            TrySubscribeDialogue();

            if (dialogueRunner != null && dialogueRunner.IsDialogueActive)
            {
                pendingAutoReveal = true;
                return;
            }

            ShowTemporarily();
        }

        private void HandleDialogueEnded()
        {
            if (!pendingAutoReveal)
            {
                return;
            }

            pendingAutoReveal = false;
            ShowTemporarily();
        }

        private void ShowTemporarily()
        {
            if (currentVisibleQuest == null)
            {
                return;
            }

            StopAutoHide();
            MovePanel(shownAnchoredX, showDuration, Ease.OutCubic, true);
            autoHideCoroutine = StartCoroutine(HideAfterDelay());
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(autoHideDelay);
            autoHideCoroutine = null;
            MovePanel(hiddenAnchoredX, hideDuration, Ease.InCubic, false);
        }

        private void StopAutoHide()
        {
            if (autoHideCoroutine == null)
            {
                return;
            }

            StopCoroutine(autoHideCoroutine);
            autoHideCoroutine = null;
        }

        private void MovePanel(float targetX, float duration, Ease ease, bool openAfterMove)
        {
            CachePanelRect();
            if (panelRect == null)
            {
                SetPanelActive(openAfterMove);
                isOpen = openAfterMove;
                return;
            }

            SetPanelActive(true);
            isOpen = openAfterMove;

            Vector2 targetPosition = panelRect.anchoredPosition;
            targetPosition.x = targetX;

            panelTween?.Kill();
            panelTween = panelRect
                .DOAnchorPos(targetPosition, duration)
                .SetEase(ease)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    panelTween = null;
                    SetPanelActive(openAfterMove);
                });
        }

        private void SnapHidden()
        {
            CachePanelRect();
            if (panelRect != null)
            {
                Vector2 position = panelRect.anchoredPosition;
                position.x = hiddenAnchoredX;
                panelRect.anchoredPosition = position;
            }

            isOpen = false;
            SetPanelActive(false);
        }

        private void CachePanelRect()
        {
            if (panelRect == null && panelRoot != null)
            {
                panelRect = panelRoot.GetComponent<RectTransform>();
            }
        }

        private void SetPanelActive(bool isActive)
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(isActive);
            }
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }
    }
}
