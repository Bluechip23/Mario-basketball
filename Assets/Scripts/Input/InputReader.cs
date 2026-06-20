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
        public Vector2 PassAim { get; private set; }
        public bool SprintHeld { get; private set; }
        public bool PostUpHeld { get; private set; }
        /// <summary>LB held — bring up teammate pass icons (passing only).</summary>
        public bool IconHeld { get; private set; }

        /// <summary>The right stick was flicked (pushed hard and released within
        /// <see cref="flickMaxHold"/>) — the ball-handler's hard-dribble gesture.
        /// Carries the flick direction (stick space). A stick that's pushed and
        /// held is a pass aim, not a flick.</summary>
        public event Action<Vector2> DribbleFlick;

        /// <summary>Turbo (sprint) tapped twice in quick succession — Delfan's
        /// "called shot" gesture while a shot is in the air.</summary>
        public event Action TurboDoubleTap;

        public event Action ShootPressed;
        public event Action ShootReleased;
        public event Action PassPressed;
        public event Action PassReleased;
        public event Action JumpPressed;
        public event Action StealPressed;
        public event Action DivePressed;
        public event Action BackDownPressed;
        public event Action HookPressed;
        public event Action DropStepPressed;
        public event Action SpinPressed;
        public event Action FakePressed;

        readonly InputAction _move;
        readonly InputAction _passAim;
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
        readonly InputAction _iconPass;

        public InputReader()
        {
            _move = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            _move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            _move.AddBinding("<Gamepad>/leftStick");

            // Pass aim: right stick (or IJKL) points at a teammate to direct a pass.
            _passAim = new InputAction("PassAim", InputActionType.Value, expectedControlType: "Vector2");
            _passAim.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/i")
                .With("Down", "<Keyboard>/k")
                .With("Left", "<Keyboard>/j")
                .With("Right", "<Keyboard>/l");
            _passAim.AddBinding("<Gamepad>/rightStick");

            _sprint = new InputAction("Sprint", InputActionType.Button, "<Keyboard>/leftShift");
            _sprint.AddBinding("<Gamepad>/leftTrigger");

            _postUp = new InputAction("PostUp", InputActionType.Button, "<Keyboard>/r");
            _postUp.AddBinding("<Gamepad>/rightShoulder");

            _shoot = new InputAction("Shoot", InputActionType.Button, "<Keyboard>/space");
            _shoot.AddBinding("<Gamepad>/buttonWest"); // X

            _pass = new InputAction("Pass", InputActionType.Button, "<Keyboard>/e");
            _pass.AddBinding("<Gamepad>/buttonSouth"); // A

            _jump = new InputAction("Jump", InputActionType.Button, "<Keyboard>/leftCtrl");
            _jump.AddBinding("<Gamepad>/buttonNorth");

            _steal = new InputAction("Steal", InputActionType.Button, "<Keyboard>/f");
            _steal.AddBinding("<Gamepad>/buttonWest"); // X — shares the shoot button; only acts on defense

            _dive = new InputAction("Dive", InputActionType.Button, "<Keyboard>/x");
            _dive.AddBinding("<Gamepad>/buttonEast"); // B (when not posting)

            _backDown = new InputAction("BackDown", InputActionType.Button, "<Keyboard>/b");
            _backDown.AddBinding("<Gamepad>/rightTrigger"); // RT

            // Post moves map to the face buttons while posting (contextual).
            _hook = new InputAction("Hook", InputActionType.Button, "<Keyboard>/h");
            _hook.AddBinding("<Gamepad>/buttonNorth"); // Y

            _dropStep = new InputAction("DropStep", InputActionType.Button, "<Keyboard>/g");
            _dropStep.AddBinding("<Gamepad>/buttonSouth"); // A

            // Spin: keyboard V. On the gamepad it's a quick Left-Trigger tap in the
            // post (detected in PlayerController) so the trigger stays free to HOLD
            // as the advanced-move (turbo) modifier the rest of the time.
            _spin = new InputAction("Spin", InputActionType.Button, "<Keyboard>/v");

            _fake = new InputAction("Fake", InputActionType.Button, "<Keyboard>/t");
            _fake.AddBinding("<Gamepad>/buttonEast"); // B (post fake)

            // Icon-pass modifier: LB (C). Hold it and tap a face button to pass to
            // that teammate. This is now LB's ONLY job — no more sharing it with the
            // post fake or the finish air-adjust.
            _iconPass = new InputAction("IconPass", InputActionType.Button, "<Keyboard>/c");
            _iconPass.AddBinding("<Gamepad>/leftShoulder"); // LB

            _shoot.performed += _ => ShootPressed?.Invoke();
            _shoot.canceled += _ => ShootReleased?.Invoke();
            _pass.performed += _ => PassPressed?.Invoke();
            _pass.canceled += _ => PassReleased?.Invoke();
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
            _move.Enable(); _passAim.Enable(); _sprint.Enable(); _postUp.Enable();
            _shoot.Enable(); _pass.Enable(); _jump.Enable(); _steal.Enable(); _dive.Enable();
            _backDown.Enable(); _hook.Enable(); _dropStep.Enable(); _spin.Enable(); _fake.Enable();
            _iconPass.Enable();
        }

        public void Disable()
        {
            _move.Disable(); _passAim.Disable(); _sprint.Disable(); _postUp.Disable();
            _shoot.Disable(); _pass.Disable(); _jump.Disable(); _steal.Disable(); _dive.Disable();
            _backDown.Disable(); _hook.Disable(); _dropStep.Disable(); _spin.Disable(); _fake.Disable();
            _iconPass.Disable();
        }

        /// <summary>Sample the continuously-read values. Call once per frame.</summary>
        public void Tick()
        {
            Move = _move.ReadValue<Vector2>();
            PassAim = _passAim.ReadValue<Vector2>();
            SprintHeld = _sprint.IsPressed();
            PostUpHeld = _postUp.IsPressed();
            IconHeld = _iconPass.IsPressed(); // LB / C held = pass-icon modifier
            DetectFlick();
            DetectTurboDoubleTap();
        }

        /// <summary>Max seconds between the two turbo taps to count as a double-tap.</summary>
        public float turboDoubleTapWindow = 0.3f;
        bool _prevSprint;
        float _lastSprintPressTime = -10f;

        // Two quick presses of turbo fire TurboDoubleTap (the called-shot gesture).
        void DetectTurboDoubleTap()
        {
            if (SprintHeld && !_prevSprint) // a fresh turbo press
            {
                if (Time.unscaledTime - _lastSprintPressTime <= turboDoubleTapWindow)
                {
                    TurboDoubleTap?.Invoke();
                    _lastSprintPressTime = -10f; // consume, so a third tap doesn't re-fire
                }
                else _lastSprintPressTime = Time.unscaledTime;
            }
            _prevSprint = SprintHeld;
        }

        /// <summary>Stick magnitude that arms a flick.</summary>
        public float flickThreshold = 0.8f;
        /// <summary>Magnitude the stick must fall back below to fire the flick.</summary>
        public float flickRelease = 0.35f;
        /// <summary>Armed longer than this and it's a held pass aim, not a flick.</summary>
        public float flickMaxHold = 0.22f;

        bool _flickArmed;
        float _flickArmTime;
        Vector2 _flickPeak;
        float _prevAimMag;

        // A flick is a sharp push past flickThreshold that returns to neutral
        // within flickMaxHold — distinct from the held right-stick pass aim.
        void DetectFlick()
        {
            float mag = PassAim.magnitude;

            if (!_flickArmed)
            {
                if (mag >= flickThreshold && _prevAimMag < flickThreshold)
                {
                    _flickArmed = true;
                    _flickArmTime = Time.unscaledTime;
                    _flickPeak = PassAim;
                }
            }
            else
            {
                if (mag > _flickPeak.magnitude) _flickPeak = PassAim;

                if (mag <= flickRelease)
                {
                    _flickArmed = false;
                    DribbleFlick?.Invoke(_flickPeak.normalized);
                }
                else if (Time.unscaledTime - _flickArmTime > flickMaxHold)
                {
                    _flickArmed = false; // held — it's an aim, not a flick
                }
            }

            _prevAimMag = mag;
        }
    }
}
