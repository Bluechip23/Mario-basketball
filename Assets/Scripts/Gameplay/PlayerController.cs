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
        public float pickupRadius = 1.2f;
        [Tooltip("Distance from the basket beyond which a make is worth 3.")]
        public float threePointDistance = 6.75f;
        [Tooltip("Within this radius a shot uses Inside Scoring, not Mid Range.")]
        public float paintRadius = 2.5f;

        [Header("Shooting (accuracy mapped from the relevant scoring stat)")]
        public float shotFlightTime = 1.1f;
        public float maxShotSpread = 1.2f;
        public float minShotSpread = 0.05f;
        public float passPower = 9f;
        [Tooltip("Extra chance an on-fire shot just goes in (after the block check).")]
        [Range(0f, 1f)] public float onFireMakeBonus = 0.30f;

        [Header("Contest / block (defense on a shot)")]
        public float contestRange = 3f;
        public float contestMaxSpread = 1.6f;
        public float blockRange = 1.1f;
        public float blockBaseChance = 0.04f;
        public float blockStatScale = 0.05f;
        public float blockMaxChance = 0.5f;
        public float blockKnockPower = 4f;

        [Header("Steal (Steals vs Ball Handling)")]
        public float stealReach = 1.15f;
        public float stealCooldown = 1.5f;
        public float stealWhiffCooldown = 0.4f;
        public float stealBaseChance = 0.04f;
        public float stealStatScale = 0.035f;
        public float stealMinChance = 0.02f;
        public float stealMaxChance = 0.4f;

        [Header("Dive / shove")]
        public float diveDuration = 0.5f;
        public float diveSpeed = 9f;
        public float diveReachBonus = 1.0f;
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

        public Vector3 BallHoldPoint => transform.position + transform.forward * 0.55f + Vector3.up * 0.4f;

        public PlayerCharacter Character => _character;
        public PostUpController Post => _post;
        public bool HasBall => Ball != null && Ball.Holder == this;
        public bool IsPosting => _post != null && _post.IsPosting;
        public bool IsStunned => _stunTimer > 0f;

        CharacterController _cc;
        PlayerCharacter _character;
        PostUpController _post;
        InputReader _input;
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

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _character = GetComponent<PlayerCharacter>();
            _post = GetComponent<PostUpController>();
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
                if (IsPosting) _post.End();
            }
        }

        void EnableInput()
        {
            if (_input != null) return;
            _input = new InputReader();
            _input.ShootPressed += TriggerShoot;
            _input.PassPressed += TriggerPass;
            _input.JumpPressed += TriggerJump;
            _input.StealPressed += TriggerSteal;
            _input.DivePressed += TriggerDive;
            _input.BackDownPressed += TriggerBackDown;
            _input.HookPressed += TriggerHook;
            _input.DropStepPressed += TriggerDropStep;
            _input.SpinPressed += TriggerSpin;
            _input.FakePressed += TriggerFake;
            _input.Enable();
        }

        void DisableInput()
        {
            if (_input == null) return;
            _input.ShootPressed -= TriggerShoot;
            _input.PassPressed -= TriggerPass;
            _input.JumpPressed -= TriggerJump;
            _input.StealPressed -= TriggerSteal;
            _input.DivePressed -= TriggerDive;
            _input.BackDownPressed -= TriggerBackDown;
            _input.HookPressed -= TriggerHook;
            _input.DropStepPressed -= TriggerDropStep;
            _input.SpinPressed -= TriggerSpin;
            _input.FakePressed -= TriggerFake;
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
            if (_diveTimer > 0f) _diveTimer -= dt;
            if (_shoveTimer > 0f) _shoveTimer -= dt;
            if (_pushCooldown > 0f) _pushCooldown -= dt;

            if (isHuman && _input != null)
            {
                _input.Tick();
                _moveIntent = _input.Move;
                _sprintIntent = _input.SprintHeld;
                HandlePostHold();
            }

            Move();
            TryPickUpLooseBall();
        }

        void HandlePostHold()
        {
            bool wantPost = _input.PostUpHeld && HasBall && !IsStunned && _cc.isGrounded;
            if (wantPost && !IsPosting) _post.Begin(NearestOpponentTo(transform.position));
            else if (!_input.PostUpHeld && IsPosting) _post.End();
        }

        public void Teleport(Vector3 position)
        {
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
            else
            {
                Vector3 dir = new Vector3(_moveIntent.x, 0f, _moveIntent.y);
                if (dir.sqrMagnitude > 1f) dir.Normalize();

                float speedStat = Effective(StatType.Speed, 5f);
                float baseSpeed = Mathf.Lerp(minMoveSpeed, maxMoveSpeed, Mathf.Clamp01((speedStat - 1f) / 9f));
                bool sprinting = _sprintIntent && dir.sqrMagnitude > 0.01f;
                float speed = baseSpeed * (sprinting ? sprintMultiplier : 1f);
                _character?.ReportActivity(dir.sqrMagnitude > 0.01f, sprinting);

                horizontal = dir * speed;
                rotateToMove = dir.sqrMagnitude > 0.01f;
                faceDir = dir;
            }

            if (_shoveTimer > 0f) horizontal += _shoveVel;

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

        BallController Ball => GameManager.Instance != null ? GameManager.Instance.ball : null;

        void TryPickUpLooseBall()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;
            var ball = Ball;
            if (ball == null || !ball.CanBePickedUpBy(this)) return;
            float reach = _diveTimer > 0f ? pickupRadius + diveReachBonus : pickupRadius;
            if (Vector3.Distance(transform.position, ball.transform.position) <= reach)
            {
                ball.PickUp(this);
                GameManager.Instance.OnPossessionGained(this);
            }
        }

        // ---- Actions (input events or AI brain) ----------------------------

        public void TriggerShoot()
        {
            if (MatchPause.IsPaused || IsStunned || IsPosting || !HasBall) return;
            Hoop hoop = GameManager.Instance.GetAttackingHoop(team);
            if (hoop == null) return;

            Vector3 aim = hoop.AimPoint;
            float distance = HorizontalDistance(transform.position, aim);
            int points = distance >= threePointDistance ? 3 : 2;

            StatType shotStat =
                distance >= threePointDistance ? StatType.ThreePoint :
                distance <= paintRadius ? StatType.InsideScoring :
                StatType.MidRange;

            float stat = Effective(shotStat, 5f);
            float t = Mathf.Clamp01((stat - 1f) / 9f);
            float spread = Mathf.Lerp(maxShotSpread, minShotSpread, t);

            PlayerController defender = NearestOpponentTo(transform.position);
            if (defender != null)
            {
                float dd = HorizontalDistance(defender.transform.position, transform.position);
                if (dd < contestRange)
                {
                    float closeness = 1f - dd / contestRange;
                    bool outside = shotStat == StatType.ThreePoint || shotStat == StatType.MidRange;
                    float defStat = defender.EffectiveStat(outside ? StatType.PerimeterDefense : StatType.PostDefense);
                    spread += contestMaxSpread * closeness * Mathf.Clamp01(defStat / 10f);

                    if (dd < blockRange)
                    {
                        float blk = defender.EffectiveStat(StatType.Blocks);
                        float chance = Mathf.Clamp(blockBaseChance + blockStatScale * (blk - stat), 0f, blockMaxChance) * closeness;
                        if (Random.value < chance)
                        {
                            Vector3 away = transform.position - aim; away.y = 0f;
                            Ball.Pass(away.sqrMagnitude > 0.01f ? away : -transform.forward, blockKnockPower);
                            GameManager.Instance.OnShotMissed(this); // blocked → streak broken
                            return;
                        }
                    }
                }
            }

            // On fire: a flat extra chance the ball just drops (block already resolved).
            if (_character != null && _character.OnFire && Random.value < onFireMakeBonus)
                spread = Mathf.Min(spread, minShotSpread);

            Ball.Shoot(aim, team, points, shotFlightTime, spread, this);
        }

        public void TriggerPass()
        {
            if (MatchPause.IsPaused || IsStunned || !HasBall) return;
            if (IsPosting) _post.End(); // kick out of the post
            // Pass to the most open teammate (a blind outlet if nobody's open).
            var mate = FindOpenTeammate();
            if (mate != null) Ball.PassTo(mate.transform.position + Vector3.up * 0.6f);
            else Ball.Pass(transform.forward, passPower);
        }

        /// <summary>A directed pass to a teammate (used by the AI).</summary>
        public void PassToward(Vector3 worldPoint)
        {
            if (MatchPause.IsPaused || IsStunned || !HasBall) return;
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
            }
        }

        /// <summary>Lunge toward a nearby loose ball with extended reach.</summary>
        public void TriggerDive()
        {
            if (MatchPause.IsPaused || IsStunned || IsPosting || _diveTimer > 0f || !_cc.isGrounded) return;

            _diveDir = transform.forward;
            var ball = Ball;
            if (ball != null && ball.CanBePickedUpBy(this))
            {
                Vector3 to = ball.transform.position - transform.position; to.y = 0f;
                if (to.sqrMagnitude > 0.01f && to.magnitude <= diveBallSeekRange) _diveDir = to.normalized;
            }
            _diveTimer = diveDuration;
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

        void TriggerHook() => TriggerPostMove(PostMove.Hook);
        void TriggerDropStep() => TriggerPostMove(PostMove.DropStep);
        void TriggerSpin() => TriggerPostMove(PostMove.Spin);
        void TriggerFake() => TriggerPostMove(PostMove.Fake);

        public void TriggerPostMove(PostMove move)
        {
            if (MatchPause.IsPaused || IsStunned || !IsPosting) return;
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

        public void Stun(float seconds)
        {
            _stunTimer = Mathf.Max(_stunTimer, seconds);
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
