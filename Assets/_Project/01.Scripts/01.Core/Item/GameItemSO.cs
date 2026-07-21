using UnityEngine;
using UnityEngine.Serialization;

namespace TownLife.Core
{
    /// <summary>
    /// 아이템의 공통 데이터입니다. 인벤토리에는 이 SO가 아니라 Id와 수량만 저장합니다.
    /// </summary>
    [CreateAssetMenu(menuName = "TownLife/Item")]
    public sealed class GameItemSO : IdentifiableSO
    {
        [FormerlySerializedAs("_displayName")]
        [SerializeField, Tooltip("UI와 인벤토리에 표시할 아이템 이름입니다.")]
        private string displayName = string.Empty;

        [FormerlySerializedAs("_description")]
        [SerializeField, TextArea, Tooltip("아이템 상세 정보 UI에 표시할 설명 문구입니다.")]
        private string description = string.Empty;

        [FormerlySerializedAs("_icon")]
        [SerializeField, Tooltip("인벤토리, 상점, 보상 UI에 표시할 아이템 아이콘입니다.")]
        private Sprite icon = null;

        [SerializeField, Tooltip("Addressables로 아이콘 Sprite를 불러올 때 사용할 주소입니다. Addressables 연동 전에는 비워둘 수 있습니다.")]
        private string iconAddress = string.Empty;

        [FormerlySerializedAs("_category")]
        [SerializeField, Tooltip("아이템의 큰 분류입니다. UI 필터, 판매 가능 판정, 마법북 구분 등에 사용합니다.")]
        private ItemCategory category = ItemCategory.Ore;

        [FormerlySerializedAs("_baseSellPrice")]
        [SerializeField, Min(0), Tooltip("상점 판매나 보상 계산에 사용할 기본 판매 가격입니다.")]
        private int baseSellPrice = 0;

        [FormerlySerializedAs("_maxStack")]
        [SerializeField, Min(1), Tooltip("인벤토리 한 묶음에 쌓을 수 있는 최대 수량입니다.")]
        private int maxStack = 99;

        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public string IconAddress => iconAddress;
        public ItemCategory Category => category;
        public int BaseSellPrice => baseSellPrice;
        public int MaxStack => maxStack;
    }
}
