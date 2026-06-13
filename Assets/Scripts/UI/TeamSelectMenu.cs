using System.Collections.Generic;
using UnityEngine;
using MarioBasketball.Characters;
using MarioBasketball.Bootstrap;

namespace MarioBasketball.UI
{
    /// <summary>
    /// Pre-match team picker (IMGUI). The roster is laid out as <b>character
    /// cards</b> grouped by position — GUARDS / WINGS / BIGS. A card's front is
    /// the character's face (an empty portrait box for now) and a scouting
    /// blurb; pressing B (circle) flips every card over to its stat sheet, with
    /// values colour-coded (10 gold, 7-8 green, 4-6 plain, 1-3 red).
    ///
    /// The player drafts five characters per side; the first HOME pick is the
    /// player they'll control. Controller-first: d-pad / stick moves a flashing
    /// yellow outline between the team slots, the cards and the bottom buttons;
    /// A adds (or removes, on a slot). The mouse still works. On Start it hands
    /// the two rosters to <see cref="GameBootstrap.StartMatch"/>.
    /// </summary>
    public class TeamSelectMenu : MonoBehaviour
    {
        public GameBootstrap bootstrap;
        public MainMenu mainMenu;

        const int TeamSize = 5;
        const float CardW = 180f;
        const float CardH = 240f;
        const float CardPad = 10f;

        static readonly Color Gold = new Color(1f, 0.84f, 0.1f);
        static readonly Color Green = new Color(0.35f, 0.85f, 0.35f);
        static readonly Color Red = new Color(0.95f, 0.3f, 0.25f);

        List<CharacterStats> _roster = new List<CharacterStats>();
        readonly List<int> _home = new List<int>();
        readonly List<int> _away = new List<int>();
        bool _editingAway;
        bool _showStats;     // circle flips ALL cards to the stat side
        Vector2 _scroll;
        MenuNav _nav;

        // ---- Focus (controller cursor) --------------------------------------
        enum Zone { Slots, Cards, Bottom }
        Zone _zone = Zone.Cards;
        int _slotRow;   // 0 = HOME slots, 1 = AWAY slots, 2 = control buttons
        int _slotCol;
        int _cardIndex;
        int _bottomCol; // 0 = Back, 1 = Start

        struct CardEntry
        {
            public int rosterIndex;
            public int row, col;   // grid position for navigation
            public Rect rect;      // in scroll-content space
        }

        struct Layout
        {
            public Rect area;
            public Rect[][] slotRects;   // [home, away, controls]
            public Rect scrollView;
            public float contentHeight;
            public List<CardEntry> cards;
            public List<(string label, Rect rect)> sections;
            public Rect backRect, startRect;
            public int cardRows;
        }

        GUIStyle _title;
        GUIStyle _header;
        GUIStyle _button;
        GUIStyle _hint;
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

            if (_zone == Zone.Cards) ScrollToCard(layout);
        }

