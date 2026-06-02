using UnityEngine;
using MarioBasketball.Core;
using MarioBasketball.Characters;
using MarioBasketball.InputControl;

namespace MarioBasketball.Gameplay
{
    /// <summary>
    /// A player's body and actions. Movement and actions are driven by an
    /// <i>intent</i> (a move vector plus shoot/pass/jump/steal triggers) fed by
    /// either the human (<see cref="InputReader"/>) or a <c>PlayerAI</c> brain.
    ///
    /// Outcomes are stat-driven: move speed (Speed) and shot accuracy (3-Point /
    /// Mid Range / Inside Scoring by distance) scale with effective stats, and
    /// shots are <b>contested</b> by nearby defenders (Perimeter/Post Defense
    /// widen the miss, Blocks can swat point-blank attempts). Stealing pits the
    /// thief's Steals against the handler's Ball Handling.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Identity")]
        public TeamSide team = TeamSide.Home;
        [Tooltip("The human-controlled player reads input; others are AI-driven.")]
        public bool isHuman = false;

        [Header("Movement (mapped from the Speed stat)")]
        [Tooltip("Move speed at effective Speed 1 / Speed 10, in m/s.")]
        public float minMoveSpeed = 4f;
        public float maxMoveSpeed = 9f;
        public float sprintMultiplier = 1.4f;
        public float turnSpeed = 720f;
        public float gravity = -25f;
        public float jumpHeight = 1.4f;

        [Header("Ball handling")]
        [Tooltip("How close a loose ball must be to scoop it up, in metres.")]
        public float pickupRadius = 1.2f;
        [Tooltip("Distance from the basket beyond which a make is worth 3.")]
        public float threePointDistance = 6.75f;
        [Tooltip("Within this radius a shot uses Inside Scoring, not Mid Range.")]
        public float paintRadius = 2.5f;

        [Header("Shooting (accuracy mapped from the relevant scoring stat)")]
        public float shotFlightTime = 1.1f;
        [Tooltip("Rim miss spread (metres) at stat 1 / stat 10. Lower stat = wilder.")]
        public float maxShotSpread = 1.2f;
        public float minShotSpread = 0.05f;
        public float passPower = 9f;

        [Header("Contest / block (defense on a shot)")]
        [Tooltip("A defender within this range contests the shot.")]
        public float contestRange = 3f;
        [Tooltip("Extra miss spread added by a point-blank contest from a great defender.")]
        public float contestMaxSpread = 1.6f;
        [Tooltip("A defender within this range can block.")]
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

        /// <summary>Where the carried ball sits — out in front, hip height.</summary>
        public Vector3 BallHoldPoint => transform.position + transform.forward * 0.55f + Vector3.up * 0.4f;

        public PlayerCharacter Character => _character;
        public bool HasBall => Ball != null && Ball.Holder == this;

