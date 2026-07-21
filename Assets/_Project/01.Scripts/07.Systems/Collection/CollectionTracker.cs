using System.Collections.Generic;
using TownLife.Core;
using UnityEngine;

namespace TownLife.Systems.Collection
{
    public sealed class CollectionTracker : MonoBehaviour
    {
        [SerializeField, Tooltip("아이템 획득 성공 시 itemId와 count를 발행하는 채널입니다.")]
        private ItemStackEventChannelSO itemAcquiredChannel = null;

        [SerializeField, Tooltip("제작 성공 시 결과 itemId를 발행하는 채널입니다.")]
        private StringEventChannelSO itemCraftedChannel = null;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [SerializeField, Tooltip("도감 발견/복원 상태를 Console에 출력합니다.")]
        private bool logCollectionDebug = true;
#endif

        private readonly HashSet<string> discoveredItemIds = new();

        public IReadOnlyCollection<string> DiscoveredItemIds => discoveredItemIds;

        private void OnEnable()
        {
            // 다른 시스템이 아이템을 얻거나 제작했을 때 도감 기록만 갱신합니다.
            if (itemAcquiredChannel != null)
            {
                itemAcquiredChannel.OnRaised += HandleItemAcquired;
            }

            if (itemCraftedChannel != null)
            {
                itemCraftedChannel.OnRaised += RecordDiscoveredItem;
            }
        }

        private void OnDisable()
        {
            // 씬 전환이나 오브젝트 비활성화 후 중복 구독이 남지 않게 해제합니다.
            if (itemAcquiredChannel != null)
            {
                itemAcquiredChannel.OnRaised -= HandleItemAcquired;
            }

            if (itemCraftedChannel != null)
            {
                itemCraftedChannel.OnRaised -= RecordDiscoveredItem;
            }
        }

        public bool HasDiscovered(string itemId)
        {
            // 빈 ID는 도감에 기록하지 않는 값이므로 항상 미발견으로 봅니다.
            return !string.IsNullOrWhiteSpace(itemId) && discoveredItemIds.Contains(itemId);
        }

        public void RecordDiscoveredItem(string itemId)
        {
            // 이벤트 채널에서 경고를 냈더라도 기록자는 빈 ID를 조용히 무시합니다.
            if (string.IsNullOrWhiteSpace(itemId))
            {
                LogDebug("도감 기록 무시: itemId가 비어 있습니다.");
                return;
            }

            // HashSet으로 같은 아이템이 여러 번 들어와도 최초 발견 상태만 유지합니다.
            bool added = discoveredItemIds.Add(itemId);
            LogDebug(added
                ? $"도감 발견 기록: itemId={itemId}, total={discoveredItemIds.Count}"
                : $"도감 중복 기록 무시: itemId={itemId}, total={discoveredItemIds.Count}");
        }

        public void ClearDiscoveredItems()
        {
            // 로드 전 기존 런타임 상태를 비워 세이브 데이터와 맞춥니다.
            discoveredItemIds.Clear();
            LogDebug("도감 기록 초기화");
        }

        private void HandleItemAcquired(string itemId, int count)
        {
            // 도감은 발견 여부만 필요하므로 획득 수량은 저장하지 않습니다.
            LogDebug($"도감 획득 이벤트 수신: itemId={itemId}, count={count}");
            RecordDiscoveredItem(itemId);
        }

        private void LogDebug(string message)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (logCollectionDebug)
            {
                Debug.Log(message, this);
            }
#endif
        }

#if UNITY_EDITOR
        [ContextMenu("도감 발견 목록 로그")]
        private void LogDiscoveredItemsForEditor()
        {
            if (discoveredItemIds.Count == 0)
            {
                LogDebug("도감 발견 목록: 비어 있음");
                return;
            }

            List<string> sortedItemIds = new(discoveredItemIds);
            sortedItemIds.Sort();
            LogDebug($"도감 발견 목록 ({sortedItemIds.Count})\n- {string.Join("\n- ", sortedItemIds)}");
        }
#endif
    }
}
