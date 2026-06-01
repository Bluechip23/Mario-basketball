using UnityEngine;

namespace MarioBasketball.CameraControl
{
    /// <summary>
    /// A smooth chase camera that frames a follow target (the player) from a
    /// raised, angled-back position — the broadcast-ish view NBA Street uses.
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0f, 9f, -11f);
        public float followSmoothing = 6f;
        public float lookHeight = 1.5f;

        void LateUpdate()
        {
            if (target == null) return;

            Vector3 desired = target.position + offset;
            transform.position = Vector3.Lerp(transform.position, desired, followSmoothing * Time.deltaTime);
            transform.LookAt(target.position + Vector3.up * lookHeight);
        }
    }
}
