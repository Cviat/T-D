using System.Linq;
using RPGTable.Core;
using RPGTable.GameMaster;
using UnityEngine;

namespace RPGTable.UI
{
    public sealed class PrototypeHud : MonoBehaviour
    {
        private GUIStyle panelStyle;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;

        private void OnGUI()
        {
            EnsureStyles();

            GUILayout.BeginArea(new Rect(16, 16, 320, 360), panelStyle);
            GUILayout.Label("RPG Table Prototype", titleStyle);

            var viewMode = ViewModeController.Instance != null
                ? ViewModeController.Instance.Mode.ToString()
                : "Unknown";

            GUILayout.Label($"View: {viewMode}", bodyStyle);
            GUILayout.Label("Tab: switch Master / Player", bodyStyle);
            GUILayout.Label("Drag tokens in Master View", bodyStyle);
            GUILayout.Space(12);
            GUILayout.Label("Initiative", titleStyle);

            foreach (var token in FindObjectsByType<BoardToken>(FindObjectsInactive.Exclude).OrderByDescending(t => t.initiative))
            {
                GUILayout.Label($"{token.initiative:00}  {token.displayName}  ({token.team})", bodyStyle);
            }

            GUILayout.Space(12);
            GUILayout.Label("First slice: grid, tokens, visibility, fog, initiative.", bodyStyle);
            GUILayout.EndArea();
        }

        private void EnsureStyles()
        {
            if (panelStyle != null)
            {
                return;
            }

            panelStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(14, 14, 12, 12)
            };

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 16,
                normal = { textColor = Color.white }
            };

            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                normal = { textColor = Color.white }
            };
        }
    }
}
