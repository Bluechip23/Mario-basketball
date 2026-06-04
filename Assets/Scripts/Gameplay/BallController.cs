using UnityEngine;
using MarioBasketball.Core;

namespace MarioBasketball.Gameplay
{
    /// <summary>
    /// The basketball. It is always in one of three states:
    /// <list type="bullet">
    ///   <item><b>Free</b> – loose on the court, driven by physics.</item>
    ///   <item><b>Held</b> – carried by a player, follows their hold point.</item>
    ///   <item><b>Shot</b> – in flight toward a hoop after a shot.</item>
    /// </list>
    /// A <see cref="ScoreZone"/> reads <see cref="PendingPoints"/> and
    /// <see cref="ShooterTeam"/> when the ball drops through, so the ball
    /// carries the "who shot it and for how much" information in flight.
    ///
    /// A short release lockout stops whoever just shot or passed from instantly
    /// scooping the ball back up.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    public class BallController : MonoBehaviour
    {
        public enum BallState { Free, Held, Shot }

        [Header("Dribble feel")]
        [Tooltip("How quickly the ball snaps to the holder's hold point.")]
        public float followLerp = 25f;
        [Tooltip("Height of the dribble bob while held, in metres.")]
        public float dribbleBob = 0.15f;
        public float dribbleSpeed = 6f;
        [Tooltip("Seconds the releaser can't re-grab after shooting/passing.")]
        public float releaseLockDuration = 0.4f;

        public BallState State { get; private set; } = BallState.Free;
        public PlayerController Holder { get; private set; }
        public int PendingPoints { get; private set; }
        public TeamSide ShooterTeam { get; private set; }

        Rigidbody _rb;
        Vector3 _centreCourt;
        float _shotTimer;
        PlayerController _recentReleaser;
        float _releaseLockTimer;

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _centreCourt = transform.position;
        }

        void Update()
        {
            if (_releaseLockTimer > 0f)
                _releaseLockTimer -= Time.deltaTime;

            if (State == BallState.Held && Holder != null)
            {
                Vector3 target = Holder.BallHoldPoint;
                target.y += Mathf.Abs(Mathf.Sin(Time.time * dribbleSpeed)) * dribbleBob;
                transform.position = Vector3.Lerp(transform.position, target, followLerp * Time.deltaTime);
            }
            else if (State == BallState.Shot)
            {
                // After a while a missed shot becomes a loose ball again.
                _shotTimer -= Time.deltaTime;
                if (_shotTimer <= 0f)
                    State = BallState.Free;
            }
        }

        /// <summary>Whether <paramref name="player"/> may scoop up this loose ball.</summary>
        public bool CanBePickedUpBy(PlayerController player)
        {
            if (State != BallState.Free) return false;
            if (player != null && player == _recentReleaser && _releaseLockTimer > 0f) return false;
            return true;
        }

        public void PickUp(PlayerController player)
        {
            Holder = player;
            State = BallState.Held;
            _rb.isKinematic = true;
            _rb.detectCollisions = false;
        }

        /// <summary>
        /// Launch the ball on an arc that lands on <paramref name="target"/>.
        /// Uses a fixed flight time so the arc reads well regardless of range;
        /// <paramref name="spread"/> adds a little horizontal miss.
        /// </summary>
        public void Shoot(Vector3 target, TeamSide team, int points, float flightTime, float spread)
        {
            MarkReleased();
            ShooterTeam = team;
            PendingPoints = points;
            State = BallState.Shot;
            _shotTimer = flightTime + 3f;

            GoLive();

            Vector3 start = transform.position;
            Vector3 offset = new Vector3(Random.Range(-spread, spread), 0f, Random.Range(-spread, spread));
            Vector3 to = (target + offset) - start;

            float t = Mathf.Max(0.1f, flightTime);
            Vector3 velocity;
            velocity.x = to.x / t;
            velocity.z = to.z / t;
            // vY solves: dy = vY*t + 0.5*g*t^2  (g is negative, so this lifts the arc)
            velocity.y = (to.y - 0.5f * Physics.gravity.y * t * t) / t;

            _rb.linearVelocity = velocity;
            _rb.angularVelocity = Vector3.zero;
        }

        /// <summary>A directed pass that leads to <paramref name="target"/> as a
        /// loose ball — a teammate (or a defender) can pick it off.</summary>
        public void PassTo(Vector3 target)
        {
            MarkReleased();
            PendingPoints = 0;
            State = BallState.Free;
            GoLive();

            Vector3 start = transform.position;
            Vector3 to = target - start;
            float t = 0.5f; // quick, flat pass
            Vector3 velocity;
            velocity.x = to.x / t;
            velocity.z = to.z / t;
            velocity.y = (to.y - 0.5f * Physics.gravity.y * t * t) / t;
            _rb.linearVelocity = velocity;
        }

        /// <summary>A gentle pass: released with a forward shove, no scoring intent.</summary>
        public void Pass(Vector3 direction, float power)
        {
            MarkReleased();
            PendingPoints = 0;
            State = BallState.Free;
            GoLive();
            _rb.linearVelocity = direction.normalized * power + Vector3.up * 1.5f;
        }

        public void ResetToCentre()
        {
            Holder = null;
            State = BallState.Free;
            PendingPoints = 0;
            GoLive();
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            transform.position = _centreCourt;
        }

        /// <summary>Called by a <see cref="ScoreZone"/> after a basket is counted.</summary>
        public void OnScored()
        {
            PendingPoints = 0;
            State = BallState.Free;
        }

        void MarkReleased()
        {
            _recentReleaser = Holder;
            _releaseLockTimer = releaseLockDuration;
            Holder = null;
        }

        void GoLive()
        {
            _rb.isKinematic = false;
            _rb.detectCollisions = true;
        }
    }
}
