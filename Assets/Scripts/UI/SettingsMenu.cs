using System;
using UnityEngine;
using MarioBasketball.Core;

namespace MarioBasketball.UI
{
    /// <summary>
    /// The Settings screen (IMGUI), reachable from both the main menu and the
    /// pause menu. Edits <see cref="GameSettings"/>: audio applies immediately,
    /// match rules (quarter length, shot clock) apply to the next game.
    /// Controller-friendly: up/down selects a row (flashing yellow outline),
    /// left/right adjusts it, B/circle backs out. Mouse still works.
    /// </summary>
    public class SettingsMenu : MonoBehaviour
    {
        /// <summary>Invoked when the player backs out (the opener restores itself).</summary>
        public Action onClose;

        // Row order: volume, quarter length, shot clock, back.
        const int RowCount = 4;
        const int BackRow = 3;
        int _row;
        MenuNav _nav;

        GUIStyle _title;
        GUIStyle _label;
        GUIStyle _value;
        GUIStyle _button;
        GUIStyle _hint;

        /// <summary>Open the settings screen; <paramref name="onClose"/> runs
        /// when the player backs out.</summary>
        public void Open(Action onClose)
        {
            this.onClose = onClose;
            _row = 0;
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

            if (_nav.Step.y != 0)
                _row = (_row - _nav.Step.y + RowCount) % RowCount; // stick up = previous row

            if (_nav.Step.x != 0) Adjust(_row, _nav.Step.x);

            if (_nav.Submit && _row == BackRow) Close();
            if (_nav.East) Close();
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
            }
        }

        void OnGUI()
        {
            EnsureStyles();

            var prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.65f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = prev;

            float w = 520f, h = 380f;
            var area = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);
            GUI.Box(area, GUIContent.none);
            GUI.Label(new Rect(area.x, area.y + 12, area.width, 36), "SETTINGS", _title);

            DrawRow(area, 0, "Master Volume", $"{GameSettings.MasterVolume}%");
            DrawRow(area, 1, "Quarter Length", $"{GameSettings.QuarterMinutes} min");
            DrawRow(area, 2, "Shot Clock", $"{GameSettings.ShotClockSeconds} s");

            GUI.Label(new Rect(area.x + 30, RowRect(area, 2).yMax + 8, area.width - 60, 40),
                "Match settings apply to the next game.", _hint);

            var backRect = new Rect(area.x + 30, area.y + h - 64, area.width - 60, 44);
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

        void EnsureStyles()
        {
            _title ??= new GUIStyle(GUI.skin.label)
            { fontSize = 24, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _label ??= new GUIStyle(GUI.skin.label) { fontSize = 17, alignment = TextAnchor.MiddleLeft };
            _value ??= new GUIStyle(GUI.skin.label)
            { fontSize = 17, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleRight };
            _button ??= new GUIStyle(GUI.skin.button) { fontSize = 16 };
            _hint ??= new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true, alignment = TextAnchor.UpperCenter };
        }
    }
}
