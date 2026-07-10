using System;
using UnityEngine;
using UnityEngine.UI;
using RPGTable.TokenEditor;

namespace RPGTable.Runtime
{
    public class EntityInspectorView : MonoBehaviour
    {
        [SerializeField] private Image portraitImage;
        [SerializeField] private Text nameLabel;
        [SerializeField] private Text stateLabel;
        [SerializeField] private Text descLabel;
        [SerializeField] private Text statsLabel;
        [SerializeField] private InputField hpInput;
        [SerializeField] private Button damageButton;
        [SerializeField] private Button healButton;

        private Button weaponSwitchBtn;
        private Text weaponSwitchTxt;

        public void Setup(
            CampaignRuntimeToken token, 
            SavedTokenData tokenData, 
            RPGTable.CharacterEditor.SavedCharacterData charData,
            Sprite portrait, 
            Action<int> onDamage, 
            Action<int> onHeal)
        {
            if (portraitImage != null && portrait != null)
            {
                portraitImage.sprite = portrait;
                portraitImage.preserveAspect = true;
            }

            if (nameLabel != null) nameLabel.text = token.DisplayName;

            if (stateLabel != null)
            {
                stateLabel.text = $"HP: {token.CurrentHp}/{token.MaxHp} " + (token.IsDead ? "[МЕРТВ]" : "[ЖИВ]");
                stateLabel.color = token.IsDead ? Color.red : new Color(0.2f, 0.8f, 0.2f, 1f);
            }

            if (descLabel != null)
            {
                var desc = charData != null ? charData.description : "";
                desc = string.IsNullOrWhiteSpace(desc) ? "Нет описания." : desc;
                if (desc.Length > 60) desc = desc.Substring(0, 57) + "...";
                descLabel.text = desc;
            }

            if (statsLabel != null)
            if (charData != null)
            {
                bool hasW1 = !string.IsNullOrEmpty(charData.eqWeapon);
                bool hasW2 = !string.IsNullOrEmpty(charData.eqWeapon2);
                if (hasW1 && !hasW2)
                {
                    token.ActiveWeaponIndex = 0;
                }
                else if (!hasW1 && hasW2)
                {
                    token.ActiveWeaponIndex = 1;
                }
            }

            if (statsLabel != null)
            {
                var footprint = tokenData != null ? tokenData.footprintSize : 1;
                
                RPGTable.Core.ItemCard activeWeaponCard = null;
                if (charData != null)
                {
                    string activeWeaponName = (token.ActiveWeaponIndex == 0) ? charData.eqWeapon : charData.eqWeapon2;
                    activeWeaponCard = FindItemCard(activeWeaponName);
                }

                bool melee = activeWeaponCard == null || activeWeaponCard.attackType == RPGTable.Core.AttackType.Melee;
                bool ranged = activeWeaponCard != null && activeWeaponCard.attackType == RPGTable.Core.AttackType.Ranged;
                bool magic = activeWeaponCard != null && activeWeaponCard.attackType == RPGTable.Core.AttackType.Magic;

                string moveStr = CampaignGameSession.IsCombatActive
                    ? $"{token.CurrentMovementPoints}/{token.MaxMovementPoints}"
                    : "∞";

                statsLabel.text = $"Размер сетки: {footprint}x{footprint}\n" +
                                  $"Тип атаки: " + (melee ? "Ближний " : "") + (magic ? "Магия " : "") + (ranged ? "Дальний" : "") + "\n" +
                                  $"Движение: {moveStr}";
            }

            var hasBothWeapons = charData != null && !string.IsNullOrEmpty(charData.eqWeapon) && !string.IsNullOrEmpty(charData.eqWeapon2);

            if (damageButton != null && weaponSwitchBtn == null && hasBothWeapons)
            {
                var go = UnityEngine.Object.Instantiate(damageButton.gameObject, damageButton.transform.parent, false);
                go.name = "Weapon Switch Button";
                weaponSwitchBtn = go.GetComponent<Button>();
                weaponSwitchTxt = go.GetComponentInChildren<Text>();

                var rect = go.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, rect.anchoredPosition.y + 40f);
            }

            if (weaponSwitchBtn != null)
            {
                weaponSwitchBtn.gameObject.SetActive(hasBothWeapons);
                if (hasBothWeapons)
                {
                    var w1 = charData.eqWeapon;
                    var w2 = charData.eqWeapon2;
                    weaponSwitchTxt.text = token.ActiveWeaponIndex == 0 ? $"Оружие: ⚔1 ({w1})" : $"Оружие: ⚔2 ({w2})";

                    weaponSwitchBtn.onClick.RemoveAllListeners();
                    weaponSwitchBtn.onClick.AddListener(() => {
                        token.ActiveWeaponIndex = token.ActiveWeaponIndex == 0 ? 1 : 0;
                        weaponSwitchTxt.text = token.ActiveWeaponIndex == 0 ? $"Оружие: ⚔1 ({w1})" : $"Оружие: ⚔2 ({w2})";
                        
                        // Force refresh UI text
                        Setup(token, tokenData, charData, portrait, onDamage, onHeal);

                        // Force update range highlighting!
                        if (RPGTable.Board.GridHighlighter.Instance != null)
                        {
                            RPGTable.Board.GridHighlighter.Instance.HighlightTokenRanges(token);
                        }
                    });
                }
            }

            if (damageButton != null)
            {
                damageButton.onClick.RemoveAllListeners();
                damageButton.onClick.AddListener(() => {
                    if (hpInput != null && int.TryParse(hpInput.text, out var val))
                    {
                        onDamage?.Invoke(val);
                    }
                });
            }

            if (healButton != null)
            {
                healButton.onClick.RemoveAllListeners();
                healButton.onClick.AddListener(() => {
                    if (hpInput != null && int.TryParse(hpInput.text, out var val))
                    {
                        onHeal?.Invoke(val);
                    }
                });
            }
        }

        private RPGTable.Core.ItemCard FindItemCard(string title)
        {
            if (string.IsNullOrEmpty(title)) return null;
            var cards = Resources.LoadAll<RPGTable.Core.ItemCard>("ItemCards");
            foreach (var card in cards)
            {
                if (card != null && string.Equals(card.title, title, StringComparison.OrdinalIgnoreCase))
                {
                    return card;
                }
            }
            return null;
        }
    }
}
