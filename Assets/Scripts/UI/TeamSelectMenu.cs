using System.Collections.Generic;
using UnityEngine;
using MarioBasketball.Characters;
using MarioBasketball.Bootstrap;

namespace MarioBasketball.UI
{
    /// <summary>
    /// Pre-match team picker (IMGUI), styled bright and Mario-esque. The screen is
    /// split into two panels: the <b>left</b> is the roster you pick from — a
    /// scrolling wall of <b>character cards</b> grouped by position (GUARDS /
    /// WINGS / BIGS); the <b>right</b> shows the two squads you're building (HOME
    /// and AWAY) plus the controls. A card's front is the character's face (an
    /// empty portrait box for now) and a scouting blurb; pressing B (circle) /
    /// Tab flips every card to its colour-coded stat sheet.
    ///
    /// You draft five characters per side; the first HOME pick is the player
    /// you'll control. Controller-first: d-pad / left stick moves a flashing
    /// yellow outline, A adds (on a card) or removes (on a filled slot), and the
    /// <b>right analog stick scrolls</b> the roster. The mouse still works. On
    /// Start it hands the two rosters to <see cref="GameBootstrap.StartMatch"/>.
    /// </summary>
    public class TeamSelectMenu : MonoBehaviour
    {
        public GameBootstrap bootstrap;
        public MainMenu mainMenu;

        const int TeamSize = 5;
        const int TeamItemCount = 2 * TeamSize + 4; // home + away slots + switch/random/back/start
        const float CardW = 172f;
        const float CardH = 236f;
        const float CardPad = 10f;
        const float ScrollSpeed = 1100f; // right-stick scroll, px/sec

        // ---- Mario palette (shared look lives in MenuTheme) -----------------
        static readonly Color Cloud      = MenuTheme.Cloud;
        static readonly Color Cream      = MenuTheme.Cream;
        static readonly Color MarioRed   = MenuTheme.MarioRed;
        static readonly Color LuigiGreen = MenuTheme.LuigiGreen;
        static readonly Color SkyDeep    = MenuTheme.SkyDeep;
        static readonly Color Coin       = MenuTheme.Coin;
        static readonly Color Ink        = MenuTheme.Ink;
        static readonly Color HomeTint   = new Color(0.99f, 0.84f, 0.82f); // soft red wash
        static readonly Color AwayTint   = new Color(0.82f, 0.89f, 1f);    // soft blue wash

        static readonly Color Gold  = new Color(0.86f, 0.65f, 0.05f);
        static readonly Color Green = new Color(0.16f, 0.55f, 0.20f);
        static readonly Color Red   = new Color(0.85f, 0.20f, 0.18f);

        List<CharacterStats> _roster = new List<CharacterStats>();
        readonly List<int> _home = new List<int>();
        readonly List<int> _away = new List<int>();
        bool _editingAway;
        bool _showStats;     // circle flips ALL cards to the stat side
        Vector2 _scroll;
        MenuNav _nav;

        // ---- Focus (controller cursor) --------------------------------------
        enum Zone { Cards, Team }
        Zone _zone = Zone.Cards;
        int _cardIndex;
        int _teamIndex; // 0-4 HOME slots, 5-9 AWAY slots, 10 switch, 11 random, 12 back, 13 start

        struct CardEntry
        {
            public int rosterIndex;
            public int row, col;   // grid position for navigation
            public Rect rect;      // in scroll-content space
        }

        struct Layout
        {
            public Rect window, title;
            public Rect leftPanel, rightPanel;
            public Rect scrollView;
            public float contentHeight;
            public int cardRows, cols;
            public List<CardEntry> cards;
            public List<(string label, Rect rect, Color color)> sections;
            public Rect homeBox, awayBox;
            public Rect[] homeSlots, awaySlots;
            public Rect switchRect, randomRect, backRect, startRect;
        }

        GUIStyle _title;
        GUIStyle _panelHeader;
        GUIStyle _teamHeader;
        GUIStyle _button;
        GUIStyle _slotButton;
        GUIStyle _hint;
        GUIStyle _sectionChip;
        GUIStyle _cardName;
        GUIStyle _cardText;
        GUIStyle _cardTag;
        GUIStyle _statLabel;
        GUIStyle _statValue;

