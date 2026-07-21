namespace TownLife.Core
{
    /// <summary>
    /// 제작, 보상, 채집처럼 아이템을 지급하는 기능이 공통으로 사용하는 획득 계약입니다.
    /// </summary>
    public interface IItemAcquisition
    {
        /// <summary>
        /// 지정한 itemId와 수량을 인벤토리에 추가합니다.
        /// </summary>
        bool AcquireItem(string itemId, int count);

        /// <summary>
        /// GameItemSO 참조에서 Id를 꺼내 인벤토리에 추가합니다.
        /// </summary>
        bool AcquireItem(GameItemSO item, int count);
    }
}
