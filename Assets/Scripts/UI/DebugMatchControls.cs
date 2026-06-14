using UnityEngine;
using UnityEngine.InputSystem;
using MarioBasketball.Core;

namespace MarioBasketball.UI
{
    /// <summary>
    /// Match shortcuts that don't have full UI yet: call a timeout and make a
    /// substitution for the home team. Controller-first (D-pad up / down), with
    /// keyboard fallbacks (T / Y). Subs are only legal during a timeout or a
    /// quarter break (enforced by <see cref="GameManager.CanSubstitute"/>); an
    /// illegal attempt is rejected with a brief on-screen note so it's clear why
    /// nothing happened. Replace with proper menus later.
    /// </summary>
    public class DebugMatchControls : MonoBehaviour
    {
        string _message;
        float _messageTimer;
        GUIStyle _style;

        void Update()
        {
            if (_messageTimer > 0f) _messageTimer -= Time.unscaledDeltaTime;
            if (MatchPause.IsPaused || GameManager.Instance == null) return;

            var kb = Keyboard.current;
            var gp = Gamepad.current;

            bool timeout = (kb != null && kb.tKey.wasPressedThisFrame) ||
                           (gp != null && gp.dpad.up.wasPressedThisFrame);
            bool substitute = (kb != null && kb.yKey.wasPressedThisFrame) ||
                              (gp != null && gp.dpad.down.wasPressedThisFrame);

            // D-pad up / T: home timeout (+30 energy to the on-court five).
            if (timeout)
            {
                bool ok = GameManager.Instance.CallTimeout(TeamSide.Home);
                Flash(ok ? "Home timeout" : "No timeouts left");
            }

            // D-pad down / Y: sub home's 3rd on-court player for the 1st bench
            // player — only during a timeout or between quarters.
            if (substitute)
            {
                if (!GameManager.Instance.CanSubstitute)
                    Flash("Subs only during a timeout or quarter break");
                else
                    Flash(GameManager.Instance.Substitute(TeamSide.Home, 2, 0) ? "Substitution made" : "Substitution failed");
            }
        }

        void Flash(string text)
        {
            _message = text;
            _messageTimer = 2.5f;
        }

        void OnGUI()
        {
            if (_messageTimer <= 0f || string.IsNullOrEmpty(_message)) return;
            _style ??= new GUIStyle(GUI.skin.label)
            { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter,
              normal = { textColor = Color.white } };
            GUI.Label(new Rect((Screen.width - 520f) / 2f, Screen.height - 64f, 520f, 28f), _message, _style);
        }
    }
}
