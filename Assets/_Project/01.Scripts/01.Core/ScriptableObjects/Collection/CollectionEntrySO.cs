using UnityEngine;

namespace TownLife.Core
{
    [CreateAssetMenu(menuName = "TownLife/Collection/Collection Entry")]
    public sealed class CollectionEntrySO : IdentifiableSO
    {
        [SerializeField, Tooltip("이 도감 항목이 기록하는 실제 콘텐츠 ID입니다. 예: item.ore.iron")]
        private string targetId = string.Empty;

        [SerializeField, Tooltip("도감 UI에 표시할 이름입니다.")]
        private string displayName = string.Empty;

        [SerializeField, TextArea, Tooltip("도감 UI에 표시할 설명입니다.")]
        private string description = string.Empty;

        [SerializeField, Tooltip("도감 UI에 표시할 아이콘입니다.")]
        private Sprite icon = null;

        [SerializeField, Tooltip("도감 탭과 필터에 사용할 분류입니다.")]
        private CollectionEntryCategory category = CollectionEntryCategory.Item;

        public string TargetId => targetId;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public CollectionEntryCategory Category => category;
    }
}
