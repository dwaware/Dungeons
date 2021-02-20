using System.Collections.Generic;

namespace ItemSystem.Database
{
    [System.Serializable]
    public class ItemSubtypeV25
    {
        public string name;
        public ItemType type;
        public List<int> itemIDs;

        public ItemSubtypeV25()
        {

        }

        public ItemSubtypeV25(string subtypeName, ItemType subtype)
        {
            name = subtypeName;
            type = subtype;
            itemIDs = new List<int>();
        }
    }
}