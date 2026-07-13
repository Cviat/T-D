using UnityEngine;
using UnityEngine.EventSystems;

namespace RPGTable.MapEditor
{
    public sealed class MapEditorCharacterPaletteItem : MonoBehaviour, IPointerClickHandler
    {
        private string characterPath;
        private Sprite portrait;
        private MapEditorElementSpawner spawner;

        public void Initialize(string path, Sprite sprite, MapEditorElementSpawner elementSpawner)
        {
            characterPath = path;
            portrait = sprite;
            spawner = elementSpawner;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                spawner.BeginCharacterDrag(characterPath, portrait);
            }
        }
    }
}
