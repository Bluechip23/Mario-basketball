using System.Collections.Generic;
using UnityEngine;
using MarioBasketball.Characters;
using MarioBasketball.Bootstrap;

namespace MarioBasketball.UI
{
    /// <summary>
    /// Pre-match team picker (IMGUI). The player drafts five characters for each
    /// side from the roster; the first HOME pick is the player they'll control.
    /// On Start it hands the two rosters to <see cref="GameBootstrap.StartMatch"/>
    /// and removes itself. Throwaway IMGUI like the rest of the prototype UI.
    /// </summary>
    public class TeamSelectMenu : MonoBehaviour
    {
        public GameBootstrap bootstrap;
        public MainMenu mainMenu;

        const int TeamSize = 5;

        List<CharacterStats> _roster = new List<CharacterStats>();
        readonly List<int> _home = new List<int>();
        readonly List<int> _away = new List<int>();
        bool _editingAway;
        Vector2 _scroll;

        GUIStyle _title;
        GUIStyle _header;
        GUIStyle _row;
        GUIStyle _button;

        void OnEnable()
        {
            // Rebuild the pool each time so freshly created players appear.
            _roster = new List<CharacterStats>(CharacterLibrary.All());
            foreach (var created in CreatedPlayerStore.All())
                if (created != null && created.stats != null) _roster.Add(created.stats);

            _home.Clear();
            _away.Clear();
            _editingAway = false;
            Prefill(_home, "Mario", "Luigi", "Peach", "Toad", "Diddy Kong");
            Prefill(_away, "Bowser", "Donkey Kong", "Waluigi", "Yoshi", "Boo");
        }

        void Prefill(List<int> team, params string[] names)
        {
            foreach (var name in names)
            {
                int idx = IndexOf(name);
                if (idx >= 0 && team.Count < TeamSize && !team.Contains(idx)) team.Add(idx);
            }
        }

        int IndexOf(string name)
        {
            for (int i = 0; i < _roster.Count; i++)
                if (_roster[i].characterName == name) return i;
            return -1;
        }

        void OnGUI()
        {
            EnsureStyles();

            var prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = prev;

            float w = Mathf.Min(Screen.width - 40f, 900f);
            float h = Screen.height - 40f;
            GUILayout.BeginArea(new Rect((Screen.width - w) / 2f, 20f, w, h), GUI.skin.box);

            GUILayout.Label("TEAM SELECT", _title);
            GUILayout.Label("Draft 5 per team. Your 1st HOME pick is the player you control.", _header);

            DrawTeamRow("HOME", _home, editing: !_editingAway);
            DrawTeamRow("AWAY", _away, editing: _editingAway);

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(_editingAway ? "Editing: AWAY  (switch to HOME)" : "Editing: HOME  (switch to AWAY)", _button))
                _editingAway = !_editingAway;
            if (GUILayout.Button("Randomize this team", _button))
                Randomize(_editingAway ? _away : _home);
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.Label($"Tap a character to add to {(_editingAway ? "AWAY" : "HOME")} (tap a slot above to remove):", _header);

            _scroll = GUILayout.BeginScrollView(_scroll);
            var current = _editingAway ? _away : _home;
            for (int i = 0; i < _roster.Count; i++)
            {
                var s = _roster[i];
                GUILayout.BeginHorizontal();
                GUI.enabled = current.Count < TeamSize && !current.Contains(i);
                if (GUILayout.Button(s.characterName, _button, GUILayout.Width(130))) current.Add(i);
                GUI.enabled = true;
                GUILayout.Label(ShortStats(s), _row);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Back", _button, GUILayout.Height(46), GUILayout.Width(120)))
            {
                enabled = false;
                if (mainMenu != null) mainMenu.Show();
            }
            bool ready = _home.Count == TeamSize && _away.Count == TeamSize;
            GUI.enabled = ready;
            if (GUILayout.Button(ready ? "START GAME" : "Pick 5 per team to start", _button, GUILayout.Height(46)))
                StartGame();
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        void DrawTeamRow(string label, List<int> team, bool editing)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(editing ? $"▶ {label} ({team.Count}/{TeamSize})" : $"   {label} ({team.Count}/{TeamSize})",
                _header, GUILayout.Width(150));
            for (int slot = 0; slot < TeamSize; slot++)
            {
                if (slot < team.Count)
                {
                    string tag = slot == 0 && label == "HOME" ? "★ " : "";
                    if (GUILayout.Button(tag + _roster[team[slot]].characterName, _button, GUILayout.Width(130)))
                        team.RemoveAt(slot);
                }
                else
                {
                    GUILayout.Box("—", GUILayout.Width(130));
                }
            }
            GUILayout.EndHorizontal();
        }

        void Randomize(List<int> team)
        {
            team.Clear();
            var pool = new List<int>();
            for (int i = 0; i < _roster.Count; i++) pool.Add(i);
            for (int n = 0; n < TeamSize && pool.Count > 0; n++)
            {
                int pick = Random.Range(0, pool.Count);
                team.Add(pool[pick]);
                pool.RemoveAt(pick);
            }
        }

        void StartGame()
        {
            if (bootstrap == null) return;
            var home = new CharacterStats[TeamSize];
            var away = new CharacterStats[TeamSize];
            for (int i = 0; i < TeamSize; i++)
            {
                home[i] = _roster[_home[i]].Clone();
                away[i] = _roster[_away[i]].Clone();
            }
            enabled = false; // stop drawing the menu
            bootstrap.StartMatch(home, away);
        }

        static string ShortStats(CharacterStats s) =>
            $"Spd{s.speed}  BH{s.ballHandling}  3PT{s.threePoint}  Mid{s.midRange}  Ins{s.insideScoring}  " +
            $"Dnk{s.dunk}  Pow{s.power}  Reb{s.rebounds}  Stl{s.steals}  Sta{s.stamina}";

        void EnsureStyles()
        {
            _title ??= new GUIStyle(GUI.skin.label)
            { fontSize = 26, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _header ??= new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold };
            _row ??= new GUIStyle(GUI.skin.label) { fontSize = 13 };
            _button ??= new GUIStyle(GUI.skin.button) { fontSize = 14 };
        }
    }
}
