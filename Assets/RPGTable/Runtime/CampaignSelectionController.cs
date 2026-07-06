using RPGTable.CharacterEditor;
using RPGTable.MapEditor;
using RPGTable.TokenEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RPGTable.Runtime
{
    public sealed class CampaignSelectionController : MonoBehaviour
    {
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private string gameSceneName = "CampaignGame";
        [SerializeField] private string tokenEditorSceneName = "TokenEditor";
        [SerializeField] private string characterEditorSceneName = "CharacterEditor";
        [SerializeField] private RectTransform campaignListRoot;
        [SerializeField] private RectTransform playerCardsRoot;
        [SerializeField] private Image campaignCover;
        [SerializeField] private Text campaignTitle;
        [SerializeField] private Text campaignDescription;
        [SerializeField] private Text warningText;

        private string selectedCampaignPath;
        private SavedCampaignData selectedCampaign;

        public void Initialize(
            RectTransform listRoot,
            RectTransform playersRoot,
            Image cover,
            Text title,
            Text description,
            Text warning)
        {
            campaignListRoot = listRoot;
            playerCardsRoot = playersRoot;
            campaignCover = cover;
            campaignTitle = title;
            campaignDescription = description;
            warningText = warning;
        }

        private void Start()
        {
            ReloadCampaigns();
            RefreshPlayerCards();
            RefreshSelectedCampaign();
            SetWarning(null);
            CampaignGameSession.OnPlayersChanged += RefreshPlayerCards;
        }

        private void OnDestroy()
        {
            CampaignGameSession.OnPlayersChanged -= RefreshPlayerCards;
        }

        public void BackToMainMenu()
        {
            if (Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
            {
                SceneManager.LoadScene(mainMenuSceneName);
            }
        }

        public void AddDefaultPlayer()
        {
            CharacterEditorDialog.ShowOpenCharacter(UserCharacterStore.GetCharacterPaths(), AddCharacterPlayer, OpenCharacterEditor);
        }

        public void StartSelectedCampaign()
        {
            if (string.IsNullOrWhiteSpace(selectedCampaignPath) || selectedCampaign == null)
            {
                return;
            }

            if (CampaignGameSession.HasPlayersWithoutTokens())
            {
                SetWarning("Назначьте фишки всем игрокам перед стартом.");
                RefreshPlayerCards();
                return;
            }

            CampaignGameSession.SelectedCampaignPath = selectedCampaignPath;
            CampaignGameSession.ResetRuntimePositions();

            if (Networking.WebServerManager.Instance != null)
            {
                Networking.WebServerManager.Instance.GameStarted = true;
            }

            if (Application.CanStreamedLevelBeLoaded(gameSceneName))
            {
                SceneManager.LoadScene(gameSceneName);
            }
        }

        private void ReloadCampaigns()
        {
            if (campaignListRoot == null)
            {
                return;
            }

            for (var i = campaignListRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(campaignListRoot.GetChild(i).gameObject);
            }

            var paths = UserCampaignStore.GetCampaignPaths();

            foreach (var path in paths)
            {
                CreateCampaignListButton(path);
            }

            if (paths.Count > 0)
            {
                SelectCampaign(paths[0]);
            }
        }

        private void SelectCampaign(string path)
        {
            selectedCampaignPath = path;
            selectedCampaign = UserCampaignStore.LoadCampaign(path);
            RefreshSelectedCampaign();
            SetWarning(null);
        }

        private void RefreshSelectedCampaign()
        {
            if (selectedCampaign == null)
            {
                if (campaignTitle != null)
                {
                    campaignTitle.text = "Кампании не найдены";
                }

                if (campaignDescription != null)
                {
                    campaignDescription.text = "Создайте кампанию в редакторе кампаний.";
                }

                if (campaignCover != null)
                {
                    campaignCover.sprite = null;
                    campaignCover.color = new Color(0.15f, 0.14f, 0.12f, 1f);
                }

                return;
            }

            if (campaignTitle != null)
            {
                campaignTitle.text = selectedCampaign.name;
            }

            if (campaignDescription != null)
            {
                campaignDescription.text = string.IsNullOrWhiteSpace(selectedCampaign.description)
                    ? "Описание кампании пока не добавлено."
                    : selectedCampaign.description;
            }

            if (campaignCover != null)
            {
                var sprite = UserCampaignStore.LoadCoverSprite(selectedCampaign.coverImagePath);
                campaignCover.sprite = sprite;
                campaignCover.color = sprite == null ? new Color(0.15f, 0.14f, 0.12f, 1f) : Color.white;
                campaignCover.preserveAspect = true;
            }
        }

        private void RefreshPlayerCards()
        {
            if (playerCardsRoot == null)
            {
                return;
            }

            for (var i = playerCardsRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(playerCardsRoot.GetChild(i).gameObject);
            }

            foreach (var player in CampaignGameSession.CurrentPlayers)
            {
                CreatePlayerCard(player);
            }
        }

        private void CreatePlayerCard(CampaignPlayerData player)
        {
            var hasToken = !string.IsNullOrWhiteSpace(player.tokenPath);
            var card = new GameObject(player.name, typeof(RectTransform));
            card.transform.SetParent(playerCardsRoot, false);
            var rect = card.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(150f, 150f);

            var background = card.AddComponent<Image>();
            background.color = hasToken
                ? new Color(0.08f, 0.07f, 0.06f, 0.96f)
                : new Color(0.08f, 0.08f, 0.08f, 0.72f);

            var button = card.AddComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(() => OpenTokenEditorForPlayer(player.id));

            var avatarObject = new GameObject("Avatar", typeof(RectTransform));
            avatarObject.transform.SetParent(card.transform, false);
            var avatarRect = avatarObject.GetComponent<RectTransform>();
            avatarRect.anchorMin = new Vector2(0.5f, 1f);
            avatarRect.anchorMax = new Vector2(0.5f, 1f);
            avatarRect.pivot = new Vector2(0.5f, 1f);
            avatarRect.anchoredPosition = new Vector2(0f, -10f);
            avatarRect.sizeDelta = new Vector2(104f, 92f);
            var avatar = avatarObject.AddComponent<Image>();
            avatar.sprite = LoadPlayerSprite(player);
            avatar.color = avatar.sprite == null
                ? new Color(0.2f, 0.18f, 0.15f, hasToken ? 1f : 0.45f)
                : new Color(1f, 1f, 1f, hasToken ? 1f : 0.45f);
            avatar.preserveAspect = true;
            avatar.raycastTarget = false;

            var nameObject = new GameObject("Name", typeof(RectTransform));
            nameObject.transform.SetParent(card.transform, false);
            var nameRect = nameObject.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 0f);
            nameRect.anchorMax = new Vector2(1f, 0f);
            nameRect.offsetMin = new Vector2(8f, 8f);
            nameRect.offsetMax = new Vector2(-8f, 42f);
            var text = nameObject.AddComponent<Text>();
            text.text = player.name;
            text.alignment = TextAnchor.MiddleCenter;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontStyle = FontStyle.Bold;
            text.fontSize = 18;
            text.color = hasToken ? Color.white : new Color(0.58f, 0.58f, 0.58f, 1f);
            text.raycastTarget = false;
        }

        private void OpenTokenEditorForPlayer(string playerId)
        {
            CampaignGameSession.PendingTokenPlayerId = playerId;
            CampaignGameSession.TokenEditorReturnSceneName = gameObject.scene.name;

            if (Application.CanStreamedLevelBeLoaded(tokenEditorSceneName))
            {
                SceneManager.LoadScene(tokenEditorSceneName);
            }
        }

        private void OpenCharacterEditor()
        {
            if (Application.CanStreamedLevelBeLoaded(characterEditorSceneName))
            {
                SceneManager.LoadScene(characterEditorSceneName);
            }
        }

        private static Sprite LoadPlayerSprite(CampaignPlayerData player)
        {
            var characterSprite = UserCharacterStore.LoadSprite(player.portraitPath);

            if (characterSprite != null)
            {
                return characterSprite;
            }

            if (!string.IsNullOrWhiteSpace(player.tokenPath))
            {
                var token = UserTokenStore.LoadToken(player.tokenPath);
                var tokenSprite = UserTokenStore.LoadSprite(token?.portraitPath);

                if (tokenSprite != null)
                {
                    return tokenSprite;
                }
            }

            return Resources.Load<Sprite>(player.avatarResourceName);
        }

        private void AddCharacterPlayer(string characterPath)
        {
            var character = UserCharacterStore.LoadCharacter(characterPath);

            if (character == null)
            {
                return;
            }

            CampaignGameSession.AddCharacterPlayer(characterPath, character.name, character.portraitPath, character.tokenPath);
            SetWarning(null);
            RefreshPlayerCards();
        }

        private void CreateCampaignListButton(string path)
        {
            var buttonObject = new GameObject(UserCampaignStore.GetDisplayName(path), typeof(RectTransform));
            buttonObject.transform.SetParent(campaignListRoot, false);
            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.13f, 0.09f, 0.05f, 0.96f);
            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => SelectCampaign(path));

            var textObject = new GameObject("Label", typeof(RectTransform));
            textObject.transform.SetParent(buttonObject.transform, false);
            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(16f, 0f);
            rect.offsetMax = new Vector2(-16f, 0f);
            var text = textObject.AddComponent<Text>();
            text.text = UserCampaignStore.GetDisplayName(path);
            text.alignment = TextAnchor.MiddleLeft;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontStyle = FontStyle.Bold;
            text.fontSize = 20;
            text.color = Color.white;
            text.raycastTarget = false;

            buttonObject.AddComponent<LayoutElement>().preferredHeight = 58f;
        }

        private void SetWarning(string message)
        {
            if (warningText == null)
            {
                return;
            }

            warningText.text = message ?? string.Empty;
            warningText.gameObject.SetActive(!string.IsNullOrWhiteSpace(message));
        }
    }
}
