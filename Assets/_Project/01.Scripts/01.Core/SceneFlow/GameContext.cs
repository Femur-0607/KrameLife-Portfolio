// 게임 전역 공통 매니저 참조를 제공하는 런타임 컨텍스트입니다.
using UnityEngine;
using UnityEngine.Serialization;

namespace TownLife.Core
{
    /// <summary>
    /// Persistent 씬에 하나만 두고 씬별 오브젝트가 공통 런타임 매니저를 찾게 하는 컨텍스트입니다.
    /// </summary>
    public sealed class GameContext : MonoBehaviour
    {
        [FormerlySerializedAs("itemInventoryBehaviour")] [SerializeField, Tooltip("일반 아이템 인벤토리를 관리하는 컴포넌트입니다.")]
        private MonoBehaviour itemInventory = null;

        [SerializeField, FormerlySerializedAs("goldBehaviour"), Tooltip("플레이어 골드를 관리하는 컴포넌트입니다.")]
        private MonoBehaviour gold = null;

        [FormerlySerializedAs("itemAcquisitionBehaviour")] [SerializeField, Tooltip("아이템 획득 서비스를 관리하는 컴포넌트입니다.")]
        private MonoBehaviour itemAcquisition = null;

        [SerializeField, Tooltip("Persistent 씬에서 공용 오브젝트 풀을 관리하는 매니저입니다.")]
        private ObjectPoolManager objectPoolManager = null;

        /// <summary>
        /// 현재 로드된 Persistent 씬의 전역 컨텍스트입니다.
        /// </summary>
        public static GameContext Instance { get; private set; }

        /// <summary>
        /// 일반 아이템 인벤토리 인터페이스입니다.
        /// </summary>
        public IInventory ItemInventory { get; private set; }

        /// <summary>
        /// 플레이어 골드 인터페이스입니다.
        /// </summary>
        public IGold Gold { get; private set; }

        /// <summary>
        /// 아이템 획득 공통 진입점 인터페이스입니다.
        /// </summary>
        public IItemAcquisition ItemAcquisition { get; private set; }

        /// <summary>
        /// 씬별 스포너가 사용하는 공용 오브젝트 풀 매니저입니다.
        /// </summary>
        public ObjectPoolManager ObjectPoolManager { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("GameContext가 둘 이상 로드되었습니다. 먼저 로드된 컨텍스트를 유지합니다.", this);
                return;
            }

            Instance = this;
            CacheReferences();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void CacheReferences()
        {
            ItemInventory = itemInventory as IInventory;
            Gold = gold as IGold;
            ItemAcquisition = itemAcquisition as IItemAcquisition;
            ObjectPoolManager = objectPoolManager;

            CacheMissingReferencesFromScene();
        }

        private void CacheMissingReferencesFromScene()
        {
            MonoBehaviour[] sceneBehaviours = FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

            foreach (MonoBehaviour behaviour in sceneBehaviours)
            {
                if (behaviour is IInventory inventory)
                {
                    itemInventory = behaviour;
                    ItemInventory = inventory;
                }

                if (behaviour is IGold foundGold)
                {
                    gold = behaviour;
                    Gold = foundGold;
                }

                if (behaviour is IItemAcquisition itemAcquisition)
                {
                    this.itemAcquisition = behaviour;
                    ItemAcquisition = itemAcquisition;
                }

                if (ObjectPoolManager == null && behaviour is ObjectPoolManager poolManager)
                {
                    objectPoolManager = poolManager;
                    ObjectPoolManager = poolManager;
                }

                if (ItemInventory != null && Gold != null && ItemAcquisition != null && ObjectPoolManager != null)
                {
                    return;
                }
            }
        }
    }
}
