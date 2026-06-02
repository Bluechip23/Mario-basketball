using UnityEngine;
using MarioBasketball.Core;
using MarioBasketball.Characters;

namespace MarioBasketball.UI
{
    /// <summary>
    /// A throwaway on-screen scoreboard, energy readout and controls hint drawn
    /// with IMGUI. It needs no scene UI setup, which keeps the core-loop
    /// prototype to a single bootstrap object. Replace with a proper UGUI /
    /// UI Toolkit HUD once the game takes shape.
    /// </summary>
    public class DebugHUD : MonoBehaviour
    {
        GUIStyle _big;
        GUIStyle _small;
        PlayerCharacter _player;

        void OnGUI()
        {
            _big ??= new GUIStyle(GUI.skin.label) { fontSize = 34, fontStyle = FontStyle.Bold };
            _small ??= new GUIStyle(GUI.skin.label) { fontSize = 16 };
            if (_player == null) _player = FindFirstObjectByType<PlayerCharacter>();

            var gm = GameManager.Instance;
            if (gm != null)
            {
                GUI.Label(new Rect(20, 14, 600, 50), $"HOME {gm.HomeScore}   —   AWAY {gm.AwayScore}", _big);
                if (gm.State == GameState.GameOver)
                {
                    string winner = gm.HomeScore >= gm.AwayScore ? "HOME" : "AWAY";
                    GUI.Label(new Rect(20, 56, 600, 40), $"{winner} WINS!", _big);
                }
            }

            if (_player != null)
            {
                string fire = _player.OnFire ? "   *** ON FIRE ***" : "";
                GUI.Label(new Rect(20, 100, 600, 26),
                    $"{_player.stats.characterName}   Energy {_player.Energy:0}{fire}", _small);
                // Simple energy bar.
                GUI.Box(new Rect(20, 124, 220, 14), GUIContent.none);
                GUI.Box(new Rect(20, 124, 220 * Mathf.Clamp01(_player.EnergyFraction), 14), GUIContent.none);
            }

            GUI.Label(new Rect(20, Screen.height - 92, 700, 90),
                "Move: WASD / Left Stick    Sprint: Shift / LT\n" +
                "Shoot: Space / A    Pass: E / X    Jump: Ctrl / Y\n" +
                "Walk over the ball to pick it up, then shoot at the far hoop.",
                _small);
        }
    }
}
