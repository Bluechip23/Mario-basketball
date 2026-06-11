using UnityEngine;
using MarioBasketball.Gameplay;

namespace MarioBasketball.Presentation
{
    /// <summary>
    /// Cheap procedural animation for the placeholder models: swings the limb
    /// joints (<c>JointArmL/R</c>, <c>JointLegL/R</c>) created by
    /// <c>CharacterModelBuilder</c> from the player's state.
    /// <list type="bullet">
    ///   <item><b>Run</b>: legs and arms counter-swing, scaled by speed.</item>
    ///   <item><b>Dribble</b>: the ball-side arm pumps in time with the dribble
    ///   bob; the off arm guards.</item>
    ///   <item><b>Shoot / finish</b>: both arms extend overhead.</item>
    ///   <item><b>Idle</b>: everything eases back to neutral.</item>
    /// </list>
    /// Characters without limbs (Boo, Piranha Plant) simply have no joints and
    /// are untouched. Replaced wholesale when rigged models/animations arrive.
    /// </summary>
    public class ProceduralAnimator : MonoBehaviour
    {
        [Header("Run cycle")]
        public float strideFrequency = 1.6f;   // cycles/sec at min speed
        public float strideFrequencyPerSpeed = 0.22f;
        public float legSwingDegrees = 45f;
        public float armSwingDegrees = 35f;

        [Header("Dribble / shoot poses")]
        public float dribblePumpDegrees = 28f;
        public float dribbleArmBase = -35f;    // forward raise of the dribbling arm
        public float guardArmDegrees = -20f;
        public float shootArmDegrees = -165f;  // overhead
        public float poseLerp = 12f;

        PlayerController _pc;
        BallController _ball;
        Transform _armL, _armR, _legL, _legR;
        float _phase;

        void Start()
        {
            _pc = GetComponent<PlayerController>();
            _armL = FindDeep(transform, "JointArmL");
            _armR = FindDeep(transform, "JointArmR");
            _legL = FindDeep(transform, "JointLegL");
            _legR = FindDeep(transform, "JointLegR");
        }

        void LateUpdate()
        {
            if (_pc == null) return;
            if (_ball == null && MarioBasketball.Core.GameManager.Instance != null)
                _ball = MarioBasketball.Core.GameManager.Instance.ball;

            float speed = _pc.PlanarSpeed;
            bool moving = speed > 0.6f;
            _phase += Time.deltaTime * Mathf.PI * 2f * (strideFrequency + strideFrequencyPerSpeed * speed);
            float swing = moving ? Mathf.Sin(_phase) : 0f;
            float speedScale = Mathf.Clamp01(speed / 7f);

            // Legs always run the stride (or settle to neutral).
            SetX(_legL, swing * legSwingDegrees * speedScale);
            SetX(_legR, -swing * legSwingDegrees * speedScale);

            // Arms by state.
            if (_pc.IsShooting || _pc.IsFinishing)
            {
                SetX(_armL, shootArmDegrees);
                SetX(_armR, shootArmDegrees);
            }
            else if (_pc.HasBall)
            {
                // Pump the dribbling arm in time with the ball bob.
                float bob = _ball != null ? _ball.dribbleSpeed : 6f;
                float pump = Mathf.Sin(Time.time * bob) * dribblePumpDegrees;
                SetX(_armR, dribbleArmBase + pump);
                SetX(_armL, guardArmDegrees + (moving ? swing * armSwingDegrees * 0.4f * speedScale : 0f));
            }
            else
            {
                SetX(_armL, -swing * armSwingDegrees * speedScale);
                SetX(_armR, swing * armSwingDegrees * speedScale);
            }
        }

        void SetX(Transform joint, float degrees)
        {
            if (joint == null) return;
            Quaternion target = Quaternion.Euler(degrees, 0f, 0f);
            joint.localRotation = Quaternion.Slerp(joint.localRotation, target, poseLerp * Time.deltaTime);
        }

        static Transform FindDeep(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }
    }
}
