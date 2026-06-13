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
    /// <b>Settings</b>, Restart and Quit. Controller navigation matches the
    /// other menus (flashing yellow selection, A confirms, B resumes).
    /// Throwaway IMGUI like the rest of the prototype UI.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        static readonly string[] Items = { "Resume", "Stats", "Settings", "Restart", "Quit" };

        InputAction _pause;
        bool _showStats;
        bool _inSettings;
        int _sel;
        MenuNav _nav;
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
            _nav = new MenuNav();
            _nav.Enable();
        }

        void OnDisable()
        {
            if (_pause == null) return;
            _pause.performed -= OnPausePressed;
            _pause.Disable();
            _pause = null;
            _nav?.Disable();
            _nav = null;
            // Don't leave the game frozen if this object goes away.
            Time.timeScale = 1f;
            MatchPause.IsPaused = false;
        }

        void OnPausePressed(InputAction.CallbackContext ctx) => SetPaused(!MatchPause.IsPaused);

        void SetPaused(bool paused)
        {
            MatchPause.IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            if (paused) _sel = 0;
            else
            {
                _showStats = false;
                CloseSettings();
            }
        }

        void Update()
        {
            if (!MatchPause.IsPaused || _inSettings) return;
            _nav.Tick();

            if (_showStats)
            {
                if (_nav.East || _nav.Submit) _showStats = false;
                return;
            }

            if (_nav.Step.y != 0)
                _sel = (_sel - _nav.Step.y + Items.Length) % Items.Length;
            if (_nav.Submit) Activate(_sel);
            else if (_nav.East) SetPaused(false);
        }

        void Activate(int index)
        {
            switch (index)
            {
                case 0: SetPaused(false); break;
                case 1: _showStats = true; break;
                case 2: OpenSettings(); break;
                case 3: Restart(); break;
                case 4: Application.Quit(); break;
            }
        }

        void OpenSettings()
        {
            var settings = GetComponent<SettingsMenu>();
            if (settings == null) settings = gameObject.AddComponent<SettingsMenu>();
            _inSettings = true;
            settings.Open(() => _inSettings = false);
        }

        void CloseSettings()
        {
            if (!_inSettings) return;
            _inSettings = false;
            var settings = GetComponent<SettingsMenu>();
            if (settings != null) settings.enabled = false;
        }

        void OnGUI()
        {
            if (!MatchPause.IsPaused || _inSettings) return;
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
            float w = 320f, h = 340f;
            var rect = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);
            GUI.Box(rect, GUIContent.none);
            GUI.Label(new Rect(rect.x, rect.y + 10, rect.width, 32), "PAUSED", _title);

            for (int i = 0; i < Items.Length; i++)
            {
                var r = new Rect(rect.x + 24, rect.y + 54 + i * 54, w - 48, 44);
                if (_sel == i) MenuNav.DrawSelection(r);
                if (GUI.Button(r, Items[i], _button)) { _sel = i; Activate(i); }
            }
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
