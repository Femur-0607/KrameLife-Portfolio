namespace TownLife.Core
{
    /// <summary>
    /// 플레이어가 들고 있는 아이템을 월드 오브젝트에 사용할 때 대상이 구현하는 계약입니다.
    /// </summary>
    public interface IItemUseTarget
    {
        /// <summary>
        /// 지정한 아이템을 현재 대상에 사용할 수 있는지 확인합니다.
        /// </summary>
        bool CanUseItem(string itemId);

        /// <summary>
        /// 지정한 아이템을 현재 대상에 사용합니다.
        /// </summary>
        bool TryUseItem(string itemId);
    }
}
