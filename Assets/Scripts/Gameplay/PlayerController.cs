using UnityEngine;
using MarioBasketball.Core;
using MarioBasketball.Characters;
using MarioBasketball.InputControl;

namespace MarioBasketball.Gameplay
{
    /// <summary>
    /// A controllable player. Handles ground movement, jumping, scooping up a
    /// loose ball, shooting on an arc toward the attacking hoop, and a simple
    /// forward pass. Built on a <see cref="CharacterController"/> for crisp,
    /// arcade-style movement (NBA Street rather than sim).
    ///
    /// Movement speed and shot accuracy are derived from the attached
    /// <see cref="PlayerCharacter"/>'s effective stats, so Bowser (Speed 2,
    /// 3-Point 1) lumbers and bricks from deep while a guard would not. The
    /// many remaining mechanics (post-ups, tricks, fouling, on-fire streaks)
    /// are tracked in <c>docs/DESIGN.md</c> and not yet implemented here.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Team")]
        public TeamSide team = TeamSide.Home;

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
        public float threePointDistance = 7f;
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

        CharacterController _cc;
        PlayerCharacter _character;
        InputReader _input;
        float _verticalVelocity;

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _character = GetComponent<PlayerCharacter>();
        }

        void OnEnable()
        {
            _input = new InputReader();
            _input.ShootPressed += OnShoot;
            _input.PassPressed += OnPass;
            _input.JumpPressed += OnJump;
            _input.Enable();
        }

        void OnDisable()
        {
            if (_input == null) return;
            _input.ShootPressed -= OnShoot;
            _input.PassPressed -= OnPass;
            _input.JumpPressed -= OnJump;
            _input.Disable();
        }

        void Update()
        {
            _input.Tick();
            Move();
            TryPickUpLooseBall();
        }

        float Effective(StatType stat, float fallback) =>
            _character != null ? _character.GetEffective(stat) : fallback;

        void Move()
        {
            Vector2 m = _input.Move;
            Vector3 dir = new Vector3(m.x, 0f, m.y);
            if (dir.sqrMagnitude > 1f) dir.Normalize();

            // Effective Speed (1-10ish) maps onto the configured m/s band.
            float speedStat = Effective(StatType.Speed, 5f);
            float baseSpeed = Mathf.Lerp(minMoveSpeed, maxMoveSpeed, Mathf.Clamp01((speedStat - 1f) / 9f));
            bool sprinting = _input.SprintHeld && dir.sqrMagnitude > 0.01f;
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
        bool HasBall => Ball != null && Ball.Holder == this;

        void TryPickUpLooseBall()
        {
            var ball = Ball;
            if (ball == null || !ball.CanBePickedUp) return;
            if (Vector3.Distance(transform.position, ball.transform.position) <= pickupRadius)
                ball.PickUp(this);
        }

        void OnShoot()
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

        void OnPass()
        {
            if (!HasBall) return;
            Ball.Pass(transform.forward, passPower);
        }

        void OnJump()
        {
            if (_cc.isGrounded)
                _verticalVelocity = Mathf.Sqrt(-2f * gravity * jumpHeight);
        }
    }
}
