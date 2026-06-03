using UnityEngine;
using MarioBasketball.Core;
using MarioBasketball.Characters;

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

                GUI.Label(new Rect(20, 86, 700, 24),
                    $"Timeouts  H:{gm.Home.TimeoutsRemaining} A:{gm.Away.TimeoutsRemaining}", _small);

                if (gm.State == GameState.GameOver)
                {
                    string winner = gm.HomeScore == gm.AwayScore ? "TIE" :
                        gm.HomeScore > gm.AwayScore ? "HOME WINS!" : "AWAY WINS!";
                    GUI.Label(new Rect(20, 112, 700, 40), winner, _big);
                }

                PlayerCharacter human = gm.humanPlayer != null ? gm.humanPlayer.Character : null;
                if (human != null)
                {
                    string fire = human.OnFire ? "   *** ON FIRE ***" : "";
                    GUI.Label(new Rect(20, 138, 700, 24),
                        $"{human.stats.characterName}   Energy {human.Energy:0}{fire}", _small);
                    GUI.Box(new Rect(20, 160, 220, 14), GUIContent.none);
                    GUI.Box(new Rect(20, 160, 220 * Mathf.Clamp01(human.EnergyFraction), 14), GUIContent.none);
                }
            }

            GUI.Label(new Rect(20, Screen.height - 142, 760, 142),
                "Move: WASD / Left Stick    Sprint: Shift / LT    Jump: Ctrl / Y\n" +
                "Shoot: Space / A    Pass: E / X    Steal: F / B    Switch: Q / LB\n" +
                "Timeout (home): T    Substitute (home): Y    Pause/Stats: Esc\n" +
                "You control the player under the gold marker. On offense control\n" +
                "follows the ball; on defense press Switch for the nearest man and\n" +
                "Steal when you're on the ball handler.",
                _small);
        }
    }
}
