using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using RPGTable.Runtime;

namespace RPGTable.Editor
{
    public static class CampaignUIBuilder
    {
        [MenuItem("RPG Table/Generate Campaign UI")]
        public static void GenerateCampaignUI()
        {
            var existing = GameObject.Find("CampaignCanvas");
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing);
            }

            // Create Canvas
            GameObject canvasGo = new GameObject("CampaignCanvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasGo.AddComponent<GraphicRaycaster>();

            // Create Top Panel (Map Tabs)
            GameObject topPanel = CreatePanel("TopPanel_Maps", canvasGo.transform);
            RectTransform topRt = topPanel.GetComponent<RectTransform>();
            topRt.anchorMin = new Vector2(0, 1);
            topRt.anchorMax = new Vector2(1, 1);
            topRt.pivot = new Vector2(0.5f, 1);
            topRt.sizeDelta = new Vector2(0, 80); // 80 height, stretches width
            topRt.anchoredPosition = Vector2.zero;
            
            HorizontalLayoutGroup topLayout = topPanel.AddComponent<HorizontalLayoutGroup>();
            topLayout.childAlignment = TextAnchor.MiddleLeft;
            topLayout.childControlHeight = true;
            topLayout.childControlWidth = false;
            topLayout.childForceExpandHeight = true;
            topLayout.childForceExpandWidth = false;
            topLayout.spacing = 10;
            topLayout.padding = new RectOffset(10, 10, 10, 10);
            topPanel.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

            // Create Left Panel (Tokens)
            GameObject leftPanel = CreatePanel("LeftPanel_Tokens", canvasGo.transform);
            RectTransform leftRt = leftPanel.GetComponent<RectTransform>();
            leftRt.anchorMin = new Vector2(0, 0);
            leftRt.anchorMax = new Vector2(0, 1);
            leftRt.pivot = new Vector2(0, 0.5f);
            leftRt.offsetMin = new Vector2(0, 100);
            leftRt.offsetMax = new Vector2(300, -80);
            
            // Sidebar
            GameObject sidebar = CreatePanel("Sidebar", leftPanel.transform);
            RectTransform sideRt = sidebar.GetComponent<RectTransform>();
            sideRt.anchorMin = new Vector2(0, 0);
            sideRt.anchorMax = new Vector2(0, 1);
            sideRt.pivot = new Vector2(0, 0.5f);
            sideRt.sizeDelta = new Vector2(50, 0);
            sideRt.anchoredPosition = Vector2.zero;
            sidebar.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.08f, 0.9f);
            
            VerticalLayoutGroup sideLayout = sidebar.AddComponent<VerticalLayoutGroup>();
            sideLayout.spacing = 10;
            sideLayout.padding = new RectOffset(4, 4, 20, 20);
            sideLayout.childControlHeight = false;
            sideLayout.childControlWidth = true;
            sideLayout.childForceExpandHeight = false;
            sideLayout.childForceExpandWidth = true;
            
            GameObject activeBtnGo = new GameObject("ActiveTabBtn");
            activeBtnGo.transform.SetParent(sidebar.transform, false);
            activeBtnGo.AddComponent<RectTransform>();
            var activeLayoutElement = activeBtnGo.AddComponent<LayoutElement>();
            activeLayoutElement.preferredHeight = 40f;
            activeBtnGo.AddComponent<Image>().color = new Color(0.24f, 0.14f, 0.045f, 1f);
            var activeBtn = activeBtnGo.AddComponent<Button>();
            
