using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RPGTable.MapEditor
{
    public sealed class MapEditorPaletteItem : MonoBehaviour, IPointerClickHandler
    {
        private string path;
        private Sprite sprite;
        private MapEditorElementSpawner spawner;
        private MapEditorElementPalette palette;

        public void Initialize(
            string imagePath,
            Sprite itemSprite,
            MapEditorElementSpawner elementSpawner,
            MapEditorElementPalette elementPalette)
        {
            path = imagePath;
            sprite = itemSprite;
            spawner = elementSpawner;
            palette = elementPalette;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                spawner.BeginDrag(path, sprite);
                return;
            }

            if (eventData.button != PointerEventData.InputButton.Right)
            {
                return;
            }

            MapEditorDeletePopup.Show(eventData.position, "Удалить", DeleteImportedImage);
        }

        private void DeleteImportedImage()
        {
            if (UserElementAssetStore.DeleteImage(path))
            {
                palette.Reload();
            }
        }
    }
}
