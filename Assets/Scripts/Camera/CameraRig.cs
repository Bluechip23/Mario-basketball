using UnityEngine;

namespace MarioBasketball.CameraControl
{
    /// <summary>
    /// NBA-Street-style presentation camera: it sits off one sideline, fairly
    /// low and close (so players read big on screen), and pans along the court
    /// following the action — normally the ball. The court runs left-right
    /// across the screen with a hoop at each side; the camera slides along the
    /// sideline but never past <see cref="zRange"/>, so it pans-and-looks into
    /// the corners rather than chasing through them.
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        [Tooltip("What the camera tracks — the ball once a match starts.")]
        public Transform target;

        [Header("Sideline framing")]
        [Tooltip("Fixed x position off the sideline (negative = -x side).")]
        public float sideX = -11f;
        public float height = 4.5f;
        [Tooltip("The camera slides along the sideline within ±zRange.")]
        public float zRange = 8f;
        [Tooltip("Height of the look-at point, keeps the horizon stable.")]
        public float lookHeight = 1.4f;
        public float followSmoothing = 5f;
        public float fieldOfView = 46f;

        Camera _cam;

        void Awake()
        {
            _cam = GetComponent<Camera>();
        }

        void LateUpdate()
        {
            if (_cam != null) _cam.fieldOfView = fieldOfView;
            if (target == null) return;

            float z = Mathf.Clamp(target.position.z, -zRange, zRange);
            Vector3 desired = new Vector3(sideX, height, z);
            transform.position = Vector3.Lerp(transform.position, desired, followSmoothing * Time.deltaTime);

            Vector3 look = target.position;
            look.y = lookHeight;
            transform.LookAt(look);
        }
    }
}
