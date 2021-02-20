using UnityEngine;
using System.Collections.Generic;

namespace ItemSystem
{//#VID-ISNB
}//#VID-ISNE

namespace ItemSystem
{//#VID-2ISNB
	public enum MeleeWeaponItems
	{
		None = 0,
		Dagger = 954029080,
		LightSword = -1515496841,
		Mace = -1822839697,
	}
}//#VID-2ISNE

namespace ItemSystem.Database
{
    public class VIDItemListsV3 : ScriptableObject
    {
        /*Do NOT change the formatting of anything between comments starting with '#VID-'*/

        public static readonly string itemListsName = "VIDItemListsV3";

        //#VID-ICB
		public List<ItemMelee> autoMeleeWeapon = new List<ItemMelee>();
        //#VID-ICE

        /*Those two lists are 'parallel', one shouldn't be changed without the other*/
        /// <summary>Stores taken IDs</summary>
        [HideInInspector]
        public List<int> usedIDs = new List<int>();

        /// <summary>Stores the types of taken IDs</summary>
        [HideInInspector]
        public List<ItemType> typesOfUsedIDs = new List<ItemType>();

        [HideInInspector]
        public List<ItemSubtypeV25> subtypes = new List<ItemSubtypeV25>();
        [HideInInspector]
        public List<ItemTypeGroup> typeGroups = new List<ItemTypeGroup>();
    }
}
