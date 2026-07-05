using System.IO;
using RPGTable.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RPGTable.Editor
{
    public static class RPGTablePrefabBuilder
    {
        private const string PrefabFolder = "Assets/RPGTable/Resources/Prefabs";
        private const string TokenCardPath = PrefabFolder + "/TokenCard.prefab";
        private const string MapCardPath = PrefabFolder + "/MapCard.prefab";
        private const string GMBottomToolsPath = PrefabFolder + "/GMBottomTools.prefab";

        private static Sprite defaultUiSprite;

        [MenuItem("RPG Table/Create UI Prefabs")]
        public static void CreatePrefabs()
        {
            Directory.CreateDirectory(PrefabFolder);
            LoadDefaultUiSprite();

            BuildTokenCardPrefab();
            BuildMapCardPrefab();
            BuildGMBottomToolsPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("RPG Table GM UI Prefabs created successfully in: " + PrefabFolder);
        }

        private static void LoadDefaultUiSprite()
        {
            defaultUiSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        }

        private static void BuildTokenCardPrefab()
        {
            var card = CreateUIElement("Token Card", new Vector2(230f, 58f));
            card.AddComponent<Image>().color = new Color(0.16f, 0.16f, 0.16f, 0.95f);
            if (defaultUiSprite != null)
            {
                card.GetComponent<Image>().sprite = defaultUiSprite;
                card.GetComponent<Image>().type = Image.Type.Sliced;
            }
            card.AddComponent<LayoutElement>().preferredHeight = 58f;
            var button = card.AddComponent<Button>();

            // Portrait image
            var portrait = CreateUIElement("Portrait", new Vector2(40f, 40f), card.transform);
            var portRect = portrait.GetComponent<RectTransform>();
            portRect.anchorMin = new Vector2(0f, 0.5f);
            portRect.anchorMax = new Vector2(0f, 0.5f);
            portRect.anchoredPosition = new Vector2(25f, 0f);
            var portImg = portrait.AddComponent<Image>();
            portImg.color = new Color(0f, 0f, 0f, 0.5f);

            // Name Label
            var nameLbl = CreateTextElement("NameLabel", "Token Name", 13, FontStyle.Bold, card.transform);
            var nameRect = nameLbl.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 0.5f);
            nameRect.anchorMax = new Vector2(1f, 0.5f);
            nameRect.offsetMin = new Vector2(56f, 8f);
            nameRect.offsetMax = new Vector2(-10f, 22f);
            nameLbl.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;

            // HP Bar Background
            var hpBarBg = CreateUIElement("HP Bar Bg", new Vector2(0f, 10f), card.transform);
            var hpBgRect = hpBarBg.GetComponent<RectTransform>();
            hpBgRect.anchorMin = new Vector2(0f, 0.5f);
            hpBgRect.anchorMax = new Vector2(1f, 0.5f);
            hpBgRect.offsetMin = new Vector2(56f, -20f);
            hpBgRect.offsetMax = new Vector2(-10f, -8f);
            hpBarBg.AddComponent<Image>().color = new Color(0.25f, 0.25f, 0.25f, 1f);
            if (defaultUiSprite != null)
            {
                hpBarBg.GetComponent<Image>().sprite = defaultUiSprite;
                hpBarBg.GetComponent<Image>().type = Image.Type.Sliced;
            }

            // HP Bar Fill
            var hpBarFill = CreateUIElement("HP Bar Fill", Vector2.zero, hpBarBg.transform);
            var hpFillRect = hpBarFill.GetComponent<RectTransform>();
            hpFillRect.anchorMin = Vector2.zero;
            hpFillRect.anchorMax = new Vector2(1f, 1f);
            hpFillRect.pivot = new Vector2(0f, 0.5f);
            hpFillRect.offsetMin = Vector2.zero;
            hpFillRect.offsetMax = Vector2.zero;
            hpBarFill.AddComponent<Image>().color = new Color(0.12f, 0.7f, 0.12f, 1f);
            if (defaultUiSprite != null)
            {
                hpBarFill.GetComponent<Image>().sprite = defaultUiSprite;
                hpBarFill.GetComponent<Image>().type = Image.Type.Sliced;
            }

            // HP Text
            var hpTxt = CreateTextElement("HP Text", "50/50", 8, FontStyle.Bold, hpBarBg.transform);
            var hpTxtRect = hpTxt.GetComponent<RectTransform>();
            hpTxtRect.anchorMin = Vector2.zero;
            hpTxtRect.anchorMax = Vector2.one;
            hpTxtRect.offsetMin = Vector2.zero;
            hpTxtRect.offsetMax = Vector2.zero;

            // Attach View component & Wire references
            var view = card.AddComponent<TokenCardView>();
            view.portraitImage = portImg;
            view.nameText = nameLbl.GetComponent<Text>();
            view.hpBarFill = hpFillRect;
            view.hpText = hpTxt.GetComponent<Text>();
            view.selectButton = button;

            PrefabUtility.SaveAsPrefabAsset(card, TokenCardPath);
            Object.DestroyImmediate(card);
        }

        private static void BuildMapCardPrefab()
        {
            var card = CreateUIElement("Map Card", new Vector2(110f, 70f));
            card.AddComponent<Image>().color = new Color(0.22f, 0.22f, 0.22f, 0.95f);
            if (defaultUiSprite != null)
            {
                card.GetComponent<Image>().sprite = defaultUiSprite;
                card.GetComponent<Image>().type = Image.Type.Sliced;
            }
            var button = card.AddComponent<Button>();

            // Map Preview
            var preview = CreateUIElement("Map Preview", Vector2.zero, card.transform);
            var prevRect = preview.GetComponent<RectTransform>();
            prevRect.anchorMin = Vector2.zero;
            prevRect.anchorMax = Vector2.one;
            prevRect.offsetMin = new Vector2(4f, 22f);
            prevRect.offsetMax = new Vector2(-4f, -4f);
            var prevImg = preview.AddComponent<Image>();
            prevImg.color = new Color(0f, 0f, 0f, 0.3f);

            // Map Title label at the bottom
            var titleLbl = CreateTextElement("MapTitle", "Location", 11, FontStyle.Bold, card.transform);
            var titleRect = titleLbl.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0f);
            titleRect.anchorMax = new Vector2(1f, 0f);
            titleRect.offsetMin = new Vector2(2f, 2f);
            titleRect.offsetMax = new Vector2(-2f, 20f);

            // Wire view
            var view = card.AddComponent<MapCardView>();
            view.previewImage = prevImg;
            view.nameText = titleLbl.GetComponent<Text>();
            view.selectButton = button;

            PrefabUtility.SaveAsPrefabAsset(card, MapCardPath);
            Object.DestroyImmediate(card);
        }

        private static void BuildGMBottomToolsPrefab()
        {
            var root = CreateUIElement("GMBottomTools", new Vector2(300f, 65f));
            root.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.12f, 0.7f);
            if (defaultUiSprite != null)
            {
                root.GetComponent<Image>().sprite = defaultUiSprite;
                root.GetComponent<Image>().type = Image.Type.Sliced;
            }

            var horizontalList = new GameObject("Toolset Group", typeof(RectTransform));
            horizontalList.transform.SetParent(root.transform, false);
            var listRect = horizontalList.GetComponent<RectTransform>();
            listRect.anchorMin = Vector2.zero;
            listRect.anchorMax = Vector2.one;
            listRect.offsetMin = new Vector2(6f, 6f);
            listRect.offsetMax = new Vector2(-6f, -6f);

            var layout = horizontalList.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = false;

            var pvCamBtn = CreateButtonElement("PV Camera", "Камера игроков", 110f, horizontalList.transform);
            var drawBtn = CreateButtonElement("Draw", "Рисование", 85f, horizontalList.transform);
            var measureBtn = CreateButtonElement("Measure", "Линейка", 85f, horizontalList.transform);

            var view = root.AddComponent<GMBottomToolsView>();
            view.playerViewCameraButton = pvCamBtn.GetComponent<Button>();
            view.drawButton = drawBtn.GetComponent<Button>();
            view.measureButton = measureBtn.GetComponent<Button>();

            PrefabUtility.SaveAsPrefabAsset(root, GMBottomToolsPath);
            Object.DestroyImmediate(root);
        }

        // ── Assembly Helpers ─────────────────────────────────────────────

        private static GameObject CreateUIElement(string name, Vector2 size, Transform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            return go;
        }

        private static GameObject CreateTextElement(string name, string label, int fontSize, FontStyle style, Transform parent)
        {
            var go = CreateUIElement(name, Vector2.zero, parent);
            var text = go.AddComponent<Text>();
            text.text = label;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return go;
        }

        private static GameObject CreateButtonElement(string name, string label, float width, Transform parent)
        {
            var go = CreateUIElement(name, new Vector2(width, 0f), parent);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            if (defaultUiSprite != null)
            {
                img.sprite = defaultUiSprite;
                img.type = Image.Type.Sliced;
            }
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            go.AddComponent<LayoutElement>().preferredWidth = width;

            var text = CreateTextElement("Label", label, 13, FontStyle.Bold, go.transform);
            var tRect = text.GetComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.offsetMin = new Vector2(6f, 3f);
            tRect.offsetMax = new Vector2(-6f, -3f);

            return go;
        }
    }
}
