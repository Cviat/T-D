using UnityEngine;

namespace RPGTable.Core
{
    [CreateAssetMenu(fileName = "NewCombatAttribute", menuName = "RPG Table/Combat Attribute")]
    public sealed class CombatAttribute : ScriptableObject
    {
        [Header("Visuals")]
        public Sprite icon;

        [Header("Information")]
        public string attributeName = "New Attribute";
        public int value = 1; // Duration or strength

        [TextArea(3, 6)]
        public string description = "Describe the combat effect (e.g. Stun, Bleed).";
    }
}
