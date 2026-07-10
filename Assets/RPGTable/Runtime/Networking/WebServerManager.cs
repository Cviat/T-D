using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;

namespace RPGTable.Runtime.Networking
{
    public sealed class WebServerManager : MonoBehaviour
    {
        private static WebServerManager instance;

        public static WebServerManager Instance => instance;

        private TcpListener listener;
        private Thread serverThread;
        private bool isRunning = false;
        private string cachedTokensJson = "[]";
        private string cachedStreamingAssetsPath;
        private string cachedTokenImagesFolder;
        
        [Serializable]
        public class ConnectedPlayer
        {
            public string id;
            public string name;
            public string characterPath;
            public string portraitPath;
            public string tokenPath;
            public bool isReady;
        }

        [Serializable]
        public class JoinPayload
        {
            public string name;
            public string description;
            public string photoBase64;
            public string tokenId;
        }

        [Serializable]
        public class RegisterPayload
        {
            public string name;
            public string photoBase64;
        }

        [Serializable]
        public class ImportCharacterPayload
        {
            public string playerId;
            public string characterPath;
            public bool usePlayerPhoto;
        }

        [Serializable]
        public class ReadyPayload
        {
            public string playerId;
            public bool ready;
        }

        [Serializable]
        public class CameraFocusPayload
        {
            public string playerId;
        }

        [Serializable]
        public class MovePayload
        {
            public string playerId;
            public int dirX;
            public int dirY;
        }

        [Serializable]
        public class TransitionPayload
        {
            public string playerId;
            public string action;
        }

        [Serializable]
        public class AttackPayload
        {
            public string playerId;
            public string targetId;
        }
        
        public List<ConnectedPlayer> ConnectedPlayers = new List<ConnectedPlayer>();
        public bool GameStarted = false;

        private readonly System.Collections.Concurrent.ConcurrentQueue<Action> mainThreadActions = new System.Collections.Concurrent.ConcurrentQueue<Action>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (instance == null)
            {
                var go = new GameObject("WebServerManager");
                instance = go.AddComponent<WebServerManager>();
                DontDestroyOnLoad(go);
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            instance = this;
            DontDestroyOnLoad(gameObject);

            cachedStreamingAssetsPath = Application.streamingAssetsPath;
            cachedTokenImagesFolder = System.IO.Path.Combine(Application.persistentDataPath, "RPGTable", "TokenImages");
            System.IO.Directory.CreateDirectory(cachedTokenImagesFolder);

            RefreshTokensCache();
        }

        private void Start()
        {
            StartServer(8080);
        }

        public void ExecuteOnMainThreadBlocking(Action action)
        {
            bool done = false;
            Exception error = null;
            mainThreadActions.Enqueue(() => {
                try { action(); }
                catch (Exception ex) { error = ex; }
                finally { Volatile.Write(ref done, true); }
            });

            while (!Volatile.Read(ref done))
            {
                Thread.Sleep(5);
            }
            
            if (error != null) throw new Exception(error.Message, error);
        }

        public void RefreshTokensCache()
        {
            var tokens = new List<string>();
            var tokenPaths = RPGTable.TokenEditor.UserTokenStore.GetTokenPaths();
            
            foreach (var path in tokenPaths)
            {
                var data = RPGTable.TokenEditor.UserTokenStore.LoadToken(path);
                if (data != null)
                {
                    string id = Convert.ToBase64String(Encoding.UTF8.GetBytes(path));
                    string pPath = data.portraitPath != null ? Convert.ToBase64String(Encoding.UTF8.GetBytes(data.portraitPath)) : "";
                    string fPath = data.framePath != null ? Convert.ToBase64String(Encoding.UTF8.GetBytes(data.framePath)) : "";
                    
                    tokens.Add($"{{\"id\":\"{id}\",\"name\":\"{data.name}\",\"portraitUrl\":\"/api/image?path={pPath}\",\"frameUrl\":\"/api/image?path={fPath}\"}}");
                }
            }
            
            cachedTokensJson = "[" + string.Join(",", tokens) + "]";
        }

