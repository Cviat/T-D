using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace RPGTable.Runtime.ActionCards
{
    public class ActionCardVisualManager : MonoBehaviour
    {
        public static ActionCardVisualManager Instance { get; private set; }

        public GameObject cardUIPrefab; // UI card element with title/desc/icon
        public Canvas targetCanvas;     // The screen canvas to overlay

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void ShowCardFlyIn(ActionCard card)
        {
            if (cardUIPrefab == null)
            {
                cardUIPrefab = GenerateCardUIPrefab();
            }

            // Find all Canvas in the scene to play animation on GM screen AND Player View screen
            var canvases = GameObject.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            bool animatedAny = false;

            foreach (var canvas in canvases)
            {
                // Only spawn on screenspace overlay canvases representing active UI interfaces
                if (canvas != null && canvas.isActiveAndEnabled && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    StartCoroutine(AnimateCardOnCanvas(canvas, card));
                    animatedAny = true;
                }
            }

            if (!animatedAny)
            {
                Debug.LogWarning($"[ActionCardVisualManager] No active Canvas found to show card play animation");
            }
        }

        private GameObject GenerateCardUIPrefab()
        {
            // Create root UI panel for the card
            GameObject cardObj = new GameObject("ActionCardUI");
            cardObj.AddComponent<CanvasGroup>();
            RectTransform rect = cardObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(240f, 340f);

            // Add Background Image (Sleek dark card frame)
            Image bg = cardObj.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.12f, 0.16f, 0.95f); // Deep dark purple-blue

            // Outline / Border Effect
            GameObject borderObj = new GameObject("Border");
            borderObj.transform.SetParent(cardObj.transform, false);
            RectTransform borderRect = borderObj.AddComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.sizeDelta = Vector2.zero;
            Image borderImg = borderObj.AddComponent<Image>();
            borderImg.color = new Color(0.85f, 0.65f, 0.2f, 1f); // Sleek gold outline

            // Add padding to border to make it look like a frame outline
            borderRect.offsetMin = new Vector2(4f, 4f);
            borderRect.offsetMax = new Vector2(-4f, -4f);
            // Re-order to put it behind contents but in front of background
            Image borderBg = borderObj.GetComponent<Image>();
            borderBg.color = new Color(0.15f, 0.15f, 0.2f, 1f);

            // Icon Panel
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(cardObj.transform, false);
            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 1f);
            iconRect.anchorMax = new Vector2(0.5f, 1f);
            iconRect.pivot = new Vector2(0.5f, 1f);
            iconRect.anchoredPosition = new Vector3(0f, -25f, 0f);
            iconRect.sizeDelta = new Vector2(100f, 100f);
            Image iconImg = iconObj.AddComponent<Image>();
            iconImg.color = Color.white;

            // Title Text
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(cardObj.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.5f);
            titleRect.anchorMax = new Vector2(1f, 0.5f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = new Vector3(0f, 15f, 0f);
            titleRect.sizeDelta = new Vector2(-20f, 40f);
            Text titleTxt = titleObj.AddComponent<Text>();
            titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleTxt.fontSize = 20;
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.color = new Color(0.95f, 0.8f, 0.3f, 1f); // Gold title text
            titleTxt.fontStyle = FontStyle.Bold;

            // Description Text
            GameObject descObj = new GameObject("Desc");
            descObj.transform.SetParent(cardObj.transform, false);
            RectTransform descRect = descObj.AddComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0f, 0f);
            descRect.anchorMax = new Vector2(1f, 0.5f);
            descRect.pivot = new Vector2(0.5f, 0f);
            descRect.anchoredPosition = new Vector3(0f, 15f, 0f);
            descRect.sizeDelta = new Vector2(-25f, -40f);
            Text descTxt = descObj.AddComponent<Text>();
            descTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            descTxt.fontSize = 13;
            descTxt.alignment = TextAnchor.UpperCenter;
            descTxt.color = new Color(0.9f, 0.9f, 0.95f, 1f); // Soft white desc
            descTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            descTxt.verticalOverflow = VerticalWrapMode.Truncate;

            // Keep it alive in memory but inactive as template prefab
            cardObj.SetActive(false);
            DontDestroyOnLoad(cardObj);
            return cardObj;
        }

        private IEnumerator AnimateCardOnCanvas(Canvas canvas, ActionCard card)
        {
            if (canvas == null) yield break;

            // Spawn card prefab as child of Canvas and enable it
            GameObject cardObj = Instantiate(cardUIPrefab, canvas.transform);
            cardObj.SetActive(true);
            
            // Set fields (title, desc, icon)
            var titleText = cardObj.transform.Find("Title")?.GetComponent<Text>();
            if (titleText != null) titleText.text = card.title;

            var descText = cardObj.transform.Find("Desc")?.GetComponent<Text>();
            if (descText != null) descText.text = card.description;

            var iconImg = cardObj.transform.Find("Icon")?.GetComponent<Image>();
            if (iconImg != null)
            {
                if (card.icon != null)
                {
                    iconImg.sprite = card.icon;
                    iconImg.color = Color.white;
                }
                else
                {
                    // Fallback default icon color / placeholder if sprite missing
                    iconImg.color = new Color(0.4f, 0.4f, 0.8f, 0.8f);
                }
            }

            RectTransform rect = cardObj.GetComponent<RectTransform>();
            CanvasGroup group = cardObj.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = cardObj.AddComponent<CanvasGroup>();
            }

            // Animate scale/position (Fly in from bottom)
            float duration = 1.0f;
            float elapsed = 0f;
            Vector3 startPos = new Vector3(0, -Screen.height, 0);
            Vector3 targetPos = Vector3.zero;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                rect.anchoredPosition = Vector3.Lerp(startPos, targetPos, smoothT);
                rect.localScale = Vector3.Lerp(Vector3.one * 0.2f, Vector3.one * 1.3f, smoothT);
                yield return null;
            }

            yield return new WaitForSeconds(1.8f); // Display at center

            // Fade out
            elapsed = 0f;
            float fadeDuration = 0.5f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                group.alpha = 1f - (elapsed / fadeDuration);
                yield return null;
            }

            Destroy(cardObj);
        }
    }
}
