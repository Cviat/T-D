using System;
using UnityEngine;
using UnityEngine.UI;

namespace RPGTable.Runtime
{
    public sealed class MapCardView : MonoBehaviour
    {
        [Header("UI Reference Links")]
        public Image previewImage;
        public Text nameText;
        public Button selectButton;

        public void Setup(string displayName, Sprite previewSprite, bool isCurrent, Action onClick)
        {
            if (nameText != null)
            {
                nameText.text = displayName;
            }

            if (previewImage != null)
            {
                if (previewSprite != null)
                {
                    previewImage.sprite = previewSprite;
                    previewImage.color = Color.white;
                }
                else
                {
                    previewImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
                }
            }

            if (selectButton != null)
            {
                selectButton.onClick.RemoveAllListeners();
                selectButton.onClick.AddListener(() => onClick?.Invoke());
            }

            var cardImage = GetComponent<Image>();
            if (cardImage != null)
            {
                cardImage.color = isCurrent 
                    ? new Color(0.35f, 0.25f, 0.15f, 0.95f) 
                    : new Color(0.22f, 0.22f, 0.22f, 0.95f);
            }
        }
    }
}
