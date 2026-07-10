using UnityEngine;
using UnityEngine.UI;

namespace RPGTable.Runtime.UI
{
    public sealed class TokenWorldBarsView : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField] private GameObject healthRoot;
        [SerializeField] private RectTransform healthFill;
        [SerializeField] private Text healthText;

        [Header("Armor")]
        [SerializeField] private GameObject armorRoot;
        [SerializeField] private RectTransform armorFill;
        [SerializeField] private Text armorText;

        public void SetValues(int currentHp, int maxHp, int currentArmor, int maxArmor)
        {
            SetBar(healthRoot, healthFill, healthText, currentHp, maxHp);
            SetBar(armorRoot, armorFill, armorText, currentArmor, maxArmor);

            if (armorRoot != null)
            {
                armorRoot.SetActive(maxArmor > 0);
            }
        }

        private static void SetBar(GameObject root, RectTransform fill, Text label, int current, int max)
        {
            if (root != null)
            {
                root.SetActive(max > 0);
            }

            float ratio = max <= 0 ? 0f : Mathf.Clamp01((float)current / max);
            if (fill != null)
            {
                fill.anchorMax = new Vector2(ratio, 1f);
            }

            if (label != null)
            {
                label.text = max <= 0 ? string.Empty : $"{current}/{max}";
            }
        }
    }
}
