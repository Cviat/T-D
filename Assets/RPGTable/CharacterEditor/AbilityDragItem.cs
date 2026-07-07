using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RPGTable.CharacterEditor
{
    public sealed class AbilityDragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public string abilityName;
        private GameObject dragGhost;

        public void Initialize(string name)
        {
            abilityName = name;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            var myImg = GetComponent<Image>();
            var myIcon = transform.Find("Icon")?.GetComponent<Image>();

            dragGhost = new GameObject("Ability Drag Ghost", typeof(RectTransform));
            dragGhost.transform.SetParent(transform.root, false);
            var rect = dragGhost.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(64f, 64f);
            
            var image = dragGhost.AddComponent<Image>();
            if (myImg != null)
            {
                image.sprite = myImg.sprite;
                image.color = new Color(myImg.color.r, myImg.color.g, myImg.color.b, 0.75f);
            }
            image.raycastTarget = false;

            if (myIcon != null && myIcon.sprite != null)
            {
                var iconObj = new GameObject("Icon", typeof(RectTransform));
                iconObj.transform.SetParent(dragGhost.transform, false);
                var iconRect = iconObj.GetComponent<RectTransform>();
                iconRect.anchorMin = Vector2.zero;
                iconRect.anchorMax = Vector2.one;
                iconRect.offsetMin = new Vector2(6f, 6f);
                iconRect.offsetMax = new Vector2(-6f, -6f);
                var iconImg = iconObj.AddComponent<Image>();
                iconImg.sprite = myIcon.sprite;
                iconImg.color = new Color(myIcon.color.r, myIcon.color.g, myIcon.color.b, 0.85f);
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;
            }
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
                var slot = result.gameObject.GetComponent<AbilityDropSlot>();
                if (slot != null)
                {
                    slot.TryDropAbility(abilityName);
                    break;
                }
            }
        }
    }
}
