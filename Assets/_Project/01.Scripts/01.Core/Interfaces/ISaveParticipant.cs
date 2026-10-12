namespace TownLife.Core
{
    public interface ISaveParticipant
    {
        void Capture(SaveData data);
        void Restore(SaveData data);
    }
}
