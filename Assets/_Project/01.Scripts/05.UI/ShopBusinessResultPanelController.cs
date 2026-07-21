using DG.Tweening;
using TMPro;
using TownLife.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace TownLife.UI
{
    /// <summary>
    /// 영업 시작 문구와 영업 마감 리포트를 상점 UI에 표시합니다.
    /// </summary>
    public sealed class ShopBusinessResultPanelController : MonoBehaviour
    {
        [SerializeField, Tooltip("상점 UI 초안 전체 루트입니다. 결과창을 열 때 함께 켭니다.")]
        private GameObject shopUiRoot = null;

        [SerializeField, Tooltip("영업 결과 패널 루트입니다.")]
        private GameObject resultPanelRoot = null;

        [SerializeField, Tooltip("영업 결과 패널 크기를 변경할 RectTransform입니다.")]
        private RectTransform resultPanelRect = null;

        [SerializeField, Tooltip("영업 결과 패널이 완전히 펼쳐졌을 때의 크기입니다.")]
        private Vector2 resultPanelSize = new(560f, 420f);

        [SerializeField, Min(0.01f), Tooltip("영업 결과 패널이 펼쳐지는 시간입니다.")]
        private float resultPanelOpenDuration = 0.35f;

        [SerializeField, Tooltip("영업 시작 직후 잠시 표시할 시작 문구 패널입니다.")]
        private GameObject businessStartToastRoot = null;

        [SerializeField, Tooltip("영업 시작 문구 패널의 크기를 변경할 RectTransform입니다.")]
        private RectTransform businessStartToastRect = null;

        [SerializeField, Tooltip("패널 펼침이 끝난 뒤 표시할 영업 시작 텍스트입니다.")]
        private TMP_Text businessStartText = null;

        [SerializeField, Tooltip("영업 시작 문구 패널이 완전히 펼쳐졌을 때의 크기입니다.")]
        private Vector2 businessStartToastSize = new(520f, 100f);

        [SerializeField, Min(0.01f), Tooltip("영업 시작 문구 패널이 펼쳐지는 시간입니다.")]
        private float businessStartToastOpenDuration = 0.35f;

        [SerializeField, Min(0.1f), Tooltip("영업 시작 문구를 표시할 시간입니다.")]
        private float businessStartToastDuration = 2f;

        [SerializeField, Min(0.01f), Tooltip("영업 시작 문구 패널이 접히는 시간입니다.")]
        private float businessStartToastCloseDuration = 0.25f;

        [SerializeField, Tooltip("영업 결과 제목 텍스트입니다.")]
        private TMP_Text titleText = null;

        [SerializeField, Tooltip("영업 결과 요약 텍스트입니다. 비어 있으면 ResultSummaryRows에 자동 생성합니다.")]
        private TMP_Text summaryText = null;

        [SerializeField, Tooltip("결과창 닫기 버튼입니다. 비어 있으면 ResultCloseButton에서 자동 연결합니다.")]
        private Button closeButton = null;

        [SerializeField, Min(0.05f), Tooltip("씬에 늦게 생기는 영업 세션 매니저를 다시 찾는 간격입니다.")]
        private float sessionSearchInterval = 0.5f;

        private ShopBusinessSessionManager subscribedSessionManager = null;
        private float nextSessionSearchTime;
        private bool wasBusinessOpen;
        private Tween resultPanelTween;
        private Tween businessStartToastTween;

        private void Awake()
        {
            CachePanelReferences();
            BindCloseButton();
            HideBusinessStartToast();
            Close();
        }

        private void OnEnable()
        {
            TrySubscribeSessionManager();
        }

        private void OnDisable()
        {
            UnsubscribeSessionManager();
            resultPanelTween?.Kill();
            resultPanelTween = null;
            HideBusinessStartToast();
        }

        private void Update()
        {
            if (subscribedSessionManager == null && Time.unscaledTime >= nextSessionSearchTime)
            {
                nextSessionSearchTime = Time.unscaledTime + sessionSearchInterval;
                TrySubscribeSessionManager();
            }

            DetectBusinessStarted();
        }

        /// <summary>
        /// 결과 패널을 닫습니다.
        /// </summary>
        public void Close()
        {
            resultPanelTween?.Kill();
            resultPanelTween = null;
            SetResultPanelChildrenActive(false);

            if (resultPanelRect != null)
            {
                resultPanelRect.sizeDelta = Vector2.zero;
            }

            SetActive(resultPanelRoot, false);
        }

        private void TrySubscribeSessionManager()
        {
            ShopBusinessSessionManager sessionManager = FindAnyObjectByType<ShopBusinessSessionManager>();
            if (sessionManager == null || sessionManager == subscribedSessionManager)
            {
                return;
            }

            UnsubscribeSessionManager();
            subscribedSessionManager = sessionManager;
            subscribedSessionManager.BusinessClosed += Open;
            wasBusinessOpen = false;
        }

        private void UnsubscribeSessionManager()
        {
            if (subscribedSessionManager == null)
            {
                return;
            }

            subscribedSessionManager.BusinessClosed -= Open;
            subscribedSessionManager = null;
            wasBusinessOpen = false;
        }

        /// <summary>
        /// 영업 상태가 닫힘에서 열림으로 바뀌면 시작 문구를 표시합니다.
        /// </summary>
        private void DetectBusinessStarted()
        {
            if (subscribedSessionManager == null)
            {
                return;
            }

            bool isBusinessOpen = subscribedSessionManager.IsOpen;
            if (isBusinessOpen && !wasBusinessOpen)
            {
                ShowBusinessStartToast();
            }

            wasBusinessOpen = isBusinessOpen;
        }

        /// <summary>
        /// 영업 시작 패널을 펼치고 텍스트를 표시한 뒤 다시 접습니다.
        /// </summary>
        private void ShowBusinessStartToast()
        {
            CachePanelReferences();
            businessStartToastTween?.Kill();

            SetActive(shopUiRoot, true);
            SetActive(businessStartToastRoot, true);

            if (businessStartToastRect == null)
            {
                SetActive(businessStartText?.gameObject, true);
                businessStartToastTween = DOVirtual
                    .DelayedCall(businessStartToastDuration, HideBusinessStartToast)
                    .SetUpdate(true);
                return;
            }

            businessStartToastRect.sizeDelta = Vector2.zero;
            SetActive(businessStartText?.gameObject, false);

            Sequence sequence = DOTween.Sequence();
            sequence.Append(businessStartToastRect
                .DOSizeDelta(businessStartToastSize, businessStartToastOpenDuration)
                .SetEase(Ease.OutCubic));
            sequence.AppendCallback(() => SetActive(businessStartText?.gameObject, true));
            sequence.AppendInterval(businessStartToastDuration);
            sequence.AppendCallback(() => SetActive(businessStartText?.gameObject, false));
            sequence.Append(businessStartToastRect
                .DOSizeDelta(Vector2.zero, businessStartToastCloseDuration)
                .SetEase(Ease.InCubic));
            sequence.OnComplete(() =>
            {
                businessStartToastTween = null;
                SetActive(businessStartToastRoot, false);
            });
            sequence.SetUpdate(true);
            businessStartToastTween = sequence;
        }

        /// <summary>
        /// 진행 중인 시작 문구 타이머를 정리하고 패널을 끕니다.
        /// </summary>
        private void HideBusinessStartToast()
        {
            businessStartToastTween?.Kill();
            businessStartToastTween = null;
            SetActive(businessStartText?.gameObject, false);

            if (businessStartToastRect != null)
            {
                businessStartToastRect.sizeDelta = Vector2.zero;
            }

            SetActive(businessStartToastRoot, false);
        }

        private void Open(ShopBusinessReport report)
        {
            CachePanelReferences();

            SetActive(shopUiRoot, true);
            SetActive(resultPanelRoot, true);
            SetResultPanelChildrenActive(false);

            SetText(titleText, "영업 결과");
            SetText(summaryText, BuildSummary(report));

            resultPanelTween?.Kill();

            if (resultPanelRect == null)
            {
                SetResultPanelChildrenActive(true);
                return;
            }

            resultPanelRect.sizeDelta = Vector2.zero;
            resultPanelTween = resultPanelRect
                .DOSizeDelta(resultPanelSize, resultPanelOpenDuration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    resultPanelTween = null;
                    SetResultPanelChildrenActive(true);
                });
        }

        private static string BuildSummary(ShopBusinessReport report)
        {
            if (!report.IsValid)
            {
                return "생성된 영업 결과가 없습니다.";
            }

            return $"날짜: {report.Day}일\n오픈: {report.OpenedTimeText}\n마감: {report.ClosedTimeText}\n방문객: {report.VisitorCount}\n판매량: {report.SalesCount}\n총매출: {report.TotalSalesGold}G";
        }

        private void CachePanelReferences()
        {
            shopUiRoot ??= FindChildGameObject("ShopUiDraftRoot");
            resultPanelRoot ??= FindChildGameObject("BusinessResultDraftPanel");
            resultPanelRect ??= resultPanelRoot != null
                ? resultPanelRoot.GetComponent<RectTransform>()
                : null;
            businessStartToastRoot ??= FindChildGameObject("BusinessStartToast");
            businessStartToastRect ??= businessStartToastRoot != null
                ? businessStartToastRoot.GetComponent<RectTransform>()
                : null;
            businessStartText ??= FindText("BusinessStartText (TMP)");
            titleText ??= FindText("ResultTitleText (TMP)");
            summaryText ??= FindText("ResultSummaryText");
            summaryText ??= EnsureSummaryText();
            closeButton ??= FindButton("ResultCloseButton");
        }

        private void SetResultPanelChildrenActive(bool isActive)
        {
            if (resultPanelRoot == null)
            {
                return;
            }

            Transform resultPanelTransform = resultPanelRoot.transform;
            for (int index = 0; index < resultPanelTransform.childCount; index++)
            {
                Transform child = resultPanelTransform.GetChild(index);
                if (child != null)
                {
                    child.gameObject.SetActive(isActive);
                }
            }
        }

        private void BindCloseButton()
        {
            if (closeButton == null)
            {
                return;
            }

            closeButton.onClick.RemoveListener(Close);
            closeButton.onClick.AddListener(Close);
        }

        private TMP_Text EnsureSummaryText()
        {
            GameObject rows = FindChildGameObject("ResultSummaryRows");
            if (rows == null)
            {
                return null;
            }

            TextMeshProUGUI text = rows.GetComponent<TextMeshProUGUI>();
            if (text == null)
            {
                text = rows.AddComponent<TextMeshProUGUI>();
            }

            text.name = "ResultSummaryText";
            text.fontSize = 24f;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.raycastTarget = false;
            return text;
        }

        private GameObject FindChildGameObject(string childName)
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < children.Length; index++)
            {
                if (children[index] != null && children[index].name == childName)
                {
                    return children[index].gameObject;
                }
            }

            return null;
        }

        private TMP_Text FindText(string childName)
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < children.Length; index++)
            {
                if (children[index] != null && children[index].name == childName)
                {
                    return children[index].GetComponent<TMP_Text>();
                }
            }

            return null;
        }

        private Button FindButton(string childName)
        {
            GameObject child = FindChildGameObject(childName);
            if (child == null)
            {
                return null;
            }

            if (!child.TryGetComponent(out Button button))
            {
                button = child.AddComponent<Button>();
            }

            if (button.targetGraphic == null)
            {
                button.targetGraphic = child.GetComponent<Graphic>();
            }

            return button;
        }

        private static void SetActive(GameObject target, bool isActive)
        {
            if (target != null)
            {
                target.SetActive(isActive);
            }
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }
    }
}
