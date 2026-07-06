using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RPGTable.CharacterEditor
{
    public sealed class AbilityDropSlot : MonoBehaviour, IDropHandler, IPointerClickHandler
    {
        public string abilityName;
        public Text labelText;
        public Image slotImage; // Optional icon display
        private static Sprite defaultIconSprite;

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
            if (labelText != null)
            {
                labelText.text = string.IsNullOrWhiteSpace(name) ? "Пусто" : name;
            }
            
            // Optional: update icon/image if we load ability icons
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
                    slotImage.color = new Color(0.12f, 0.105f, 0.085f, 1f); // default slot bg
                }
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            var dragItem = eventData.pointerDrag?.GetComponent<AbilityDragItem>();
            if (dragItem != null)
            {
                SetAbility(dragItem.abilityName);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // Clear on left click/tap
            SetAbility(string.Empty);
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
