using System;
using UnityEngine;
using MarioBasketball.Core;

namespace MarioBasketball.UI
{
    /// <summary>
    /// The Settings screen (IMGUI), reachable from both the main menu and the
    /// pause menu. Edits <see cref="GameSettings"/>: audio applies immediately,
    /// match rules (quarter length, shot clock) apply to the next game. A
    /// <b>Controls</b> row opens the full control reference (grouped by
    /// situation). Controller-friendly: up/down selects a row (flashing yellow
    /// outline), left/right adjusts it, A/cross activates Controls or Back,
    /// B/circle backs out. Mouse still works.
    /// </summary>
    public class SettingsMenu : MonoBehaviour
    {
        /// <summary>Invoked when the player backs out (the opener restores itself).</summary>
        public Action onClose;

        // Row order: volume, quarter length, shot clock, vibration, box score,
        // controls, back.
        const int RowCount = 7;
        const int VibrationRow = 3;
        const int BoxScoreRow = 4;
        const int ControlsRow = 5;
        const int BackRow = 6;
        int _row;
        bool _showControls;
        MenuNav _nav;
        Vector2 _ctrlScroll;

        GUIStyle _title;
        GUIStyle _label;
        GUIStyle _value;
        GUIStyle _button;
        GUIStyle _hint;
        GUIStyle _group;
        GUIStyle _action;
        GUIStyle _bind;

        /// <summary>Open the settings screen; <paramref name="onClose"/> runs
        /// when the player backs out.</summary>
        public void Open(Action onClose)
        {
            this.onClose = onClose;
            _row = 0;
            _showControls = false;
            enabled = true;
        }

        void OnEnable()
        {
            _nav = new MenuNav();
            _nav.Enable();
        }

        void OnDisable()
        {
            _nav?.Disable();
            _nav = null;
        }

        void Update()
        {
            _nav.Tick();

            // The controls reference is a sub-page: any confirm/back returns to
            // the settings rows.
            if (_showControls)
            {
                if (_nav.East || _nav.Submit) _showControls = false;
                return;
            }

            if (_nav.Step.y != 0)
                _row = (_row - _nav.Step.y + RowCount) % RowCount; // stick up = previous row

            if (_nav.Step.x != 0) Adjust(_row, _nav.Step.x);

            // Close() disables this component, which nulls _nav synchronously —
            // so we must return immediately and never touch _nav again this frame.
            if (_nav.Submit)
            {
                if (_row == VibrationRow) { GameSettings.Vibration = !GameSettings.Vibration; return; }
                if (_row == BoxScoreRow) { GameSettings.ShowBoxScore = !GameSettings.ShowBoxScore; return; }
                if (_row == ControlsRow) { _showControls = true; return; }
                if (_row == BackRow) { Close(); return; }
            }
            if (_nav.East) { Close(); return; }
        }

        void Close()
        {
            enabled = false;
            onClose?.Invoke();
        }

        static void Adjust(int row, int dir)
        {
            switch (row)
            {
                case 0: GameSettings.MasterVolume += dir * 5; break;
                case 1: GameSettings.QuarterMinutes += dir; break;
                case 2: GameSettings.ShotClockSeconds += dir; break;
                case VibrationRow: GameSettings.Vibration = !GameSettings.Vibration; break;
                case BoxScoreRow: GameSettings.ShowBoxScore = !GameSettings.ShowBoxScore; break;
            }
        }

        void OnGUI()
        {
            EnsureStyles();

            var prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.65f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = prev;

            if (_showControls) { DrawControls(); return; }

            float w = 520f, h = 560f;
            var area = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);
            GUI.Box(area, GUIContent.none);
            GUI.Label(new Rect(area.x, area.y + 12, area.width, 36), "SETTINGS", _title);

            DrawRow(area, 0, "Master Volume", $"{GameSettings.MasterVolume}%");
            DrawRow(area, 1, "Quarter Length", $"{GameSettings.QuarterMinutes} min");
            DrawRow(area, 2, "Shot Clock", $"{GameSettings.ShotClockSeconds} s");
            DrawRow(area, VibrationRow, "Vibration", GameSettings.Vibration ? "On" : "Off");
            DrawRow(area, BoxScoreRow, "Box Score", GameSettings.ShowBoxScore ? "Shown" : "Hidden");
            DrawButtonRow(area, ControlsRow, "Controls");

            GUI.Label(new Rect(area.x + 30, RowRect(area, ControlsRow).yMax + 8, area.width - 60, 24),
                "Match settings apply to the next game.", _hint);

            var backRect = new Rect(area.x + 30, area.y + h - 60, area.width - 60, 44);
            if (_row == BackRow) MenuNav.DrawSelection(backRect);
            if (GUI.Button(backRect, "Back", _button)) Close();
        }

        static Rect RowRect(Rect area, int row) =>
            new Rect(area.x + 30, area.y + 70 + row * 58, area.width - 60, 48);

