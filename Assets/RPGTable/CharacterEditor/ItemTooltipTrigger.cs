using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using RPGTable.Core;

namespace RPGTable.CharacterEditor
{
    public sealed class ItemTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public string itemName;
        public InputField boundInputField;
        public AbilityDropSlot boundAbilitySlot;

        public void OnPointerEnter(PointerEventData eventData)
        {
            var targetName = itemName;
            if (boundInputField != null)
            {
                targetName = boundInputField.text;
            }
            else if (boundAbilitySlot != null)
            {
                targetName = boundAbilitySlot.abilityName;
            }

            if (string.IsNullOrWhiteSpace(targetName)) return;

            // Search for matching item card first
            string info = null;
            var item = FindItemCard(targetName);
            if (item != null)
            {
                info = GetItemInfoString(item);
            }
            else
            {
                // Fallback to ability card search
                var ability = FindAbilityCard(targetName);
                if (ability != null)
                {
                    info = GetAbilityInfoString(ability);
                }
                else
                {
                    // Generic fallback
                    info = $"<b>{targetName}</b>";
                }
            }

            if (ItemTooltip.Instance != null && !string.IsNullOrEmpty(info))
            {
                ItemTooltip.Instance.Show(info);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (ItemTooltip.Instance != null)
            {
                ItemTooltip.Instance.Hide();
            }
        }

        private void OnDisable()
        {
            if (ItemTooltip.Instance != null)
            {
                ItemTooltip.Instance.Hide();
            }
        }

        private string GetItemInfoString(ItemCard card)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"<b><color=#FFD700>{card.title}</color></b>");
            sb.AppendLine($"<size=11><color=#AAAAAA>{GetSlotTypeName(card.itemType)}</color></size>");
            sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(card.description))
            {
                sb.AppendLine($"<i>{card.description}</i>");
                sb.AppendLine();
            }

            if (card.armorPoints > 0)
            {
                sb.AppendLine($"Броня: <b>+{card.armorPoints}</b>");
            }

            if (card.bonusHp > 0) sb.AppendLine($"Здоровье: <b>+{card.bonusHp}</b>");
            if (card.bonusStr > 0) sb.AppendLine($"Сила: <b>+{card.bonusStr}</b>");
            if (card.bonusAgi > 0) sb.AppendLine($"Ловкость: <b>+{card.bonusAgi}</b>");
            if (card.bonusInt > 0) sb.AppendLine($"Интеллект: <b>+{card.bonusInt}</b>");
            if (card.bonusHol > 0) sb.AppendLine($"Святость: <b>+{card.bonusHol}</b>");

            if (card.itemType == ItemType.Weapon)
            {
                if (card.scaleStat1 != "None" && card.coef1 > 0f)
                {
                    sb.AppendLine($"Скейлинг 1: <b>{card.scaleStat1}</b> (x{card.coef1:F1})");
                }
                if (card.scaleStat2 != "None" && card.coef2 > 0f)
                {
                    sb.AppendLine($"Скейлинг 2: <b>{card.scaleStat2}</b> (x{card.coef2:F1})");
                }

                // Show multiple combat attributes if configured, fallback to string attribute
                if (card.attributes != null && card.attributes.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("<color=#FF8C00>Свойства оружия:</color>");
                    foreach (var attr in card.attributes)
                    {
                        if (attr != null)
                        {
                            sb.AppendLine($"• <b>{attr.attributeName}</b> (сила/длит: {attr.value})");
                            if (!string.IsNullOrWhiteSpace(attr.description))
                            {
                                sb.AppendLine($"  <size=10><color=#CCCCCC>{attr.description}</color></size>");
                            }
                        }
                    }
                }
                else if (!string.IsNullOrWhiteSpace(card.attribute))
                {
                    sb.AppendLine($"Свойство: <b>{card.attribute}</b>");
                }
            }

            return sb.ToString().TrimEnd();
        }

        private string GetAbilityInfoString(AbilityCard card)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"<b><color=#FFD700>{card.title}</color></b>");
            sb.AppendLine($"<size=11><color=#AAAAAA>Прием ({GetEffectTypeName(card.effectType)})</color></size>");
            sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(card.description))
            {
                sb.AppendLine($"<i>{card.description}</i>");
                sb.AppendLine();
            }

            sb.AppendLine($"Стоимость: <b>{card.cost} SP</b>");
            sb.AppendLine($"Дальность: <b>{card.range}</b>");
            sb.AppendLine($"Цель: <b>{GetTargetTypeName(card.targetType)}</b>");
            sb.AppendLine($"Множитель: <b>x{card.multiplier:F1}</b>");

            if (card.attributes != null && card.attributes.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("<color=#FF8C00>Эффекты приёма:</color>");
                foreach (var attr in card.attributes)
                {
                    if (attr != null)
                    {
                        sb.AppendLine($"• <b>{attr.attributeName}</b> (сила/длит: {attr.value})");
                        if (!string.IsNullOrWhiteSpace(attr.description))
                        {
                            sb.AppendLine($"  <size=10><color=#CCCCCC>{attr.description}</color></size>");
                        }
                    }
                }
            }

            return sb.ToString().TrimEnd();
        }

        private string GetSlotTypeName(ItemType type)
        {
            switch (type)
            {
                case ItemType.Helmet: return "Шлем";
                case ItemType.Armor: return "Доспех";
                case ItemType.Weapon: return "Оружие";
                case ItemType.Shield: return "Щит";
                case ItemType.Boots: return "Обувь";
                case ItemType.Amulet: return "Амулет";
                case ItemType.Ring: return "Кольцо";
                case ItemType.Artifact: return "Артефакт";
                case ItemType.Belt: return "Пояс";
                default: return "Общий предмет";
            }
        }

        private string GetEffectTypeName(AbilityEffectType type)
        {
            switch (type)
            {
                case AbilityEffectType.Damage: return "Урон";
                case AbilityEffectType.Heal: return "Лечение";
                case AbilityEffectType.Move: return "Перемещение";
                case AbilityEffectType.Status: return "Эффект состояния";
                case AbilityEffectType.Reveal: return "Раскрытие";
                default: return type.ToString();
            }
        }

        private string GetTargetTypeName(AbilityTargetType type)
        {
            switch (type)
            {
                case AbilityTargetType.Self: return "Себя";
                case AbilityTargetType.Ally: return "Союзник";
                case AbilityTargetType.Enemy: return "Враг";
                case AbilityTargetType.Area: return "Область";
                case AbilityTargetType.Object: return "Объект";
                default: return type.ToString();
            }
        }

        private ItemCard FindItemCard(string title)
        {
            if (string.IsNullOrEmpty(title)) return null;
            var cards = Resources.LoadAll<ItemCard>("ItemCards");
            foreach (var card in cards)
            {
                if (card != null && string.Equals(card.title, title, System.StringComparison.OrdinalIgnoreCase))
                {
                    return card;
                }
            }
            return null;
        }

        private AbilityCard FindAbilityCard(string title)
        {
            if (string.IsNullOrEmpty(title)) return null;
            var cards = Resources.LoadAll<AbilityCard>("AbilityCards");
            foreach (var card in cards)
            {
                if (card != null && string.Equals(card.title, title, System.StringComparison.OrdinalIgnoreCase))
                {
                    return card;
                }
            }
            return null;
        }
    }
}
