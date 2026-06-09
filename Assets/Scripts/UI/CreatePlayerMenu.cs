using UnityEngine;
using MarioBasketball.Characters;

namespace MarioBasketball.UI
{
    /// <summary>
    /// The stat editor for Create-a-Player (IMGUI). Two modes:
    /// <list type="bullet">
    ///   <item><b>Standard</b> — unlimited: every stat is freely set 1-10.</item>
    ///   <item><b>Journey</b> — a point budget (starts at 10) where raising a
    ///   stat costs more as it climbs: reaching 1-3 costs 1 each, 4-5 cost 2,
    ///   6-8 cost 3, 9 costs 4, 10 costs 5.</item>
    /// </list>
    /// On save the player is stored via <see cref="CreatedPlayerStore"/> and
    /// becomes available in exhibition team select.
    /// </summary>
    public class CreatePlayerMenu : MonoBehaviour
    {
        public MainMenu mainMenu;

        public const int JourneyStartingPoints = 10;

        static readonly string[] StatNames =
        {
            "Speed", "Ball Handling", "3-Point", "Mid Range", "Inside Scoring",
            "Post Offense", "Dunk", "Power", "Rebounds", "Blocks", "Steals",
            "Post Defense", "Perimeter Defense", "Stamina"
        };

        bool _journey;
        string _name = "My Player";
        readonly int[] _values = new int[14];
        int _remaining;
        Vector2 _scroll;

        GUIStyle _title;
        GUIStyle _header;
        GUIStyle _button;
        GUIStyle _row;

        /// <summary>Start a fresh creation in the given mode.</summary>
        public void Begin(bool journey)
        {
            _journey = journey;
            _name = journey ? "Journey Player" : "Created Player";
            for (int i = 0; i < _values.Length; i++) _values[i] = 1; // base 1
            _remaining = JourneyStartingPoints;
            _scroll = Vector2.zero;
        }

        /// <summary>Point cost of raising a stat <i>to</i> the given level.</summary>
        public static int CostToReach(int level)
        {
            if (level <= 3) return 1;
            if (level <= 5) return 2;
            if (level <= 8) return 3;
            if (level == 9) return 4;
            return 5; // 10
        }

        void OnGUI()
        {
            EnsureStyles();

            var prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.65f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = prev;

            float w = 560f, h = Mathf.Min(Screen.height - 40f, 620f);
            GUILayout.BeginArea(new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h), GUI.skin.box);

            GUILayout.Label(_journey ? "CREATE JOURNEY CHARACTER" : "CREATE STANDARD PLAYER", _title);

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Name:", _header, GUILayout.Width(60));
            _name = GUILayout.TextField(_name, 24, GUILayout.Width(280));
            GUILayout.EndHorizontal();

            GUILayout.Label(_journey
                ? $"Points remaining: {_remaining}   (cost rises as a stat climbs: 1-3=1, 4-5=2, 6-8=3, 9=4, 10=5)"
                : "Unlimited stats. Standard players are exhibition-only.", _header);

            _scroll = GUILayout.BeginScrollView(_scroll);
            for (int i = 0; i < StatNames.Length; i++) DrawStatRow(i);
            GUILayout.EndScrollView();

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Player", _button, GUILayout.Height(40))) Save();
            if (GUILayout.Button("Cancel", _button, GUILayout.Height(40))) Close();
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        void DrawStatRow(int i)
        {
            int v = _values[i];
            GUILayout.BeginHorizontal();
            GUILayout.Label(StatNames[i], _row, GUILayout.Width(150));

            GUI.enabled = v > 1;
            if (GUILayout.Button("-", _button, GUILayout.Width(34))) Decrement(i);
            GUI.enabled = true;

            GUILayout.Label(v.ToString(), _row, GUILayout.Width(28));

            int nextCost = CostToReach(v + 1);
            GUI.enabled = v < 10 && (!_journey || nextCost <= _remaining);
            if (GUILayout.Button("+", _button, GUILayout.Width(34))) Increment(i);
            GUI.enabled = true;

            if (_journey && v < 10)
                GUILayout.Label($"(+{nextCost} pt)", _row, GUILayout.Width(70));

            GUILayout.EndHorizontal();
        }

        void Increment(int i)
        {
            int v = _values[i];
            if (v >= 10) return;
            int cost = CostToReach(v + 1);
            if (_journey && cost > _remaining) return;
            _values[i] = v + 1;
            if (_journey) _remaining -= cost;
        }

        void Decrement(int i)
        {
            int v = _values[i];
            if (v <= 1) return;
            if (_journey) _remaining += CostToReach(v); // refund the level being removed
            _values[i] = v - 1;
        }

        void Save()
        {
            var stats = new CharacterStats
            {
                characterName = string.IsNullOrWhiteSpace(_name) ? "Created Player" : _name.Trim(),
                speed = _values[0], ballHandling = _values[1], threePoint = _values[2],
                midRange = _values[3], insideScoring = _values[4], postOffense = _values[5],
                dunk = _values[6], power = _values[7], rebounds = _values[8], blocks = _values[9],
                steals = _values[10], postDefense = _values[11], perimeterDefense = _values[12],
                stamina = _values[13], hiddenTrait = HiddenTrait.None
            };
            stats.Validate();
            CreatedPlayerStore.Add(stats, _journey);
            Close();
        }

        void Close()
        {
            enabled = false;
            if (mainMenu != null) mainMenu.Show();
        }

        void EnsureStyles()
        {
            _title ??= new GUIStyle(GUI.skin.label)
            { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _header ??= new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };
            _button ??= new GUIStyle(GUI.skin.button) { fontSize = 16 };
            _row ??= new GUIStyle(GUI.skin.label) { fontSize = 15 };
        }
    }
}
