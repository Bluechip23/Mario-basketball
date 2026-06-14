using UnityEngine;
using MarioBasketball.Core;
using MarioBasketball.Characters;
using MarioBasketball.InputControl;

namespace MarioBasketball.Gameplay
{
    /// <summary>
    /// A player's body and actions. Movement and actions are driven by an
    /// <i>intent</i> (a move vector plus action triggers) fed by either the
    /// human (<see cref="InputReader"/>) or a <c>PlayerAI</c> brain. Built on a
    /// <see cref="CharacterController"/> for crisp, arcade movement.
    ///
    /// Outcomes are stat-driven: speed (Speed), shot accuracy (3-Point / Mid
    /// Range / Inside Scoring), contests (Perimeter/Post Defense, Blocks),
    /// steals (Steals vs Ball Handling) and the post game (see
    /// <see cref="PostUpController"/>, Power + Post Offense/Defense).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PostUpController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Identity")]
        public TeamSide team = TeamSide.Home;
        [Tooltip("The human-controlled player reads input; others are AI-driven.")]
        public bool isHuman = false;

        [Header("Movement (mapped from the Speed stat)")]
        public float minMoveSpeed = 4f;
        public float maxMoveSpeed = 9f;
        public float sprintMultiplier = 1.4f;
        public float turnSpeed = 720f;
        public float gravity = -25f;
        public float jumpHeight = 1.4f;

        [Header("Ball handling")]
        [Tooltip("Distance from the basket beyond which a make is worth 3.")]
        public float threePointDistance = 6.75f;
        [Tooltip("Within this radius a shot uses Inside Scoring, not Mid Range.")]
        public float paintRadius = 2.5f;

        [Header("Shooting")]
        [Tooltip("Make odds, distance falloff and contest live in ShotMath.")]
        public float shotFlightTime = 1.1f;
        public float passPower = 9f;
        [Tooltip("Hold Pass at least this long for a hard pass; shorter is a loft.")]
        public float passHoldThreshold = 0.25f;
        [Tooltip("Flight time of a tapped loft pass (slow, arcs over defenders).")]
        public float loftPassTime = 0.85f;
        [Tooltip("Flight time of a held hard pass (fast, flat, stealable).")]
        public float hardPassTime = 0.28f;
        [Tooltip("Right-stick magnitude needed to aim a directed pass.")]
        public float passAimDeadzone = 0.5f;
        [Tooltip("Lead-pass spread (m) at Ball Handling 1 / 10 — low handles miss.")]
        public float passErrorMax = 1.3f;
        public float passErrorMin = 0.05f;

        [Header("Shot timing (jump shots only; layups/dunks are instant)")]
        [Tooltip("Release within this many seconds of the jump's apex for a perfect shot.")]
        public float perfectReleaseWindow = 0.07f;
        [Tooltip("Make% multiplier lost per second of mistiming beyond the window.")]
        public float timingFalloffPerSec = 2f;
        [Range(0f, 1f)] public float minTimingMultiplier = 0.35f;
        [Tooltip("Auto-release this long after the apex if the button is still held.")]
        public float shotAutoReleaseAfterApex = 0.45f;
        [Tooltip("Catch-and-shoot window for the quick-catch shooter trait.")]
        public float quickCatchWindow = 0.3f;
        [Tooltip("Window to shoot off a Playmaker's pass for the +2 assist bonus.")]
        public float assistWindow = 1.0f;
        [Tooltip("Acrobat trait (Baby Mario): fraction of the shot-mistiming penalty he ignores — 0.8 = suffers 80% less from early/late releases.")]
        [Range(0f, 1f)] public float acrobatTimingRelief = 0.8f;

        [Header("Hidden traits")]
        [Tooltip("Killer Instinct (Daisy): bonus to Mid/3PT/Inside/Perimeter-D at full opponent fatigue (Mid can reach 11, the rest cap at 10).")]
        public float killerMaxBonus = 4f;
        [Tooltip("Killer Instinct: opponent fatigue below this fraction gives no bonus (fresh legs).")]
        [Range(0f, 1f)] public float killerFatigueFloor = 0.1f;
        [Tooltip("Called Shot (Delfan): guaranteed makes allowed per game.")]
        public int calledShotMax = 2;
        [Tooltip("Called Shot: only shots launched within this distance (m) of the hoop — i.e. within half court — qualify.")]
        public float calledShotRange = 14f;
        [Tooltip("Min planar speed (m/s) to count as actively dribbling.")]
        public float dribbleMoveThreshold = 0.6f;

        [Header("Fadeaway / lean (jump shots)")]
        [Tooltip("Hold the move stick during a jump shot to fade that way; release nothing and it's a straight-up shot. Drift speed (m/s) at a full-held stick.")]
        public float fadeSpeed = 3.2f;
        [Tooltip("Fraction of the defender's block chance removed at a full fade (the separation a fadeaway buys).")]
        [Range(0f, 1f)] public float fadeBlockReduction = 0.6f;
        [Tooltip("Fraction of the defender's contest make-penalty removed at a full fade.")]
        [Range(0f, 1f)] public float fadeContestReduction = 0.7f;
        [Tooltip("Fade multiplier when leaning fully AGAINST your run direction at top speed (leaning WITH your momentum stays full). Lower = momentum matters more; 1 disables the asymmetry.")]
        [Range(0f, 1f)] public float fadeAgainstMomentumMin = 0.25f;

        [Header("Inside finishing (dunk / layup)")]
        [Tooltip("Time in the air before an unheld finish auto-resolves.")]
        public float finishAirTime = 0.45f;
        public float finishApproachSpeed = 4f;
        public float finishFlightTime = 0.5f;
        [Tooltip("Effective Dunk at/above this goes up for a dunk; below, a layup.")]
        public float dunkThreshold = 6f;
        [Tooltip("Dunk block resistance per point of Power.")]
        public float dunkPowerBlockResist = 0.3f;
        [Tooltip("Block chance multiplier when the shot is air-adjusted.")]
        [Range(0f, 1f)] public float adjustBlockReduction = 0.4f;
        [Tooltip("Max make% lost to an air-adjust (mitigated by Inside Scoring).")]
        [Range(0f, 1f)] public float maxAdjustPenalty = 0.35f;

        [Header("Alley-oop")]
        [Tooltip("A loft to a teammate within this of the rim becomes an alley-oop.")]
        public float oopRange = 3.0f;
        public float oopFlightTime = 1.0f;
        [Tooltip("Make% bonus on an alley-oop finish (it's a high-percentage play).")]
        [Range(0f, 1f)] public float alleyOopBonus = 0.2f;

        [Header("Dribble move (Ball Handling vs Perimeter Defense)")]
        public float dribbleRange = 2.0f;
        public float dribbleCooldownTime = 0.8f;
        public float dribbleBoostTime = 0.7f;
        public float dribbleBoostMult = 1.5f;
        [Tooltip("How long the beaten defender is frozen on a successful move.")]
        public float ankleStun = 0.9f;
        public float dribbleBaseChance = 0.45f;
        public float dribbleStatScale = 0.06f;

        [Header("Dribble flicks (right-stick hard dribbles for separation)")]
        public float flickCooldownTime = 0.45f;
        [Tooltip("Burst impulse on a flick toward / across the defender.")]
        public float flickBurstPower = 6f;
        [Tooltip("Backward impulse on a step-back (flick away from the basket).")]
        public float stepBackPower = 7.5f;
        [Tooltip("Opposite lateral flicks within this window chain into a hesitation cross.")]
        public float hesitationWindow = 0.6f;
        [Tooltip("How long the defender freezes when a flick move shakes them (the full ankleStun is reserved for the hesitation cross).")]
        public float flickFreeze = 0.4f;

        [Header("Block (defense on a shot; contest % lives in ShotMath)")]
        public float contestRange = 3f;
        public float blockRange = 1.1f;
        public float blockBaseChance = 0.04f;
        public float blockStatScale = 0.05f;
        public float blockMaxChance = 0.5f;
        public float blockKnockPower = 4f;

