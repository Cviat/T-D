using System;
using System.Collections.Generic;
using RPGTable.Core;
using UnityEngine;

namespace RPGTable.Runtime
{
    public class CampaignUIManager : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private RectTransform topMapsRoot;
        [SerializeField] private RectTransform leftPanelRoot;
        [SerializeField] private RectTransform bottomToolsetRoot;
        [SerializeField] private RectTransform rightScenarioRoot;
        [SerializeField] private RectTransform rightInspectorRoot;
        [SerializeField] private RectTransform initiativeScrollContent;

        [Header("Prefabs")]
        [SerializeField] private GameObject tokenCardPrefab;
        [SerializeField] private GameObject mapCardPrefab;
        [SerializeField] private GameObject tokenBankItemPrefab;
        [SerializeField] private GameObject initiativeItemPrefab;
        [SerializeField] private GameObject inspectorContentPrefab;

        public RectTransform TopMapsRoot => topMapsRoot;
        public RectTransform LeftPanelRoot => leftPanelRoot;
        public RectTransform BottomToolsetRoot => bottomToolsetRoot;
        public RectTransform RightScenarioRoot => rightScenarioRoot;
        public RectTransform RightInspectorRoot => rightInspectorRoot;
        public RectTransform InitiativeScrollContent => initiativeScrollContent;

        public GameObject TokenCardPrefab => tokenCardPrefab;
        public GameObject MapCardPrefab => mapCardPrefab;
        public GameObject TokenBankItemPrefab => tokenBankItemPrefab;
        public GameObject InitiativeItemPrefab => initiativeItemPrefab;
        public GameObject InspectorContentPrefab => inspectorContentPrefab;

        [Header("Left Panel UI")]
        [SerializeField] private UnityEngine.UI.Button activeTabBtn;
        [SerializeField] private UnityEngine.UI.Button bankTabBtn;
        [SerializeField] private UnityEngine.UI.Text tabTitleLabel;

        public UnityEngine.UI.Button ActiveTabBtn => activeTabBtn;
        public UnityEngine.UI.Button BankTabBtn => bankTabBtn;
        public UnityEngine.UI.Text TabTitleLabel => tabTitleLabel;

        // Future references to specific views will be added here
        
        public void Initialize(
            Action onPromptConfirm,
            Action onPromptCancel,
            Action onTogglePVCamera,
            Action<string> onBankTokenSelected)
        {
            // Connect to prefab components if necessary
            // E.g., find GMBottomToolsView and initialize it
            var bottomToolsView = bottomToolsetRoot.GetComponentInChildren<GMBottomToolsView>();
            if (bottomToolsView != null)
            {
                bottomToolsView.Initialize(onTogglePVCamera, () => Debug.Log("Draw Tool"), () => Debug.Log("Measure Tool"));
            }
        }
    }
}
