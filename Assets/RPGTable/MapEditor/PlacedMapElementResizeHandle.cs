using UnityEngine;

namespace RPGTable.MapEditor
{
    public sealed class PlacedMapElementResizeHandle : MonoBehaviour
    {
        private PlacedMapElement owner;
        private int handleIndex;

        public void Initialize(PlacedMapElement placedMapElement, int resizeHandleIndex)
        {
            owner = placedMapElement;
            handleIndex = resizeHandleIndex;
        }

        private void OnMouseDown()
        {
            owner?.BeginResize(handleIndex);
        }

        private void OnMouseDrag()
        {
            owner?.ResizeToMouse();
        }

        private void OnMouseUp()
        {
            owner?.EndResize();
        }
    }
}
