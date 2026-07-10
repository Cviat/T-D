using UnityEngine;

namespace RPGTable.Core
{
    public enum ItemType
    {
        Helmet,
        Armor,
        Weapon,
        Shield,
        Boots,
        Amulet,
        Ring,
        Artifact,
        Belt,
        General
    }

    [CreateAssetMenu(fileName = "NewItemCard", menuName = "RPG Table/Item Card")]
    public sealed class ItemCard : ScriptableObject
    {
        public string title;
        [TextArea(3, 5)]
        public string description;
        public Sprite icon;
        public ItemType itemType = ItemType.General;

        [Header("Weapon Stats")]
        public string scaleStat1 = "None"; // STR, AGI, INT, HOL, None
        public float coef1 = 0f;
        public string scaleStat2 = "None";
        public float coef2 = 0f;

        [Header("Armor Stats")]
        public int armorPoints = 0;

        [Header("Stat Bonuses")]
        public int bonusHp = 0;
        public int bonusStr = 0;
        public int bonusAgi = 0;
        public int bonusInt = 0;
        public int bonusHol = 0;

        [Header("Attributes / Effects")]
        public System.Collections.Generic.List<CombatAttribute> attributes = new System.Collections.Generic.List<CombatAttribute>();
        public AttackType attackType = AttackType.Melee;
    }
}
