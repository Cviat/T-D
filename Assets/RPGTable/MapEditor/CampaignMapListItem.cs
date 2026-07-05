using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RPGTable.MapEditor
{
    public sealed class CampaignMapListItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        private CampaignEditorController controller;
        private string mapPath;
        private GameObject dragGhost;

        public void Initialize(CampaignEditorController campaignController, string path)
        {
            controller = campaignController;
            mapPath = path;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                controller.DropMapOnBoard(mapPath, eventData);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            dragGhost = new GameObject("Map Drag Ghost", typeof(RectTransform));
            dragGhost.transform.SetParent(transform.root, false);
            var rect = dragGhost.GetComponent<RectTransform>();
            rect.sizeDelta = ((RectTransform)transform).sizeDelta;
            var image = dragGhost.AddComponent<Image>();
            image.color = new Color(0.18f, 0.12f, 0.065f, 0.65f);
            image.raycastTarget = false;
            var text = new GameObject("Label", typeof(RectTransform)).AddComponent<Text>();
            text.transform.SetParent(dragGhost.transform, false);
            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            text.text = UserMapStore.GetDisplayName(mapPath);
            text.alignment = TextAnchor.MiddleCenter;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontStyle = FontStyle.Bold;
            text.fontSize = 18;
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

            controller.DropMapOnBoard(mapPath, eventData);
        }
    }
}