            GameObject bankBtnGo = new GameObject("BankTabBtn");
            bankBtnGo.transform.SetParent(sidebar.transform, false);
            bankBtnGo.AddComponent<RectTransform>();
            var bankLayoutElement = bankBtnGo.AddComponent<LayoutElement>();
            bankLayoutElement.preferredHeight = 40f;
            bankBtnGo.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.12f, 1f);
            var bankBtn = bankBtnGo.AddComponent<Button>();

            // Content Area
            GameObject contentArea = CreatePanel("ContentArea", leftPanel.transform);
            RectTransform contentRt = contentArea.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 0);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.offsetMin = new Vector2(55, 0);
            contentRt.offsetMax = new Vector2(-5, 0);
            contentArea.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 0.8f);

            GameObject titleGo = new GameObject("Tab Title");
            titleGo.transform.SetParent(contentArea.transform, false);
            RectTransform titleRt = titleGo.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0, 1);
            titleRt.anchorMax = new Vector2(1, 1);
            titleRt.offsetMin = new Vector2(8, -40);
            titleRt.offsetMax = new Vector2(-8, -5);
            Text titleTxt = titleGo.AddComponent<Text>();
            titleTxt.text = "ИГРОКИ";
            titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleTxt.fontSize = 16;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleLeft;
            titleTxt.color = Color.white;
            
            // Create Scroll View
            GameObject scrollViewGo = new GameObject("Scroll View", typeof(RectTransform));
            scrollViewGo.transform.SetParent(contentArea.transform, false);
            RectTransform scrollRt = scrollViewGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0, 0);
            scrollRt.anchorMax = new Vector2(1, 1);
            scrollRt.offsetMin = new Vector2(5, 5);
            scrollRt.offsetMax = new Vector2(-5, -45);

            ScrollRect scrollRect = scrollViewGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            // Create Viewport
            GameObject viewportGo = new GameObject("Viewport", typeof(RectTransform));
            viewportGo.transform.SetParent(scrollViewGo.transform, false);
            RectTransform viewportRt = viewportGo.GetComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.sizeDelta = Vector2.zero;
            viewportGo.AddComponent<RectMask2D>();

            // Create Content
            GameObject listRootGo = new GameObject("List Content", typeof(RectTransform));
            listRootGo.transform.SetParent(viewportGo.transform, false);
            RectTransform listRoot = listRootGo.GetComponent<RectTransform>();
            listRoot.anchorMin = new Vector2(0, 1);
            listRoot.anchorMax = new Vector2(1, 1);
            listRoot.pivot = new Vector2(0.5f, 1);
            listRoot.sizeDelta = new Vector2(0, 0);

            // Connect ScrollRect
            scrollRect.viewport = viewportRt;
            scrollRect.content = listRoot;

            VerticalLayoutGroup leftLayout = listRootGo.AddComponent<VerticalLayoutGroup>();
            leftLayout.childAlignment = TextAnchor.UpperCenter;
            leftLayout.childControlHeight = false;
            leftLayout.childControlWidth = true;
            leftLayout.childForceExpandHeight = false;
            leftLayout.childForceExpandWidth = true;
            leftLayout.spacing = 10;
            leftLayout.padding = new RectOffset(5, 5, 5, 5);

            ContentSizeFitter fitter = listRootGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Create Bottom Panel (Tools/Camera)
            GameObject bottomPanel = CreatePanel("BottomPanel_Tools", canvasGo.transform);
            RectTransform bottomRt = bottomPanel.GetComponent<RectTransform>();
            bottomRt.anchorMin = new Vector2(0, 0);
            bottomRt.anchorMax = new Vector2(1, 0);
            bottomRt.pivot = new Vector2(0.5f, 0);
            bottomRt.sizeDelta = new Vector2(0, 100); // 100 height, stretches width
            bottomRt.anchoredPosition = Vector2.zero;

            HorizontalLayoutGroup bottomLayout = bottomPanel.AddComponent<HorizontalLayoutGroup>();
            bottomLayout.childAlignment = TextAnchor.MiddleCenter;
            bottomLayout.childControlHeight = true;
            bottomLayout.childControlWidth = false;
            bottomLayout.childForceExpandHeight = true;
            bottomLayout.childForceExpandWidth = false;
            bottomLayout.spacing = 20;
            bottomLayout.padding = new RectOffset(10, 10, 10, 10);
            bottomPanel.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

            // Add GMBottomToolsView and the Button
            var bottomToolsView = bottomPanel.AddComponent<GMBottomToolsView>();

            GameObject camBtnGo = new GameObject("PlayerCameraBtn");
            camBtnGo.transform.SetParent(bottomPanel.transform, false);
            var camBtnRt = camBtnGo.AddComponent<RectTransform>();
            var camBtnLayout = camBtnGo.AddComponent<LayoutElement>();
            camBtnLayout.preferredWidth = 200f;
            camBtnLayout.preferredHeight = 60f;
            camBtnGo.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 1f);
            var camBtn = camBtnGo.AddComponent<Button>();

            GameObject camBtnTextGo = new GameObject("Text");
            camBtnTextGo.transform.SetParent(camBtnGo.transform, false);
            RectTransform camBtnTextRt = camBtnTextGo.AddComponent<RectTransform>();
            camBtnTextRt.anchorMin = Vector2.zero;
            camBtnTextRt.anchorMax = Vector2.one;
            camBtnTextRt.sizeDelta = Vector2.zero;
            Text camBtnText = camBtnTextGo.AddComponent<Text>();
            camBtnText.text = "Камера игрока";
            camBtnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            camBtnText.fontSize = 18;
            camBtnText.alignment = TextAnchor.MiddleCenter;
            camBtnText.color = Color.white;

            bottomToolsView.playerViewCameraButton = camBtn;

            // Create EventSystem if it doesn't exist
            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // Attach Manager
            CampaignUIManager manager = canvasGo.AddComponent<CampaignUIManager>();
            var so = new SerializedObject(manager);
            so.FindProperty("topMapsRoot").objectReferenceValue = topRt;
            so.FindProperty("leftPanelRoot").objectReferenceValue = listRoot;
            so.FindProperty("bottomToolsetRoot").objectReferenceValue = bottomRt;
            so.FindProperty("activeTabBtn").objectReferenceValue = activeBtn;
            so.FindProperty("bankTabBtn").objectReferenceValue = bankBtn;
            so.FindProperty("tabTitleLabel").objectReferenceValue = titleTxt;
            
            // Assign Prefabs
            GameObject tokenCardPrefab = Resources.Load<GameObject>("Prefabs/TokenCard");
            GameObject mapCardPrefab = Resources.Load<GameObject>("Prefabs/MapCard");
            
            so.FindProperty("tokenCardPrefab").objectReferenceValue = tokenCardPrefab;
            so.FindProperty("tokenBankItemPrefab").objectReferenceValue = tokenCardPrefab; // User requested to use the same prefab
            so.FindProperty("mapCardPrefab").objectReferenceValue = mapCardPrefab;
            
            so.ApplyModifiedProperties();

            Undo.RegisterCreatedObjectUndo(canvasGo, "Create Campaign UI");
            Selection.activeGameObject = canvasGo;
        }

        private static GameObject CreatePanel(string name, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }
    }
}
