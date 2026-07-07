using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RPGTable.CharacterEditor
{
    public sealed class AbilityDropSlot : MonoBehaviour, IDropHandler, IPointerClickHandler
    {
        public string abilityName;
        public Text labelText;
        public Image slotImage; // Centered icon image

        [Tooltip("The D6 face value (1-6) to show as a faint placeholder when empty.")]
        public string diceFaceNumber = "";

        [Tooltip("The AttackType filter for this D6 slot.")]
        public RPGTable.Core.AttackType allowedType = RPGTable.Core.AttackType.Melee;

        private void Awake()
        {
            if (labelText == null)
            {
                labelText = GetComponentInChildren<Text>();
            }
        }

        public void SetAbility(string name)
        {
            abilityName = name;
            
            if (string.IsNullOrWhiteSpace(name))
            {
                // Empty state: show faint dice face number (1-6)
                if (labelText != null)
                {
                    labelText.text = diceFaceNumber;
                    labelText.color = new Color(1f, 1f, 1f, 0.15f); // bld-faint placeholder
                    labelText.fontSize = 24;
                }

                if (slotImage != null)
                {
                    slotImage.sprite = null;
                    slotImage.color = Color.clear;
                }
            }
            else
            {
                // Equipped state: hide placeholder text, show skill icon
                if (labelText != null)
                {
                    labelText.color = Color.clear;
                }

                if (slotImage != null)
                {
                    var card = FindAbilityCard(name);
                    if (card != null && card.icon != null)
                    {
                        slotImage.sprite = card.icon;
                        slotImage.color = Color.white;
                    }
                    else
                    {
                        slotImage.sprite = null;
                        slotImage.color = Color.clear;
                    }
                }
            }
        }

        public bool TryDropAbility(string name)
        {
            var card = FindAbilityCard(name);
            if (card != null && card.attackType != allowedType)
            {
                Debug.LogWarning($"Cannot drop ability {name} ({card.attackType}) into slot expecting {allowedType}");
                return false; // Reject!
            }

            SetAbility(name);
            
            // Trigger event end edit or save
            var input = GetComponent<InputField>();
            if (input != null)
            {
                input.text = name;
                input.onEndEdit?.Invoke(name);
            }
            return true;
        }

        public void OnDrop(PointerEventData eventData)
        {
            var dragItem = eventData.pointerDrag?.GetComponent<AbilityDragItem>();
            if (dragItem != null)
            {
                TryDropAbility(dragItem.abilityName);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            SetAbility(string.Empty);
            
            var input = GetComponent<InputField>();
            if (input != null)
            {
                input.text = string.Empty;
                input.onEndEdit?.Invoke(string.Empty);
            }
        }

        private RPGTable.Core.AbilityCard FindAbilityCard(string title)
        {
            if (string.IsNullOrEmpty(title)) return null;
            var cards = Resources.LoadAll<RPGTable.Core.AbilityCard>("AbilityCards");
            foreach (var card in cards)
            {
                if (card != null && card.title == title)
                {
                    return card;
                }
            }
            return null;
        }
    }
}
