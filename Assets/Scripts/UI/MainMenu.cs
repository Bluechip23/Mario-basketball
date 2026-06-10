using UnityEngine;

namespace MarioBasketball.UI
{
    /// <summary>
    /// The game's start menu (IMGUI). Routes to an Exhibition game (the existing
    /// <see cref="TeamSelectMenu"/>) or to Create-a-Player, which offers a
    /// Journey character (limited stats, earns more in story mode) or a Standard
    /// player (unlimited stats, exhibition only) with an info box per option.
    /// One menu is enabled at a time; this component enables the others.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        public TeamSelectMenu teamSelect;
        public CreatePlayerMenu createPlayer;

        const string JourneyInfo =
            "create journey player with limited stats. More stats can be earned " +
            "as you journey through and beat other teams across the kingdom.";
        const string StandardInfo =
            "create standard player with unlimited stats but can only be used in " +
            "exhibition games";

        enum Page { Main, CreateSelect }
        Page _page = Page.Main;

        GUIStyle _title;
        GUIStyle _button;
        GUIStyle _info;

        /// <summary>Bring the menu back up at the top level.</summary>
        public void Show()
        {
            _page = Page.Main;
            enabled = true;
        }

        void OnGUI()
        {
            EnsureStyles();

            var prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.65f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = prev;

            if (_page == Page.Main) DrawMain();
            else DrawCreateSelect();
        }

        void DrawMain()
        {
            float w = 420f, h = 360f;
            var area = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);
            GUILayout.BeginArea(area, GUI.skin.box);
            GUILayout.Space(10);
            GUILayout.Label("MARIO STREET BASKETBALL", _title);
            GUILayout.Space(18);

            if (GUILayout.Button("Exhibition Game", _button, GUILayout.Height(48)))
                StartExhibition();
            if (GUILayout.Button("Create a Player", _button, GUILayout.Height(48)))
                _page = Page.CreateSelect;

            GUI.enabled = false;
            GUILayout.Button("Journey (Story Mode) — coming soon", _button, GUILayout.Height(48));
            GUI.enabled = true;

            if (GUILayout.Button("Quit", _button, GUILayout.Height(40)))
                Application.Quit();

            GUILayout.EndArea();
        }

        void DrawCreateSelect()
        {
            float w = 560f, h = 440f;
            var area = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);
            GUI.Box(area, GUIContent.none);

            GUI.Label(new Rect(area.x, area.y + 12, area.width, 36), "CREATE A PLAYER", _title);

            var journeyRect = new Rect(area.x + 30, area.y + 70, area.width - 60, 56);
            var standardRect = new Rect(area.x + 30, area.y + 138, area.width - 60, 56);
            var infoRect = new Rect(area.x + 30, area.y + 210, area.width - 60, 150);
            var backRect = new Rect(area.x + 30, area.y + h - 60, area.width - 60, 42);

            Vector2 mouse = Event.current.mousePosition;
            bool journeyHover = journeyRect.Contains(mouse);
            bool standardHover = standardRect.Contains(mouse);

            if (GUI.Button(journeyRect, "Create Journey Character", _button)) OpenEditor(journey: true);
            if (GUI.Button(standardRect, "Create Standard Player", _button)) OpenEditor(journey: false);

            string info = journeyHover ? JourneyInfo
                        : standardHover ? StandardInfo
                        : "Highlight an option to see what it does.";
            GUI.Box(infoRect, GUIContent.none);
            GUI.Label(new Rect(infoRect.x + 10, infoRect.y + 8, infoRect.width - 20, infoRect.height - 16), info, _info);

            if (GUI.Button(backRect, "Back", _button)) _page = Page.Main;
        }

        void StartExhibition()
        {
            enabled = false;
            if (teamSelect != null) teamSelect.enabled = true;
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
            { fontSize = 24, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _button ??= new GUIStyle(GUI.skin.button) { fontSize = 18 };
            _info ??= new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true };
        }
    }
}
