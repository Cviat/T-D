using UnityEngine;
using System.Linq;

namespace RPGTable.Runtime.ActionCards
{
    public class InvisibilityCardEffect : IActionCardEffect
    {
        public string EffectId => "invisibility";

        public bool Execute(CampaignPlayerData caster, ActionCard card, Vector2Int targetPosition, out string failReason)
        {
            failReason = "";
            var tokens = GameObject.FindObjectsByType<CampaignRuntimeToken>(FindObjectsInactive.Exclude);
            var targetToken = tokens.FirstOrDefault(t => !t.IsPlayerViewClone && t.PlayerId == caster.id);
            if (targetToken == null)
            {
                failReason = "Caster token not found on the active map";
                return false;
            }

            // Apply translucency effect to sprite renderer / mesh
            targetToken.ApplyInvisibility(card.duration);
            return true;
        }
    }

    public class HealPartyCardEffect : IActionCardEffect
    {
        public string EffectId => "heal_party";

        public bool Execute(CampaignPlayerData caster, ActionCard card, Vector2Int targetPosition, out string failReason)
        {
            failReason = "";
            int healAmount = Mathf.RoundToInt(card.power);
            foreach (var player in CampaignGameSession.CurrentPlayers)
            {
                if (player != null && !player.isDead)
                {
                    player.currentHp = Mathf.Min(player.maxHp, player.currentHp + healAmount);
                    CampaignGameSession.UpdateTokenCombatStats(
                        player.id, player.currentMapId,
                        player.currentHp, player.maxHp,
                        player.currentArmor, player.maxArmor,
                        player.currentMovementPoints, player.maxMovementPoints,
                        player.currentRolls, player.maxRolls,
                        player.activeWeaponIndex, player.rerollCoins,
                        player.statusEffects, player.isDead);
                }
            }
            return true;
        }
    }

    public class RestoreArmorCardEffect : IActionCardEffect
    {
        public string EffectId => "restore_armor";

        public bool Execute(CampaignPlayerData caster, ActionCard card, Vector2Int targetPosition, out string failReason)
        {
            failReason = "";
            int armorAmount = Mathf.RoundToInt(card.power);
            foreach (var player in CampaignGameSession.CurrentPlayers)
            {
                if (player != null && !player.isDead)
                {
                    player.currentArmor = Mathf.Min(player.maxArmor, player.currentArmor + armorAmount);
                    CampaignGameSession.UpdateTokenCombatStats(
                        player.id, player.currentMapId,
                        player.currentHp, player.maxHp,
                        player.currentArmor, player.maxArmor,
                        player.currentMovementPoints, player.maxMovementPoints,
                        player.currentRolls, player.maxRolls,
                        player.activeWeaponIndex, player.rerollCoins,
                        player.statusEffects, player.isDead);
                }
            }
            return true;
        }
    }

    public class FireballCardEffect : IActionCardEffect
    {
        public string EffectId => "fireball";

        public bool Execute(CampaignPlayerData caster, ActionCard card, Vector2Int targetPosition, out string failReason)
        {
            failReason = "";
            int damage = Mathf.RoundToInt(card.power);
            int radius = card.radius;

            // Damage all tokens (usually NPCs) in target radius
            if (string.IsNullOrEmpty(caster.currentMapId))
            {
                failReason = "Caster current map is unknown";
                return false;
            }

            if (CampaignGameSession.MapTokenStates.TryGetValue(caster.currentMapId, out var tokens))
            {
                foreach (var token in tokens)
                {
                    if (token != null && !token.isDead && token.team != Core.TokenTeam.Player)
                    {
                        int dx = Mathf.Abs(token.gridPosition.x - targetPosition.x);
                        int dy = Mathf.Abs(token.gridPosition.y - targetPosition.y);
                        if (dx <= radius && dy <= radius)
                        {
                            token.currentHp = Mathf.Max(0, token.currentHp - damage);
                            if (token.currentHp <= 0)
                            {
                                token.isDead = true;
                            }
                            CampaignGameSession.TriggerPlayersChanged();
                        }
                    }
                }
            }

            return true;
        }
    }
}
