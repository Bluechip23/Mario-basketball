using UnityEngine;
using MarioBasketball.Gameplay;

namespace MarioBasketball.Presentation
{
    /// <summary>
    /// Cheap procedural animation for the placeholder models: swings the limb
    /// joints (<c>JointArmL/R</c>, <c>JointLegL/R</c>) and tips the whole body on
    /// a knockdown, driven from the player's state.
    /// <list type="bullet">
    ///   <item><b>Run</b>: legs and arms counter-swing, scaled by speed.</item>
    ///   <item><b>Dribble</b>: the ball-side arm pumps in sync with the bounce.</item>
    ///   <item><b>Jump shot</b>: both hands rise overhead.</item>
    ///   <item><b>Dunk</b>: both arms up; <b>layup</b>: one hand up.</item>
    ///   <item><b>Ankle-broken / leveled</b>: the body sprawls to the floor.</item>
    /// </list>
    /// Characters without limbs (Boo, Piranha Plant) have no joints and are
    /// untouched. Replaced wholesale when rigged models/animations arrive.
    /// </summary>
    public class ProceduralAnimator : MonoBehaviour
    {
        [Header("Run cycle")]
        public float strideFrequency = 1.5f;
        public float strideFrequencyPerSpeed = 0.2f;
        public float legSwingDegrees = 50f;
        public float armSwingDegrees = 38f;

        [Header("Poses")]
        public float dribblePumpDegrees = 30f;
        public float dribbleArmBase = -40f;    // forward raise of the dribbling arm
        public float guardArmDegrees = -18f;
        public float shootArmDegrees = -165f;  // overhead
        public float poseLerp = 13f;
        public float fallAngle = 80f;

        PlayerController _pc;
        BallController _ball;
        Transform _model, _armL, _armR, _legL, _legR;
        float _phase;
        float _fallTilt;

        void Start()
        {
            _pc = GetComponent<PlayerController>();
            _model = transform.Find("Model");
            _armL = FindDeep(transform, "JointArmL");
            _armR = FindDeep(transform, "JointArmR");
            _legL = FindDeep(transform, "JointLegL");
            _legR = FindDeep(transform, "JointLegR");
            _fallTilt = Random.Range(-25f, 25f);
        }

        void LateUpdate()
        {
            if (_pc == null) return;
            if (_ball == null && MarioBasketball.Core.GameManager.Instance != null)
                _ball = MarioBasketball.Core.GameManager.Instance.ball;

            // Knocked down: tip the whole body to the floor (and skip limb poses).
            if (_model != null)
            {
                Quaternion want = _pc.IsFallen ? Quaternion.Euler(fallAngle, 0f, _fallTilt) : Quaternion.identity;
                _model.localRotation = Quaternion.Slerp(_model.localRotation, want, poseLerp * Time.deltaTime);
            }
            if (_pc.IsFallen) return;

            float speed = _pc.PlanarSpeed;
            bool moving = speed > 0.6f;
            _phase += Time.deltaTime * Mathf.PI * 2f * (strideFrequency + strideFrequencyPerSpeed * speed);
            float swing = moving ? Mathf.Sin(_phase) : 0f;
            float speedScale = Mathf.Clamp01(speed / 7f);

            SetX(_legL, swing * legSwingDegrees * speedScale);
            SetX(_legR, -swing * legSwingDegrees * speedScale);

            if (_pc.IsShooting || (_pc.IsFinishing && _pc.FinishIsDunk))
            {
                SetX(_armL, shootArmDegrees);   // both hands up
                SetX(_armR, shootArmDegrees);
            }
            else if (_pc.IsFinishing)
            {
                SetX(_armR, shootArmDegrees);    // one-handed layup
                SetX(_armL, guardArmDegrees);
            }
            else if (_pc.IsDribbling)
            {
                float hz = _ball != null ? _ball.dribbleHz : 2.3f;
                float pump = Mathf.Sin(Time.time * hz * Mathf.PI * 2f) * dribblePumpDegrees;
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
