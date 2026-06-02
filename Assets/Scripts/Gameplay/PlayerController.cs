using UnityEngine;
using MarioBasketball.Core;
using MarioBasketball.Characters;
using MarioBasketball.InputControl;

namespace MarioBasketball.Gameplay
{
    /// <summary>
    /// A player's body and actions. Movement and actions are driven by an
    /// <i>intent</i> (a move vector plus shoot/pass/jump triggers) that comes
    /// from either the human (<see cref="isHuman"/>, via <see cref="InputReader"/>)
    /// or a <c>PlayerAI</c> brain. Built on a <see cref="CharacterController"/>
    /// for crisp, arcade-style movement.
    ///
    /// Movement speed and shot accuracy are derived from the attached
    /// <see cref="PlayerCharacter"/>'s effective stats, so Bowser lumbers and
    /// bricks from deep.
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
            _input.Enable();
        }

        void DisableInput()
        {
            if (_input == null) return;
            _input.ShootPressed -= TriggerShoot;
            _input.PassPressed -= TriggerPass;
            _input.JumpPressed -= TriggerJump;
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

        /// <summary>Face the attacking hoop and shoot, accuracy from the right stat.</summary>
        public void TriggerShoot()
        {
            if (!HasBall) return;
            Hoop hoop = GameManager.Instance.GetAttackingHoop(team);
            if (hoop == null) return;

            float distance = Vector3.Distance(transform.position, hoop.AimPoint);
            int points = distance >= threePointDistance ? 3 : 2;

            // Which scoring stat governs this look depends on where it's taken.
            StatType shotStat =
                distance >= threePointDistance ? StatType.ThreePoint :
                distance <= paintRadius ? StatType.InsideScoring :
                StatType.MidRange;

            // Higher effective stat → tighter spread → more makes.
            float stat = Effective(shotStat, 5f);
            float t = Mathf.Clamp01((stat - 1f) / 9f);
            float spread = Mathf.Lerp(maxShotSpread, minShotSpread, t);

            Ball.Shoot(hoop.AimPoint, team, points, shotFlightTime, spread);
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
    }
}
