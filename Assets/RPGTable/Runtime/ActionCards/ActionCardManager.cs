using UnityEngine;
using System.Collections.Generic;

namespace RPGTable.Runtime.ActionCards
{
    public class ActionCardManager : MonoBehaviour
    {
        private static ActionCardManager instance;
        public static ActionCardManager Instance => instance;

        private Dictionary<string, IActionCardEffect> effects = new Dictionary<string, IActionCardEffect>();

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);

            RegisterEffect(new InvisibilityCardEffect());
            RegisterEffect(new HealPartyCardEffect());
            RegisterEffect(new RestoreArmorCardEffect());
            RegisterEffect(new FireballCardEffect());
        }

        public void RegisterEffect(IActionCardEffect effect)
        {
            effects[effect.EffectId] = effect;
        }

        public bool PlayCard(string playerId, string cardId, Vector2Int targetPosition, out string error)
        {
            error = "";
            var player = CampaignGameSession.FindPlayer(playerId);
            if (player == null)
            {
                error = "Player not found";
                return false;
            }

            var card = Resources.Load<ActionCard>($"ActionCards/{cardId}");
            if (card == null)
            {
                error = $"Card not found: {cardId}";
                return false;
            }

            if (effects.TryGetValue(card.effectId, out var effect))
            {
                bool success = effect.Execute(player, card, targetPosition, out error);
                if (success)
                {
                    if (ActionCardVisualManager.Instance != null)
                    {
                        ActionCardVisualManager.Instance.ShowCardFlyIn(card);
                    }
                    return true;
                }
                return false;
            }

            error = $"Effect strategy not implemented for {card.effectId}";
            return false;
        }
    }
}
