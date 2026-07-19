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

            // В локальном пространстве фишки (которая масштабируется родителем до footprint):
            // Верхняя граница фишки всегда находится по Y = 0.5f.
            // Мы хотим расположить бар прямо над фишкой с небольшим зазором.
            // Чтобы зазор был постоянным в мировых координатах, делим его на footprint.
            float localY = 0.6f + (0.05f / footprint);

            if (barsRect != null)
            {
                barsView.transform.localPosition = new Vector3(0f, localY, -0.1f / footprint);
                
                // Чтобы ширина бара всегда составляла ровно 70% от ширины фишки в мировых координатах,
                // локальный масштаб не должен зависеть от footprint (так как родитель уже масштабирует до footprint).
                float scale = 0.90f / 120f;
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
