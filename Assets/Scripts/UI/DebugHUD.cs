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

        void OnGUI()
        {
            _big ??= new GUIStyle(GUI.skin.label) { fontSize = 32, fontStyle = FontStyle.Bold };
            _mid ??= new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
            _small ??= new GUIStyle(GUI.skin.label) { fontSize = 15 };

            var gm = GameManager.Instance;
            if (gm != null)
            {
                GUI.Label(new Rect(20, 12, 700, 44), $"HOME {gm.HomeScore}   —   AWAY {gm.AwayScore}", _big);

                string clock = gm.Clock != null ? gm.Clock.Display : "0:00";
                int quarter = gm.Clock != null ? gm.Clock.Quarter : 1;
                string shot = gm.Shot != null ? gm.Shot.Display : "20";
                GUI.Label(new Rect(20, 56, 700, 28),
                    $"Q{quarter}   {clock}   |   Shot {shot}   |   Ball: {gm.Possession}   |   {gm.State}", _mid);

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

                    if (humanPc.IsAimingPass) DrawPassIcons(gm, humanPc);

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

            GUI.Label(new Rect(20, Screen.height - 176, 860, 176),
                "CONTROLLER  —  Move: L-stick   Turbo: LT   Jump/Contest: Y   Pause: Start\n" +
                "With ball:  Shoot A (jumpers: release at marker; inside: hold to dunk/layup, L1 adjusts, X passes)\n" +
                "            Pass X (aim a teammate with the Right stick to direct it)   Post up (hold) RB   Dive B\n" +
                "Posting (hold RB):  Hook Y   Drop step A   Spin B   Fake LB   Back down RT   Pass X\n" +
                "Defense:  Switch A/LB   Steal X   Push/foul or bump RT   Jump/Block Y\n" +
                "D-pad Up: Timeout   D-pad Down: Sub        (keyboard fallbacks exist too)\n" +
                "You control the gold-ringed player; on offense control follows the ball.",
                _small);
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
