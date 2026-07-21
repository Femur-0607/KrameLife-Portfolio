namespace TownLife.Core
{
    /// <summary>
    /// UI나 입력 시스템이 현재 선택한 인벤토리 아이템을 다른 기능에 알려주는 계약입니다.
    /// </summary>
    public interface ISelectedInventoryItemProvider
    {
        /// <summary>
        /// 현재 선택한 아이템 ID입니다. 선택 아이템이 없으면 빈 문자열입니다.
        /// </summary>
        string SelectedItemId { get; }
    }
}
