using System;
using System.Collections.Generic;

namespace TownLife.Core
{
    [Serializable]
    public class InventorySave
    {
        public List<ItemStack> items = new();
        public int unlockedSlotCount;
    }
}
