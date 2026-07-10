using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RPGTable.CharacterEditor
{
    public sealed class ItemDropSlot : MonoBehaviour, IDropHandler, IPointerClickHandler
    {
        public InputField inputField;
        public Image slotIcon; // Bind icon display
        public RPGTable.Core.ItemType slotType = RPGTable.Core.ItemType.General;

        private void Awake()
        {
            if (inputField == null)
            {
                inputField = GetComponent<InputField>();
            }
            if (inputField != null)
            {
                inputField.onValueChanged.AddListener((val) => UpdateIcon(val));
            }
        }

        private void Start()
        {
            if (inputField != null)
            {
                UpdateIcon(inputField.text);
            }
        }

        public void SetItem(string name)
        {
            if (inputField != null)
            {
                inputField.text = name;
                inputField.onEndEdit?.Invoke(name);
            }
            UpdateIcon(name);
        }

        private void UpdateIcon(string itemName)
        {
            if (slotIcon == null) return;

            if (string.IsNullOrWhiteSpace(itemName))
            {
                slotIcon.sprite = null;
                slotIcon.color = Color.clear;
                return;
            }

            var card = FindItemCard(itemName);
            if (card != null && card.icon != null)
            {
                slotIcon.sprite = card.icon;
                slotIcon.color = Color.white;
            }
            else
            {
                slotIcon.sprite = null;
                slotIcon.color = Color.clear;
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            var dragItem = eventData.pointerDrag?.GetComponent<ItemDragItem>();
            if (dragItem != null)
            {
                var card = FindItemCard(dragItem.itemName);
                if (slotType == RPGTable.Core.ItemType.General || (card != null && card.itemType == slotType))
                {
                    SetItem(dragItem.itemName);
                }
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (inputField != null)
            {
                var controller = Object.FindAnyObjectByType<CharacterEditorController>();
                Sprite dialFrame = controller != null ? controller.DialogFrameSprite : null;
                Sprite dialBg = controller != null ? controller.DialogBgSprite : null;

                ItemSelectDialog.Show(slotType, dialFrame, dialBg, (selectedName) =>
                {
                    SetItem(selectedName);
                });
            }
        }

        private RPGTable.Core.ItemCard FindItemCard(string title)
        {
            if (string.IsNullOrEmpty(title)) return null;
            var cards = Resources.LoadAll<RPGTable.Core.ItemCard>("ItemCards");
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
