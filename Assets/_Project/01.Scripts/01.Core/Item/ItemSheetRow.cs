namespace TownLife.Core
{
    /// <summary>
    /// Google Sheets 카테고리 탭의 한 행을 코드에서 다루기 쉬운 형태로 보관합니다.
    /// category는 컬럼이 아니라 탭 이름에서 결정됩니다.
    /// </summary>
    public sealed class ItemSheetRow
    {
        /// <summary>
        /// 아이템 고유 ID입니다. 인벤토리 저장, 드롭, 상점, 레시피에서 참조합니다.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// UI에 표시할 아이템 이름입니다.
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// UI 상세 설명에 표시할 아이템 설명입니다.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 이 행이 들어 있던 탭의 아이템 카테고리입니다.
        /// </summary>
        public ItemCategory Category { get; set; } = ItemCategory.Ore;

        /// <summary>
        /// 상점 판매나 보상 계산에 사용할 기본 판매 가격 문자열입니다.
        /// 검증 단계에서 int로 변환합니다.
        /// </summary>
        public string BaseSellPrice { get; set; } = string.Empty;

        /// <summary>
        /// 인벤토리 한 슬롯에 쌓을 수 있는 최대 수량 문자열입니다.
        /// 검증 단계에서 int로 변환합니다.
        /// </summary>
        public string MaxStack { get; set; } = string.Empty;

        /// <summary>
        /// Addressables로 아이콘 Sprite를 불러올 때 사용할 주소 문자열입니다.
        /// Addressables 연동 전에는 비워둘 수 있습니다.
        /// </summary>
        public string IconAddress { get; set; } = string.Empty;
    }
}
