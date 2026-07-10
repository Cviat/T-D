using UnityEngine;

namespace RPGTable.Runtime
{
    using RPGTable.Core;
    public sealed class CampaignRuntimeToken : MonoBehaviour
    {
        public string PlayerId { get; set; }
        public string RuntimeId { get; set; }
        public string TokenPath { get; set; }
        public string CharacterPath { get; set; }
        public string DisplayName { get; set; }
        public TokenTeam Team { get; set; }
        public bool VisibleToPlayers { get; set; }
        public bool IsPlayerViewClone { get; set; }
        public bool IsDead { get; set; }
        public int FootprintSize { get; set; } = 1;
        public int MaxHp { get; set; }
        public int CurrentHp { get; set; }
        public int MaxArmor { get; set; }
        public int CurrentArmor { get; set; }
        public int MaxMovementPoints { get; set; } = 3;
        public int CurrentMovementPoints { get; set; } = 3;
        public int MaxRolls { get; set; } = 1;
        public int CurrentRolls { get; set; } = 1;
        public int ActiveWeaponIndex { get; set; } = 0; // 0 = Weapon 1, 1 = Weapon 2
        public System.Collections.Generic.List<ActiveStatusEffect> statusEffects = new System.Collections.Generic.List<ActiveStatusEffect>();
    }
}
