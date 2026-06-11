using UnityEngine;
using MarioBasketball.Gameplay;

namespace MarioBasketball.Presentation
{
    /// <summary>
    /// Cheap procedural animation for the placeholder models: swings the limb
    /// joints (<c>JointArmL/R</c> at the shoulder, <c>JointElbowL/R</c> at the
    /// elbow, <c>JointLegL/R</c> at the hip) and tips the whole body on a
    /// knockdown, driven from the player's state.
    /// <list type="bullet">
    ///   <item><b>Run</b>: legs and bent arms counter-swing, scaled by speed.</item>
    ///   <item><b>Dribble</b>: the ball-side hand rides the real ball — it pushes
    ///   down (elbow extending) as the ball drops and waits at the hip for it to
    ///   come back up. Follows crossovers to the other hand.</item>
    ///   <item><b>Jump shot</b>: gather at the chest → set overhead → elbow
    ///   extends into the release, off-hand guides; brief follow-through after
    ///   the ball leaves.</item>
    ///   <item><b>Dunk</b>: both arms drive up; <b>layup</b>: one arm extends.</item>
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
        [Tooltip("Elbow bend while running (arms pump bent, not straight).")]
        public float runElbowDegrees = -40f;
        public float idleElbowDegrees = -10f;

        [Header("Dribble")]
        [Tooltip("Shoulder raise when the hand meets the ball at the hip.")]
        public float dribbleArmBase = -40f;
        [Tooltip("How far the shoulder drops toward hanging as the hand pushes the ball to the floor.")]
        public float dribblePushDegrees = 34f;
        public float dribbleElbowBent = -70f;   // at hip contact
        public float dribbleElbowPushed = -12f; // pushing through the bounce
        public float guardArmDegrees = -18f;

        [Header("Jump shot form")]
        public float gatherArmDegrees = -70f;    // ball gathered at the chest
        public float gatherElbowDegrees = -95f;
        public float releaseArmDegrees = -150f;  // arm extended toward the rim
        public float releaseElbowDegrees = -10f;
        public float guideArmDegrees = -105f;    // off-hand steadies the ball
        public float guideElbowDegrees = -50f;
        [Tooltip("Seconds the shooting arm holds the release pose after the ball leaves.")]
        public float followThroughTime = 0.35f;

        [Header("Finish (dunk / layup)")]
        public float dunkArmDegrees = -160f;
        public float dunkElbowDegrees = -25f;
        public float layupArmDegrees = -170f;
        public float layupElbowDegrees = -5f;

        [Header("Misc")]
        public float poseLerp = 13f;
        public float fallAngle = 80f;

        PlayerController _pc;
        BallController _ball;
        Transform _model, _armL, _armR, _elbowL, _elbowR, _legL, _legR;
        float _phase;
        float _fallTilt;
        bool _wasShooting;
        float _followThrough;

        void Start()
        {
            _pc = GetComponent<PlayerController>();
            _model = transform.Find("Model");
            _armL = FindDeep(transform, "JointArmL");
            _armR = FindDeep(transform, "JointArmR");
            _elbowL = FindDeep(transform, "JointElbowL");
            _elbowR = FindDeep(transform, "JointElbowR");
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

            // A released jump shot holds its follow-through briefly.
            bool shooting = _pc.IsShooting;
            if (_wasShooting && !shooting) _followThrough = followThroughTime;
            else if (_followThrough > 0f) _followThrough -= Time.deltaTime;
            _wasShooting = shooting;

            if (shooting)
            {
                // Form tracks the shot meter: gather → set → release at the apex.
                float k = Mathf.Clamp01(_pc.ShotChargeFraction / Mathf.Max(0.01f, _pc.ShotPerfectFraction));
                Pose(_armR, _elbowR,
                    Mathf.Lerp(gatherArmDegrees, releaseArmDegrees, k),
                    Mathf.Lerp(gatherElbowDegrees, releaseElbowDegrees, k));
                Pose(_armL, _elbowL,
                    Mathf.Lerp(gatherArmDegrees, guideArmDegrees, k),
                    Mathf.Lerp(gatherElbowDegrees, guideElbowDegrees, k));
            }
            else if (_followThrough > 0f)
            {
                Pose(_armR, _elbowR, releaseArmDegrees, releaseElbowDegrees);
                Pose(_armL, _elbowL, guideArmDegrees, guideElbowDegrees);
            }
            else if (_pc.IsFinishing && _pc.FinishIsDunk)
            {
                Pose(_armR, _elbowR, dunkArmDegrees, dunkElbowDegrees); // two-hand slam
                Pose(_armL, _elbowL, dunkArmDegrees, dunkElbowDegrees);
            }
            else if (_pc.IsFinishing)
            {
                Pose(_armR, _elbowR, layupArmDegrees, layupElbowDegrees); // one-hand finish
                Pose(_armL, _elbowL, guardArmDegrees, dribbleElbowBent);
            }
            else if (_pc.IsDribbling)
            {
                // In phase with the actual ball: 0 = hip contact, 0.5 = floor
                // (see BallController.DribblePosition). The hand pushes down
                // with the ball and waits at the hip while it rises.
                float frac = _ball != null ? _ball.DribblePhase01 : (Time.time * 2.3f) % 1f;
                float u = 2f * frac - 1f;
                float push = 1f - u * u;
                bool leftHand = _ball != null && _ball.DribbleHand < 0;
                Transform pumpArm = leftHand ? _armL : _armR, pumpElbow = leftHand ? _elbowL : _elbowR;
                Transform offArm = leftHand ? _armR : _armL, offElbow = leftHand ? _elbowR : _elbowL;
                Pose(pumpArm, pumpElbow,
                    dribbleArmBase + dribblePushDegrees * push,
                    Mathf.Lerp(dribbleElbowBent, dribbleElbowPushed, push));
                Pose(offArm, offElbow,
                    guardArmDegrees + (moving ? swing * armSwingDegrees * 0.4f * speedScale : 0f),
                    dribbleElbowBent * 0.5f);
            }
            else
            {
                float elbow = moving ? runElbowDegrees : idleElbowDegrees;
                Pose(_armL, _elbowL, -swing * armSwingDegrees * speedScale, elbow);
                Pose(_armR, _elbowR, swing * armSwingDegrees * speedScale, elbow);
            }
        }

        void Pose(Transform shoulder, Transform elbow, float shoulderDeg, float elbowDeg)
        {
            SetX(shoulder, shoulderDeg);
            SetX(elbow, elbowDeg);
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
