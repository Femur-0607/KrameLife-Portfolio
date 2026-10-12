using System.Collections;
using System.Collections.Generic;
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

        [SerializeField, Tooltip("획득 로그 항목을 재사용할 오브젝트 풀 매니저입니다. 비워두면 GameContext에서 찾습니다.")]
        private ObjectPoolManager objectPoolManager = null;

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
        private int maxEntries = 3;

        [SerializeField, Tooltip("프리팹 미리보기용으로 남아 있는 기존 로그 자식을 시작할 때 비웁니다.")]
        private bool clearExistingEntriesOnAwake = true;

        private readonly Dictionary<GameObject, Coroutine> fadeRoutinesByEntry = new();
        private readonly HashSet<GameObject> pooledEntries = new();

        private void Awake()
        {
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

            GameItemSO item = ResolveItem(itemId);
            string displayName = item != null && !string.IsNullOrWhiteSpace(item.DisplayName)
                ? item.DisplayName
                : itemId;
            ShowEntry($"{displayName} x{count}", item);
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
        private void ShowEntry(string message, GameItemSO item = null)
        {
            if (entryRoot == null || entryPrefab == null)
            {
                return;
            }

            Image entry = GetEntry();
            if (entry == null)
            {
                return;
            }

            AcquisitionLogEntryView entryView = entry.GetComponent<AcquisitionLogEntryView>();
            if (entryView != null)
            {
                entryView.SetEntry(message, item);
            }
            else
            {
                TMP_Text entryText = entry.GetComponentInChildren<TMP_Text>(true);
                if (entryText != null)
                {
                    entryText.text = message;
                }
            }

            SetAlpha(entry.gameObject, 1f);
            entry.transform.SetAsFirstSibling();

            TrimEntries();
            RebuildLayout();
            fadeRoutinesByEntry[entry.gameObject] = StartCoroutine(FadeAndReturn(entry.gameObject));
        }

        /// <summary>
        /// 아이템 ID에 해당하는 게임 아이템 데이터를 반환합니다.
        /// </summary>
        private GameItemSO ResolveItem(string itemId)
        {
            return gameDatabase != null ? gameDatabase.Get<GameItemSO>(itemId) : null;
        }

        /// <summary>
        /// 최대 표시 개수를 넘은 오래된 로그 항목을 정리합니다.
        /// </summary>
        private void TrimEntries()
        {
            int overflowCount = entryRoot.childCount - maxEntries;
            for (int index = 0; index < overflowCount; index++)
            {
                Transform oldestEntry = entryRoot.GetChild(entryRoot.childCount - 1);
                ReturnEntry(oldestEntry.gameObject);
            }
        }

        /// <summary>
        /// Vertical Layout Group에 최신 자식 순서를 반영합니다.
        /// </summary>
        private void RebuildLayout()
        {
            if (entryRoot is not RectTransform entryRootRect)
            {
                return;
            }

            LayoutRebuilder.MarkLayoutForRebuild(entryRootRect);
        }

        /// <summary>
        /// 지정 시간 동안 표시한 뒤 로그 항목을 페이드 아웃하고 풀로 반환합니다.
        /// </summary>
        private IEnumerator FadeAndReturn(GameObject entry)
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
                fadeRoutinesByEntry.Remove(entry);
                ReturnEntry(entry, false);
                RebuildLayout();
            }
        }

        /// <summary>
        /// 풀에서 로그 항목을 가져오고, 풀을 사용할 수 없으면 직접 생성합니다.
        /// </summary>
        private Image GetEntry()
        {
            ObjectPoolManager poolManager = ResolveObjectPoolManager();
            if (poolManager != null)
            {
                Image pooledEntry = poolManager.Get(entryPrefab, Vector3.zero, Quaternion.identity, entryRoot);
                if (pooledEntry != null)
                {
                    pooledEntry.transform.SetParent(entryRoot, false);
                    pooledEntry.rectTransform.localScale = entryPrefab.rectTransform.localScale;
                    pooledEntries.Add(pooledEntry.gameObject);
                    return pooledEntry;
                }
            }

            return Instantiate(entryPrefab, entryRoot);
        }

        /// <summary>
        /// 사용이 끝난 로그 항목을 풀로 돌려보내거나 직접 생성된 항목이면 제거합니다.
        /// </summary>
        private void ReturnEntry(GameObject entry, bool stopFadeRoutine = true)
        {
            if (entry == null)
            {
                return;
            }

            if (stopFadeRoutine)
            {
                StopFadeRoutine(entry);
            }

            ObjectPoolManager poolManager = ResolveObjectPoolManager();
            if (poolManager != null && pooledEntries.Contains(entry))
            {
                poolManager.Return(entry);
                return;
            }

            entry.transform.SetParent(null, false);
            Destroy(entry);
        }

        /// <summary>
        /// 재사용 전 이전 표시 코루틴이 남아 있으면 정리합니다.
        /// </summary>
        private void StopFadeRoutine(GameObject entry)
        {
            if (fadeRoutinesByEntry.TryGetValue(entry, out Coroutine fadeRoutine) && fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
            }

            fadeRoutinesByEntry.Remove(entry);
        }

        /// <summary>
        /// 인스펙터 연결 또는 GameContext에서 오브젝트 풀 매니저를 반환합니다.
        /// </summary>
        private ObjectPoolManager ResolveObjectPoolManager()
        {
            if (objectPoolManager == null && GameContext.Instance != null)
            {
                objectPoolManager = GameContext.Instance.ObjectPoolManager;
            }

            return objectPoolManager;
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
