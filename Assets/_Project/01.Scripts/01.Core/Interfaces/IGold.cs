namespace TownLife.Core
{
    public interface IGold
    {
        int Gold { get; }
        bool TrySpend(int amount);
        void Add(int amount);
    }
}
