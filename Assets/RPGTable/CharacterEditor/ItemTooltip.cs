using UnityEngine;
using UnityEngine.UI;

namespace RPGTable.CharacterEditor
{
    public sealed class ItemTooltip : MonoBehaviour
    {
        public static ItemTooltip Instance { get; private set; }

        private Text tooltipText;
        private Image background;
        private RectTransform rectTransform;

        private void Awake()
        {
            Instance = this;
            rectTransform = GetComponent<RectTransform>();
            tooltipText = GetComponentInChildren<Text>();
            background = GetComponent<Image>();

            // Ensure tooltip is always drawn on top of all canvases (such as the dialog popup)
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 2000;

            // Load and apply the styled frame background
            if (background != null)
            {
                background.sprite = FindFrameSprite();
                background.color = new Color(0.065f, 0.058f, 0.048f, 0.99f); // Solid dark brown matching editor theme
            }

            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (gameObject.activeSelf)
            {
                Vector3 mousePos = UnityEngine.Input.mousePosition;
                
                float xOffset = 15f;
                float yOffset = -15f;

                if (mousePos.x + 250f > Screen.width)
                {
                    xOffset = -255f;
                }
                if (mousePos.y - 150f < 0)
                {
                    yOffset = 15f;
                }

                rectTransform.position = mousePos + new Vector3(xOffset, yOffset, 0f);
            }
        }

        public void Show(string textContent)
        {
            if (string.IsNullOrWhiteSpace(textContent)) return;
            gameObject.SetActive(true);
            
            if (tooltipText != null)
            {
                tooltipText.text = textContent;
                
                Canvas.ForceUpdateCanvases();
                var textHeight = tooltipText.preferredHeight;
                rectTransform.sizeDelta = new Vector2(240f, textHeight + 24f); // Slightly extra padding
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private Sprite FindFrameSprite()
        {
            var images = UnityEngine.Object.FindObjectsByType<Image>(FindObjectsInactive.Include);
            foreach (var i in images)
            {
                if (i.gameObject.name == "Portrait Frame" && i.sprite != null) return i.sprite;
            }
            return null;
        }
    }
}
