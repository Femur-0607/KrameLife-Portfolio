using System.Collections;
using TMPro;
using TownLife.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TownLife.UI
{
    /// <summary>
    /// 아이템과 골드 획득 내역을 짧게 표시하는 HUD 로그입니다.
    /// </summary>
    public sealed class AcquisitionLogController : MonoBehaviour
    {
        [SerializeField, Tooltip("획득 로그 TMP 항목을 배치할 부모 Transform입니다.")]
        private Transform entryRoot = null;

        [SerializeField, Tooltip("획득 로그 한 줄에 사용할 Image 프리팹입니다. 자식에 TMP_Text가 있어야 합니다.")]
        private Image entryPrefab = null;

        [SerializeField, Tooltip("아이템 획득 성공 시 itemId와 count를 발행하는 채널입니다.")]
        private ItemStackEventChannelSO itemAcquiredChannel = null;

        [SerializeField, Tooltip("골드 획득 성공 시 획득량을 발행하는 채널입니다.")]
        private IntEventChannelSO goldAcquiredChannel = null;

        [SerializeField, Tooltip("아이템 ID를 표시 이름으로 바꿀 때 사용할 데이터베이스입니다. 비워두면 itemId를 그대로 표시합니다.")]
        private GameDatabase gameDatabase = null;

        [SerializeField, Min(0.1f), Tooltip("획득 로그가 화면에 남아 있는 시간입니다.")]
        private float visibleSeconds = 1.5f;

        [SerializeField, Min(0.05f), Tooltip("획득 로그가 사라질 때 페이드되는 시간입니다.")]
        private float fadeSeconds = 0.35f;

        [SerializeField, Min(1), Tooltip("동시에 화면에 유지할 최대 로그 수입니다.")]
        private int maxEntries = 5;

        [SerializeField, Tooltip("획득 로그 한 줄의 고정 크기입니다.")]
        private Vector2 entrySize = new(260f, 44f);

        [SerializeField, Min(0f), Tooltip("획득 로그 줄 사이 간격입니다.")]
        private float entrySpacing = 8f;

        [SerializeField, Tooltip("프리팹 미리보기용으로 남아 있는 기존 로그 자식을 시작할 때 비웁니다.")]
        private bool clearExistingEntriesOnAwake = true;

        private void Awake()
        {
            DisableLayoutGroups();
            ClearExistingEntries();
        }

        private void OnEnable()
        {
            if (itemAcquiredChannel != null)
            {
                itemAcquiredChannel.OnRaised += HandleItemAcquired;
            }

            if (goldAcquiredChannel != null)
            {
                goldAcquiredChannel.OnRaised += HandleGoldAcquired;
            }
        }

        private void OnDisable()
        {
            if (itemAcquiredChannel != null)
            {
                itemAcquiredChannel.OnRaised -= HandleItemAcquired;
            }

            if (goldAcquiredChannel != null)
            {
                goldAcquiredChannel.OnRaised -= HandleGoldAcquired;
            }
        }

        private void HandleItemAcquired(string itemId, int count)
        {
            if (string.IsNullOrWhiteSpace(itemId) || count <= 0)
            {
                return;
            }

            string displayName = ResolveItemDisplayName(itemId);
            ShowEntry($"{displayName} x{count}");
        }

        private void HandleGoldAcquired(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            ShowEntry($"+{amount} 골드");
        }

        /// <summary>
        /// 획득 로그 항목을 만들고 자동 삭제 코루틴을 시작합니다.
        /// </summary>
        private void ShowEntry(string message)
        {
            if (entryRoot == null || entryPrefab == null)
            {
                return;
            }

            Image entry = Instantiate(entryPrefab, entryRoot);
            TMP_Text entryText = entry.GetComponentInChildren<TMP_Text>(true);
            if (entryText != null)
            {
                entryText.text = message;
            }

            SetAlpha(entry.gameObject, 1f);
            entry.transform.SetAsFirstSibling();

            TrimEntries();
            LayoutEntries();
            StartCoroutine(FadeAndDestroy(entry.gameObject));
        }

        /// <summary>
        /// 데이터베이스에 표시 이름이 있으면 사용하고, 없으면 itemId를 반환합니다.
        /// </summary>
        private string ResolveItemDisplayName(string itemId)
        {
            GameItemSO item = gameDatabase != null ? gameDatabase.Get<GameItemSO>(itemId) : null;
            if (item == null || string.IsNullOrWhiteSpace(item.DisplayName))
            {
                return itemId;
            }

            return item.DisplayName;
        }

        /// <summary>
        /// 최대 표시 개수를 넘은 오래된 로그 항목을 정리합니다.
        /// </summary>
        private void TrimEntries()
        {
            while (entryRoot.childCount > maxEntries)
            {
                Transform oldestEntry = entryRoot.GetChild(entryRoot.childCount - 1);
                Destroy(oldestEntry.gameObject);
            }
        }

        /// <summary>
        /// 레이아웃 그룹 대신 고정 크기 로그를 아래에서 위로 직접 정렬합니다.
        /// </summary>
        private void LayoutEntries()
        {
            if (entryRoot == null)
            {
                return;
            }

            for (int index = 0; index < entryRoot.childCount; index++)
            {
                if (entryRoot.GetChild(index) is not RectTransform entryTransform)
                {
                    continue;
                }

                entryTransform.anchorMin = new Vector2(0f, 0f);
                entryTransform.anchorMax = new Vector2(0f, 0f);
                entryTransform.pivot = new Vector2(0f, 0f);
                entryTransform.sizeDelta = entrySize;
                entryTransform.anchoredPosition = new Vector2(
                    0f,
                    index * (entrySize.y + entrySpacing));
            }
        }

        /// <summary>
        /// 지정 시간 동안 표시한 뒤 로그 항목을 페이드 아웃하고 제거합니다.
        /// </summary>
        private IEnumerator FadeAndDestroy(GameObject entry)
        {
            yield return new WaitForSeconds(visibleSeconds);

            float elapsed = 0f;
            while (elapsed < fadeSeconds && entry != null)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeSeconds);
                SetAlpha(entry, alpha);
                yield return null;
            }

            if (entry != null)
            {
                Destroy(entry);
                LayoutEntries();
            }
        }

        /// <summary>
        /// CanvasGroup으로 이미지와 자식 TMP를 함께 투명하게 만듭니다.
        /// </summary>
        private static void SetAlpha(GameObject entry, float alpha)
        {
            if (entry == null)
            {
                return;
            }

            CanvasGroup canvasGroup = entry.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = entry.AddComponent<CanvasGroup>();
            }

            canvasGroup.alpha = alpha;
        }

        /// <summary>
        /// 수동 배치와 충돌하지 않게 entryRoot의 자동 레이아웃 컴포넌트를 끕니다.
        /// </summary>
        private void DisableLayoutGroups()
        {
            if (entryRoot == null)
            {
                return;
            }

            LayoutGroup[] layoutGroups = entryRoot.GetComponents<LayoutGroup>();
            foreach (LayoutGroup layoutGroup in layoutGroups)
            {
                layoutGroup.enabled = false;
            }
        }

        /// <summary>
        /// 프리팹에 배치해둔 미리보기용 로그 항목을 런타임 시작 시 제거합니다.
        /// </summary>
        private void ClearExistingEntries()
        {
            if (!clearExistingEntriesOnAwake || entryRoot == null)
            {
                return;
            }

            for (int index = entryRoot.childCount - 1; index >= 0; index--)
            {
                Destroy(entryRoot.GetChild(index).gameObject);
            }
        }
    }
}
