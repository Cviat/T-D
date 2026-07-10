using UnityEngine;
using RPGTable.Runtime.UI;

namespace RPGTable.Runtime
{
    public sealed class TokenHealthArmorBars : MonoBehaviour
    {
        private CampaignRuntimeToken token;
        private TokenWorldBarsView barsView;
        private RectTransform barsRect;

        private void Start()
        {
            token = GetComponent<CampaignRuntimeToken>();
            var prefab = Resources.Load<TokenWorldBarsView>("Prefabs/TokenWorldBars");
            if (prefab == null)
            {
                Debug.LogWarning("TokenWorldBars prefab is missing. Run RPG Table/Create UI Prefabs.");
                enabled = false;
                return;
            }

            barsView = Instantiate(prefab, transform, false);
            SetLayerRecursively(barsView.gameObject, gameObject.layer);
            barsRect = barsView.GetComponent<RectTransform>();
            UpdateBarsPositionAndScale();
            UpdateBarFills();
        }

        private void Update()
        {
            bool shouldShow = CampaignGameSession.IsCombatActive && !token.IsDead;
            if (barsView != null)
            {
                barsView.gameObject.SetActive(shouldShow);
            }

            if (shouldShow)
            {
                UpdateBarsPositionAndScale();
                UpdateBarFills();
            }
        }

        private void UpdateBarsPositionAndScale()
        {
            var boardToken = GetComponent<RPGTable.Core.BoardToken>();
            int footprint = boardToken != null ? boardToken.footprintSize : Mathf.Max(1, token.FootprintSize);
            float desiredWidth = footprint * 0.9f;
            float baseOffset = footprint * 0.5f;

            if (barsRect != null)
            {
                barsView.transform.localPosition = new Vector3(0f, baseOffset + 0.35f, -0.1f);
                float scale = desiredWidth / 120f;
                barsView.transform.localScale = new Vector3(scale, scale, scale);
            }
        }

        private void UpdateBarFills()
        {
            if (barsView != null)
            {
                barsView.SetValues(token.CurrentHp, token.MaxHp, token.CurrentArmor, token.MaxArmor);
            }
        }

        private static void SetLayerRecursively(GameObject target, int layer)
        {
            if (target == null)
            {
                return;
            }

            target.layer = layer;

            for (var i = 0; i < target.transform.childCount; i++)
            {
                SetLayerRecursively(target.transform.GetChild(i).gameObject, layer);
            }
        }
    }
}
