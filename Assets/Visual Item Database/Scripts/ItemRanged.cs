using UnityEngine;

namespace ItemSystem
{
    [System.Serializable]
    public class ItemRanged : ItemBase, IWeapon
    {
        [SerializeField, Range(1, 100), Header("Unique properties")]
        int condition = 100;

        [SerializeField]
        int crushDamage, edgeDamage, pointDamage;

        public int Condition
        {
            get { return condition; }
            set { condition = Mathf.Clamp(value, 0, 100); }
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
            ItemRanged ranged = (ItemRanged)itemToChangeTo;

            condition = ranged.condition;
            crushDamage = ranged.crushDamage;
            edgeDamage = ranged.edgeDamage;
            pointDamage = ranged.pointDamage;
        }

        public void Break()
        {
        }
    }
}