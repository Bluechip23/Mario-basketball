using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using MarioBasketball.Core;
using MarioBasketball.Gameplay;

namespace MarioBasketball.UI
{
    /// <summary>
    /// An IMGUI pause menu toggled with Esc / Start. Freezes the game
    /// (<c>Time.timeScale = 0</c>) and offers Resume, a <b>Stats</b> view that
    /// lists every player's full stat sheet (handy for balancing mid-game),
    /// Restart and Quit. Throwaway IMGUI like the rest of the prototype UI.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        InputAction _pause;
        bool _showStats;
        Vector2 _scroll;

        GUIStyle _title;
        GUIStyle _button;
        GUIStyle _header;
        GUIStyle _row;

        void Awake()
        {
            // Always start unpaused (static flag/timeScale persist across reloads).
            MatchPause.IsPaused = false;
            Time.timeScale = 1f;
        }

        void OnEnable()
        {
            _pause = new InputAction("Pause", InputActionType.Button, "<Keyboard>/escape");
            _pause.AddBinding("<Gamepad>/start");
            _pause.performed += OnPausePressed;
            _pause.Enable();
        }

        void OnDisable()
        {
            if (_pause == null) return;
            _pause.performed -= OnPausePressed;
            _pause.Disable();
            _pause = null;
            // Don't leave the game frozen if this object goes away.
            Time.timeScale = 1f;
            MatchPause.IsPaused = false;
        }

        void OnPausePressed(InputAction.CallbackContext ctx) => SetPaused(!MatchPause.IsPaused);

        void SetPaused(bool paused)
        {
            MatchPause.IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            if (!paused) _showStats = false;
        }

        void OnGUI()
        {
            if (!MatchPause.IsPaused) return;
            EnsureStyles();

            // Dim the field behind the menu.
            var prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.5f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = prev;

            if (_showStats) DrawStats();
            else DrawMenu();
        }

        void DrawMenu()
        {
            float w = 320f, h = 280f;
            var rect = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);
            GUILayout.BeginArea(rect, GUI.skin.box);
            GUILayout.Space(8);
            GUILayout.Label("PAUSED", _title);
            GUILayout.Space(12);
            if (GUILayout.Button("Resume", _button, GUILayout.Height(44))) SetPaused(false);
            if (GUILayout.Button("Stats", _button, GUILayout.Height(44))) _showStats = true;
            if (GUILayout.Button("Restart", _button, GUILayout.Height(44))) Restart();
            if (GUILayout.Button("Quit", _button, GUILayout.Height(44))) Application.Quit();
            GUILayout.EndArea();
        }

        void DrawStats()
        {
            float w = Mathf.Min(Screen.width - 40f, 1180f);
            float h = Screen.height - 80f;
            var rect = new Rect((Screen.width - w) / 2f, 40f, w, h);
            GUILayout.BeginArea(rect, GUI.skin.box);

            GUILayout.Label("PLAYER STATS", _title);
            GUILayout.Label("Spd  BH  3PT  Mid  Ins  PsO  Dnk  Pow  Reb  Blk  Stl  PsD  PerD  Sta   (Energy)", _header);

            _scroll = GUILayout.BeginScrollView(_scroll);
            var gm = GameManager.Instance;
            if (gm != null)
            {
                DrawTeam("HOME", gm.Home);
                GUILayout.Space(10);
                DrawTeam("AWAY", gm.Away);
            }
            else
            {
                GUILayout.Label("No match in progress.", _row);
            }
            GUILayout.EndScrollView();

            if (GUILayout.Button("Back", _button, GUILayout.Height(36))) _showStats = false;
            GUILayout.EndArea();
        }

        void DrawTeam(string label, TeamState team)
        {
            GUILayout.Label($"== {label} ==", _header);
            foreach (var p in team.onCourt) DrawPlayer(p, onCourt: true);
            foreach (var p in team.bench) DrawPlayer(p, onCourt: false);
        }

        void DrawPlayer(PlayerController pc, bool onCourt)
        {
            if (pc == null || pc.Character == null) return;
            var s = pc.Character.stats;
            string where = onCourt ? "  " : " (bench)";
            string line = string.Format(
                "{0,-13}{1,3}{2,4}{3,5}{4,5}{5,5}{6,5}{7,5}{8,5}{9,5}{10,5}{11,5}{12,5}{13,6}{14,5}   {15,4:0}{16}",
                s.characterName,
                s.speed, s.ballHandling, s.threePoint, s.midRange, s.insideScoring,
                s.postOffense, s.dunk, s.power, s.rebounds, s.blocks, s.steals,
                s.postDefense, s.perimeterDefense, s.stamina,
                pc.Character.Energy, where);
            GUILayout.Label(line, _row);
        }

        void Restart()
        {
            SetPaused(false);
            var scene = SceneManager.GetActiveScene();
            if (scene.buildIndex >= 0) SceneManager.LoadScene(scene.buildIndex);
            else if (!string.IsNullOrEmpty(scene.name)) SceneManager.LoadScene(scene.name);
            else Debug.LogWarning("Restart: active scene isn't loadable by index or name.");
        }

        void EnsureStyles()
        {
            _title ??= new GUIStyle(GUI.skin.label)
            { fontSize = 26, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _button ??= new GUIStyle(GUI.skin.button) { fontSize = 18 };
            _header ??= new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold };
            _row ??= new GUIStyle(GUI.skin.label) { fontSize = 14, richText = false };
        }
    }
}
