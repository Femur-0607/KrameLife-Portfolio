using System;
using System.Collections.Generic;

namespace TownLife.Core
{
    [Serializable]
    public class RelationshipSave
    {
        public Dictionary<string, int> affection = new();
    }
}
