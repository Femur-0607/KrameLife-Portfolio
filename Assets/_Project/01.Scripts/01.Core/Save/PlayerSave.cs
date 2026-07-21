using System;
using System.Collections.Generic;

namespace TownLife.Core
{
    [Serializable]
    public class PlayerSave
    {
        public int gold;
        public float stamina;
        public float maxStamina;
        public float hp;
        public float maxHp;
        public Dictionary<string, int> skills = new();
    }
}