        [Header("Steal (Steals vs Ball Handling)")]
        public float stealReach = 0.95f;
        public float stealCooldown = 1.0f;
        public float stealWhiffCooldown = 0.4f;
        public float stealBaseChance = 0.04f;
        public float stealStatScale = 0.035f;
        public float stealMinChance = 0.02f;
        public float stealMaxChance = 0.4f;

        [Header("Dive / shove")]
        public float diveDuration = 0.5f;
        public float diveSpeed = 9f;
        public float diveBallSeekRange = 6f;
        public float shoveDuration = 0.35f;

        [Header("Push / foul (Power)")]
        public float pushRange = 1.7f;
        public float pushCooldown = 0.8f;
        public float pushWhiffCooldown = 0.3f;
        public float pushForce = 7f;
        [Tooltip("Power advantage at/above which the push knocks the target down.")]
        public float pushKnockdownPowerGap = 4f;
        public float pushKnockLooseBase = 0.2f;
        public float pushKnockLooseScale = 0.06f;

        [Header("Animation gestures (cosmetic timers read by the animator)")]
        [Tooltip("How long the pass/throw arm pose holds after the ball leaves.")]
        public float passGestureTime = 0.28f;
        [Tooltip("How long the cross-body sweep pose holds after a dribble move/flick.")]
        public float dribbleMoveGestureTime = 0.3f;

        /// <summary>Where the carried ball sits — out in front, hip height,
        /// scaled to the character's body size.</summary>
        public Vector3 BallHoldPoint
        {
            get
            {
                float h = _cc != null ? _cc.height : 1.9f;
                return transform.position + transform.forward * (0.29f * h) + Vector3.up * (0.21f * h);
            }
        }

        /// <summary>Where a gathered (non-dribbled) ball is carried. During a
        /// jump shot it rises with the meter from the chest gather to an
        /// overhead set point (so the shot releases above the head, matching the
        /// arm pose); held high in both hands for a dunk/layup; at the hip
        /// otherwise.</summary>
        public Vector3 CarriedBallPoint
        {
            get
            {
                float h = BodyHeight;
                if (IsShooting)
                {
                    float k = Mathf.Clamp01(ShotChargeFraction / Mathf.Max(0.01f, ShotPerfectFraction));
                    Vector3 gather = transform.position + transform.forward * (0.24f * h) + Vector3.up * (0.10f * h);
                    Vector3 set = transform.position + transform.forward * (0.10f * h) + Vector3.up * (0.62f * h);
                    return Vector3.Lerp(gather, set, k);
                }
                if (IsFinishing)
                    return transform.position + transform.forward * (0.18f * h) + Vector3.up * (0.55f * h);
                if (IsPostShooting)
                {
                    // The shot rises overhead as the release meter fills (the
                    // poster's back is to the rim, so keep it straight up).
                    float k = Mathf.Clamp01(PostShotChargeFraction / Mathf.Max(0.01f, PostShotPerfectFraction));
                    Vector3 gather = transform.position + Vector3.up * (0.30f * h);
                    Vector3 set = transform.position + Vector3.up * (0.66f * h);
                    return Vector3.Lerp(gather, set, k);
                }
                return BallHoldPoint;
            }
        }

        public PlayerCharacter Character => _character;
        public PostUpController Post => _post;
        public bool HasBall => Ball != null && Ball.Holder == this;
        public bool IsPosting => _post != null && _post.IsPosting;
        public bool IsStunned => _stunTimer > 0f;
        /// <summary>Knocked down (ankle-broken / leveled) — sprawls on the floor.</summary>
        public bool IsFallen => _fallTimer > 0f;
        /// <summary>Dribbling: you've put the ball on the floor and are live with
        /// it. A fresh catch does NOT auto-dribble — you stay in triple-threat
        /// until you actually move with it. Once dribbling, simply stopping does
        /// not end it (you keep your dribble standing still); it ends when you
        /// shoot, finish, post up, leave your feet, get stunned, or lose the
        /// ball. Latched in <see cref="UpdateDribbleState"/>.</summary>
        public bool IsDribbling => _dribbling;
        /// <summary>Whether the ball should be bouncing as a live dribble. You keep
        /// your dribble in the post — backing down or posting up does NOT pick the
        /// ball up; it's only gathered when the post shot actually goes up.</summary>
        public bool IsDribblingBall => HasBall && (_dribbling || (IsPosting && !IsPostShooting));
        /// <summary>Briefly true right after a pass/throw (drives the throw pose).</summary>
        public bool IsPassing => _passGestureTimer > 0f;
        /// <summary>Briefly true after a dribble move / flick (drives the cross sweep).</summary>
        public bool IsDribbleMoveGesture => _dribbleMoveGestureTimer > 0f;
        /// <summary>Contorting a finish in the air (L1 air-adjust) — alters the layup.</summary>
        public bool IsAdjustingFinish => _finishing && _finishAdjusted;
        /// <summary>Airborne for a dunk/layup (can air-adjust or pass).</summary>
        public bool IsFinishing => _finishing;
        public bool FinishIsDunk => _finishIsDunk;
        /// <summary>The human is aiming a directed pass (right stick pushed).</summary>
        public bool IsAimingPass => _passAim.magnitude >= passAimDeadzone && HasBall;
        /// <summary>The teammate currently targeted by the pass aim (for icons).</summary>
        public PlayerController PassTarget => IsAimingPass ? TargetedTeammate(_passAim) : null;
        /// <summary>Holding the icon-pass modifier (LB) with the ball — show
        /// teammate icons and pass to one via a face button.</summary>
        public bool IconPassActive => _iconHeld && HasBall && !IsPosting && !IsFinishing;
        /// <summary>Physical body height (m), drives rebound reach.</summary>
        public float BodyHeight => _cc != null ? _cc.height : 1.8f;
        public bool IsAirborne => _cc != null && !_cc.isGrounded;
        public bool IsDiving => _diveTimer > 0f;
        /// <summary>Current horizontal speed (m/s) — drives the run animation.</summary>
        public float PlanarSpeed { get; private set; }
        public bool IsShooting => _shooting;
        /// <summary>A post move's shot is mid-release (its timing meter is up).</summary>
        public bool IsPostShooting => _post != null && _post.PostShotActive;
        /// <summary>Post-shot meter fill (0-1), for the release-timing pose/feedback.</summary>
        public float PostShotChargeFraction => _post != null ? _post.PostShotChargeFraction : 0f;
        /// <summary>Where the perfect post-shot release sits on the meter (0-1).</summary>
        public float PostShotPerfectFraction => _post != null ? _post.PostShotPerfectFraction : 0f;
        /// <summary>World-space planar direction the current jump shot is fading
        /// toward (zero for a straight-up shot). Drives the body lean.</summary>
        public Vector3 FadeDirection => _fadeDir;
        /// <summary>How hard the shot is fading, 0 (straight up) to 1 (full lean).</summary>
        public float FadeAmount => _fadeAmount;
        /// <summary>How full the shot meter is (0-1) for the jump in progress.</summary>
        public float ShotChargeFraction => _shooting ? Mathf.Clamp01(_shotCharge / ShotMeterDuration) : 0f;
        /// <summary>Where the perfect-release marker sits on the meter (0-1).</summary>
        public float ShotPerfectFraction => Mathf.Clamp01(_apexTime / ShotMeterDuration);
        float ShotMeterDuration => Mathf.Max(0.01f, _apexTime + shotAutoReleaseAfterApex);

