using UnityEngine;
using MarioBasketball.Core;

namespace MarioBasketball.Gameplay
{
    /// <summary>
    /// A trigger around the rim that reports when a live shot touches it. The
    /// shot clock rule is "hit the rim within 20 seconds or it's a turnover",
    /// so any contact here satisfies the attempt and resets the shot clock.
    /// Sits just outside the smaller <see cref="ScoreZone"/>, so a made shot
    /// registers both a rim touch and a basket.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Rim : MonoBehaviour
    {
        void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            var ball = other.GetComponent<BallController>();
            if (ball == null || ball.State != BallController.BallState.Shot) return;
            if (GameManager.Instance != null)
                GameManager.Instance.OnRimHit();
        }
    }
}
