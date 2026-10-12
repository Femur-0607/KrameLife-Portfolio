namespace TownLife.Core
{
    public interface ISaveStorage
    {
        bool Exists(int slot);
        void Save(int slot, SaveData data);
        SaveData Load(int slot);
    }
}
