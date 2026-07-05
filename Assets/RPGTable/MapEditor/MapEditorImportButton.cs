using UnityEngine;
using UnityEngine.UI;

namespace RPGTable.MapEditor
{
    [RequireComponent(typeof(Button))]
    public sealed class MapEditorImportButton : MonoBehaviour
    {
        [SerializeField] private MapEditorElementPalette palette;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(Import);
        }

        private void Import()
        {
            if (palette == null)
            {
                Debug.LogWarning("Import button has no element palette.");
                return;
            }

            palette.ImportImage();
        }
    }
}
