using UnityEngine;
using UnityEngine.InputSystem;

namespace MarioBasketball.UI
{
    /// <summary>
    /// Shared controller/keyboard navigation for the IMGUI menus: d-pad / left
    /// stick / arrow keys to move, A (cross) to confirm, B (circle) / Tab as the
    /// menu's secondary action (back or card-flip, per menu). A menu component
    /// owns one, calls <see cref="Tick"/> once per Update, then reads the
    /// *ThisFrame flags. <see cref="DrawSelection"/> draws the flashing yellow
    /// outline around whatever is currently selected.
    /// </summary>
    public class MenuNav
    {
        /// <summary>This frame's navigation step: each axis is -1, 0 or +1
        /// (held directions auto-repeat).</summary>
        public Vector2Int Step { get; private set; }
        /// <summary>A / cross / Enter pressed this frame.</summary>
        public bool Submit { get; private set; }
        /// <summary>B / circle / Tab pressed this frame (back or flip, per menu).</summary>
        public bool East { get; private set; }
        /// <summary>Right-stick value this frame (Y &gt; 0 = pushed up). Menus use
        /// it for free scrolling of long lists.</summary>
        public Vector2 RightStick { get; private set; }

        const float RepeatDelay = 0.35f;
        const float RepeatRate = 0.15f;

        readonly InputAction _nav;
        readonly InputAction _submit;
        readonly InputAction _east;
        readonly InputAction _rightStick;
        Vector2Int _heldDir;
        float _repeatTimer;

        public MenuNav()
        {
            _nav = new InputAction("MenuNav", InputActionType.Value, expectedControlType: "Vector2");
            _nav.AddBinding("<Gamepad>/dpad");
            _nav.AddBinding("<Gamepad>/leftStick");
            _nav.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");

            _submit = new InputAction("MenuSubmit", InputActionType.Button, "<Gamepad>/buttonSouth");
            _submit.AddBinding("<Keyboard>/enter");

            _east = new InputAction("MenuEast", InputActionType.Button, "<Gamepad>/buttonEast");
            _east.AddBinding("<Keyboard>/tab");

            _rightStick = new InputAction("MenuScroll", InputActionType.Value, expectedControlType: "Vector2");
            _rightStick.AddBinding("<Gamepad>/rightStick");
        }

        public void Enable() { _nav.Enable(); _submit.Enable(); _east.Enable(); _rightStick.Enable(); }
        public void Disable() { _nav.Disable(); _submit.Disable(); _east.Disable(); _rightStick.Disable(); }

        /// <summary>Sample input. Call exactly once per frame (from Update —
        /// menus run with the game paused, so timing uses unscaled time).</summary>
        public void Tick()
        {
            Submit = _submit.WasPressedThisFrame();
            East = _east.WasPressedThisFrame();
            RightStick = _rightStick.ReadValue<Vector2>();

            Vector2 raw = _nav.ReadValue<Vector2>();
            var dir = new Vector2Int(
                raw.x > 0.5f ? 1 : raw.x < -0.5f ? -1 : 0,
                raw.y > 0.5f ? 1 : raw.y < -0.5f ? -1 : 0);

            if (dir == Vector2Int.zero)
            {
                Step = Vector2Int.zero;
                _heldDir = Vector2Int.zero;
            }
            else if (dir != _heldDir)
            {
                Step = dir; // fresh press
                _heldDir = dir;
                _repeatTimer = RepeatDelay;
            }
            else
            {
                _repeatTimer -= Time.unscaledDeltaTime;
                if (_repeatTimer <= 0f) { Step = dir; _repeatTimer = RepeatRate; }
                else Step = Vector2Int.zero;
            }
        }

        /// <summary>The flashing yellow outline marking the selected menu item —
        /// the controller cursor. A solid black border sits behind the yellow so
        /// it reads clearly against the bright menu backgrounds. Call from OnGUI.</summary>
        public static void DrawSelection(Rect r, float thickness = 3f)
        {
            float pulse = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 7f);
            var prev = GUI.color;
            // Black backing outline (a touch thicker so it frames the yellow).
            GUI.color = new Color(0f, 0f, 0f, Mathf.Lerp(0.65f, 0.95f, pulse));
            DrawBox(r, thickness * 2f);
            // Yellow cursor on top.
            GUI.color = new Color(1f, 0.88f, 0.1f, pulse);
            DrawBox(r, thickness);
            GUI.color = prev;
        }

        static void DrawBox(Rect r, float thickness)
        {
            GUI.DrawTexture(new Rect(r.x - thickness, r.y - thickness, r.width + thickness * 2f, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x - thickness, r.yMax, r.width + thickness * 2f, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x - thickness, r.y, thickness, r.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.xMax, r.y, thickness, r.height), Texture2D.whiteTexture);
        }
    }
}
