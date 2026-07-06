using UnityEngine;

namespace RPGTable.Core
{
    public enum AbilityTargetType
    {
        Self,
        Ally,
        Enemy,
        Area,
        Object
    }

    public enum AbilityEffectType
    {
        Damage,
        Heal,
        Move,
        Status,
        Reveal
    }

    [CreateAssetMenu(menuName = "RPG Table/Ability Card", fileName = "AbilityCard")]
    public sealed class AbilityCard : ScriptableObject
    {
        [Header("Visuals")]
        public Sprite icon;

        [Header("Player-facing")]
        public string title = "New Ability";
        [TextArea(3, 8)]
        public string description = "Describe what the card does.";
        public int cost;
        public int range = 1;
        public AbilityTargetType targetType = AbilityTargetType.Enemy;

        [Header("Master-facing")]
        public AbilityEffectType effectType = AbilityEffectType.Damage;
        public int effectValue = 1;
        [TextArea(2, 5)]
        public string hiddenNotes;
    }
}