        private static string JsonString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "";
            }

            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");
        }

        private string SavePlayerPhoto(string playerName, string photoBase64, string playerId)
        {
            if (string.IsNullOrWhiteSpace(photoBase64))
            {
                return null;
            }

            if (photoBase64.Contains(","))
            {
                photoBase64 = photoBase64.Substring(photoBase64.IndexOf(",") + 1);
            }

            byte[] photoBytes = Convert.FromBase64String(photoBase64);
            string safeName = string.Join("_", (playerName ?? "player").Split(System.IO.Path.GetInvalidFileNameChars()));
            string photoPath = System.IO.Path.Combine(cachedTokenImagesFolder, $"player_photo_{safeName}_{playerId}.jpg");
            System.IO.File.WriteAllBytes(photoPath, photoBytes);
            return photoPath;
        }

        private ConnectedPlayer FindConnectedPlayer(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId))
            {
                return null;
            }

            return ConnectedPlayers.Find(player => player.id == playerId);
        }

        private static bool TryFindRuntimeTokenByPlayerId(string playerId, out RPGTable.Runtime.CampaignRuntimeToken runtimeToken)
        {
            runtimeToken = null;
            var tokens = GameObject.FindObjectsByType<RPGTable.Runtime.CampaignRuntimeToken>(FindObjectsInactive.Exclude);
            foreach (var token in tokens)
            {
                if (token != null
                    && !token.IsPlayerViewClone
                    && !token.IsDead
                    && token.PlayerId == playerId)
                {
                    runtimeToken = token;
                    return true;
                }
            }

            return false;
        }

        private static bool TryMovePlayerToken(string playerId, int dirX, int dirY)
        {
            if (dirX == 0 && dirY == 0)
            {
                return false;
            }

            if (!TryFindRuntimeTokenByPlayerId(playerId, out var runtimeToken))
            {
                return false;
            }

            if (RPGTable.Runtime.CampaignGameSession.IsCombatActive)
            {
                if (RPGTable.Runtime.CombatManager.Instance == null
                    || RPGTable.Runtime.CombatManager.Instance.ActiveToken != runtimeToken
                    || runtimeToken.CurrentMovementPoints <= 0)
                {
                    return false;
                }
            }

            var boardToken = runtimeToken.GetComponent<RPGTable.Core.BoardToken>();
            var grid = GameObject.FindAnyObjectByType<RPGTable.Board.BoardGrid>();
            if (boardToken == null || grid == null)
            {
                return false;
            }

            var size = Mathf.Max(1, boardToken.footprintSize);
            var next = boardToken.gridPosition + new Vector2Int(Mathf.Clamp(dirX, -1, 1), Mathf.Clamp(dirY, -1, 1));
            next.x = Mathf.Clamp(next.x, 0, Mathf.Max(0, grid.width - size));
            next.y = Mathf.Clamp(next.y, 0, Mathf.Max(0, grid.height - size));

            int distance = Mathf.Max(Mathf.Abs(next.x - boardToken.gridPosition.x), Mathf.Abs(next.y - boardToken.gridPosition.y));
            if (distance <= 0)
            {
                return false;
            }

            if (RPGTable.Runtime.CampaignGameSession.IsCombatActive && distance > runtimeToken.CurrentMovementPoints)
            {
                return false;
            }

            boardToken.gridPosition = next;
            var offset = new Vector3((size - 1) * grid.cellSize * 0.5f, (size - 1) * grid.cellSize * 0.5f, 0f);
            runtimeToken.transform.position = grid.CellToWorld(next) + offset;

            if (RPGTable.Runtime.CampaignGameSession.IsCombatActive)
            {
                runtimeToken.CurrentMovementPoints = Mathf.Max(0, runtimeToken.CurrentMovementPoints - distance);
            }

            var player = RPGTable.Runtime.CampaignGameSession.FindPlayer(playerId);
            if (player != null)
            {
                player.gridX = next.x;
                player.gridY = next.y;
                player.currentHp = runtimeToken.CurrentHp;
                player.maxHp = runtimeToken.MaxHp;
            }

            var loader = GameObject.FindAnyObjectByType<RPGTable.Runtime.CampaignGameLoader>();
            if (loader != null && loader.UI != null)
            {
                loader.UI.RefreshActiveTokensPanel();
                loader.UI.RefreshEntityInspector(runtimeToken);
            }

            return true;
        }

        private float nextTokenRefreshTime = 0f;

        private void Update()
        {
            if (Time.time > nextTokenRefreshTime)
            {
                nextTokenRefreshTime = Time.time + 2f;
                RefreshTokensCache();
            }

            while (mainThreadActions.TryDequeue(out Action action))
            {
                try { action(); }
                catch (Exception ex) { Debug.LogError($"[WebServerManager] Main thread action error: {ex}"); }
            }
        }

        public void InitializeServer()
        {
            if (!isRunning)
            {
                StartServer(8080);
            }
        }

        private void StartServer(int port)
        {
            try
            {
                listener = new TcpListener(IPAddress.Any, port);
                listener.Start();
                isRunning = true;
                
                serverThread = new Thread(ListenForClients);
                serverThread.IsBackground = true;
                serverThread.Start();
                
                Debug.Log($"[WebServerManager] TCP Server started on port {port}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebServerManager] Ошибка запуска TCP сервера: {ex.Message}");
            }
        }

        private void ListenForClients()
        {
            while (isRunning)
            {
                try
                {
                    if (listener.Pending())
                    {
                        var client = listener.AcceptTcpClient();
                        ThreadPool.QueueUserWorkItem(ProcessClient, client);
                    }
                    else
                    {
                        Thread.Sleep(10);
                    }
                }
                catch (SocketException)
                {
                    // Happens on shutdown
                }
                catch (Exception ex)
                {
                    if (isRunning) Debug.LogError($"[WebServerManager] Server error: {ex}");
                }
            }
        }

        private void ProcessClient(object state)
        {
            using (var client = (TcpClient)state)
            using (var stream = client.GetStream())
            {
                try
                {
                    List<byte> requestBytes = new List<byte>();
                    byte[] buffer = new byte[8192];
                    int bytesRead;
                    
                    // Read headers
                    string headersStr = "";
                    int bodyStartIndex = -1;
                    
                    while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        for (int i = 0; i < bytesRead; i++) requestBytes.Add(buffer[i]);
                        headersStr = Encoding.UTF8.GetString(requestBytes.ToArray());
                        
                        bodyStartIndex = headersStr.IndexOf("\r\n\r\n");
                        if (bodyStartIndex != -1)
                        {
                            bodyStartIndex += 4; // Skip \r\n\r\n
                            break;
                        }
                    }

                    if (bodyStartIndex == -1) return;

                    string[] lines = headersStr.Substring(0, bodyStartIndex).Split(new[] { "\r\n" }, StringSplitOptions.None);
                    if (lines.Length == 0) return;

                    string[] firstLineParts = lines[0].Split(' ');
                    if (firstLineParts.Length < 2) return;

                    string method = firstLineParts[0];
                    string url = firstLineParts[1];

                    // Read body
                    int contentLength = 0;
                    foreach (string line in lines)
                    {
                        if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                        {
                            int.TryParse(line.Substring(15).Trim(), out contentLength);
                            break;
                        }
                    }

                    int currentBodyLength = requestBytes.Count - bodyStartIndex;
                    while (currentBodyLength < contentLength)
                    {
                        bytesRead = stream.Read(buffer, 0, buffer.Length);
                        if (bytesRead == 0) break;
                        for (int i = 0; i < bytesRead; i++) requestBytes.Add(buffer[i]);
                        currentBodyLength += bytesRead;
                    }

                    string bodyStr = "";
                    if (contentLength > 0)
                    {
                        bodyStr = Encoding.UTF8.GetString(requestBytes.ToArray(), bodyStartIndex, Math.Min(contentLength, requestBytes.Count - bodyStartIndex));
                    }

                    if (url.StartsWith("/api/"))
                    {
                        ProcessApiRequest(method, url, bodyStr, stream);
                    }
                    else
                    {
                        ServeStaticFile(url, stream);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[WebServerManager] Client processing error: {ex.Message}");
                }
            }
        }

        private void ServeStaticFile(string url, NetworkStream stream)
        {
            if (url == "/") url = "/index.html";
            
            // Защита от выхода за пределы папки
            if (url.Contains("..")) 
            {
                SendResponse(stream, 400, "Bad Request", "text/plain", Encoding.UTF8.GetBytes("Bad Request"));
                return;
            }

            // Убираем параметры запроса (напр. ?v=1)
            int queryIndex = url.IndexOf('?');
            if (queryIndex != -1) url = url.Substring(0, queryIndex);

            string filePath = System.IO.Path.Combine(cachedStreamingAssetsPath, "WebClient", url.TrimStart('/'));

            if (System.IO.File.Exists(filePath))
            {
                byte[] content = System.IO.File.ReadAllBytes(filePath);
                string mimeType = GetMimeType(filePath);
                SendResponse(stream, 200, "OK", mimeType, content);
            }
            else
            {
                SendResponse(stream, 404, "Not Found", "text/plain", Encoding.UTF8.GetBytes("404 Not Found"));
            }
        }

        private void ProcessApiRequest(string method, string url, string requestStr, NetworkStream stream)
        {
            if (method == "POST" && url == "/api/session/register")
            {
                try
                {
                    var payload = JsonUtility.FromJson<RegisterPayload>(requestStr);
                    if (payload == null || string.IsNullOrWhiteSpace(payload.name) || string.IsNullOrWhiteSpace(payload.photoBase64))
                    {
                        SendResponse(stream, 400, "Bad Request", "text/plain", Encoding.UTF8.GetBytes("Invalid payload"));
                        return;
                    }

                    string playerId = null;
                    string photoPath = null;

                    ExecuteOnMainThreadBlocking(() =>
                    {
                        playerId = Guid.NewGuid().ToString("N");
                        photoPath = SavePlayerPhoto(payload.name, payload.photoBase64, playerId);
                        var sessionPlayer = RPGTable.Runtime.CampaignGameSession.AddRegisteredPlayer(payload.name, photoPath);
                        playerId = sessionPlayer.id;

                        ConnectedPlayers.Add(new ConnectedPlayer
                        {
                            id = playerId,
                            name = payload.name,
                            portraitPath = photoPath,
                            characterPath = null,
                            tokenPath = null,
                            isReady = false
                        });
                    });

                    string portraitUrl = string.IsNullOrWhiteSpace(photoPath)
                        ? ""
                        : $"/api/image?path={Convert.ToBase64String(Encoding.UTF8.GetBytes(photoPath))}";
                    string responseJson = $"{{\"status\":\"success\",\"playerId\":\"{JsonString(playerId)}\",\"name\":\"{JsonString(payload.name)}\",\"portraitUrl\":\"{JsonString(portraitUrl)}\",\"hasCharacter\":false,\"isReady\":false}}";
                    SendResponse(stream, 200, "OK", "application/json", Encoding.UTF8.GetBytes(responseJson));
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[WebServerManager] /api/session/register error: {ex}");
                    SendResponse(stream, 500, "Internal Server Error", "text/plain", Encoding.UTF8.GetBytes(ex.Message));
                }
                return;
            }

            if (method == "GET" && url.StartsWith("/api/session/restore?playerId="))
            {
                string playerId = Uri.UnescapeDataString(url.Substring("/api/session/restore?playerId=".Length));
                string json = "{\"status\":\"missing\"}";

                ExecuteOnMainThreadBlocking(() =>
                {
                    var player = RPGTable.Runtime.CampaignGameSession.FindPlayer(playerId);
                    if (player == null)
                    {
                        return;
                    }

                    string portraitUrl = string.IsNullOrWhiteSpace(player.portraitPath)
                        ? ""
                        : $"/api/image?path={Convert.ToBase64String(Encoding.UTF8.GetBytes(player.portraitPath))}";
                    json = $"{{\"status\":\"success\",\"playerId\":\"{JsonString(player.id)}\",\"name\":\"{JsonString(player.name)}\",\"portraitUrl\":\"{JsonString(portraitUrl)}\",\"hasCharacter\":{(!string.IsNullOrWhiteSpace(player.characterPath)).ToString().ToLowerInvariant()},\"isReady\":{player.isReady.ToString().ToLowerInvariant()},\"gameStarted\":{GameStarted.ToString().ToLowerInvariant()}}}";
                });

                SendResponse(stream, 200, "OK", "application/json", Encoding.UTF8.GetBytes(json));
                return;
            }

            if (method == "GET" && url == "/api/lobby/state")
            {
                string json = "{}";

                ExecuteOnMainThreadBlocking(() =>
                {
                    var playersJson = new List<string>();
                    foreach (var player in RPGTable.Runtime.CampaignGameSession.CurrentPlayers)
                    {
                        string portraitUrl = string.IsNullOrWhiteSpace(player.portraitPath)
                            ? ""
                            : $"/api/image?path={Convert.ToBase64String(Encoding.UTF8.GetBytes(player.portraitPath))}";
                        bool hasCharacter = !string.IsNullOrWhiteSpace(player.characterPath);
                        playersJson.Add($"{{\"id\":\"{JsonString(player.id)}\",\"name\":\"{JsonString(player.name)}\",\"portraitUrl\":\"{JsonString(portraitUrl)}\",\"hasCharacter\":{hasCharacter.ToString().ToLowerInvariant()},\"isReady\":{player.isReady.ToString().ToLowerInvariant()}}}");
                    }

                    string selectedCampaign = RPGTable.Runtime.CampaignGameSession.SelectedCampaignPath ?? "";
                    json = $"{{\"gameStarted\":{GameStarted.ToString().ToLowerInvariant()},\"selectedCampaign\":\"{JsonString(selectedCampaign)}\",\"players\":[{string.Join(",", playersJson)}]}}";
                });

                SendResponse(stream, 200, "OK", "application/json", Encoding.UTF8.GetBytes(json));
                return;
            }

            if (method == "GET" && url == "/api/characters")
            {
                string json = "[]";

                ExecuteOnMainThreadBlocking(() =>
                {
                    var entries = new List<string>();
                    foreach (var path in RPGTable.CharacterEditor.UserCharacterStore.GetCharacterPaths())
                    {
                        var character = RPGTable.CharacterEditor.UserCharacterStore.LoadCharacter(path);
                        if (character == null)
                        {
                            continue;
                        }

                        string id = Convert.ToBase64String(Encoding.UTF8.GetBytes(path));
                        string portraitUrl = string.IsNullOrWhiteSpace(character.portraitPath)
                            ? ""
                            : $"/api/image?path={Convert.ToBase64String(Encoding.UTF8.GetBytes(character.portraitPath))}";
                        entries.Add($"{{\"id\":\"{JsonString(id)}\",\"name\":\"{JsonString(character.name)}\",\"portraitUrl\":\"{JsonString(portraitUrl)}\",\"level\":{character.level}}}");
                    }

                    json = "[" + string.Join(",", entries) + "]";
                });

                SendResponse(stream, 200, "OK", "application/json", Encoding.UTF8.GetBytes(json));
                return;
            }

            if (method == "POST" && url == "/api/character/import")
            {
                try
                {
                    var payload = JsonUtility.FromJson<ImportCharacterPayload>(requestStr);
                    if (payload == null || string.IsNullOrWhiteSpace(payload.playerId) || string.IsNullOrWhiteSpace(payload.characterPath))
                    {
                        SendResponse(stream, 400, "Bad Request", "text/plain", Encoding.UTF8.GetBytes("Invalid payload"));
                        return;
                    }

                    bool success = false;
                    ExecuteOnMainThreadBlocking(() =>
                    {
                        string characterPath = payload.characterPath;
                        try
                        {
                            characterPath = Encoding.UTF8.GetString(Convert.FromBase64String(Uri.UnescapeDataString(payload.characterPath)));
                        }
                        catch
                        {
                            // Already a raw path.
                        }

                        var character = RPGTable.CharacterEditor.UserCharacterStore.LoadCharacter(characterPath);
                        var player = RPGTable.Runtime.CampaignGameSession.FindPlayer(payload.playerId);
                        if (character == null || player == null)
                        {
                            return;
                        }

                        string portraitPath = payload.usePlayerPhoto && !string.IsNullOrWhiteSpace(player.portraitPath)
                            ? player.portraitPath
                            : character.portraitPath;

                        success = RPGTable.Runtime.CampaignGameSession.AssignCharacterToPlayer(
                            payload.playerId,
                            characterPath,
                            character.name,
                            portraitPath,
                            character.tokenPath);

                        var connected = FindConnectedPlayer(payload.playerId);
                        if (connected != null)
                        {
                            connected.name = character.name;
                            connected.characterPath = characterPath;
                            connected.portraitPath = portraitPath;
                            connected.tokenPath = character.tokenPath;
                            connected.isReady = success;
                        }
                    });

                    if (!success)
                    {
                        SendResponse(stream, 404, "Not Found", "text/plain", Encoding.UTF8.GetBytes("Character or player not found"));
                        return;
                    }

                    SendResponse(stream, 200, "OK", "application/json", Encoding.UTF8.GetBytes("{\"status\":\"success\"}"));
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[WebServerManager] /api/character/import error: {ex}");
                    SendResponse(stream, 500, "Internal Server Error", "text/plain", Encoding.UTF8.GetBytes(ex.Message));
                }
                return;
            }

            if (method == "POST" && url == "/api/player/ready")
            {
                var payload = JsonUtility.FromJson<ReadyPayload>(requestStr);
                if (payload == null || string.IsNullOrWhiteSpace(payload.playerId))
                {
                    SendResponse(stream, 400, "Bad Request", "text/plain", Encoding.UTF8.GetBytes("Invalid payload"));
                    return;
                }

                ExecuteOnMainThreadBlocking(() =>
                {
                    RPGTable.Runtime.CampaignGameSession.SetPlayerReady(payload.playerId, payload.ready);
                    var connected = FindConnectedPlayer(payload.playerId);
                    if (connected != null)
                    {
                        connected.isReady = payload.ready;
                    }
                });

                SendResponse(stream, 200, "OK", "application/json", Encoding.UTF8.GetBytes("{\"status\":\"success\"}"));
                return;
            }

            if (method == "POST" && url == "/api/camera/focus")
            {
                var payload = JsonUtility.FromJson<CameraFocusPayload>(requestStr);
                if (payload == null || string.IsNullOrWhiteSpace(payload.playerId))
                {
                    SendResponse(stream, 400, "Bad Request", "text/plain", Encoding.UTF8.GetBytes("Invalid payload"));
                    return;
                }

                ExecuteOnMainThreadBlocking(() =>
                {
                    var loader = GameObject.FindAnyObjectByType<RPGTable.Runtime.CampaignGameLoader>();
                    if (loader != null)
                    {
                        loader.FocusPlayerViewOnPlayer(payload.playerId);
                    }
                });

                SendResponse(stream, 200, "OK", "application/json", Encoding.UTF8.GetBytes("{\"status\":\"success\"}"));
                return;
            }

            if (method == "GET" && url == "/api/lobby/tokens")
            {
                SendResponse(stream, 200, "OK", "application/json", Encoding.UTF8.GetBytes(cachedTokensJson));
                return;
            }

            if (method == "GET" && url.StartsWith("/api/image?path="))
            {
                string rawB64Path = url.Substring("/api/image?path=".Length);
                try
                {
                    string b64Path = Uri.UnescapeDataString(rawB64Path);
                    string path = Encoding.UTF8.GetString(Convert.FromBase64String(b64Path));
                    if (System.IO.File.Exists(path))
                    {
                        byte[] imgData = System.IO.File.ReadAllBytes(path);
                        SendResponse(stream, 200, "OK", GetMimeType(path), imgData);
                        return;
                    }
                }
                catch { /* Ignore */ }
                
                SendResponse(stream, 404, "Not Found", "text/plain", Encoding.UTF8.GetBytes("Image not found"));
                return;
            }

            if (method == "POST" && url == "/api/lobby/join")
            {
                try
                {
                    var payload = JsonUtility.FromJson<JoinPayload>(requestStr);
                    if (payload == null || string.IsNullOrEmpty(payload.name) || string.IsNullOrEmpty(payload.photoBase64))
                    {
                        SendResponse(stream, 400, "Bad Request", "text/plain", Encoding.UTF8.GetBytes("Invalid payload"));
                        return;
                    }

                    string photoBase64 = payload.photoBase64;
                    if (photoBase64.Contains(","))
                    {
                        photoBase64 = photoBase64.Substring(photoBase64.IndexOf(",") + 1);
                    }
                    byte[] photoBytes = Convert.FromBase64String(photoBase64);

                    string playerId = Guid.NewGuid().ToString();
                    string safeName = string.Join("_", payload.name.Split(System.IO.Path.GetInvalidFileNameChars()));
                    string photoPath = System.IO.Path.Combine(cachedTokenImagesFolder, $"player_photo_{safeName}_{playerId}.png");

                    System.IO.File.WriteAllBytes(photoPath, photoBytes);

                    string characterPath = null;
                    
                    string decodedTokenPath = payload.tokenId;
                    try
                    {
                        decodedTokenPath = Encoding.UTF8.GetString(Convert.FromBase64String(Uri.UnescapeDataString(payload.tokenId)));
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Failed to decode tokenId: {payload.tokenId}, error: {ex.Message}");
                    }

                    ExecuteOnMainThreadBlocking(() => {
                        var charData = new RPGTable.CharacterEditor.SavedCharacterData
                        {
                            name = payload.name,
                            description = payload.description,
                            portraitPath = photoPath,
                            tokenPath = decodedTokenPath // The token path the user chose
                        };
                        characterPath = RPGTable.CharacterEditor.UserCharacterStore.SaveCharacter(payload.name, charData);
                        
                        // Add to game session so it spawns automatically
                        var newPlayer = RPGTable.Runtime.CampaignGameSession.AddCharacterPlayer(characterPath, payload.name, photoPath, decodedTokenPath);
                        
                        // Update playerId to match the CampaignGameSession id (e.g. "player_1")
                        playerId = newPlayer.id;

                        ConnectedPlayers.Add(new ConnectedPlayer
                        {
                            id = playerId,
                            name = payload.name,
                            characterPath = characterPath,
                            portraitPath = photoPath,
                            tokenPath = decodedTokenPath,
                            isReady = true
                        });
                    });

                    string responseJson = $"{{\"status\":\"success\", \"playerId\":\"{playerId}\"}}";
                    SendResponse(stream, 200, "OK", "application/json", Encoding.UTF8.GetBytes(responseJson));
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[WebServerManager] /api/lobby/join error: {ex}");
                    SendResponse(stream, 500, "Internal Server Error", "text/plain", Encoding.UTF8.GetBytes(ex.Message));
                }
                return;
            }

            if (method == "GET" && url.StartsWith("/api/lobby/status?playerId="))
            {
                string status = GameStarted ? "game_started" : "waiting";
                string json = $"{{\"status\":\"{status}\"}}";
                SendResponse(stream, 200, "OK", "application/json", Encoding.UTF8.GetBytes(json));
                return;
            }

            if (method == "POST" && url == "/api/action/move")
            {
                try
                {
                    var payload = JsonUtility.FromJson<MovePayload>(requestStr);
                    if (payload != null && !string.IsNullOrEmpty(payload.playerId))
                    {
                        bool moved = false;
                        ExecuteOnMainThreadBlocking(() =>
                        {
                            moved = TryMovePlayerToken(payload.playerId, payload.dirX, payload.dirY);
                        });

                        string responseJson = moved
                            ? "{\"status\":\"success\"}"
                            : "{\"status\":\"blocked\"}";
                        SendResponse(stream, 200, "OK", "application/json", Encoding.UTF8.GetBytes(responseJson));
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[WebServerManager] move error: {ex}");
                }
                return;
            }

            if (method == "GET" && url.StartsWith("/api/game/state?playerId="))
            {
                string playerId = url.Substring("/api/game/state?playerId=".Length);
                string json = "{}";
                string promptText = null;
                var enemiesJson = new List<string>();
                string playerStateJson = "";

                ExecuteOnMainThreadBlocking(() => {
#if UNITY_2023_1_OR_NEWER
                    var loader = GameObject.FindAnyObjectByType<RPGTable.Runtime.CampaignGameLoader>();
#else
                    var loader = GameObject.FindAnyObjectByType<RPGTable.Runtime.CampaignGameLoader>();
#endif
                    if (loader != null && loader.PendingTransitionPlayerId == playerId)
                    {
                        promptText = loader.PendingTransitionPrompt;
                    }

                    var allBoardTokens = GameObject.FindObjectsByType<RPGTable.Core.BoardToken>(FindObjectsInactive.Exclude);
                    var myBoardToken = System.Linq.Enumerable.FirstOrDefault(allBoardTokens, t => 
                    {
                        var r = t.GetComponent<RPGTable.Runtime.CampaignRuntimeToken>();
                        return r != null && r.PlayerId == playerId;
                    });

                    if (myBoardToken != null)
                    {
                        var myRuntimeToken = myBoardToken.GetComponent<RPGTable.Runtime.CampaignRuntimeToken>();
                        if (myRuntimeToken != null)
                        {
                            var charData = string.IsNullOrWhiteSpace(myRuntimeToken.CharacterPath)
                                ? null
                                : RPGTable.CharacterEditor.UserCharacterStore.LoadCharacter(myRuntimeToken.CharacterPath);
                            string activeWeapon = "";
                            if (charData != null)
                            {
                                activeWeapon = myRuntimeToken.ActiveWeaponIndex == 0 ? charData.eqWeapon : charData.eqWeapon2;
                            }

                            var statusJson = new List<string>();
                            foreach (var effect in myRuntimeToken.statusEffects)
                            {
                                if (effect == null)
                                {
                                    continue;
                                }

                                statusJson.Add($"{{\"name\":\"{JsonString(effect.effectName)}\",\"turns\":{effect.durationTurns},\"value\":{effect.value}}}");
                            }

                            bool isMyTurn = RPGTable.Runtime.CampaignGameSession.IsCombatActive
                                && RPGTable.Runtime.CombatManager.Instance != null
                                && RPGTable.Runtime.CombatManager.Instance.ActiveToken == myRuntimeToken;

                            playerStateJson =
                                $"\"hp\":{myRuntimeToken.CurrentHp}," +
                                $"\"maxHp\":{myRuntimeToken.MaxHp}," +
                                $"\"armor\":{myRuntimeToken.CurrentArmor}," +
                                $"\"maxArmor\":{myRuntimeToken.MaxArmor}," +
                                $"\"movement\":{myRuntimeToken.CurrentMovementPoints}," +
                                $"\"maxMovement\":{myRuntimeToken.MaxMovementPoints}," +
                                $"\"rolls\":{myRuntimeToken.CurrentRolls}," +
                                $"\"maxRolls\":{myRuntimeToken.MaxRolls}," +
                                $"\"activeWeapon\":\"{JsonString(activeWeapon)}\"," +
                                $"\"activeWeaponIndex\":{myRuntimeToken.ActiveWeaponIndex}," +
                                $"\"isMyTurn\":{isMyTurn.ToString().ToLowerInvariant()}," +
                                $"\"isCombatActive\":{RPGTable.Runtime.CampaignGameSession.IsCombatActive.ToString().ToLowerInvariant()}," +
                                $"\"statuses\":[{string.Join(",", statusJson)}],";
                        }

                        foreach (var targetBoardToken in allBoardTokens)
                        {
                            if (targetBoardToken != myBoardToken && targetBoardToken.team != RPGTable.Core.TokenTeam.Player)
                            {
                                var targetRuntime = targetBoardToken.GetComponent<RPGTable.Runtime.CampaignRuntimeToken>();
                                if (targetRuntime == null)
                                {
                                    targetRuntime = targetBoardToken.gameObject.AddComponent<RPGTable.Runtime.CampaignRuntimeToken>();
                                    targetRuntime.RuntimeId = System.Guid.NewGuid().ToString("N");
                                    targetRuntime.DisplayName = targetBoardToken.displayName;
                                    targetRuntime.Team = targetBoardToken.team;
                                    targetRuntime.MaxHp = 10;
                                    targetRuntime.CurrentHp = 10;
                                    
                                    var grid = GameObject.FindAnyObjectByType<RPGTable.Board.BoardGrid>();
                                    if (grid != null) targetBoardToken.SnapToGrid(grid);
                                }

                                if (!targetRuntime.IsDead)
                                {
                                    Vector2Int targetGridPos = targetBoardToken.gridPosition;
                                    var grid = GameObject.FindAnyObjectByType<RPGTable.Board.BoardGrid>();
                                    if (grid != null)
                                    {
                                        var size = Mathf.Max(1, targetBoardToken.footprintSize);
                                        var offset = new Vector3((size - 1) * grid.cellSize * 0.5f, (size - 1) * grid.cellSize * 0.5f, 0f);
                                        targetGridPos = grid.WorldToCell(targetBoardToken.transform.position - offset);
                                        // Also snap it if it was completely uninitialized
                                        if (targetBoardToken.gridPosition == Vector2Int.zero && targetBoardToken.transform.position.sqrMagnitude > 0.1f)
                                        {
                                            targetBoardToken.SnapToGrid(grid);
                                        }
                                    }

                                    int myMinX = myBoardToken.gridPosition.x;
                                    int myMaxX = myBoardToken.gridPosition.x + myBoardToken.footprintSize - 1;
                                    int myMinY = myBoardToken.gridPosition.y;
                                    int myMaxY = myBoardToken.gridPosition.y + myBoardToken.footprintSize - 1;

                                    int tMinX = targetGridPos.x;
                                    int tMaxX = targetGridPos.x + targetBoardToken.footprintSize - 1;
                                    int tMinY = targetGridPos.y;
                                    int tMaxY = targetGridPos.y + targetBoardToken.footprintSize - 1;

                                    int dx = Mathf.Max(0, Mathf.Max(myMinX - tMaxX, tMinX - myMaxX));
                                    int dy = Mathf.Max(0, Mathf.Max(myMinY - tMaxY, tMinY - myMaxY));

                                    // dx/dy = 1 means touching. dx/dy = 2 means 1 empty cell between them.
                                    if (dx <= 2 && dy <= 2)
                                    {
                                        var data = RPGTable.TokenEditor.UserTokenStore.LoadToken(targetRuntime.TokenPath);
                                        string pPath = data != null && data.portraitPath != null ? Convert.ToBase64String(Encoding.UTF8.GetBytes(data.portraitPath)) : "";
                                        string url = $"/api/image?path={pPath}";
                                        enemiesJson.Add($"{{\"id\":\"{targetRuntime.RuntimeId}\",\"name\":\"{targetRuntime.DisplayName}\",\"portraitUrl\":\"{url}\",\"hp\":{targetRuntime.CurrentHp},\"maxHp\":{targetRuntime.MaxHp}}}");
                                    }
                                }
                            }
                        }
                    }
                });

                if (!string.IsNullOrEmpty(promptText))
                {
                    json = $"{{{playerStateJson}\"prompt\":\"{JsonString(promptText)}\", \"enemies\":[{string.Join(",", enemiesJson)}]}}";
                }
                else
                {
                    json = $"{{{playerStateJson}\"enemies\":[{string.Join(",", enemiesJson)}]}}";
                }
                
                SendResponse(stream, 200, "OK", "application/json", Encoding.UTF8.GetBytes(json));
                return;
            }

            if (method == "POST" && url == "/api/action/transition")
            {
                try
                {
                    var payload = JsonUtility.FromJson<TransitionPayload>(requestStr);
                    if (payload != null && !string.IsNullOrEmpty(payload.playerId))
                    {
                        ExecuteOnMainThreadBlocking(() => {
#if UNITY_2023_1_OR_NEWER
                            var loader = GameObject.FindAnyObjectByType<RPGTable.Runtime.CampaignGameLoader>();
#else
                            var loader = GameObject.FindAnyObjectByType<RPGTable.Runtime.CampaignGameLoader>();
#endif
                            if (loader != null && loader.PendingTransitionPlayerId == payload.playerId)
                            {
                                if (payload.action == "confirm")
                                {
                                    loader.HandleConfirmTransition();
                                }
                                else if (payload.action == "cancel")
                                {
                                    loader.HandleCancelTransition();
                                }
                            }
                        });
                        SendResponse(stream, 200, "OK", "application/json", Encoding.UTF8.GetBytes("{\"status\":\"success\"}"));
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[WebServerManager] transition error: {ex}");
                }
                return;
            }

            if (method == "POST" && url == "/api/action/attack")
            {
                try
                {
                    var payload = JsonUtility.FromJson<AttackPayload>(requestStr);
                    if (payload != null && !string.IsNullOrEmpty(payload.playerId) && !string.IsNullOrEmpty(payload.targetId))
                    {
                        ExecuteOnMainThreadBlocking(() => {
                            var tokens = GameObject.FindObjectsByType<RPGTable.Runtime.CampaignRuntimeToken>(FindObjectsInactive.Exclude);
                            var myToken = System.Linq.Enumerable.FirstOrDefault(tokens, t => t.PlayerId == payload.playerId);
                            var targetToken = System.Linq.Enumerable.FirstOrDefault(tokens, t => t.RuntimeId == payload.targetId);

                            if (myToken != null && targetToken != null && !targetToken.IsDead)
                            {
                                // Apply damage
                                targetToken.CurrentHp -= 1;
                                // Update GM UI
#if UNITY_2023_1_OR_NEWER
                                var uiLoader = GameObject.FindAnyObjectByType<RPGTable.Runtime.CampaignGameLoader>();
#else
                                var uiLoader = GameObject.FindAnyObjectByType<RPGTable.Runtime.CampaignGameLoader>();
#endif
                                if (uiLoader != null)
                                {
                                    uiLoader.UI.RefreshActiveTokensPanel();
                                }

                                if (targetToken.CurrentHp <= 0)
                                {
                                    var targetBoardToken = targetToken.GetComponent<RPGTable.Core.BoardToken>();
                                    if (uiLoader != null && targetBoardToken != null)
                                    {
                                        uiLoader.StartCoroutine(DelayDeathRoutine(targetToken, targetBoardToken.footprintSize, uiLoader));
                                    }
                                }

                                // Animate GM token
                                var animator = myToken.GetComponent<RPGTable.Runtime.TokenAttackAnimator>();
                                if (animator == null) animator = myToken.gameObject.AddComponent<RPGTable.Runtime.TokenAttackAnimator>();
                                animator.AnimateAttack(targetToken.transform.position);

                                var targetAnimator = targetToken.GetComponent<RPGTable.Runtime.TokenAttackAnimator>();
                                if (targetAnimator == null) targetAnimator = targetToken.gameObject.AddComponent<RPGTable.Runtime.TokenAttackAnimator>();
                                targetAnimator.AnimateDamage(myToken.transform.position);

                                // Animate Player View token (layer 31)
                                GameObject pvMyToken = null;
                                GameObject pvTargetToken = null;
                                foreach (var obj in GameObject.FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
                                {
                                    if (obj.gameObject.layer == 31)
                                    {
                                        if (obj.name == myToken.name || obj.name == myToken.DisplayName) pvMyToken = obj.gameObject;
                                        if (obj.name == targetToken.name || obj.name == targetToken.DisplayName) pvTargetToken = obj.gameObject;
                                    }
                                }
                                if (pvMyToken != null && pvTargetToken != null)
                                {
                                    var pvAnimator = pvMyToken.GetComponent<RPGTable.Runtime.TokenAttackAnimator>();
                                    if (pvAnimator == null) pvAnimator = pvMyToken.AddComponent<RPGTable.Runtime.TokenAttackAnimator>();
                                    pvAnimator.AnimateAttack(pvTargetToken.transform.position);

                                    var pvTargetAnimator = pvTargetToken.GetComponent<RPGTable.Runtime.TokenAttackAnimator>();
                                    if (pvTargetAnimator == null) pvTargetAnimator = pvTargetToken.AddComponent<RPGTable.Runtime.TokenAttackAnimator>();
                                    pvTargetAnimator.AnimateDamage(pvMyToken.transform.position);
                                }
                            }
                        });
                        SendResponse(stream, 200, "OK", "application/json", Encoding.UTF8.GetBytes("{\"status\":\"success\"}"));
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[WebServerManager] attack error: {ex}");
                }
                return;
            }

            SendResponse(stream, 404, "Not Found", "text/plain", Encoding.UTF8.GetBytes("API endpoint not found"));
        }

        private void SendResponse(NetworkStream stream, int statusCode, string statusText, string mimeType, byte[] contentBytes)
        {
            string header = $"HTTP/1.1 {statusCode} {statusText}\r\n" +
                            $"Content-Type: {mimeType}; charset=utf-8\r\n" +
                            $"Content-Length: {contentBytes.Length}\r\n" +
                            "Access-Control-Allow-Origin: *\r\n" +
                            "Connection: close\r\n\r\n";
                            
            byte[] headerBytes = Encoding.UTF8.GetBytes(header);
            stream.Write(headerBytes, 0, headerBytes.Length);
            if (contentBytes.Length > 0)
            {
                stream.Write(contentBytes, 0, contentBytes.Length);
            }
        }

        private System.Collections.IEnumerator DelayDeathRoutine(RPGTable.Runtime.CampaignRuntimeToken token, int footprint, RPGTable.Runtime.CampaignGameLoader loader)
        {
            yield return new WaitForSeconds(0.6f);
            if (token != null)
            {
                token.IsDead = true;
                if (loader != null && loader.Spawner != null)
                {
                    loader.Spawner.ApplyDeadVisual(token, footprint);
                }
                if (loader != null && loader.UI != null)
                {
                    loader.UI.RefreshActiveTokensPanel();
                }
            }
        }

        private string GetMimeType(string filePath)
        {
            string ext = System.IO.Path.GetExtension(filePath).ToLower();
            switch (ext)
            {
                case ".html": return "text/html";
                case ".css": return "text/css";
                case ".js": return "application/javascript";
                case ".json": return "application/json";
                case ".png": return "image/png";
                case ".jpg": case ".jpeg": return "image/jpeg";
                default: return "application/octet-stream";
            }
        }

        private void OnDestroy()
        {
            Debug.Log($"[WebServerManager] OnDestroy called. Stopping server. StackTrace:\n{StackTraceUtility.ExtractStackTrace()}");
            if (instance == this)
            {
                StopServer();
                instance = null;
            }
        }

        private void StopServer()
        {
            Debug.Log("[WebServerManager] StopServer called.");
            isRunning = false;
            if (listener != null)
            {
                listener.Stop();
                listener = null;
            }
            if (serverThread != null && serverThread.IsAlive)
            {
                serverThread.Join(500);
            }
        }
    }
}
