using System;
using RPGTable.TokenEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RPGTable.Runtime
{
    public class EntityInspectorView : MonoBehaviour
    {
        [SerializeField] private Image portraitImage;
        [SerializeField] private Text nameLabel;
        [SerializeField] private Text stateLabel;
        [SerializeField] private Text descLabel;
        [SerializeField] private Text statsLabel;
        [SerializeField] private Text armorLabel;
        [SerializeField] private Text rollsLabel;
        [SerializeField] private Text movementLabel;
        [SerializeField] private Text weaponLabel;
        [SerializeField] private Text attacksLabel;
        [SerializeField] private Text defenseLabel;
        [SerializeField] private Text statusesLabel;
        [SerializeField] private InputField hpInput;
        [SerializeField] private Button damageButton;
        [SerializeField] private Button healButton;
        [SerializeField] private Button weaponSwitchButton;
        [SerializeField] private Text weaponSwitchText;

        public void Setup(
            CampaignRuntimeToken token,
            SavedTokenData tokenData,
            RPGTable.CharacterEditor.SavedCharacterData charData,
            Sprite portrait,
            Action<int> onDamage,
            Action<int> onHeal)
        {
            if (token == null)
            {
                return;
            }

            ConfigureTextLayout();

            if (portraitImage != null && portrait != null)
            {
                portraitImage.sprite = portrait;
                portraitImage.preserveAspect = true;
            }

            if (nameLabel != null) nameLabel.text = token.DisplayName;
            if (stateLabel != null)
            {
                stateLabel.text = $"HP: {token.CurrentHp}/{token.MaxHp} " + (token.IsDead ? "[\u041c\u0415\u0420\u0422\u0412]" : "[\u0416\u0418\u0412]");
                stateLabel.color = token.IsDead ? Color.red : new Color(0.2f, 0.8f, 0.2f, 1f);
            }

            if (descLabel != null)
            {
                var desc = charData != null ? charData.description : "";
                desc = string.IsNullOrWhiteSpace(desc) ? "\u041d\u0435\u0442 \u043e\u043f\u0438\u0441\u0430\u043d\u0438\u044f." : desc;
                if (desc.Length > 72) desc = desc.Substring(0, 69) + "...";
                descLabel.text = desc;
            }

            NormalizeActiveWeapon(token, charData);

            var footprint = tokenData != null ? tokenData.footprintSize : Mathf.Max(1, token.FootprintSize);
            var activeWeaponName = "";
            var activeAttackSlots = (string[])null;
            var activeWeaponCard = (RPGTable.Core.ItemCard)null;

            if (charData != null)
            {
                activeWeaponName = token.ActiveWeaponIndex == 0 ? charData.eqWeapon : charData.eqWeapon2;
                activeAttackSlots = token.ActiveWeaponIndex == 0 ? charData.attackSlots : charData.attack2Slots;
                activeWeaponCard = FindItemCard(activeWeaponName);
            }

            if (statsLabel != null)
            {
                var melee = activeWeaponCard == null || activeWeaponCard.attackType == RPGTable.Core.AttackType.Melee;
                var ranged = activeWeaponCard != null && activeWeaponCard.attackType == RPGTable.Core.AttackType.Ranged;
                var magic = activeWeaponCard != null && activeWeaponCard.attackType == RPGTable.Core.AttackType.Magic;

                statsLabel.text = $"\u0420\u0430\u0437\u043c\u0435\u0440: {footprint}x{footprint}\n" +
                                  "\u0422\u0438\u043f: " + (melee ? "\u0411\u043b\u0438\u0436\u043d\u0438\u0439 " : "") + (magic ? "\u041c\u0430\u0433\u0438\u044f " : "") + (ranged ? "\u0414\u0430\u043b\u044c\u043d\u0438\u0439" : "");
            }

            if (armorLabel != null) armorLabel.text = $"\u0411\u0440\u043e\u043d\u044f: {token.CurrentArmor}/{token.MaxArmor}";
            if (rollsLabel != null) rollsLabel.text = $"\u0411\u0440\u043e\u0441\u043a\u0438: {token.CurrentRolls}/{token.MaxRolls}";
            if (movementLabel != null)
            {
                movementLabel.text = CampaignGameSession.IsCombatActive
                    ? $"\u0425\u043e\u0434: {token.CurrentMovementPoints}/{token.MaxMovementPoints}"
                    : "\u0425\u043e\u0434: \u0441\u0432\u043e\u0431\u043e\u0434\u043d\u043e";
            }

            if (weaponLabel != null)
            {
                var slot = token.ActiveWeaponIndex == 0 ? "1" : "2";
                weaponLabel.text = string.IsNullOrWhiteSpace(activeWeaponName)
                    ? "\u041e\u0440\u0443\u0436\u0438\u0435: \u043d\u0435\u0442"
                    : $"\u041e\u0440\u0443\u0436\u0438\u0435 {slot}: {activeWeaponName}";
            }

            if (attacksLabel != null) attacksLabel.text = "\u0410\u0442\u0430\u043a\u0438:\n" + FormatSlots(activeAttackSlots);
            if (defenseLabel != null) defenseLabel.text = "\u0417\u0430\u0449\u0438\u0442\u0430:\n" + FormatSlots(charData?.defenseSlots);
            if (statusesLabel != null) statusesLabel.text = "\u0421\u0442\u0430\u0442\u0443\u0441\u044b: " + FormatStatuses(token);

            ConfigureWeaponSwitch(token, tokenData, charData, portrait, onDamage, onHeal);
            ConfigureHpButtons(onDamage, onHeal);
        }

        private void ConfigureWeaponSwitch(
            CampaignRuntimeToken token,
            SavedTokenData tokenData,
            RPGTable.CharacterEditor.SavedCharacterData charData,
            Sprite portrait,
            Action<int> onDamage,
            Action<int> onHeal)
        {
            var hasBothWeapons = charData != null
                && !string.IsNullOrWhiteSpace(charData.eqWeapon)
                && !string.IsNullOrWhiteSpace(charData.eqWeapon2);

            if (weaponSwitchButton == null)
            {
                return;
            }

            weaponSwitchButton.gameObject.SetActive(hasBothWeapons);
            weaponSwitchButton.onClick.RemoveAllListeners();

            if (!hasBothWeapons)
            {
                return;
            }

            if (weaponSwitchText != null)
            {
                weaponSwitchText.text = token.ActiveWeaponIndex == 0 ? "\u0421\u043c\u0435\u043d\u0438\u0442\u044c \u043d\u0430 2" : "\u0421\u043c\u0435\u043d\u0438\u0442\u044c \u043d\u0430 1";
            }

            weaponSwitchButton.onClick.AddListener(() =>
            {
                token.ActiveWeaponIndex = token.ActiveWeaponIndex == 0 ? 1 : 0;
                Setup(token, tokenData, charData, portrait, onDamage, onHeal);

                if (RPGTable.Board.GridHighlighter.Instance != null)
                {
                    RPGTable.Board.GridHighlighter.Instance.HighlightTokenRanges(token);
                }
            });
        }

        private void ConfigureHpButtons(Action<int> onDamage, Action<int> onHeal)
        {
            if (damageButton != null)
            {
                damageButton.onClick.RemoveAllListeners();
                damageButton.onClick.AddListener(() =>
                {
                    if (hpInput != null && int.TryParse(hpInput.text, out var val))
                    {
                        onDamage?.Invoke(val);
                    }
                });
            }

            if (healButton != null)
            {
                healButton.onClick.RemoveAllListeners();
                healButton.onClick.AddListener(() =>
                {
                    if (hpInput != null && int.TryParse(hpInput.text, out var val))
                    {
                        onHeal?.Invoke(val);
                    }
                });
            }
        }

        private void ConfigureTextLayout()
        {
            ConfigureBestFit(nameLabel, 10, 15, HorizontalWrapMode.Wrap, VerticalWrapMode.Truncate);
            ConfigureBestFit(stateLabel, 9, 12, HorizontalWrapMode.Wrap, VerticalWrapMode.Truncate);
            ConfigureBestFit(descLabel, 8, 10, HorizontalWrapMode.Wrap, VerticalWrapMode.Truncate);
            ConfigureBestFit(weaponLabel, 8, 11, HorizontalWrapMode.Wrap, VerticalWrapMode.Truncate);
            ConfigureBestFit(attacksLabel, 7, 9, HorizontalWrapMode.Wrap, VerticalWrapMode.Truncate);
            ConfigureBestFit(defenseLabel, 7, 9, HorizontalWrapMode.Wrap, VerticalWrapMode.Truncate);
            ConfigureBestFit(statusesLabel, 7, 9, HorizontalWrapMode.Wrap, VerticalWrapMode.Truncate);
        }

        private static void ConfigureBestFit(Text text, int minSize, int maxSize, HorizontalWrapMode horizontalWrap, VerticalWrapMode verticalWrap)
        {
            if (text == null)
            {
                return;
            }

            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = minSize;
            text.resizeTextMaxSize = maxSize;
            text.horizontalOverflow = horizontalWrap;
            text.verticalOverflow = verticalWrap;
        }

        private static void NormalizeActiveWeapon(CampaignRuntimeToken token, RPGTable.CharacterEditor.SavedCharacterData charData)
        {
            if (charData == null)
            {
                return;
            }

            var hasW1 = !string.IsNullOrWhiteSpace(charData.eqWeapon);
            var hasW2 = !string.IsNullOrWhiteSpace(charData.eqWeapon2);

            if (hasW1 && !hasW2)
            {
                token.ActiveWeaponIndex = 0;
            }
            else if (!hasW1 && hasW2)
            {
                token.ActiveWeaponIndex = 1;
            }
        }

        private static string FormatSlots(string[] slots)
        {
            if (slots == null || slots.Length == 0)
            {
                return "-";
            }

            var result = "";
            for (var i = 0; i < slots.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(slots[i]))
                {
                    continue;
                }

                if (result.Length > 0)
                {
                    result += "\n";
                }

                result += $"{i + 1}:{slots[i]}";
            }

            return result.Length == 0 ? "-" : result;
        }

        private static string FormatStatuses(CampaignRuntimeToken token)
        {
            if (token == null || token.statusEffects == null || token.statusEffects.Count == 0)
            {
                return "-";
            }

            var result = "";
            foreach (var effect in token.statusEffects)
            {
                if (effect == null || string.IsNullOrWhiteSpace(effect.effectName))
                {
                    continue;
                }

                if (result.Length > 0)
                {
                    result += ", ";
                }

                result += $"{effect.effectName}({effect.durationTurns})";
            }

            return result.Length == 0 ? "-" : result;
        }

        private RPGTable.Core.ItemCard FindItemCard(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return null;

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

