using UnityEngine;
using UnityEngine.UI;

namespace RPGTable.Runtime
{
    public sealed class TokenHealthArmorBars : MonoBehaviour
    {
        private CampaignRuntimeToken token;
        private GameObject hpBarRoot;
        private GameObject armorBarRoot;

        private Image hpFillImage;
        private Image armorFillImage;

        private void Start()
        {
            token = GetComponent<CampaignRuntimeToken>();
            
            // Create UI bars programmatically
            CreateHealthBar();
            CreateArmorBar();

            UpdateBarsPositionAndScale();
            UpdateBarFills();
        }

        private void CreateHealthBar()
        {
            hpBarRoot = new GameObject("HP Bar", typeof(Image), typeof(RectTransform));
            hpBarRoot.transform.SetParent(transform, false);
            
            var rootRt = hpBarRoot.GetComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0.5f, 0.5f);
            rootRt.anchorMax = new Vector2(0.5f, 0.5f);
            rootRt.pivot = new Vector2(0.5f, 0.5f);
            rootRt.sizeDelta = new Vector2(1f, 0.2f);

            var rootImg = hpBarRoot.GetComponent<Image>();
            rootImg.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);
            rootImg.raycastTarget = false;

            // Fill (progress bar)
            var fillObj = new GameObject("Fill", typeof(Image), typeof(RectTransform));
            fillObj.transform.SetParent(hpBarRoot.transform, false);
            hpFillImage = fillObj.GetComponent<Image>();
            hpFillImage.fillMethod = Image.FillMethod.Horizontal;
            hpFillImage.fillOrigin = 0; // Left to right
            hpFillImage.fillAmount = 1f;
            hpFillImage.color = new Color(0.9f, 0.12f, 0.12f, 1f);
            hpFillImage.raycastTarget = false;

            var fillRt = hpFillImage.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.sizeDelta = Vector2.zero;
        }

        private void CreateArmorBar()
        {
            armorBarRoot = new GameObject("Armor Bar", typeof(Image), typeof(RectTransform));
            armorBarRoot.transform.SetParent(transform, false);
            
            var rootRt = armorBarRoot.GetComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0.5f, 0.5f);
            rootRt.anchorMax = new Vector2(0.5f, 0.5f);
            rootRt.pivot = new Vector2(0.5f, 0.5f);
            rootRt.sizeDelta = new Vector2(1f, 0.2f);

            var rootImg = armorBarRoot.GetComponent<Image>();
            rootImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            rootImg.raycastTarget = false;

            // Fill (progress bar)
            var fillObj = new GameObject("Fill", typeof(Image), typeof(RectTransform));
            fillObj.transform.SetParent(armorBarRoot.transform, false);
            armorFillImage = fillObj.GetComponent<Image>();
            armorFillImage.fillMethod = Image.FillMethod.Horizontal;
            armorFillImage.fillOrigin = 0; // Left to right
            armorFillImage.fillAmount = 1f;
            armorFillImage.color = new Color(0.7f, 0.72f, 0.78f, 1f);
            armorFillImage.raycastTarget = false;

            var fillRt = armorFillImage.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.sizeDelta = Vector2.zero;
        }

        private void Update()
        {
            // Only show bars during combat and when alive
            bool shouldShow = CampaignGameSession.IsCombatActive && !token.IsDead;
            
            if (hpBarRoot != null) hpBarRoot.SetActive(shouldShow);
            if (armorBarRoot != null) armorBarRoot.SetActive(shouldShow && token.MaxArmor > 0);

            if (shouldShow)
            {
                UpdateBarsPositionAndScale();
                UpdateBarFills();
            }
        }

        private void UpdateBarsPositionAndScale()
        {
            var boardToken = GetComponent<RPGTable.Core.BoardToken>();
            float desiredWidth = boardToken != null ? boardToken.footprintSize * 0.9f : 0.9f;
            float desiredHeight = 0.2f;

            // Position Health bar slightly higher and Armor bar slightly lower above the token
            float baseOffset = boardToken != null ? boardToken.footprintSize * 0.5f : 0.5f;

            if (hpBarRoot != null)
            {
                var rootRt = hpBarRoot.GetComponent<RectTransform>();
                rootRt.anchoredPosition = new Vector2(0f, baseOffset + 0.4f);
                rootRt.sizeDelta = new Vector2(desiredWidth, desiredHeight);
            }

            if (armorBarRoot != null)
            {
                var rootRt = armorBarRoot.GetComponent<RectTransform>();
                rootRt.anchoredPosition = new Vector2(0f, baseOffset + 0.2f);
                rootRt.sizeDelta = new Vector2(desiredWidth, desiredHeight);
            }
        }

        private void UpdateBarFills()
        {
            // Health fill
            if (hpFillImage != null && token.MaxHp > 0)
            {
                float pct = Mathf.Clamp01((float)token.CurrentHp / token.MaxHp);
                hpFillImage.fillAmount = pct;
            }

            // Armor fill
            if (armorFillImage != null && token.MaxArmor > 0)
            {
                float pct = Mathf.Clamp01((float)token.CurrentArmor / token.MaxArmor);
                armorFillImage.fillAmount = pct;
            }
        }
    }
}
