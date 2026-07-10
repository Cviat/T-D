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

        public List<CampaignRuntimeToken> Queue { get; } = new List<CampaignRuntimeToken>();
        public int ActiveTokenIndex { get; private set; } = -1;
        public int CurrentTurnNumber { get; private set; } = 1;

        public CampaignRuntimeToken ActiveToken
        {
            get
            {
                if (ActiveTokenIndex >= 0 && ActiveTokenIndex < Queue.Count)
                {
                    return Queue[ActiveTokenIndex];
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

        public void StartCombat()
        {
            CampaignGameSession.IsCombatActive = true;
            CurrentTurnNumber = 1;
            ActiveTokenIndex = -1;

            Queue.Clear();
            var allTokens = GameObject.FindObjectsOfType<CampaignRuntimeToken>();
            foreach (var token in allTokens)
            {
                if (!token.IsDead)
                {
                    Queue.Add(token);
                }
            }

            // Sort by initiative (from BoardToken initiative field) descending
            Queue.Sort((a, b) => {
                var btA = a.GetComponent<BoardToken>();
                var btB = b.GetComponent<BoardToken>();
                int initA = btA != null ? btA.initiative : 0;
                int initB = btB != null ? btB.initiative : 0;
                return initB.CompareTo(initA);
            });

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

        public void StartTokenTurn(CampaignRuntimeToken token)
        {
            if (token == null || token.IsDead)
            {
                EndTokenTurn();
                return;
            }

            // Restore action rolls and movement points
            token.CurrentMovementPoints = token.MaxMovementPoints;
            token.CurrentRolls = token.MaxRolls;

            // Pan camera to the active token
            FocusCameraOn(token.transform.position);

            // Tick and apply status effects
            ProcessStatusEffects(token);

            // Check if token died from status ticks or is stunned
            if (token.IsDead)
            {
                EndTokenTurn();
                return;
            }

            bool isStunned = false;
            foreach (var effect in token.statusEffects)
            {
                if (string.Equals(effect.effectName, "Stun", StringComparison.OrdinalIgnoreCase) || string.Equals(effect.effectName, "Оглушение", StringComparison.OrdinalIgnoreCase))
                {
                    isStunned = true;
                }
            }

            if (isStunned)
            {
                token.CurrentRolls = 0;
                token.CurrentMovementPoints = 0;
            }

            // Auto-select token to update inspector and highlights
#if UNITY_2023_1_OR_NEWER
            var loader = FindFirstObjectByType<CampaignGameLoader>();
#else
            var loader = FindObjectOfType<CampaignGameLoader>();
#endif
            if (loader != null)
            {
                loader.SelectRuntimeToken(token);
            }

            // If stunned/has no rolls left, auto-end turn after a delay
            if (token.CurrentRolls <= 0)
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

            ActiveTokenIndex++;
            if (ActiveTokenIndex >= Queue.Count)
            {
                ActiveTokenIndex = 0;
                CurrentTurnNumber++;
                
                // Show turn animation
                SpawnTextBanner($"ХОД {CurrentTurnNumber}", new Color(1f, 0.84f, 0f, 1f), 2.0f, true);
            }

            // Skip dead tokens
            var nextToken = ActiveToken;
            if (nextToken != null && nextToken.IsDead)
            {
                EndTokenTurn();
                return;
            }

            var ui = GameObject.FindAnyObjectByType<CampaignGameLoader>()?.UI;
            if (ui != null)
            {
                ui.RefreshCombatUI();
            }

            if (nextToken != null)
            {
                StartTokenTurn(nextToken);
            }
        }

        private void ProcessStatusEffects(CampaignRuntimeToken token)
        {
            for (int i = token.statusEffects.Count - 1; i >= 0; i--)
            {
                var effect = token.statusEffects[i];
                
                // Apply ticking damage/effect dynamically based on affectedStat
                if (string.Equals(effect.affectedStat, "HP", StringComparison.OrdinalIgnoreCase))
                {
                    token.CurrentHp = Mathf.Clamp(token.CurrentHp + effect.value, 0, token.MaxHp);
                    if (token.CurrentHp <= 0)
                    {
                        token.IsDead = true;
#if UNITY_2023_1_OR_NEWER
                        var loader = FindFirstObjectByType<CampaignGameLoader>();
#else
                        var loader = FindObjectOfType<CampaignGameLoader>();
#endif
                        if (loader != null) loader.KillRuntimeToken(token);
                    }
                }
                else if (string.Equals(effect.affectedStat, "MovementPoints", StringComparison.OrdinalIgnoreCase))
                {
                    token.CurrentMovementPoints = Mathf.Max(0, token.CurrentMovementPoints + effect.value);
                }
                else if (string.Equals(effect.affectedStat, "Rolls", StringComparison.OrdinalIgnoreCase))
                {
                    token.CurrentRolls = Mathf.Max(0, token.CurrentRolls + effect.value);
                }
                else if (string.Equals(effect.affectedStat, "Armor", StringComparison.OrdinalIgnoreCase))
                {
                    token.CurrentArmor = Mathf.Clamp(token.CurrentArmor + effect.value, 0, token.MaxArmor);
                }

                // Decrement duration
                effect.durationTurns--;
                if (effect.durationTurns <= 0)
                {
                    token.statusEffects.RemoveAt(i);
                }
            }
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
    }
}