        void DrawRow(Rect area, int row, string label, string value)
        {
            var r = RowRect(area, row);
            if (_row == row) MenuNav.DrawSelection(r);
            GUI.Box(r, GUIContent.none);
            GUI.Label(new Rect(r.x + 12, r.y, r.width * 0.5f, r.height), label, _label);
            GUI.Label(new Rect(r.x + r.width * 0.5f, r.y, r.width * 0.28f, r.height), value, _value);

            // Mouse fallback for the left/right adjustment.
            if (GUI.Button(new Rect(r.xMax - 84, r.y + 7, 34, 34), "−", _button)) { _row = row; Adjust(row, -1); }
            if (GUI.Button(new Rect(r.xMax - 44, r.y + 7, 34, 34), "+", _button)) { _row = row; Adjust(row, +1); }
        }

        void DrawButtonRow(Rect area, int row, string label)
        {
            var r = RowRect(area, row);
            if (_row == row) MenuNav.DrawSelection(r);
            if (GUI.Button(r, label, _button)) { _row = row; _showControls = true; }
        }

        // ---- Controls reference -------------------------------------------

        // Each group is a heading plus rows of (action, gamepad, keyboard).
        static readonly (string header, (string action, string pad, string key)[] rows)[] Groups =
        {
            ("GENERAL", new[]
            {
                ("Move", "Left stick", "WASD"),
                ("Turbo / sprint (hold)", "LT", "Left Shift"),
                ("Pause", "Start", "Esc"),
                ("Timeout", "D-pad Up", "T"),
                ("Substitute", "D-pad Down", "Y"),
            }),
            ("OFFENSE — WITH BALL", new[]
            {
                ("Shoot jumper (release at the marker)", "A", "Space"),
                ("Dunk / layup inside (hold)", "A", "Space"),
                ("Air-adjust a finish", "LB", "C"),
                ("Pass — tap = loft, hold = bullet", "X", "E"),
                ("Aim the pass", "Right stick", "IJKL"),
                ("Pass to a teammate icon", "Hold LB + A/B", "Hold C + A/B"),
                ("Fade on a jumper", "Hold Move", "Hold Move"),
                ("Dribble move (break down the D)", "B", "X"),
                ("Dribble flick — step-back / cross", "Right-stick flick", "—"),
                ("Post up (hold)", "RB", "R"),
                ("Back down in the post", "RT", "B"),
                ("Post: hook / drop step / spin / fake", "Y / A / B / LB", "H / G / V / C"),
                ("Called shot (Delfan)", "Double-tap LT", "Double-tap Shift"),
            }),
            ("OFFENSE — OFF BALL", new[]
            {
                ("Move / cut to get open", "Left stick", "WASD"),
                ("Sprint (hold)", "LT", "Left Shift"),
                ("Jump for a rebound / oop", "Y", "Left Ctrl"),
            }),
            ("DEFENSE", new[]
            {
                ("Switch to the man nearest the ball", "A / LB", "Q"),
                ("Steal", "X", "F"),
                ("Jump / block / contest", "Y", "Left Ctrl"),
                ("Push / foul (or bump a poster)", "RT", "B"),
                ("Dive for a loose ball", "B", "X"),
            }),
        };

        void DrawControls()
        {
            float w = Mathf.Min(Screen.width - 60f, 760f);
            float h = Mathf.Min(Screen.height - 80f, 640f);
            var area = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);
            GUILayout.BeginArea(area, GUI.skin.box);

            GUILayout.Label("CONTROLS", _title);

            // Column key.
            GUILayout.BeginHorizontal();
            GUILayout.Label("", _action, GUILayout.Width(w * 0.50f));
            GUILayout.Label("Gamepad", _group, GUILayout.Width(w * 0.27f));
            GUILayout.Label("Keyboard", _group);
            GUILayout.EndHorizontal();

            _ctrlScroll = GUILayout.BeginScrollView(_ctrlScroll);
            foreach (var grp in Groups)
            {
                GUILayout.Space(8);
                GUILayout.Label(grp.header, _group);
                foreach (var (action, pad, key) in grp.rows)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(action, _action, GUILayout.Width(w * 0.50f));
                    GUILayout.Label(pad, _bind, GUILayout.Width(w * 0.27f));
                    GUILayout.Label(key, _bind);
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView();

            if (GUILayout.Button("Back", _button, GUILayout.Height(36))) _showControls = false;
            GUILayout.EndArea();
        }

        void EnsureStyles()
        {
            _title ??= new GUIStyle(GUI.skin.label)
            { fontSize = 24, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _label ??= new GUIStyle(GUI.skin.label) { fontSize = 17, alignment = TextAnchor.MiddleLeft };
            _value ??= new GUIStyle(GUI.skin.label)
            { fontSize = 17, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleRight };
            _button ??= new GUIStyle(GUI.skin.button) { fontSize = 16 };
            _hint ??= new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true, alignment = TextAnchor.UpperCenter };
            _group ??= new GUIStyle(GUI.skin.label)
            { fontSize = 15, fontStyle = FontStyle.Bold };
            _action ??= new GUIStyle(GUI.skin.label) { fontSize = 14 };
            _bind ??= new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };
        }
    }
}
