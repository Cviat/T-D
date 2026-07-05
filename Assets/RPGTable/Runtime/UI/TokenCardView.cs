using System;
using UnityEngine;
using UnityEngine.UI;

namespace RPGTable.Runtime
{
    public sealed class TokenCardView : MonoBehaviour
    {
        [Header("UI Reference Links")]
        public Image portraitImage;
        public Text nameText;
        public RectTransform hpBarFill;
        public Text hpText;
        public Button selectButton;

        private Color defaultColor;
        private bool colorInitialized;

        private void Awake()
        {
            var cardImage = GetComponent<Image>();
            if (cardImage != null)
            {
                defaultColor = cardImage.color;
                colorInitialized = true;
            }
        }

        public void Setup(string displayName, Sprite portraitSprite, int currentHp, int maxHp, bool isDead, Action onClick)
        {
            if (nameText != null)
            {
                nameText.text = displayName;
            }

            if (portraitImage != null && portraitSprite != null)
            {
                portraitImage.sprite = portraitSprite;
                portraitImage.color = Color.white;
            }
            else if (portraitImage != null)
            {
                portraitImage.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);
            }

            if (hpText != null)
            {
                hpText.text = $"{currentHp}/{maxHp}";
            }

            if (hpBarFill != null)
            {
                float ratio = maxHp <= 0 ? 0f : Mathf.Clamp01((float)currentHp / maxHp);
                hpBarFill.anchorMax = new Vector2(ratio, 1f);
            }

            if (selectButton != null)
            {
                selectButton.onClick.RemoveAllListeners();
                selectButton.onClick.AddListener(() => onClick?.Invoke());
            }

            // dead overlay visual handling
            var cardImage = GetComponent<Image>();
            if (cardImage != null)
            {
                if (!colorInitialized)
                {
                    defaultColor = cardImage.color;
                    colorInitialized = true;
                }
                cardImage.color = isDead 
                    ? new Color(0.35f, 0.15f, 0.15f, 0.95f) 
                    : defaultColor;
            }
        }
    }
}
