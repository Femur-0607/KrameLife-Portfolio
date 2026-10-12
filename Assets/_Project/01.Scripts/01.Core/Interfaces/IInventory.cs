namespace TownLife.Core
{
    public interface IInventory
    {
        bool Add(string itemId, int count);
        bool Remove(string itemId, int count);
        int Count(string itemId);
    }
}
