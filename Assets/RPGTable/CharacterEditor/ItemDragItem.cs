using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RPGTable.CharacterEditor
{
    public sealed class ItemDragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public string itemName;
        private GameObject dragGhost;

        public void Initialize(string name)
        {
            itemName = name;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            dragGhost = new GameObject("Item Drag Ghost", typeof(RectTransform));
            dragGhost.transform.SetParent(transform.root, false);
            var rect = dragGhost.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(120f, 40f);
            
            var image = dragGhost.AddComponent<Image>();
            image.color = new Color(0.55f, 0.45f, 0.18f, 0.85f); // Nice golden item drag ghost
            image.raycastTarget = false;

            var textObj = new GameObject("Label", typeof(RectTransform));
            textObj.transform.SetParent(dragGhost.transform, false);
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textObj.AddComponent<Text>();
            text.text = itemName;
            text.alignment = TextAnchor.MiddleCenter;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontStyle = FontStyle.Bold;
            text.fontSize = 14;
            text.color = Color.white;
            text.raycastTarget = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (dragGhost != null)
            {
                ((RectTransform)dragGhost.transform).position = eventData.position;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (dragGhost != null)
            {
                Destroy(dragGhost);
            }

            var pointerEventData = new PointerEventData(EventSystem.current) { position = eventData.position };
            var results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerEventData, results);

            foreach (var result in results)
            {
                var slot = result.gameObject.GetComponent<ItemDropSlot>();
                if (slot == null)
                {
                    slot = result.gameObject.GetComponentInParent<ItemDropSlot>();
                }
                if (slot != null && slot.inputField != null)
                {
                    slot.inputField.text = itemName;
                    break;
                }
            }
        }
    }
}
