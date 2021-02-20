using System.Collections.Generic;

namespace ItemSystem.Database
{
    [System.Serializable]
    public class ItemTypeGroup
    {
        public string name;
        public List<ItemType> types = new List<ItemType>();

        public ItemTypeGroup(string groupName, ItemType defaultType)
        {
            name = groupName;
            types.Add(defaultType);
        }
    }
}