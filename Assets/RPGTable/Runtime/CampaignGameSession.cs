namespace RPGTable.Runtime
{
    using System;
    using System.Collections.Generic;

    [Serializable]
    public sealed class CampaignPlayerData
    {
        public string id;
        public string name;
        public string avatarResourceName;
        public string characterPath;
        public string portraitPath;
        public string tokenPath;
        public string currentMapId;
        public int gridX;
        public int gridY;
        public bool isDead;
        public bool isReady;
        public int currentHp;
        public int maxHp;
    }

    public static class CampaignGameSession
    {
        private static readonly List<CampaignPlayerData> Players = new List<CampaignPlayerData>();
        private static int nextPlayerIndex = 1;

        public static event Action OnPlayersChanged;

        public static string SelectedCampaignPath { get; set; }
        public static string PendingTokenPlayerId { get; set; }
        public static string TokenEditorReturnSceneName { get; set; }
        public static bool IsCombatActive { get; set; } = false;
        public static string PendingCharacterTokenPath { get; set; }
        public static string PendingCharacterDraftName { get; set; }
        public static string PendingCharacterDraftDescription { get; set; }
        public static string PendingCharacterDraftPortraitPath { get; set; }

        public static IReadOnlyList<CampaignPlayerData> CurrentPlayers => Players;

        public static CampaignPlayerData AddDefaultPlayer()
        {
            var player = new CampaignPlayerData
            {
                id = $"player_{nextPlayerIndex++}",
                name = "Турбослав",
                avatarResourceName = "DefaultPlayer"
            };

            Players.Add(player);
            OnPlayersChanged?.Invoke();
            return player;
        }

        public static CampaignPlayerData AddCharacterPlayer(
            string characterPath,
            string characterName,
            string portraitPath,
            string tokenPath)
        {
            var player = new CampaignPlayerData
            {
                id = $"player_{nextPlayerIndex++}",
                name = string.IsNullOrWhiteSpace(characterName) ? "Player" : characterName,
                avatarResourceName = "DefaultPlayer",
                characterPath = characterPath,
                portraitPath = portraitPath,
                tokenPath = tokenPath
            };

            Players.Add(player);
            OnPlayersChanged?.Invoke();
            return player;
        }

        public static CampaignPlayerData AddRegisteredPlayer(string playerName, string portraitPath)
        {
            var player = new CampaignPlayerData
            {
                id = $"player_{nextPlayerIndex++}",
                name = string.IsNullOrWhiteSpace(playerName) ? "Player" : playerName,
                avatarResourceName = "DefaultPlayer",
                portraitPath = portraitPath,
                isReady = false
            };

            Players.Add(player);
            OnPlayersChanged?.Invoke();
            return player;
        }

        public static bool AssignCharacterToPlayer(
            string playerId,
            string characterPath,
            string characterName,
            string portraitPath,
            string tokenPath)
        {
            var player = FindPlayer(playerId);

            if (player == null)
            {
                return false;
            }

            player.name = string.IsNullOrWhiteSpace(characterName) ? player.name : characterName;
            player.characterPath = characterPath;
            if (!string.IsNullOrWhiteSpace(portraitPath))
            {
                player.portraitPath = portraitPath;
            }
            player.tokenPath = tokenPath;
            player.isReady = !string.IsNullOrWhiteSpace(tokenPath);
            OnPlayersChanged?.Invoke();
            return true;
        }

        public static void SetPlayerReady(string playerId, bool ready)
        {
            var player = FindPlayer(playerId);

            if (player == null)
            {
                return;
            }

            player.isReady = ready && !string.IsNullOrWhiteSpace(player.tokenPath);
            OnPlayersChanged?.Invoke();
        }

        public static CampaignPlayerData FindPlayer(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            foreach (var player in Players)
            {
                if (player.id == id)
                {
                    return player;
                }
            }

            return null;
        }

        public static bool HasPlayersWithoutTokens()
        {
            if (Players.Count == 0)
            {
                return true;
            }

            foreach (var player in Players)
            {
                if (string.IsNullOrWhiteSpace(player.tokenPath))
                {
                    return true;
                }
            }

            return false;
        }

        public static void AssignTokenToPendingPlayer(string tokenPath)
        {
            var player = FindPlayer(PendingTokenPlayerId);

            if (player != null)
            {
                player.tokenPath = tokenPath;
            }

            PendingTokenPlayerId = null;
        }

        public static void RemovePlayer(string playerId)
        {
            var count = Players.RemoveAll(p => p.id == playerId);
            if (count > 0) OnPlayersChanged?.Invoke();
        }

        public static void ClearPlayers()
        {
            if (Players.Count > 0)
            {
                Players.Clear();
                OnPlayersChanged?.Invoke();
            }
        }

        public static void ResetRuntimePositions()
        {
            foreach (var player in Players)
            {
                player.currentMapId = null;
                player.gridX = 0;
                player.gridY = 0;
                player.isDead = false;
            }
        }
    }
}
