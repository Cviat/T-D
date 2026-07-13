using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Net;
using System.Net.Sockets;

namespace RPGTable.UI
{
    public sealed class MainMenuPlayerViewManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f);
        
        private Camera playerViewCamera;
        private Canvas playerViewCanvas;
        private Image qrCodeImage;
        private Transform rosterRoot;

        private void Start()
        {
            InitializePlayerView();
        }

        private void InitializePlayerView()
        {
            if (Display.displays.Length > 1)
            {
                Display.displays[1].Activate();
            }

            var camObject = new GameObject("MainMenu_PlayerViewCamera");
            playerViewCamera = camObject.AddComponent<Camera>();
            playerViewCamera.targetDisplay = 1; // Always target Display 2

            var canvasObject = new GameObject("MainMenu_PlayerViewCanvas", typeof(RectTransform));
            playerViewCanvas = canvasObject.AddComponent<Canvas>();
            playerViewCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            playerViewCanvas.worldCamera = playerViewCamera;
            playerViewCanvas.planeDistance = 5f;
            playerViewCanvas.targetDisplay = 1;
            
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasObject.AddComponent<GraphicRaycaster>();

            // Copy background from main menu
            var bgGo = new GameObject("PlayerView_Background", typeof(RectTransform));
            bgGo.transform.SetParent(canvasObject.transform, false);
            var bgRect = bgGo.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            var bgImage = bgGo.AddComponent<Image>();

            var mainBg = GameObject.Find("Background");
            if (mainBg != null)
            {
                var mainImage = mainBg.GetComponent<Image>();
                if (mainImage != null)
                {
                    bgImage.sprite = mainImage.sprite;
                    bgImage.color = mainImage.color;
                    bgImage.type = mainImage.type;
                    bgImage.preserveAspect = mainImage.preserveAspect;
                }
            }
            else
            {
                bgImage.color = backgroundColor;
            }

            CreateUI(canvasObject.transform);
        }

        private void CreateUI(Transform parent)
        {
            // 1. Title
            var mainTitleGo = new GameObject("MainTitle", typeof(RectTransform));
            mainTitleGo.transform.SetParent(parent, false);
            var mainTitleRect = mainTitleGo.GetComponent<RectTransform>();
            mainTitleRect.anchorMin = new Vector2(0f, 1f);
            mainTitleRect.anchorMax = new Vector2(1f, 1f);
            mainTitleRect.pivot = new Vector2(0.5f, 1f);
            mainTitleRect.anchoredPosition = new Vector2(0f, -40f);
            mainTitleRect.sizeDelta = new Vector2(0f, 80f);
            var mainTitleText = mainTitleGo.AddComponent<Text>();
            mainTitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            mainTitleText.text = "ОЖИДАНИЕ ИГРОКОВ...";
            mainTitleText.alignment = TextAnchor.MiddleCenter;
            mainTitleText.color = Color.white;
            mainTitleText.fontSize = 64;
            mainTitleText.fontStyle = FontStyle.Bold;

            var shadow = mainTitleGo.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            shadow.effectDistance = new Vector2(4f, -4f);

            // 2. Roster Panel (Right side)
            var rosterPanel = new GameObject("Roster_Panel", typeof(RectTransform));
            rosterPanel.transform.SetParent(parent, false);
            var rosterRect = rosterPanel.GetComponent<RectTransform>();
            rosterRect.anchorMin = new Vector2(0.3f, 0f); // Cover 70% of screen from right
            rosterRect.anchorMax = new Vector2(1f, 1f);
            rosterRect.pivot = new Vector2(1f, 1f);
            rosterRect.offsetMin = new Vector2(0f, 60f);
            rosterRect.offsetMax = new Vector2(-60f, -140f); // Top offset for title

            var hlg = rosterPanel.AddComponent<GridLayoutGroup>();
            hlg.cellSize = new Vector2(200f, 200f);
            hlg.spacing = new Vector2(30f, 30f);
            hlg.startCorner = GridLayoutGroup.Corner.UpperRight; // Start from top right
            hlg.startAxis = GridLayoutGroup.Axis.Horizontal;
            hlg.childAlignment = TextAnchor.UpperRight; // Align to top right

            rosterRoot = rosterPanel.transform;

            // 3. QR Code Panel (Left side, middle)
            var qrPanel = new GameObject("QR_Panel", typeof(RectTransform));
            qrPanel.transform.SetParent(parent, false);
            var qrRect = qrPanel.GetComponent<RectTransform>();
            qrRect.anchorMin = new Vector2(0f, 0.5f);
            qrRect.anchorMax = new Vector2(0f, 0.5f);
            qrRect.pivot = new Vector2(0f, 0.5f);
            qrRect.anchoredPosition = new Vector2(60f, 0f); // 60px from left edge
            qrRect.sizeDelta = new Vector2(240f, 280f);

            var qrBg = qrPanel.AddComponent<Image>();
            qrBg.color = new Color(0.1f, 0.1f, 0.12f, 0.95f);

            var qrImgGo = new GameObject("QR_Image", typeof(RectTransform));
            qrImgGo.transform.SetParent(qrPanel.transform, false);
            var qrImgRect = qrImgGo.GetComponent<RectTransform>();
            qrImgRect.anchorMin = new Vector2(0.5f, 1f);
            qrImgRect.anchorMax = new Vector2(0.5f, 1f);
            qrImgRect.pivot = new Vector2(0.5f, 1f);
            qrImgRect.anchoredPosition = new Vector2(0f, -20f);
            qrImgRect.sizeDelta = new Vector2(200f, 200f);
            qrCodeImage = qrImgGo.AddComponent<Image>();

            var titleGo = new GameObject("QR_Text", typeof(RectTransform));
            titleGo.transform.SetParent(qrPanel.transform, false);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0f);
            titleRect.anchorMax = new Vector2(1f, 0f);
            titleRect.pivot = new Vector2(0.5f, 0f);
            titleRect.anchoredPosition = new Vector2(0f, 15f);
            titleRect.sizeDelta = new Vector2(0f, 45f); // Increased height to prevent clipping
            var titleText = titleGo.AddComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.text = "СКАНИРУЙ ДЛЯ ВХОДА";
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = new Color(0.9f, 0.8f, 0.5f, 1f); // Gold tint
            titleText.fontSize = 20;
            titleText.fontStyle = FontStyle.Bold;
            titleText.verticalOverflow = VerticalWrapMode.Overflow; // Prevent cutting off text

            string localIp = GetLocalIPAddress();
            string url = $"http://{localIp}:8080";
            Debug.Log($"[MainMenuPlayerView] Сгенерирован QR-код для URL: {url}");
            StartCoroutine(LoadQRCode(url));
        }

        private IEnumerator LoadQRCode(string data)
        {
            string url = $"https://api.qrserver.com/v1/create-qr-code/?size=250x250&data={UnityWebRequest.EscapeURL(data)}";
            using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url))
            {
                yield return uwr.SendWebRequest();

                if (uwr.result == UnityWebRequest.Result.Success)
                {
                    var texture = DownloadHandlerTexture.GetContent(uwr);
                    var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    if (qrCodeImage != null)
                    {
                        qrCodeImage.sprite = sprite;
                    }
                }
                else
                {
                    Debug.LogError("Failed to load QR code: " + uwr.error);
                }
            }
        }

        private int lastPlayerCount = 0;

        private void Update()
        {
            if (RPGTable.Runtime.Networking.WebServerManager.Instance != null)
            {
                var players = RPGTable.Runtime.Networking.WebServerManager.Instance.ConnectedPlayers;
                if (players.Count > lastPlayerCount)
                {
                    for (int i = lastPlayerCount; i < players.Count; i++)
                    {
                        var p = players[i];
                        Sprite portrait = null;
                        if (!string.IsNullOrEmpty(p.portraitPath))
                        {
                            portrait = RPGTable.TokenEditor.UserTokenStore.LoadSprite(p.portraitPath);
                        }

                        if (portrait == null && !string.IsNullOrEmpty(p.characterPath))
                        {
                            var charData = RPGTable.CharacterEditor.UserCharacterStore.LoadCharacter(p.characterPath);
                            if (charData != null && !string.IsNullOrEmpty(charData.portraitPath))
                            {
                                portrait = RPGTable.TokenEditor.UserTokenStore.LoadSprite(charData.portraitPath);
                            }
                        }
                        AddPlayerToLobby(p.name, portrait);
                    }
                    lastPlayerCount = players.Count;
                }
            }
        }

        public void AddPlayerToLobby(string playerName, Sprite portrait)
        {
            var cardGo = new GameObject($"Player_{playerName}", typeof(RectTransform));
            cardGo.transform.SetParent(rosterRoot, false);
            var cardRect = cardGo.GetComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(200f, 200f);

            // 1. Background Image
            var bgGo = new GameObject("Background", typeof(RectTransform));
            bgGo.transform.SetParent(cardGo.transform, false);
            var bgRect = bgGo.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bg = bgGo.AddComponent<Image>();
            bg.sprite = Resources.Load<Sprite>("image/Mini_background");
            bg.color = Color.white;

            // 2. Portrait Mask Container (placed inside background margins)
            var maskGo = new GameObject("PortraitMask", typeof(RectTransform));
            maskGo.transform.SetParent(cardGo.transform, false);
            var maskRect = maskGo.GetComponent<RectTransform>();
            maskRect.anchorMin = Vector2.zero;
            maskRect.anchorMax = Vector2.one;
            maskRect.offsetMin = new Vector2(16f, 16f); 
            maskRect.offsetMax = new Vector2(-16f, -16f); 
            maskGo.AddComponent<RectMask2D>();

            // 3. Portrait Image
            var imgGo = new GameObject("Portrait", typeof(RectTransform));
            imgGo.transform.SetParent(maskGo.transform, false);
            var imgRect = imgGo.GetComponent<RectTransform>();
            imgRect.anchorMin = Vector2.zero;
            imgRect.anchorMax = Vector2.one;
            imgRect.offsetMin = Vector2.zero;
            imgRect.offsetMax = Vector2.zero;
            var img = imgGo.AddComponent<Image>();
            if (portrait != null)
            {
                img.sprite = portrait;
                img.preserveAspect = true;
            }
            else
            {
                img.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            }

            // 4. Frame Image (Mini_frame0 on top of the portrait)
            var frameGo = new GameObject("Frame", typeof(RectTransform));
            frameGo.transform.SetParent(cardGo.transform, false);
            var frameRect = frameGo.GetComponent<RectTransform>();
            frameRect.anchorMin = Vector2.zero;
            frameRect.anchorMax = Vector2.one;
            frameRect.offsetMin = Vector2.zero;
            frameRect.offsetMax = Vector2.zero;
            var frame = frameGo.AddComponent<Image>();
            frame.sprite = Resources.Load<Sprite>("image/Mini_frame0");
            frame.color = Color.white;

            // 5. Name Bar (name_bar2 at bottom of the card)
            var nameBgGo = new GameObject("NameBg", typeof(RectTransform));
            nameBgGo.transform.SetParent(cardGo.transform, false);
            var nameBgRect = nameBgGo.GetComponent<RectTransform>();
            nameBgRect.anchorMin = new Vector2(0f, 0f);
            nameBgRect.anchorMax = new Vector2(1f, 0f);
            nameBgRect.pivot = new Vector2(0.5f, 0f);
            nameBgRect.anchoredPosition = new Vector2(0f, 8f); // Slightly offset from bottom edge to align inside frame
            nameBgRect.sizeDelta = new Vector2(-12f, 32f); // Width offset by 12px and height is 32px
            var nameBg = nameBgGo.AddComponent<Image>();
            nameBg.sprite = Resources.Load<Sprite>("image/name_bar2");
            nameBg.color = Color.white;

            // 6. Name Text
            var textGo = new GameObject("Name", typeof(RectTransform));
            textGo.transform.SetParent(nameBgGo.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one; // Fill the nameBg block
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var text = textGo.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = playerName;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(1f, 0.95f, 0.85f, 1f);
            text.fontSize = 16;
            text.fontStyle = FontStyle.Bold;
        }

        private string GetLocalIPAddress()
        {
            try
            {
                string bestIp = "127.0.0.1";
                foreach (var netInterface in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
                {
                    // Ignore Loopback, Tunnel, and virtual adapters
                    if (netInterface.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Loopback ||
                        netInterface.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Tunnel ||
                        netInterface.Description.ToLower().Contains("virtual") ||
                        netInterface.Description.ToLower().Contains("vpn") ||
                        netInterface.Description.ToLower().Contains("wireguard") ||
                        netInterface.Description.ToLower().Contains("tap") ||
                        netInterface.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up)
                    {
                        continue;
                    }

                    foreach (var addrInfo in netInterface.GetIPProperties().UnicastAddresses)
                    {
                        if (addrInfo.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            string ip = addrInfo.Address.ToString();
                            // Prioritize standard local subnets (usually Wi-Fi or Ethernet)
                            if (ip.StartsWith("192.168.") || ip.StartsWith("10.") || ip.StartsWith("172."))
                            {
                                return ip; // Return the first solid local IP found
                            }
                            bestIp = ip; // Fallback to whatever IP it has
                        }
                    }
                }
                return bestIp;
            }
            catch
            {
                return "127.0.0.1";
            }
        }
    }
}
