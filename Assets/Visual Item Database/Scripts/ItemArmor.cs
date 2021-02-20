using UnityEngine;

namespace ItemSystem
{
    [System.Serializable]
    public class ItemArmor : ItemBase, IArmor, IDamage
    {
        [SerializeField, Header("Unique properties")]
        ArmorType armorType;

        [Range(1, 100)]
        int condition = 100;

        [SerializeField]
        int defence, crushDamage, edgeDamage, pointDamage;

        public ArmorType ArmorType
        {
            get { return armorType; }
            set { armorType = value; }
        }
        public int Condition
        {
            get { return condition; }
            set { condition = Mathf.Clamp(value, 0, 100); }
        }
        public int Defence
        {
            get { return defence; }
            set { defence = value; }
        }
        public int CrushDamage
        {
            get { return crushDamage; }
            set { crushDamage = value; }
        }
        public int EdgeDamage
        {
            get { return edgeDamage; }
            set { edgeDamage = value; }
        }
        public int PointDamage
        {
            get { return pointDamage; }
            set { pointDamage = value; }
        }

        public override void UpdateUniqueProperties(ItemBase itemToChangeTo)
        {
            ItemArmor newArmor = (ItemArmor)itemToChangeTo;

            ArmorType = newArmor.ArmorType;
            defence = newArmor.defence;
            condition = newArmor.condition;
            crushDamage = newArmor.crushDamage;
            edgeDamage = newArmor.edgeDamage;
            pointDamage = newArmor.pointDamage;
        }

        public void Break()
        {
        }
    }

    /// <summary>
    /// Specifies where this item can be equipped
    /// </summary>
    public enum ArmorType
    {
        Head,
        Chest,
        Hands,
        Feet
    };
}