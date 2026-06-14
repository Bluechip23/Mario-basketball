using UnityEngine;
using MarioBasketball.Core;

namespace MarioBasketball.Gameplay
{
    /// <summary>
    /// A trigger volume sitting just under the rim. When a ball that is in
    /// the <see cref="BallController.BallState.Shot"/> state passes downward
    /// through it, the basket counts for that ball's recorded shooter and
    /// point value.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ScoreZone : MonoBehaviour
    {
        void Reset()
        {
            // Make sure designers who add this in the editor get a trigger.
            GetComponent<Collider>().isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            var ball = other.GetComponent<BallController>();
            if (ball == null || ball.State != BallController.BallState.Shot)
                return;

            // Only count a clean drop-through (ball travelling downward).
            var rb = other.attachedRigidbody;
            if (rb != null && rb.linearVelocity.y > 0.5f)
                return;

            if (GameManager.Instance != null)
                GameManager.Instance.RegisterBasket(ball.ShooterTeam, ball.PendingPoints, ball.Shooter, ball.Assister);

            // Snap the net.
            if (transform.parent != null)
            {
                var net = transform.parent.GetComponentInChildren<MarioBasketball.Presentation.NetSwish>();
                if (net != null) net.Swish();
            }

            ball.OnScored();
        }
    }
}
