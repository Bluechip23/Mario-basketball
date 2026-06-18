using UnityEngine;
using MarioBasketball.Core;
using MarioBasketball.Gameplay;

namespace MarioBasketball.CameraControl
{
    /// <summary>
    /// NBA-Street-style chase camera: it trails <i>behind</i> the action (the
    /// ball), sitting low and close so players read big on screen, and looks
    /// down-court toward the hoop the team in possession is attacking. You see
    /// the offense driving away from you toward the basket. On a change of
    /// possession the "down-court" direction flips and the camera swings around
    /// to trail the other way.
    ///
    /// "Forward" is the court's long axis (between the two hoops), not the raw
    /// ball→hoop line, so the camera looks straight down the floor and stays
    /// steady instead of pivoting every time the ball swings to a wing.
    ///
    /// Movement input is camera-relative (see <c>PlayerController.CameraRelative</c>),
    /// so "up the stick" stays "up-court" no matter which way the camera faces.
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        [Tooltip("What the camera tracks — the ball once a match starts.")]
        public Transform target;

        [Header("Chase framing")]
        [Tooltip("How far behind the ball (m) the camera trails down-court. Bigger = more zoomed out.")]
        public float distanceBehind = 8.5f;
        [Tooltip("Camera height above the floor (m). Low = flatter, in-the-action angle.")]
        public float height = 3.4f;
        [Tooltip("Sideways shift (m) off the straight-behind line, for a slight over-the-shoulder 3/4 look. 0 = dead centre.")]
        public float lateralOffset = 1.5f;
        [Tooltip("How far ahead of the ball (m, down-court) the camera aims, so the offense and hoop sit in frame.")]
        public float lookAhead = 3f;
        [Tooltip("Height of the look-at point (m) — keeps the horizon stable.")]
        public float lookHeight = 1.2f;
        [Tooltip("How quickly the camera position catches up to the ball (higher = tighter, less trailing lag).")]
        public float followSmoothing = 5f;
        [Tooltip("How quickly the camera swings around when possession (down-court direction) flips.")]
        public float turnSmoothing = 3f;
        public float fieldOfView = 46f;

        Camera _cam;
        Vector3 _forward = Vector3.zero; // current down-court direction (smoothed)

        void Awake()
        {
            _cam = GetComponent<Camera>();
        }

        void LateUpdate()
        {
            if (_cam != null) _cam.fieldOfView = fieldOfView;
            if (target == null) return;

            // Down-court direction = court long axis toward the attacked hoop of
            // whoever has the ball. Stable, and flips on a change of possession.
            Vector3 wantFwd = _forward;
            var gm = GameManager.Instance;
            if (gm != null && gm.hoops.Count >= 2)
            {
                Hoop attack = gm.GetAttackingHoop(gm.Possession);
                if (attack != null)
                {
                    Hoop other = null;
                    foreach (var h in gm.hoops) if (h != attack) { other = h; break; }
                    if (other != null)
                    {
                        Vector3 axis = attack.AimPoint - other.AimPoint; axis.y = 0f;
                        if (axis.sqrMagnitude > 0.01f) wantFwd = axis.normalized;
                    }
                }
            }
            if (_forward == Vector3.zero) _forward = wantFwd;                       // snap on the first valid frame
            else _forward = Vector3.Slerp(_forward, wantFwd, turnSmoothing * Time.deltaTime);
            if (_forward.sqrMagnitude > 0.0001f) _forward.Normalize();
            else return;

            Vector3 ground = new Vector3(target.position.x, 0f, target.position.z);
            Vector3 right = Vector3.Cross(Vector3.up, _forward); // unit, perpendicular to forward

            Vector3 desired = ground - _forward * distanceBehind + right * lateralOffset + Vector3.up * height;
            transform.position = Vector3.Lerp(transform.position, desired, followSmoothing * Time.deltaTime);

            Vector3 look = ground + _forward * lookAhead + Vector3.up * lookHeight;
            transform.LookAt(look);
        }
    }
}