        void Navigate(Layout layout, Vector2Int step)
        {
            switch (_zone)
            {
                case Zone.Slots:
                    int cols = _slotRow == 2 ? layout.slotRects[2].Length : TeamSize;
                    _slotCol = Mathf.Clamp(_slotCol + step.x, 0, cols - 1);
                    if (step.y > 0) _slotRow = Mathf.Max(0, _slotRow - 1);
                    else if (step.y < 0)
                    {
                        if (_slotRow < 2) _slotRow++;
                        else { _zone = Zone.Cards; _cardIndex = NearestCardInRow(layout, 0, _slotCol * 0.5f); }
                    }
                    _slotCol = Mathf.Clamp(_slotCol, 0, (_slotRow == 2 ? layout.slotRects[2].Length : TeamSize) - 1);
                    break;

                case Zone.Cards:
                    if (layout.cards.Count == 0) { if (step.y != 0) _zone = step.y < 0 ? Zone.Bottom : Zone.Slots; break; }
                    _cardIndex = Mathf.Clamp(_cardIndex, 0, layout.cards.Count - 1);
                    var cur = layout.cards[_cardIndex];
                    if (step.x != 0)
                    {
                        int next = _cardIndex + step.x;
                        if (next >= 0 && next < layout.cards.Count && layout.cards[next].row == cur.row)
                            _cardIndex = next;
                    }
                    if (step.y < 0) // down
                    {
                        if (cur.row >= layout.cardRows - 1) { _zone = Zone.Bottom; _bottomCol = 1; }
                        else _cardIndex = NearestCardInRow(layout, cur.row + 1, cur.col);
                    }
                    else if (step.y > 0) // up
                    {
                        if (cur.row == 0) { _zone = Zone.Slots; _slotRow = 2; _slotCol = 0; }
                        else _cardIndex = NearestCardInRow(layout, cur.row - 1, cur.col);
                    }
                    break;

                case Zone.Bottom:
                    _bottomCol = Mathf.Clamp(_bottomCol + step.x, 0, 1);
                    if (step.y > 0)
                    {
                        _zone = Zone.Cards;
                        _cardIndex = NearestCardInRow(layout, layout.cardRows - 1, 1);
                    }
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
                case Zone.Slots:
                    if (_slotRow == 0 && _slotCol < _home.Count) _home.RemoveAt(_slotCol);
                    else if (_slotRow == 1 && _slotCol < _away.Count) _away.RemoveAt(_slotCol);
                    else if (_slotRow == 2 && _slotCol == 0) _editingAway = !_editingAway;
                    else if (_slotRow == 2 && _slotCol == 1) Randomize(_editingAway ? _away : _home);
                    break;

                case Zone.Cards:
                    if (_cardIndex < layout.cards.Count) AddToTeam(layout.cards[_cardIndex].rosterIndex);
                    break;

                case Zone.Bottom:
                    if (_bottomCol == 0) BackToMain();
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
            float w = Mathf.Min(Screen.width - 40f, 1000f);
            float h = Screen.height - 40f;
            l.area = new Rect((Screen.width - w) / 2f, 20f, w, h);

            float x = l.area.x + 14f;
            float innerW = w - 28f;
            float y = l.area.y + 52f; // below the title

            l.slotRects = new Rect[3][];
            float slotW = (innerW - 110f - 4f * 6f) / TeamSize;
            for (int row = 0; row < 2; row++)
            {
                l.slotRects[row] = new Rect[TeamSize];
                for (int c = 0; c < TeamSize; c++)
                    l.slotRects[row][c] = new Rect(x + 110f + c * (slotW + 6f), y, slotW, 30f);
                y += 36f;
            }
            l.slotRects[2] = new Rect[2];
            l.slotRects[2][0] = new Rect(x, y, innerW * 0.5f - 4f, 32f);
            l.slotRects[2][1] = new Rect(x + innerW * 0.5f + 4f, y, innerW * 0.5f - 4f, 32f);
            y += 38f;

            // Hint line, then the card scroller fills down to the bottom buttons.
            y += 22f;
            float bottomH = 46f;
            l.scrollView = new Rect(x, y, innerW, l.area.yMax - y - bottomH - 16f);

            // Cards grouped by archetype, in scroll-content space.
            l.cards = new List<CardEntry>();
            l.sections = new List<(string, Rect)>();
            int cols = Mathf.Max(1, Mathf.FloorToInt((innerW - 16f) / (CardW + CardPad)));
            float cy = 0f;
            int row2 = 0;
            foreach (var group in new[] { PlayerArchetype.Guard, PlayerArchetype.Wing, PlayerArchetype.Big })
            {
                var members = new List<int>();
                for (int i = 0; i < _roster.Count; i++)
                    if (_roster[i].Archetype == group) members.Add(i);
                if (members.Count == 0) continue;

                string label = group == PlayerArchetype.Guard ? "GUARDS" : group == PlayerArchetype.Wing ? "WINGS" : "BIGS";
                l.sections.Add((label, new Rect(0f, cy, innerW - 16f, 24f)));
                cy += 28f;

                for (int i = 0; i < members.Count; i++)
                {
                    int col = i % cols;
                    if (i > 0 && col == 0) { cy += CardH + CardPad; row2++; }
                    l.cards.Add(new CardEntry
                    {
                        rosterIndex = members[i],
                        row = row2,
                        col = col,
                        rect = new Rect(col * (CardW + CardPad), cy, CardW, CardH)
                    });
                }
                cy += CardH + CardPad + 8f;
                row2++;
            }
            l.contentHeight = cy;
            l.cardRows = row2;

            l.backRect = new Rect(x, l.area.yMax - bottomH - 8f, 130f, bottomH);
            l.startRect = new Rect(x + 140f, l.area.yMax - bottomH - 8f, innerW - 140f, bottomH);
            return l;
        }

        // ---- Drawing ---------------------------------------------------------

        void OnGUI()
        {
            EnsureStyles();
            var layout = BuildLayout();

            var prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = prev;

            GUI.Box(layout.area, GUIContent.none);
            GUI.Label(new Rect(layout.area.x, layout.area.y + 10f, layout.area.width, 32f), "TEAM SELECT", _title);

            DrawSlotRow(layout, 0, "HOME", _home, editing: !_editingAway);
            DrawSlotRow(layout, 1, "AWAY", _away, editing: _editingAway);
            DrawControls(layout);

            GUI.Label(new Rect(layout.scrollView.x, layout.scrollView.y - 22f, layout.scrollView.width, 20f),
                $"A / click a card: add to {(_editingAway ? "AWAY" : "HOME")} · B (circle) / Tab: flip cards to stats · a filled slot above: remove",
                _hint);

            _scroll = GUI.BeginScrollView(layout.scrollView, _scroll,
                new Rect(0f, 0f, layout.scrollView.width - 16f, layout.contentHeight));

            foreach (var (label, rect) in layout.sections)
                GUI.Label(rect, label, _header);

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

            if (_zone == Zone.Bottom && _bottomCol == 0) MenuNav.DrawSelection(layout.backRect);
            if (_zone == Zone.Bottom && _bottomCol == 1) MenuNav.DrawSelection(layout.startRect);
            if (GUI.Button(layout.backRect, "Back", _button)) BackToMain();
            GUI.enabled = Ready;
            if (GUI.Button(layout.startRect, Ready ? "START GAME" : "Pick 5 per team to start", _button)) StartGame();
            GUI.enabled = true;
        }

        void DrawSlotRow(Layout layout, int row, string label, List<int> team, bool editing)
        {
            var first = layout.slotRects[row][0];
            GUI.Label(new Rect(layout.area.x + 14f, first.y, 105f, first.height),
                $"{(editing ? "▶ " : "   ")}{label} ({team.Count}/{TeamSize})", _header);

            for (int slot = 0; slot < TeamSize; slot++)
            {
                Rect r = layout.slotRects[row][slot];
                if (_zone == Zone.Slots && _slotRow == row && _slotCol == slot) MenuNav.DrawSelection(r);
                if (slot < team.Count)
                {
                    string tag = slot == 0 && row == 0 ? "★ " : "";
                    if (GUI.Button(r, tag + _roster[team[slot]].characterName, _button))
                        team.RemoveAt(slot);
                }
                else
                {
                    GUI.Box(r, "—");
                }
            }
        }

        void DrawControls(Layout layout)
        {
            Rect switchR = layout.slotRects[2][0];
            Rect randomR = layout.slotRects[2][1];
            if (_zone == Zone.Slots && _slotRow == 2 && _slotCol == 0) MenuNav.DrawSelection(switchR);
            if (_zone == Zone.Slots && _slotRow == 2 && _slotCol == 1) MenuNav.DrawSelection(randomR);
            if (GUI.Button(switchR, _editingAway ? "Editing: AWAY  (switch to HOME)" : "Editing: HOME  (switch to AWAY)", _button))
                _editingAway = !_editingAway;
            if (GUI.Button(randomR, "Randomize this team", _button))
                Randomize(_editingAway ? _away : _home);
        }

        /// <summary>One character card. Front: portrait placeholder + scouting
        /// blurb. Back (all cards flip together): the colour-coded stat sheet.
        /// Returns true when clicked.</summary>
        bool DrawCard(Rect r, CharacterStats s, int rosterIndex)
        {
            bool clicked = GUI.Button(r, GUIContent.none);
            GUI.Label(new Rect(r.x, r.y + 4f, r.width, 22f), s.characterName, _cardName);

            if (_showStats) DrawCardStats(r, s);
            else DrawCardFront(r, s);

            // Already-drafted badge.
            if (_home.Contains(rosterIndex)) DrawBadge(r, "HOME", new Color(0.85f, 0.15f, 0.15f));
            else if (_away.Contains(rosterIndex)) DrawBadge(r, "AWAY", new Color(0.15f, 0.35f, 0.9f));
            return clicked;
        }

        void DrawCardFront(Rect r, CharacterStats s)
        {
            // Portrait placeholder — an empty box until real character art lands.
            var face = new Rect(r.x + 12f, r.y + 28f, r.width - 24f, 82f);
            GUI.Box(face, GUIContent.none);
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
                float cy = r.y + 30f + (i % 7) * rowH;
                GUI.Label(new Rect(cx, cy, colW * 0.55f, rowH), stats[i].label, _statLabel);
                var prev = GUI.color;
                GUI.color = StatColor(stats[i].value);
                GUI.Label(new Rect(cx + colW * 0.55f, cy, colW * 0.4f, rowH), stats[i].value.ToString(), _statValue);
                GUI.color = prev;
            }
        }