        CharacterController _cc;
        PlayerCharacter _character;
        InputReader _input;
        float _verticalVelocity;
        Vector2 _moveIntent;
        bool _sprintIntent;
        float _stealCooldown;

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _character = GetComponent<PlayerCharacter>();
        }

        void OnEnable()
        {
            if (isHuman) EnableInput();
        }

        void OnDisable()
        {
            DisableInput();
        }

        /// <summary>Hand control of this player to / from the human. The switch
        /// manager calls this; exactly one player is human-controlled at a time
        /// so only one <see cref="InputReader"/> is ever active.</summary>
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
            _input.Enable();
        }

        void DisableInput()
        {
            if (_input == null) return;
            _input.ShootPressed -= TriggerShoot;
            _input.PassPressed -= TriggerPass;
            _input.JumpPressed -= TriggerJump;
            _input.StealPressed -= TriggerSteal;
            _input.Disable();
            _input = null;
        }

        /// <summary>Set this frame's desired movement. Used by the AI brain;
        /// the human overrides it from input each frame.</summary>
        public void SetMoveIntent(Vector2 move, bool sprint)
        {
            _moveIntent = move;
            _sprintIntent = sprint;
        }

        void Update()
        {
            if (_stealCooldown > 0f) _stealCooldown -= Time.deltaTime;

            if (isHuman && _input != null)
            {
                _input.Tick();
                _moveIntent = _input.Move;
                _sprintIntent = _input.SprintHeld;
            }
            Move();
            TryPickUpLooseBall();
        }

        /// <summary>
        /// Move the player, working around the CharacterController (which
        /// otherwise overrides direct transform writes). Used for inbounds,
        /// tip-offs and substitutions.
        /// </summary>
        public void Teleport(Vector3 position)
        {
            bool was = _cc.enabled;
            _cc.enabled = false;
            transform.position = position;
            _cc.enabled = was;
        }

        float Effective(StatType stat, float fallback) =>
            _character != null ? _character.GetEffective(stat) : fallback;

        /// <summary>Public effective-stat read for other systems (contest, steal, AI).</summary>
        public float EffectiveStat(StatType stat) => Effective(stat, 5f);

        void Move()
        {
            Vector3 dir = new Vector3(_moveIntent.x, 0f, _moveIntent.y);
            if (dir.sqrMagnitude > 1f) dir.Normalize();

            // Effective Speed (1-10ish) maps onto the configured m/s band.
            float speedStat = Effective(StatType.Speed, 5f);
            float baseSpeed = Mathf.Lerp(minMoveSpeed, maxMoveSpeed, Mathf.Clamp01((speedStat - 1f) / 9f));
            bool sprinting = _sprintIntent && dir.sqrMagnitude > 0.01f;
            float speed = baseSpeed * (sprinting ? sprintMultiplier : 1f);

            if (_character != null)
                _character.ReportActivity(dir.sqrMagnitude > 0.01f, sprinting);

            Vector3 horizontal = dir * speed;

            if (_cc.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f; // keep the controller snug to the floor
            _verticalVelocity += gravity * Time.deltaTime;

            Vector3 velocity = horizontal + Vector3.up * _verticalVelocity;
            _cc.Move(velocity * Time.deltaTime);

            if (dir.sqrMagnitude > 0.01f)
            {
                Quaternion want = Quaternion.LookRotation(dir, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, want, turnSpeed * Time.deltaTime);
            }
        }

        BallController Ball => GameManager.Instance != null ? GameManager.Instance.ball : null;

        void TryPickUpLooseBall()
        {
            // Only contest live balls — not during inbounds/stoppages.
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;
            var ball = Ball;
            if (ball == null || !ball.CanBePickedUpBy(this)) return;
            if (Vector3.Distance(transform.position, ball.transform.position) <= pickupRadius)
            {
                ball.PickUp(this);
                GameManager.Instance.OnPossessionGained(this);
            }
        }

        // ---- Actions (called by input events or the AI brain) --------------

        /// <summary>Shoot at the attacking hoop. Accuracy comes from the right
        /// scoring stat, then nearby defenders contest (and may block).</summary>
        public void TriggerShoot()
        {
            if (!HasBall) return;
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

            // Defensive contest.
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
                            // Swatted: the ball is knocked loose away from the rim.
                            Vector3 away = transform.position - aim; away.y = 0f;
                            Ball.Pass(away.sqrMagnitude > 0.01f ? away : -transform.forward, blockKnockPower);
                            return;
                        }
                    }
                }
            }

            Ball.Shoot(aim, team, points, shotFlightTime, spread);
        }

        public void TriggerPass()
        {
            if (!HasBall) return;
            Ball.Pass(transform.forward, passPower);
        }

        /// <summary>A directed pass to a teammate (used by the AI).</summary>
        public void PassToward(Vector3 worldPoint)
        {
            if (!HasBall) return;
            Ball.PassTo(worldPoint);
        }

        public void TriggerJump()
        {
            if (_cc.isGrounded)
                _verticalVelocity = Mathf.Sqrt(-2f * gravity * jumpHeight);
        }

        /// <summary>Attempt to strip the ball from a nearby opponent ball
        /// handler — Steals vs their Ball Handling, on a cooldown.</summary>
        public void TriggerSteal()
        {
            if (_stealCooldown > 0f) return;
            var gm = GameManager.Instance;
            if (gm == null || gm.ball == null) return;

            var holder = gm.ball.Holder;
            if (holder == null || holder == this || holder.team == team) return;

            float dist = HorizontalDistance(transform.position, holder.transform.position);
            if (dist > stealReach)
            {
                _stealCooldown = stealWhiffCooldown;
                return;
            }

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

        // ---- Helpers -------------------------------------------------------

        PlayerController NearestOpponentTo(Vector3 point)
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

        static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f; b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
