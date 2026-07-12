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

        [Serializable]
        public class RequestAttackPayload
        {
            public string playerId;
            public string targetId;
        }

        [Serializable]
        public class SubmitRollPayload
        {
            public string playerId;
            public int rollResult;
        }

        [Serializable]
        public class RerollPayload
        {
            public string playerId;
        }

        [Serializable]
        public class SwitchWeaponPayload
        {
            public string playerId;
        }

        [Serializable]
        public class EndTurnPayload
        {
            public string playerId;
        }

        [Serializable]
        public class PendingRoll
        {
            public string id;
            public string type; // "attack" or "defense"
            public string playerId;
            public string targetTokenId;
            public string attackerTokenId;
            public string attackerAbilityName;
            public int attackerRollResult;
            public float baseDamage;
            public bool canReroll;
            public int rerollCost;
        }
        
        public List<ConnectedPlayer> ConnectedPlayers = new List<ConnectedPlayer>();
        public Dictionary<string, PendingRoll> ActiveRolls = new Dictionary<string, PendingRoll>();
        public static readonly System.Collections.Concurrent.ConcurrentDictionary<string, System.DateTime> LastSeenTimes = new System.Collections.Concurrent.ConcurrentDictionary<string, System.DateTime>();
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

            var player = RPGTable.Runtime.CampaignGameSession.FindPlayer(playerId);
            if (player == null || player.isDead)
            {
                return false;
            }

            if (RPGTable.Runtime.CampaignGameSession.IsCombatActive)
            {
                if (RPGTable.Runtime.CombatManager.Instance == null
                    || RPGTable.Runtime.CombatManager.Instance.ActiveTokenId != playerId
                    || player.currentMovementPoints <= 0)
                {
                    return false;
                }
            }

            var grid = GameObject.FindAnyObjectByType<RPGTable.Board.BoardGrid>();
            if (grid == null)
            {
                return false;
            }

            int footprint = 1;
            if (!string.IsNullOrEmpty(player.tokenPath))
            {
                var tokenData = RPGTable.TokenEditor.UserTokenStore.LoadToken(player.tokenPath);
                footprint = RPGTable.Runtime.CampaignTokenSpawner.GetFootprint(tokenData);
            }

            var size = Mathf.Max(1, footprint);
            // Each tap moves one full footprint-step (e.g. 2x2 token → 2 cells per tap)
            int stepX = Mathf.Clamp(dirX, -1, 1) * footprint;
            int stepY = Mathf.Clamp(dirY, -1, 1) * footprint;
            var next = new Vector2Int(player.gridX + stepX, player.gridY + stepY);
            next.x = Mathf.Clamp(next.x, 0, Mathf.Max(0, grid.width - size));
            next.y = Mathf.Clamp(next.y, 0, Mathf.Max(0, grid.height - size));

            // Each tap = 1 step; divide cell distance by footprint to get step count
            int stepsDistance = Mathf.Max(Mathf.Abs(next.x - player.gridX), Mathf.Abs(next.y - player.gridY)) / Mathf.Max(1, footprint);
            if (stepsDistance <= 0)
            {
                return false;
            }

            if (RPGTable.Runtime.CampaignGameSession.IsCombatActive && stepsDistance > player.currentMovementPoints)
            {
                return false;
            }

            if (RPGTable.Runtime.CampaignGameSession.IsCombatActive)
            {
                player.currentMovementPoints = Mathf.Max(0, player.currentMovementPoints - stepsDistance);
                // Fire the data-changed event so the GM grid highlights refresh
                RPGTable.Runtime.CampaignGameSession.UpdateTokenCombatStats(
                    playerId, player.currentMapId,
                    player.currentHp, player.maxHp,
                    player.currentArmor, player.maxArmor,
                    player.currentMovementPoints, player.maxMovementPoints,
                    player.currentRolls, player.maxRolls,
                    player.activeWeaponIndex, player.rerollCoins,
                    player.statusEffects, player.isDead);
            }

            RPGTable.Runtime.CampaignGameSession.MoveToken(playerId, player.currentMapId, next);
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
                LastSeenTimes[playerId] = System.DateTime.UtcNow;
                string json = "{}";
                string promptText = null;
                var enemiesJson = new List<string>();
                string playerStateJson = "";
                string pendingRollJson = "null";

                ExecuteOnMainThreadBlocking(() => {
                    var player = RPGTable.Runtime.CampaignGameSession.FindPlayer(playerId);
                    if (player != null)
                    {
#if UNITY_2023_1_OR_NEWER
                        var loader = GameObject.FindAnyObjectByType<RPGTable.Runtime.CampaignGameLoader>();
#else
                        var loader = GameObject.FindAnyObjectByType<RPGTable.Runtime.CampaignGameLoader>();
#endif
                        if (loader != null && loader.PendingTransitionPlayerId == playerId)
                        {
                            promptText = loader.PendingTransitionPrompt;
                        }

                        var charData = string.IsNullOrWhiteSpace(player.characterPath)
                            ? null
                            : RPGTable.CharacterEditor.UserCharacterStore.LoadCharacter(player.characterPath);
                        string activeWeapon = "";
                        if (charData != null)
                        {
                            activeWeapon = player.activeWeaponIndex == 0 ? charData.eqWeapon : charData.eqWeapon2;
                        }

                        var statusJson = new List<string>();
                        foreach (var effect in player.statusEffects)
                        {
                            if (effect == null) continue;
                            statusJson.Add($"{{\"name\":\"{JsonString(effect.effectName)}\",\"turns\":{effect.durationTurns},\"value\":{effect.value}}}");
                        }

                        var attackSlotsJson = new List<string>();
                        var defenseSlotsJson = new List<string>();
                        if (charData != null)
                        {
                            var slots = (player.activeWeaponIndex == 0) ? charData.attackSlots : charData.attack2Slots;
                            if (slots != null)
                            {
                                foreach (var s in slots) attackSlotsJson.Add($"\"{JsonString(s)}\"");
                            }
                            if (charData.defenseSlots != null)
                            {
                                foreach (var s in charData.defenseSlots) defenseSlotsJson.Add($"\"{JsonString(s)}\"");
                            }
                        }

                        bool isMyTurn = RPGTable.Runtime.CampaignGameSession.IsCombatActive
                            && RPGTable.Runtime.CombatManager.Instance != null
                            && RPGTable.Runtime.CombatManager.Instance.ActiveTokenId == playerId;

                        playerStateJson =
                            $"\"hp\":{player.currentHp}," +
                            $"\"maxHp\":{player.maxHp}," +
                            $"\"armor\":{player.currentArmor}," +
                            $"\"maxArmor\":{player.maxArmor}," +
                            $"\"movement\":{player.currentMovementPoints}," +
                            $"\"maxMovement\":{player.maxMovementPoints}," +
                            $"\"rolls\":{player.currentRolls}," +
                            $"\"maxRolls\":{player.maxRolls}," +
                            $"\"activeWeapon\":\"{JsonString(activeWeapon)}\"," +
                            $"\"activeWeaponIndex\":{player.activeWeaponIndex}," +
                            $"\"isMyTurn\":{isMyTurn.ToString().ToLowerInvariant()}," +
                            $"\"isCombatActive\":{RPGTable.Runtime.CampaignGameSession.IsCombatActive.ToString().ToLowerInvariant()}," +
                            $"\"statuses\":[{string.Join(",", statusJson)}]," +
                            $"\"attacks\":[{string.Join(",", attackSlotsJson)}]," +
                            $"\"defenses\":[{string.Join(",", defenseSlotsJson)}]," +
                            $"\"rerollCoins\":{player.rerollCoins},";

                        int myFootprint = 1;
                        if (!string.IsNullOrEmpty(player.tokenPath))
                        {
                            var tokenData = RPGTable.TokenEditor.UserTokenStore.LoadToken(player.tokenPath);
                            myFootprint = RPGTable.Runtime.CampaignTokenSpawner.GetFootprint(tokenData);
                        }

                        int myMinX = player.gridX;
                        int myMaxX = player.gridX + myFootprint - 1;
                        int myMinY = player.gridY;
                        int myMaxY = player.gridY + myFootprint - 1;

                        int maxRange = 2;
                        if (loader != null)
                        {
                            maxRange = Mathf.Max(2, RPGTable.Runtime.CampaignGameLoader.GetMaxAbilityRange(player.characterPath, player.activeWeaponIndex));
                        }

                        if (!string.IsNullOrEmpty(player.currentMapId) && RPGTable.Runtime.CampaignGameSession.MapTokenStates.TryGetValue(player.currentMapId, out var npcList))
                        {
                            foreach (var npc in npcList)
                            {
                                if (npc != null && !npc.isDead && npc.team != RPGTable.Core.TokenTeam.Player)
                                {
                                    int npcFootprint = 1;
                                    if (!string.IsNullOrEmpty(npc.tokenPath))
                                    {
                                        var npcData = RPGTable.TokenEditor.UserTokenStore.LoadToken(npc.tokenPath);
                                        npcFootprint = RPGTable.Runtime.CampaignTokenSpawner.GetFootprint(npcData);
                                    }

                                    int tMinX = npc.gridPosition.x;
                                    int tMaxX = npc.gridPosition.x + npcFootprint - 1;
                                    int tMinY = npc.gridPosition.y;
                                    int tMaxY = npc.gridPosition.y + npcFootprint - 1;

                                    int dx = Mathf.Max(0, Mathf.Max(myMinX - tMaxX, tMinX - myMaxX));
                                    int dy = Mathf.Max(0, Mathf.Max(myMinY - tMaxY, tMinY - myMaxY));

                                    if (dx <= maxRange && dy <= maxRange)
                                    {
                                        var data = RPGTable.TokenEditor.UserTokenStore.LoadToken(npc.tokenPath);
                                        string pPath = data != null && data.portraitPath != null ? Convert.ToBase64String(Encoding.UTF8.GetBytes(data.portraitPath)) : "";
                                        string url = $"/api/image?path={pPath}";
                                        enemiesJson.Add($"{{\"id\":\"{npc.runtimeId}\",\"name\":\"{npc.displayName}\",\"portraitUrl\":\"{url}\",\"hp\":{npc.currentHp},\"maxHp\":{npc.maxHp}}}");
                                    }
                                }
                            }
                        }

                        if (ActiveRolls.TryGetValue(playerId, out var pr))
                        {
                            string attackerName = "";
                            if (pr.type == "defense" && !string.IsNullOrEmpty(pr.attackerTokenId))
                            {
                                var attackerToken = RPGTable.Runtime.CampaignGameSession.FindPlayer(pr.attackerTokenId);
                                if (attackerToken != null)
                                {
                                    attackerName = attackerToken.name;
                                }
                                else if (!string.IsNullOrEmpty(player.currentMapId))
                                {
                                    var npc = RPGTable.Runtime.CampaignGameSession.FindNPCState(player.currentMapId, pr.attackerTokenId);
                                    if (npc != null) attackerName = npc.displayName;
                                }
                            }

                             pendingRollJson = "{" +
                                $"\"id\":\"{JsonString(pr.id)}\"," +
                                $"\"type\":\"{JsonString(pr.type)}\"," +
                                $"\"playerId\":\"{JsonString(pr.playerId)}\"," +
                                $"\"targetTokenId\":\"{JsonString(pr.targetTokenId)}\"," +
                                $"\"attackerTokenId\":\"{JsonString(pr.attackerTokenId)}\"," +
                                $"\"attackerName\":\"{JsonString(attackerName)}\"," +
                                $"\"attackerAbilityName\":\"{JsonString(pr.attackerAbilityName)}\"," +
                                $"\"attackerRollResult\":{pr.attackerRollResult}," +
                                $"\"baseDamage\":{pr.baseDamage}," +
                                $"\"canReroll\":{pr.canReroll.ToString().ToLowerInvariant()}," +
                                $"\"rerollCost\":{pr.rerollCost}" +
                                "}";
                         }
                    }
                });

                if (!string.IsNullOrEmpty(promptText))
                {
                    json = $"{{{playerStateJson}\"prompt\":\"{JsonString(promptText)}\", \"enemies\":[{string.Join(",", enemiesJson)}], \"pendingRoll\":{pendingRollJson}}}";
                }
                else
                {
                    json = $"{{{playerStateJson}\"enemies\":[{string.Join(",", enemiesJson)}], \"pendingRoll\":{pendingRollJson}}}";
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

            if (method == "POST" && (url == "/api/action/request-attack" || url == "/api/action/attack"))
            {
                try
                {
                    var payload = JsonUtility.FromJson<RequestAttackPayload>(requestStr);
                    if (payload != null)
                    {
                        Debug.Log($"[WebServerManager] request-attack payload: playerId={payload.playerId}, targetId={payload.targetId}");
                    }
                    if (payload != null && !string.IsNullOrEmpty(payload.playerId) && !string.IsNullOrEmpty(payload.targetId))
                    {
                        bool success = false;
                        string failReason = "unknown";
                        ExecuteOnMainThreadBlocking(() => {
                            var loader = GameObject.FindAnyObjectByType<RPGTable.Runtime.CampaignGameLoader>();
                            if (loader != null)
                            {
                                var tokens = GameObject.FindObjectsByType<RPGTable.Runtime.CampaignRuntimeToken>(FindObjectsInactive.Exclude);
                                var myToken = System.Linq.Enumerable.FirstOrDefault(tokens, t => !t.IsPlayerViewClone && t.PlayerId == payload.playerId);
                                var targetToken = System.Linq.Enumerable.FirstOrDefault(tokens, t => !t.IsPlayerViewClone && t.RuntimeId == payload.targetId);
                                
                                if (myToken == null) Debug.LogWarning($"[WebServerManager] myToken not found for playerId={payload.playerId}");
                                if (targetToken == null) Debug.LogWarning($"[WebServerManager] targetToken not found for targetId={payload.targetId}");
                                
                                if (myToken != null && targetToken != null)
                                {
                                    if (targetToken.IsDead)
                                    {
                                        failReason = "target is dead";
                                    }
                                    else
                                    {
                                        Debug.Log($"[WebServerManager] Calling loader.InitiateAttackSequence attacker={myToken.DisplayName}, target={targetToken.DisplayName}");
                                        loader.InitiateAttackSequence(myToken, targetToken);
                                        success = true;
                                    }
                                }
                                else
                                {
                                    failReason = "token not found";
                                }
                            }
                            else
                            {
                                failReason = "loader not found";
                            }
                        });
                        string resp = success ? "{\"status\":\"success\"}" : $"{{\"status\":\"failed\",\"reason\":\"{failReason}\"}}";
                        SendResponse(stream, 200, "OK", "application/json", Encoding.UTF8.GetBytes(resp));
                    }
                    else
                    {
                        SendResponse(stream, 400, "Bad Request", "text/plain", Encoding.UTF8.GetBytes("Invalid payload"));
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[WebServerManager] request-attack error: {ex}");
                    SendResponse(stream, 500, "Internal Server Error", "text/plain", Encoding.UTF8.GetBytes(ex.Message));
                }
                return;
            }

            if (method == "POST" && url == "/api/roll/submit")
            {
                try
                {
                    var payload = JsonUtility.FromJson<SubmitRollPayload>(requestStr);
                    if (payload != null && !string.IsNullOrEmpty(payload.playerId))
                    {
                        bool success = false;
                        ExecuteOnMainThreadBlocking(() => {
                            var loader = GameObject.FindAnyObjectByType<RPGTable.Runtime.CampaignGameLoader>();
                            if (loader != null)
                            {
                                success = loader.SubmitRoll(payload.playerId, payload.rollResult);
                            }
                        });
                        string resp = success ? "{\"status\":\"success\"}" : "{\"status\":\"failed\"}";
                        SendResponse(stream, 200, "OK", "application/json", Encoding.UTF8.GetBytes(resp));
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[WebServerManager] roll/submit error: {ex}");
                    SendResponse(stream, 500, "Internal Server Error", "text/plain", Encoding.UTF8.GetBytes(ex.Message));
                }
                return;
            }

            if (method == "POST" && url == "/api/roll/reroll")
            {
                try
                {
                    var payload = JsonUtility.FromJson<RerollPayload>(requestStr);
                    if (payload != null && !string.IsNullOrEmpty(payload.playerId))
                    {
                        int newRoll = 0;
                        ExecuteOnMainThreadBlocking(() => {
                            var player = CampaignGameSession.FindPlayer(payload.playerId);
                            if (player != null && player.rerollCoins > 0)
                            {
                                player.rerollCoins--;
                                newRoll = UnityEngine.Random.Range(1, 7);
                                CampaignGameSession.UpdateTokenCombatStats(
                                    player.id, player.currentMapId,
                                    player.currentHp, player.maxHp,
                                    player.currentArmor, player.maxArmor,
                                    player.currentMovementPoints, player.maxMovementPoints,
                                    player.currentRolls, player.maxRolls,
                                    player.activeWeaponIndex, player.rerollCoins,
                                    player.statusEffects, player.isDead);
                            }
                        });
                        string resp = newRoll > 0 
                            ? $"{{\"status\":\"success\",\"rollResult\":{newRoll}}}"
                            : "{\"status\":\"failed\",\"reason\":\"No coins left\"}";
                        SendResponse(stream, 200, "OK", "application/json", Encoding.UTF8.GetBytes(resp));
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[WebServerManager] roll/reroll error: {ex}");
                    SendResponse(stream, 500, "Internal Server Error", "text/plain", Encoding.UTF8.GetBytes(ex.Message));
                }
                return;
            }

            if (method == "POST" && url == "/api/action/switch-weapon")
            {
                try
                {
                    var payload = JsonUtility.FromJson<SwitchWeaponPayload>(requestStr);
                    if (payload != null && !string.IsNullOrEmpty(payload.playerId))
                    {
                        bool success = false;
                        ExecuteOnMainThreadBlocking(() => {
                            var player = CampaignGameSession.FindPlayer(payload.playerId);
                            if (player != null)
                            {
                                player.activeWeaponIndex = player.activeWeaponIndex == 0 ? 1 : 0;
                                CampaignGameSession.UpdateTokenCombatStats(
                                    player.id, player.currentMapId,
                                    player.currentHp, player.maxHp,
                                    player.currentArmor, player.maxArmor,
                                    player.currentMovementPoints, player.maxMovementPoints,
                                    player.currentRolls, player.maxRolls,
                                    player.activeWeaponIndex, player.rerollCoins,
                                    player.statusEffects, player.isDead);
                                success = true;
                            }
                        });
                        string resp = success ? "{\"status\":\"success\"}" : "{\"status\":\"failed\"}";
                        SendResponse(stream, 200, "OK", "application/json", Encoding.UTF8.GetBytes(resp));
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[WebServerManager] switch-weapon error: {ex}");
                    SendResponse(stream, 500, "Internal Server Error", "text/plain", Encoding.UTF8.GetBytes(ex.Message));
                }
                return;
            }

            if (method == "POST" && url == "/api/action/end-turn")
            {
                try
                {
                    var payload = JsonUtility.FromJson<EndTurnPayload>(requestStr);
                    if (payload != null && !string.IsNullOrEmpty(payload.playerId))
                    {
                        bool success = false;
                        ExecuteOnMainThreadBlocking(() => {
                            if (RPGTable.Runtime.CampaignGameSession.IsCombatActive 
                                && RPGTable.Runtime.CombatManager.Instance != null
                                && RPGTable.Runtime.CombatManager.Instance.ActiveToken != null
                                && RPGTable.Runtime.CombatManager.Instance.ActiveToken.PlayerId == payload.playerId)
                            {
                                RPGTable.Runtime.CombatManager.Instance.EndTokenTurn();
                                success = true;
                            }
                        });
                        string resp = success ? "{\"status\":\"success\"}" : "{\"status\":\"failed\"}";
                        SendResponse(stream, 200, "OK", "application/json", Encoding.UTF8.GetBytes(resp));
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[WebServerManager] end-turn error: {ex}");
                    SendResponse(stream, 500, "Internal Server Error", "text/plain", Encoding.UTF8.GetBytes(ex.Message));
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
