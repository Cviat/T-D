using UnityEngine;
using UnityEngine.UI;

namespace RPGTable.MapEditor
{
    public sealed class CampaignLinkView : MonoBehaviour
    {
        private CampaignExitPin fromPin;
        private CampaignExitPin toPin;
        private RectTransform rectTransform;

        public void Initialize(CampaignExitPin from, CampaignExitPin to)
        {
            fromPin = from;
            toPin = to;
            rectTransform = GetComponent<RectTransform>();
            Refresh();
        }

        public bool Matches(CampaignExitPin first, CampaignExitPin second)
        {
            return (fromPin == first && toPin == second) || (fromPin == second && toPin == first);
        }

        public bool Involves(CampaignMapNode node)
        {
            return fromPin != null && fromPin.Owner == node || toPin != null && toPin.Owner == node;
        }

        public SavedCampaignLinkData ToData()
        {
            return new SavedCampaignLinkData
            {
                fromMapId = fromPin.Owner.Id,
                fromExitId = fromPin.ExitId,
                toMapId = toPin.Owner.Id,
                toExitId = toPin.ExitId
            };
        }

        public void Refresh()
        {
            if (fromPin == null || toPin == null || rectTransform == null)
            {
                return;
            }

            var parent = rectTransform.parent as RectTransform;
            var start = ParentLocalPosition(parent, fromPin.RectTransform);
            var end = ParentLocalPosition(parent, toPin.RectTransform);
            var delta = end - start;
            rectTransform.anchoredPosition = start + delta * 0.5f;
            rectTransform.sizeDelta = new Vector2(delta.magnitude, 4f);
            rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        private static Vector2 ParentLocalPosition(RectTransform parent, RectTransform child)
        {
            var world = child.TransformPoint(child.rect.center);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent,
                RectTransformUtility.WorldToScreenPoint(null, world),
                null,
                out var local);
            return local;
        }
    }
}
