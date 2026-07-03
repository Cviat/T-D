using UnityEngine;

namespace RPGTable.MapEditor
{
    public sealed class MapExitPointResizeHandle : MonoBehaviour
    {
        private MapExitPoint owner;
        private int handleIndex;

        public void Initialize(MapExitPoint exitPoint, int resizeHandleIndex)
        {
            owner = exitPoint;
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

    public sealed class MapSpawnZoneResizeHandle : MonoBehaviour
    {
        private MapSpawnZone owner;
        private int handleIndex;

        public void Initialize(MapSpawnZone spawnZone, int resizeHandleIndex)
        {
            owner = spawnZone;
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
