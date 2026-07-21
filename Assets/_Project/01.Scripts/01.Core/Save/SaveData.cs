using System;

namespace TownLife.Core
{
    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public PlayerSave player = new();
        public InventorySave inventory = new();
        public TimeSave time = new();
        public ShopSave shop = new();
        public ReputationSave reputation = new();
        public MagicBookSave magicBook = new();
        public UnlockSave unlocks = new();
        public CollectionSave collection = new();
        public RelationshipSave relationships = new();
    }
}
