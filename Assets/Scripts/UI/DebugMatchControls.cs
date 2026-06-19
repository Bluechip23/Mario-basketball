using UnityEngine;
using UnityEngine.InputSystem;
using MarioBasketball.Core;
using MarioBasketball.Gameplay;

namespace MarioBasketball.UI
{
    /// <summary>
    /// Match shortcuts that don't have full UI yet: call a timeout, and open a
    /// <b>substitution menu</b> for the home team — pick who comes OUT (from the
    /// on-court five) and who comes IN (from the bench), then confirm. Subs are
    /// only legal during a timeout or a quarter break
    /// (<see cref="GameManager.CanSubstitute"/>). Controller/keyboard open it
    /// (D-pad down / Y); the menu itself is clicked with the mouse.
    /// </summary>
    public class DebugMatchControls : MonoBehaviour
    {
        string _message;
        float _messageTimer;
        GUIStyle _style, _title, _col, _btn, _btnSel, _hint;

        bool _subOpen;
        int _outSel = -1; // chosen on-court player to remove
        int _inSel = -1;  // chosen bench player to bring in

        void Update()
        {
            if (_messageTimer > 0f) _messageTimer -= Time.unscaledDeltaTime;
            if (MatchPause.IsPaused || GameManager.Instance == null) return;

            var kb = Keyboard.current;
            var gp = Gamepad.current;

            bool timeout = (kb != null && kb.tKey.wasPressedThisFrame) ||
                           (gp != null && gp.dpad.up.wasPressedThisFrame);
            bool substitute = (kb != null && kb.yKey.wasPressedThisFrame) ||
                              (gp != null && gp.dpad.down.wasPressedThisFrame);
            bool cancel = (kb != null && kb.escapeKey.wasPressedThisFrame) ||
                          (gp != null && gp.bButton.wasPressedThisFrame);

            // D-pad up / T: home timeout (+30 energy to the on-court five).
            if (timeout)
            {
                bool ok = GameManager.Instance.CallTimeout(TeamSide.Home);
                Flash(ok ? "Home timeout" : "No timeouts left");
            }

            // D-pad down / Y: open the substitution menu (timeout / quarter break only).
            if (substitute && !_subOpen)
            {
                if (!GameManager.Instance.CanSubstitute)
                    Flash("Subs only during a timeout or quarter break");
                else { _subOpen = true; _outSel = -1; _inSel = -1; }
            }

            if (_subOpen && (cancel || !GameManager.Instance.CanSubstitute)) _subOpen = false;
        }

        void Flash(string text)
        {
            _message = text;
            _messageTimer = 2.5f;
        }

        void OnGUI()
        {
            EnsureStyles();

            if (_subOpen && GameManager.Instance != null) DrawSubMenu(GameManager.Instance);

            if (_messageTimer > 0f && !string.IsNullOrEmpty(_message))
                GUI.Label(new Rect((Screen.width - 520f) / 2f, Screen.height - 64f, 520f, 28f), _message, _style);
        }

        // Two columns: pick one OUT (on court) and one IN (bench), then Substitute.
        void DrawSubMenu(GameManager gm)
        {
            var team = gm.Home;
            const float w = 560f, h = 360f;
            var area = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);

            GUI.color = new Color(0f, 0f, 0f, 0.85f);
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUILayout.BeginArea(new Rect(area.x + 16f, area.y + 14f, area.width - 32f, area.height - 28f));
            GUILayout.Label("SUBSTITUTION — HOME", _title);
            GUILayout.Space(6f);

            GUILayout.BeginHorizontal();

            // OUT column (on court).
            GUILayout.BeginVertical(GUILayout.Width(255f));
            GUILayout.Label("OUT — On court", _col);
            for (int i = 0; i < team.onCourt.Count; i++)
                if (GUILayout.Button(PlayerLabel(team.onCourt[i]), i == _outSel ? _btnSel : _btn))
                    _outSel = i;
            GUILayout.EndVertical();

            GUILayout.Space(12f);

            // IN column (bench).
            GUILayout.BeginVertical(GUILayout.Width(255f));
            GUILayout.Label("IN — Bench", _col);
            if (team.bench.Count == 0) GUILayout.Label("(no bench players)", _hint);
            for (int j = 0; j < team.bench.Count; j++)
                if (GUILayout.Button(PlayerLabel(team.bench[j]), j == _inSel ? _btnSel : _btn))
                    _inSel = j;
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUI.enabled = _outSel >= 0 && _inSel >= 0;
            if (GUILayout.Button("Substitute", _btn, GUILayout.Height(34f)))
            {
                bool ok = gm.Substitute(TeamSide.Home, _outSel, _inSel);
                Flash(ok ? "Substitution made" : "Substitution failed");
                _outSel = -1; _inSel = -1; // lists swapped — pick again for another sub
            }
            GUI.enabled = true;
            if (GUILayout.Button("Done", _btn, GUILayout.Height(34f))) _subOpen = false;
            GUILayout.EndHorizontal();
            GUILayout.Label("Pick one player to take OUT and one to bring IN, then Substitute.", _hint);
            GUILayout.EndArea();
        }

        static string PlayerLabel(PlayerController p)
        {
            if (p == null || p.Character == null) return "—";
            return $"{p.Character.stats.characterName}   (E {p.Character.Energy:0})";
        }

        void EnsureStyles()
        {
            _style ??= new GUIStyle(GUI.skin.label)
            { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
            _title ??= new GUIStyle(GUI.skin.label)
            { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
            _col ??= new GUIStyle(GUI.skin.label)
            { fontSize = 16, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.85f, 0.85f, 0.9f) } };
            _btn ??= new GUIStyle(GUI.skin.button) { fontSize = 15, alignment = TextAnchor.MiddleLeft };
            _btnSel ??= new GUIStyle(GUI.skin.button)
            { fontSize = 15, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, normal = { textColor = new Color(1f, 0.85f, 0.2f) } };
            _hint ??= new GUIStyle(GUI.skin.label) { fontSize = 13, normal = { textColor = new Color(0.8f, 0.8f, 0.8f) } };
        }
    }
}
