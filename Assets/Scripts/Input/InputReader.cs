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
    /// press Play. An editable asset version lives at
    /// <c>Assets/Settings/Controls.inputactions</c> for richer rebinding later.
    ///
    /// This is a plain C# class (not a MonoBehaviour); a <c>PlayerController</c>
    /// creates one, calls <see cref="Enable"/>, and reads it each frame.
    /// </summary>
    public class InputReader
    {
        public Vector2 Move { get; private set; }
        public bool SprintHeld { get; private set; }
        public bool PostUpHeld { get; private set; }

        public event Action ShootPressed;
        public event Action PassPressed;
        public event Action JumpPressed;
        public event Action StealPressed;
        public event Action DivePressed;
        public event Action BackDownPressed;
        public event Action HookPressed;
        public event Action DropStepPressed;
        public event Action SpinPressed;
        public event Action FakePressed;

        readonly InputAction _move;
        readonly InputAction _sprint;
        readonly InputAction _postUp;
        readonly InputAction _shoot;
        readonly InputAction _pass;
        readonly InputAction _jump;
        readonly InputAction _steal;
        readonly InputAction _dive;
        readonly InputAction _backDown;
        readonly InputAction _hook;
        readonly InputAction _dropStep;
        readonly InputAction _spin;
        readonly InputAction _fake;

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

            _postUp = new InputAction("PostUp", InputActionType.Button, "<Keyboard>/r");
            _postUp.AddBinding("<Gamepad>/rightShoulder");

            _shoot = new InputAction("Shoot", InputActionType.Button, "<Keyboard>/space");
            _shoot.AddBinding("<Gamepad>/buttonSouth");

            _pass = new InputAction("Pass", InputActionType.Button, "<Keyboard>/e");
            _pass.AddBinding("<Gamepad>/buttonWest");

            _jump = new InputAction("Jump", InputActionType.Button, "<Keyboard>/leftCtrl");
            _jump.AddBinding("<Gamepad>/buttonNorth");

            _steal = new InputAction("Steal", InputActionType.Button, "<Keyboard>/f");
            _steal.AddBinding("<Gamepad>/buttonEast");

            _dive = new InputAction("Dive", InputActionType.Button, "<Keyboard>/x");
            _dive.AddBinding("<Gamepad>/rightStickPress");

            _backDown = new InputAction("BackDown", InputActionType.Button, "<Keyboard>/b");
            _backDown.AddBinding("<Gamepad>/rightTrigger");

            _hook = new InputAction("Hook", InputActionType.Button, "<Keyboard>/h");
            _hook.AddBinding("<Gamepad>/dpad/up");

            _dropStep = new InputAction("DropStep", InputActionType.Button, "<Keyboard>/g");
            _dropStep.AddBinding("<Gamepad>/dpad/down");

            _spin = new InputAction("Spin", InputActionType.Button, "<Keyboard>/v");
            _spin.AddBinding("<Gamepad>/dpad/left");

            _fake = new InputAction("Fake", InputActionType.Button, "<Keyboard>/c");
            _fake.AddBinding("<Gamepad>/dpad/right");

            _shoot.performed += _ => ShootPressed?.Invoke();
            _pass.performed += _ => PassPressed?.Invoke();
            _jump.performed += _ => JumpPressed?.Invoke();
            _steal.performed += _ => StealPressed?.Invoke();
            _dive.performed += _ => DivePressed?.Invoke();
            _backDown.performed += _ => BackDownPressed?.Invoke();
            _hook.performed += _ => HookPressed?.Invoke();
            _dropStep.performed += _ => DropStepPressed?.Invoke();
            _spin.performed += _ => SpinPressed?.Invoke();
            _fake.performed += _ => FakePressed?.Invoke();
        }

        public void Enable()
        {
            _move.Enable(); _sprint.Enable(); _postUp.Enable();
            _shoot.Enable(); _pass.Enable(); _jump.Enable(); _steal.Enable(); _dive.Enable();
            _backDown.Enable(); _hook.Enable(); _dropStep.Enable(); _spin.Enable(); _fake.Enable();
        }

        public void Disable()
        {
            _move.Disable(); _sprint.Disable(); _postUp.Disable();
            _shoot.Disable(); _pass.Disable(); _jump.Disable(); _steal.Disable(); _dive.Disable();
            _backDown.Disable(); _hook.Disable(); _dropStep.Disable(); _spin.Disable(); _fake.Disable();
        }

        /// <summary>Sample the continuously-read values. Call once per frame.</summary>
        public void Tick()
        {
            Move = _move.ReadValue<Vector2>();
            SprintHeld = _sprint.IsPressed();
            PostUpHeld = _postUp.IsPressed();
        }
    }
}
