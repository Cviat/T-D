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

            // Create Floating Graph Window
            GameObject graphWindowGo = CreatePanel("CampaignGraphWindow", canvasGo.transform);
            RectTransform graphWindowRt = graphWindowGo.GetComponent<RectTransform>();
            graphWindowRt.anchorMin = new Vector2(0.5f, 0.5f);
            graphWindowRt.anchorMax = new Vector2(0.5f, 0.5f);
            graphWindowRt.pivot = new Vector2(0.5f, 0.5f);
            graphWindowRt.sizeDelta = new Vector2(400f, 300f);
            graphWindowRt.anchoredPosition = new Vector2(100f, 150f);
            graphWindowGo.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.08f, 0.95f);

            // Title Bar
            GameObject titleBarGo = CreatePanel("TitleBar", graphWindowGo.transform);
            RectTransform titleBarRt = titleBarGo.GetComponent<RectTransform>();
            titleBarRt.anchorMin = new Vector2(0, 1);
            titleBarRt.anchorMax = new Vector2(1, 1);
            titleBarRt.pivot = new Vector2(0.5f, 1);
            titleBarRt.sizeDelta = new Vector2(0, 40f);
            titleBarRt.anchoredPosition = Vector2.zero;
            titleBarGo.AddComponent<Image>().color = new Color(0.18f, 0.12f, 0.065f, 1f);

            GameObject titleTextGo = new GameObject("Text");
            titleTextGo.transform.SetParent(titleBarGo.transform, false);
            RectTransform titleTextRt = titleTextGo.AddComponent<RectTransform>();
            titleTextRt.anchorMin = Vector2.zero;
            titleTextRt.anchorMax = Vector2.one;
            titleTextRt.offsetMin = new Vector2(12f, 0f);
            titleTextRt.offsetMax = new Vector2(-50f, 0f);
            Text titleText = titleTextGo.AddComponent<Text>();
            titleText.text = "Карта кампании";
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 16;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.color = Color.white;

            // Minimize Button
            GameObject minBtnGo = new GameObject("MinimizeBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            minBtnGo.transform.SetParent(titleBarGo.transform, false);
            RectTransform minBtnRt = minBtnGo.GetComponent<RectTransform>();
            minBtnRt.anchorMin = new Vector2(1, 0.5f);
            minBtnRt.anchorMax = new Vector2(1, 0.5f);
            minBtnRt.pivot = new Vector2(1, 0.5f);
            minBtnRt.sizeDelta = new Vector2(28f, 28f);
            minBtnRt.anchoredPosition = new Vector2(-6f, 0f);
            minBtnGo.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.5f);
            Button minBtn = minBtnGo.GetComponent<Button>();

            GameObject minBtnTextGo = new GameObject("Text");
            minBtnTextGo.transform.SetParent(minBtnGo.transform, false);
            RectTransform minBtnTextRt = minBtnTextGo.AddComponent<RectTransform>();
            minBtnTextRt.anchorMin = Vector2.zero;
            minBtnTextRt.anchorMax = Vector2.one;
            minBtnTextRt.sizeDelta = Vector2.zero;
            Text minBtnText = minBtnTextGo.AddComponent<Text>();
            minBtnText.text = "[-]";
            minBtnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            minBtnText.fontSize = 14;
            minBtnText.alignment = TextAnchor.MiddleCenter;
            minBtnText.color = Color.white;

            // Content Parent (collapsible area)
            GameObject contentParentGo = CreatePanel("ContentParent", graphWindowGo.transform);
            RectTransform contentParentRt = contentParentGo.GetComponent<RectTransform>();
            contentParentRt.anchorMin = Vector2.zero;
            contentParentRt.anchorMax = Vector2.one;
            contentParentRt.offsetMin = Vector2.zero;
            contentParentRt.offsetMax = new Vector2(0f, -40f);

            // Scroll View
            GameObject graphScrollViewGo = new GameObject("Scroll View", typeof(RectTransform), typeof(ScrollRect));
            graphScrollViewGo.transform.SetParent(contentParentGo.transform, false);
            RectTransform graphScrollRt = graphScrollViewGo.GetComponent<RectTransform>();
            graphScrollRt.anchorMin = Vector2.zero;
            graphScrollRt.anchorMax = Vector2.one;
            graphScrollRt.offsetMin = new Vector2(4f, 4f);
            graphScrollRt.offsetMax = new Vector2(-4f, -4f);
            ScrollRect graphScrollRect = graphScrollViewGo.GetComponent<ScrollRect>();
            graphScrollRect.horizontal = true;
            graphScrollRect.vertical = true;
            graphScrollRect.movementType = ScrollRect.MovementType.Clamped;

            // Viewport
            GameObject graphViewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
            graphViewportGo.transform.SetParent(graphScrollViewGo.transform, false);
            RectTransform graphViewportRt = graphViewportGo.GetComponent<RectTransform>();
            graphViewportRt.anchorMin = Vector2.zero;
            graphViewportRt.anchorMax = Vector2.one;
            graphViewportRt.sizeDelta = Vector2.zero;
            var vpImg = graphViewportGo.GetComponent<Image>();
            vpImg.color = new Color(0f, 0f, 0f, 0f);
            vpImg.raycastTarget = true;

            // Content
            GameObject graphContentGo = new GameObject("GraphContent", typeof(RectTransform));
            graphContentGo.transform.SetParent(graphViewportGo.transform, false);
            RectTransform graphContentRt = graphContentGo.GetComponent<RectTransform>();
            graphContentRt.anchorMin = new Vector2(0f, 1f);
            graphContentRt.anchorMax = new Vector2(0f, 1f);
            graphContentRt.pivot = new Vector2(0f, 1f);
            graphContentRt.sizeDelta = new Vector2(1000f, 1000f);

            graphScrollRect.viewport = graphViewportRt;
            graphScrollRect.content = graphContentRt;

            // Resize Handle
            GameObject resizeHandleGo = CreatePanel("ResizeHandle", graphWindowGo.transform);
            RectTransform resizeHandleRt = resizeHandleGo.GetComponent<RectTransform>();
            resizeHandleRt.anchorMin = new Vector2(1, 0);
            resizeHandleRt.anchorMax = new Vector2(1, 0);
            resizeHandleRt.pivot = new Vector2(1, 0);
            resizeHandleRt.sizeDelta = new Vector2(16f, 16f);
            resizeHandleRt.anchoredPosition = Vector2.zero;
            resizeHandleGo.AddComponent<Image>().color = new Color(0.4f, 0.4f, 0.4f, 0.6f);

            // Add Components
            UIFloatingWindow uiFloating = graphWindowGo.AddComponent<UIFloatingWindow>();
            var serializedFloat = new UnityEditor.SerializedObject(uiFloating);
            serializedFloat.FindProperty("windowRect").objectReferenceValue = graphWindowRt;
            serializedFloat.FindProperty("titleBar").objectReferenceValue = titleBarRt;
            serializedFloat.FindProperty("resizeHandle").objectReferenceValue = resizeHandleRt;
            serializedFloat.FindProperty("contentParent").objectReferenceValue = contentParentGo;
            serializedFloat.FindProperty("minimizeButton").objectReferenceValue = minBtn;
            serializedFloat.FindProperty("minimizeButtonText").objectReferenceValue = minBtnText;
            serializedFloat.ApplyModifiedPropertiesWithoutUndo();

            CampaignGraphWindow graphWindow = graphWindowGo.AddComponent<CampaignGraphWindow>();
            var serializedGraph = new UnityEditor.SerializedObject(graphWindow);
            serializedGraph.FindProperty("contentContainer").objectReferenceValue = graphContentRt;
            serializedGraph.FindProperty("scrollRect").objectReferenceValue = graphScrollRect;
            serializedGraph.ApplyModifiedPropertiesWithoutUndo();

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

            // Load sprite for toggle buttons
            Sprite toggleBtnSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GUI_Parts/Gui_parts/button_ready_on.png");

            // Toggle Maps Button
            GameObject toggleMapsGo = new GameObject("ToggleMapsBtn");
            toggleMapsGo.transform.SetParent(bottomPanel.transform, false);
            var toggleMapsRt = toggleMapsGo.AddComponent<RectTransform>();
            var toggleMapsLayout = toggleMapsGo.AddComponent<LayoutElement>();
            toggleMapsLayout.preferredWidth = 140f;
            toggleMapsLayout.preferredHeight = 60f;
            var toggleMapsImg = toggleMapsGo.AddComponent<Image>();
            if (toggleBtnSprite != null)
            {
                toggleMapsImg.sprite = toggleBtnSprite;
                toggleMapsImg.type = Image.Type.Simple;
            }
            else
            {
                toggleMapsImg.color = new Color(0.25f, 0.2f, 0.15f, 1f);
            }
            var mapsBtn = toggleMapsGo.AddComponent<Button>();

            GameObject mapsBtnTextGo = new GameObject("Text");
            mapsBtnTextGo.transform.SetParent(toggleMapsGo.transform, false);
            RectTransform mapsBtnTextRt = mapsBtnTextGo.AddComponent<RectTransform>();
            mapsBtnTextRt.anchorMin = Vector2.zero;
            mapsBtnTextRt.anchorMax = Vector2.one;
            mapsBtnTextRt.sizeDelta = Vector2.zero;
            Text mapsBtnText = mapsBtnTextGo.AddComponent<Text>();
            mapsBtnText.text = "Карта";
            mapsBtnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            mapsBtnText.fontSize = 18;
            mapsBtnText.alignment = TextAnchor.MiddleCenter;
            mapsBtnText.color = Color.white;

            // Toggle Inspector Button
            GameObject toggleInspectorGo = new GameObject("ToggleInspectorBtn");
            toggleInspectorGo.transform.SetParent(bottomPanel.transform, false);
            var toggleInspectorRt = toggleInspectorGo.AddComponent<RectTransform>();
            var toggleInspectorLayout = toggleInspectorGo.AddComponent<LayoutElement>();
            toggleInspectorLayout.preferredWidth = 140f;
            toggleInspectorLayout.preferredHeight = 60f;
            var toggleInspectorImg = toggleInspectorGo.AddComponent<Image>();
            if (toggleBtnSprite != null)
            {
                toggleInspectorImg.sprite = toggleBtnSprite;
                toggleInspectorImg.type = Image.Type.Simple;
            }
            else
            {
                toggleInspectorImg.color = new Color(0.25f, 0.2f, 0.15f, 1f);
            }
            var inspectorBtn = toggleInspectorGo.AddComponent<Button>();

            GameObject inspectorBtnTextGo = new GameObject("Text");
            inspectorBtnTextGo.transform.SetParent(toggleInspectorGo.transform, false);
            RectTransform inspectorBtnTextRt = inspectorBtnTextGo.AddComponent<RectTransform>();
            inspectorBtnTextRt.anchorMin = Vector2.zero;
            inspectorBtnTextRt.anchorMax = Vector2.one;
            inspectorBtnTextRt.sizeDelta = Vector2.zero;
            Text inspectorBtnText = inspectorBtnTextGo.AddComponent<Text>();
            inspectorBtnText.text = "Инспектор";
            inspectorBtnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            inspectorBtnText.fontSize = 18;
            inspectorBtnText.alignment = TextAnchor.MiddleCenter;
            inspectorBtnText.color = Color.white;

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
            so.FindProperty("topMapsRoot").objectReferenceValue = null;
            so.FindProperty("campaignGraphWindow").objectReferenceValue = graphWindow;
            so.FindProperty("leftPanelRoot").objectReferenceValue = listRoot;
            so.FindProperty("bottomToolsetRoot").objectReferenceValue = bottomRt;
            so.FindProperty("activeTabBtn").objectReferenceValue = activeBtn;
            so.FindProperty("bankTabBtn").objectReferenceValue = bankBtn;
            so.FindProperty("tabTitleLabel").objectReferenceValue = titleTxt;
            so.FindProperty("toggleMapsBtn").objectReferenceValue = mapsBtn;
            so.FindProperty("toggleInspectorBtn").objectReferenceValue = inspectorBtn;
            
            // Assign Prefabs
            GameObject tokenCardPrefab = Resources.Load<GameObject>("Prefabs/TokenCard");
            GameObject mapCardPrefab = Resources.Load<GameObject>("Prefabs/MapCard");
            
            so.FindProperty("tokenCardPrefab").objectReferenceValue = tokenCardPrefab;
            so.FindProperty("tokenBankItemPrefab").objectReferenceValue = tokenCardPrefab; // User requested to use the same prefab
            so.FindProperty("mapCardPrefab").objectReferenceValue = mapCardPrefab;
            
            so.ApplyModifiedProperties();

            var soGraph = new SerializedObject(graphWindow);
            soGraph.FindProperty("nodePrefab").objectReferenceValue = mapCardPrefab;
            soGraph.ApplyModifiedProperties();

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
