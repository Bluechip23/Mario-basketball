using UnityEngine;
using UnityEngine.InputSystem;

namespace MarioBasketball.Core
{
    /// <summary>
    /// Tiny controller-rumble service for gameplay feedback (a block, a steal, a
    /// rebound, a knockdown). Pulses are deliberately <b>slight</b> and short,
    /// and the whole thing is gated by <see cref="GameSettings.Vibration"/> so a
    /// player can switch it off. One instance lives on the match object; call the
    /// static <see cref="Play"/> from anywhere. No-op when there's no gamepad.
    /// </summary>
    public class Haptics : MonoBehaviour
    {
        public enum Cue { Block, Steal, Rebound, Knockdown }

        static Haptics _instance;
        float _timer;

        void Awake() { _instance = this; }

        void OnDisable()
        {
            StopMotors();
            if (_instance == this) _instance = null;
        }

        /// <summary>Fire a short, slight rumble for the given event (if enabled).</summary>
        public static void Play(Cue cue)
        {
            if (!GameSettings.Vibration || _instance == null) return;

            float low, high, dur;
            switch (cue)
            {
                case Cue.Block:    low = 0.18f; high = 0.40f; dur = 0.16f; break;
                case Cue.Steal:    low = 0.12f; high = 0.28f; dur = 0.11f; break;
                case Cue.Rebound:  low = 0.14f; high = 0.24f; dur = 0.11f; break;
                default:           low = 0.30f; high = 0.50f; dur = 0.20f; break; // Knockdown
            }
            _instance.Pulse(low, high, dur);
        }

        void Pulse(float low, float high, float duration)
        {
            var gp = Gamepad.current;
            if (gp == null) return;
            gp.SetMotorSpeeds(low, high);
            _timer = duration;
        }

        void Update()
        {
            if (_timer <= 0f) return;
            // Unscaled so a pulse still ends even if the game is paused/slowed.
            _timer -= Time.unscaledDeltaTime;
            if (_timer <= 0f) StopMotors();
        }

        static void StopMotors()
        {
            var gp = Gamepad.current;
            if (gp != null) gp.SetMotorSpeeds(0f, 0f);
        }
    }
}
