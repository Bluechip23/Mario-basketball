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
        [Tooltip("Seconds a thrown pass is in the air before ANYONE can grab it, so the passer's own defender can't pick it off the instant it leaves the hand.")]
        public float passArmTime = 0.16f;
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
        /// <summary>Where the current in-flight pass was released (so a defender
        /// can't pick it off until it's travelled past them).</summary>
        public Vector3 PassOrigin { get; private set; }
        /// <summary>The teammate a pass is aimed at — they get a big edge winning it
        /// so passes actually complete unless a defender is right on the ball.</summary>
        public PlayerController IntendedReceiver { get; private set; }
        /// <summary>Set once a defender has had their single interception attempt on
        /// the current pass, so a pass can't be re-rolled every frame.</summary>
        public bool PassContested { get; set; }
        /// <summary>True while a lob is an alley-oop — a teammate catching it near
        /// the rim finishes immediately.</summary>
        public bool IsAlleyOop { get; private set; }

        Rigidbody _rb;
        Vector3 _centreCourt;
        float _shotTimer;
        bool _shotPending;
        PlayerController _recentReleaser;
        float _releaseLockTimer;
        float _passArmTimer;

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _centreCourt = transform.position;
        }

        void Update()
        {
            if (_releaseLockTimer > 0f)
                _releaseLockTimer -= Time.deltaTime;
            if (_passArmTimer > 0f)
                _passArmTimer -= Time.deltaTime;

            if (State == BallState.Held && Holder != null)
            {
                if (_moveTimer > 0f)
                {
                    _moveTimer -= Time.deltaTime;
                    if (_moveTimer <= 0f && DribbleMoves.SwitchesHands(_moveType)) _handSign = -_handSign;
                }
                // Bounce while dribbling (or mid-move), and keep the dribble alive
                // in the post — backing down / posting up does not pick the ball
                // up. Held still in triple-threat it's palmed (no auto-bounce).
                if (Holder.IsDribblingBall || _moveTimer > 0f)
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
                    if (_passTimer <= 0f) { IsPass = false; IsAlleyOop = false; IntendedReceiver = null; }
                }
                // A loose ball sheds horizontal speed so the scramble resolves
                // near the players instead of pinballing around the court — hard on
                // the floor, gentler in the air so a high bounce still settles down
                // toward the players rather than rocketing off a wall.
                if (!_rb.isKinematic && !IsPass)
                {
                    float damp = transform.position.y < looseRollHeight ? looseRollDamping : looseRollDamping * 0.4f;
                    float keep = Mathf.Max(0f, 1f - damp * Time.deltaTime);
                    Vector3 v = _rb.linearVelocity;
                    _rb.linearVelocity = new Vector3(v.x * keep, v.y, v.z * keep);
                }
            }
        }

        float _passTimer;
        int _handSign = 1;     // which hand the ball is on (+1 right, -1 left)
        DribbleMoveType _moveType;
        float _moveTimer;      // a dribble move's ball path in progress
        float _moveDuration;
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
        public void Crossover() => DribbleMove(DribbleMoveType.Crossover);

        /// <summary>Run a specific dribble move's ball path. The matching body pose
        /// is driven by <c>ProceduralAnimator</c> from the holder's move state.</summary>
        public void DribbleMove(DribbleMoveType type)
        {
            _moveType = type;
            _moveDuration = DribbleMoves.Duration(type);
            _moveTimer = _moveDuration;
        }

        /// <summary>Where the ball sits this frame: a real bouncing dribble beside
        /// the handler, or the distinct path of a dribble move in progress.</summary>
        Vector3 DribblePosition()
        {
            float ballRadius = transform.localScale.x * 0.5f;
            float groundY = Holder.transform.position.y - Holder.BodyHeight * 0.5f;
            float hipY = Holder.transform.position.y;            // ~hip/waist
            Vector3 ground = Holder.transform.position;
            Vector3 right = Holder.transform.right;
            Vector3 fwd = Holder.transform.forward;
            float lowY = groundY + ballRadius;

            if (_moveTimer > 0f)
            {
                float p = _moveDuration > 0.0001f ? Mathf.Clamp01(1f - _moveTimer / _moveDuration) : 1f;
                float s = _handSign; // starting hand (sign)
                switch (_moveType)
                {
                    case DribbleMoveType.Crossover:
                    {
                        // Wide and hard across to the other hand, kept low.
                        float x = Mathf.Lerp(s, -s, Smooth(p)) * dribbleHandSide * 1.7f;
                        return At(ground, right, fwd, x, dribbleForward, lowY + 0.10f);
                    }
                    case DribbleMoveType.Hesitation:
                    {
                        // Hold the ball out, hitch, then rip it across.
                        float k = p < 0.45f ? 0f : (p - 0.45f) / 0.55f;
                        float x = Mathf.Lerp(s, -s, Smooth(k)) * dribbleHandSide * 1.5f;
                        return At(ground, right, fwd, x, dribbleForward * 1.2f, lowY + 0.12f);
                    }
                    case DribbleMoveType.BehindBack:
                    {
                        // Crosses while wrapping behind the hips.
                        float x = Mathf.Lerp(s, -s, Smooth(p)) * dribbleHandSide * 1.2f;
                        float behind = -Mathf.Sin(Mathf.PI * p) * dribbleHandSide * 1.6f;
                        return At(ground, right, fwd, x, dribbleForward + behind, lowY + 0.20f);
                    }
                    case DribbleMoveType.BetweenLegs:
                    {
                        // Dips to the floor between the feet, crossing to the other hand.
                        float x = Mathf.Lerp(s, -s, Smooth(p)) * dribbleHandSide * 0.7f;
                        float dip = Mathf.Abs(2f * p - 1f); // lowest at the midpoint
                        return At(ground, right, fwd, x, dribbleForward * 0.6f, lowY + dip * 0.22f);
                    }
                    case DribbleMoveType.Spin:
                    {
                        // Cradled close and orbiting as the body whips around.
                        float ang = p * Mathf.PI * 2f;
                        float r = dribbleHandSide * 0.55f;
                        Vector3 sp = ground + right * (Mathf.Cos(ang) * r)
                                   + fwd * (dribbleForward * 0.4f + Mathf.Sin(ang) * r * 0.3f);
                        sp.y = hipY - 0.05f;
                        return sp;
                    }
                    case DribbleMoveType.OffTheHead:
                    {
                        // Tossed up and forward over the defender, then back down.
                        float lift = Mathf.Sin(Mathf.PI * p);
                        float fwdAmt = dribbleForward + lift * 1.2f;
                        Vector3 op = ground + right * (s * dribbleHandSide * 0.2f) + fwd * fwdAmt;
                        op.y = hipY + lift * Holder.BodyHeight * 0.95f;
                        return op;
                    }
                    case DribbleMoveType.StepBack:
                    {
                        // Pushed back behind the body to open up shooting space.
                        float back = -Mathf.Sin(Mathf.PI * p) * 0.55f;
                        return At(ground, right, fwd, s * dribbleHandSide, dribbleForward + back, lowY + 0.12f);
                    }
                }
            }

            // Parabolic bounce: hip at the ends of the cycle, floor in the middle.
            float frac = DribblePhase01;
            float u = 2f * frac - 1f;
            float y = Mathf.Lerp(lowY, hipY, u * u);
            // On the move the ball is pushed out ahead of the body.
            float lead = dribbleForward + dribbleForwardPerSpeed * Holder.PlanarSpeed;
            Vector3 pos = ground + right * (_handSign * dribbleHandSide) + fwd * lead;
            pos.y = y;
            return pos;
        }

        static float Smooth(float t) => Mathf.SmoothStep(0f, 1f, t);

        static Vector3 At(Vector3 ground, Vector3 right, Vector3 fwd, float x, float z, float y)
        {
            Vector3 p = ground + right * x + fwd * z;
            p.y = y;
            return p;
        }

        /// <summary>Whether <paramref name="player"/> may scoop up this loose ball.</summary>
        public bool CanBePickedUpBy(PlayerController player)
        {
            if (State != BallState.Free) return false;
            // A just-thrown pass is briefly untouchable so it actually leaves the
            // passer instead of being scooped by the defender right next to them.
            if (_passArmTimer > 0f) return false;
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
            IntendedReceiver = null;
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
        public void PassTo(Vector3 target, float flightTime = 0.5f, bool alleyOop = false, PlayerController receiver = null)
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
            IntendedReceiver = receiver;
            PassOrigin = transform.position;
            PassContested = false;
            _passTimer = passLiveTime;
            _passArmTimer = passArmTime; // brief window where it can't be grabbed
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

        /// <summary>A shot got swatted away (one-handed block): snap the ball to the
        /// blocker's hand <paramref name="from"/> and fling it off in
        /// <paramref name="dir"/> with a hard, spinning ricochet — a chaotic loose
        /// ball that clearly reads as a block rather than a quiet miss.</summary>
        public void Swat(Vector3 from, Vector3 dir, float power)
        {
            MarkReleased();
            PendingPoints = 0;
            State = BallState.Free;
            IsPass = false;
            IsAlleyOop = false;
            IntendedReceiver = null;
            GoLive();
            transform.position = from;                 // the ball is up at the block point
            Vector3 v = dir.normalized * power;
            v.y = 2.5f;                                 // a little pop so it arcs as it caroms off
            _rb.linearVelocity = v;
            _rb.angularVelocity = new Vector3(0f, 0f, power) + Vector3.up * power; // visible spin off the hand
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
            IntendedReceiver = null;
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
            IntendedReceiver = null;
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
            IntendedReceiver = null;
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
