using System;
using System.Collections.Generic;

namespace TownLife.Core
{
    [Serializable]
    public class CollectionSave
    {
        public List<string> dex = new();
        public List<string> achievements = new();
        public List<string> museum = new();
    }
}
