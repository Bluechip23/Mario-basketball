using UnityEngine;
using MarioBasketball.Gameplay;

namespace MarioBasketball.Presentation
{
    /// <summary>
    /// Cheap procedural animation for the placeholder models: swings the limb
    /// joints (<c>JointArmL/R</c> at the shoulder, <c>JointElbowL/R</c>,
    /// <c>JointWristL/R</c>, <c>JointLegL/R</c> at the hip, <c>JointKneeL/R</c>)
    /// and tips the whole body on a knockdown, driven from the player's state.
    /// <list type="bullet">
    ///   <item><b>Run</b>: legs and bent arms counter-swing, scaled by speed;
    ///   knees bend through the recovery half of each stride and the legs tuck
    ///   while airborne.</item>
    ///   <item><b>Dribble</b>: the ball-side hand rides the real ball — it pushes
    ///   down (elbow extending, wrist snapping through) as the ball drops and
    ///   waits at the hip for it to come back up. Follows crossovers to the
    ///   other hand.</item>
    ///   <item><b>Jump shot</b>: gather at the chest with the wrist cocked back →
    ///   set overhead → elbow extends into the release, off-hand guides; the
    ///   wrist flicks forward in a brief follow-through after the ball leaves.</item>
    ///   <item><b>Dunk</b>: both arms drive up; <b>layup</b>: one arm extends.</item>
    ///   <item><b>Carrying a gathered ball</b>: both hands hold it at the chest.</item>
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
        [Tooltip("Knee bend as the trailing leg swings back through under the body.")]
        public float runKneeDegrees = 65f;
        public float idleKneeDegrees = 4f;
        [Tooltip("Knee tuck while off the ground (jump shot, dunk, rebound).")]
        public float airTuckKneeDegrees = 75f;

        [Header("Dribble")]
        [Tooltip("Shoulder raise when the hand meets the ball at the hip.")]
        public float dribbleArmBase = -40f;
        [Tooltip("How far the shoulder drops toward hanging as the hand pushes the ball to the floor.")]
        public float dribblePushDegrees = 34f;
        public float dribbleElbowBent = -70f;    // at hip contact
        public float dribbleElbowPushed = -12f;  // pushing through the bounce
        public float dribbleWristCocked = 35f;   // hand laid back on the ball at the hip
        public float dribbleWristPushed = -25f;  // snapped through at the floor
        public float guardArmDegrees = -18f;

        [Header("Jump shot form")]
        public float gatherArmDegrees = -70f;    // ball gathered at the chest
        public float gatherElbowDegrees = -95f;
        public float gatherWristDegrees = 45f;   // wrist cocked back under the ball
        public float releaseArmDegrees = -150f;  // arm extended toward the rim
        public float releaseElbowDegrees = -10f;
        public float releaseWristDegrees = -55f; // the gooseneck flick
        public float guideArmDegrees = -105f;    // off-hand steadies the ball
        public float guideElbowDegrees = -50f;
        [Tooltip("Seconds the shooting arm holds the release pose after the ball leaves.")]
        public float followThroughTime = 0.35f;

        [Header("Finish (dunk / layup)")]
        public float dunkArmDegrees = -160f;
        public float dunkElbowDegrees = -25f;
        public float dunkWristDegrees = -35f;    // throwing the ball down through the rim
        public float layupArmDegrees = -170f;
        public float layupElbowDegrees = -5f;

        [Header("Carrying a gathered ball")]
        public float holdArmDegrees = -45f;      // both hands on the ball at the chest
        public float holdElbowDegrees = -65f;
        public float holdWristDegrees = 15f;     // palms cradling under it

        [Header("Misc")]
        public float poseLerp = 13f;
        public float fallAngle = 80f;

        PlayerController _pc;
        BallController _ball;
        Transform _model, _armL, _armR, _elbowL, _elbowR, _wristL, _wristR, _legL, _legR, _kneeL, _kneeR;
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
            _wristL = FindDeep(transform, "JointWristL");
            _wristR = FindDeep(transform, "JointWristR");
            _legL = FindDeep(transform, "JointLegL");
            _legR = FindDeep(transform, "JointLegR");
            _kneeL = FindDeep(transform, "JointKneeL");
            _kneeR = FindDeep(transform, "JointKneeR");
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
            if (_pc.IsAirborne)
            {
                SetX(_kneeL, airTuckKneeDegrees);
                SetX(_kneeR, airTuckKneeDegrees);
            }
            else
            {
                // The knee folds while its leg recovers forward and is straight
                // for the planted half of the stride.
                float recover = moving ? Mathf.Cos(_phase) : 0f;
                SetX(_kneeL, idleKneeDegrees + Mathf.Max(0f, recover) * runKneeDegrees * speedScale);
                SetX(_kneeR, idleKneeDegrees + Mathf.Max(0f, -recover) * runKneeDegrees * speedScale);
            }

