using UnityEngine;
using UnityEngine.InputSystem;
using MarioBasketball.Core;

namespace MarioBasketball.UI
{
    /// <summary>
    /// Match shortcuts that don't have full UI yet: call a timeout and make a
    /// substitution for the home team. Controller-first (D-pad up / down), with
    /// keyboard fallbacks (T / Y). Replace with proper menus later.
    /// </summary>
    public class DebugMatchControls : MonoBehaviour
    {
        void Update()
        {
            if (MatchPause.IsPaused || GameManager.Instance == null) return;

            var kb = Keyboard.current;
            var gp = Gamepad.current;

            bool timeout = (kb != null && kb.tKey.wasPressedThisFrame) ||
                           (gp != null && gp.dpad.up.wasPressedThisFrame);
            bool substitute = (kb != null && kb.yKey.wasPressedThisFrame) ||
                              (gp != null && gp.dpad.down.wasPressedThisFrame);

            // D-pad up / T: home timeout (+30 energy to the on-court five).
            if (timeout) GameManager.Instance.CallTimeout(TeamSide.Home);

            // D-pad down / Y: sub home's 3rd on-court player for the 1st bench player.
            if (substitute) GameManager.Instance.Substitute(TeamSide.Home, 2, 0);
        }
    }
}
