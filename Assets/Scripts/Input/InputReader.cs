using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MarioBasketball.InputControl
{
    /// <summary>
    /// A single player's controls, built on Unity's <b>new Input System</b>.
    ///
    /// The action set is defined here in code rather than loaded from an
    /// <c>.inputactions</c> asset so the core loop is playable the moment you
    /// press Play — no manual asset wiring required. An editable asset version
    /// lives at <c>Assets/Settings/Controls.inputactions</c> for when you want
    /// rebinding and split-screen device assignment later on.
    ///
    /// This is a plain C# class (not a MonoBehaviour); a <c>PlayerController</c>
    /// creates one, calls <see cref="Enable"/>, and reads it each frame.
    /// </summary>
    public class InputReader
    {
        public Vector2 Move { get; private set; }
        public bool SprintHeld { get; private set; }

        public event Action ShootPressed;
        public event Action PassPressed;
        public event Action JumpPressed;
        public event Action StealPressed;

        readonly InputAction _move;
        readonly InputAction _sprint;
        readonly InputAction _shoot;
        readonly InputAction _pass;
        readonly InputAction _jump;
        readonly InputAction _steal;

        public InputReader()
        {
            _move = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            _move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            _move.AddBinding("<Gamepad>/leftStick");

            _sprint = new InputAction("Sprint", InputActionType.Button, "<Keyboard>/leftShift");
            _sprint.AddBinding("<Gamepad>/leftTrigger");

            _shoot = new InputAction("Shoot", InputActionType.Button, "<Keyboard>/space");
            _shoot.AddBinding("<Gamepad>/buttonSouth");

            _pass = new InputAction("Pass", InputActionType.Button, "<Keyboard>/e");
            _pass.AddBinding("<Gamepad>/buttonWest");

            _jump = new InputAction("Jump", InputActionType.Button, "<Keyboard>/leftCtrl");
            _jump.AddBinding("<Gamepad>/buttonNorth");

            _steal = new InputAction("Steal", InputActionType.Button, "<Keyboard>/f");
            _steal.AddBinding("<Gamepad>/buttonEast");

            _shoot.performed += _ => ShootPressed?.Invoke();
            _pass.performed += _ => PassPressed?.Invoke();
            _jump.performed += _ => JumpPressed?.Invoke();
            _steal.performed += _ => StealPressed?.Invoke();
        }

        public void Enable()
        {
            _move.Enable();
            _sprint.Enable();
            _shoot.Enable();
            _pass.Enable();
            _jump.Enable();
            _steal.Enable();
        }

        public void Disable()
        {
            _move.Disable();
            _sprint.Disable();
            _shoot.Disable();
            _pass.Disable();
            _jump.Disable();
            _steal.Disable();
        }

        /// <summary>Sample the continuously-read values. Call once per frame.</summary>
        public void Tick()
        {
            Move = _move.ReadValue<Vector2>();
            SprintHeld = _sprint.IsPressed();
        }
    }
}
