using System.Collections.Generic;
using UnityEngine;
using MarioBasketball.Gameplay;

namespace MarioBasketball.Presentation
{
    /// <summary>
    /// Strobes a player's whole body through a bright star-power palette while
    /// they're <see cref="Characters.PlayerCharacter.OnFire"/> — the Mario
    /// invincibility-star flash. Caches each renderer's original colour up front
    /// and snaps it back the instant the fire goes out. Added to the player root
    /// at build time (sits beside <see cref="ProceduralAnimator"/>).
    /// </summary>
    public class OnFireFlash : MonoBehaviour
    {
        [Tooltip("Palette steps cycled per second — the higher, the faster the strobe.")]
        public float cycleSpeed = 11f;

        // Bright invincibility-star colours, cycled fast.
        static readonly Color[] Palette =
        {
            new Color(1f, 1f, 1f),
            new Color(1f, 0.95f, 0.2f),
            new Color(1f, 0.55f, 0.1f),
            new Color(1f, 0.2f, 0.2f),
            new Color(0.4f, 0.85f, 1f),
            new Color(0.5f, 1f, 0.35f),
        };

        struct Tinted
        {
            public Renderer r;
            public bool hasBase; public Color baseCol;
            public bool hasCol;  public Color col;
        }

        PlayerController _pc;
        readonly List<Tinted> _renderers = new List<Tinted>();
        bool _flashing;

        void Awake()
        {
            _pc = GetComponent<PlayerController>();
            Transform model = transform.Find("Model");
            Transform scan = model != null ? model : transform;
            foreach (var r in scan.GetComponentsInChildren<Renderer>())
            {
                var mat = r.material; // own instance, so tinting doesn't bleed to others
                var t = new Tinted { r = r };
                if (mat.HasProperty("_BaseColor")) { t.hasBase = true; t.baseCol = mat.GetColor("_BaseColor"); }
                if (mat.HasProperty("_Color")) { t.hasCol = true; t.col = mat.GetColor("_Color"); }
                _renderers.Add(t);
            }
        }

        void LateUpdate()
        {
            bool onFire = _pc != null && _pc.Character != null && _pc.Character.OnFire;
            if (!onFire)
            {
                if (_flashing) Restore();
                return;
            }
            _flashing = true;

            int idx = Mathf.FloorToInt(Time.time * cycleSpeed) % Palette.Length;
            float pulse = 0.78f + 0.22f * Mathf.Sin(Time.time * 28f); // shimmer
            Color c = Palette[idx] * pulse;
            c.a = 1f;
            foreach (var item in _renderers)
            {
                if (item.r == null) continue;
                var mat = item.r.material;
                if (item.hasBase) mat.SetColor("_BaseColor", c);
                if (item.hasCol) mat.SetColor("_Color", c);
            }
        }

        void Restore()
        {
            _flashing = false;
            foreach (var item in _renderers)
            {
                if (item.r == null) continue;
                var mat = item.r.material;
                if (item.hasBase) mat.SetColor("_BaseColor", item.baseCol);
                if (item.hasCol) mat.SetColor("_Color", item.col);
            }
        }
    }
}
