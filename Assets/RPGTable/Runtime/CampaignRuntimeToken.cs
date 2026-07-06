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
        public bool IsDead { get; set; }
        public int MaxHp { get; set; }
        public int CurrentHp { get; set; }
    }
}
