using UnityEngine;
using UnityEngine.InputSystem;
using MarioBasketball.Core;

namespace MarioBasketball.UI
{
    /// <summary>
    /// Temporary keyboard shortcuts to exercise the match systems that don't
    /// have real UI yet: call a timeout and make a substitution for the home
    /// team. Replace with proper menus later.
    /// </summary>
    public class DebugMatchControls : MonoBehaviour
    {
        void Update()
        {
            if (MatchPause.IsPaused) return;
            var kb = Keyboard.current;
            if (kb == null || GameManager.Instance == null) return;

            // T: home timeout (+30 energy to the on-court five).
            if (kb.tKey.wasPressedThisFrame)
                GameManager.Instance.CallTimeout(TeamSide.Home);

            // Y: sub home's 3rd on-court player for the 1st bench player.
            if (kb.yKey.wasPressedThisFrame)
                GameManager.Instance.Substitute(TeamSide.Home, 2, 0);
        }
    }
}
