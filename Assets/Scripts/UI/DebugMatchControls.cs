using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using MarioBasketball.Core;
using MarioBasketball.Gameplay;

namespace MarioBasketball.UI
{
    /// <summary>
    /// Match shortcuts that don't have full UI yet: call a timeout, and open a
    /// <b>substitution menu</b> for the home team — pick who comes OUT (from the
    /// on-court five) and who comes IN (from the bench), then confirm. Subs are
    /// only legal during a timeout or a quarter break
    /// (<see cref="GameManager.CanSubstitute"/>), but while the menu is open the
    /// countdown freezes (<see cref="GameManager.SubMenuOpen"/>) so it's never on
    /// a clock. Fully navigable with the controller or keyboard (and the mouse
    /// still clicks).
    /// </summary>
    public class DebugMatchControls : MonoBehaviour
    {
        string _message;
        float _messageTimer;
        GUIStyle _style, _title, _col, _btn, _btnSel, _hint;

        bool _subOpen;
        bool _justOpened;
        int _outSel = -1; // chosen on-court player to remove
        int _inSel = -1;  // chosen bench player to bring in

        // Controller cursor. Zone 0 = the two player lists; zone 1 = the action row.
        int _zone;    // 0 = lists, 1 = actions
        int _col;     // 0 = OUT (on court), 1 = IN (bench)
        int _row;     // index within the focused column
        int _action;  // 0 = Substitute, 1 = Done
        bool _stickLatched;

        void Update()
        {
            if (_messageTimer > 0f) _messageTimer -= Time.unscaledDeltaTime;

            var gm = GameManager.Instance;
            if (MatchPause.IsPaused || gm == null) { CloseMenu(gm); return; }

            var kb = Keyboard.current;
            var gp = Gamepad.current;

            bool timeout = Pressed(kb?.tKey) || Pressed(gp?.dpad.up);
            bool openSub = Pressed(kb?.yKey) || Pressed(gp?.dpad.down);
            bool cancel = Pressed(kb?.escapeKey) || Pressed(gp?.bButton);

            // D-pad up / T: home timeout (+30 energy to the on-court five).
            if (timeout && !_subOpen)
            {
                bool ok = gm.CallTimeout(TeamSide.Home);
                Flash(ok ? "Home timeout" : "No timeouts left");
            }

            // D-pad down / Y: open the substitution menu (timeout / quarter break only).
            _justOpened = false;
            if (openSub && !_subOpen)
            {
                if (!gm.CanSubstitute) Flash("Subs only during a timeout or quarter break");
                else
                {
                    _subOpen = true; _justOpened = true;
                    _outSel = -1; _inSel = -1;
                    _zone = 0; _col = 0; _row = 0; _action = 0;
                    gm.SubMenuOpen = true; // freeze the countdown while choosing
                }
            }

            if (_subOpen && (cancel || !gm.CanSubstitute)) { CloseMenu(gm); return; }
            if (!_subOpen) { gm.SubMenuOpen = false; return; }

            // Navigate the open menu (skip the frame it opened so the same press
            // that opened it doesn't also move the cursor).
            if (!_justOpened) NavigateSubMenu(gm, kb, gp);
        }

        void NavigateSubMenu(GameManager gm, Keyboard kb, Gamepad gp)
        {
            var team = gm.Home;
            Vector2Int step = ReadStep(kb, gp);
            bool submit = Pressed(kb?.enterKey) || Pressed(gp?.aButton);

            if (_zone == 0)
            {
                if (step.x < 0) _col = 0;
                else if (step.x > 0) _col = 1;
                int count = ColumnCount(team, _col);
                if (step.y > 0) _row = Mathf.Max(0, _row - 1);
                else if (step.y < 0)
                {
                    if (_row < count - 1) _row++;
                    else { _zone = 1; _action = 0; } // off the bottom → action row
                }
                _row = count > 0 ? Mathf.Clamp(_row, 0, count - 1) : 0;

                if (submit)
                {
                    if (_col == 0 && _row < team.onCourt.Count) _outSel = _row;
                    else if (_col == 1 && _row < team.bench.Count) _inSel = _row;
                }
            }
            else // action row
            {
                if (step.x < 0) _action = 0;
                else if (step.x > 0) _action = 1;
                if (step.y > 0)
                {
                    _zone = 0;
                    _row = Mathf.Max(0, ColumnCount(team, _col) - 1);
                }
                if (submit)
                {
                    if (_action == 0) DoSubstitute(gm);
                    else CloseMenu(gm);
                }
            }
        }

        void DoSubstitute(GameManager gm)
        {
            if (_outSel < 0 || _inSel < 0) { Flash("Pick one OUT and one IN first"); return; }
            bool ok = gm.Substitute(TeamSide.Home, _outSel, _inSel);
            Flash(ok ? "Substitution made" : "Substitution failed");
            _outSel = -1; _inSel = -1; // lists swapped — pick again for another sub
            int count = ColumnCount(gm.Home, _col);
            _row = count > 0 ? Mathf.Clamp(_row, 0, count - 1) : 0;
        }

