using UnityEngine;

namespace RPGTable.Core
{
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

        [Header("Master-facing")]
        public AbilityEffectType effectType = AbilityEffectType.Damage;
        public float multiplier = 1.0f;
        public int defenseValue = 0;
        
        [Header("Combat Attributes")]
        public System.Collections.Generic.List<CombatAttribute> attributes = new System.Collections.Generic.List<CombatAttribute>();
        public AttackType attackType = AttackType.Melee;

        [Header("Combat Animations")]
        public RPGTable.Runtime.CombatAnimationEffect animationEffect;

        [Header("Combat Sound")]
        public AudioClip soundEffect;
    }
}
