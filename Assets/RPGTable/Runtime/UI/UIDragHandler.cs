using UnityEngine;
using UnityEngine.EventSystems;

namespace RPGTable.Runtime
{
    public class UIDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        private RectTransform rectTransform;
        private Canvas canvas;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvas = GetComponentInParent<Canvas>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            // Bring to front
            rectTransform.SetAsLastSibling();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (canvas == null || rectTransform == null) return;
            
            // Adjust position based on drag delta scaled by canvas scale factor
            rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
        }
    }
}