        CharacterController _cc;
        PlayerCharacter _character;
        PostUpController _post;
        InputReader _input;
        Camera _cam;
        bool _dribbling;
        float _verticalVelocity;
        Vector2 _moveIntent;
        bool _sprintIntent;
        float _stealCooldown;
        float _stunTimer;
        float _diveTimer;
        Vector3 _diveDir;
        Vector3 _shoveVel;
        float _shoveTimer;
        float _pushCooldown;
        bool _shooting;
        float _shotCharge;
        float _apexTime;
        Vector3 _fadeDir;
        float _fadeAmount;
        Vector3 _lastRunVelocity;
        Vector3 _launchVel;
        bool _pendingQuickCatch;
        bool _hadBall;
        float _catchTime = -10f;
        bool _finishing;
        float _finishTimer;
        bool _finishIsDunk;
        bool _finishAdjusted;
        Vector2 _passAim;
        bool _iconHeld;
        float _dribbleCooldown;
        float _dribbleBoostTimer;
        float _flickCooldown;
        float _lastLateralFlickTime = -10f;
        float _lastLateralFlickSign;
        bool _passCharging;
        float _passChargeTime;
        float _fallTimer;
        PlayerController _assistPasser;
        float _assistTime;
        bool _assistDribbled;
        float _lastShotDistance;
        int _calledShotsUsed;
        float _passGestureTimer;
        float _dribbleMoveGestureTimer;

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _character = GetComponent<PlayerCharacter>();
            _post = GetComponent<PostUpController>();
            // Time from launch to the top of the jump = the ideal release point.
            _apexTime = Mathf.Sqrt(-2f * gravity * jumpHeight) / -gravity;
        }

        void OnEnable()
        {
            if (isHuman) EnableInput();
        }

        void OnDisable()
        {
            DisableInput();
        }

        public void SetHumanControlled(bool value)
        {
            isHuman = value;
            if (value)
            {
                EnableInput();
            }
            else
            {
                DisableInput();
                _moveIntent = Vector2.zero;
                _sprintIntent = false;
                _shooting = false;
                _finishing = false;
                if (IsPosting) _post.End();
            }
        }

        void EnableInput()
        {
            if (_input != null) return;
            _input = new InputReader();
            _input.ShootPressed += OnShootPressed;
            _input.ShootReleased += OnShootReleased;
            _input.PassPressed += OnPassPressed;
            _input.PassReleased += OnPassReleased;
            _input.JumpPressed += TriggerJump;
            _input.StealPressed += TriggerSteal;
            _input.DivePressed += TriggerDive;
            _input.BackDownPressed += TriggerBackDown;
            _input.HookPressed += TriggerHook;
            _input.DropStepPressed += TriggerDropStep;
            _input.SpinPressed += TriggerSpin;
            _input.FakePressed += TriggerFake;
            _input.DribbleFlick += OnDribbleFlick;
            _input.TurboDoubleTap += OnTurboDoubleTap;
            _input.Enable();
        }

        void DisableInput()
        {
            if (_input == null) return;
            _input.ShootPressed -= OnShootPressed;
            _input.ShootReleased -= OnShootReleased;
            _input.PassPressed -= OnPassPressed;
            _input.PassReleased -= OnPassReleased;
            _input.JumpPressed -= TriggerJump;
            _input.StealPressed -= TriggerSteal;
            _input.DivePressed -= TriggerDive;
            _input.BackDownPressed -= TriggerBackDown;
            _input.HookPressed -= TriggerHook;
            _input.DropStepPressed -= TriggerDropStep;
            _input.SpinPressed -= TriggerSpin;
            _input.FakePressed -= TriggerFake;
            _input.DribbleFlick -= OnDribbleFlick;
            _input.TurboDoubleTap -= OnTurboDoubleTap;
            _input.Disable();
            _input = null;
        }

        /// <summary>Set this frame's desired movement (AI; the human overrides
        /// it from input each frame).</summary>
        public void SetMoveIntent(Vector2 move, bool sprint)
        {
            _moveIntent = move;
            _sprintIntent = sprint;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (_stealCooldown > 0f) _stealCooldown -= dt;
            if (_stunTimer > 0f) _stunTimer -= dt;
            if (_fallTimer > 0f) _fallTimer -= dt;
            if (_diveTimer > 0f) _diveTimer -= dt;
            if (_shoveTimer > 0f) _shoveTimer -= dt;
            if (_pushCooldown > 0f) _pushCooldown -= dt;
            if (_dribbleCooldown > 0f) _dribbleCooldown -= dt;
            if (_dribbleBoostTimer > 0f) _dribbleBoostTimer -= dt;
            if (_flickCooldown > 0f) _flickCooldown -= dt;
            if (_passGestureTimer > 0f) _passGestureTimer -= dt;
            if (_dribbleMoveGestureTimer > 0f) _dribbleMoveGestureTimer -= dt;
            if (_passCharging)
            {
                _passChargeTime += dt;
                if (IsStunned || !HasBall) _passCharging = false; // lost it mid-windup
            }

            if (isHuman && _input != null)
            {
                _input.Tick();
                // The stick is read relative to the camera, so "up" on the stick
                // is "up the screen" regardless of where the sideline camera sits.
                _moveIntent = CameraRelative(_input.Move);
                _passAim = _input.PassAim;
                _sprintIntent = _input.SprintHeld;
                _iconHeld = _input.IconHeld;
                HandlePostHold();
            }

            UpdateKillerInstinct();
            AdvanceShotMeter(dt);
            AdvanceFinish(dt);
            Move();
            UpdateDribbleState();
            // Loose balls / rebounds are resolved centrally (GameManager) so it's
            // a Rebounds + height + jump contest, not a first-come grab.

            // Track when this player gains the ball (for catch-and-shoot timing).
            bool has = HasBall;
            if (has && !_hadBall) _catchTime = Time.time;
            _hadBall = has;

            // Assist window: voided if it lapses, the ball is gone, or the
            // receiver puts it on the floor (starts dribbling).
            if (_assistPasser != null)
            {
                if (Time.time - _assistTime > assistWindow || !has) _assistPasser = null;
                else if (IsDribbling) _assistDribbled = true;
            }
        }

        /// <summary>Called when this player catches a pass — records the passer
        /// for the Playmaker assist bonus.</summary>
        public void OnCaughtPass(PlayerController passer)
        {
            _assistPasser = passer;
            _assistTime = Time.time;
            _assistDribbled = false;
        }

        /// <summary>The teammate whose pass this shot is going up directly off of
        /// (within the assist window, no dribble), or null. Captured by the ball
        /// on a shot for assist effects (Playmaker, Energizer).</summary>
        public PlayerController AssistingPasser =>
            (_assistPasser != null && !_assistDribbled && Time.time - _assistTime <= assistWindow)
                ? _assistPasser : null;

        /// <summary>+2 if shooting directly off a Playmaker's pass (in time, no drive).</summary>
        int AssistBonus()
        {
            if (_assistPasser == null || _assistDribbled || Time.time - _assistTime > assistWindow) return 0;
            var s = _assistPasser.Character != null ? _assistPasser.Character.stats : null;
            return (s != null && s.hiddenTrait == HiddenTrait.Playmaker) ? 2 : 0;
        }

        bool QuickCatchReady() =>
            _character != null && _character.stats != null
            && _character.stats.hiddenTrait == HiddenTrait.QuickCatchShooter
            && (Time.time - _catchTime) <= quickCatchWindow;

        void AdvanceShotMeter(float dt)
        {
            if (!_shooting) return;
            if (IsStunned || !HasBall) { _shooting = false; return; } // lost the ball / knocked
            _shotCharge += dt;
            if (_shotCharge >= ShotMeterDuration) ReleaseJumpShot(); // held too long → late shot
        }

        void AdvanceFinish(float dt)
        {
            if (!_finishing) return;
            if (IsStunned || !HasBall) { _finishing = false; return; }
            _finishTimer += dt;
            if (_finishTimer >= finishAirTime) ResolveFinish(); // committed at the rim
        }

        void HandlePostHold()
        {
            bool wantPost = _input.PostUpHeld && HasBall && !IsStunned && _cc.isGrounded;
            if (wantPost && !IsPosting) _post.Begin(NearestOpponentTo(transform.position));
            else if (!_input.PostUpHeld && IsPosting) _post.End();
        }

        public void Teleport(Vector3 position)
        {
            // Spots are authored for ~2 m players; keep taller bodies above the
            // floor (centre must sit at half the controller height).
            position.y = Mathf.Max(position.y, _cc.height / 2f + 0.05f);
            bool was = _cc.enabled;
            _cc.enabled = false;
            transform.position = position;
            _cc.enabled = was;
        }

        float Effective(StatType stat, float fallback) =>
            _character != null ? _character.GetEffective(stat) : fallback;

        public float EffectiveStat(StatType stat) => Effective(stat, 5f);

        void Move()
        {
            float dt = Time.deltaTime;
            Vector3 horizontal;
            bool rotateToMove = false;
            Vector3 faceDir = Vector3.zero;

            if (IsStunned)
            {
                horizontal = Vector3.zero;
                _character?.ReportActivity(false, false);
            }
            else if (IsPosting)
            {
                horizontal = _post.DriveVelocity; // PostUpController owns facing
                _character?.ReportActivity(true, false);
            }
            else if (_diveTimer > 0f)
            {
                horizontal = _diveDir * diveSpeed;
                _character?.ReportActivity(true, true);
            }
            else if (_finishing)
            {
                // Glide toward the rim while up for the dunk/layup.
                Vector3 toRim = RimDirection();
                horizontal = toRim.sqrMagnitude > 0.01f ? toRim.normalized * finishApproachSpeed : Vector3.zero;
                _character?.ReportActivity(true, false);
            }
            else if (_shooting)
            {
                // A jump shot doesn't run: hold the stick to fade that way (the
                // body leans, see ProceduralAnimator), or hold nothing to rise
                // straight up. We stay squared to the rim so it reads as a
                // fadeaway, not a drift.
                Vector3 fade = new Vector3(_moveIntent.x, 0f, _moveIntent.y);
                _fadeAmount = Mathf.Clamp01(fade.magnitude);
                if (_fadeAmount > 0.05f)
                {
                    _fadeDir = fade.normalized;
                    // Fading with your momentum is easy; against it (planting the
                    // wrong way at the last second) barely leans — and the faster
                    // you were going, the harder it is to reverse.
                    _fadeAmount *= MomentumFadeScale(_fadeDir);
                    horizontal = _fadeDir * fadeSpeed * _fadeAmount;
                }
                else
                {
                    _fadeAmount = 0f;
                    horizontal = Vector3.zero;
                }
                Vector3 toRim = RimDirection();
                if (toRim.sqrMagnitude > 0.01f) { rotateToMove = true; faceDir = toRim.normalized; }
                _character?.ReportActivity(false, false);
            }
            else
            {
                Vector3 dir = new Vector3(_moveIntent.x, 0f, _moveIntent.y);
                if (dir.sqrMagnitude > 1f) dir.Normalize();

                float speedStat = Effective(StatType.Speed, 5f);
                float baseSpeed = Mathf.Lerp(minMoveSpeed, maxMoveSpeed, Mathf.Clamp01((speedStat - 1f) / 9f));
                bool sprinting = _sprintIntent && dir.sqrMagnitude > 0.01f;
                float speed = baseSpeed * (sprinting ? sprintMultiplier : 1f);
                if (_dribbleBoostTimer > 0f) speed *= dribbleBoostMult; // separation after a move
                _character?.ReportActivity(dir.sqrMagnitude > 0.01f, sprinting);

                horizontal = dir * speed;
                _lastRunVelocity = horizontal; // momentum carried into a fadeaway
                rotateToMove = dir.sqrMagnitude > 0.01f;
                faceDir = dir;
            }

            if (_shoveTimer > 0f) horizontal += _shoveVel;
            horizontal += Separation();   // never stand on / inside another player
            PlanarSpeed = horizontal.magnitude;

            if (_cc.isGrounded && _verticalVelocity < 0f) _verticalVelocity = -2f;
            _verticalVelocity += gravity * dt;

            Vector3 velocity = horizontal + Vector3.up * _verticalVelocity;
            _cc.Move(velocity * dt);

            if (rotateToMove)
            {
                Quaternion want = Quaternion.LookRotation(faceDir, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, want, turnSpeed * dt);
            }
        }

        /// <summary>Latch the dribble state. You start dribbling the moment you
        /// move with the ball; once started, standing still keeps the dribble
        /// alive. It ends when you no longer have the ball, leave your feet, or
        /// go into a shot / finish / post / stun.</summary>
        void UpdateDribbleState()
        {
            if (!HasBall || _cc == null || !_cc.isGrounded
                || IsShooting || IsFinishing || IsPosting || IsStunned)
            {
                _dribbling = false;
                return;
            }
            // Putting it on the floor (any real movement) starts the dribble.
            if (PlanarSpeed > dribbleMoveThreshold) _dribbling = true;
        }

        /// <summary>Convert a raw stick vector into a world-plane move direction
        /// relative to the camera, so pushing the stick "up" drives the player up
        /// the screen no matter where the sideline camera is. Returned as an XZ
        /// vector (x → world X, y → world Z) to match how <see cref="Move"/> reads
        /// the intent. AI intents bypass this — they're already world-space.</summary>
        Vector2 CameraRelative(Vector2 stick)
        {
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return stick;

            Vector3 fwd = _cam.transform.forward; fwd.y = 0f;
            Vector3 right = _cam.transform.right; right.y = 0f;
            if (fwd.sqrMagnitude < 1e-4f) fwd = Vector3.forward;
            fwd.Normalize();
            right.Normalize();

            Vector3 world = right * stick.x + fwd * stick.y;
            return new Vector2(world.x, world.z);
        }

        /// <summary>Soft body separation: if another player is overlapping us
        /// horizontally, push apart so nobody can stand on (or inside) anyone.</summary>
        Vector3 Separation()
        {
            var gm = GameManager.Instance;
            if (gm == null) return Vector3.zero;
            float myR = _cc != null ? _cc.radius : 0.4f;
            Vector3 push = Vector3.zero;

            foreach (var side in new[] { gm.Home, gm.Away })
            {
                foreach (var other in side.onCourt)
                {
                    if (other == null || other == this || !other.enabled) continue;
                    Vector3 d = transform.position - other.transform.position; d.y = 0f;
                    float minDist = myR + other.BodyRadius;
                    float dist = d.magnitude;
                    if (dist >= minDist) continue;
                    Vector3 dir = dist > 0.01f ? d / dist : new Vector3(Random.value - 0.5f, 0f, Random.value - 0.5f).normalized;
                    push += dir * (minDist - dist) * 6f; // proportional to overlap
                }
            }
            return push;
        }

        public float BodyRadius => _cc != null ? _cc.radius : 0.4f;

        BallController Ball => GameManager.Instance != null ? GameManager.Instance.ball : null;

        // ---- Actions (input events or AI brain) ----------------------------

        /// <summary>Immediate shot with perfect timing — used by the AI. Inside
        /// the paint it finishes (dunk/layup) with no air-adjust.</summary>
        public void TriggerShoot()
        {
            if (MatchPause.IsPaused || IsStunned || IsPosting) return;
            if (InsideRange()) FinishShot(Effective(StatType.Dunk, 5f) >= dunkThreshold, adjusted: false);
            else ExecuteShot(1f, QuickCatchReady());
        }

        // Human shooting: hold to rise, release to commit. Jump shots use the
        // release-timing meter; inside (dunk/layup) goes up for a finish you can
        // air-adjust (L1) or pass out of.
        void OnShootPressed()
        {
            if (MatchPause.IsPaused || IsStunned || IsPosting || !HasBall || _shooting || _finishing) return;
            if (IconPassActive) { PassToSlot(0); return; } // LB + A → pass to teammate 1
            Hoop hoop = GameManager.Instance.GetAttackingHoop(team);
            if (hoop == null) return;

            if (InsideRange()) { StartFinish(); return; }

            _pendingQuickCatch = QuickCatchReady(); // captured at the catch, before the jump
            _shooting = true;
            _shotCharge = 0f;
            _fadeDir = Vector3.zero;
            _fadeAmount = 0f;
            _launchVel = _lastRunVelocity; // the momentum you take into the jump
            if (_cc.isGrounded) _verticalVelocity = Mathf.Sqrt(-2f * gravity * jumpHeight); // jump
        }

        void OnShootReleased()
        {
            if (_shooting) ReleaseJumpShot();
            else if (_finishing) ResolveFinish();
        }

        bool InsideRange()
        {
            var gm = GameManager.Instance;
            Hoop hoop = gm != null ? gm.GetAttackingHoop(team) : null;
            return hoop != null && HorizontalDistance(transform.position, hoop.AimPoint) <= paintRadius;
        }

        Vector3 RimDirection()
        {
            var gm = GameManager.Instance;
            Hoop hoop = gm != null ? gm.GetAttackingHoop(team) : null;
            if (hoop == null) return Vector3.zero;
            Vector3 d = hoop.AimPoint - transform.position; d.y = 0f;
            return d;
        }

        // ---- Inside finishing (dunk / layup) -------------------------------

        void StartFinish()
        {
            _finishing = true;
            _finishTimer = 0f;
            _finishAdjusted = false;
            _finishIsDunk = Effective(StatType.Dunk, 5f) >= dunkThreshold;
            if (_cc.isGrounded) _verticalVelocity = Mathf.Sqrt(-2f * gravity * jumpHeight); // go up
        }

        void ResolveFinish()
        {
            if (!_finishing) return;
            _finishing = false;
            FinishShot(_finishIsDunk, _finishAdjusted);
        }

        /// <summary>Resolve a dunk or layup: a block roll first (reduced by an
        /// air-adjust, and resisted by Power on dunks), then a make roll. A dunk
        /// scores off the Dunk stat, a layup off Inside Scoring.</summary>
        void FinishShot(bool isDunk, bool adjusted, float makeBonus = 0f)
        {
            if (!HasBall) return;
            var gm = GameManager.Instance;
            Hoop hoop = gm.GetAttackingHoop(team);
            if (hoop == null) return;

            Vector3 aim = hoop.AimPoint;
            _lastShotDistance = HorizontalDistance(transform.position, aim); // for Delfan's called shot
            gm.RecordShotAttempt(this, 2); // an inside finish is always a 2
            // Dunk scores off Dunk, layup off Inside Scoring; +2 off a Playmaker pass.
            StatType scoreStat = isDunk ? StatType.Dunk : StatType.InsideScoring;
            int rawFinish = (_character != null ? _character.stats.Get(scoreStat) : 5) + AssistBonus();
            float finisherStat = _character != null ? _character.GetEffectiveForStat(rawFinish, scoreStat) : 5f;
            PlayerController defender = NearestOpponentTo(transform.position);

            if (defender != null)
            {
                float dd = HorizontalDistance(defender.transform.position, transform.position);
                if (dd < blockRange)
                {
                    float closeness = 1f - dd / contestRange;
                    float blk = defender.EffectiveStat(StatType.Blocks);
                    float resist = isDunk ? Effective(StatType.Power, 5f) * dunkPowerBlockResist : 0f;
                    float chance = Mathf.Clamp(blockBaseChance + blockStatScale * (blk - finisherStat - resist), 0f, blockMaxChance) * closeness;
                    if (adjusted) chance *= adjustBlockReduction; // contort away from the block
                    if (Random.value < chance)
                    {
                        Vector3 away = transform.position - aim; away.y = 0f;
                        Ball.Pass(away.sqrMagnitude > 0.01f ? away : -transform.forward, blockKnockPower);
                        GameManager.Instance.RecordBlock(defender);
                        GameManager.Instance.OnShotMissed(this);
                        return;
                    }
                }
            }

            bool onFire = _character != null && _character.OnFire;
            float over = finisherStat; // already includes the scoring stat + assist
            float makeChance = ShotMath.MakeChance(this, StatType.InsideScoring, HorizontalDistance(transform.position, aim), defender, onFire, over);
            if (adjusted) makeChance -= AdjustPenalty();
            makeChance += makeBonus;
            makeChance = Mathf.Clamp(makeChance, 0f, ShotMath.MaxChance);
            bool make = Random.value < makeChance;
            Ball.Shoot(aim, team, 2, finishFlightTime, ShotMath.AimOffset(make), this);
        }

        /// <summary>True if this player has the given hidden trait.</summary>
        bool HasTrait(HiddenTrait trait)
            => _character != null && _character.stats != null && _character.stats.hiddenTrait == trait;

        /// <summary>Apply the Acrobat (Baby Mario) timing relief to a raw
        /// release-timing multiplier: he eats only a fraction of the mistiming
        /// penalty. Shared by jump shots and timed post shots.</summary>
        public float TimingWithTrait(float timing)
            => HasTrait(HiddenTrait.Acrobat) ? 1f - (1f - timing) * (1f - acrobatTimingRelief) : timing;

        /// <summary>Killer Instinct (Daisy): refresh the bonus from how gassed the
        /// opposing on-court team is — fresh legs give nothing, dead legs give the
        /// full <see cref="killerMaxBonus"/>. It only lands on her scoring and
        /// perimeter-defense stats (see <c>PlayerCharacter.TraitBonusForStat</c>).</summary>
        void UpdateKillerInstinct()
        {
            if (_character == null || !HasTrait(HiddenTrait.KillerInstinct)) return;
            var gm = GameManager.Instance;
            if (gm == null) { _character.KillerBonus = 0f; return; }

            float sum = 0f; int n = 0;
            foreach (var o in gm.TeamFor(GameManager.Opponent(team)).onCourt)
            {
                if (o == null || o.Character == null || !o.enabled) continue;
                sum += 1f - o.Character.EnergyFraction; // 0 fresh … 1 spent
                n++;
            }
            float fatigue = n > 0 ? sum / n : 0f;
            float scaled = Mathf.Clamp01((fatigue - killerFatigueFloor) / Mathf.Max(0.01f, 1f - killerFatigueFloor));
            _character.KillerBonus = killerMaxBonus * scaled;
        }

        /// <summary>Called Shot (Delfan): double-tapping turbo while one of his
        /// shots — taken from within half court — is in the air guarantees it
        /// drops. Twice a game.</summary>
        void OnTurboDoubleTap()
        {
            if (MatchPause.IsPaused || !HasTrait(HiddenTrait.CalledShot)) return;
            if (_calledShotsUsed >= calledShotMax) return;
            var ball = Ball;
            if (ball == null || ball.State != BallController.BallState.Shot || ball.Shooter != this) return;
            if (_lastShotDistance > calledShotRange) return; // beyond half court — no dice
            if (ball.ForceMake()) _calledShotsUsed++;
        }

        /// <summary>Make% lost to an air-adjust — fully mitigated at Inside 10,
        /// and waived entirely for an Acrobat (Baby Mario alters in the air free).</summary>
        float AdjustPenalty()
        {
            if (HasTrait(HiddenTrait.Acrobat)) return 0f;
            float inside = Effective(StatType.InsideScoring, 5f);
            return maxAdjustPenalty * (1f - Mathf.Clamp01((inside - 1f) / 9f));
        }

        /// <summary>How much of the requested fade actually comes out, given the
        /// momentum carried into the jump: leaning <b>with</b> your run direction
        /// keeps the full fade, leaning <b>against</b> it (at speed) collapses
        /// toward <see cref="fadeAgainstMomentumMin"/>. Standing still, you fade
        /// freely either way.</summary>
        float MomentumFadeScale(Vector3 fadeDir)
        {
            float sp = _launchVel.magnitude;
            if (sp < 0.5f) return 1f; // not moving — lean wherever you like
            float align = Vector3.Dot(fadeDir, _launchVel / sp);          // -1 against … +1 with
            float withness = (align + 1f) * 0.5f;                          // 0 … 1
            float scaleAtSpeed = Mathf.Lerp(fadeAgainstMomentumMin, 1f, withness);
            float speed01 = Mathf.Clamp01(sp / maxMoveSpeed);              // slow runs barely constrain
            return Mathf.Lerp(1f, scaleAtSpeed, speed01);
        }

        void ReleaseJumpShot()
        {
            _shooting = false;
            float error = Mathf.Abs(_shotCharge - _apexTime);
            float timing = error <= perfectReleaseWindow
                ? 1f
                : Mathf.Clamp(1f - (error - perfectReleaseWindow) * timingFalloffPerSec, minTimingMultiplier, 1f);
            timing = TimingWithTrait(timing); // Acrobat (Baby Mario) shrugs off mistiming
            ExecuteShot(timing, _pendingQuickCatch, _fadeAmount);
        }

        /// <summary>
        /// Resolve a shot: block roll first (unaffected by timing or on fire),
        /// then a make roll using <see cref="ShotMath"/> scaled by the release
        /// <paramref name="timingMultiplier"/> (1 = perfect). A quick catch-and-
        /// shoot three overrides the 3-Point rating to a 10. A
        /// <paramref name="fadeAmount"/> (0-1) is a fadeaway: it buys separation
        /// (lower block + contest) at the cost of a harder shot.
        /// </summary>
        void ExecuteShot(float timingMultiplier, bool quickCatch, float fadeAmount = 0f)
        {
            if (!HasBall) return;
            Hoop hoop = GameManager.Instance.GetAttackingHoop(team);
            if (hoop == null) return;

            Vector3 aim = hoop.AimPoint;
            float distance = HorizontalDistance(transform.position, aim);
            _lastShotDistance = distance; // for Delfan's within-half-court called shot
            int points = distance >= threePointDistance ? 3 : 2;
            GameManager.Instance.RecordShotAttempt(this, points);

            StatType shotStat =
                distance >= threePointDistance ? StatType.ThreePoint :
                distance <= paintRadius ? StatType.InsideScoring :
                StatType.MidRange;

            PlayerController defender = NearestOpponentTo(transform.position);

            // Effective scoring stat with trait modifiers: quick catch-and-shoot
            // three counts as 10 (Piranha), and +2 off a Playmaker pass (Koopa).
            int rawStat = _character != null ? _character.stats.Get(shotStat) : 5;
            if (quickCatch && shotStat == StatType.ThreePoint) rawStat = 10;
            rawStat += AssistBonus();
            float shotStatValue = _character != null ? _character.GetEffectiveForStat(rawStat, shotStat) : 5f;

            // Block check first — unaffected by timing or being on fire.
            if (defender != null)
            {
                float dd = HorizontalDistance(defender.transform.position, transform.position);
                if (dd < blockRange)
                {
                    float closeness = 1f - dd / contestRange;
                    float blk = defender.EffectiveStat(StatType.Blocks);
                    float chance = Mathf.Clamp(blockBaseChance + blockStatScale * (blk - shotStatValue), 0f, blockMaxChance) * closeness;
                    chance *= 1f - fadeBlockReduction * fadeAmount; // fading away from the contest
                    if (Random.value < chance)
                    {
                        Vector3 away = transform.position - aim; away.y = 0f;
                        Ball.Pass(away.sqrMagnitude > 0.01f ? away : -transform.forward, blockKnockPower);
                        GameManager.Instance.RecordBlock(defender);
                        GameManager.Instance.OnShotMissed(this); // blocked → streak broken
                        return;
                    }
                }
            }

            bool onFire = _character != null && _character.OnFire;
            float contestScale = 1f - fadeContestReduction * fadeAmount;
            float makeChance = ShotMath.MakeChance(this, shotStat, distance, defender, onFire, shotStatValue, contestScale) * timingMultiplier;
            makeChance -= ShotMath.FadePenalty(this, fadeAmount); // flat fadeaway difficulty (0 for an Acrobat)
            makeChance = Mathf.Clamp(makeChance, 0f, ShotMath.MaxChance);
            bool make = Random.value < makeChance;
            Ball.Shoot(aim, team, points, shotFlightTime, ShotMath.AimOffset(make), this);
        }

        /// <summary>AI pass entry — throws a loft immediately.</summary>
        public void TriggerPass() => ReleasePass(hard: false);

        // Human passing: tap → loft (slow, arcs over defenders); hold past
        // passHoldThreshold → hard pass (fast, flat, lives in the steal lane).
        void OnPassPressed()
        {
            if (MatchPause.IsPaused || IsStunned || !HasBall || _passCharging) return;
            _passCharging = true;
            _passChargeTime = 0f;
        }

        void OnPassReleased()
        {
            if (!_passCharging) return;
            _passCharging = false;
            ReleasePass(hard: _passChargeTime >= passHoldThreshold);
        }

        void ReleasePass(bool hard)
        {
            if (MatchPause.IsPaused || IsStunned || !HasBall) return;
            _passGestureTimer = passGestureTime; // throw animation
            bool fromPost = IsPosting;
            if (IsPosting) _post.End();   // kick out of the post
            _finishing = false;           // or dump it off out of the air

            // Aim with the right stick to direct it to a specific teammate
            // (icons); otherwise pass to whoever's most open.
            var mate = IsAimingPass ? TargetedTeammate(_passAim) : FindOpenTeammate();
            if (mate == null) { Ball.Pass(transform.forward, passPower); return; }

            // A loft to a teammate near the rim is an alley-oop.
            if (!hard && IsOopTarget(mate)) ThrowOop(mate, fromPost);
            else PassToTeammate(mate, fromPost, hard);
        }

        bool IsOopTarget(PlayerController mate)
        {
            var gm = GameManager.Instance;
            Hoop hoop = gm != null ? gm.GetAttackingHoop(team) : null;
            return hoop != null && HorizontalDistance(mate.transform.position, hoop.AimPoint) <= oopRange;
        }

        void ThrowOop(PlayerController mate, bool fromPost)
        {
            var gm = GameManager.Instance;
            Hoop hoop = gm != null ? gm.GetAttackingHoop(team) : null;
            if (hoop == null) { PassToTeammate(mate, fromPost, false); return; }

            // Lob to a high point near the rim, led slightly toward the cutter.
            Vector3 target = Vector3.Lerp(hoop.AimPoint, mate.transform.position, 0.35f);
            target.y = hoop.AimPoint.y; // rim height — the cutter jumps to meet it
            float err = PassError(PassBallHandling(fromPost));
            Vector2 j = Random.insideUnitCircle * err;
            target += new Vector3(j.x, 0f, j.y);
            Ball.PassTo(target, oopFlightTime, alleyOop: true);
        }

        void PassToSlot(int index)
        {
            var mate = TeammateSlot(index);
            if (mate != null) { _passGestureTimer = passGestureTime; PassToTeammate(mate, fromPost: false, hard: false); }
        }

        /// <summary>Lead pass to a teammate; Ball Handling sets the accuracy, so
        /// a weak handler's pass lands off-target (and can be picked off). A
        /// Smooth Passer throws with Ball Handling counted as 8 (10 out of a post).</summary>
        void PassToTeammate(PlayerController mate, bool fromPost, bool hard)
        {
            float err = PassError(PassBallHandling(fromPost));
            Vector2 j = Random.insideUnitCircle * err;
            Vector3 dest = mate.transform.position + new Vector3(j.x, 0.6f, j.y);
            Ball.PassTo(dest, hard ? hardPassTime : loftPassTime);
        }

        float PassBallHandling(bool fromPost)
        {
            if (_character != null && _character.stats != null && _character.stats.hiddenTrait == HiddenTrait.SmoothPasser)
                return _character.GetEffectiveFor(fromPost ? 10 : 8);
            return Effective(StatType.BallHandling, 5f);
        }

        float PassError(float bh) => Mathf.Lerp(passErrorMax, passErrorMin, Mathf.Clamp01((bh - 1f) / 9f));

        /// <summary>Catch an alley-oop and finish it immediately (GameManager calls
        /// this when a teammate snags an oop near the rim).</summary>
        public void CatchAlleyOop()
        {
            if (!HasBall) return;
            bool dunk = Effective(StatType.Dunk, 5f) >= dunkThreshold;
            FinishShot(dunk, adjusted: false, makeBonus: alleyOopBonus);
        }

        /// <summary>The index-th on-court teammate (excluding self) — for icon passing.</summary>
        PlayerController TeammateSlot(int index)
        {
            var gm = GameManager.Instance;
            if (gm == null) return null;
            int n = 0;
            foreach (var mate in gm.TeamFor(team).onCourt)
            {
                if (mate == null || mate == this || !mate.enabled) continue;
                if (n == index) return mate;
                n++;
            }
            return null;
        }

        /// <summary>Teammate whose on-screen direction best matches the aim.</summary>
        PlayerController TargetedTeammate(Vector2 aim)
        {
            var gm = GameManager.Instance;
            if (gm == null || aim.sqrMagnitude < 0.0001f) return null;
            Vector2 a = aim.normalized;
            Camera cam = Camera.main;
            Vector2 from = ScreenOf(cam, transform.position);

            PlayerController best = null;
            float bestDot = 0.25f; // require some alignment
            foreach (var mate in gm.TeamFor(team).onCourt)
            {
                if (mate == null || mate == this || !mate.enabled) continue;
                Vector2 dir = ScreenOf(cam, mate.transform.position) - from;
                if (dir.sqrMagnitude < 1f) continue;
                float dot = Vector2.Dot(dir.normalized, a);
                if (dot > bestDot) { bestDot = dot; best = mate; }
            }
            return best;
        }

        static Vector2 ScreenOf(Camera cam, Vector3 world)
        {
            if (cam != null)
            {
                Vector3 sp = cam.WorldToScreenPoint(world);
                return new Vector2(sp.x, sp.y);
            }
            return new Vector2(world.x, world.z); // fallback: world plane
        }

        /// <summary>A directed pass to a teammate (used by the AI).</summary>
        public void PassToward(Vector3 worldPoint)
        {
            if (MatchPause.IsPaused || IsStunned || !HasBall) return;
            _passGestureTimer = passGestureTime; // throw animation
            Ball.PassTo(worldPoint);
        }

        public void TriggerJump()
        {
            if (MatchPause.IsPaused || IsStunned || IsPosting) return; // Y is Hook while posting
            if (_cc.isGrounded) _verticalVelocity = Mathf.Sqrt(-2f * gravity * jumpHeight);
        }

        public void TriggerSteal()
        {
            if (MatchPause.IsPaused || IsStunned || _stealCooldown > 0f) return;
            var gm = GameManager.Instance;
            if (gm == null || gm.ball == null) return;

            var holder = gm.ball.Holder;
            if (holder == null || holder == this || holder.team == team) return;

            float dist = HorizontalDistance(transform.position, holder.transform.position);
            if (dist > stealReach) { _stealCooldown = stealWhiffCooldown; return; }

            _stealCooldown = stealCooldown;
            float steal = EffectiveStat(StatType.Steals);
            float handle = holder.EffectiveStat(StatType.BallHandling);
            float chance = Mathf.Clamp(stealBaseChance + stealStatScale * (steal - handle), stealMinChance, stealMaxChance);
            if (Random.value < chance)
            {
                gm.ball.PickUp(this);
                gm.OnPossessionGained(this);
                gm.RecordSteal(this);
            }
        }

        /// <summary>The B button: pass-icon select (LB held), dribble move (with
        /// the ball), or dive for a loose ball.</summary>
        public void TriggerDive()
        {
            if (MatchPause.IsPaused || IsStunned) return;
            if (IconPassActive) { PassToSlot(1); return; }  // LB + B → pass to teammate 2
            if (IsPosting) return;                          // B is Spin while posting
            if (HasBall) { TriggerDribbleMove(); return; }  // with the ball, it's a dribble move

            if (_diveTimer > 0f || !_cc.isGrounded) return;
            _diveDir = transform.forward;
            var ball = Ball;
            if (ball != null && ball.CanBePickedUpBy(this))
            {
                Vector3 to = ball.transform.position - transform.position; to.y = 0f;
                if (to.sqrMagnitude > 0.01f && to.magnitude <= diveBallSeekRange) _diveDir = to.normalized;
            }
            _diveTimer = diveDuration;
        }

        /// <summary>AI hook for a dribble move.</summary>
        public void AttemptDribbleMove() => TriggerDribbleMove();

        /// <summary>Break the on-ball defender down — Ball Handling vs Perimeter
        /// Defense. Win → the defender is frozen ("ankles broken") and you get a
        /// burst of separation; a bad miss can get you stripped.</summary>
        void TriggerDribbleMove()
        {
            if (_dribbleCooldown > 0f || !HasBall || !_cc.isGrounded) return;
            var def = NearestOpponentTo(transform.position);
            if (def == null) { _dribbleCooldown = dribbleCooldownTime; return; }
            if (HorizontalDistance(transform.position, def.transform.position) > dribbleRange)
            {
                _dribbleCooldown = dribbleCooldownTime * 0.5f;
                return;
            }

            _dribbleCooldown = dribbleCooldownTime;
            _dribbleMoveGestureTimer = dribbleMoveGestureTime; // cross-body sweep
            float bh = Effective(StatType.BallHandling, 5f);
            float pd = def.EffectiveStat(StatType.PerimeterDefense);
            float chance = Mathf.Clamp(dribbleBaseChance + dribbleStatScale * (bh - pd), 0.05f, 0.95f);

            if (Random.value < chance)
            {
                def.Stun(ankleStun, fall: true);  // broken ankles — they hit the deck
                _dribbleBoostTimer = dribbleBoostTime; // separation
                if (GameManager.Instance != null && GameManager.Instance.ball != null)
                    GameManager.Instance.ball.Crossover(); // sweep the ball across
                if (RimDirection().sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(RimDirection().normalized, Vector3.up);
            }
            else
            {
                // Overhandled it — a good defender can poke it away.
                float strip = Mathf.Clamp(0.05f + 0.04f * (pd - bh), 0f, 0.4f);
                if (Random.value < strip && GameManager.Instance != null && GameManager.Instance.ball != null)
                {
                    GameManager.Instance.ball.PickUp(def);
                    GameManager.Instance.OnPossessionGained(def);
                    GameManager.Instance.RecordSteal(def); // defender poked it away
                }
            }
        }

        /// <summary>A right-stick flick — a hard dribble in that direction to
        /// create separation. Read relative to the basket: away = step-back,
        /// toward = attacking burst, sideways = crossover (and two quick opposite
        /// sideways flicks chain into a hesitation cross, the big ankle-breaker).
        /// In the post it's a shimmy (<see cref="PostUpController.Shimmy"/>).
        /// Layers on top of the dribble-move button, it doesn't replace it.</summary>
        void OnDribbleFlick(Vector2 stick)
        {
            if (MatchPause.IsPaused || IsStunned || !HasBall || _shooting || _finishing) return;
            if (_flickCooldown > 0f || !_cc.isGrounded) return;
            Vector3 dir = new Vector3(stick.x, 0f, stick.y);
            if (dir.sqrMagnitude < 0.01f) return;
            dir.Normalize();
            _flickCooldown = flickCooldownTime;
            _dribbleMoveGestureTimer = dribbleMoveGestureTime; // hard-dribble sweep

            var ball = Ball;
            if (IsPosting)
            {
                _post.Shimmy(dir);
                if (ball != null) ball.Crossover();
                return;
            }

            // Read the flick relative to the basket.
            Vector3 toBasket = RimDirection();
            toBasket = toBasket.sqrMagnitude > 0.01f ? toBasket.normalized : transform.forward;
            float dot = Vector3.Dot(dir, toBasket);

            bool stepBack = dot <= -0.5f;
            bool attack = dot >= 0.5f;
            bool hesitationCross = false;
            if (!stepBack && !attack)
            {
                // Lateral: opposite flicks in quick succession = hesitation cross.
                float side = Mathf.Sign(Vector3.Cross(toBasket, dir).y);
                hesitationCross = Time.time - _lastLateralFlickTime <= hesitationWindow
                                  && side != _lastLateralFlickSign && _lastLateralFlickSign != 0f;
                _lastLateralFlickTime = Time.time;
                _lastLateralFlickSign = side;
            }

            // The dribble itself always happens: a burst in the flick direction.
            ApplyShove(dir * (stepBack ? stepBackPower : flickBurstPower));
            _dribbleBoostTimer = attack || hesitationCross ? dribbleBoostTime : dribbleBoostTime * 0.5f;
            if (ball != null) ball.Crossover();
            // A step-back squares you to the hoop for the shot; otherwise face the move.
            transform.rotation = Quaternion.LookRotation(stepBack ? toBasket : dir, Vector3.up);

            // Whether it shakes the defender is Ball Handling vs Perimeter Defense.
            var def = NearestOpponentTo(transform.position);
            if (def == null || HorizontalDistance(transform.position, def.transform.position) > dribbleRange) return;

            float bh = Effective(StatType.BallHandling, 5f);
            float pd = def.EffectiveStat(StatType.PerimeterDefense);
            float chance = Mathf.Clamp(dribbleBaseChance + dribbleStatScale * (bh - pd), 0.05f, 0.95f);
            if (Random.value < chance)
            {
                // The hesitation cross is the highlight move — full broken ankles.
                if (hesitationCross) def.Stun(ankleStun, fall: true);
                else def.Stun(flickFreeze);
            }
            else
            {
                // Exposed the ball on the move — a good defender can poke it free.
                float strip = Mathf.Clamp(0.04f + 0.03f * (pd - bh), 0f, 0.3f);
                if (Random.value < strip && GameManager.Instance != null && GameManager.Instance.ball != null)
                {
                    GameManager.Instance.ball.PickUp(def);
                    GameManager.Instance.OnPossessionGained(def);
                    GameManager.Instance.RecordSteal(def); // exposed it on the move
                }
            }
        }

        public void TriggerBackDown()
        {
            if (MatchPause.IsPaused || IsStunned) return;
            if (IsPosting) { _post.OffenseTap(); return; }       // push in
            var poster = FindPosterGuardingMe();
            if (poster != null) { poster.DefenderTap(); return; } // bump a poster
            TryPush();                                            // push/foul in space
        }

        /// <summary>AI hook to commit a foul.</summary>
        public void AttemptPush() => TryPush();

        /// <summary>
        /// Shove the nearest opponent (Power vs Power). It's a team foul: below
        /// the penalty limit play continues and the shove just disrupts (and can
        /// knock the ball loose / knock a weaker player down); in the penalty it
        /// sends them to the line.
        /// </summary>
        void TryPush()
        {
            if (_pushCooldown > 0f || HasBall) return;
            var gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.Playing) return;

            var target = NearestOpponentTo(transform.position);
            if (target == null) return;
            if (HorizontalDistance(transform.position, target.transform.position) > pushRange)
            {
                _pushCooldown = pushWhiffCooldown;
                return;
            }

            _pushCooldown = pushCooldown;

            bool whistle = gm.RegisterFoul(team, target, target.HasBall);
            if (whistle) return; // free throws — don't play the contact out

            float myPower = EffectiveStat(StatType.Power);
            float targetPower = target.EffectiveStat(StatType.Power);

            Vector3 dir = target.transform.position - transform.position; dir.y = 0f;
            dir = dir.sqrMagnitude > 0.01f ? dir.normalized : transform.forward;

            float strength = Mathf.Clamp01((myPower - targetPower + 5f) / 10f);
            target.ApplyShove(dir * pushForce * strength);

            bool overpowered = myPower - targetPower >= pushKnockdownPowerGap;
            if (overpowered) target.Stun(0.7f);

            if (target.HasBall && gm.ball != null)
            {
                float gap = myPower - (targetPower + target.EffectiveStat(StatType.BallHandling)) * 0.5f;
                float knock = overpowered ? 1f : Mathf.Clamp(pushKnockLooseBase + pushKnockLooseScale * gap, 0.05f, 0.9f);
                if (Random.value < knock) gm.ball.Pass(dir, 3f); // pop it loose
            }
        }

        // The post buttons are layered: turbo (LT) upgrades each to its advanced
        // version, and Drop Step pressed while a fake is live is the up-and-under.
        void TriggerHook() => TriggerPostMove(_sprintIntent ? PostMove.SkyHook : PostMove.Hook);

        void TriggerDropStep()
        {
            if (IsPosting && _post.FakeActive) { TriggerPostMove(PostMove.UpAndUnder); return; }
            TriggerPostMove(_sprintIntent ? PostMove.PowerDropStep : PostMove.DropStep);
        }

        void TriggerSpin() => TriggerPostMove(_sprintIntent ? PostMove.TurnaroundJumper : PostMove.Spin);

        // L1: air-adjust while finishing, otherwise the post fake.
        void TriggerFake()
        {
            if (_finishing) { _finishAdjusted = true; return; }
            TriggerPostMove(PostMove.Fake);
        }

        public void TriggerPostMove(PostMove move)
        {
            if (MatchPause.IsPaused || IsStunned || !IsPosting) return;
            // Once a move's shot is charging, any post button releases it (timing
            // the shot) rather than starting a new move.
            if (_post.PostShotActive) { _post.ReleasePostShot(); return; }
            _post.DoMove(move);
        }

        // ---- AI hooks ------------------------------------------------------

        public void BeginPost()
        {
            if (HasBall && !IsStunned && _cc.isGrounded && !IsPosting)
                _post.Begin(NearestOpponentTo(transform.position));
        }

        public void EndPost() { if (IsPosting) _post.End(); }
        public void PostBackDown() { if (IsPosting) _post.OffenseTap(); }
        public void DoPostMove(PostMove move) { if (IsPosting) _post.DoMove(move); }

        // ---- State changes from other systems ------------------------------

        public void Stun(float seconds, bool fall = false)
        {
            _stunTimer = Mathf.Max(_stunTimer, seconds);
            if (fall) _fallTimer = Mathf.Max(_fallTimer, seconds);
            if (IsPosting) _post.End();
        }

        public void ApplyShove(Vector3 velocity)
        {
            _shoveVel = velocity;
            _shoveTimer = shoveDuration;
        }

        // ---- Helpers -------------------------------------------------------

        public PlayerController NearestOpponentTo(Vector3 point)
        {
            var gm = GameManager.Instance;
            if (gm == null) return null;
            var opponents = gm.TeamFor(GameManager.Opponent(team)).onCourt;
            PlayerController best = null;
            float bestD = Mathf.Infinity;
            foreach (var o in opponents)
            {
                if (o == null || !o.enabled) continue;
                float d = HorizontalDistance(o.transform.position, point);
                if (d < bestD) { bestD = d; best = o; }
            }
            return best;
        }

        PostUpController FindPosterGuardingMe()
        {
            var gm = GameManager.Instance;
            if (gm == null) return null;
            foreach (var o in gm.TeamFor(GameManager.Opponent(team)).onCourt)
            {
                if (o == null || o.Post == null) continue;
                if (o.Post.IsPosting && o.Post.EngagedDefender == this) return o.Post;
            }
            return null;
        }

        PlayerController FindOpenTeammate()
        {
            var gm = GameManager.Instance;
            if (gm == null) return null;
            PlayerController best = null;
            float bestOpen = -1f;
            foreach (var m in gm.TeamFor(team).onCourt)
            {
                if (m == null || m == this || !m.enabled) continue;
                var opp = m.NearestOpponentTo(m.transform.position);
                float open = opp != null ? HorizontalDistance(opp.transform.position, m.transform.position) : 99f;
                if (open > bestOpen) { bestOpen = open; best = m; }
            }
            return best;
        }

        static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f; b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
