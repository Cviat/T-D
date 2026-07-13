using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RPGTable.Core;

namespace RPGTable.Runtime
{
    public sealed class CombatManager : MonoBehaviour
    {
        private static CombatManager instance;
        public static CombatManager Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = GameObject.Find("CombatManager");
                    if (go == null)
                    {
                        go = new GameObject("CombatManager");
                        DontDestroyOnLoad(go);
                    }
                    instance = go.AddComponent<CombatManager>();
                }
                return instance;
            }
        }

        public List<string> Queue { get; } = new List<string>();
        public int ActiveTokenIndex { get; private set; } = -1;
        public int CurrentTurnNumber { get; private set; } = 1;
        private readonly Dictionary<int, int> floatingTextStacks = new Dictionary<int, int>();

        public string ActiveTokenId
        {
            get
            {
                if (ActiveTokenIndex >= 0 && ActiveTokenIndex < Queue.Count)
                {
                    return Queue[ActiveTokenIndex];
                }
                return "";
            }
        }

        public CampaignRuntimeToken ActiveToken
        {
            get
            {
                var id = ActiveTokenId;
                if (string.IsNullOrEmpty(id)) return null;
                var tokens = GameObject.FindObjectsByType<CampaignRuntimeToken>(FindObjectsInactive.Exclude);
                foreach (var t in tokens)
                {
                    if (!t.IsPlayerViewClone && t.RuntimeId == id) return t;
                }
                return null;
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
        }

        public void StartCombat(string initiatorId = "")
        {
            CampaignGameSession.IsCombatActive = true;
            CurrentTurnNumber = 1;
            ActiveTokenIndex = -1;

            Queue.Clear();
            var allTokens = GameObject.FindObjectsByType<CampaignRuntimeToken>(FindObjectsInactive.Exclude);
            var sortedList = new List<CampaignRuntimeToken>();
            foreach (var token in allTokens)
            {
                if (!token.IsPlayerViewClone && !token.IsDead)
                {
                    sortedList.Add(token);
                }
            }

            // Sort by initiative (from BoardToken initiative field) descending
            sortedList.Sort((a, b) => {
                var btA = a.GetComponent<BoardToken>();
                var btB = b.GetComponent<BoardToken>();
                int initA = btA != null ? btA.initiative : 0;
                int initB = btB != null ? btB.initiative : 0;
                return initB.CompareTo(initA);
            });

            var loader = GameObject.FindAnyObjectByType<CampaignGameLoader>();
            foreach (var token in sortedList)
            {
                string id = string.IsNullOrEmpty(token.PlayerId) ? token.RuntimeId : token.PlayerId;
                Queue.Add(id);

                string mapId = loader != null && loader.Context != null && loader.Context.CurrentMapNode != null ? loader.Context.CurrentMapNode.id : "";
                int hp, maxHp, armor, maxArmor, movement, maxMovement, rolls, maxRolls, activeWeapon, rerollCoins;
                List<RPGTable.Core.ActiveStatusEffect> statusEffects;
                bool dead;

                if (CampaignGameSession.TryGetTokenCombatStats(id, mapId, out hp, out maxHp, out armor, out maxArmor, out movement, out maxMovement, out rolls, out maxRolls, out activeWeapon, out rerollCoins, out statusEffects, out dead))
                {
                    CampaignGameSession.UpdateTokenCombatStats(
                        id, mapId,
                        hp, maxHp,
                        armor, maxArmor,
                        3, 3, // Set movement points to 3 at start of combat
                        rolls, maxRolls,
                        activeWeapon, rerollCoins,
                        statusEffects, dead
                    );
                }
            }

            if (!string.IsNullOrEmpty(initiatorId))
            {
                int index = Queue.IndexOf(initiatorId);
                if (index > 0)
                {
                    Queue.RemoveAt(index);
                    Queue.Insert(0, initiatorId);
                }
            }

            if (!HasOpposingLivingSides())
            {
                EndCombat();
                return;
            }

            // Refresh UI
            var ui = GameObject.FindAnyObjectByType<CampaignGameLoader>()?.UI;
            if (ui != null)
            {
                ui.RefreshCombatUI();
            }

            // Play animations
            SpawnTextBanner("В БОЙ!", new Color(0.85f, 0.15f, 0.15f, 1f), 1.5f, false);
            StartCoroutine(DelayedTurnBanner(1.5f));
        }

        private IEnumerator DelayedTurnBanner(float delay)
        {
            yield return new WaitForSeconds(delay);
            SpawnTextBanner($"ХОД {CurrentTurnNumber}", new Color(1f, 0.84f, 0f, 1f), 2.0f, true);
            
            if (Queue.Count > 0)
            {
                ActiveTokenIndex = 0;
                StartTokenTurn(Queue[0]);
            }
        }

        public void EndCombat()
        {
            CampaignGameSession.IsCombatActive = false;
            Queue.Clear();
            ActiveTokenIndex = -1;

            if (RPGTable.Board.GridHighlighter.Instance != null)
            {
                RPGTable.Board.GridHighlighter.Instance.Clear();
            }

            var ui = GameObject.FindAnyObjectByType<CampaignGameLoader>()?.UI;
            if (ui != null)
            {
                ui.RefreshCombatUI();
                if (ui.UIInstanceSelectedToken != null)
                {
                    ui.RefreshEntityInspector(ui.UIInstanceSelectedToken);
                }
            }
        }

        public void StartTokenTurn(string tokenId)
        {
            if (string.IsNullOrEmpty(tokenId))
            {
                EndTokenTurn();
                return;
            }

            var loader = FindAnyObjectByType<CampaignGameLoader>();
            string mapId = loader != null && loader.Context != null && loader.Context.CurrentMapNode != null ? loader.Context.CurrentMapNode.id : "";

            int hp, maxHp, armor, maxArmor, movement, maxMovement, rolls, maxRolls, activeWeapon, rerollCoins;
            List<RPGTable.Core.ActiveStatusEffect> statusEffects;
            bool dead;

            if (!CampaignGameSession.TryGetTokenCombatStats(tokenId, mapId, out hp, out maxHp, out armor, out maxArmor, out movement, out maxMovement, out rolls, out maxRolls, out activeWeapon, out rerollCoins, out statusEffects, out dead))
            {
                EndTokenTurn();
                return;
            }

            if (dead)
            {
                EndTokenTurn();
                return;
            }

            // Restore action rolls and movement points
            movement = maxMovement;
            rolls = maxRolls;

            // Tick and apply status effects
            statusEffects = ProcessStatusEffectsData(statusEffects, ref hp, maxHp, ref armor, maxArmor, ref movement, ref rolls, ref dead);

            CampaignGameSession.UpdateTokenCombatStats(tokenId, mapId, hp, maxHp, armor, maxArmor, movement, maxMovement, rolls, maxRolls, activeWeapon, rerollCoins, statusEffects, dead);

            if (dead)
            {
                EndTokenTurn();
                return;
            }

            // Pan camera to the active token
            var tokenVisual = ActiveToken;
            if (tokenVisual != null)
            {
                FocusCameraOn(tokenVisual.transform.position);
                if (loader != null)
                {
                    loader.SelectRuntimeToken(tokenVisual);
                }
            }

            // If stunned/has no rolls left, auto-end turn after a delay
            if (rolls <= 0)
            {
                StartCoroutine(DelayedEndTurn(1.5f));
            }
        }

        private IEnumerator DelayedEndTurn(float delay)
        {
            yield return new WaitForSeconds(delay);
            EndTokenTurn();
        }

        public void EndTokenTurn()
        {
            if (!CampaignGameSession.IsCombatActive) return;
            if (!HasOpposingLivingSides())
            {
                EndCombat();
                return;
            }
            RemoveInvalidQueueEntries();
            if (Queue.Count == 0)
            {
                EndCombat();
                return;
            }

            ActiveTokenIndex++;
            if (ActiveTokenIndex >= Queue.Count)
            {
                ActiveTokenIndex = 0;
                CurrentTurnNumber++;
                
                // Show turn animation
                SpawnTextBanner($"ХОД {CurrentTurnNumber}", new Color(1f, 0.84f, 0f, 1f), 2.0f, true);
            }

            // Skip dead tokens
            var nextTokenId = ActiveTokenId;
            var tokenVisual = ActiveToken;
            if (tokenVisual != null && tokenVisual.IsDead)
            {
                EndCombat();
                return;
            }

            var ui = GameObject.FindAnyObjectByType<CampaignGameLoader>()?.UI;
            if (ui != null)
            {
                ui.RefreshCombatUI();
            }

            if (!string.IsNullOrEmpty(nextTokenId))
            {
                StartTokenTurn(nextTokenId);
            }
        }

        private void RemoveInvalidQueueEntries()
        {
            Queue.RemoveAll(id => {
                var tokens = GameObject.FindObjectsByType<CampaignRuntimeToken>(FindObjectsInactive.Exclude);
                CampaignRuntimeToken activeToken = null;
                foreach (var t in tokens)
                {
                    if (!t.IsPlayerViewClone && (t.RuntimeId == id || t.PlayerId == id))
                    {
                        activeToken = t;
                        break;
                    }
                }

                if (activeToken != null)
                {
                    return activeToken.IsDead || activeToken.CurrentHp <= 0;
                }

                var player = CampaignGameSession.FindPlayer(id);
                if (player != null) return player.isDead || player.currentHp <= 0;
                var loader = FindAnyObjectByType<CampaignGameLoader>();
                string mapId = loader != null && loader.Context != null && loader.Context.CurrentMapNode != null ? loader.Context.CurrentMapNode.id : "";
                var npc = CampaignGameSession.FindNPCState(mapId, id);
                if (npc != null) return npc.isDead || npc.currentHp <= 0;
                return true;
            });

            if (ActiveTokenIndex >= Queue.Count)
            {
                ActiveTokenIndex = Queue.Count - 1;
            }
        }

        private bool HasOpposingLivingSides()
        {
            var aliveFactions = new System.Collections.Generic.HashSet<string>();

            foreach (var p in CampaignGameSession.CurrentPlayers)
            {
                if (!p.isDead && p.currentHp > 0)
                {
                    aliveFactions.Add("Party");
                }
            }

            var allTokens = GameObject.FindObjectsByType<CampaignRuntimeToken>(FindObjectsInactive.Exclude);
            foreach (var t in allTokens)
            {
                if (t != null && !t.IsPlayerViewClone && !t.IsDead)
                {
                    if (t.Team == TokenTeam.Player || t.Team == TokenTeam.Ally)
                    {
                        aliveFactions.Add("Party");
                    }
                    else if (t.Team == TokenTeam.Enemy)
                    {
                        aliveFactions.Add("Enemy");
                    }
                    else if (t.Team == TokenTeam.Neutral)
                    {
                        aliveFactions.Add("Neutral");
                    }
                }
            }

            var loader = FindAnyObjectByType<CampaignGameLoader>();
            string mapId = loader != null && loader.Context != null && loader.Context.CurrentMapNode != null ? loader.Context.CurrentMapNode.id : "";
            if (!string.IsNullOrEmpty(mapId) && CampaignGameSession.MapTokenStates.TryGetValue(mapId, out var npcList))
            {
                foreach (var npc in npcList)
                {
                    if (npc != null && !npc.isDead && npc.currentHp > 0)
                    {
                        if (npc.team == TokenTeam.Player || npc.team == TokenTeam.Ally)
                        {
                            aliveFactions.Add("Party");
                        }
                        else if (npc.team == TokenTeam.Enemy)
                        {
                            aliveFactions.Add("Enemy");
                        }
                        else if (npc.team == TokenTeam.Neutral)
                        {
                            aliveFactions.Add("Neutral");
                        }
                    }
                }
            }

            return aliveFactions.Count > 1;
        }

        private List<RPGTable.Core.ActiveStatusEffect> ProcessStatusEffectsData(
            List<RPGTable.Core.ActiveStatusEffect> effects,
            ref int hp, int maxHp,
            ref int armor, int maxArmor,
            ref int movement,
            ref int rolls,
            ref bool dead)
        {
            var nextEffects = new List<RPGTable.Core.ActiveStatusEffect>(effects);
            for (int i = nextEffects.Count - 1; i >= 0; i--)
            {
                var effect = nextEffects[i];
                switch (effect.affectedStat)
                {
                    case CombatAttributeStat.HP:
                        hp = Mathf.Clamp(hp + effect.value, 0, maxHp);
                        if (hp <= 0) dead = true;
                        break;
                    case CombatAttributeStat.MovementPoints:
                        movement = Mathf.Max(0, movement + effect.value);
                        break;
                    case CombatAttributeStat.Rolls:
                        rolls = Mathf.Max(0, rolls + effect.value);
                        break;
                    case CombatAttributeStat.Armor:
                        armor = Mathf.Clamp(armor + effect.value, 0, maxArmor);
                        break;
                }

                effect.durationTurns--;
                if (effect.durationTurns <= 0)
                {
                    nextEffects.RemoveAt(i);
                }
            }
            return nextEffects;
        }

        private void FocusCameraOn(Vector3 position)
        {
            var cam = Camera.main;
            if (cam != null)
            {
                Vector3 targetPos = new Vector3(position.x, position.y, cam.transform.position.z);
                StartCoroutine(SmoothPanCamera(cam.transform, targetPos));
            }

            // Pan player view camera as well
            var pvCamObj = GameObject.Find("Player View Camera");
            if (pvCamObj != null)
            {
                var pvCam = pvCamObj.GetComponent<Camera>();
                if (pvCam != null)
                {
                    Vector3 targetPos = new Vector3(position.x, position.y, pvCam.transform.position.z);
                    StartCoroutine(SmoothPanCamera(pvCam.transform, targetPos));
                }
            }
        }

        private IEnumerator SmoothPanCamera(Transform camTransform, Vector3 targetPos)
        {
            float elapsed = 0f;
            float duration = 0.5f;
            Vector3 startPos = camTransform.position;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                camTransform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
                yield return null;
            }
            camTransform.position = targetPos;
        }

        public void SpawnTextBanner(string message, Color color, float duration, bool slide)
        {
            var canvasGo = GameObject.Find("CombatBannerCanvas");
            if (canvasGo == null)
            {
                canvasGo = new GameObject("CombatBannerCanvas", typeof(RectTransform));
                var canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 999;
                canvasGo.AddComponent<CanvasScaler>();
                canvasGo.AddComponent<GraphicRaycaster>();
            }

            CreateBannerOnCanvas(canvasGo, message, color, duration, slide);

            var pvCanvasGo = GameObject.Find("Player View Interface");
            if (pvCanvasGo != null)
            {
                CreateBannerOnCanvas(pvCanvasGo, message, color, duration, slide);
            }
        }

        public void SpawnFloatingText(Vector3 worldPosition, string message, Color color, float duration = 1.1f, float fontSize = 24f, int layer = -1, int stackKey = 0)
        {
            var stackIndex = ReserveFloatingTextStack(stackKey);
            var side = stackIndex % 2 == 0 ? -1f : 1f;
            var lane = stackIndex / 2;
            var offset = new Vector3(
                side * (0.18f + lane * 0.22f) + UnityEngine.Random.Range(-0.06f, 0.06f),
                stackIndex * 0.28f,
                -0.7f);
            var textObject = CreateFloatingTextObject("FloatingCombatText", worldPosition + offset, message, color, fontSize);
            if (layer >= 0)
            {
                textObject.layer = layer;
            }
            StartCoroutine(AnimateFloatingText(textObject, duration, stackKey));
        }

        private int ReserveFloatingTextStack(int stackKey)
        {
            if (stackKey == 0)
            {
                return 0;
            }

            floatingTextStacks.TryGetValue(stackKey, out var stackIndex);
            floatingTextStacks[stackKey] = stackIndex + 1;
            return stackIndex;
        }

        private static GameObject CreateFloatingTextObject(string name, Vector3 position, string message, Color color, float fontSize)
        {
            var go = new GameObject(name);
            go.transform.position = position;

            var text = go.AddComponent<TextMesh>();
            text.text = message;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = Mathf.RoundToInt(fontSize);
            text.characterSize = 0.16f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = color;

            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = 500;
            }

            return go;
        }

        private IEnumerator AnimateFloatingText(GameObject textObject, float duration, int stackKey)
        {
            if (textObject == null)
            {
                yield break;
            }

            var start = textObject.transform.position;
            var end = start + new Vector3(0f, 0.75f, 0f);
            var text = textObject.GetComponent<TextMesh>();
            var startColor = text != null ? text.color : Color.white;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                textObject.transform.position = Vector3.Lerp(start, end, t);

                if (text != null)
                {
                    var alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Max(0f, (t - 0.35f) / 0.65f));
                    text.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                }

                yield return null;
            }

            Destroy(textObject);

            if (stackKey != 0 && floatingTextStacks.TryGetValue(stackKey, out var stackCount))
            {
                stackCount = Mathf.Max(0, stackCount - 1);
                if (stackCount == 0)
                {
                    floatingTextStacks.Remove(stackKey);
                }
                else
                {
                    floatingTextStacks[stackKey] = stackCount;
                }
            }
        }

        private void CreateBannerOnCanvas(GameObject canvasGo, string message, Color color, float duration, bool slide)
        {
            var bannerGo = new GameObject("BannerText", typeof(RectTransform));
            bannerGo.transform.SetParent(canvasGo.transform, false);
            var rect = bannerGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(800f, 150f);

            var text = bannerGo.AddComponent<Text>();
            text.text = message;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontStyle = FontStyle.Bold;
            text.fontSize = 72;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            
            var outline = bannerGo.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(3f, -3f);

            StartCoroutine(AnimateBanner(bannerGo, duration, slide));
        }

        private IEnumerator AnimateBanner(GameObject banner, float duration, bool slide)
        {
            var rect = banner.GetComponent<RectTransform>();
            var text = banner.GetComponent<Text>();
            float elapsed = 0f;

            Vector2 startPos = slide ? new Vector2(-1500f, 0f) : Vector2.zero;
            Vector2 endPos = slide ? new Vector2(1500f, 0f) : Vector2.zero;
            rect.anchoredPosition = startPos;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                if (slide)
                {
                    rect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                }
                else
                {
                    rect.localScale = Vector3.Lerp(Vector3.one * 0.5f, Vector3.one * 1.5f, t);
                    text.color = new Color(text.color.r, text.color.g, text.color.b, 1f - t);
                }
                yield return null;
            }

            Destroy(banner);
        }

        public void SpawnFloatingStatusIcon(Vector3 position, Sprite sprite, float duration = 1.4f)
        {
            if (sprite == null) return;

            var go = new GameObject("FloatingStatusIcon");
            go.transform.position = position + new Vector3(0f, 1.2f, -0.6f); // Offset above head

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 500; // Render on top of tokens/map

            // Make the icon a nice size (e.g., 0.5 units wide/high)
            float scale = 0.5f / Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
            go.transform.localScale = new Vector3(scale, scale, 1f);

            // Also mirror on Player View Layer 31
            var pvGo = Instantiate(go, go.transform.position, Quaternion.identity);
            pvGo.name = "PlayerViewFloatingStatusIcon";
            pvGo.layer = 31; // Player view layer
            pvGo.transform.localScale = go.transform.localScale;

            StartCoroutine(AnimateFloatingIcon(go, pvGo, duration));
        }

        private IEnumerator AnimateFloatingIcon(GameObject mainIcon, GameObject pvIcon, float duration)
        {
            var start = mainIcon.transform.position;
            var end = start + new Vector3(0f, 0.9f, 0f);
            var elapsed = 0f;

            var sr1 = mainIcon.GetComponent<SpriteRenderer>();
            var sr2 = pvIcon != null ? pvIcon.GetComponent<SpriteRenderer>() : null;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                
                var currentPos = Vector3.Lerp(start, end, t);
                if (mainIcon != null) mainIcon.transform.position = currentPos;
                if (pvIcon != null) pvIcon.transform.position = currentPos;

                float alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Max(0f, (t - 0.25f) / 0.75f));
                
                if (sr1 != null) sr1.color = new Color(sr1.color.r, sr1.color.g, sr1.color.b, alpha);
                if (sr2 != null) sr2.color = new Color(sr2.color.r, sr2.color.g, sr2.color.b, alpha);

                yield return null;
            }

            if (mainIcon != null) Destroy(mainIcon);
            if (pvIcon != null) Destroy(pvIcon);
        }
    }
}
