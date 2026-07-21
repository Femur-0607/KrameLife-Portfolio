namespace TownLife.Core
{
    /// <summary>
    /// 플레이어가 월드에서 클릭하거나 조사할 수 있는 오브젝트의 공통 상호작용 약속입니다.
    /// </summary>
    public interface IWorldInteractable
    {
        /// <summary>
        /// 현재 상태에서 상호작용을 실행할 수 있는지 확인합니다.
        /// </summary>
        bool CanInteract();

        /// <summary>
        /// 월드 상호작용 입력을 실행합니다.
        /// </summary>
        void Interact();
    }
}
