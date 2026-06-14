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
        [Tooltip("Dribble bounces per second.")]
        public float dribbleHz = 2.3f;
        [Tooltip("How far to the side the dribble hand is, in metres.")]
        public float dribbleHandSide = 0.42f;
        public float dribbleForward = 0.25f;
        [Tooltip("Extra forward lead per m/s of run speed, so the ball is pushed out ahead on the move.")]
        public float dribbleForwardPerSpeed = 0.035f;
        public float crossoverDuration = 0.32f;
        public float dribbleSpeed = 6f; // legacy; kept for the animator
        [Tooltip("Seconds the releaser can't re-grab after shooting/passing.")]
        public float releaseLockDuration = 0.4f;
        [Tooltip("A missed shot becomes a live (grabbable) rebound once it falls below this height.")]
        public float reboundHeight = 2.2f;
        [Tooltip("How long a thrown pass stays an 'in-flight pass' (Steals to intercept) before becoming a true loose ball (Rebounds).")]
        public float passLiveTime = 1.4f;
        [Tooltip("Horizontal damping (per second) while loose near the floor, so scrambles settle into someone's hands instead of pinballing.")]
        public float looseRollDamping = 1.4f;
        [Tooltip("A loose ball below this height counts as 'on the floor' for the roll damping.")]
        public float looseRollHeight = 0.6f;

        public BallState State { get; private set; } = BallState.Free;
        public PlayerController Holder { get; private set; }
        public int PendingPoints { get; private set; }
        public TeamSide ShooterTeam { get; private set; }
        /// <summary>Who launched the current shot (for streak attribution).</summary>
        public PlayerController Shooter { get; private set; }
        /// <summary>The teammate who assisted the current shot (a valid pass it was
        /// taken directly off of), or null. Read on a make for assist effects.</summary>
        public PlayerController Assister { get; private set; }
        /// <summary>True while the current loose ball is a missed-shot rebound
        /// (vs. a steal/pass scramble) — used for offensive-rebound bonuses.</summary>
        public bool IsRebound { get; private set; }
        /// <summary>True while a thrown pass is in flight (intercept with Steals,
        /// not Rebounds). Reverts to a true loose ball after <see cref="passLiveTime"/>.</summary>
        public bool IsPass { get; private set; }
        /// <summary>The team that threw the in-flight pass.</summary>
        public TeamSide PassingTeam { get; private set; }
        /// <summary>The player who threw the in-flight pass (for assist traits).</summary>
        public PlayerController Passer { get; private set; }
        /// <summary>True while a lob is an alley-oop — a teammate catching it near
        /// the rim finishes immediately.</summary>
        public bool IsAlleyOop { get; private set; }

        Rigidbody _rb;
        Vector3 _centreCourt;
        float _shotTimer;
        bool _shotPending;
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
                if (_crossTimer > 0f) _crossTimer -= Time.deltaTime;
                // Bounce only while actively dribbling (or mid-crossover); held
                // while stationary it's palmed — no auto-bounce.
                if (Holder.IsDribbling || _crossTimer > 0f)
                    transform.position = DribblePosition();
                else
                    transform.position = Vector3.Lerp(transform.position, Holder.CarriedBallPoint, followLerp * Time.deltaTime);
            }
            else if (State == BallState.Shot)
            {
                // A miss becomes a live rebound as soon as it falls back down
                // (a make passes through the score zone, higher up, first).
                _shotTimer -= Time.deltaTime;
                bool comingDown = _rb.linearVelocity.y < 0f && transform.position.y < reboundHeight;
                if (_shotPending && (comingDown || _shotTimer <= 0f))
                {
                    State = BallState.Free;
                    IsRebound = true;
                    _shotPending = false; // it didn't go in → a miss
                    if (GameManager.Instance != null) GameManager.Instance.OnShotMissed(Shooter);
                }
                else if (_shotTimer <= 0f)
                {
                    State = BallState.Free;
                }
            }
            else if (State == BallState.Free)
            {
                if (IsPass)
                {
                    // An uncaught pass eventually becomes a plain loose ball.
                    _passTimer -= Time.deltaTime;
                    if (_passTimer <= 0f) { IsPass = false; IsAlleyOop = false; }
                }
                // A loose ball on the floor sheds speed so the scramble resolves
                // near the players instead of pinballing around the court.
                if (!_rb.isKinematic && !IsPass && transform.position.y < looseRollHeight)
                {
                    float keep = Mathf.Max(0f, 1f - looseRollDamping * Time.deltaTime);
                    Vector3 v = _rb.linearVelocity;
                    _rb.linearVelocity = new Vector3(v.x * keep, v.y, v.z * keep);
                }
            }
        }

        float _passTimer;
        int _handSign = 1;     // which hand the ball is on (+1 right, -1 left)
        float _crossTimer;     // a low crossover sweep in progress
        bool _wasDribbling;
        float _dribbleStart;   // when the current dribble began (phase anchor)

        /// <summary>Which hand the dribble is on (+1 right, -1 left) — the
        /// animator pumps the matching arm.</summary>
        public int DribbleHand => _handSign;
        /// <summary>Where the dribble is in its bounce cycle: 0/1 = hip contact,
        /// 0.5 = floor. Anchored to when the dribble started, so the first bounce
        /// leaves from the hand. Shared with the animator so the hand rides the
        /// ball.</summary>
        public float DribblePhase01 => ((Time.time - _dribbleStart) * dribbleHz) % 1f;

        /// <summary>Sweep the ball low across to the other hand (a crossover).</summary>
        public void Crossover() => _crossTimer = crossoverDuration;

        /// <summary>A real bouncing dribble beside the ball-handler (drops to the
        /// floor and back to hip height), with a low cross-sweep during a move.</summary>
        Vector3 DribblePosition()
        {
            float ballRadius = transform.localScale.x * 0.5f;
            float groundY = Holder.transform.position.y - Holder.BodyHeight * 0.5f;
            float hipY = Holder.transform.position.y;            // ~hip/waist
            Vector3 ground = Holder.transform.position;

            if (_crossTimer > 0f)
            {
                // Stays low and sweeps from one hand to the other.
                float k = 1f - _crossTimer / crossoverDuration;
                float side = Mathf.Lerp(_handSign, -_handSign, k) * dribbleHandSide;
                if (_crossTimer - Time.deltaTime <= 0f) _handSign = -_handSign;
                Vector3 cp = ground + Holder.transform.right * side + Holder.transform.forward * dribbleForward;
                cp.y = groundY + ballRadius + 0.12f;
                return cp;
            }

            // Parabolic bounce: hip at the ends of the cycle, floor in the middle.
            float frac = DribblePhase01;
            float u = 2f * frac - 1f;
            float y = Mathf.Lerp(groundY + ballRadius, hipY, u * u);
            // On the move the ball is pushed out ahead of the body.
            float lead = dribbleForward + dribbleForwardPerSpeed * Holder.PlanarSpeed;
            Vector3 pos = ground + Holder.transform.right * (_handSign * dribbleHandSide) + Holder.transform.forward * lead;
            pos.y = y;
            return pos;
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
            IsRebound = false;
            IsPass = false;
            IsAlleyOop = false;
            _wasDribbling = false; // re-anchor the bounce on the next dribble
            _rb.isKinematic = true;
            _rb.detectCollisions = false;
        }

        /// <summary>
        /// Launch the ball on an arc toward <paramref name="target"/> plus an
        /// <paramref name="aimOffset"/> (small for a made shot, off-rim for a
        /// miss — see <see cref="ShotMath"/>). Fixed flight time so the arc
        /// reads well regardless of range.
        /// </summary>
        public void Shoot(Vector3 target, TeamSide team, int points, float flightTime, Vector3 aimOffset, PlayerController shooter)
        {
            MarkReleased();
            ShooterTeam = team;
            Shooter = shooter;
            Assister = shooter != null ? shooter.AssistingPasser : null; // for assist effects on a make
            PendingPoints = points;
            State = BallState.Shot;
            _shotPending = true;
            _shotTimer = flightTime + 3f;

            GoLive();

            Vector3 start = transform.position;
            Vector3 to = (target + aimOffset) - start;

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
        /// loose ball — a teammate (or a defender) can pick it off.
        /// <paramref name="flightTime"/> shapes it: short = a fast, flat bullet
        /// through the lane; long = a slow lob that arcs over defenders.</summary>
        public void PassTo(Vector3 target, float flightTime = 0.5f, bool alleyOop = false)
        {
            PlayerController passer = Holder;
            TeamSide thrower = Holder != null ? Holder.team : ShooterTeam;
            MarkReleased();
            PendingPoints = 0;
            State = BallState.Free;
            IsPass = true;            // intercept with Steals until it goes stale
            IsAlleyOop = alleyOop;
            PassingTeam = thrower;
            Passer = passer;
            _passTimer = passLiveTime;
            GoLive();

            Vector3 start = transform.position;
            Vector3 to = target - start;
            float t = Mathf.Max(0.15f, flightTime);
            Vector3 velocity;
            velocity.x = to.x / t;
            velocity.z = to.z / t;
            // Gravity solve: a longer flight time naturally arcs higher.
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
            _shotPending = false;
            IsRebound = false;
            IsPass = false;
            IsAlleyOop = false;
            GoLive();
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            transform.position = _centreCourt;
        }

        /// <summary>Called by a <see cref="ScoreZone"/> after a basket is counted.</summary>
        public void OnScored()
        {
            PendingPoints = 0;
            _shotPending = false; // resolved as a make
            IsRebound = false;
            IsPass = false;
            IsAlleyOop = false;
            State = BallState.Free;
        }

        void MarkReleased()
        {
            _recentReleaser = Holder;
            _releaseLockTimer = releaseLockDuration;
            _shotPending = false;
            IsRebound = false;
            IsPass = false;
            IsAlleyOop = false;
            Assister = null;
            Holder = null;
        }

        /// <summary>Force the in-flight shot to drop cleanly through the rim
        /// (Delfan's called shot). Re-arcs the ball from where it is to the
        /// attacking hoop so it scores. No-op unless a shot is in the air.</summary>
        public bool ForceMake()
        {
            if (State != BallState.Shot) return false;
            var gm = GameManager.Instance;
            Hoop hoop = gm != null ? gm.GetAttackingHoop(ShooterTeam) : null;
            if (hoop == null) return false;

            Vector3 target = hoop.AimPoint + ShotMath.AimOffset(true); // dead-centre drop
            Vector3 start = transform.position;
            Vector3 to = target - start;
            const float t = 0.55f;
            Vector3 v;
            v.x = to.x / t;
            v.z = to.z / t;
            v.y = (to.y - 0.5f * Physics.gravity.y * t * t) / t;
            _rb.linearVelocity = v;
            _rb.angularVelocity = Vector3.zero;
            _shotPending = true;       // still a live shot — the score zone will count it
            _shotTimer = t + 3f;
            return true;
        }

        void GoLive()
        {
            _rb.isKinematic = false;
            _rb.detectCollisions = true;
        }
    }
}
