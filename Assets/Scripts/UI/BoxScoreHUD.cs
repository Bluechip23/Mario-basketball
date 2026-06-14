using UnityEngine;
using MarioBasketball.Core;

namespace MarioBasketball.UI
{
    /// <summary>
    /// The optional live on-court box score, shown in the top-right while a match
    /// is running when <see cref="GameSettings.ShowBoxScore"/> is on (toggled from
    /// the Settings screen, reachable off the start menu). A throwaway IMGUI panel
    /// like the rest of the prototype HUD; built mainly to make testing legible.
    /// </summary>
    public class BoxScoreHUD : MonoBehaviour
    {
        GUIStyle _title;
        GUIStyle _header;
        GUIStyle _row;

        void OnGUI()
        {
            if (!GameSettings.ShowBoxScore) return;
            var gm = GameManager.Instance;
            if (gm == null) return;
            EnsureStyles();

            float w = 470f, h = 340f;
            var area = new Rect(Screen.width - w - 12f, 150f, w, h);

            var prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = prev;

            GUILayout.BeginArea(new Rect(area.x + 10, area.y + 8, area.width - 20, area.height - 16));
            GUILayout.Label("BOX SCORE", _title);
            BoxScoreView.Draw(gm, _header, _row);
            GUILayout.EndArea();
        }

        void EnsureStyles()
        {
            _title ??= new GUIStyle(GUI.skin.label)
            { fontSize = 15, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            _header ??= new GUIStyle(GUI.skin.label)
            { fontSize = 12, fontStyle = FontStyle.Bold, normal = { textColor = new Color(1f, 0.95f, 0.6f) } };
            _row ??= new GUIStyle(GUI.skin.label)
            { fontSize = 12, normal = { textColor = Color.white } };
        }
    }
}
