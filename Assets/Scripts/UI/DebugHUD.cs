using UnityEngine;
using MarioBasketball.Core;
using MarioBasketball.Characters;
using MarioBasketball.Gameplay;

namespace MarioBasketball.UI
{
    /// <summary>
    /// A throwaway IMGUI HUD: scoreboard, quarter and game clock, shot clock,
    /// possession, team fouls/timeouts, and the human player's energy. Needs no
    /// scene UI setup. Replace with a proper UGUI / UI Toolkit HUD later.
    /// </summary>
    public class DebugHUD : MonoBehaviour
    {
        GUIStyle _big;
        GUIStyle _mid;
        GUIStyle _small;
        GUIStyle _score;
        GUIStyle _centerTop;
        GUIStyle _centerShot;
        GUIStyle _head;

        // Team jersey colours (match GameBootstrap HomeColor / AwayColor).
        static readonly Color HomeColor = new Color(0.85f, 0.15f, 0.15f);
        static readonly Color AwayColor = new Color(0.15f, 0.35f, 0.90f);

        Texture2D _white;
        Texture2D White
        {
            get
            {
                if (_white == null) { _white = new Texture2D(1, 1); _white.SetPixel(0, 0, Color.white); _white.Apply(); }
                return _white;
            }
        }

        void OnGUI()
        {
            _big ??= new GUIStyle(GUI.skin.label) { fontSize = 32, fontStyle = FontStyle.Bold };
            _mid ??= new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
            _small ??= new GUIStyle(GUI.skin.label) { fontSize = 15 };
            _score ??= new GUIStyle(GUI.skin.label) { fontSize = 36, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _centerTop ??= new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _centerShot ??= new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _centerShot.normal.textColor = new Color(1f, 0.82f, 0.2f);
            _head ??= new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _head.normal.textColor = Color.white;

            var gm = GameManager.Instance;
            if (gm != null)
            {
                DrawScoreboard(gm);
                GUI.Label(new Rect(20, 56, 480, 24), $"Ball: {gm.Possession}   |   {gm.State}", _small);

                string homePen = gm.Home.InPenalty ? "(PEN)" : "";
                string awayPen = gm.Away.InPenalty ? "(PEN)" : "";
                GUI.Label(new Rect(20, 86, 760, 24),
                    $"Fouls  H:{gm.Home.Fouls}{homePen} A:{gm.Away.Fouls}{awayPen}     " +
                    $"Timeouts  H:{gm.Home.TimeoutsRemaining} A:{gm.Away.TimeoutsRemaining}", _small);

                if (gm.IsFreeThrow && gm.FreeThrowShooter != null && gm.FreeThrowShooter.Character != null)
                    GUI.Label(new Rect(20, 110, 760, 26),
                        $"FREE THROW — {gm.FreeThrowShooter.Character.stats.characterName}  ({gm.FreeThrowsRemaining} left)", _mid);

                string onFire = OnFireNames(gm);
                if (!string.IsNullOrEmpty(onFire))
                    GUI.Label(new Rect(Screen.width - 340, 14, 320, 26), $"ON FIRE: {onFire}", _mid);

                if (gm.State == GameState.GameOver)
                {
                    string winner = gm.HomeScore == gm.AwayScore ? "TIE" :
                        gm.HomeScore > gm.AwayScore ? "HOME WINS!" : "AWAY WINS!";
                    GUI.Label(new Rect(20, 112, 700, 40), winner, _big);
                }

                var humanPc = gm.humanPlayer;
                PlayerCharacter human = humanPc != null ? humanPc.Character : null;
                if (human != null)
                {
                    string fire = human.OnFire ? "   *** ON FIRE ***" : "";
                    string posting = humanPc.IsPosting ? "   [POSTING]" : "";
                    GUI.Label(new Rect(20, 138, 700, 24),
                        $"{human.stats.characterName}   Energy {human.Energy:0}{fire}{posting}", _small);
                    GUI.Box(new Rect(20, 160, 220, 14), GUIContent.none);
                    GUI.Box(new Rect(20, 160, 220 * Mathf.Clamp01(human.EnergyFraction), 14), GUIContent.none);

                    if (humanPc.IconPassActive) DrawIconButtons(gm, humanPc);
                    else if (humanPc.IsAimingPass) DrawPassIcons(gm, humanPc);

                    if (humanPc.IsFinishing)
                        GUI.Label(new Rect((Screen.width - 320) / 2f, Screen.height - 232f, 320f, 22f),
                            "In the air — L1 adjust  ·  X pass", _mid);

                    if (humanPc.IsShooting)
                    {
                        // Shot meter: fill rises; hit the marker for a perfect release.
                        const float mw = 240f, mh = 16f;
                        float mx = (Screen.width - mw) / 2f, my = Screen.height - 210f;
                        GUI.Box(new Rect(mx, my, mw, mh), GUIContent.none);
                        GUI.Box(new Rect(mx, my, mw * humanPc.ShotChargeFraction, mh), GUIContent.none);
                        float markX = mx + mw * humanPc.ShotPerfectFraction;
                        GUI.Box(new Rect(markX - 2f, my - 4f, 4f, mh + 8f), GUIContent.none);
                        GUI.Label(new Rect(mx, my - 22f, mw, 20f), "Release at the marker!", _small);
                    }

                    if (humanPc.IsPosting && humanPc.Post != null)
                    {
                        float lev = Mathf.Clamp(humanPc.Post.Leverage, -humanPc.Post.maxLeverage, humanPc.Post.maxLeverage);
                        float frac = Mathf.InverseLerp(-humanPc.Post.maxLeverage, humanPc.Post.maxLeverage, lev);
                        GUI.Label(new Rect(20, 178, 300, 20), "Back-down (tap B):", _small);
                        GUI.Box(new Rect(150, 180, 160, 14), GUIContent.none);
                        GUI.Box(new Rect(150, 180, 160 * frac, 14), GUIContent.none);
                    }
                }
            }
        }

        // Centred scoreboard: [head][turbo] HOME · Q/time + shot clock · AWAY [turbo][head].
        void DrawScoreboard(GameManager gm)
        {
            const float headSize = 50f, turboW = 120f, turboH = 14f, scoreW = 64f, centerW = 150f, gap = 8f;
            float panelW = headSize * 2f + turboW * 2f + scoreW * 2f + centerW + gap * 6f;
            float x = (Screen.width - panelW) / 2f;
            float y = 10f;
            const float rowH = 64f;

            Fill(new Rect(x - 14f, y - 6f, panelW + 28f, rowH + 12f), new Color(0f, 0f, 0f, 0.55f));

            PlayerController home = Featured(gm, TeamSide.Home);
            PlayerController away = Featured(gm, TeamSide.Away);

            string clock = gm.Clock != null ? gm.Clock.Display : "0:00";
            int quarter = gm.Clock != null ? gm.Clock.Quarter : 1;
            string shot = gm.Shot != null ? gm.Shot.Display : "20";

            float cx = x;
            DrawHead(new Rect(cx, y + (rowH - headSize) / 2f, headSize, headSize), home, HomeColor); cx += headSize + gap;
            DrawTurbo(new Rect(cx, y + (rowH - turboH) / 2f, turboW, turboH), home, fillTowardLeft: true); cx += turboW + gap;
            GUI.Label(new Rect(cx, y, scoreW, rowH), gm.HomeScore.ToString(), _score); cx += scoreW + gap;

            GUI.Label(new Rect(cx, y + 4f, centerW, 30f), $"Q{quarter}    {clock}", _centerTop);
            GUI.Label(new Rect(cx, y + 33f, centerW, 28f), shot, _centerShot); cx += centerW + gap;

            GUI.Label(new Rect(cx, y, scoreW, rowH), gm.AwayScore.ToString(), _score); cx += scoreW + gap;
            DrawTurbo(new Rect(cx, y + (rowH - turboH) / 2f, turboW, turboH), away, fillTowardLeft: false); cx += turboW + gap;
            DrawHead(new Rect(cx, y + (rowH - headSize) / 2f, headSize, headSize), away, AwayColor);
        }

        /// <summary>Whose turbo to show for a team: the ball handler if they have it,
        /// else the controlled human, else the teammate nearest the ball.</summary>
        static PlayerController Featured(GameManager gm, TeamSide side)
        {
            var holder = gm.ball != null ? gm.ball.Holder : null;
            if (holder != null && holder.team == side) return holder;
            if (gm.humanPlayer != null && gm.humanPlayer.team == side && gm.humanPlayer.enabled) return gm.humanPlayer;

            Vector3 ballPos = gm.ball != null ? gm.ball.transform.position : Vector3.zero;
            PlayerController best = null; float bestD = Mathf.Infinity;
            foreach (var p in gm.TeamFor(side).onCourt)
            {
                if (p == null || !p.enabled) continue;
                float d = Vector3.Distance(p.transform.position, ballPos);
                if (d < bestD) { bestD = d; best = p; }
            }
            return best;
        }

        void DrawHead(Rect r, PlayerController p, Color teamColor)
        {
            Fill(r, teamColor);
            DrawBorder(r, Color.white, 2f);
            if (p != null && p.Character != null)
                GUI.Label(r, Initials(p.Character.stats.characterName), _head);
        }

        void DrawTurbo(Rect r, PlayerController p, bool fillTowardLeft)
        {
            Fill(r, new Color(0.08f, 0.08f, 0.10f, 0.9f)); // track
            float t = p != null ? Mathf.Clamp01(p.Turbo01) : 0f;
            // Amber when full, fading to red as it empties.
            Color c = Color.Lerp(new Color(0.9f, 0.25f, 0.1f), new Color(1f, 0.85f, 0.15f), t);
            float w = r.width * t;
            // Bars grow toward each team's head (on the outside): home fills leftward,
            // away rightward.
            Rect fill = fillTowardLeft ? new Rect(r.xMax - w, r.y, w, r.height) : new Rect(r.x, r.y, w, r.height);
            Fill(fill, c);
            DrawBorder(r, new Color(1f, 1f, 1f, 0.6f), 1f);
        }

        void Fill(Rect r, Color c)
        {
            Color old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, White);
            GUI.color = old;
        }

