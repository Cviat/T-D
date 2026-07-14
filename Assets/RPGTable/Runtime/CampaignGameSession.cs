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
        public int currentArmor;
        public int maxArmor;
        public int rerollCoins = 3;
        public int currentMovementPoints = 3;
        public int maxMovementPoints = 3;
        public int currentRolls = 1;
        public int maxRolls = 1;
        public int activeWeaponIndex = 0;
        public List<RPGTable.Core.ActiveStatusEffect> statusEffects = new List<RPGTable.Core.ActiveStatusEffect>();
        public RPGTable.CharacterEditor.SavedCharacterData characterRuntimeData;
    }

    public static class CampaignGameSession
    {
        [Serializable]
        public class RuntimeMapTokenState
        {
            public string runtimeId;
            public string displayName;
            public string characterPath;
            public string tokenPath;
            public RPGTable.Core.TokenTeam team;
            public bool visibleToPlayers;
            public UnityEngine.Vector2Int gridPosition;
            public bool isDead;
            public int currentHp;
            public int maxHp;
            public int currentArmor;
            public int maxArmor;
            public int currentMovementPoints = 3;
            public int maxMovementPoints = 3;
            public int currentRolls = 1;
            public int maxRolls = 1;
            public int activeWeaponIndex = 0;
            public List<RPGTable.Core.ActiveStatusEffect> statusEffects = new List<RPGTable.Core.ActiveStatusEffect>();
        }

        public static readonly Dictionary<string, List<RuntimeMapTokenState>> MapTokenStates = 
            new Dictionary<string, List<RuntimeMapTokenState>>();

        public static event Action<string, string, int, int, bool> OnTokenDataChanged;
        public static event Action<string, string, UnityEngine.Vector2Int> OnTokenPositionChanged;
        public static event Action<string, string, string, string> OnTokenActionTriggered;
        public static event Action<string> OnTokenFocused;

        private static readonly List<CampaignPlayerData> Players = new List<CampaignPlayerData>();
        private static Dictionary<string, RPGTable.Core.AbilityCard> abilityCardCache;
        private static int nextPlayerIndex = 1;

        public static event Action OnPlayersChanged;

        public static void TriggerPlayersChanged()
        {
            OnPlayersChanged?.Invoke();
        }

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
                avatarResourceName = "DefaultPlayer",
                rerollCoins = 3
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
            var charData = string.IsNullOrWhiteSpace(characterPath) ? null : RPGTable.CharacterEditor.UserCharacterStore.LoadCharacter(characterPath);
            EnsureCharacterProgressInitialized(charData);
            var player = new CampaignPlayerData
            {
                id = $"player_{nextPlayerIndex++}",
                name = string.IsNullOrWhiteSpace(characterName) ? "Player" : characterName,
                avatarResourceName = "DefaultPlayer",
                characterPath = characterPath,
                portraitPath = portraitPath,
                tokenPath = tokenPath,
                rerollCoins = charData != null ? charData.rerollCoins : 3,
                maxHp = charData != null ? charData.maxHp : 10,
                currentHp = charData != null ? charData.maxHp : 10,
                maxArmor = charData != null ? charData.maxArmor : 0,
                currentArmor = charData != null ? charData.maxArmor : 0,
                characterRuntimeData = charData
            };

            Players.Add(player);
            OnPlayersChanged?.Invoke();
            return player;
        }

        public static CampaignPlayerData CreateCharacterForPlayer(string playerId)
        {
            var player = FindPlayer(playerId);

            if (player == null)
            {
                return null;
            }

            if (player.characterRuntimeData == null)
            {
                if (string.IsNullOrWhiteSpace(player.tokenPath) && !string.IsNullOrWhiteSpace(player.portraitPath))
                {
                    player.tokenPath = RPGTable.TokenEditor.UserTokenStore.SaveToken(
                        player.name,
                        new RPGTable.TokenEditor.SavedTokenData
                        {
                            name = player.name,
                            portraitPath = player.portraitPath,
                            footprintSize = 1
                        });
                }

                var data = new RPGTable.CharacterEditor.SavedCharacterData
                {
                    name = string.IsNullOrWhiteSpace(player.name) ? "Player" : player.name,
                    portraitPath = player.portraitPath,
                    tokenPath = player.tokenPath,
                    level = 1,
                    maxHp = 10,
                    maxArmor = 0
                };
                EnsureCharacterProgressInitialized(data);

                player.characterRuntimeData = data;
                player.characterPath = RPGTable.CharacterEditor.UserCharacterStore.SaveCharacter(data.name, data);
                player.tokenPath = data.tokenPath;
                player.maxHp = data.maxHp;
                player.currentHp = data.maxHp;
                player.maxArmor = data.maxArmor;
                player.currentArmor = data.maxArmor;
                player.rerollCoins = data.rerollCoins;
                player.isReady = !string.IsNullOrWhiteSpace(player.tokenPath);
            }
            else
            {
                EnsureCharacterProgressInitialized(player.characterRuntimeData);
            }

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
                isReady = false,
                rerollCoins = 3,
                maxHp = 10,
                currentHp = 10,
                maxArmor = 0,
                currentArmor = 0
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

            var charData = string.IsNullOrWhiteSpace(characterPath) ? null : RPGTable.CharacterEditor.UserCharacterStore.LoadCharacter(characterPath);
            EnsureCharacterProgressInitialized(charData);
            player.name = string.IsNullOrWhiteSpace(characterName) ? player.name : characterName;
            player.characterPath = characterPath;
            if (!string.IsNullOrWhiteSpace(portraitPath))
            {
                player.portraitPath = portraitPath;
            }
            player.tokenPath = tokenPath;
            player.isReady = !string.IsNullOrWhiteSpace(tokenPath);
            player.rerollCoins = charData != null ? charData.rerollCoins : 3;
            player.maxHp = charData != null ? charData.maxHp : 10;
            player.currentHp = player.maxHp;
            player.maxArmor = charData != null ? charData.maxArmor : 0;
            player.currentArmor = player.maxArmor;
            player.characterRuntimeData = charData;
            OnPlayersChanged?.Invoke();
            return true;
        }

        public static bool IncreaseCharacterAttribute(string playerId, string attribute)
        {
            var player = FindPlayer(playerId);

            if (player == null || player.characterRuntimeData == null || player.characterRuntimeData.attributePoints <= 0)
            {
                return false;
            }

            var data = player.characterRuntimeData;
            switch ((attribute ?? "").Trim().ToUpperInvariant())
            {
                case "STR":
                    data.strength++;
                    break;
                case "AGI":
                    data.agility++;
                    break;
                case "INT":
                    data.intelligence++;
                    break;
                case "HOL":
                    data.holiness++;
                    break;
                default:
                    return false;
            }

            data.attributePoints--;
            OnPlayersChanged?.Invoke();
            return true;
        }

        public static bool AllocateCharacterSkillPoint(string playerId, string pool)
        {
            var player = FindPlayer(playerId);

            if (player == null || player.characterRuntimeData == null || player.characterRuntimeData.skillPoints <= 0)
            {
                return false;
            }

            var data = player.characterRuntimeData;
            switch ((pool ?? "").Trim().ToLowerInvariant())
            {
                case "attack":
                    data.attackSkillPoints++;
                    break;
                case "defense":
                    data.defenseSkillPoints++;
                    break;
                default:
                    return false;
            }

            data.skillPoints--;
            OnPlayersChanged?.Invoke();
            return true;
        }

        public static bool GrantCharacterLevel(string playerId, int levelCount = 1)
        {
            var player = FindPlayer(playerId);

            if (player == null || player.characterRuntimeData == null)
            {
                return false;
            }

            levelCount = Math.Max(1, levelCount);
            ApplyPlayerLevelBonus(player, levelCount);

            OnPlayersChanged?.Invoke();
            return true;
        }

        public static bool GrantCharacterXp(string playerId, int xpAmount)
        {
            var player = FindPlayer(playerId);

            if (player == null || player.characterRuntimeData == null || xpAmount <= 0)
            {
                return false;
            }

            player.characterRuntimeData.xp = Math.Max(0, player.characterRuntimeData.xp + xpAmount);
            ApplyEligibleXpLevels(player);
            OnPlayersChanged?.Invoke();
            return true;
        }

        public static int GetRequiredXpForLevel(int level)
        {
            return Math.Max(2, level) * 100;
        }

        public static int GetRequiredXpForNextLevel(int currentLevel)
        {
            return GetRequiredXpForLevel(Math.Max(1, currentLevel) + 1);
        }

        private static void ApplyEligibleXpLevels(CampaignPlayerData player)
        {
            if (player == null || player.characterRuntimeData == null)
            {
                return;
            }

            var gainedLevels = 0;
            var level = Math.Max(1, player.characterRuntimeData.level);
            var xp = Math.Max(0, player.characterRuntimeData.xp);

            while (xp >= GetRequiredXpForNextLevel(level))
            {
                gainedLevels++;
                level++;
            }

            if (gainedLevels > 0)
            {
                ApplyPlayerLevelBonus(player, gainedLevels);
            }
        }

        private static void ApplyPlayerLevelBonus(CampaignPlayerData player, int levelCount)
        {
            if (player == null || player.characterRuntimeData == null || levelCount <= 0)
            {
                return;
            }

            var hpBonus = levelCount * 5;

            player.characterRuntimeData.level = Math.Max(1, player.characterRuntimeData.level) + levelCount;
            player.characterRuntimeData.attributePoints += levelCount * 3;
            player.characterRuntimeData.skillPoints += levelCount;
            player.characterRuntimeData.maxHp = Math.Max(1, player.characterRuntimeData.maxHp) + hpBonus;

            player.maxHp = Math.Max(1, player.maxHp) + hpBonus;
            if (!player.isDead)
            {
                player.currentHp = Math.Min(player.currentHp + hpBonus, player.maxHp);
            }

            UpdateTokenCombatStats(
                player.id,
                player.currentMapId,
                player.currentHp,
                player.maxHp,
                player.currentArmor,
                player.maxArmor,
                player.currentMovementPoints,
                player.maxMovementPoints,
                player.currentRolls,
                player.maxRolls,
                player.activeWeaponIndex,
                player.rerollCoins,
                player.statusEffects,
                player.isDead);
        }

        public static void EnsureCharacterProgressInitialized(RPGTable.CharacterEditor.SavedCharacterData data)
        {
            if (data == null)
            {
                return;
            }

            data.level = Math.Max(1, data.level);

            var hasAllocatedPools = data.attackSkillPoints > 0 || data.defenseSkillPoints > 0;
            var hasAbilities = HasAnyFilledSlot(data.attackSlots)
                || HasAnyFilledSlot(data.attack2Slots)
                || HasAnyFilledSlot(data.defenseSlots);

            var startingSkillPoints = 10;
            var totalEarnedSkillPoints = startingSkillPoints + Math.Max(0, data.level - 1);

            if (hasAllocatedPools)
            {
                return;
            }

            if (!hasAbilities)
            {
                data.skillPoints = Math.Max(data.skillPoints, totalEarnedSkillPoints);
                return;
            }

            var attack1Cost = CalculateAbilitySlotsCost(data.attackSlots, false);
            var attack2Cost = CalculateAbilitySlotsCost(data.attack2Slots, false);
            var defenseCost = CalculateAbilitySlotsCost(data.defenseSlots, true);

            data.attackSkillPoints = Math.Max(data.attackSkillPoints, Math.Max(attack1Cost, attack2Cost));
            data.defenseSkillPoints = Math.Max(data.defenseSkillPoints, defenseCost);
            data.skillPoints = Math.Max(data.skillPoints, totalEarnedSkillPoints);
        }

        private static int CalculateAbilitySlotsCost(string[] slots, bool defenseSlots)
        {
            if (slots == null)
            {
                return 0;
            }

            var cost = 0;
            foreach (var abilityName in slots)
            {
                if (string.IsNullOrWhiteSpace(abilityName))
                {
                    continue;
                }

                var card = FindAbilityCard(abilityName);
                if (card == null)
                {
                    continue;
                }

                var isDefenseAbility = card.attackType == RPGTable.Core.AttackType.Defense;
                if (defenseSlots != isDefenseAbility)
                {
                    continue;
                }

                cost += Math.Max(0, card.cost);
            }

            return cost;
        }

        private static RPGTable.Core.AbilityCard FindAbilityCard(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return null;
            }

            if (abilityCardCache == null)
            {
                abilityCardCache = new Dictionary<string, RPGTable.Core.AbilityCard>(StringComparer.OrdinalIgnoreCase);
                var cards = UnityEngine.Resources.LoadAll<RPGTable.Core.AbilityCard>("AbilityCards");
                foreach (var card in cards)
                {
                    if (card != null && !string.IsNullOrWhiteSpace(card.title))
                    {
                        abilityCardCache[card.title] = card;
                    }
                }
            }

            abilityCardCache.TryGetValue(title, out var result);
            return result;
        }

        private static bool HasAnyFilledSlot(string[] slots)
        {
            if (slots == null)
            {
                return false;
            }

            foreach (var slot in slots)
            {
                if (!string.IsNullOrWhiteSpace(slot))
                {
                    return true;
                }
            }

            return false;
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

        public static RuntimeMapTokenState FindNPCState(string mapId, string runtimeId)
        {
            if (string.IsNullOrEmpty(mapId) || string.IsNullOrEmpty(runtimeId)) return null;
            if (MapTokenStates.TryGetValue(mapId, out var list))
            {
                return list.Find(s => s.runtimeId == runtimeId);
            }
            return null;
        }

        public static void UpdateNPCState(string mapId, string runtimeId, int currentHp, int currentArmor, bool isDead)
        {
            var state = FindNPCState(mapId, runtimeId);
            if (state != null)
            {
                state.currentHp = currentHp;
                state.currentArmor = currentArmor;
                state.isDead = isDead;
            }
        }

        public static void UpdateNPCTeam(string mapId, string runtimeId, RPGTable.Core.TokenTeam team)
        {
            var state = FindNPCState(mapId, runtimeId);
            if (state != null)
            {
                state.team = team;
            }
        }

        public static bool TryGetTokenData(string id, string mapId, out int hp, out int maxHp, out int armor, out int maxArmor, out bool dead)
        {
            hp = 0; maxHp = 0; armor = 0; maxArmor = 0; dead = false;

            var player = FindPlayer(id);
            if (player != null)
            {
                hp = player.currentHp;
                maxHp = player.maxHp;
                armor = player.currentArmor;
                maxArmor = player.maxArmor;
                dead = player.isDead;
                return true;
            }

            var npc = FindNPCState(mapId, id);
            if (npc != null)
            {
                hp = npc.currentHp;
                maxHp = npc.maxHp;
                armor = npc.currentArmor;
                maxArmor = npc.maxArmor;
                dead = npc.isDead;
                return true;
            }

            return false;
        }

        public static void UpdateTokenData(string id, string mapId, int hp, int armor, bool dead)
        {
            var player = FindPlayer(id);
            if (player != null)
            {
                player.currentHp = hp;
                player.currentArmor = armor;
                player.isDead = dead;
                OnTokenDataChanged?.Invoke(id, mapId, hp, armor, dead);
                return;
            }

            UpdateNPCState(mapId, id, hp, armor, dead);
            OnTokenChanged(id, mapId, hp, armor, dead);
        }

        private static void OnTokenChanged(string id, string mapId, int hp, int armor, bool dead)
        {
            OnTokenDataChanged?.Invoke(id, mapId, hp, armor, dead);
        }

        public static void MoveToken(string id, string mapId, UnityEngine.Vector2Int nextPos)
        {
            var player = FindPlayer(id);
            if (player != null)
            {
                player.gridX = nextPos.x;
                player.gridY = nextPos.y;
                player.currentMapId = mapId;
                OnTokenPositionChanged?.Invoke(id, mapId, nextPos);
                return;
            }

            var npc = FindNPCState(mapId, id);
            if (npc != null)
            {
                npc.gridPosition = nextPos;
                OnTokenPositionChanged?.Invoke(id, mapId, nextPos);
            }
        }

        public static void TriggerTokenAction(string attackerId, string targetId, string actionType, string details = "")
        {
            OnTokenActionTriggered?.Invoke(attackerId, targetId, actionType, details);
        }

        public static void FocusToken(string id)
        {
            OnTokenFocused?.Invoke(id);
        }

        public static void UpdateTokenCombatStats(
            string id,
            string mapId,
            int hp, int maxHp,
            int armor, int maxArmor,
            int movement, int maxMovement,
            int rolls, int maxRolls,
            int activeWeapon,
            int rerollCoins,
            List<RPGTable.Core.ActiveStatusEffect> statusEffects,
            bool dead)
        {
            var player = FindPlayer(id);
            if (player != null)
            {
                player.currentHp = hp;
                player.maxHp = maxHp;
                player.currentArmor = armor;
                player.maxArmor = maxArmor;
                player.currentMovementPoints = movement;
                player.maxMovementPoints = maxMovement;
                player.currentRolls = rolls;
                player.maxRolls = maxRolls;
                player.activeWeaponIndex = activeWeapon;
                player.rerollCoins = rerollCoins;
                player.statusEffects = statusEffects ?? new List<RPGTable.Core.ActiveStatusEffect>();
                player.isDead = dead;
                OnTokenDataChanged?.Invoke(id, mapId, hp, armor, dead);
                return;
            }

            var npc = FindNPCState(mapId, id);
            if (npc != null)
            {
                npc.currentHp = hp;
                npc.maxHp = maxHp;
                npc.currentArmor = armor;
                npc.maxArmor = maxArmor;
                npc.currentMovementPoints = movement;
                npc.maxMovementPoints = maxMovement;
                npc.currentRolls = rolls;
                npc.maxRolls = maxRolls;
                npc.activeWeaponIndex = activeWeapon;
                npc.statusEffects = statusEffects ?? new List<RPGTable.Core.ActiveStatusEffect>();
                npc.isDead = dead;
                OnTokenDataChanged?.Invoke(id, mapId, hp, armor, dead);
            }
        }

        public static bool TryGetTokenCombatStats(
            string id,
            string mapId,
            out int hp, out int maxHp,
            out int armor, out int maxArmor,
            out int movement, out int maxMovement,
            out int rolls, out int maxRolls,
            out int activeWeapon,
            out int rerollCoins,
            out List<RPGTable.Core.ActiveStatusEffect> statusEffects,
            out bool dead)
        {
            hp = 0; maxHp = 0; armor = 0; maxArmor = 0; dead = false;
            movement = 3; maxMovement = 3; rolls = 1; maxRolls = 1; activeWeapon = 0; rerollCoins = 3;
            statusEffects = new List<RPGTable.Core.ActiveStatusEffect>();

            var player = FindPlayer(id);
            if (player != null)
            {
                hp = player.currentHp;
                maxHp = player.maxHp;
                armor = player.currentArmor;
                maxArmor = player.maxArmor;
                movement = player.currentMovementPoints;
                maxMovement = player.maxMovementPoints;
                rolls = player.currentRolls;
                maxRolls = player.maxRolls;
                activeWeapon = player.activeWeaponIndex;
                rerollCoins = player.rerollCoins;
                statusEffects = player.statusEffects;
                dead = player.isDead;
                return true;
            }

            var npc = FindNPCState(mapId, id);
            if (npc != null)
            {
                hp = npc.currentHp;
                maxHp = npc.maxHp;
                armor = npc.currentArmor;
                maxArmor = npc.maxArmor;
                movement = npc.currentMovementPoints;
                maxMovement = npc.maxMovementPoints;
                rolls = npc.currentRolls;
                maxRolls = npc.maxRolls;
                activeWeapon = npc.activeWeaponIndex;
                rerollCoins = 3;
                statusEffects = npc.statusEffects;
                dead = npc.isDead;
                return true;
            }

            return false;
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
