using UnityEngine;

namespace RPGTable.Core
{
    public enum CombatAttributeStat
    {
        None,
        HP,
        MovementPoints,
        Rolls,
        Armor
    }

    [CreateAssetMenu(fileName = "NewCombatAttribute", menuName = "RPG Table/Combat Attribute")]
    public sealed class CombatAttribute : ScriptableObject
    {
        [Header("Visuals")]
        public Sprite icon;

        [Header("Information")]
        public string attributeName = "New Attribute";
        public CombatAttributeStat affectedStat = CombatAttributeStat.HP;
        public int value = 1; // Strength or magnitude of modifier (e.g. -2, +1)
        public int durationTurns = 1; // 0 for instant, >0 for status effects
        public bool appliedToSelf = false;

        [TextArea(3, 6)]
        public string description = "Describe the combat effect (e.g. Stun, Bleed).";
    }
}