        void DrawBorder(Rect r, Color c, float t)
        {
            Fill(new Rect(r.x, r.y, r.width, t), c);
            Fill(new Rect(r.x, r.yMax - t, r.width, t), c);
            Fill(new Rect(r.x, r.y, t, r.height), c);
            Fill(new Rect(r.xMax - t, r.y, t, r.height), c);
        }

        static string Initials(string name)
        {
            if (string.IsNullOrEmpty(name)) return "?";
            var parts = name.Split(' ');
            if (parts.Length >= 2 && parts[0].Length > 0 && parts[1].Length > 0)
                return ("" + parts[0][0] + parts[1][0]).ToUpper();
            return name.Substring(0, Mathf.Min(2, name.Length)).ToUpper();
        }

        // LB held: label the on-court teammates with the face button that passes to them.
        void DrawIconButtons(GameManager gm, PlayerController human)
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            string[] labels = { "A", "B" };
            int i = 0;
            foreach (var mate in gm.TeamFor(human.team).onCourt)
            {
                if (mate == null || mate == human || !mate.enabled) continue;
                if (i >= labels.Length) break;
                Vector3 sp = cam.WorldToScreenPoint(mate.transform.position + Vector3.up * (mate.BodyHeight + 0.4f));
                if (sp.z > 0f)
                    GUI.Label(new Rect(sp.x - 30f, Screen.height - sp.y - 14f, 60f, 24f), $"[{labels[i]}]", _mid);
                i++;
            }
        }

        void DrawPassIcons(GameManager gm, PlayerController human)
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            var target = human.PassTarget;
            foreach (var mate in gm.TeamFor(human.team).onCourt)
            {
                if (mate == null || mate == human || !mate.enabled) continue;
                Vector3 sp = cam.WorldToScreenPoint(mate.transform.position + Vector3.up * (mate.BodyHeight + 0.4f));
                if (sp.z <= 0f) continue; // behind the camera
                var r = new Rect(sp.x - 40f, Screen.height - sp.y - 14f, 80f, 24f);
                bool on = mate == target;
                GUI.Label(r, on ? "▶ PASS ◀" : "○", on ? _mid : _small);
            }
        }

        static string OnFireNames(GameManager gm)
        {
            string result = "";
            AppendOnFire(gm.Home, ref result);
            AppendOnFire(gm.Away, ref result);
            return result;
        }

        static void AppendOnFire(TeamState team, ref string result)
        {
            foreach (var p in team.onCourt)
            {
                if (p == null || p.Character == null || !p.Character.OnFire) continue;
                if (result.Length > 0) result += ", ";
                result += p.Character.stats.characterName;
            }
        }
    }
}
