using UnityEngine;
using MarioBasketball.Core;
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
        [Tooltip("Stride cadence (leg/arm swings per second) at a walk. This is purely the look of the gait — it does NOT change how fast the player actually moves (that's PlayerController). Kept fairly low so the legs don't churn in fast-forward.")]
        public float strideFrequency = 1.1f;
        [Tooltip("Extra stride cadence per m/s of speed. Low so a sprint reads as long strides, not a frantic shuffle.")]
        public float strideFrequencyPerSpeed = 0.15f;
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
        [Tooltip("How fast the body whips into a post-shot turn (hook / turnaround / drop step). Higher than poseLerp so the turn lands before the quick release, instead of lagging behind it.")]
        public float postTurnLerp = 30f;
        public float fallAngle = 80f;
        [Tooltip("Body lean (degrees) at a full fadeaway jump shot.")]
        public float fadeLeanAngle = 24f;
        [Tooltip("A loose/shot ball within this of an airborne player makes them reach for the board (vs hands straight up to contest).")]
        public float reboundReachDistance = 2.6f;
        [Tooltip("How far the post defender leans their chest/forearm into the poster.")]
        public float postDefendLean = 16f;
        [Tooltip("Forward torso lean (deg) the poster sinks into while backing their man down — an athletic stance, chest over the thighs, instead of standing bolt upright.")]
        public float postPosterLean = 15f;

        [Header("Bench")]
        [Tooltip("How long a benched player claps after their team scores.")]
        public float benchClapTime = 2.2f;

        [Header("Post shot form")]
        [Tooltip("How far (deg) the body turns sideways to the rim on a hook shot.")]
        public float hookBodyTurn = 95f;
        [Tooltip("Off-arm shoulder raise while barring out space on a hook.")]
        public float hookGuardArmDegrees = -72f;
        [Tooltip("Off-arm elbow bend (≈90°) while barring out space on a hook.")]
        public float hookGuardElbowDegrees = -100f;

        [Header("Hand grip")]
        [Tooltip("How far the fingers curl (deg) into a closed grip when grabbing the rim on a dunk.")]
        public float gripCloseDegrees = 95f;
        [Tooltip("How far the thumb curls in (deg) to clamp the grip shut around the rim.")]
        public float thumbCloseDegrees = 60f;
        [Tooltip("How fast the hands open/close. High so the grip snaps onto the rim as the slam lands.")]
        public float gripLerp = 22f;

        [Header("Layup / finish")]
        [Tooltip("Shooting-arm raise (deg) as a one-hand layup lays the ball up off the glass.")]
        public float layupReleaseArmDegrees = -175f;
        [Tooltip("Shoulder the off-hand rides at while it stays on the side of the ball through the layup (until release).")]
        public float layupGuideArmDegrees = -110f;
        public float layupGuideElbowDegrees = -55f;

        [Header("Finish air-adjust")]
        [Tooltip("Shooting-arm pitch the windmill starts from (0 = arm hanging down, matching the ball's gather).")]
        public float windmillStartDegrees = 0f;
        [Tooltip("Total sweep (deg) the shooting arm circles on a windmill. Keep at -360 (one full loop) to track the ball's windmill arc in PlayerController.CarriedBallPoint.")]
        public float windmillSweepDegrees = -360f;
        [Tooltip("Shooting-arm extension (deg) on a low scoop — out in front and low, not overhead.")]
        public float lowScoopArmDegrees = -70f;
        public float lowScoopElbowDegrees = -38f;

        PlayerController _pc;
        BallController _ball;
        Transform _model, _armL, _armR, _elbowL, _elbowR, _wristL, _wristR, _legL, _legR, _kneeL, _kneeR;
        Transform _fingersL, _fingersR, _thumbL, _thumbR;
        float _gripL, _gripR;
        float _phase;
        float _fallTilt;
        bool _wasShooting;
        float _followThrough;
        int _lastTeamScore = -1;
        float _benchClapTimer;

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
            _fingersL = FindDeep(transform, "JointFingersL");
            _fingersR = FindDeep(transform, "JointFingersR");
            _thumbL = FindDeep(transform, "JointThumbL");
            _thumbR = FindDeep(transform, "JointThumbR");
            _fallTilt = Random.Range(-25f, 25f);
        }

        void LateUpdate()
        {
            if (_pc == null) return;
            if (_ball == null && MarioBasketball.Core.GameManager.Instance != null)
                _ball = MarioBasketball.Core.GameManager.Instance.ball;

            // The poster (if any) currently backing this player down on D.
            PlayerController postingMe = _pc.PostingMeOnD;
            // A poster shorter than the defender is guarded differently: you can't
            // legally lean a forearm into a smaller man, so the D stays vertical
            // with hands straight up (still in a stance).
            bool posterShorter = postingMe != null && postingMe.BodyHeight < _pc.BodyHeight - 0.05f;

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
                    else if (postingMe != null && !posterShorter)
                    {
                        // Engaged on the back-down against a taller-or-equal poster:
                        // lean the chest/forearm into them. (A shorter poster: stay
                        // vertical — falls through to identity, hands go up below.)
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
                        // back, the power drop step squares up to bury it. (Checked
                        // before the plain posting lean, since IsPosting is still
                        // true through the shot — otherwise the move never reads.)
                        var move = _pc.CurrentPostMove;
                        float turn =
                            move == PostMove.Hook || move == PostMove.SkyHook ? hookBodyTurn :
                            move == PostMove.TurnaroundJumper ? 165f :
                            move == PostMove.DropStep || move == PostMove.PowerDropStep ? 130f :
                            100f; // spin / up-and-under
                        float lean = move == PostMove.TurnaroundJumper ? -fadeLeanAngle : 0f;
                        want = Quaternion.Euler(lean, turn, 0f);
                    }
                    else if (_pc.IsPosting)
                    {
                        // Backing your man down: sit into an athletic stance with the
                        // chest forward over the thighs, not standing bolt upright.
                        want = Quaternion.Euler(postPosterLean, 0f, 0f);
                    }
                    else if (!_pc.IsAirborne && !_pc.IsPosting && !_pc.IsHanging && !_pc.IsSkyingForOop
                             && !_pc.IsFinishing && _pc.PlanarSpeed > 0.6f)
                    {
                        // Lean into the run, scaled by speed — drives the whole gait.
                        want = Quaternion.Euler(runLeanAngle * Mathf.Clamp01(_pc.PlanarSpeed / 7f), 0f, 0f);
                    }
                    else want = Quaternion.identity;
                    // Post shots whip into their turn fast so the body is squared up
                    // for the release instead of still rotating behind it.
                    float turnRate = _pc.IsPostShooting ? postTurnLerp : poseLerp;
                    _model.localRotation = Quaternion.Slerp(_model.localRotation, want, turnRate * Time.deltaTime);
                }
            }
            if (_pc.IsFallen) return;

            // Benched players just sit on the sideline (arms at their sides) and
            // clap when their team scores — they aren't running the live gameplay
            // poses (which would leave them floating with their arms up).
            if (_pc.Character != null && _pc.Character.IsBenched) { BenchIdle(); return; }

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
            // Sit into an athletic stance: the poster sinks as they back down, and
            // the engaged defender bends their knees and braces (even against a
            // shorter poster they stay down in a stance — just with hands up).
            if (!_pc.IsAirborne && (_pc.IsPosting || postingMe != null))
            {
                SetX(_kneeL, 40f);
                SetX(_kneeR, 40f);
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
            else if (_pc.IsFinishing && _pc.IsAirborne)
            {
                // Two-foot takeoff: both legs rise together (no running counter-
                // swing), trailing slightly and tucking up as the player gathers to
                // the rim. They stay matched the whole way up.
                float rp = _pc.FinishRiseProgress01;
                float tuck = Mathf.Lerp(8f, 52f, rp);
                SetX(_legL, -10f); SetX(_legR, -10f);
                SetX(_kneeL, tuck); SetX(_kneeR, tuck);
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
            else if (_pc.IsFinishing)
            {
                FinishArms();
            }
            else if (_pc.IsPostFaking)
            {
                // Pump fake: snap the ball up toward a shooting set and back down,
                // both hands going up with it (0 → 1 → 0).
                float pf = Mathf.Sin(Mathf.PI * _pc.PostFake01);
                float arm = Mathf.Lerp(holdArmDegrees, -150f, pf);
                float elb = Mathf.Lerp(holdElbowDegrees, -18f, pf);
                float wr = Mathf.Lerp(holdWristDegrees, -28f, pf);
                Pose(_armR, _elbowR, _wristR, arm, elb, wr);
                Pose(_armL, _elbowL, _wristL, arm, elb, wr);
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
                if (posterShorter)
                {
                    // Defending a smaller man backing in: stay tall, both hands
                    // straight up — verticality instead of a forearm in the back.
                    Pose(_armR, _elbowR, _wristR, -178f, -6f, 0f);
                    Pose(_armL, _elbowL, _wristL, -178f, -6f, 0f);
                }
                else
                {
                    // Bracing a taller-or-equal poster: the right forearm bars into
                    // their back (~90° bend) while the off-hand rides up for balance.
                    Pose(_armR, _elbowR, _wristR, -44f, -95f, 0f);
                    Pose(_armL, _elbowL, _wristL, -150f, -22f, 0f);
                }
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

            // The hands close into a grip to grab the rim: they ramp shut over the
            // tail of a dunk's slam (so they clamp as the ball goes down) and stay
            // locked through the rim hang, then spring back open. A one-hand flush
            // only grips with the dunking (right) hand; the off-hand stays a guard.
            bool oneHandFlush = _pc.CurrentFinishStyle == FinishStyle.OneFootOneHandDunk;
            float wantGripR = 0f, wantGripL = 0f;
            if (_pc.IsHanging)
            {
                wantGripR = 1f;
                wantGripL = oneHandFlush ? 0f : 1f;
            }
            else if (_pc.IsFinishing && _pc.FinishIsDunk && _pc.IsSlammingFinish)
            {
                float g = Mathf.Clamp01((_pc.FinishSlamProgress01 - 0.6f) / 0.4f);
                wantGripR = g;
                wantGripL = oneHandFlush ? 0f : g;
            }
            _gripR = Mathf.Lerp(_gripR, wantGripR, gripLerp * Time.deltaTime);
            _gripL = Mathf.Lerp(_gripL, wantGripL, gripLerp * Time.deltaTime);
            ApplyGrip(_fingersR, _thumbR, _gripR);
            ApplyGrip(_fingersL, _thumbL, _gripL);
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

        /// <summary>All in-air arm work for a dunk/layup: the gather→extend on the
        /// way up, the chosen air-adjust contort (windmill / switch hands / low
        /// scoop), and the release. A one-hand layup finishes with the hand opposite
        /// the takeoff foot and keeps the off-hand on the side of the ball until the
        /// release; a two-foot gather goes up with both hands and releases right.</summary>
        void FinishArms()
        {
            bool shL = _pc.ShootHandLeft;
            Transform shArm = shL ? _armL : _armR, shElb = shL ? _elbowL : _elbowR, shWr = shL ? _wristL : _wristR;
            Transform ofArm = shL ? _armR : _armL, ofElb = shL ? _elbowR : _elbowL, ofWr = shL ? _wristR : _wristL;

            // Release / lay-in: drive a dunk down through the rim, or flick a layup
            // up off the glass. The off-hand (which rode the ball) drops to a guard.
            if (_pc.IsSlammingFinish)
            {
                float p = _pc.FinishSlamProgress01;
                if (_pc.FinishIsDunk)
                {
                    float sh = Mathf.Lerp(dunkArmDegrees, -38f, p);
                    float el = Mathf.Lerp(dunkElbowDegrees, -8f, p);
                    float wr = Mathf.Lerp(0f, dunkWristDegrees, p);
                    Pose(_armR, _elbowR, _wristR, sh, el, wr);
                    if (_pc.CurrentFinishStyle != FinishStyle.OneFootOneHandDunk)
                        Pose(_armL, _elbowL, _wristL, sh, el, wr);
                    else
                        Pose(_armL, _elbowL, _wristL, guardArmDegrees, dribbleElbowBent, 0f);
                }
                else
                {
                    // Lay it up high and flick the wrist (gooseneck) — not slammed down.
                    Pose(shArm, shElb, shWr,
                        Mathf.Lerp(layupArmDegrees, layupReleaseArmDegrees, p),
                        Mathf.Lerp(layupElbowDegrees, -6f, p),
                        Mathf.Lerp(0f, releaseWristDegrees, p));
                    Pose(ofArm, ofElb, ofWr, guardArmDegrees, dribbleElbowBent, 0f);
                }
                return;
            }

            // Air-adjust contort on the way up (before the release).
            if (_pc.IsAdjustingFinish)
            {
                switch (_pc.CurrentAdjustMove)
                {
                    case AdjustMove.Windmill:
                        // The shooting arm circles a big loop to carry the ball around
                        // the block; the off-arm tucks in for balance.
                        Pose(shArm, shElb, shWr,
                            windmillStartDegrees + windmillSweepDegrees * _pc.FinishRiseProgress01, -10f, 0f);
                        Pose(ofArm, ofElb, ofWr, guardArmDegrees, dribbleElbowBent, 0f);
                        return;
                    case AdjustMove.LowRelease:
                        // Drop the ball to a low scoop out front, shielded by the off-hand.
                        Pose(shArm, shElb, shWr, lowScoopArmDegrees, lowScoopElbowDegrees, gatherWristDegrees);
                        Pose(ofArm, ofElb, ofWr, layupGuideArmDegrees, layupGuideElbowDegrees, 0f);
                        return;
                    default: // SwitchHands — finish on the (already-switched) shooting side
                        Pose(shArm, shElb, shWr, layupArmDegrees, layupElbowDegrees, gatherWristDegrees);
                        Pose(ofArm, ofElb, ofWr, layupGuideArmDegrees, layupGuideElbowDegrees, gatherWristDegrees * 0.5f);
                        return;
                }
            }

            // Straight layups (no adjust).
            if (!_pc.FinishIsDunk)
            {
                if (_pc.FinishOneFoot)
                {
                    // One-hand layup: the shooting hand rises to lay it up while the
                    // off-hand stays on the SIDE of the ball (a guide) until release.
                    Pose(shArm, shElb, shWr, layupArmDegrees, layupElbowDegrees, gatherWristDegrees);
                    Pose(ofArm, ofElb, ofWr, layupGuideArmDegrees, layupGuideElbowDegrees, gatherWristDegrees * 0.5f);
                }
                else
                {
                    // Two-foot gather: both hands carry the ball up together (it
                    // releases from the shooting hand in the lay-in above).
                    float rp = _pc.FinishRiseProgress01;
                    float arm = Mathf.Lerp(holdArmDegrees, -160f, rp);
                    float elb = Mathf.Lerp(holdElbowDegrees, -12f, rp);
                    Pose(_armR, _elbowR, _wristR, arm, elb, 0f);
                    Pose(_armL, _elbowL, _wristL, arm, elb, 0f);
                }
                return;
            }

            // Straight dunks.
            if (_pc.CurrentFinishStyle == FinishStyle.OneFootOneHandDunk)
            {
                // One-handed flush: cock the ball overhead in the dunking hand.
                Pose(_armR, _elbowR, _wristR, dunkArmDegrees, dunkElbowDegrees, dunkWristDegrees);
                Pose(_armL, _elbowL, _wristL, guardArmDegrees, dribbleElbowBent, 0f);
                return;
            }

            // Two-hand dunk: gather the ball low and EXTEND it overhead as you rise
            // to the rim — not held up the whole way. Wrists stay neutral until the
            // slam drives it down through the rim.
            {
                float rp = _pc.FinishRiseProgress01;
                float arm = Mathf.Lerp(holdArmDegrees, dunkArmDegrees, rp);
                float elb = Mathf.Lerp(holdElbowDegrees, dunkElbowDegrees, rp);
                float wr = Mathf.Lerp(holdWristDegrees, 0f, rp);
                Pose(_armR, _elbowR, _wristR, arm, elb, wr);
                Pose(_armL, _elbowL, _wristL, arm, elb, wr);
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

        /// <summary>Seated-on-the-bench pose: thighs forward, knees bent, body
        /// upright, hands resting at the sides — and a quick clap whenever their
        /// team's score ticks up.</summary>
        void BenchIdle()
        {
            // Watch our team's score; a bump kicks off a celebratory clap.
            var gm = GameManager.Instance;
            if (gm != null)
            {
                int score = _pc.team == TeamSide.Home ? gm.HomeScore : gm.AwayScore;
                if (_lastTeamScore < 0) _lastTeamScore = score;
                else if (score > _lastTeamScore) { _benchClapTimer = benchClapTime; _lastTeamScore = score; }
            }
            if (_benchClapTimer > 0f) _benchClapTimer -= Time.deltaTime;

            // Sit: thighs forward off the bench, knees bent so the shins drop.
            SetX(_legL, -52f); SetX(_legR, -52f);
            SetX(_kneeL, 66f); SetX(_kneeR, 66f);

            if (_benchClapTimer > 0f)
            {
                // Hands meet in front of the chest and clap, opening and closing fast.
                float t = Mathf.Sin(Time.unscaledTime * 18f) * 0.5f + 0.5f;
                float elbow = Mathf.Lerp(-72f, -118f, t);
                Pose(_armL, _elbowL, _wristL, -48f, elbow, 0f);
                Pose(_armR, _elbowR, _wristR, -48f, elbow, 0f);
            }
            else
            {
                // Arms hanging at the sides.
                Pose(_armL, _elbowL, _wristL, 0f, idleElbowDegrees, 0f);
                Pose(_armR, _elbowR, _wristR, 0f, idleElbowDegrees, 0f);
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

        /// <summary>Curl a hand toward a closed grip (0 = open, 1 = clamped shut):
        /// the fingers hook in at the knuckle and the thumb closes to meet them.
        /// A negative X rotation swings them toward the palm. The smoothing already
        /// lives in <c>grip01</c>, so the pivots are set straight to the target.</summary>
        void ApplyGrip(Transform fingers, Transform thumb, float grip01)
        {
            if (fingers != null) fingers.localRotation = Quaternion.Euler(-grip01 * gripCloseDegrees, 0f, 0f);
            if (thumb != null) thumb.localRotation = Quaternion.Euler(-grip01 * thumbCloseDegrees, 0f, 0f);
        }

        static Transform FindDeep(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }
    }
}
