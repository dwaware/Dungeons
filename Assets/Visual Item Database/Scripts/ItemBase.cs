using UnityEngine;

namespace ItemSystem
{
    [System.Serializable]
    public class ItemBase
    {
        [Header("Generic properties")]
        public string itemName = string.Empty;  //To make sure its defaulted to "", to avoid some weird button name problems
        [TextArea(1, 10)]
        public string itemDescription;

        /// <summary>Unique item ID, should ONLY be changed by the item database!!</summary>
        [HideInInspector]
        public int itemID;
        public bool stackable;
        public int maxStackAmount;

        [Range(0, 1000)]
        public float weight = 1;
        [Range(0, 200)]
        public float volume = 1;    //If you want to make an inventory based on item volume
        public Sprite itemSprite, itemIcon;

        //[Header("Equipping settings")]
        //public bool headEquip;
        //public bool torsoEquip;
        //public bool handEquip;
        //public bool legEquip;
        //public bool twoHanded;

        [HideInInspector/*, Space(10)*/]
        public ItemType itemType;
        //public ElementalDamage elementalDamage;

        /// <summary>
        /// Makes this item an instance of the passed item if found
        /// </summary>
        /// <param name="itemID"></param>
        /// <param name="type"></param>
        public void UpdateGenericProperties(ItemBase itemToChangeTo)
        {
            //Updates generic properties
            itemName = itemToChangeTo.itemName;
            itemDescription = itemToChangeTo.itemDescription;
            itemID = itemToChangeTo.itemID;
            stackable = itemToChangeTo.stackable;
            maxStackAmount = itemToChangeTo.maxStackAmount;
            volume = itemToChangeTo.volume;
            weight = itemToChangeTo.weight;
            itemSprite = itemToChangeTo.itemSprite;
            itemIcon = itemToChangeTo.itemIcon;
            itemType = itemToChangeTo.itemType;
        }

        /// <summary>
        /// Updates any unique properties of the item
        /// </summary>
        /// <param name="itemToChangeTo"></param>
        public virtual void UpdateUniqueProperties(ItemBase itemToChangeTo)
        {
            //Since this is the base item, we have no unique properties to update
        }
    }

    public enum ItemType
    {//#VID-ITB
        Generic,
        MeleeWeapon,
        RangedWeapon,
        Armor,
        Consumable,
    };//#VID-ITE

    public enum ElementalDamage
    {
        None,
        FireDamage,
        IceDamage,
        EarthDamage,
    };
}
