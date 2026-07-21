using System;
using System.Collections.Generic;

namespace TownLife.Core
{
    [Serializable]
    public class UnlockSave
    {
        public List<string> recipes = new();
        public List<string> paths = new();
        public List<string> techNodes = new();
    }
}