        void OnEnable()
        {
            // Rebuild the pool each time so freshly created players appear.
            _roster = new List<CharacterStats>(CharacterLibrary.All());
            foreach (var created in CreatedPlayerStore.All())
                if (created != null && created.stats != null) _roster.Add(created.stats);

            _home.Clear();
            _away.Clear();
            _editingAway = false;
            _showStats = false;
            _zone = Zone.Cards;
            _cardIndex = 0;
            _teamIndex = 0;
            _scroll = Vector2.zero;
            Prefill(_home, "Mario", "Luigi", "Peach", "Toad", "Diddy Kong");
            Prefill(_away, "Bowser", "Donkey Kong", "Waluigi", "Yoshi", "Boo");

            _nav = new MenuNav();
            _nav.Enable();
        }

        void OnDisable()
        {
            _nav?.Disable();
            _nav = null;
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

        // ---- Controller navigation ------------------------------------------

        void Update()
        {
            _nav.Tick();
            var layout = BuildLayout();

            if (_nav.East) _showStats = !_showStats; // circle: flip every card

            if (_nav.Step != Vector2Int.zero) Navigate(layout, _nav.Step);
            if (_nav.Submit) ActivateFocused(layout);

            // Submitting START / Back disables this menu mid-Update, which runs
            // OnDisable and nulls _nav. Bail before touching it again this frame.
            if (!enabled || _nav == null) return;

            // Right analog stick free-scrolls the roster (up = scroll up).
            float maxScroll = Mathf.Max(0f, layout.contentHeight - layout.scrollView.height);
            if (Mathf.Abs(_nav.RightStick.y) > 0.2f)
                _scroll.y -= _nav.RightStick.y * ScrollSpeed * Time.unscaledDeltaTime;
            _scroll.y = Mathf.Clamp(_scroll.y, 0f, maxScroll);

            if (_zone == Zone.Cards) ScrollToCard(layout);
        }

        void Navigate(Layout layout, Vector2Int step)
        {
            switch (_zone)
            {
                case Zone.Cards:
                    if (layout.cards.Count == 0)
                    {
                        if (step.x > 0) { _zone = Zone.Team; _teamIndex = 0; }
                        break;
                    }
                    _cardIndex = Mathf.Clamp(_cardIndex, 0, layout.cards.Count - 1);
                    var cur = layout.cards[_cardIndex];
                    if (step.x > 0)
                    {
                        int next = _cardIndex + 1;
                        if (next < layout.cards.Count && layout.cards[next].row == cur.row) _cardIndex = next;
                        else { _zone = Zone.Team; _teamIndex = 0; } // off the right edge → squads
                    }
                    else if (step.x < 0)
                    {
                        int prev = _cardIndex - 1;
                        if (prev >= 0 && layout.cards[prev].row == cur.row) _cardIndex = prev;
                    }
                    if (step.y < 0) // down
                    {
                        if (cur.row < layout.cardRows - 1) _cardIndex = NearestCardInRow(layout, cur.row + 1, cur.col);
                    }
                    else if (step.y > 0) // up
                    {
                        if (cur.row > 0) _cardIndex = NearestCardInRow(layout, cur.row - 1, cur.col);
                    }
                    break;

                case Zone.Team:
                    if (step.x < 0) { _zone = Zone.Cards; break; } // back to the roster
                    _teamIndex = Mathf.Clamp(_teamIndex - step.y, 0, TeamItemCount - 1);
                    break;
            }
        }

        int NearestCardInRow(Layout layout, int row, float col)
        {
            int best = 0;
            float bestD = float.MaxValue;
            for (int i = 0; i < layout.cards.Count; i++)
            {
                if (layout.cards[i].row != row) continue;
                float d = Mathf.Abs(layout.cards[i].col - col);
                if (d < bestD) { bestD = d; best = i; }
            }
            return best;
        }

        void ActivateFocused(Layout layout)
        {
            switch (_zone)
            {
                case Zone.Cards:
                    if (_cardIndex < layout.cards.Count) AddToTeam(layout.cards[_cardIndex].rosterIndex);
                    break;

                case Zone.Team:
                    if (_teamIndex < TeamSize)
                    {
                        if (_teamIndex < _home.Count) _home.RemoveAt(_teamIndex);
                    }
                    else if (_teamIndex < 2 * TeamSize)
                    {
                        int j = _teamIndex - TeamSize;
                        if (j < _away.Count) _away.RemoveAt(j);
                    }
                    else if (_teamIndex == 2 * TeamSize) _editingAway = !_editingAway;
                    else if (_teamIndex == 2 * TeamSize + 1) Randomize(_editingAway ? _away : _home);
                    else if (_teamIndex == 2 * TeamSize + 2) BackToMain();
                    else if (Ready) StartGame();
                    break;
            }
        }

        void ScrollToCard(Layout layout)
        {
            if (_cardIndex >= layout.cards.Count) return;
            Rect r = layout.cards[_cardIndex].rect;
            float viewH = layout.scrollView.height;
            if (r.y < _scroll.y) _scroll.y = Mathf.Max(0f, r.y - 8f);
            else if (r.yMax > _scroll.y + viewH) _scroll.y = r.yMax - viewH + 8f;
        }

        // ---- Actions ---------------------------------------------------------

        bool Ready => _home.Count == TeamSize && _away.Count == TeamSize;

        void AddToTeam(int rosterIndex)
        {
            var team = _editingAway ? _away : _home;
            if (team.Count < TeamSize && !team.Contains(rosterIndex)) team.Add(rosterIndex);
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

        void BackToMain()
        {
            enabled = false;
            if (mainMenu != null) mainMenu.Show();
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

        // ---- Layout ----------------------------------------------------------

        Layout BuildLayout()
        {
            var l = new Layout();
            float w = Mathf.Min(Screen.width - 40f, 1180f);
            float h = Screen.height - 40f;
            l.window = new Rect((Screen.width - w) / 2f, 20f, w, h);
            l.title = new Rect(l.window.x, l.window.y + 10f, l.window.width, 36f);

            float top = l.window.y + 56f;
            float bottom = l.window.yMax - 14f;
            float leftW = Mathf.Round(w * 0.60f) - 18f;
            l.leftPanel = new Rect(l.window.x + 14f, top, leftW, bottom - top);
            float rx = l.leftPanel.xMax + 16f;
            l.rightPanel = new Rect(rx, top, l.window.xMax - 14f - rx, bottom - top);

            // Left: hint line, then the scrolling card wall.
            l.scrollView = new Rect(l.leftPanel.x + 10f, l.leftPanel.y + 56f,
                l.leftPanel.width - 20f, l.leftPanel.yMax - (l.leftPanel.y + 56f) - 10f);

            l.cards = new List<CardEntry>();
            l.sections = new List<(string, Rect, Color)>();
            l.cols = Mathf.Max(1, Mathf.FloorToInt((l.scrollView.width - 16f) / (CardW + CardPad)));
            float cy = 0f;
            int row = 0;
            foreach (var group in new[] { PlayerArchetype.Guard, PlayerArchetype.Wing, PlayerArchetype.Big })
            {
                var members = new List<int>();
                for (int i = 0; i < _roster.Count; i++)
                    if (_roster[i].Archetype == group) members.Add(i);
                if (members.Count == 0) continue;

                string label = group == PlayerArchetype.Guard ? "GUARDS" : group == PlayerArchetype.Wing ? "WINGS" : "BIGS";
                l.sections.Add((label, new Rect(0f, cy, l.scrollView.width - 16f, 24f), ArchColor(group)));
                cy += 30f;

                for (int i = 0; i < members.Count; i++)
                {
                    int col = i % l.cols;
                    if (i > 0 && col == 0) { cy += CardH + CardPad; row++; }
                    l.cards.Add(new CardEntry
                    {
                        rosterIndex = members[i],
                        row = row,
                        col = col,
                        rect = new Rect(col * (CardW + CardPad), cy, CardW, CardH)
                    });
                }
                cy += CardH + CardPad + 10f;
                row++;
            }
            l.contentHeight = cy;
            l.cardRows = row;

            // Right: the two squads stacked, then the controls.
            float x = l.rightPanel.x + 12f;
            float colW = l.rightPanel.width - 24f;
            float y = l.rightPanel.y + 40f; // below the panel header

            l.homeSlots = new Rect[TeamSize];
            l.awaySlots = new Rect[TeamSize];
            y = LayoutTeamBox(ref l.homeBox, l.homeSlots, x, y, colW);
            y += 8f;
            y = LayoutTeamBox(ref l.awayBox, l.awaySlots, x, y, colW);
            y += 10f;

            // Controls, anchored as a clean stack to the bottom of the panel so they
            // always line up regardless of how tall the squad boxes end up: the
            // back/start row sits on the floor, with switch + randomize stacked
            // directly above it.
            float by = l.rightPanel.yMax - 48f;                 // back / start row
            l.backRect = new Rect(x, by, colW * 0.38f - 4f, 40f);
            l.startRect = new Rect(x + colW * 0.38f + 4f, by, colW * 0.62f - 4f, 40f);
            l.randomRect = new Rect(x, by - 38f, colW, 30f);
            l.switchRect = new Rect(x, by - 74f, colW, 30f);
            return l;
        }

        float LayoutTeamBox(ref Rect box, Rect[] slots, float x, float y, float w)
        {
            float boxTop = y;
            float innerY = y + 28f; // below the team label
            for (int i = 0; i < TeamSize; i++)
            {
                slots[i] = new Rect(x + 6f, innerY, w - 12f, 30f);
                innerY += 34f;
            }
            box = new Rect(x, boxTop, w, innerY - boxTop + 4f);
            return box.yMax;
        }

        // ---- Drawing ---------------------------------------------------------

        void OnGUI()
        {
            EnsureStyles();
            var layout = BuildLayout();

            // Bright Mario sky behind the whole screen, then a cream window.
            MenuTheme.DrawBackground();
            Fill(layout.window, Cream);
            Frame(layout.window, MarioRed, 4f);
            GUI.Label(layout.title, "TEAM SELECT", _title);

            DrawRosterPanel(layout);
            DrawSquadsPanel(layout);
        }

        void DrawRosterPanel(Layout layout)
        {
            Fill(layout.leftPanel, Cloud);
            Frame(layout.leftPanel, SkyDeep, 3f);
            GUI.Label(new Rect(layout.leftPanel.x + 12f, layout.leftPanel.y + 8f, layout.leftPanel.width - 24f, 24f),
                "CHOOSE YOUR SQUAD", _panelHeader);
            GUI.Label(new Rect(layout.leftPanel.x + 12f, layout.leftPanel.y + 32f, layout.leftPanel.width - 24f, 20f),
                $"A / click: add to {(_editingAway ? "AWAY" : "HOME")}  ·  B / Tab: flip to stats  ·  right stick: scroll",
                _hint);

            _scroll = GUI.BeginScrollView(layout.scrollView, _scroll,
                new Rect(0f, 0f, layout.scrollView.width - 16f, layout.contentHeight));

            foreach (var (label, rect, color) in layout.sections)
            {
                Fill(rect, color);
                GUI.Label(rect, "  " + label, _sectionChip);
            }

            for (int i = 0; i < layout.cards.Count; i++)
            {
                var entry = layout.cards[i];
                if (_zone == Zone.Cards && _cardIndex == i) MenuNav.DrawSelection(entry.rect);
                if (DrawCard(entry.rect, _roster[entry.rosterIndex], entry.rosterIndex))
                {
                    _zone = Zone.Cards; _cardIndex = i;
                    AddToTeam(entry.rosterIndex);
                }
            }

            GUI.EndScrollView();
        }

        void DrawSquadsPanel(Layout layout)
        {
            Fill(layout.rightPanel, Cloud);
            Frame(layout.rightPanel, Coin, 3f);
            GUI.Label(new Rect(layout.rightPanel.x + 12f, layout.rightPanel.y + 8f, layout.rightPanel.width - 24f, 24f),
                "YOUR SQUADS", _panelHeader);

            DrawTeamBox(layout, layout.homeBox, layout.homeSlots, "HOME", _home, HomeTint, MarioRed, slotBase: 0, editing: !_editingAway);
            DrawTeamBox(layout, layout.awayBox, layout.awaySlots, "AWAY", _away, AwayTint, SkyDeep, slotBase: TeamSize, editing: _editingAway);

            // Controls.
            if (_zone == Zone.Team && _teamIndex == 2 * TeamSize) MenuNav.DrawSelection(layout.switchRect);
            if (GUI.Button(layout.switchRect, _editingAway ? "Editing AWAY  →  switch to HOME" : "Editing HOME  →  switch to AWAY", _button))
                _editingAway = !_editingAway;

            if (_zone == Zone.Team && _teamIndex == 2 * TeamSize + 1) MenuNav.DrawSelection(layout.randomRect);
            if (GUI.Button(layout.randomRect, $"Randomize {(_editingAway ? "AWAY" : "HOME")}", _button))
                Randomize(_editingAway ? _away : _home);

            if (_zone == Zone.Team && _teamIndex == 2 * TeamSize + 2) MenuNav.DrawSelection(layout.backRect);
            if (GUI.Button(layout.backRect, "Back", _button)) BackToMain();

            if (_zone == Zone.Team && _teamIndex == 2 * TeamSize + 3 && Ready) MenuNav.DrawSelection(layout.startRect);
            GUI.enabled = Ready;
            if (GUI.Button(layout.startRect, Ready ? "START GAME" : "Pick 5 per team", _button)) StartGame();
            GUI.enabled = true;
        }

        void DrawTeamBox(Layout layout, Rect box, Rect[] slots, string label, List<int> team,
            Color tint, Color accent, int slotBase, bool editing)
        {
            Fill(box, tint);
            Frame(box, accent, editing ? 4f : 2f);
            if (editing) MenuNav.DrawSelection(box, 2f); // glow the squad you're editing

            GUI.Label(new Rect(box.x + 8f, box.y + 4f, box.width - 16f, 22f),
                $"{(editing ? "▶ " : "")}{label}  ({team.Count}/{TeamSize})", _teamHeader);

            for (int slot = 0; slot < TeamSize; slot++)
            {
                Rect r = slots[slot];
                int navIdx = slotBase + slot;
                if (_zone == Zone.Team && _teamIndex == navIdx) MenuNav.DrawSelection(r);
                if (slot < team.Count)
                {
                    string tag = slot == 0 && slotBase == 0 ? "★ " : "";
                    if (GUI.Button(r, tag + _roster[team[slot]].characterName, _slotButton))
                        team.RemoveAt(slot);
                }
                else
                {
                    Fill(r, new Color(1f, 1f, 1f, 0.45f));
                    GUI.Label(r, "—", _cardTag);
                }
            }
        }

        /// <summary>One character card. Front: portrait placeholder + scouting
        /// blurb. Back (all cards flip together): the colour-coded stat sheet.
        /// Returns true when clicked.</summary>
        bool DrawCard(Rect r, CharacterStats s, int rosterIndex)
        {
            Color arch = ArchColor(s.Archetype);
            Fill(r, Cloud);
            Frame(r, arch, 3f);
            bool clicked = GUI.Button(r, GUIContent.none, GUIStyle.none);

            // Name banner in the archetype colour.
            var banner = new Rect(r.x + 3f, r.y + 3f, r.width - 6f, 24f);
            Fill(banner, arch);
            GUI.Label(banner, s.characterName, _cardName);

            if (_showStats) DrawCardStats(r, s);
            else DrawCardFront(r, s);

            // Already-drafted badge.
            if (_home.Contains(rosterIndex)) DrawBadge(r, "HOME", MarioRed);
            else if (_away.Contains(rosterIndex)) DrawBadge(r, "AWAY", SkyDeep);
            return clicked;
        }

        void DrawCardFront(Rect r, CharacterStats s)
        {
            // Portrait placeholder — an empty box until real character art lands.
            var face = new Rect(r.x + 12f, r.y + 32f, r.width - 24f, 80f);
            Fill(face, new Color(0.90f, 0.92f, 0.96f));
            Frame(face, new Color(0.7f, 0.74f, 0.8f), 1f);
            GUI.Label(face, "(face)", _cardTag);

            GUI.Label(new Rect(r.x + 12f, face.yMax + 2f, r.width - 24f, 16f),
                $"{s.Archetype.ToString().ToUpper()} · {s.heightMeters:0.0} m", _cardTag);

            GUI.Label(new Rect(r.x + 10f, face.yMax + 20f, r.width - 20f, r.yMax - (face.yMax + 24f)),
                s.description, _cardText);
        }

        void DrawCardStats(Rect r, CharacterStats s)
        {
            (string label, int value)[] stats =
            {
                ("Spd", s.speed), ("BH", s.ballHandling), ("3PT", s.threePoint), ("Mid", s.midRange),
                ("Ins", s.insideScoring), ("PsO", s.postOffense), ("Dnk", s.dunk),
                ("Pow", s.power), ("Reb", s.rebounds), ("Blk", s.blocks), ("Stl", s.steals),
                ("PsD", s.postDefense), ("PrD", s.perimeterDefense), ("Sta", s.stamina)
            };

            float colW = (r.width - 24f) / 2f;
            const float rowH = 24f;
            for (int i = 0; i < stats.Length; i++)
            {
                float cx = r.x + 12f + (i / 7) * colW;
                float cy = r.y + 34f + (i % 7) * rowH;
                GUI.Label(new Rect(cx, cy, colW * 0.55f, rowH), stats[i].label, _statLabel);
                var prev = GUI.color;
                GUI.color = StatColor(stats[i].value);
                GUI.Label(new Rect(cx + colW * 0.55f, cy, colW * 0.4f, rowH), stats[i].value.ToString(), _statValue);
                GUI.color = prev;
            }
        }

        /// <summary>10 is gold, 7-8 green, 4-6 dark ink, 1-3 red.</summary>
        static Color StatColor(int value) =>
            value >= 10 ? Gold :
            value >= 7 ? Green :
            value >= 4 ? Ink :
            Red;

        static Color ArchColor(PlayerArchetype a) =>
            a == PlayerArchetype.Guard ? SkyDeep :
            a == PlayerArchetype.Wing ? LuigiGreen :
            MarioRed;

        void DrawBadge(Rect card, string text, Color color)
        {
            var r = new Rect(card.xMax - 52f, card.yMax - 22f, 48f, 18f);
            Fill(r, color);
            GUI.Label(r, text, _cardTag);
        }

        // ---- Colour helpers (shared look lives in MenuTheme) ----------------

        static void Fill(Rect r, Color c) => MenuTheme.Fill(r, c);
        static void Frame(Rect r, Color c, float t) => MenuTheme.Frame(r, c, t);

        void EnsureStyles()
        {
            _title ??= new GUIStyle(GUI.skin.label)
            { fontSize = 28, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = MarioRed } };
            _panelHeader ??= new GUIStyle(GUI.skin.label)
            { fontSize = 17, fontStyle = FontStyle.Bold, normal = { textColor = SkyDeep } };
            _teamHeader ??= new GUIStyle(GUI.skin.label)
            { fontSize = 15, fontStyle = FontStyle.Bold, normal = { textColor = Ink } };
            _button ??= new GUIStyle(GUI.skin.button) { fontSize = 13, fontStyle = FontStyle.Bold };
            _slotButton ??= new GUIStyle(GUI.skin.button) { fontSize = 13, alignment = TextAnchor.MiddleLeft };
            _hint ??= new GUIStyle(GUI.skin.label) { fontSize = 11, normal = { textColor = new Color(0.35f, 0.4f, 0.45f) } };
            _sectionChip ??= new GUIStyle(GUI.skin.label)
            { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, normal = { textColor = Color.white } };
            _cardName ??= new GUIStyle(GUI.skin.label)
            { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
            _cardText ??= new GUIStyle(GUI.skin.label)
            { fontSize = 11, wordWrap = true, alignment = TextAnchor.UpperLeft, normal = { textColor = Ink } };
            _cardTag ??= new GUIStyle(GUI.skin.label)
            { fontSize = 11, alignment = TextAnchor.MiddleCenter, normal = { textColor = Ink } };
            _statLabel ??= new GUIStyle(GUI.skin.label) { fontSize = 13, normal = { textColor = Ink } };
            _statValue ??= new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleRight };
        }
    }
}
