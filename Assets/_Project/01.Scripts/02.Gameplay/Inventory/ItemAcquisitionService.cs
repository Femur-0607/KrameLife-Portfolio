using TownLife.Core;
using UnityEngine;

namespace TownLife.Gameplay
{
    /// <summary>
    /// 제작, 보상, 채집처럼 아이템을 지급하는 기능이 공통으로 사용하는 획득 진입점입니다.
    /// </summary>
    public sealed class ItemAcquisitionService : MonoBehaviour, IItemAcquisition
    {
        [SerializeField, Tooltip("아이템 ID가 등록된 GameItemSO인지 확인할 데이터베이스입니다.")]
        private GameDatabase gameDatabase = null;

        [SerializeField, Tooltip("아이템을 추가할 일반 인벤토리 컴포넌트입니다. 비워두면 GameContext의 ItemInventory를 사용합니다.")]
        private MonoBehaviour inventoryBehaviour = null;

        [SerializeField, Tooltip("아이템 획득 성공 시 itemId와 수량을 발행할 이벤트 채널입니다.")]
        private ItemStackEventChannelSO itemAcquiredChannel = null;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [SerializeField, Tooltip("아이템 획득/이벤트 발행 상태를 Console에 출력합니다.")]
        private bool logAcquisitionDebug = true;
#endif

        private IInventory inventory = null;

        private void Awake()
        {
            CacheInventory();
        }

        /// <summary>
        /// GameItemSO 참조에서 Id를 꺼내 일반 인벤토리에 아이템을 추가합니다.
        /// </summary>
        public bool AcquireItem(GameItemSO item, int count)
        {
            if (item == null)
            {
                return false;
            }

            return AcquireItem(item.Id, count);
        }

        /// <summary>
        /// 지정한 itemId와 수량을 일반 인벤토리에 추가하고, 성공 시 획득 이벤트를 발행합니다.
        /// </summary>
        public bool AcquireItem(string itemId, int count)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return false;
            }

            if (count <= 0)
            {
                return false;
            }

            if (gameDatabase == null)
            {
                Debug.LogWarning($"Item acquisition failed because GameDatabase is not assigned. itemId: {itemId}", this);
                return false;
            }

            if (gameDatabase.Get<GameItemSO>(itemId) == null)
            {
                return false;
            }

            if (!TryGetInventory())
            {
                Debug.LogWarning($"Item acquisition failed because inventory is not assigned. itemId: {itemId}", this);
                return false;
            }

            if (!inventory.Add(itemId, count))
            {
                return false;
            }

            LogDebug($"아이템 획득 성공: itemId={itemId}, count={count}");
            itemAcquiredChannel?.Raise(itemId, count);
            LogDebug(itemAcquiredChannel != null
                ? $"아이템 획득 이벤트 발행: itemId={itemId}, count={count}"
                : $"아이템 획득 이벤트 미발행: 채널 없음, itemId={itemId}, count={count}");
            return true;
        }

        private bool TryGetInventory()
        {
            if (inventory != null)
            {
                return true;
            }

            CacheInventory();
            return inventory != null;
        }

        private void CacheInventory()
        {
            inventory = inventoryBehaviour as IInventory;

            if (inventory == null && GameContext.Instance != null)
            {
                inventory = GameContext.Instance.ItemInventory;
            }

            if (inventoryBehaviour != null && inventoryBehaviour is not IInventory)
            {
                Debug.LogWarning($"{nameof(ItemAcquisitionService)} needs an inventory component implementing {nameof(IInventory)}.", this);
            }
        }

        private void LogDebug(string message)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (logAcquisitionDebug)
            {
                Debug.Log(message, this);
            }
#endif
        }

    }
}