        void CloseMenu(GameManager gm)
        {
            _subOpen = false;
            if (gm != null) gm.SubMenuOpen = false;
        }

        static int ColumnCount(TeamState team, int col) => col == 0 ? team.onCourt.Count : team.bench.Count;

        static bool Pressed(ButtonControl b) => b != null && b.wasPressedThisFrame;

        // One discrete step per press from d-pad / arrows / WASD, plus a latched
        // left-stick (push once = one step) so the stick doesn't fly through lists.
        Vector2Int ReadStep(Keyboard kb, Gamepad gp)
        {
            int x = 0, y = 0;
            if (Pressed(kb?.upArrowKey) || Pressed(kb?.wKey) || Pressed(gp?.dpad.up)) y = 1;
            else if (Pressed(kb?.downArrowKey) || Pressed(kb?.sKey) || Pressed(gp?.dpad.down)) y = -1;
            if (Pressed(kb?.leftArrowKey) || Pressed(kb?.aKey) || Pressed(gp?.dpad.left)) x = -1;
            else if (Pressed(kb?.rightArrowKey) || Pressed(kb?.dKey) || Pressed(gp?.dpad.right)) x = 1;

            Vector2 ls = gp != null ? gp.leftStick.ReadValue() : Vector2.zero;
            if (ls.magnitude < 0.5f) _stickLatched = false;
            else if (!_stickLatched && (x == 0 && y == 0))
            {
                _stickLatched = true;
                if (Mathf.Abs(ls.x) > Mathf.Abs(ls.y)) x = ls.x > 0 ? 1 : -1;
                else y = ls.y > 0 ? 1 : -1;
            }
            return new Vector2Int(x, y);
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

        // Two columns of explicit rects — pick one OUT (on court) and one IN
        // (bench), then Substitute. The yellow cursor marks controller focus; the
        // gold-text button marks the current selection. The mouse still clicks.
        void DrawSubMenu(GameManager gm)
        {
            var team = gm.Home;
            const float w = 580f, h = 392f;
            const float colW = 252f, rowH = 30f, gap = 6f;
            var area = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);

            GUI.color = new Color(0f, 0f, 0f, 0.88f);
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUI.Label(new Rect(area.x, area.y + 12f, area.width, 28f), "SUBSTITUTION — HOME", _title);

            float listTop = area.y + 56f;
            float outX = area.x + 20f;
            float inX = area.x + 28f + colW;

            GUI.Label(new Rect(outX, listTop, colW, 22f), "OUT — On court", _col);
            GUI.Label(new Rect(inX, listTop, colW, 22f), "IN — Bench", _col);
            float rowTop = listTop + 26f;

            for (int i = 0; i < team.onCourt.Count; i++)
            {
                var r = new Rect(outX, rowTop + i * (rowH + gap), colW, rowH);
                if (_zone == 0 && _col == 0 && _row == i) MenuNav.DrawSelection(r);
                if (GUI.Button(r, PlayerLabel(team.onCourt[i]), i == _outSel ? _btnSel : _btn))
                { _zone = 0; _col = 0; _row = i; _outSel = i; }
            }

            if (team.bench.Count == 0)
                GUI.Label(new Rect(inX, rowTop, colW, rowH), "(no bench players)", _hint);
            for (int j = 0; j < team.bench.Count; j++)
            {
                var r = new Rect(inX, rowTop + j * (rowH + gap), colW, rowH);
                if (_zone == 0 && _col == 1 && _row == j) MenuNav.DrawSelection(r);
                if (GUI.Button(r, PlayerLabel(team.bench[j]), j == _inSel ? _btnSel : _btn))
                { _zone = 0; _col = 1; _row = j; _inSel = j; }
            }

            // Action row along the bottom.
            float actY = area.yMax - 76f;
            var subRect = new Rect(outX, actY, colW, 38f);
            var doneRect = new Rect(inX, actY, colW, 38f);

            bool ready = _outSel >= 0 && _inSel >= 0;
            if (_zone == 1 && _action == 0) MenuNav.DrawSelection(subRect);
            GUI.enabled = ready;
            if (GUI.Button(subRect, "Substitute", _btn)) DoSubstitute(gm);
            GUI.enabled = true;

            if (_zone == 1 && _action == 1) MenuNav.DrawSelection(doneRect);
            if (GUI.Button(doneRect, "Done", _btn)) CloseMenu(gm);

            GUI.Label(new Rect(area.x + 20f, area.yMax - 30f, area.width - 40f, 22f),
                "D-pad/stick move  ·  A pick OUT then IN  ·  A on Substitute  ·  B close", _hint);
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
