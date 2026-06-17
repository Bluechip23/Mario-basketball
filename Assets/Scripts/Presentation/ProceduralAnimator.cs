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
        public float strideFrequency = 1.6f;
        public float strideFrequencyPerSpeed = 0.22f;
        public float legSwingDegrees = 60f;
        public float armSwingDegrees = 55f;
        [Tooltip("Elbow bend while running (arms pump at ~90°, not straight nutcrackers).")]
        public float runElbowDegrees = -88f;
        public float idleElbowDegrees = -10f;
        [Tooltip("How high the knee drives up as the leg swings forward (high-knee run).")]
        public float runKneeDegrees = 88f;
        public float idleKneeDegrees = 4f;
        [Tooltip("Knee tuck while off the ground (jump shot, dunk, rebound).")]
        public float airTuckKneeDegrees = 75f;
        [Tooltip("Forward torso lean (degrees) at full running speed.")]
        public float runLeanAngle = 14f;

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
        [Tooltip("Body lean (degrees) at a full fadeaway jump shot.")]
        public float fadeLeanAngle = 24f;
        [Tooltip("A loose/shot ball within this of an airborne player makes them reach for the board (vs hands straight up to contest).")]
        public float reboundReachDistance = 2.6f;
        [Tooltip("How far the post defender leans their chest into the poster.")]
        public float postDefendLean = 16f;

        [Header("Post shot form")]
        [Tooltip("How far (deg) the body turns sideways to the rim on a hook shot.")]
        public float hookBodyTurn = 62f;
        [Tooltip("Off-arm shoulder raise while barring out space on a hook.")]
        public float hookGuardArmDegrees = -72f;
        [Tooltip("Off-arm elbow bend (≈90°) while barring out space on a hook.")]
        public float hookGuardElbowDegrees = -100f;

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

            // The poster (if any) currently backing this player down on D.
            PlayerController postingMe = _pc.PostingMeOnD;

            // Body tilt: whip around on a spin move, sprawl to the floor when
            // knocked down, or lean into a fadeaway jump shot.
            bool spinMove = _pc.IsDribbleMoveGesture && _pc.CurrentDribbleMove == DribbleMoveType.Spin;
            if (_model != null)
            {
                if (spinMove && !_pc.IsFallen)
                {
                    // Snap the whole body through a full rotation (no slerp, so it
                    // actually whips); direction follows the dribbling hand.
                    float spinDir = _ball != null && _ball.DribbleHand < 0 ? -1f : 1f;
                    _model.localRotation = Quaternion.Euler(0f, spinDir * 360f * _pc.DribbleMoveProgress01, 0f);
                }
                else
                {
                    Quaternion want;
                    if (_pc.IsFallen)
                        want = Quaternion.Euler(fallAngle, 0f, _fallTilt);
                    else if (_pc.IsShooting && _pc.FadeAmount > 0.05f)
                    {
                        // Convert the world fade into the body's own frame: a fade
                        // away from the rim pitches the torso back, a sideways fade
                        // rolls it over that hip.
                        Vector3 local = transform.InverseTransformDirection(_pc.FadeDirection);
                        float amt = _pc.FadeAmount * fadeLeanAngle;
                        want = Quaternion.Euler(local.z * amt, 0f, -local.x * amt);
                    }
                    else if (postingMe != null)
                    {
                        // Engaged on the back-down: lean the chest into the poster.
                        Vector3 toP = postingMe.transform.position - transform.position; toP.y = 0f;
                        if (toP.sqrMagnitude > 0.01f)
                        {
                            Vector3 local = transform.InverseTransformDirection(toP.normalized);
                            want = Quaternion.Euler(local.z * postDefendLean, 0f, -local.x * postDefendLean * 0.5f);
                        }
                        else want = Quaternion.identity;
                    }
                    else if (_pc.IsPostShooting)
                    {
                        // Each post shot turns the body its own way: a hook turns
                        // sideways to the rim, the turnaround faces up and fades
                        // back, the power drop step squares up to bury it.
                        var move = _pc.CurrentPostMove;
                        float turn =
                            move == PostMove.Hook || move == PostMove.SkyHook ? hookBodyTurn :
                            move == PostMove.TurnaroundJumper ? 165f :
                            move == PostMove.DropStep || move == PostMove.PowerDropStep ? 130f :
                            100f; // spin / up-and-under
                        float lean = move == PostMove.TurnaroundJumper ? -fadeLeanAngle : 0f;
                        want = Quaternion.Euler(lean, turn, 0f);
                    }
                    else if (!_pc.IsAirborne && !_pc.IsPosting && !_pc.IsHanging && !_pc.IsSkyingForOop
                             && !_pc.IsFinishing && _pc.PlanarSpeed > 0.6f)
                    {
                        // Lean into the run, scaled by speed — drives the whole gait.
                        want = Quaternion.Euler(runLeanAngle * Mathf.Clamp01(_pc.PlanarSpeed / 7f), 0f, 0f);
                    }
                    else want = Quaternion.identity;
                    _model.localRotation = Quaternion.Slerp(_model.localRotation, want, poseLerp * Time.deltaTime);
                }
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
                // High knees: the leg swinging forward drives its knee up; the
                // trailing leg straightens to push off — a real running gait.
                SetX(_kneeL, idleKneeDegrees + Mathf.Max(0f, swing) * runKneeDegrees * speedScale);
                SetX(_kneeR, idleKneeDegrees + Mathf.Max(0f, -swing) * runKneeDegrees * speedScale);
            }

            // Crouch low to drop the ball between the knees.
            if (!_pc.IsAirborne && _pc.IsDribbleMoveGesture && _pc.CurrentDribbleMove == DribbleMoveType.BetweenLegs)
            {
                SetX(_kneeL, 52f);
                SetX(_kneeR, 52f);
            }
            // Sit into a stance: the poster crouches as they back down; the engaged
            // defender bends their knees and braces.
            if (!_pc.IsAirborne && (_pc.IsPosting || postingMe != null))
            {
                SetX(_kneeL, 34f);
                SetX(_kneeR, 34f);
            }

            // Leg work for a finish: one-foot takeoff drives the opposite knee up
            // hard while the jump leg trails; a rim hang leaves the legs dangling.
            if (_pc.IsHanging)
            {
                SetX(_legL, 6f); SetX(_legR, -6f);
                SetX(_kneeL, 18f); SetX(_kneeR, 22f);
            }
            else if (_pc.IsFinishing && _pc.FinishOneFoot && _pc.IsAirborne)
            {
                if (_pc.FinishTakeoffLeft)
                {
                    SetX(_legL, -12f); SetX(_kneeL, 12f);  // jump leg trails, fairly straight
                    SetX(_legR, -45f); SetX(_kneeR, 80f);  // drive knee up
                }
                else
                {
                    SetX(_legR, -12f); SetX(_kneeR, 12f);
                    SetX(_legL, -45f); SetX(_kneeL, 80f);
                }
            }

            // Post-shot footwork. The power drop step / drop step jump-stops: the
            // lead leg swings hard toward the basket on the gather, the player sinks
            // into a wide base, then explodes up. The hook rises off a balanced
            // gather (a modest dip, no big leg drive).
            if (_pc.IsPostShooting)
            {
                var pmove = _pc.CurrentPostMove;
                float pk = Mathf.Clamp01(_pc.PostShotChargeFraction / Mathf.Max(0.01f, _pc.PostShotPerfectFraction));
                if (pmove == PostMove.DropStep || pmove == PostMove.PowerDropStep)
                {
                    // Swing the lead leg through toward the rim, jump-stop low, rise.
                    float swingLeg = Mathf.Sin(Mathf.PI * Mathf.Clamp01(pk)) * 40f;
                    SetX(_legR, -swingLeg);                 // lead leg drives forward
                    SetX(_legL, swingLeg * 0.4f);           // trail leg sets the base
                    float crouch = Mathf.Lerp(50f, 10f, pk);
                    SetX(_kneeL, crouch);
                    SetX(_kneeR, crouch);
                }
                else
                {
                    // Balanced hook gather: a small dip into the rise-up.
                    float crouch = Mathf.Lerp(30f, 8f, pk);
                    SetX(_kneeL, crouch);
                    SetX(_kneeR, crouch);
                }
            }

            // A released jump shot (or timed post shot) holds its follow-through.
            bool shooting = _pc.IsShooting || _pc.IsPostShooting;
            if (_wasShooting && !shooting) _followThrough = followThroughTime;
            else if (_followThrough > 0f) _followThrough -= Time.deltaTime;
            _wasShooting = shooting;

            if (shooting)
            {
                // Form tracks the shot meter: gather → set → release at the apex.
                // The wrist stays cocked back under the ball until the flick. A
                // post shot rides its own release meter the same way.
                float charge = _pc.IsPostShooting ? _pc.PostShotChargeFraction : _pc.ShotChargeFraction;
                float perfect = _pc.IsPostShooting ? _pc.PostShotPerfectFraction : _pc.ShotPerfectFraction;
                float k = Mathf.Clamp01(charge / Mathf.Max(0.01f, perfect));
                if (_pc.IsPostShooting)
                {
                    PostShotArms(_pc.CurrentPostMove, k);
                }
                else
                {
                    Pose(_armR, _elbowR, _wristR,
                        Mathf.Lerp(gatherArmDegrees, releaseArmDegrees, k),
                        Mathf.Lerp(gatherElbowDegrees, releaseElbowDegrees, k),
                        gatherWristDegrees);
                    Pose(_armL, _elbowL, _wristL,
                        Mathf.Lerp(gatherArmDegrees, guideArmDegrees, k),
                        Mathf.Lerp(gatherElbowDegrees, guideElbowDegrees, k),
                        gatherWristDegrees * 0.5f);
                }
            }
            else if (_followThrough > 0f)
            {
                Pose(_armR, _elbowR, _wristR, releaseArmDegrees, releaseElbowDegrees, releaseWristDegrees);
                Pose(_armL, _elbowL, _wristL, guideArmDegrees, guideElbowDegrees, 0f);
            }
            else if (_pc.IsHanging)
            {
                // Grab the rim and hang. A one-hand flush hangs off the dunking
                // (right) hand with the off-arm tucked; everything else grabs iron
                // two-handed.
                Pose(_armR, _elbowR, _wristR, -178f, -2f, 0f);
                if (_pc.CurrentFinishStyle == FinishStyle.OneFootOneHandDunk)
                    Pose(_armL, _elbowL, _wristL, guardArmDegrees, dribbleElbowBent, 0f);
                else
                    Pose(_armL, _elbowL, _wristL, -178f, -2f, 0f);
            }
            else if (_pc.IsFinishing && _pc.IsAdjustingFinish)
            {
                // Air-adjust (L1): a double-clutch — cradle the ball back in and
                // shield with the off-hand before laying it up around the block.
                Pose(_armR, _elbowR, _wristR, layupArmDegrees + 34f, gatherElbowDegrees, gatherWristDegrees);
                Pose(_armL, _elbowL, _wristL, guideArmDegrees, guideElbowDegrees, 0f);
            }
            else if (_pc.IsFinishing && _pc.CurrentFinishStyle == FinishStyle.Layup)
            {
                // One-hand layup, laid up high off the glass.
                Pose(_armR, _elbowR, _wristR, layupArmDegrees, layupElbowDegrees, releaseWristDegrees * 0.5f);
                Pose(_armL, _elbowL, _wristL, guardArmDegrees, dribbleElbowBent, 0f);
            }
            else if (_pc.IsFinishing && _pc.CurrentFinishStyle == FinishStyle.OneFootOneHandDunk)
            {
                // One-handed flush: cock the ball overhead in the dunking hand.
                Pose(_armR, _elbowR, _wristR, dunkArmDegrees, dunkElbowDegrees, dunkWristDegrees);
                Pose(_armL, _elbowL, _wristL, guardArmDegrees, dribbleElbowBent, 0f);
            }
            else if (_pc.IsFinishing)
            {
                // Two-hand flush (one-foot two-hand, or the gather slam): both arms
                // drive the ball up and over the rim.
                Pose(_armR, _elbowR, _wristR, dunkArmDegrees, dunkElbowDegrees, dunkWristDegrees);
                Pose(_armL, _elbowL, _wristL, dunkArmDegrees, dunkElbowDegrees, dunkWristDegrees);
            }
            else if (_pc.IsDribblingBall || _pc.IsDribbleMoveGesture)
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
                if (_pc.IsDribbleMoveGesture)
                {
                    DribbleMovePose(_pc.CurrentDribbleMove, _pc.DribbleMoveProgress01, leftHand,
                        pumpArm, pumpElbow, pumpWrist, offArm, offElbow, offWrist);
                }
                else
                {
                    Pose(pumpArm, pumpElbow, pumpWrist,
                        dribbleArmBase + dribblePushDegrees * push,
                        Mathf.Lerp(dribbleElbowBent, dribbleElbowPushed, push),
                        Mathf.Lerp(dribbleWristCocked, dribbleWristPushed, push));
                    Pose(offArm, offElbow, offWrist,
                        guardArmDegrees + (moving ? swing * armSwingDegrees * 0.4f * speedScale : 0f),
                        dribbleElbowBent * 0.5f,
                        0f);
                }
            }
            else if (_pc.HasBall && _pc.IsAirborne)
            {
                // Just grabbed a board in the air — secure it overhead in both hands.
                Pose(_armR, _elbowR, _wristR, -168f, -10f, 0f);
                Pose(_armL, _elbowL, _wristL, -168f, -10f, 0f);
            }
            else if (_pc.HasBall)
            {
                // Gathered ball (fresh pickup, triple threat): both hands carry it
                // at the chest instead of hanging loose.
                Pose(_armL, _elbowL, _wristL, holdArmDegrees, holdElbowDegrees, holdWristDegrees);
                Pose(_armR, _elbowR, _wristR, holdArmDegrees, holdElbowDegrees, holdWristDegrees);
            }
            else if (_pc.IsStealing)
            {
                // Swipe at the ball: the lead arm jabs out and snaps back through.
                float sw = Mathf.Sin(Mathf.PI * _pc.StealProgress01);
                Pose(_armR, _elbowR, _wristR,
                    Mathf.Lerp(guardArmDegrees, -120f, sw),
                    Mathf.Lerp(dribbleElbowBent, -10f, sw),
                    Mathf.Lerp(0f, -45f, sw));
                Pose(_armL, _elbowL, _wristL, guardArmDegrees, idleElbowDegrees, 0f);
            }
            else if (_pc.IsPassing)
            {
                // Just threw a pass: both arms push out toward the target and the
                // wrists snap through (a chest/push pass), held briefly.
                Pose(_armR, _elbowR, _wristR, releaseArmDegrees * 0.72f, releaseElbowDegrees, releaseWristDegrees);
                Pose(_armL, _elbowL, _wristL, releaseArmDegrees * 0.72f, releaseElbowDegrees, releaseWristDegrees);
            }
            else if (postingMe != null)
            {
                // Bracing against the back-down: forearms bar into the poster.
                Pose(_armR, _elbowR, _wristR, -44f, -95f, 0f);
                Pose(_armL, _elbowL, _wristL, -44f, -95f, 0f);
            }
            else if (_pc.IsAirborne && !_pc.HasBall)
            {
                // Up off the ball: reach both hands toward a loose/shot ball to grab
                // the board, or throw them straight up to contest/block a shot.
                bool reachBall = _ball != null && _ball.State != BallController.BallState.Held
                    && Vector3.Distance(_ball.transform.position, transform.position) < reboundReachDistance;
                float pitch = reachBall ? -165f : -178f; // toward the ball, or straight up
                Pose(_armR, _elbowR, _wristR, pitch, -6f, 0f);
                Pose(_armL, _elbowL, _wristL, pitch, -6f, 0f);
            }
            else
            {
                float elbow = moving ? runElbowDegrees : idleElbowDegrees;
                Pose(_armL, _elbowL, _wristL, -swing * armSwingDegrees * speedScale, elbow, 0f);
                Pose(_armR, _elbowR, _wristR, swing * armSwingDegrees * speedScale, elbow, 0f);
            }
        }

        /// <summary>Distinct arm work for each dribble move (the ball path lives in
        /// <see cref="BallController.DribblePosition"/>; here we sell the hands).</summary>
        void DribbleMovePose(DribbleMoveType move, float p, bool leftHand,
            Transform pumpArm, Transform pumpElbow, Transform pumpWrist,
            Transform offArm, Transform offElbow, Transform offWrist)
        {
            switch (move)
            {
                case DribbleMoveType.Crossover:
                case DribbleMoveType.Hesitation:
                {
                    // Arms spread wide, ball ripped hard and low across the body.
                    const float spread = 52f;
                    float pumpRoll = leftHand ? -spread : spread;
                    float offRoll = leftHand ? spread : -spread;
                    PoseRoll(pumpArm, pumpElbow, pumpWrist,
                        dribbleArmBase + dribblePushDegrees, dribbleElbowPushed, dribbleWristPushed, pumpRoll);
                    PoseRoll(offArm, offElbow, offWrist,
                        dribbleArmBase + dribblePushDegrees * 0.7f, dribbleElbowPushed, 0f, offRoll);
                    break;
                }
                case DribbleMoveType.BehindBack:
                {
                    // The ball hand wraps from behind the hip around to the front.
                    float pitch = Mathf.Lerp(45f, -70f, p);
                    Pose(pumpArm, pumpElbow, pumpWrist, pitch, dribbleElbowBent, dribbleWristPushed);
                    Pose(offArm, offElbow, offWrist, guardArmDegrees, dribbleElbowBent * 0.5f, 0f);
                    break;
                }
                case DribbleMoveType.BetweenLegs:
                {
                    // Both hands reach down low (paired with the knee crouch).
                    Pose(pumpArm, pumpElbow, pumpWrist,
                        dribbleArmBase + dribblePushDegrees, dribbleElbowPushed, dribbleWristPushed);
                    Pose(offArm, offElbow, offWrist,
                        dribbleArmBase + dribblePushDegrees, dribbleElbowPushed, dribbleWristPushed);
                    break;
                }
                case DribbleMoveType.Spin:
                {
                    // Ball cradled tight to the chest while the body whips around.
                    Pose(pumpArm, pumpElbow, pumpWrist, holdArmDegrees, holdElbowDegrees, holdWristDegrees);
                    Pose(offArm, offElbow, offWrist, holdArmDegrees, holdElbowDegrees, holdWristDegrees);
                    break;
                }
                case DribbleMoveType.OffTheHead:
                {
                    // Toss it up and over: the ball arm flicks overhead at the peak.
                    float up = Mathf.Sin(Mathf.PI * p);
                    Pose(pumpArm, pumpElbow, pumpWrist,
                        Mathf.Lerp(dribbleArmBase, releaseArmDegrees, up),
                        Mathf.Lerp(dribbleElbowBent, releaseElbowDegrees, up),
                        releaseWristDegrees);
                    Pose(offArm, offElbow, offWrist, guideArmDegrees * 0.6f, guideElbowDegrees, 0f);
                    break;
                }
                default: // StepBack
                {
                    // Shove the ball back low; the off arm rides up for balance.
                    Pose(pumpArm, pumpElbow, pumpWrist,
                        dribbleArmBase + dribblePushDegrees, dribbleElbowPushed, dribbleWristPushed);
                    Pose(offArm, offElbow, offWrist, guardArmDegrees, dribbleElbowBent * 0.5f, 0f);
                    break;
                }
            }
        }

        /// <summary>Arm work for each post shot. The hook sweeps one hand up and
        /// over the head with the off-arm barred out for space; the power drop
        /// step and the other rim finishes drive both hands up to flush it; the
        /// turnaround uses normal jumper form (faded back by the body lean).</summary>
        void PostShotArms(PostMove move, float k)
        {
            switch (move)
            {
                case PostMove.Hook:
                case PostMove.SkyHook:
                {
                    // Shooting arm (right) sweeps from the shoulder up and over the
                    // head in the hook arc; the wrist snaps through near the top. The
                    // sky hook releases that touch higher and straighter.
                    float top = move == PostMove.SkyHook ? -192f : -176f;
                    float sweep = Mathf.Lerp(-58f, top, k);
                    float flick = Mathf.Clamp01((k - 0.65f) / 0.35f);
                    Pose(_armR, _elbowR, _wristR, sweep, -12f,
                        Mathf.Lerp(gatherWristDegrees, releaseWristDegrees, flick));
                    // Off arm (left): raised, bent ~90°, barring out a sliver of space.
                    Pose(_armL, _elbowL, _wristL, hookGuardArmDegrees, hookGuardElbowDegrees, 0f);
                    break;
                }

                case PostMove.TurnaroundJumper:
                {
                    // Face-up fadeaway — standard jumper form, fading on the body lean.
                    Pose(_armR, _elbowR, _wristR,
                        Mathf.Lerp(gatherArmDegrees, releaseArmDegrees, k),
                        Mathf.Lerp(gatherElbowDegrees, releaseElbowDegrees, k),
                        gatherWristDegrees);
                    Pose(_armL, _elbowL, _wristL,
                        Mathf.Lerp(gatherArmDegrees, guideArmDegrees, k),
                        Mathf.Lerp(gatherElbowDegrees, guideElbowDegrees, k),
                        gatherWristDegrees * 0.5f);
                    break;
                }

                default: // DropStep, PowerDropStep, Spin, UpAndUnder — power rim finish
                {
                    // Gather the ball low, then drive both hands up and flush it.
                    Pose(_armR, _elbowR, _wristR,
                        Mathf.Lerp(holdArmDegrees, dunkArmDegrees, k),
                        Mathf.Lerp(holdElbowDegrees, dunkElbowDegrees, k),
                        dunkWristDegrees * k);
                    Pose(_armL, _elbowL, _wristL,
                        Mathf.Lerp(holdArmDegrees, dunkArmDegrees, k),
                        Mathf.Lerp(holdElbowDegrees, dunkElbowDegrees, k),
                        dunkWristDegrees * k);
                    break;
                }
            }
        }

        void Pose(Transform shoulder, Transform elbow, Transform wrist, float shoulderDeg, float elbowDeg, float wristDeg)
        {
            SetX(shoulder, shoulderDeg);
            SetX(elbow, elbowDeg);
            SetX(wrist, wristDeg);
        }

        void PoseRoll(Transform shoulder, Transform elbow, Transform wrist,
            float shoulderPitch, float elbowDeg, float wristDeg, float shoulderRoll)
        {
            SetRot(shoulder, shoulderPitch, 0f, shoulderRoll);
            SetX(elbow, elbowDeg);
            SetX(wrist, wristDeg);
        }

        void SetRot(Transform joint, float x, float y, float z)
        {
            if (joint == null) return;
            joint.localRotation = Quaternion.Slerp(joint.localRotation, Quaternion.Euler(x, y, z), poseLerp * Time.deltaTime);
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
