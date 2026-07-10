#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

namespace RPGTable.Editor
{
    public static class RPGTablePrefabCreator
    {
        [MenuItem("RPG Table/Create Health and Armor Prefabs")]
        public static void CreatePrefabs()
        {
            var folderPath = "Assets/RPGTable/Resources/Prefabs";
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // Load sprites
            var hpFrameSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GUI_Parts/Gui_parts/Hp_frame.png");
            var hpFillSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GUI_Parts/Gui_parts/Hp_line.png");
            var armorFrameSprite = hpFrameSprite; // Use same frame for armor
            var armorFillSprite = hpFillSprite;   // Use same fill for armor

            CreateBarPrefab("HealthBar", hpFrameSprite, hpFillSprite, new Color(0.9f, 0.12f, 0.12f, 1f), folderPath);
            CreateBarPrefab("ArmorBar", armorFrameSprite, armorFillSprite, new Color(0.7f, 0.72f, 0.78f, 1f), folderPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Health and Armor bar prefabs successfully created.");
        }

        private static void CreateBarPrefab(string name, Sprite frameSprite, Sprite fillSprite, Color fillColor, string folder)
        {
            var path = $"{folder}/{name}.prefab";

            // Create root
            var root = new GameObject(name);
            var rootRt = root.AddComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0.5f, 0.5f);
            rootRt.anchorMax = new Vector2(0.5f, 0.5f);
            rootRt.pivot = new Vector2(0.5f, 0.5f);
            rootRt.sizeDelta = new Vector2(1f, 0.2f);

            // Create frame (background)
            var frameObj = new GameObject("Frame", typeof(Image), typeof(RectTransform));
            var frameImg = frameObj.GetComponent<Image>();
            frameImg.sprite = frameSprite;
            frameImg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            frameImg.type = Image.Type.Sliced;
            frameImg.fillCenter = true;

            var frameRt = frameObj.GetComponent<RectTransform>();
            frameRt.anchorMin = Vector2.zero;
            frameRt.anchorMax = Vector2.one;
            frameRt.sizeDelta = Vector2.zero;

            // Create fill (progress bar)
            var fillObj = new GameObject("Fill", typeof(Image), typeof(RectTransform));
            var fillImg = fillObj.GetComponent<Image>();
            fillImg.sprite = fillSprite;
            fillImg.color = fillColor;
            fillImg.type = Image.Type.Sliced;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = 0; // Left to right
            fillImg.fillAmount = 1f;

            var fillRt = fillObj.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.sizeDelta = Vector2.zero;

            // Save as prefab
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }
    }
}
#endif
