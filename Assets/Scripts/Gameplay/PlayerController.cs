using UnityEngine;
using MarioBasketball.Core;
using MarioBasketball.InputControl;

namespace MarioBasketball.Gameplay
{
    /// <summary>
    /// A controllable player. Handles ground movement, jumping, scooping up a
    /// loose ball, shooting on an arc toward the attacking hoop, and a simple
    /// forward pass. Built on a <see cref="CharacterController"/> for crisp,
    /// arcade-style movement (NBA Street rather than sim).
    ///
    /// The initial core loop spawns a single human player; the AI/teammate
    /// hooks are deliberately left out so this class stays readable.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Team")]
        public TeamSide team = TeamSide.Home;

        [Header("Movement")]
        public float moveSpeed = 7f;
        public float sprintMultiplier = 1.5f;
        public float turnSpeed = 720f;
        public float gravity = -25f;
        public float jumpHeight = 1.4f;

        [Header("Ball handling")]
        [Tooltip("How close a loose ball must be to scoop it up, in metres.")]
        public float pickupRadius = 1.2f;
        [Tooltip("Distance from the basket beyond which a make is worth 3.")]
        public float threePointDistance = 7f;

        [Header("Shooting")]
        public float shotFlightTime = 1.1f;
        [Tooltip("Horizontal miss spread (metres) at the rim. 0 = always nylon.")]
        public float shotSpread = 0.35f;
        public float passPower = 9f;

        /// <summary>Where the carried ball sits — out in front, hip height.</summary>
        public Vector3 BallHoldPoint => transform.position + transform.forward * 0.55f + Vector3.up * 0.4f;

        CharacterController _cc;
        InputReader _input;
        float _verticalVelocity;

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
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

        void Move()
        {
            Vector2 m = _input.Move;
            Vector3 dir = new Vector3(m.x, 0f, m.y);
            if (dir.sqrMagnitude > 1f) dir.Normalize();

            float speed = moveSpeed * (_input.SprintHeld ? sprintMultiplier : 1f);
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
            // Long-range jumpers are a little less reliable.
            float spread = shotSpread * Mathf.Clamp01(distance / (threePointDistance * 1.5f));

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