        /// <summary>10 is gold, 7-8 green, 4-6 plain, 1-3 red.</summary>
        static Color StatColor(int value) =>
            value >= 10 ? Gold :
            value >= 7 ? Green :
            value >= 4 ? Color.white :
            Red;

        void DrawBadge(Rect card, string text, Color color)
        {
            var r = new Rect(card.xMax - 52f, card.y + 4f, 48f, 18f);
            var prev = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = prev;
            GUI.Label(r, text, _cardTag);
        }

        void EnsureStyles()
        {
            _title ??= new GUIStyle(GUI.skin.label)
            { fontSize = 26, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _header ??= new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold };
            _button ??= new GUIStyle(GUI.skin.button) { fontSize = 13 };
            _hint ??= new GUIStyle(GUI.skin.label) { fontSize = 12 };
            _cardName ??= new GUIStyle(GUI.skin.label)
            { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _cardText ??= new GUIStyle(GUI.skin.label) { fontSize = 11, wordWrap = true, alignment = TextAnchor.UpperLeft };
            _cardTag ??= new GUIStyle(GUI.skin.label) { fontSize = 11, alignment = TextAnchor.MiddleCenter };
            _statLabel ??= new GUIStyle(GUI.skin.label) { fontSize = 13 };
            _statValue ??= new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleRight };
        }
    }
}
