using System.Collections;
using UnityEngine;

namespace MarioBasketball.Presentation
{
    /// <summary>
    /// The placeholder net under a rim. <see cref="Swish"/> (called by the
    /// score zone on a make) snaps the net long-and-narrow then springs it
    /// back — a cheap, readable swish until cloth/real art arrives.
    /// </summary>
    public class NetSwish : MonoBehaviour
    {
        public float stretch = 1.45f;
        public float pinch = 0.7f;
        public float duration = 0.45f;

        Vector3 _restScale;
        Coroutine _running;

        void Awake()
        {
            _restScale = transform.localScale;
        }

        public void Swish()
        {
            if (_running != null) StopCoroutine(_running);
            _running = StartCoroutine(SwishRoutine());
        }

        IEnumerator SwishRoutine()
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = t / duration;
                // Sharp snap (pinched + stretched), then ease back to rest.
                float snap = Mathf.Sin(Mathf.Min(k * 3f, 1f) * Mathf.PI) * (1f - k);
                transform.localScale = new Vector3(
                    _restScale.x * Mathf.Lerp(1f, pinch, snap),
                    _restScale.y * Mathf.Lerp(1f, stretch, snap),
                    _restScale.z * Mathf.Lerp(1f, pinch, snap));
                yield return null;
            }
            transform.localScale = _restScale;
            _running = null;
        }
    }
}
