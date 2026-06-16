using UnityEngine;

namespace MarioBasketball.UI
{
    /// <summary>
    /// The game's start menu (IMGUI). Routes to an Exhibition game (the existing
    /// <see cref="TeamSelectMenu"/>), Create-a-Player (Journey character with
    /// limited stats vs Standard with unlimited stats, info box per option) or
    /// <see cref="SettingsMenu"/>. Controller-first: d-pad / stick moves a
    /// flashing yellow selection outline, A confirms, B backs out; the mouse
    /// still works. One menu is enabled at a time; this component enables the
    /// others.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        public TeamSelectMenu teamSelect;
        public CreatePlayerMenu createPlayer;
        public SettingsMenu settings;

        const string JourneyInfo =
            "create journey player with limited stats. More stats can be earned " +
            "as you journey through and beat other teams across the kingdom.";
        const string StandardInfo =
            "create standard player with unlimited stats but can only be used in " +
            "exhibition games";

        enum Page { Main, CreateSelect }
        Page _page = Page.Main;
        int _sel;
        MenuNav _nav;

        GUIStyle _title;
        GUIStyle _button;
        GUIStyle _info;

        // Main page entries (Journey story mode is shown but not selectable yet).
        static readonly string[] MainItems = { "Exhibition Game", "Create a Player", "Settings", "Quit" };
        static readonly string[] CreateItems = { "Create Journey Character", "Create Standard Player", "Back" };

        /// <summary>Bring the menu back up at the top level.</summary>
        public void Show()
        {
            _page = Page.Main;
            _sel = 0;
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
            int count = _page == Page.Main ? MainItems.Length : CreateItems.Length;
            if (_nav.Step.y != 0)
                _sel = (_sel - _nav.Step.y + count) % count;

            if (_nav.Submit) Activate(_sel);
            else if (_nav.East && _page == Page.CreateSelect) { _page = Page.Main; _sel = 1; }
        }

        void Activate(int index)
        {
            if (_page == Page.Main)
            {
                switch (index)
                {
                    case 0: StartExhibition(); break;
                    case 1: _page = Page.CreateSelect; _sel = 0; break;
                    case 2: OpenSettings(); break;
                    case 3: Application.Quit(); break;
                }
            }
            else
            {
                switch (index)
                {
                    case 0: OpenEditor(journey: true); break;
                    case 1: OpenEditor(journey: false); break;
                    case 2: _page = Page.Main; _sel = 1; break;
                }
            }
        }

        void OnGUI()
        {
            EnsureStyles();
            MenuTheme.DrawBackground();

            if (_page == Page.Main) DrawMain();
            else DrawCreateSelect();
        }

        void DrawMain()
        {
            float w = 460f, h = 440f;
            var area = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);
            MenuTheme.Fill(area, MenuTheme.Cream);
            MenuTheme.Frame(area, MenuTheme.MarioRed, 4f);
            GUI.Label(new Rect(area.x, area.y + 14, area.width, 36), "MARIO STREET BASKETBALL", _title);

            // Selectable entries, with the disabled Journey teaser between
            // Settings and Quit (it doesn't take the selection cursor).
            var rects = new Rect[MainItems.Length];
            float y = area.y + 70;
            for (int i = 0; i < MainItems.Length; i++)
            {
                bool beforeQuit = i == MainItems.Length - 1;
                if (beforeQuit)
                {
                    GUI.enabled = false;
                    GUI.Button(new Rect(area.x + 30, y, w - 60, 48), "Journey (Story Mode) — coming soon", _button);
                    GUI.enabled = true;
                    y += 58;
                }
                rects[i] = new Rect(area.x + 30, y, w - 60, 48);
                y += 58;
            }

            for (int i = 0; i < MainItems.Length; i++)
            {
                if (_sel == i) MenuNav.DrawSelection(rects[i]);
                if (GUI.Button(rects[i], MainItems[i], _button)) { _sel = i; Activate(i); }
            }
        }

        void DrawCreateSelect()
        {
            float w = 560f, h = 440f;
            var area = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);
            MenuTheme.Fill(area, MenuTheme.Cream);
            MenuTheme.Frame(area, MenuTheme.MarioRed, 4f);

            GUI.Label(new Rect(area.x, area.y + 12, area.width, 36), "CREATE A PLAYER", _title);

            var journeyRect = new Rect(area.x + 30, area.y + 70, area.width - 60, 56);
            var standardRect = new Rect(area.x + 30, area.y + 138, area.width - 60, 56);
            var infoRect = new Rect(area.x + 30, area.y + 210, area.width - 60, 150);
            var backRect = new Rect(area.x + 30, area.y + h - 60, area.width - 60, 42);

            // Hovering with the mouse moves the selection too.
            Vector2 mouse = Event.current.mousePosition;
            if (journeyRect.Contains(mouse)) _sel = 0;
            else if (standardRect.Contains(mouse)) _sel = 1;

            if (_sel == 0) MenuNav.DrawSelection(journeyRect);
            if (_sel == 1) MenuNav.DrawSelection(standardRect);
            if (_sel == 2) MenuNav.DrawSelection(backRect);

            if (GUI.Button(journeyRect, CreateItems[0], _button)) OpenEditor(journey: true);
            if (GUI.Button(standardRect, CreateItems[1], _button)) OpenEditor(journey: false);

            string info = _sel == 0 ? JourneyInfo
                        : _sel == 1 ? StandardInfo
                        : "Highlight an option to see what it does.";
            MenuTheme.Fill(infoRect, MenuTheme.Cloud);
            MenuTheme.Frame(infoRect, MenuTheme.SkyDeep, 2f);
            GUI.Label(new Rect(infoRect.x + 10, infoRect.y + 8, infoRect.width - 20, infoRect.height - 16), info, _info);

            if (GUI.Button(backRect, "Back", _button)) { _page = Page.Main; _sel = 1; }
        }

        void StartExhibition()
        {
            enabled = false;
            if (teamSelect != null) teamSelect.enabled = true;
        }

        void OpenSettings()
        {
            if (settings == null) return;
            enabled = false;
            settings.Open(Show);
        }

        void OpenEditor(bool journey)
        {
            enabled = false;
            if (createPlayer != null)
            {
                createPlayer.Begin(journey);
                createPlayer.enabled = true;
            }
        }

        void EnsureStyles()
        {
            _title ??= new GUIStyle(GUI.skin.label)
            { fontSize = 26, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = MenuTheme.MarioRed } };
            _button ??= new GUIStyle(GUI.skin.button) { fontSize = 18, fontStyle = FontStyle.Bold };
            _info ??= new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true, normal = { textColor = MenuTheme.Ink } };
        }
    }
}
