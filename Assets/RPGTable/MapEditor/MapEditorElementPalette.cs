using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace RPGTable.MapEditor
{
    public sealed class MapEditorElementPalette : MonoBehaviour
    {
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private MapEditorElementSpawner spawner;

        public void ImportImage()
        {
            var importedPath = UserElementAssetStore.ImportImageWithDialog();

            if (!string.IsNullOrWhiteSpace(importedPath))
            {
                Reload();
            }
        }

        public void Reload()
        {
            Clear();

            foreach (var path in UserElementAssetStore.GetImagePaths())
            {
                var sprite = UserElementAssetStore.LoadSprite(path);

                if (sprite != null)
                {
                    CreateItem(path, sprite);
                }
            }
        }

        private void Start()
        {
            Reload();
        }

        private void CreateItem(string path, Sprite sprite)
        {
            var item = new GameObject(Path.GetFileNameWithoutExtension(path), typeof(RectTransform));
            item.transform.SetParent(contentRoot, false);

            var rect = item.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(96f, 96f);

            var image = item.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.color = Color.white;

            var button = item.AddComponent<Button>();
            button.onClick.AddListener(() => spawner.Select(sprite));
        }

        private void Clear()
        {
            for (var i = contentRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(contentRoot.GetChild(i).gameObject);
            }
        }
    }
}