            // A released jump shot holds its follow-through briefly.
            bool shooting = _pc.IsShooting;
            if (_wasShooting && !shooting) _followThrough = followThroughTime;
            else if (_followThrough > 0f) _followThrough -= Time.deltaTime;
            _wasShooting = shooting;

            if (shooting)
            {
                // Form tracks the shot meter: gather → set → release at the apex.
                // The wrist stays cocked back under the ball until the flick.
                float k = Mathf.Clamp01(_pc.ShotChargeFraction / Mathf.Max(0.01f, _pc.ShotPerfectFraction));
                Pose(_armR, _elbowR, _wristR,
                    Mathf.Lerp(gatherArmDegrees, releaseArmDegrees, k),
                    Mathf.Lerp(gatherElbowDegrees, releaseElbowDegrees, k),
                    gatherWristDegrees);
                Pose(_armL, _elbowL, _wristL,
                    Mathf.Lerp(gatherArmDegrees, guideArmDegrees, k),
                    Mathf.Lerp(gatherElbowDegrees, guideElbowDegrees, k),
                    gatherWristDegrees * 0.5f);
            }
            else if (_followThrough > 0f)
            {
                Pose(_armR, _elbowR, _wristR, releaseArmDegrees, releaseElbowDegrees, releaseWristDegrees);
                Pose(_armL, _elbowL, _wristL, guideArmDegrees, guideElbowDegrees, 0f);
            }
            else if (_pc.IsFinishing && _pc.FinishIsDunk)
            {
                Pose(_armR, _elbowR, _wristR, dunkArmDegrees, dunkElbowDegrees, dunkWristDegrees); // two-hand slam
                Pose(_armL, _elbowL, _wristL, dunkArmDegrees, dunkElbowDegrees, dunkWristDegrees);
            }
            else if (_pc.IsFinishing)
            {
                Pose(_armR, _elbowR, _wristR, layupArmDegrees, layupElbowDegrees, releaseWristDegrees * 0.5f); // one-hand finish
                Pose(_armL, _elbowL, _wristL, guardArmDegrees, dribbleElbowBent, 0f);
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
                Transform pumpArm = leftHand ? _armL : _armR, pumpElbow = leftHand ? _elbowL : _elbowR, pumpWrist = leftHand ? _wristL : _wristR;
                Transform offArm = leftHand ? _armR : _armL, offElbow = leftHand ? _elbowR : _elbowL, offWrist = leftHand ? _wristR : _wristL;
                Pose(pumpArm, pumpElbow, pumpWrist,
                    dribbleArmBase + dribblePushDegrees * push,
                    Mathf.Lerp(dribbleElbowBent, dribbleElbowPushed, push),
                    Mathf.Lerp(dribbleWristCocked, dribbleWristPushed, push));
                Pose(offArm, offElbow, offWrist,
                    guardArmDegrees + (moving ? swing * armSwingDegrees * 0.4f * speedScale : 0f),
                    dribbleElbowBent * 0.5f,
                    0f);
            }
            else if (_pc.HasBall)
            {
                // Gathered ball (fresh pickup, post-up, airborne rebound):
                // both hands carry it at the chest instead of hanging loose.
                Pose(_armL, _elbowL, _wristL, holdArmDegrees, holdElbowDegrees, holdWristDegrees);
                Pose(_armR, _elbowR, _wristR, holdArmDegrees, holdElbowDegrees, holdWristDegrees);
            }
            else
            {
                float elbow = moving ? runElbowDegrees : idleElbowDegrees;
                Pose(_armL, _elbowL, _wristL, -swing * armSwingDegrees * speedScale, elbow, 0f);
                Pose(_armR, _elbowR, _wristR, swing * armSwingDegrees * speedScale, elbow, 0f);
            }
        }

        void Pose(Transform shoulder, Transform elbow, Transform wrist, float shoulderDeg, float elbowDeg, float wristDeg)
        {
            SetX(shoulder, shoulderDeg);
            SetX(elbow, elbowDeg);
            SetX(wrist, wristDeg);
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
