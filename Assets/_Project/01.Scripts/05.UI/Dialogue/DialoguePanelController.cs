using System.Collections.Generic;
using TMPro;
using TownLife.Dialogue;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TownLife.UI.Dialogue
{
    /// <summary>
    /// DialogueRunner의 현재 대사를 간단한 TMP UI로 표시합니다.
    /// </summary>
    public sealed class DialoguePanelController : MonoBehaviour
    {
        private const float ChoiceButtonHeight = 44f;
        private const float ChoiceButtonSpacing = 10f;

        [Header("References")]
        [SerializeField, Tooltip("대화 진행 상태를 제공하는 DialogueRunner입니다.")]
        private DialogueRunner dialogueRunner = null;

        [SerializeField, Tooltip("대화창 전체 루트입니다.")]
        private GameObject panelRoot = null;

        [SerializeField, Tooltip("말하는 캐릭터 ID나 이름을 표시할 텍스트입니다.")]
        private TMP_Text speakerText = null;

        [SerializeField, Tooltip("현재 대사 본문을 표시할 텍스트입니다.")]
        private TMP_Text bodyText = null;

        [SerializeField, Tooltip("선택지 버튼들이 생성될 부모 Transform입니다.")]
        private Transform choiceRoot = null;

        [SerializeField, Tooltip("선택지 버튼으로 복제할 프리팹입니다.")]
        private DialogueChoiceButtonView choiceButtonPrefab = null;

        [SerializeField, Tooltip("선택지가 없을 때 다음 대사로 넘기는 버튼입니다.")]
        private Button continueButton = null;

        [Header("Options")]
        [SerializeField, Tooltip("Play 시작 시 대화창을 닫습니다.")]
        private bool closeOnAwake = true;

        [SerializeField, Tooltip("Space 또는 Enter 키로 선택지가 없는 대화를 진행합니다.")]
        private bool advanceWithKeyboard = true;

        private readonly List<DialogueChoiceButtonView> choiceButtons = new();
        private bool isSubscribed;

        private void Awake()
        {
            ConfigureChoiceRootLayout();

            if (continueButton != null)
            {
                continueButton.onClick.AddListener(Advance);
            }

            if (closeOnAwake)
            {
                SetPanelActive(false);
            }
        }

        private void OnEnable()
        {
            TrySubscribeRunner();
        }

        private void OnDisable()
        {
            UnsubscribeRunner();
        }

        private void Update()
        {
            if (isSubscribed && dialogueRunner == null)
            {
                isSubscribed = false;
            }

            if (!isSubscribed)
            {
                TrySubscribeRunner();
            }

            if (!advanceWithKeyboard || dialogueRunner == null || !dialogueRunner.IsDialogueActive)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            bool pressed = keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame;
            if (pressed && dialogueRunner.CurrentChoices.Count == 0)
            {
                Advance();
            }
        }

        /// <summary>
        /// 현재 대화가 선택지 없이 이어질 수 있으면 다음 노드로 진행합니다.
        /// </summary>
        public void Advance()
        {
            if (dialogueRunner != null)
            {
                dialogueRunner.Advance();
            }
        }

        /// <summary>
        /// DialogueRunner가 전달한 현재 노드와 선택지로 대화창을 갱신합니다.
        /// </summary>
        private void Show(DialogueNodeData node, IReadOnlyList<DialogueChoiceData> choices)
        {
            if (node == null)
            {
                Hide();
                return;
            }

            SetPanelActive(true);
            SetText(speakerText, node.CharacterId);
            SetText(bodyText, node.Text);
            RebuildChoices(choices);

            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(choices == null || choices.Count == 0);
            }
        }

        /// <summary>
        /// 런타임에 DialogueRunner를 찾고 대화 변경 이벤트를 구독합니다.
        /// </summary>
        private void TrySubscribeRunner()
        {
            if (isSubscribed)
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

            dialogueRunner.DialogueChanged += Show;
            dialogueRunner.DialogueEnded += Hide;
            isSubscribed = true;

            if (dialogueRunner.IsDialogueActive)
            {
                Show(dialogueRunner.CurrentNode, dialogueRunner.CurrentChoices);
            }
        }

        /// <summary>
        /// 비활성화 시 중복 호출을 막기 위해 DialogueRunner 이벤트 구독을 해제합니다.
        /// </summary>
        private void UnsubscribeRunner()
        {
            if (!isSubscribed || dialogueRunner == null)
            {
                return;
            }

            dialogueRunner.DialogueChanged -= Show;
            dialogueRunner.DialogueEnded -= Hide;
            isSubscribed = false;
        }

        /// <summary>
        /// 대화창과 선택지를 닫고 이전 대사 텍스트를 비웁니다.
        /// </summary>
        private void Hide()
        {
            SetPanelActive(false);
            SetText(speakerText, string.Empty);
            SetText(bodyText, string.Empty);
            HideChoices();
        }

        /// <summary>
        /// 현재 선택지 수에 맞춰 버튼을 준비하고 각 선택 콜백을 다시 연결합니다.
        /// </summary>
        private void RebuildChoices(IReadOnlyList<DialogueChoiceData> choices)
        {
            int count = choices != null ? choices.Count : 0;
            EnsureChoiceButtonCount(count);

            for (int index = 0; index < choiceButtons.Count; index++)
            {
                DialogueChoiceButtonView buttonView = choiceButtons[index];
                if (index >= count)
                {
                    buttonView.gameObject.SetActive(false);
                    continue;
                }

                int choiceIndex = index;
                buttonView.Bind(choices[index].Text);

                Button button = buttonView.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => dialogueRunner.SelectChoice(choiceIndex));
                }
            }
        }

        /// <summary>
        /// 필요한 선택지 개수만큼 버튼 풀을 늘립니다.
        /// </summary>
        private void EnsureChoiceButtonCount(int count)
        {
            if (choiceButtonPrefab == null || choiceRoot == null)
            {
                return;
            }

            while (choiceButtons.Count < count)
            {
                DialogueChoiceButtonView button = Instantiate(choiceButtonPrefab, choiceRoot);
                ConfigureChoiceButtonLayout(button);
                choiceButtons.Add(button);
            }
        }

        /// <summary>
        /// 선택지 부모에 세로 레이아웃을 보장해 동적 버튼 배치를 안정화합니다.
        /// </summary>
        private void ConfigureChoiceRootLayout()
        {
            if (choiceRoot == null)
            {
                return;
            }

            HorizontalOrVerticalLayoutGroup layoutGroup = choiceRoot.GetComponent<HorizontalOrVerticalLayoutGroup>();

            if (layoutGroup == null)
            {
                layoutGroup = choiceRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            }

            layoutGroup.spacing = ChoiceButtonSpacing;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;
        }

        /// <summary>
        /// 선택지 버튼이 레이아웃 그룹 안에서 일정한 높이를 갖도록 설정합니다.
        /// </summary>
        private static void ConfigureChoiceButtonLayout(DialogueChoiceButtonView button)
        {
            if (button == null)
            {
                return;
            }

            LayoutElement layoutElement = button.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = button.gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.preferredHeight = ChoiceButtonHeight;
        }

        /// <summary>
        /// 생성된 선택지 버튼을 재사용할 수 있도록 비활성화합니다.
        /// </summary>
        private void HideChoices()
        {
            for (int index = 0; index < choiceButtons.Count; index++)
            {
                choiceButtons[index].gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 대화창 루트가 연결된 경우에만 안전하게 표시 상태를 바꿉니다.
        /// </summary>
        private void SetPanelActive(bool isActive)
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(isActive);
            }
        }

        /// <summary>
        /// 텍스트 참조가 없을 때도 대화 갱신 흐름이 끊기지 않도록 안전하게 값을 대입합니다.
        /// </summary>
        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }
    }
}
