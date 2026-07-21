using TownLife.Core;
using TownLife.Systems;
using UnityEngine;

namespace TownLife.Gameplay
{
    /// <summary>
    /// 소에게 특정 아이템을 먹일 수 있게 하는 아이템 사용 대상입니다.
    /// </summary>
    [RequireComponent(typeof(CowMilkProducer))]
    public sealed class CowFeedReceiver : MonoBehaviour, IItemUseTarget
    {
        [Tooltip("소에게 먹일 수 있는 여물 아이템 ID입니다.")]
        private string fodderItemId = "item.material.fodder_grass";

        [SerializeField, Min(0), Tooltip("먹이를 주면 다음날 추가될 우유 생산량입니다.")]
        private int bonusMilkCountNextDay = 1;

        [SerializeField, Tooltip("켜두면 하루에 한 번만 먹이를 줄 수 있습니다.")]
        private bool feedOncePerDay = true;

        private CowMilkProducer milkProducer;
        private TimeManager timeManager;
        private int lastFedDay = -1;

        private void Awake()
        {
            CacheReferences();
        }

        /// <summary>
        /// 현재 선택 아이템이 소에게 먹일 수 있는 아이템인지 확인합니다.
        /// </summary>
        public bool CanUseItem(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId) || itemId != fodderItemId)
            {
                return false;
            }

            return !feedOncePerDay || GetCurrentDay() != lastFedDay;
        }

        /// <summary>
        /// 소에게 먹이를 주고 다음날 우유 보너스를 예약합니다.
        /// </summary>
        public bool TryUseItem(string itemId)
        {
            if (!CanUseItem(itemId))
            {
                Debug.Log("소에게 먹일 수 없는 아이템이거나 오늘은 이미 먹이를 줬습니다.", this);
                return false;
            }

            if (milkProducer == null)
            {
                CacheReferences();
            }

            milkProducer?.AddBonusMilkNextDay(bonusMilkCountNextDay);
            lastFedDay = GetCurrentDay();
            Debug.Log($"소 먹이기 성공: itemId={itemId}, nextDayMilkBonus={bonusMilkCountNextDay}", this);
            return true;
        }

        /// <summary>
        /// 필요한 주변 컴포넌트 참조를 찾습니다.
        /// </summary>
        private void CacheReferences()
        {
            if (milkProducer == null)
            {
                milkProducer = GetComponent<CowMilkProducer>();
            }

            if (timeManager == null)
            {
                timeManager = FindAnyObjectByType<TimeManager>();
            }
        }

        /// <summary>
        /// TimeManager가 없을 때도 테스트가 가능하도록 현재 날짜를 안전하게 반환합니다.
        /// </summary>
        private int GetCurrentDay()
        {
            return timeManager != null ? timeManager.CurrentDay : 0;
        }

#if UNITY_EDITOR
        private void Reset()
        {
            CacheReferences();
        }

        private void OnValidate()
        {
            bonusMilkCountNextDay = Mathf.Max(0, bonusMilkCountNextDay);
            CacheReferences();
        }
#endif
    }
}
