using UnityEngine;
using UnityEngine.EventSystems;

namespace RPGTable.MapEditor
{
    public sealed class CampaignBoardPanZoom : MonoBehaviour, IDragHandler, IScrollHandler
    {
        [SerializeField] private RectTransform content;
        [SerializeField] private float zoomSpeed = 0.08f;
        [SerializeField] private float minScale = 0.35f;
        [SerializeField] private float maxScale = 2.5f;

        public void Initialize(RectTransform boardContent)
        {
            content = boardContent;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (content != null)
            {
                content.anchoredPosition += eventData.delta;
            }
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (content == null)
            {
                return;
            }

            var next = Mathf.Clamp(content.localScale.x + eventData.scrollDelta.y * zoomSpeed, minScale, maxScale);
            content.localScale = Vector3.one * next;
        }
    }
}
