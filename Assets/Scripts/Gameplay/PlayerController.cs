using UnityEngine;
using MarioBasketball.Core;
using MarioBasketball.Characters;
using MarioBasketball.InputControl;

namespace MarioBasketball.Gameplay
{
    /// <summary>
    /// A player's body and actions. Movement and actions are driven by an
    /// <i>intent</i> (a move vector plus action triggers) fed by either the
    /// human (<see cref="InputReader"/>) or a <c>PlayerAI</c> brain. Built on a
    /// <see cref="CharacterController"/> for crisp, arcade movement.
    ///
    /// Outcomes are stat-driven: speed (Speed), shot accuracy (3-Point / Mid
    /// Range / Inside Scoring), contests (Perimeter/Post Defense, Blocks),
    /// steals (Steals vs Ball Handling) and the post game (see
    /// <see cref="PostUpController"/>, Power + Post Offense/Defense).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PostUpController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Identity")]
        public TeamSide team = TeamSide.Home;
        [Tooltip("The human-controlled player reads input; others are AI-driven.")]
        public bool isHuman = false;

        [Header("Movement (mapped from the Speed stat)")]
        [Tooltip("Run speed (m/s) at Speed 1 — a slow player. The wide gap to maxMoveSpeed makes fast vs slow clearly matter.")]
        public float minMoveSpeed = 3.3f;
        [Tooltip("Run speed (m/s) at Speed 10 — a burner.")]
        public float maxMoveSpeed = 7.8f;
        public float sprintMultiplier = 1.4f;
        [Tooltip("Turbo bar drain per second while sprinting. Flat — not affected by any stat yet.")]
        public float turboDrainPerSec = 0.5f;
        [Tooltip("Turbo bar recovery per second while not sprinting. Flat — not affected by any stat yet.")]
        public float turboRegenPerSec = 0.35f;
        public float turnSpeed = 720f;
        public float gravity = -25f;
        public float jumpHeight = 1.4f;

        [Header("Ball handling")]
        [Tooltip("Distance from the basket beyond which a make is worth 3.")]
        public float threePointDistance = 6.75f;
        [Tooltip("Within this radius a shot uses Inside Scoring, not Mid Range.")]
        public float paintRadius = 2.5f;
        [Tooltip("Press shoot within this of the rim to drive in for a dunk/layup (sky for it from in the paint), not pull up for a jumper.")]
        public float finishRange = 3.2f;

        [Header("Shooting")]
        [Tooltip("Make odds, distance falloff and contest live in ShotMath.")]
        public float shotFlightTime = 1.1f;
        public float passPower = 9f;
        [Tooltip("Hold Pass at least this long for a hard pass; shorter is a loft.")]
        public float passHoldThreshold = 0.25f;
        [Tooltip("Flight time of a tapped loft pass (slow, arcs over defenders).")]
        public float loftPassTime = 0.85f;
        [Tooltip("Flight time of a held hard pass (fast, flat, stealable).")]
        public float hardPassTime = 0.28f;
        [Tooltip("Right-stick magnitude needed to aim a directed pass.")]
        public float passAimDeadzone = 0.5f;
        [Tooltip("Lead-pass spread (m) at Ball Handling 1 / 10 — low handles miss.")]
        public float passErrorMax = 1.3f;
        public float passErrorMin = 0.05f;

        [Header("Shot timing (jump shots only; layups/dunks are instant)")]
        [Tooltip("Release within this many seconds of the jump's apex for a perfect shot.")]
        public float perfectReleaseWindow = 0.07f;
        [Tooltip("Make% multiplier lost per second of mistiming beyond the window.")]
        public float timingFalloffPerSec = 2f;
        [Range(0f, 1f)] public float minTimingMultiplier = 0.35f;
        [Tooltip("Auto-release this long after the apex if the button is still held.")]
        public float shotAutoReleaseAfterApex = 0.45f;
        [Tooltip("Jump shots float: gravity is softened to this fraction while a jump shot is in the air, so the shooter hangs — the shot plays out more smoothly and there's more time to time the release (it stops feeling fast-forwarded).")]
        [Range(0.3f, 1f)] public float shotGravityScale = 0.45f;
        [Tooltip("Jump-shot leap height (m) — a touch higher than a normal jump so the shooter elevates and hangs into the release.")]
        public float shotJumpHeight = 1.7f;
        [Tooltip("How high a post shot (hook, drop step, turnaround…) hops off the floor — post shots leap and hang like a jumper instead of being flat-footed.")]
        public float postShotJumpHeight = 0.7f;
        [Tooltip("Sky-hook leap height (m) — released up high, so it skies more than a normal post hop.")]
        public float skyHookJumpHeight = 1.1f;
        [Tooltip("Catch-and-shoot window for the quick-catch shooter trait.")]
        public float quickCatchWindow = 0.3f;
        [Tooltip("Window to shoot off a Playmaker's pass for the +2 assist bonus.")]
        public float assistWindow = 1.0f;
        [Tooltip("Acrobat trait (Baby Mario): fraction of the shot-mistiming penalty he ignores — 0.8 = suffers 80% less from early/late releases.")]
        [Range(0f, 1f)] public float acrobatTimingRelief = 0.8f;

        [Header("Hidden traits")]
        [Tooltip("Killer Instinct (Daisy): bonus to Mid/3PT/Inside/Perimeter-D at full opponent fatigue (Mid can reach 11, the rest cap at 10).")]
        public float killerMaxBonus = 4f;
        [Tooltip("Killer Instinct: opponent fatigue below this fraction gives no bonus (fresh legs).")]
        [Range(0f, 1f)] public float killerFatigueFloor = 0.1f;
        [Tooltip("Called Shot (Delfan): guaranteed makes allowed per game.")]
        public int calledShotMax = 2;
        [Tooltip("Called Shot: only shots launched within this distance (m) of the hoop — i.e. within half court — qualify.")]
        public float calledShotRange = 14f;
        [Tooltip("Called Shot: how long the on-screen callout (\"CALLED SHOT!\" etc.) lingers.")]
        public float calledShotCalloutTime = 1.4f;
        [Tooltip("Min planar speed (m/s) to count as actively dribbling.")]
        public float dribbleMoveThreshold = 0.6f;

        [Header("Fadeaway / lean (jump shots)")]
        [Tooltip("Hold the move stick during a jump shot to fade that way; release nothing and it's a straight-up shot. Drift speed (m/s) at a full-held stick.")]
        public float fadeSpeed = 3.2f;
        [Tooltip("Fraction of the defender's block chance removed at a full fade (the separation a fadeaway buys).")]
        [Range(0f, 1f)] public float fadeBlockReduction = 0.6f;
        [Tooltip("Fraction of the defender's contest make-penalty removed at a full fade.")]
        [Range(0f, 1f)] public float fadeContestReduction = 0.7f;
        [Tooltip("Fade multiplier when leaning fully AGAINST your run direction at top speed (leaning WITH your momentum stays full). Lower = momentum matters more; 1 disables the asymmetry.")]
        [Range(0f, 1f)] public float fadeAgainstMomentumMin = 0.25f;

        [Header("Inside finishing (dunk / layup)")]
        [Tooltip("Max time in the air before a finish auto-resolves (the late cap). Longer = more arcade hang time at the rim and more room to air-adjust.")]
        public float finishAirTime = 0.95f;
        [Tooltip("Minimum air time before a finish can release — you always leave the floor and rise toward the rim first.")]
        public float finishMinAirTime = 0.22f;
        [Tooltip("Once you've driven this close to the rim (and reached the top of the leap) the finish slams right at the basket. Roomy enough that a body driving in reliably registers 'at the rim' and dunks, instead of stalling just outside it.")]
        public float finishReleaseDistance = 0.7f;
        [Tooltip("How hard you attack the rim on a finish (m/s toward the hoop).")]
        public float finishApproachSpeed = 7f;
        [Tooltip("Flight time of a layup once it leaves the hand at the rim — a soft drop off the glass.")]
        public float finishFlightTime = 0.32f;
        [Tooltip("Flight time of a dunk — very short, so it's slammed straight down through the rim from above instead of lofted in.")]
        public float dunkFlightTime = 0.18f;
        [Tooltip("Duration of the slam / lay-in: after skying up, the hand drives the ball the last stretch down into the rim (ball still in hand) and only lets go once it's there.")]
        public float finishSlamTime = 0.16f;
        [Tooltip("How long a dunk grabs the rim and hangs (the two-hand slam hangs longer).")]
        public float dunkHangTime = 0.3f;
        // A dunk/layup now leaves the floor at the SAME rise speed as a jump shot
        // (ShotTakeoffVelocity); finish gravity is derived per-jump from that speed
        // and the target apex (FinishJumpHeight) in StartFinish, so taller leaps
        // simply float longer rather than taking off faster.
        [Tooltip("Layup hop height (m) — gets up off the floor enough to finish at/above the rim, arcade style.")]
        public float layupJumpHeight = 1.4f;
        [Tooltip("Dunk leap height (m) at Dunk 10 — arcade air, soaring over the rim. Scales up from the normal jump by the Dunk stat.")]
        public float dunkJumpHeightMax = 2.4f;
        [Tooltip("Contest leap height (m) at Blocks 10 — a great shot-blocker matches a big dunker in the air.")]
        public float contestJumpHeightMax = 2.4f;
        [Tooltip("How fast the slammer settles onto the rim for the hang.")]
        public float hangSettleLerp = 12f;
        [Header("Air-adjust (hold LB on a finish)")]
        [Tooltip("Lateral air-control speed (m/s) while adjusting a finish — steer left/right to swing around a shot-blocker and finish from the other side.")]
        public float finishAirControlSpeed = 4.5f;
        [Tooltip("While adjusting, the rim is approached this much slower so the steer has room to reposition you (the auto-pull to the rim eases off).")]
        [Range(0f, 1f)] public float finishAdjustApproachScale = 0.4f;
        [Tooltip("Extra hang time (s) added to the finish air-time cap while adjusting, so there's room to maneuver before it resolves.")]
        public float finishAdjustExtraAir = 0.5f;
        [Tooltip("Gravity multiplier applied on top of the finish float while adjusting (lower = hangs longer to avoid the block).")]
        [Range(0.3f, 1f)] public float finishAdjustFloat = 0.7f;
        [Tooltip("Effective Dunk at/above this goes up for a dunk; below, a layup.")]
        public float dunkThreshold = 5f;
        [Tooltip("Dunk block resistance per point of Power.")]
        public float dunkPowerBlockResist = 0.3f;
        [Tooltip("Block chance multiplier when the shot is air-adjusted (fallback when no specific contort is picked).")]
        [Range(0f, 1f)] public float adjustBlockReduction = 0.4f;
        [Tooltip("Max make% lost to an air-adjust (mitigated by Inside Scoring).")]
        [Range(0f, 1f)] public float maxAdjustPenalty = 0.35f;
        [Tooltip("Block multiplier for a windmill contort — clears the most space.")]
        [Range(0f, 1f)] public float windmillBlockMult = 0.3f;
        [Tooltip("Block multiplier for a switch-hands contort.")]
        [Range(0f, 1f)] public float switchHandsBlockMult = 0.5f;
        [Tooltip("Block multiplier for a low-scoop contort.")]
        [Range(0f, 1f)] public float lowReleaseBlockMult = 0.55f;
        [Tooltip("Make-penalty weight for a windmill (>1 = harder than a plain adjust).")]
        public float windmillPenaltyWeight = 1.3f;
        [Tooltip("Make-penalty weight for a low scoop (<1 = safer than a plain adjust).")]
        public float lowReleasePenaltyWeight = 0.7f;
        [Tooltip("Defender lateral offset (× body height) beyond which an adjust switches hands to finish away from them.")]
        public float adjustSideThreshold = 0.4f;
        [Tooltip("Chance a non-dunk layup gathers off both feet (a power layup / floater) instead of one foot.")]
        [Range(0f, 1f)] public float layupBothFeetChance = 0.25f;
        [Tooltip("How far to the shooting-hand side a one-hand layup carries the ball (× body height).")]
        public float layupBallSideOffset = 0.14f;
        [Header("Alley-oop")]
        [Tooltip("A loft to a teammate skying within this of the rim becomes an alley-oop.")]
        public float oopRange = 3.0f;
        public float oopFlightTime = 0.65f;
        [Tooltip("How high a player skies when calling for an alley-oop (well above the rim).")]
        public float oopSkyHeight = 2.2f;
        [Tooltip("How long the sky hangs (gravity is softened) so there's time to lob it.")]
        public float oopSkyHang = 0.85f;
        [Tooltip("Gravity multiplier while hanging on an oop sky (lower = floats longer).")]
        [Range(0.1f, 1f)] public float oopSkyGravityScale = 0.4f;
        [Tooltip("Make% bonus on an alley-oop finish (it's a high-percentage play).")]
        [Range(0f, 1f)] public float alleyOopBonus = 0.2f;

        [Header("Dribble move (Ball Handling vs Perimeter Defense)")]
        public float dribbleRange = 2.0f;
        public float dribbleCooldownTime = 0.8f;
        public float dribbleBoostTime = 0.5f;
        public float dribbleBoostMult = 1.3f;
        [Tooltip("How long the beaten defender is frozen on a successful move.")]
        public float ankleStun = 0.9f;
        public float dribbleBaseChance = 0.45f;
        public float dribbleStatScale = 0.06f;

        [Header("Dribble flicks (right-stick hard dribbles for separation)")]
        public float flickCooldownTime = 0.45f;
        [Tooltip("Burst impulse on a flick toward / across the defender.")]
        public float flickBurstPower = 4f;
        [Tooltip("Backward impulse on a step-back (flick away from the basket).")]
        public float stepBackPower = 5f;
        [Tooltip("Opposite lateral flicks within this window chain into a hesitation cross.")]
        public float hesitationWindow = 0.6f;
        [Tooltip("How long the defender freezes when a flick move shakes them (the full ankleStun is reserved for the hesitation cross).")]
        public float flickFreeze = 0.4f;
        [Tooltip("How tightly the on-ball defender rides a right-stick flick at an even matchup (0 = lets you blow by, 1 = glued to you). Scales up with their Perimeter Defense.")]
        public float flickKeepUpBase = 0.45f;
        [Tooltip("Per point of (Perimeter Defense − Ball Handling), how much tighter the defender stays attached on a flick.")]
        public float flickKeepUpScale = 0.06f;

        [Header("Block (defense on a shot; contest % lives in ShotMath)")]
        public float contestRange = 3f;
        public float blockRange = 1.1f;
        public float blockBaseChance = 0.04f;
        public float blockStatScale = 0.05f;
        public float blockMaxChance = 0.5f;
        public float blockKnockPower = 4f;
        [Tooltip("How long the swat / snatch arm-swing animates after a block lands.")]
        public float blockGestureTime = 0.5f;
        [Tooltip("How long an airborne blocker hangs at the top of the contest after swatting (the Mario stall).")]
        public float blockHangTime = 0.45f;
        [Tooltip("Base chance a block is a clean two-handed snatch (their possession) vs a one-handed swat (loose ball). Scales up a little with Blocks.")]
        [Range(0f, 1f)] public float blockTwoHandBaseChance = 0.35f;

        [Header("Steal (Steals vs Ball Handling)")]
        [Tooltip("How far you can reach in to poke at the ball (body separation keeps players ~0.8 m apart, so this needs headroom above that).")]
        public float stealReach = 1.25f;
        public float stealCooldown = 1.0f;
        public float stealWhiffCooldown = 0.35f;
        [Tooltip("How long the arm-swipe animation holds after a steal attempt.")]
        public float stealGestureTime = 0.28f;
        public float stealBaseChance = 0.04f;
        public float stealStatScale = 0.035f;
        public float stealMinChance = 0.02f;
        public float stealMaxChance = 0.4f;

        [Header("Dive / shove")]
        public float diveDuration = 0.5f;
        public float diveSpeed = 9f;
        public float diveBallSeekRange = 6f;
        public float shoveDuration = 0.35f;

        [Header("Post separation moves (spin / power drop step → drive out)")]
        [Tooltip("Burst speed (m/s) a spin / power drop step drives toward the rim as it breaks out of the post. Kept modest so the move reads before you finish.")]
        public float postDriveBurstSpeed = 4f;
        [Tooltip("How long the spin whirl / drop-step lunge animates as the player breaks out of the post to finish — long enough to actually see the move.")]
        public float postMoveDriveTime = 0.6f;

        [Header("Push / foul (Power)")]
        public float pushRange = 1.7f;
        public float pushCooldown = 0.8f;
        public float pushWhiffCooldown = 0.3f;
        public float pushForce = 7f;
        [Tooltip("Power advantage at/above which the push knocks the target down.")]
        public float pushKnockdownPowerGap = 4f;
        public float pushKnockLooseBase = 0.2f;
        public float pushKnockLooseScale = 0.06f;

        [Header("Animation gestures (cosmetic timers read by the animator)")]
        [Tooltip("How long the pass/throw arm pose holds after the ball leaves.")]
        public float passGestureTime = 0.28f;

        /// <summary>Where the carried ball sits — out in front, hip height,
        /// scaled to the character's body size.</summary>
        public Vector3 BallHoldPoint
        {
            get
            {
                float h = _cc != null ? _cc.height : 1.9f;
                return transform.position + transform.forward * (0.29f * h) + Vector3.up * (0.21f * h);
            }
        }

        Transform _modelTf;
        // Carried-ball offsets are built from the MODEL's facing (the body the
        // animator turns), not the controller transform — so the ball stays in the
        // hand when the body whips sideways for a hook / turnaround / spin.
        Vector3 MFwd => _modelTf != null ? _modelTf.forward : transform.forward;
        Vector3 MRight => _modelTf != null ? _modelTf.right : transform.right;

        /// <summary>Where a gathered (non-dribbled) ball is carried. During a
        /// jump shot it rises with the meter from the chest gather to an
        /// overhead set point (so the shot releases above the head, matching the
        /// arm pose); held high in both hands for a dunk/layup; at the hip
        /// otherwise.</summary>
        public Vector3 CarriedBallPoint
        {
            get
            {
                float h = BodyHeight;
                if (IsShooting)
                {
                    float k = Mathf.Clamp01(ShotChargeFraction / Mathf.Max(0.01f, ShotPerfectFraction));
                    Vector3 gather = transform.position + MFwd * (0.24f * h) + Vector3.up * (0.10f * h);
                    Vector3 set = transform.position + MFwd * (0.10f * h) + Vector3.up * (0.62f * h);
                    return Vector3.Lerp(gather, set, k);
                }
                if (IsFinishing)
                {
                    // Bring the ball up from the gather to overhead *as the player
                    // rises*, tracking the SAME rise progress the arms use
                    // (FinishRiseProgress01) so the ball stays glued to the hands
                    // instead of racing ahead of them on the way up.
                    float k = FinishRiseProgress01;
                    // Windmill adjust: the ball loops a big vertical circle beside the
                    // shooting shoulder as the player rises (the shooting hand follows
                    // it — see ProceduralAnimator.FinishArms; keep its windmill sweep at
                    // one full -360° loop to stay in sync). The slam drives it to the rim.
                    if (_finishAdjusted && _adjustMove == AdjustMove.Windmill && !_finishSlamming)
                    {
                        float loop = Mathf.PI * 2f * k;
                        float r = 0.36f * h;
                        float sideSign = _shootHandLeft ? -1f : 1f;
                        Vector3 shoulder = transform.position + Vector3.up * (0.35f * h) + MRight * (sideSign * 0.18f * h);
                        return shoulder + MFwd * (Mathf.Sin(loop) * r) + Vector3.up * (-Mathf.Cos(loop) * r);
                    }
                    float up = Mathf.Lerp(0.5f, _finishIsDunk ? 0.98f : 0.86f, k);
                    float fwd = Mathf.Lerp(0.18f, _finishIsDunk ? 0.10f : 0.16f, k);
                    // A low-scoop adjust keeps the ball down (waist/chest) instead of
                    // overhead, so it's released from under a high contest.
                    if (_finishAdjusted && _adjustMove == AdjustMove.LowRelease)
                        up = Mathf.Lerp(0.34f, 0.52f, k);
                    // A one-hand layup carries the ball out to the shooting-hand side;
                    // dunks and two-foot gathers keep it centred overhead.
                    float side = 0f;
                    if (!_finishIsDunk && _finishFoot != TakeoffFoot.Both)
                        side = (_shootHandLeft ? -1f : 1f) * layupBallSideOffset;
                    Vector3 hand = transform.position + MFwd * (fwd * h) + MRight * (side * h) + Vector3.up * (up * h);

                    // Slam phase: the hand drives the ball the last stretch down
                    // into the rim — it stays in the hand and only lets go once
                    // it's there (no early arc out of the hand).
                    if (_finishSlamming)
                    {
                        var hoop = GameManager.Instance != null ? GameManager.Instance.GetAttackingHoop(team) : null;
                        if (hoop != null)
                        {
                            float s = 1f - Mathf.Clamp01(_finishSlamTimer / Mathf.Max(0.01f, finishSlamTime));
                            Vector3 atRim = hoop.AimPoint + Vector3.up * (_finishIsDunk ? 0.04f : 0.12f);
                            return Vector3.Lerp(hand, atRim, s);
                        }
                    }
                    return hand;
                }
                if (IsPostShooting)
                {
                    float k = Mathf.Clamp01(PostShotChargeFraction / Mathf.Max(0.01f, PostShotPerfectFraction));
                    PostMove move = CurrentPostMove;
                    if (move == PostMove.Hook || move == PostMove.SkyHook)
                    {
                        // Hook: the ball rides up in the shooting hand, out to the
                        // side and arcing high over the head (not straight up).
                        Vector3 low = transform.position + MRight * (0.34f * h) + Vector3.up * (0.40f * h);
                        Vector3 high = transform.position + MRight * (0.16f * h) + Vector3.up * (0.98f * h);
                        return Vector3.Lerp(low, high, k);
                    }
                    // Power drop step / spin / up-and-under: gather low off the
                    // jump-stop, then drive the ball straight up to the rim.
                    Vector3 gather = transform.position + MFwd * (-0.12f * h) + Vector3.up * (0.26f * h);
                    Vector3 set = transform.position + Vector3.up * (0.70f * h);
                    return Vector3.Lerp(gather, set, k);
                }
                // Pump fake: jerk the ball up toward a shooting set and back down.
                if (IsPostFaking)
                {
                    float pump = Mathf.Sin(Mathf.PI * PostFake01); // 0 → 1 → 0
                    Vector3 low = transform.position + MFwd * (0.24f * h) + Vector3.up * (0.30f * h);
                    Vector3 high = transform.position + MFwd * (0.12f * h) + Vector3.up * (0.66f * h);
                    return Vector3.Lerp(low, high, pump);
                }
                // Snagged a board in the air — the ball is up in the raised hands.
                if (IsAirborne)
                    return transform.position + MFwd * (0.12f * h) + Vector3.up * (0.72f * h);
                return BallHoldPoint;
            }
        }

        public PlayerCharacter Character => _character;
        public PostUpController Post => _post;
        public bool HasBall => Ball != null && Ball.Holder == this;
        public bool IsPosting => _post != null && _post.IsPosting;
        public bool IsStunned => _stunTimer > 0f;
        /// <summary>Knocked down (ankle-broken / leveled) — sprawls on the floor.</summary>
        public bool IsFallen => _fallTimer > 0f;
        /// <summary>Dribbling: you've put the ball on the floor and are live with
        /// it. A fresh catch does NOT auto-dribble — you stay in triple-threat
        /// until you actually move with it. Once dribbling, simply stopping does
        /// not end it (you keep your dribble standing still); it ends when you
        /// shoot, finish, post up, leave your feet, get stunned, or lose the
        /// ball. Latched in <see cref="UpdateDribbleState"/>.</summary>
        public bool IsDribbling => _dribbling;
        /// <summary>Whether the ball should be bouncing as a live dribble. You keep
        /// your dribble in the post — backing down or posting up does NOT pick the
        /// ball up; it's only gathered when the post shot actually goes up.</summary>
        public bool IsDribblingBall => HasBall && (_dribbling || (IsPosting && !IsPostShooting)) && !IsPostFaking;
        /// <summary>Mid pump-fake in the post (drives the ball/arm pump gesture).</summary>
        public bool IsPostFaking => _post != null && _post.IsFaking;
        /// <summary>How far through the post pump-fake (0-1).</summary>
        public float PostFake01 => _post != null ? _post.FakeGesture01 : 0f;
        /// <summary>Briefly true right after a pass/throw (drives the throw pose).</summary>
        public bool IsPassing => _passGestureTimer > 0f;
        /// <summary>Briefly true while swiping for a steal (drives the swipe pose).</summary>
        public bool IsStealing => _stealGestureTimer > 0f;
        /// <summary>How far through the steal swipe (0-1).</summary>
        public float StealProgress01 =>
            stealGestureTime > 0.0001f ? Mathf.Clamp01(1f - _stealGestureTimer / stealGestureTime) : 0f;
        /// <summary>A flashy dribble move is mid-animation (drives its body pose).</summary>
        public bool IsDribbleMoveGesture => _dribbleMoveTimer > 0f;
        /// <summary>Which dribble move is currently animating.</summary>
        public DribbleMoveType CurrentDribbleMove => _dribbleMoveType;
        /// <summary>How far through the current dribble move's animation (0-1).</summary>
        public float DribbleMoveProgress01 =>
            _dribbleMoveDuration > 0.0001f ? Mathf.Clamp01(1f - _dribbleMoveTimer / _dribbleMoveDuration) : 0f;
        /// <summary>Contorting a finish in the air (L1 air-adjust) — alters the layup.</summary>
        public bool IsAdjustingFinish => _finishing && _finishAdjusted;
        /// <summary>Airborne for a dunk/layup (can air-adjust or pass).</summary>
        public bool IsFinishing => _finishing;
        public bool FinishIsDunk => _finishIsDunk;
        /// <summary>True during the slam/lay-in: the player hangs at the rim and
        /// drives the ball down into it (the ball stays in the hand until then).</summary>
        public bool IsSlammingFinish => _finishSlamming;
        /// <summary>How far through the slam/lay-in (0-1) — drives the arms down
        /// with the ball.</summary>
        public float FinishSlamProgress01 =>
            _finishSlamming ? 1f - Mathf.Clamp01(_finishSlamTimer / Mathf.Max(0.01f, finishSlamTime)) : 0f;
        /// <summary>How far through the up-leap a finisher is: 0 at takeoff, 1 at the
        /// top (and through the slam). Drives the gather→extend of a two-hand dunk —
        /// the ball is cradled low off the floor and reached overhead at the rim.</summary>
        public float FinishRiseProgress01 =>
            Mathf.Clamp01(1f - _verticalVelocity / Mathf.Max(0.01f, ShotTakeoffVelocity()));
        /// <summary>How the current finish looks (layup / one-foot dunk / slam).</summary>
        public FinishStyle CurrentFinishStyle => _finishStyle;
        /// <summary>This finish leaves off a single foot (vs a two-foot gather) —
        /// true for one-foot layups and the one-foot dunks.</summary>
        public bool FinishOneFoot => _finishFoot != TakeoffFoot.Both;
        /// <summary>Which foot the one-foot finish leaves from (drives the leg pose).</summary>
        public bool FinishTakeoffLeft => _finishFoot == TakeoffFoot.Left;
        /// <summary>Which hand finishes the layup: left-foot takeoff → right hand,
        /// right-foot → left hand, two-foot → right. A switch-hands air-adjust
        /// flips it to finish away from the defender.</summary>
        public bool ShootHandLeft => _shootHandLeft;
        /// <summary>How an air-adjust is contorting this finish (None when straight).</summary>
        public AdjustMove CurrentAdjustMove => _adjustMove;
        /// <summary>Hanging on the rim after a two-hand slam (held a beat).</summary>
        public bool IsHanging => _hangTimer > 0f;
        /// <summary>Mid swat / snatch just after blocking a shot — drives the
        /// block arm-swing animation.</summary>
        public bool IsBlocking => _blockGestureTimer > 0f;
        /// <summary>How far through the block swat (0-1).</summary>
        public float BlockProgress01 =>
            blockGestureTime > 0.0001f ? Mathf.Clamp01(1f - _blockGestureTimer / blockGestureTime) : 0f;
        /// <summary>The block in progress was a two-handed snatch (both hands clamp
        /// the ball) rather than a one-handed swat.</summary>
        public bool BlockTwoHanded => _blockTwoHanded;
        /// <summary>Skying for an alley-oop — up above the rim, hands ready, hanging.</summary>
        public bool IsSkyingForOop => _skyTimer > 0f;
        /// <summary>Seconds since this player gained the ball (0 if they don't have it).</summary>
        public float TimeWithBall => HasBall ? Time.time - _catchTime : 0f;
        /// <summary>True if this possession came from a rebound / loose-ball grab
        /// (not a caught pass) — only these get put-back / tip behaviour.</summary>
        public bool GainedFromRebound => _gainedFromRebound;
        /// <summary>The human is aiming a directed pass (the aim stick — left stick,
        /// or the right-stick override — is pushed past the deadzone).</summary>
        public bool IsAimingPass => _passAim.magnitude >= passAimDeadzone && HasBall;
        /// <summary>The teammate currently targeted by the pass aim (for icons).</summary>
        public PlayerController PassTarget => IsAimingPass ? TargetedTeammate(_passAim) : null;
        /// <summary>Holding the icon-pass modifier (LB) with the ball — show
        /// teammate icons and pass to one via a face button.</summary>
        public bool IconPassActive => _iconHeld && HasBall && !IsPosting && !IsFinishing;
        /// <summary>Physical body height (m), drives rebound reach.</summary>
        public float BodyHeight => _cc != null ? _cc.height : 1.8f;
        /// <summary>Turbo/boost reserve (0-1). Drains while sprinting, recovers
        /// otherwise — flat rates, not affected by any stat yet. Drives the HUD bar.</summary>
        public float Turbo01 => _turbo;
        public bool IsAirborne => _cc != null && !_cc.isGrounded;
        public bool IsDiving => _diveTimer > 0f;
        /// <summary>Current horizontal speed (m/s) — drives the run animation.</summary>
        public float PlanarSpeed { get; private set; }
        public bool IsShooting => _shooting;
        /// <summary>A post move's shot is mid-release (its timing meter is up).</summary>
        public bool IsPostShooting => _post != null && _post.PostShotActive;
        /// <summary>Post-shot meter fill (0-1), for the release-timing pose/feedback.</summary>
        public float PostShotChargeFraction => _post != null ? _post.PostShotChargeFraction : 0f;
        /// <summary>Where the perfect post-shot release sits on the meter (0-1).</summary>
        public float PostShotPerfectFraction => _post != null ? _post.PostShotPerfectFraction : 0f;
        /// <summary>Which post move is currently being shot — drives the distinct
        /// hook / power-drop-step / fadeaway body animation.</summary>
        public PostMove CurrentPostMove => _post != null ? _post.CurrentMove : PostMove.Hook;
        /// <summary>Mid spin / power-drop-step footwork — a move that beats the
        /// defender and drives you OUT of the post toward the rim (you finish it
        /// yourself). Lives here, not on the post, so it survives the post ending.
        /// Drives the spin/lunge body animation.</summary>
        public bool IsDoingPostMove => _postMoveGestureTimer > 0f;
        /// <summary>How far through the spin / drop-step footwork (0-1).</summary>
        public float PostMoveGesture01 =>
            postMoveDriveTime > 0.0001f ? Mathf.Clamp01(1f - _postMoveGestureTimer / postMoveDriveTime) : 0f;
        /// <summary>Which separation move is driving out of the post (spin vs power
        /// drop step) — picks the spin whirl vs the shoulder-down lunge animation.</summary>
        public PostMove PostMoveType => _postMoveType;
        /// <summary>This player has Delfan's Called Shot trait (the HUD shows charges).</summary>
        public bool HasCalledShot => HasTrait(HiddenTrait.CalledShot);
        /// <summary>Called Shot charges left this game.</summary>
        public int CalledShotsRemaining => HasCalledShot ? Mathf.Max(0, calledShotMax - _calledShotsUsed) : 0;
        /// <summary>Right now there's a callable shot in the air (within half court,
        /// charge to spend) — the HUD prompts the double-tap.</summary>
        public bool CanCallShotNow
        {
            get
            {
                if (!HasCalledShot || _calledShotsUsed >= calledShotMax) return false;
                var ball = Ball;
                return ball != null && ball.State == BallController.BallState.Shot
                    && ball.Shooter == this && _lastShotDistance <= calledShotRange;
            }
        }
        /// <summary>A brief Called-Shot message to surface (or null) — "CALLED SHOT!"
        /// on a make, or a nudge explaining why a double-tap didn't take.</summary>
        public string CalledShotCallout => _calledShotCalloutTimer > 0f ? _calledShotCallout : null;
        /// <summary>Fade weight (0-1) for the current callout, for the HUD to dim it out.</summary>
        public float CalledShotCallout01 =>
            calledShotCalloutTime > 0.0001f ? Mathf.Clamp01(_calledShotCalloutTimer / calledShotCalloutTime) : 0f;
        /// <summary>How hard the player is backing their man down (0 = holding,
        /// 1 = driving at full power) — sinks the post stance deeper as they go.</summary>
        public float PostDrive01 => (_post != null && _post.IsPosting && !IsPostShooting && _post.maxBackdownSpeed > 0.01f)
            ? Mathf.Clamp01(_post.Leverage * _post.leverageToSpeed / _post.maxBackdownSpeed) : 0f;
        /// <summary>World-space planar direction the current jump shot is fading
        /// toward (zero for a straight-up shot). Drives the body lean.</summary>
        public Vector3 FadeDirection => _fadeDir;
        /// <summary>How hard the shot is fading, 0 (straight up) to 1 (full lean).</summary>
        public float FadeAmount => _fadeAmount;
        /// <summary>How full the shot meter is (0-1) for the jump in progress.</summary>
        public float ShotChargeFraction => _shooting ? Mathf.Clamp01(_shotCharge / ShotMeterDuration) : 0f;
        /// <summary>Where the perfect-release marker sits on the meter (0-1).</summary>
        public float ShotPerfectFraction => Mathf.Clamp01(_apexTime / ShotMeterDuration);
        float ShotMeterDuration => Mathf.Max(0.01f, _apexTime + shotAutoReleaseAfterApex);

        CharacterController _cc;
        PlayerCharacter _character;
        PostUpController _post;
        InputReader _input;
        Camera _cam;
        bool _dribbling;
        float _verticalVelocity;
        bool _wasPostShooting;   // rising edge → leap into the post shot
        Vector2 _moveIntent;
        bool _sprintIntent;
        bool _sprintingNow;
        float _postMoveGestureTimer; // spin / power-drop footwork driving out of the post
        PostMove _postMoveType;
        bool _postRepostBlocked;     // after a drive-out, block re-posting until RB is released
        float _turbo = 1f;
        float _stealCooldown;
        float _stunTimer;
        float _diveTimer;
        Vector3 _diveDir;
        Vector3 _shoveVel;
        float _shoveTimer;
        float _pushCooldown;
        bool _shooting;
        float _shotCharge;
        float _apexTime;
        Vector3 _fadeDir;
        float _fadeAmount;
        Vector3 _lastRunVelocity;
        Vector3 _launchVel;
        bool _pendingQuickCatch;
        bool _hadBall;
        float _catchTime = -10f;
        bool _finishing;
        float _finishTimer;
        bool _finishIsDunk;
        bool _finishAdjusted;
        bool _finishSlamming;
        float _finishSlamTimer;
        FinishStyle _finishStyle;
        TakeoffFoot _finishFoot;          // which foot/feet this finish leaves from
        bool _shootHandLeft;              // which hand finishes (flips on a switch-hands adjust)
        AdjustMove _adjustMove;           // how an air-adjust contorts the shot (None when straight)
        float _finishGravityScale = 0.4f; // per-finish, so the takeoff matches a jump shot's rise (see StartFinish)
        float _hangTimer;
        Vector3 _hangTarget;
        float _blockGestureTimer;
        bool _blockTwoHanded;
        float _skyTimer;
        Vector2 _passAim;
        bool _iconHeld;       // LB held — teammate pass icons
        bool _adjustHeld;     // LT held — air-adjust a finish / advanced post move
        bool _prevSprintHeld; // edge-detects a fresh LT press (arms the finish air-adjust)
        float _dribbleCooldown;
        float _dribbleBoostTimer;
        float _flickCooldown;
        float _lastLateralFlickTime = -10f;
        float _lastLateralFlickSign;
        bool _passCharging;
        float _passChargeTime;
        float _fallTimer;
        PlayerController _assistPasser;
        float _assistTime;
        bool _assistDribbled;
        bool _gainedFromRebound;
        float _lastShotDistance;
        int _calledShotsUsed;
        string _calledShotCallout;
        float _calledShotCalloutTimer;
        float _passGestureTimer;
        float _stealGestureTimer;
        DribbleMoveType _dribbleMoveType;
        float _dribbleMoveTimer;
        float _dribbleMoveDuration;

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _character = GetComponent<PlayerCharacter>();
            _post = GetComponent<PostUpController>();
            _modelTf = transform.Find("Model"); // the visual body the animator rotates
            // Time from launch to the top of the (floaty) jump-shot leap = the
            // ideal release point. Computed from the shot's own height/gravity so
            // the timing meter lines up with how long the player actually hangs.
            float shotG = gravity * shotGravityScale;
            _apexTime = Mathf.Sqrt(-2f * shotG * shotJumpHeight) / -shotG;
        }

        void OnEnable()
        {
            if (isHuman) EnableInput();
        }

        void OnDisable()
        {
            DisableInput();
        }

        public void SetHumanControlled(bool value)
        {
            isHuman = value;
            if (value)
            {
                EnableInput();
            }
            else
            {
                DisableInput();
                _moveIntent = Vector2.zero;
                _sprintIntent = false;
                _shooting = false;
                _finishing = false;
                if (IsPosting) _post.End();
            }
        }

        void EnableInput()
        {
            if (_input != null) return;
            _input = new InputReader();
            _input.ShootPressed += OnShootPressed;
            _input.ShootReleased += OnShootReleased;
            _input.PassPressed += OnPassPressed;
            _input.PassReleased += OnPassReleased;
            _input.JumpPressed += TriggerJump;
            _input.StealPressed += TriggerSteal;
            _input.DivePressed += TriggerDive;
            _input.BackDownPressed += TriggerBackDown;
            _input.PostNorthPressed += OnPostNorth;
            _input.PostEastPressed += OnPostEast;
            _input.PostWestPressed += OnPostWest;
            _input.PostButtonReleased += OnPostButtonReleased;
            _input.DribbleFlick += OnDribbleFlick;
            _input.TurboDoubleTap += OnTurboDoubleTap;
            _input.Enable();
        }

        void DisableInput()
        {
            if (_input == null) return;
            _input.ShootPressed -= OnShootPressed;
            _input.ShootReleased -= OnShootReleased;
            _input.PassPressed -= OnPassPressed;
            _input.PassReleased -= OnPassReleased;
            _input.JumpPressed -= TriggerJump;
            _input.StealPressed -= TriggerSteal;
            _input.DivePressed -= TriggerDive;
            _input.BackDownPressed -= TriggerBackDown;
            _input.PostNorthPressed -= OnPostNorth;
            _input.PostEastPressed -= OnPostEast;
            _input.PostWestPressed -= OnPostWest;
            _input.PostButtonReleased -= OnPostButtonReleased;
            _input.DribbleFlick -= OnDribbleFlick;
            _input.TurboDoubleTap -= OnTurboDoubleTap;
            _input.Disable();
            _input = null;
        }

        /// <summary>Set this frame's desired movement (AI; the human overrides
        /// it from input each frame).</summary>
        public void SetMoveIntent(Vector2 move, bool sprint)
        {
            _moveIntent = move;
            _sprintIntent = sprint;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (_stealCooldown > 0f) _stealCooldown -= dt;
            if (_stunTimer > 0f) _stunTimer -= dt;
            if (_postMoveGestureTimer > 0f) _postMoveGestureTimer -= dt;
            if (_calledShotCalloutTimer > 0f) _calledShotCalloutTimer -= dt;
            if (_fallTimer > 0f) _fallTimer -= dt;
            if (_hangTimer > 0f) _hangTimer -= dt;
            if (_blockGestureTimer > 0f) _blockGestureTimer -= dt;
            if (_skyTimer > 0f) _skyTimer -= dt;
            if (_diveTimer > 0f) _diveTimer -= dt;
            if (_shoveTimer > 0f) _shoveTimer -= dt;
            if (_pushCooldown > 0f) _pushCooldown -= dt;
            if (_dribbleCooldown > 0f) _dribbleCooldown -= dt;
            if (_dribbleBoostTimer > 0f) _dribbleBoostTimer -= dt;
            if (_flickCooldown > 0f) _flickCooldown -= dt;
            if (_passGestureTimer > 0f) _passGestureTimer -= dt;
            if (_stealGestureTimer > 0f) _stealGestureTimer -= dt;
            if (_dribbleMoveTimer > 0f) _dribbleMoveTimer -= dt;
            if (_passCharging)
            {
                _passChargeTime += dt;
                if (IsStunned || !HasBall) _passCharging = false; // lost it mid-windup
            }

            if (isHuman && _input != null)
            {
                _input.Tick();
                // The stick is read relative to the camera, so "up" on the stick
                // is "up the screen" regardless of where the sideline camera sits.
                _moveIntent = CameraRelative(_input.Move);
                // Pass aim: the LEFT stick (your movement stick) directs the pass —
                // hold it toward a teammate and A throws that way. The right stick
                // still works as a dedicated aim override when you push it.
                _passAim = _input.PassAim.magnitude >= passAimDeadzone ? _input.PassAim : _input.Move;
                _sprintIntent = _input.SprintHeld;
                _iconHeld = _input.IconHeld;     // LB — teammate pass icons only
                _adjustHeld = _input.SprintHeld; // LT
                // Air-adjust a finish: arm it on a FRESH LT press in the air. Doing
                // it on the press (not a held LT) means sprinting into the rim with
                // turbo held doesn't auto-contort every dunk — you tap LT mid-leap to
                // adjust around a shot-blocker. Once armed it stays for this finish,
                // and we lock in WHICH contort (from where the defender is) right then.
                if (_finishing && _adjustHeld && !_prevSprintHeld && !_finishAdjusted)
                {
                    _finishAdjusted = true;
                    _adjustMove = PickAdjustMove();
                }
                // Holding LT is the advanced-move (turbo) modifier for the post face
                // buttons (LT + Y = hook, LT + X = power drop step). Spin lives on B,
                // so the trigger no longer doubles as a tap-to-spin gesture.
                _prevSprintHeld = _input.SprintHeld;
                HandlePostHold();
                HandleBackDownHold();
            }

            UpdateKillerInstinct();
            AdvanceShotMeter(dt);
            AdvanceFinish(dt);
            Move();
            // Turbo bar: burn while sprinting, recover otherwise (flat rates).
            _turbo = Mathf.Clamp01(_turbo + (_sprintingNow ? -turboDrainPerSec : turboRegenPerSec) * dt);
            UpdateDribbleState();

            // Dribbling/driving with the ball puts it into play straight off an
            // inbound — the inbounder isn't forced to stand and pass it in.
            if (isHuman && HasBall && _moveIntent.sqrMagnitude > 0.02f && GameManager.Instance != null)
                GameManager.Instance.TryStartFromInbound();
            // Loose balls / rebounds are resolved centrally (GameManager) so it's
            // a Rebounds + height + jump contest, not a first-come grab.

            // Track when this player gains the ball (for catch-and-shoot timing).
            bool has = HasBall;
            if (has && !_hadBall) _catchTime = Time.time;
            if (!has && _hadBall) _gainedFromRebound = false; // cleared when the ball is gone
            _hadBall = has;

            // Assist window: voided if it lapses, the ball is gone, or the
            // receiver puts it on the floor (starts dribbling).
            if (_assistPasser != null)
            {
                if (Time.time - _assistTime > assistWindow || !has) _assistPasser = null;
                else if (IsDribbling) _assistDribbled = true;
            }
        }

        /// <summary>Called when this player catches a pass — records the passer
        /// for the Playmaker assist bonus.</summary>
        public void OnCaughtPass(PlayerController passer)
        {
            _assistPasser = passer;
            _assistTime = Time.time;
            _assistDribbled = false;
            _gainedFromRebound = false; // a caught pass is not a rebound/loose ball
        }

        /// <summary>Called when this player collects a rebound / loose ball — marks
        /// the possession so only these get put-back / tip behaviour.</summary>
        public void OnGrabbedRebound() => _gainedFromRebound = true;

        /// <summary>The teammate whose pass this shot is going up directly off of
        /// (within the assist window, no dribble), or null. Captured by the ball
        /// on a shot for assist effects (Playmaker, Energizer).</summary>
        public PlayerController AssistingPasser =>
            (_assistPasser != null && !_assistDribbled && Time.time - _assistTime <= assistWindow)
                ? _assistPasser : null;

        /// <summary>+2 if shooting directly off a Playmaker's pass (in time, no drive).</summary>
        int AssistBonus()
        {
            if (_assistPasser == null || _assistDribbled || Time.time - _assistTime > assistWindow) return 0;
            var s = _assistPasser.Character != null ? _assistPasser.Character.stats : null;
            return (s != null && s.hiddenTrait == HiddenTrait.Playmaker) ? 2 : 0;
        }

        bool QuickCatchReady() =>
            _character != null && _character.stats != null
            && _character.stats.hiddenTrait == HiddenTrait.QuickCatchShooter
            && (Time.time - _catchTime) <= quickCatchWindow;

        void AdvanceShotMeter(float dt)
        {
            if (!_shooting) return;
            if (IsStunned || !HasBall) { _shooting = false; return; } // lost the ball / knocked
            _shotCharge += dt;
            if (_shotCharge >= ShotMeterDuration) ReleaseJumpShot(); // held too long → late shot
        }

        void AdvanceFinish(float dt)
        {
            if (!_finishing) return;
            if (IsStunned || !HasBall) { _finishing = false; _finishSlamming = false; return; }
            _finishTimer += dt;

            // Slam / lay-in: hang at the rim while the hand drives the ball down
            // into it (CarriedBallPoint walks the ball to the rim), and only let
            // go once it's there — the ball never arcs out of the hand early.
            if (_finishSlamming)
            {
                _verticalVelocity = 0f;
                _finishSlamTimer -= dt;
                if (_finishSlamTimer <= 0f) ResolveFinish();
                return;
            }

            // Ride the leap up to its peak before finishing, so dunks and layups
            // happen up at (or above) the rim — arcade air, with room to air-adjust
            // around a shot-blocker.
            var gm = GameManager.Instance;
            Hoop hoop = gm != null ? gm.GetAttackingHoop(team) : null;
            bool atRim = hoop != null && HorizontalDistance(transform.position, hoop.AimPoint) <= finishReleaseDistance;
            bool atPeak = _verticalVelocity <= 0f && _finishTimer >= finishMinAirTime;
            if (atRim && atPeak)
            {
                // At the rim and at the top — start the slam: the ball is driven
                // down into the rim (still in hand) and released only there.
                _finishSlamming = true;
                _finishSlamTimer = finishSlamTime;
            }
            else if (_finishTimer >= finishAirTime + (_finishAdjusted ? finishAdjustExtraAir : 0f))
            {
                // Time's up before we cleanly reached the rim+apex (e.g. cut off):
                // finish AT the rim anyway — drive the still-held ball down into the
                // hoop. We never throw it loose in mid-air; the ball only leaves the
                // hand at the basket (the make/block roll still happens there).
                _finishSlamming = true;
                _finishSlamTimer = finishSlamTime;
            }
        }

        void HandlePostHold()
        {
            // While a post shot is going up, releasing the post-up button is NOT a
            // cancel — it's how you let the shot go (you naturally come off the post
            // button as you rise into it). Put the shot up instead of dropping out of
            // the post empty-handed. The post ends itself when the shot resolves.
            if (_post != null && _post.PostShotActive)
            {
                if (!_input.PostUpHeld) _post.ReleasePostShot();
                return;
            }
            // Mid spin / power-drop drive-out: don't re-post on top of the drive.
            if (IsDoingPostMove) return;
            // After driving out of the post, don't auto-repost while the post button
            // is still held — you have to release it and press again to post anew.
            if (!_input.PostUpHeld) _postRepostBlocked = false;
            if (_postRepostBlocked) return;

            bool wantPost = _input.PostUpHeld && HasBall && !IsStunned && _cc.isGrounded;
            if (wantPost && !IsPosting) _post.Begin(NearestOpponentTo(transform.position));
            else if (!_input.PostUpHeld && IsPosting) _post.End();
        }

        public void Teleport(Vector3 position)
        {
            // Spots are authored for ~2 m players; keep taller bodies above the
            // floor (centre must sit at half the controller height).
            position.y = Mathf.Max(position.y, _cc.height / 2f + 0.05f);
            bool was = _cc.enabled;
            _cc.enabled = false;
            transform.position = position;
            _cc.enabled = was;
        }

        float Effective(StatType stat, float fallback) =>
            _character != null ? _character.GetEffective(stat) : fallback;

        public float EffectiveStat(StatType stat) => Effective(stat, 5f);

        void Move()
        {
            float dt = Time.deltaTime;
            Vector3 horizontal;
            bool rotateToMove = false;
            Vector3 faceDir = Vector3.zero;
            _sprintingNow = false; // set true only when actually burning turbo (below)

            // Hanging on the rim after a slam: settle onto the rim grip and hold a
            // beat (arms up, hands on the rim), then drop off and fall.
            if (_hangTimer > 0f)
            {
                _verticalVelocity = 0f;
                PlanarSpeed = 0f;
                _character?.ReportActivity(false, false);
                Vector3 to = _hangTarget - transform.position;
                _cc.Move(to * Mathf.Clamp01(hangSettleLerp * dt));
                return;
            }

            // Slamming/laying it in: hang in place at the rim while the ball is
            // driven down into it (see CarriedBallPoint). Don't drift or fall.
            if (_finishSlamming)
            {
                _verticalVelocity = 0f;
                PlanarSpeed = 0f;
                _character?.ReportActivity(true, false);
                return;
            }

            if (IsStunned)
            {
                horizontal = Vector3.zero;
                _character?.ReportActivity(false, false);
            }
            else if (IsPosting)
            {
                horizontal = _post.DriveVelocity; // PostUpController owns facing
                _character?.ReportActivity(true, false);
            }
            else if (_diveTimer > 0f)
            {
                horizontal = _diveDir * diveSpeed;
                _character?.ReportActivity(true, true);
            }
            else if (_finishing)
            {
                // Drive toward the rim while up for the dunk/layup, then stop once
                // you reach it and rise straight up — so you slam at the rim instead
                // of sailing past it. Stay squared to the hoop.
                Vector3 toRim = RimDirection();
                float d = toRim.magnitude;
                bool adjusting = _finishAdjusted; // armed by a deliberate LT tap in the air
                // While adjusting, ease off the auto-pull to the rim so the player's
                // own steer can reposition them around it.
                float approach = d > finishReleaseDistance
                    ? finishApproachSpeed * (adjusting ? finishAdjustApproachScale : 1f) : 0f;
                horizontal = d > 0.01f ? toRim.normalized * approach : Vector3.zero;
                if (d > 0.01f) { rotateToMove = true; faceDir = toRim.normalized; }

                // Air-adjust steering: hold LB and push the stick to drift laterally
                // through the air — swing around a shot-blocker and finish from the
                // other side of the rim.
                if (adjusting)
                {
                    Vector3 steer = new Vector3(_moveIntent.x, 0f, _moveIntent.y);
                    if (steer.sqrMagnitude > 0.01f)
                        horizontal += Vector3.ClampMagnitude(steer, 1f) * finishAirControlSpeed;
                }
                _character?.ReportActivity(true, false);
            }
            else if (_shooting)
            {
                // A jump shot doesn't run: hold the stick to fade that way (the
                // body leans, see ProceduralAnimator), or hold nothing to rise
                // straight up. We stay squared to the rim so it reads as a
                // fadeaway, not a drift.
                Vector3 fade = new Vector3(_moveIntent.x, 0f, _moveIntent.y);
                _fadeAmount = Mathf.Clamp01(fade.magnitude);
                if (_fadeAmount > 0.05f)
                {
                    _fadeDir = fade.normalized;
                    // Fading with your momentum is easy; against it (planting the
                    // wrong way at the last second) barely leans — and the faster
                    // you were going, the harder it is to reverse.
                    _fadeAmount *= MomentumFadeScale(_fadeDir);
                    horizontal = _fadeDir * fadeSpeed * _fadeAmount;
                }
                else
                {
                    _fadeAmount = 0f;
                    horizontal = Vector3.zero;
                }
                Vector3 toRim = RimDirection();
                if (toRim.sqrMagnitude > 0.01f) { rotateToMove = true; faceDir = toRim.normalized; }
                _character?.ReportActivity(false, false);
            }
            else
            {
                Vector3 dir = new Vector3(_moveIntent.x, 0f, _moveIntent.y);
                if (dir.sqrMagnitude > 1f) dir.Normalize();

                float speedStat = Effective(StatType.Speed, 5f);
                float baseSpeed = Mathf.Lerp(minMoveSpeed, maxMoveSpeed, Mathf.Clamp01((speedStat - 1f) / 9f));
                bool sprinting = _sprintIntent && dir.sqrMagnitude > 0.01f && _turbo > 0.01f;
                _sprintingNow = sprinting; // burns turbo this frame
                float speed = baseSpeed * (sprinting ? sprintMultiplier : 1f);
                if (_dribbleBoostTimer > 0f) speed *= dribbleBoostMult; // separation after a move
                _character?.ReportActivity(dir.sqrMagnitude > 0.01f, sprinting);

                horizontal = dir * speed;
                _lastRunVelocity = horizontal; // momentum carried into a fadeaway
                rotateToMove = dir.sqrMagnitude > 0.01f;
                faceDir = dir;
            }

            if (_shoveTimer > 0f) horizontal += _shoveVel;
            horizontal += Separation();   // never stand on / inside another player
            PlanarSpeed = horizontal.magnitude;

            if (_cc.isGrounded && _verticalVelocity < 0f) _verticalVelocity = -2f;

            // Post shots leap off the floor instead of being flat-footed: on the
            // frame the shot starts, hop into it (the sky hook skies higher), then
            // float on softened gravity so the apex lands near the release.
            bool postShooting = IsPostShooting;
            if (postShooting && !_wasPostShooting && _cc.isGrounded)
                _verticalVelocity = PostShotTakeoffVelocity(CurrentPostMove);
            _wasPostShooting = postShooting;

            // A jump shot and a dunk/layup both float (softened gravity) so the
            // player hangs — the shot is easier to time and the finish clearly
            // elevates to the rim with the ball. Skying for an oop: rise normally,
            // then soften on the way down so the player hangs while the lob arrives.
            float g = gravity;
            if (_shooting) g = gravity * shotGravityScale;
            else if (_finishing) g = gravity * _finishGravityScale * (_finishAdjusted ? finishAdjustFloat : 1f);
            else if (postShooting) g = gravity * shotGravityScale; // hang into the post shot
            else if (_skyTimer > 0f && _verticalVelocity < 0f) g = gravity * oopSkyGravityScale;
            _verticalVelocity += g * dt;

            Vector3 velocity = horizontal + Vector3.up * _verticalVelocity;
            _cc.Move(velocity * dt);

            if (rotateToMove)
            {
                Quaternion want = Quaternion.LookRotation(faceDir, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, want, turnSpeed * dt);
            }
        }

        /// <summary>Latch the dribble state. You start dribbling the moment you
        /// move with the ball; once started, standing still keeps the dribble
        /// alive. It ends when you no longer have the ball, leave your feet, or
        /// go into a shot / finish / post / stun.</summary>
        void UpdateDribbleState()
        {
            if (!HasBall || _cc == null || !_cc.isGrounded
                || IsShooting || IsFinishing || IsPosting || IsStunned)
            {
                _dribbling = false;
                return;
            }
            // Putting it on the floor (any real movement) starts the dribble.
            if (PlanarSpeed > dribbleMoveThreshold) _dribbling = true;
        }

        /// <summary>Convert a raw stick vector into a world-plane move direction
        /// relative to the camera, so pushing the stick "up" drives the player up
        /// the screen no matter where the sideline camera is. Returned as an XZ
        /// vector (x → world X, y → world Z) to match how <see cref="Move"/> reads
        /// the intent. AI intents bypass this — they're already world-space.</summary>
        Vector2 CameraRelative(Vector2 stick)
        {
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return stick;

            Vector3 fwd = _cam.transform.forward; fwd.y = 0f;
            Vector3 right = _cam.transform.right; right.y = 0f;
            if (fwd.sqrMagnitude < 1e-4f) fwd = Vector3.forward;
            fwd.Normalize();
            right.Normalize();

            Vector3 world = right * stick.x + fwd * stick.y;
            return new Vector2(world.x, world.z);
        }

        /// <summary>Soft body separation: if another player is overlapping us
        /// horizontally, push apart so nobody can stand on (or inside) anyone.</summary>
        Vector3 Separation()
        {
            var gm = GameManager.Instance;
            if (gm == null) return Vector3.zero;
            float myR = _cc != null ? _cc.radius : 0.4f;
            Vector3 push = Vector3.zero;

            foreach (var side in new[] { gm.Home, gm.Away })
            {
                foreach (var other in side.onCourt)
                {
                    if (other == null || other == this || !other.enabled) continue;
                    Vector3 d = transform.position - other.transform.position; d.y = 0f;
                    float minDist = myR + other.BodyRadius;
                    float dist = d.magnitude;
                    if (dist >= minDist) continue;
                    Vector3 dir = dist > 0.01f ? d / dist : new Vector3(Random.value - 0.5f, 0f, Random.value - 0.5f).normalized;
                    push += dir * (minDist - dist) * 8f; // proportional to overlap
                }
            }
            return push;
        }

        public float BodyRadius => _cc != null ? _cc.radius : 0.4f;

        BallController Ball => GameManager.Instance != null ? GameManager.Instance.ball : null;

        // ---- Actions (input events or AI brain) ----------------------------

        /// <summary>Immediate shot with perfect timing — used by the AI. Inside
        /// the paint it finishes (dunk/layup) with no air-adjust.</summary>
        public void TriggerShoot()
        {
            if (MatchPause.IsPaused || IsStunned || IsPosting || _shooting || _finishing) return;
            // Inside, leap and attack the rim (same as the human) rather than firing
            // the ball off from the ground; the finish auto-resolves at the basket.
            if (InsideRange()) StartFinish();
            else ExecuteShot(1f, QuickCatchReady());
        }

        // Human shooting: hold to rise, release to commit. Jump shots use the
        // release-timing meter; inside (dunk/layup) goes up for a finish you can
        // air-adjust (L1) or pass out of.
        void OnShootPressed()
        {
            if (MatchPause.IsPaused || IsStunned || IsPosting || !HasBall || _shooting || _finishing) return;
            GameManager.Instance.TryStartFromInbound(); // shooting it in puts the ball live
            Hoop hoop = GameManager.Instance.GetAttackingHoop(team);
            if (hoop == null) return;

            if (InsideRange()) { StartFinish(); return; }

            _pendingQuickCatch = QuickCatchReady(); // captured at the catch, before the jump
            _shooting = true;
            _shotCharge = 0f;
            _fadeDir = Vector3.zero;
            _fadeAmount = 0f;
            _launchVel = _lastRunVelocity; // the momentum you take into the jump
            if (_cc.isGrounded) _verticalVelocity = ShotTakeoffVelocity(); // floaty jump-shot leap
        }

        void OnShootReleased()
        {
            if (_shooting) { ReleaseJumpShot(); return; }
            // Releasing the button during a finish (before the ball reaches the
            // rim) turns a dunk into a layup: hold it down to throw it home, let
            // go to lay it in. The finish still skies all the way up — releasing
            // only changes WHAT you do at the rim, not when. L1 alters it either
            // way (a contorted dunk, or a double-clutch layup).
            if (_finishing && !_finishSlamming && _finishIsDunk)
            {
                _finishIsDunk = false;
                _finishStyle = FinishStyle.Layup;
            }
        }

        bool InsideRange()
        {
            var gm = GameManager.Instance;
            Hoop hoop = gm != null ? gm.GetAttackingHoop(team) : null;
            return hoop != null && HorizontalDistance(transform.position, hoop.AimPoint) <= finishRange;
        }

        Vector3 RimDirection()
        {
            var gm = GameManager.Instance;
            Hoop hoop = gm != null ? gm.GetAttackingHoop(team) : null;
            if (hoop == null) return Vector3.zero;
            Vector3 d = hoop.AimPoint - transform.position; d.y = 0f;
            return d;
        }

        // ---- Inside finishing (dunk / layup) -------------------------------

        void StartFinish()
        {
            _finishing = true;
            _finishSlamming = false;
            _finishTimer = 0f;
            _finishAdjusted = false;
            _adjustMove = AdjustMove.None;
            // Commit to a dunk if this player can throw it down (off the base Dunk
            // rating, so fatigue doesn't quietly demote a real dunker). The human
            // holds the button to keep the dunk; releasing before the rim drops it
            // to a layup (see OnShootReleased). The AI never releases, so a dunker
            // dunks. A non-dunker always lays it in.
            _finishIsDunk = (_character != null ? _character.stats.Get(StatType.Dunk) : 5) >= dunkThreshold;
            _finishStyle = PickFinishStyle(_finishIsDunk);
            // Pick the takeoff foot, then the finishing hand: a one-foot layup
            // finishes with the hand opposite the takeoff foot, a two-foot gather
            // releases right (see ShootHandLeft / OnShootReleased can drop a dunk
            // to a layup but keeps the foot).
            _finishFoot = PickTakeoffFoot();
            _shootHandLeft = _finishFoot == TakeoffFoot.Right;
            // Arcade air: layups are a small hop, dunks soar (over the rim for big
            // dunkers) — the apex still scales with the Dunk stat. But the player
            // leaves the floor at the SAME rise speed as a jump shot, then gravity
            // is softened per-finish (the taller the leap, the floatier) so they
            // still reach exactly that apex. Matching the takeoff velocity keeps the
            // rise off the floor consistent with a jump shot instead of a separate,
            // floatier finish takeoff.
            float riseSpeed = ShotTakeoffVelocity();
            float apex = FinishJumpHeight();
            // g needed to top out at `apex` from `riseSpeed`: apex = v^2 / (2g).
            float gNeeded = (riseSpeed * riseSpeed) / (2f * Mathf.Max(0.01f, apex));
            _finishGravityScale = Mathf.Clamp(gNeeded / -gravity, 0.1f, 1f);
            if (_cc.isGrounded) _verticalVelocity = riseSpeed;
        }

        /// <summary>The vertical velocity a jump shot leaves the floor with — the
        /// reference "rise speed" that dunks and layups also take off at.</summary>
        float ShotTakeoffVelocity() => Mathf.Sqrt(-2f * (gravity * shotGravityScale) * shotJumpHeight);

        /// <summary>Takeoff velocity for a post shot's hop. Uses the same floaty
        /// gravity as a jump shot (so the apex lands near the timed release) and
        /// skies higher for the sky hook.</summary>
        float PostShotTakeoffVelocity(PostMove move)
        {
            float height = move == PostMove.SkyHook ? skyHookJumpHeight : postShotJumpHeight;
            return Mathf.Sqrt(-2f * (gravity * shotGravityScale) * height);
        }

        float FinishJumpHeight()
        {
            if (!_finishIsDunk) return layupJumpHeight;
            float dunk01 = Mathf.Clamp01((Effective(StatType.Dunk, 5f) - 1f) / 9f);
            return Mathf.Lerp(jumpHeight, dunkJumpHeightMax, dunk01);
        }

        /// <summary>Leap to contest a shot/dunk — a high-Blocks defender rises as
        /// high as a big dunker. Called by the AI (and usable by the human).</summary>
        public void ContestJump()
        {
            if (MatchPause.IsPaused || IsStunned || IsPosting || _hangTimer > 0f) return;
            if (!_cc.isGrounded) return;
            float block01 = Mathf.Clamp01((Effective(StatType.Blocks, 5f) - 1f) / 9f);
            _verticalVelocity = Mathf.Sqrt(-2f * gravity * Mathf.Lerp(jumpHeight, contestJumpHeightMax, block01));
        }

        /// <summary>A layup off one foot when it's not a dunk; otherwise mix the
        /// dunk styles — stronger finishers favour the two-hand rim-grab slam,
        /// athletic ones explode off one foot.</summary>
        FinishStyle PickFinishStyle(bool dunk)
        {
            if (!dunk) return FinishStyle.Layup;
            float power = Effective(StatType.Power, 5f);
            float slamBias = 0.35f + 0.4f * Mathf.Clamp01((power - 4f) / 6f);
            if (Random.value < slamBias) return FinishStyle.TwoHandSlam;
            return Random.value < 0.5f ? FinishStyle.OneFootOneHandDunk : FinishStyle.OneFootTwoHandDunk;
        }

        /// <summary>Pick which foot/feet a finish leaves from: a two-hand slam
        /// always gathers off both, a layup occasionally goes up off both (a power
        /// layup / floater) but usually drives off a single foot.</summary>
        TakeoffFoot PickTakeoffFoot()
        {
            if (_finishStyle == FinishStyle.TwoHandSlam) return TakeoffFoot.Both;
            if (!_finishIsDunk && Random.value < layupBothFeetChance) return TakeoffFoot.Both;
            return Random.value < 0.5f ? TakeoffFoot.Left : TakeoffFoot.Right;
        }

        /// <summary>Choose how to contort an air-adjust from where the nearest
        /// defender is: off to one side → switch hands and finish away from them;
        /// square in front and tight (a rim protector going vertical) → windmill
        /// the ball around it; otherwise drop into a low scoop. A switch also flips
        /// the finishing hand to the side away from the defender.</summary>
        AdjustMove PickAdjustMove()
        {
            PlayerController def = NearestOpponentTo(transform.position);
            if (def == null) return _finishIsDunk ? AdjustMove.Windmill : AdjustMove.LowRelease;

            Vector3 toDef = def.transform.position - transform.position; toDef.y = 0f;
            float dist = toDef.magnitude;
            float lateral = Vector3.Dot(toDef, MRight);     // + = defender on my right
            Vector3 toRim = RimDirection();
            float front = (toRim.sqrMagnitude > 0.01f && dist > 0.01f)
                ? Vector3.Dot(toDef / dist, toRim.normalized) : 0f;
            float h = BodyHeight;

            // Defender clearly off to one side → finish on the other side.
            if (Mathf.Abs(lateral) > adjustSideThreshold * h && Mathf.Abs(lateral) >= Mathf.Abs(front) * dist)
            {
                _shootHandLeft = lateral > 0f;  // defender on the right → finish lefty
                return AdjustMove.SwitchHands;
            }
            // Defender square in front and tight → go around it.
            if (front > 0.4f && dist < blockRange * 1.2f) return AdjustMove.Windmill;
            // Otherwise scoop it under.
            return AdjustMove.LowRelease;
        }

        void ResolveFinish()
        {
            if (!_finishing) return;
            _finishing = false;
            _finishSlamming = false;
            bool reachedRim = FinishShot(_finishIsDunk, _finishAdjusted);
            // Every dunk grabs the rim and hangs for a beat (the two-hand slam hangs
            // longest); skip it if the shot got swatted away.
            if (_finishIsDunk && reachedRim)
            {
                _hangTimer = _finishStyle == FinishStyle.TwoHandSlam ? dunkHangTime * 1.5f : dunkHangTime;
                PositionForHang();
            }
        }

        /// <summary>Work out where to hang: the near edge of the rim, at a height
        /// where the player's straight-up arms put their hands on it. They settle
        /// onto it smoothly during the hang (see <see cref="Move"/>).</summary>
        void PositionForHang()
        {
            var gm = GameManager.Instance;
            Hoop hoop = gm != null ? gm.GetAttackingHoop(team) : null;
            if (hoop == null) { _hangTarget = transform.position; return; }
            Vector3 rim = hoop.AimPoint;
            Vector3 toCourt = transform.position - rim; toCourt.y = 0f;
            toCourt = toCourt.sqrMagnitude > 0.01f ? toCourt.normalized : -transform.forward;
            _hangTarget = rim + toCourt * 0.35f;            // hang on the near edge
            _hangTarget.y = rim.y - 0.85f * BodyHeight;     // hands reach up to the rim
            Vector3 face = rim - transform.position; face.y = 0f;
            if (face.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(face.normalized, Vector3.up);
        }

        /// <summary>A shot by <paramref name="shooter"/> got blocked by this player.
        /// Two-handed it's a clean snatch (this player's ball, no loose ball); one-
        /// handed it's a swat that caroms the ball off the hand into a loose ball.
        /// Either way records the block, breaks the shooter's streak, and fires the
        /// blocker's swat/snatch animation (and an air-hang if they're up).</summary>
        public void ResolveBlock(PlayerController shooter, Vector3 shotAim)
        {
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.RecordBlock(this);
                gm.OnShotMissed(shooter); // blocked → shooter's streak broken
            }

            bool twoHand = Random.value <
                Mathf.Lerp(blockTwoHandBaseChance, blockTwoHandBaseChance + 0.25f,
                           Mathf.Clamp01((Effective(StatType.Blocks, 5f) - 1f) / 9f));

            if (twoHand && shooter.Ball != null)
            {
                // Snatch it clean out of the air — possession to the blocker.
                shooter.Ball.PickUp(this);
                if (gm != null) gm.OnPossessionGained(this);
            }
            else if (shooter.Ball != null)
            {
                // Swat it away off the hand — a chaotic loose ball.
                Vector3 away = shooter.transform.position - shotAim; away.y = 0f;
                if (away.sqrMagnitude < 0.01f) away = -shooter.transform.forward;
                shooter.Ball.Swat(BlockHandPoint(), away, blockKnockPower);
            }

            RegisterBlock(twoHand, shooter.transform.position);
        }

        /// <summary>Start the swat/snatch gesture, face the blocked shot, and (if
        /// airborne) hang at the top of the contest for a beat.</summary>
        void RegisterBlock(bool twoHanded, Vector3 toward)
        {
            _blockTwoHanded = twoHanded;
            _blockGestureTimer = blockGestureTime;
            Vector3 d = toward - transform.position; d.y = 0f;
            if (d.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(d.normalized, Vector3.up);
            if (_cc != null && !_cc.isGrounded) // stall in the air like a finisher
            {
                _hangTimer = Mathf.Max(_hangTimer, blockHangTime);
                _hangTarget = transform.position;
            }
        }

        /// <summary>World point up at the blocker's hands — where the ball is met.</summary>
        Vector3 BlockHandPoint() =>
            transform.position + Vector3.up * (BodyHeight + 0.45f) + transform.forward * 0.25f;

        /// <summary>Resolve a dunk or layup: a block roll first (reduced by an
        /// air-adjust, and resisted by Power on dunks), then a make roll. A dunk
        /// scores off the Dunk stat, a layup off Inside Scoring.</summary>
        bool FinishShot(bool isDunk, bool adjusted, float makeBonus = 0f)
        {
            if (!HasBall) return false;
            var gm = GameManager.Instance;
            Hoop hoop = gm.GetAttackingHoop(team);
            if (hoop == null) return false;

            Vector3 aim = hoop.AimPoint;
            _lastShotDistance = HorizontalDistance(transform.position, aim); // for Delfan's called shot
            gm.RecordShotAttempt(this, 2); // an inside finish is always a 2
            // Dunk scores off Dunk, layup off Inside Scoring; +2 off a Playmaker pass.
            StatType scoreStat = isDunk ? StatType.Dunk : StatType.InsideScoring;
            int rawFinish = (_character != null ? _character.stats.Get(scoreStat) : 5) + AssistBonus();
            float finisherStat = _character != null ? _character.GetEffectiveForStat(rawFinish, scoreStat) : 5f;
            PlayerController defender = NearestOpponentTo(transform.position);

            if (defender != null)
            {
                float dd = HorizontalDistance(defender.transform.position, transform.position);
                if (dd < blockRange)
                {
                    float closeness = 1f - dd / contestRange;
                    float blk = defender.EffectiveStat(StatType.Blocks);
                    float resist = isDunk ? Effective(StatType.Power, 5f) * dunkPowerBlockResist : 0f;
                    float chance = Mathf.Clamp(blockBaseChance + blockStatScale * (blk - finisherStat - resist), 0f, blockMaxChance) * closeness;
                    if (adjusted) chance *= AdjustBlockMult(); // contort away from the block
                    if (Random.value < chance)
                    {
                        defender.ResolveBlock(this, aim); // swat away or snatch clean
                        return false; // no rim grab
                    }
                }
            }

            bool onFire = _character != null && _character.OnFire;
            float over = finisherStat; // already includes the scoring stat + assist
            float makeChance = ShotMath.MakeChance(this, StatType.InsideScoring, HorizontalDistance(transform.position, aim), defender, onFire, over);
            if (adjusted) makeChance -= AdjustPenalty();
            makeChance += makeBonus;
            makeChance = Mathf.Clamp(makeChance, 0f, ShotMath.MaxChance);
            bool make = Random.value < makeChance;
            // A dunk slams straight down from above; a layup is a softer drop.
            float flight = isDunk ? dunkFlightTime : finishFlightTime;
            Ball.Shoot(aim, team, 2, flight, ShotMath.AimOffset(make), this);
            return true; // got the shot off at the rim
        }

        /// <summary>True if this player has the given hidden trait.</summary>
        bool HasTrait(HiddenTrait trait)
            => _character != null && _character.stats != null && _character.stats.hiddenTrait == trait;

        /// <summary>Apply the Acrobat (Baby Mario) timing relief to a raw
        /// release-timing multiplier: he eats only a fraction of the mistiming
        /// penalty. Shared by jump shots and timed post shots.</summary>
        public float TimingWithTrait(float timing)
            => HasTrait(HiddenTrait.Acrobat) ? 1f - (1f - timing) * (1f - acrobatTimingRelief) : timing;

        /// <summary>Killer Instinct (Daisy): refresh the bonus from how gassed the
        /// opposing on-court team is — fresh legs give nothing, dead legs give the
        /// full <see cref="killerMaxBonus"/>. It only lands on her scoring and
        /// perimeter-defense stats (see <c>PlayerCharacter.TraitBonusForStat</c>).</summary>
        void UpdateKillerInstinct()
        {
            if (_character == null || !HasTrait(HiddenTrait.KillerInstinct)) return;
            var gm = GameManager.Instance;
            if (gm == null) { _character.KillerBonus = 0f; return; }

            float sum = 0f; int n = 0;
            foreach (var o in gm.TeamFor(GameManager.Opponent(team)).onCourt)
            {
                if (o == null || o.Character == null || !o.enabled) continue;
                sum += 1f - o.Character.EnergyFraction; // 0 fresh … 1 spent
                n++;
            }
            float fatigue = n > 0 ? sum / n : 0f;
            float scaled = Mathf.Clamp01((fatigue - killerFatigueFloor) / Mathf.Max(0.01f, 1f - killerFatigueFloor));
            _character.KillerBonus = killerMaxBonus * scaled;
        }

        /// <summary>Called Shot (Delfan): double-tapping turbo while one of his
        /// shots — taken from within half court — is in the air guarantees it
        /// drops. Twice a game.</summary>
        void OnTurboDoubleTap()
        {
            if (MatchPause.IsPaused || !HasTrait(HiddenTrait.CalledShot)) return;
            var ball = Ball;
            // The called shot only exists while one of his own shots is in the air —
            // a double-tap at any other time is a no-op (no spurious nudge).
            if (ball == null || ball.State != BallController.BallState.Shot || ball.Shooter != this) return;
            if (_lastShotDistance > calledShotRange) { ShowCalledShotCallout("Too far — within half court only"); return; }
            if (_calledShotsUsed >= calledShotMax) { ShowCalledShotCallout("No called shots left"); return; }
            if (ball.ForceMake()) { _calledShotsUsed++; ShowCalledShotCallout("CALLED SHOT!"); }
        }

        void ShowCalledShotCallout(string msg)
        {
            _calledShotCallout = msg;
            _calledShotCalloutTimer = calledShotCalloutTime;
        }

        /// <summary>Make% lost to an air-adjust. Driven by Inside Scoring (fully
        /// mitigated at Inside 10, full penalty at Inside 1), then weighted by the
        /// contort — a windmill is showier and costs more, a low scoop is safer.
        /// Waived entirely for an Acrobat (Baby Mario alters in the air free).</summary>
        float AdjustPenalty()
        {
            if (HasTrait(HiddenTrait.Acrobat)) return 0f;
            float inside = Effective(StatType.InsideScoring, 5f);
            float insideMit = 1f - Mathf.Clamp01((inside - 1f) / 9f);
            float moveWeight =
                _adjustMove == AdjustMove.Windmill ? windmillPenaltyWeight :
                _adjustMove == AdjustMove.LowRelease ? lowReleasePenaltyWeight : 1f;
            return maxAdjustPenalty * insideMit * moveWeight;
        }

        /// <summary>Block-chance multiplier for the chosen contort — a windmill
        /// clears the most space, a switch / low scoop a bit less. Falls back to
        /// the generic reduction when no specific move was picked.</summary>
        float AdjustBlockMult() =>
            _adjustMove == AdjustMove.Windmill ? windmillBlockMult :
            _adjustMove == AdjustMove.SwitchHands ? switchHandsBlockMult :
            _adjustMove == AdjustMove.LowRelease ? lowReleaseBlockMult : adjustBlockReduction;

        /// <summary>How much of the requested fade actually comes out, given the
        /// momentum carried into the jump: leaning <b>with</b> your run direction
        /// keeps the full fade, leaning <b>against</b> it (at speed) collapses
        /// toward <see cref="fadeAgainstMomentumMin"/>. Standing still, you fade
        /// freely either way.</summary>
        float MomentumFadeScale(Vector3 fadeDir)
        {
            float sp = _launchVel.magnitude;
            if (sp < 0.5f) return 1f; // not moving — lean wherever you like
            float align = Vector3.Dot(fadeDir, _launchVel / sp);          // -1 against … +1 with
            float withness = (align + 1f) * 0.5f;                          // 0 … 1
            float scaleAtSpeed = Mathf.Lerp(fadeAgainstMomentumMin, 1f, withness);
            float speed01 = Mathf.Clamp01(sp / maxMoveSpeed);              // slow runs barely constrain
            return Mathf.Lerp(1f, scaleAtSpeed, speed01);
        }

        void ReleaseJumpShot()
        {
            _shooting = false;
            float error = Mathf.Abs(_shotCharge - _apexTime);
            float timing = error <= perfectReleaseWindow
                ? 1f
                : Mathf.Clamp(1f - (error - perfectReleaseWindow) * timingFalloffPerSec, minTimingMultiplier, 1f);
            timing = TimingWithTrait(timing); // Acrobat (Baby Mario) shrugs off mistiming
            ExecuteShot(timing, _pendingQuickCatch, _fadeAmount);
        }

        /// <summary>
        /// Resolve a shot: block roll first (unaffected by timing or on fire),
        /// then a make roll using <see cref="ShotMath"/> scaled by the release
        /// <paramref name="timingMultiplier"/> (1 = perfect). A quick catch-and-
        /// shoot three overrides the 3-Point rating to a 10. A
        /// <paramref name="fadeAmount"/> (0-1) is a fadeaway: it buys separation
        /// (lower block + contest) at the cost of a harder shot.
        /// </summary>
        void ExecuteShot(float timingMultiplier, bool quickCatch, float fadeAmount = 0f)
        {
            if (!HasBall) return;
            Hoop hoop = GameManager.Instance.GetAttackingHoop(team);
            if (hoop == null) return;

            Vector3 aim = hoop.AimPoint;
            float distance = HorizontalDistance(transform.position, aim);
            _lastShotDistance = distance; // for Delfan's within-half-court called shot
            int points = distance >= threePointDistance ? 3 : 2;
            GameManager.Instance.RecordShotAttempt(this, points);

            StatType shotStat =
                distance >= threePointDistance ? StatType.ThreePoint :
                distance <= paintRadius ? StatType.InsideScoring :
                StatType.MidRange;

            PlayerController defender = NearestOpponentTo(transform.position);

            // Effective scoring stat with trait modifiers: quick catch-and-shoot
            // three counts as 10 (Piranha), and +2 off a Playmaker pass (Koopa).
            int rawStat = _character != null ? _character.stats.Get(shotStat) : 5;
            if (quickCatch && shotStat == StatType.ThreePoint) rawStat = 10;
            rawStat += AssistBonus();
            float shotStatValue = _character != null ? _character.GetEffectiveForStat(rawStat, shotStat) : 5f;

            // Block check first — unaffected by timing or being on fire.
            if (defender != null)
            {
                float dd = HorizontalDistance(defender.transform.position, transform.position);
                if (dd < blockRange)
                {
                    float closeness = 1f - dd / contestRange;
                    float blk = defender.EffectiveStat(StatType.Blocks);
                    float chance = Mathf.Clamp(blockBaseChance + blockStatScale * (blk - shotStatValue), 0f, blockMaxChance) * closeness;
                    chance *= 1f - fadeBlockReduction * fadeAmount; // fading away from the contest
                    if (Random.value < chance)
                    {
                        defender.ResolveBlock(this, aim); // swat away or snatch clean
                        return;
                    }
                }
            }

            bool onFire = _character != null && _character.OnFire;
            float contestScale = 1f - fadeContestReduction * fadeAmount;
            float makeChance = ShotMath.MakeChance(this, shotStat, distance, defender, onFire, shotStatValue, contestScale) * timingMultiplier;
            makeChance -= ShotMath.FadePenalty(this, fadeAmount); // flat fadeaway difficulty (0 for an Acrobat)
            makeChance = Mathf.Clamp(makeChance, 0f, ShotMath.MaxChance);
            bool make = Random.value < makeChance;
            Ball.Shoot(aim, team, points, shotFlightTime, ShotMath.AimOffset(make), this);
        }

        /// <summary>AI pass entry — throws a loft immediately.</summary>
        public void TriggerPass() => ReleasePass(hard: false);

        // Human passing: tap → loft (slow, arcs over defenders); hold past
        // passHoldThreshold → hard pass (fast, flat, lives in the steal lane).
        void OnPassPressed()
        {
            // A is the pass at all times — you can kick it out of the post too (the
            // release ends the post and throws). The one exception is a post shot
            // already going up: that's committed, so A there releases the shot rather
            // than starting a pass.
            if (MatchPause.IsPaused || IsStunned || !HasBall || _passCharging) return;
            if (_post != null && _post.PostShotActive) return;
            if (IconPassActive) { PassToSlot(0); return; } // LB + A → pass to teammate 1
            _passCharging = true;
            _passChargeTime = 0f;
        }

        void OnPassReleased()
        {
            if (!_passCharging) return;
            _passCharging = false;
            ReleasePass(hard: _passChargeTime >= passHoldThreshold);
        }

        void ReleasePass(bool hard)
        {
            if (MatchPause.IsPaused || IsStunned || !HasBall) return;
            _passGestureTimer = passGestureTime; // throw animation
            if (GameManager.Instance != null) GameManager.Instance.TryStartFromInbound(); // a pass-in goes live
            bool fromPost = IsPosting;
            if (IsPosting) _post.End();   // kick out of the post
            _finishing = false;           // or dump it off out of the air

            // Aim the pass with the stick to direct it to a specific teammate;
            // if you're not aiming (or the aim lines up with nobody), pass to
            // whoever's most open instead of throwing it into space.
            PlayerController mate = IsAimingPass ? TargetedTeammate(_passAim) : null;
            if (mate == null) mate = FindOpenTeammate();
            if (mate == null) { Ball.Pass(transform.forward, passPower); return; }

            // A loft to a teammate near the rim is an alley-oop.
            if (!hard && IsOopTarget(mate)) ThrowOop(mate, fromPost);
            else PassToTeammate(mate, fromPost, hard);
        }

        bool IsOopTarget(PlayerController mate)
        {
            // Only an alley-oop if the teammate is actively skying for one — never
            // off a normal pass to a teammate who just happens to be near the rim.
            return mate != null && mate.IsSkyingForOop;
        }

        /// <summary>AI entry: lob an alley-oop to a teammate who's skying for it.</summary>
        public void ThrowAlleyOop(PlayerController mate)
        {
            if (MatchPause.IsPaused || IsStunned || !HasBall || mate == null) return;
            _passGestureTimer = passGestureTime;
            if (GameManager.Instance != null) GameManager.Instance.TryStartFromInbound();
            ThrowOop(mate, IsPosting);
        }

        void ThrowOop(PlayerController mate, bool fromPost)
        {
            var gm = GameManager.Instance;
            Hoop hoop = gm != null ? gm.GetAttackingHoop(team) : null;
            if (hoop == null) { PassToTeammate(mate, fromPost, false); return; }

            // Lob to a high point near the rim, led slightly toward the cutter.
            Vector3 target = Vector3.Lerp(hoop.AimPoint, mate.transform.position, 0.35f);
            target.y = hoop.AimPoint.y; // rim height — the cutter jumps to meet it
            float err = PassError(PassBallHandling(fromPost));
            Vector2 j = Random.insideUnitCircle * err;
            target += new Vector3(j.x, 0f, j.y);
            Ball.PassTo(target, oopFlightTime, alleyOop: true, receiver: mate);
        }

        void PassToSlot(int index)
        {
            var mate = TeammateSlot(index);
            if (mate == null) return;
            _passGestureTimer = passGestureTime;
            if (GameManager.Instance != null) GameManager.Instance.TryStartFromInbound();
            PassToTeammate(mate, fromPost: false, hard: false);
        }

        /// <summary>Lead pass to a teammate; Ball Handling sets the accuracy, so
        /// a weak handler's pass lands off-target (and can be picked off). A
        /// Smooth Passer throws with Ball Handling counted as 8 (10 out of a post).</summary>
        void PassToTeammate(PlayerController mate, bool fromPost, bool hard)
        {
            float err = PassError(PassBallHandling(fromPost));
            Vector2 j = Random.insideUnitCircle * err;
            Vector3 dest = mate.transform.position + new Vector3(j.x, 0.6f, j.y);
            Ball.PassTo(dest, hard ? hardPassTime : loftPassTime, receiver: mate);
        }

        float PassBallHandling(bool fromPost)
        {
            if (_character != null && _character.stats != null && _character.stats.hiddenTrait == HiddenTrait.SmoothPasser)
                return _character.GetEffectiveFor(fromPost ? 10 : 8);
            return Effective(StatType.BallHandling, 5f);
        }

        float PassError(float bh) => Mathf.Lerp(passErrorMax, passErrorMin, Mathf.Clamp01((bh - 1f) / 9f));

        /// <summary>Catch an alley-oop and finish it immediately (GameManager calls
        /// this when a teammate snags an oop near the rim).</summary>
        public void CatchAlleyOop()
        {
            if (!HasBall) return;
            _skyTimer = 0f;
            bool dunk = (_character != null ? _character.stats.Get(StatType.Dunk) : 5) >= dunkThreshold;
            _finishStyle = PickFinishStyle(dunk);
            bool reachedRim = FinishShot(dunk, adjusted: false, makeBonus: alleyOopBonus);
            if (dunk && reachedRim) { _hangTimer = dunkHangTime; PositionForHang(); } // throw it down and grab iron
        }

        /// <summary>Tip your own close miss straight back up while still in the air
        /// — a true put-back (GameManager calls this when you grab an offensive
        /// rebound airborne at the rim). It's a touch finish, so a hair tougher.</summary>
        public void TipIn()
        {
            if (!HasBall) return;
            bool dunk = (_character != null ? _character.stats.Get(StatType.Dunk) : 5) >= dunkThreshold;
            FinishShot(dunk, adjusted: false, makeBonus: -0.08f);
        }

        /// <summary>Pre-emptively leap and hang above the rim, calling for a lob —
        /// the only way an alley-oop happens. No-op without the ball already gone.</summary>
        public void SkyForOop()
        {
            if (MatchPause.IsPaused || IsStunned || HasBall || IsPosting || !_cc.isGrounded) return;
            _verticalVelocity = Mathf.Sqrt(-2f * gravity * oopSkyHeight);
            _skyTimer = oopSkyHang;
        }

        /// <summary>The index-th on-court teammate (excluding self) — for icon passing.</summary>
        PlayerController TeammateSlot(int index)
        {
            var gm = GameManager.Instance;
            if (gm == null) return null;
            int n = 0;
            foreach (var mate in gm.TeamFor(team).onCourt)
            {
                if (mate == null || mate == this || !mate.enabled) continue;
                if (n == index) return mate;
                n++;
            }
            return null;
        }

        /// <summary>The teammate the aim stick is pointing at, in world space. The
        /// stick is read camera-relative (same as movement), so pushing it toward a
        /// teammate on screen — left, up-court, wherever — targets them.</summary>
        PlayerController TargetedTeammate(Vector2 aim)
        {
            var gm = GameManager.Instance;
            if (gm == null || aim.sqrMagnitude < 0.0001f) return null;
            Vector2 camRel = CameraRelative(aim.normalized); // stick → world XZ
            Vector3 aimDir = new Vector3(camRel.x, 0f, camRel.y);
            if (aimDir.sqrMagnitude < 1e-4f) return null;
            aimDir.Normalize();

            PlayerController best = null;
            float bestDot = 0.25f; // require some alignment
            foreach (var mate in gm.TeamFor(team).onCourt)
            {
                if (mate == null || mate == this || !mate.enabled) continue;
                Vector3 dir = mate.transform.position - transform.position; dir.y = 0f;
                if (dir.sqrMagnitude < 0.5f) continue;
                float dot = Vector3.Dot(dir.normalized, aimDir);
                if (dot > bestDot) { bestDot = dot; best = mate; }
            }
            return best;
        }

        /// <summary>A directed pass to a teammate (used by the AI).</summary>
        public void PassToward(Vector3 worldPoint)
        {
            if (MatchPause.IsPaused || IsStunned || !HasBall) return;
            _passGestureTimer = passGestureTime; // throw animation
            if (GameManager.Instance != null) GameManager.Instance.TryStartFromInbound();
            Ball.PassTo(worldPoint, receiver: NearestTeammateTo(worldPoint));
        }

        /// <summary>The on-court teammate closest to a world point (pass target).</summary>
        PlayerController NearestTeammateTo(Vector3 point)
        {
            var gm = GameManager.Instance;
            if (gm == null) return null;
            PlayerController best = null;
            float bestD = Mathf.Infinity;
            foreach (var m in gm.TeamFor(team).onCourt)
            {
                if (m == null || m == this || !m.enabled) continue;
                float d = HorizontalDistance(m.transform.position, point);
                if (d < bestD) { bestD = d; best = m; }
            }
            return best;
        }

        public void TriggerJump()
        {
            if (MatchPause.IsPaused || IsStunned || IsPosting) return; // Y is the pump fake while posting
            if (_cc.isGrounded) _verticalVelocity = Mathf.Sqrt(-2f * gravity * jumpHeight);
        }

        public void TriggerSteal()
        {
            if (MatchPause.IsPaused || IsStunned || _stealCooldown > 0f) return;
            var gm = GameManager.Instance;
            if (gm == null || gm.ball == null) return;

            var holder = gm.ball.Holder;
            if (holder == null || holder == this || holder.team == team) return;

            _stealGestureTimer = stealGestureTime; // swipe at the ball whether or not it lands

            float dist = HorizontalDistance(transform.position, holder.transform.position);
            if (dist > stealReach) { _stealCooldown = stealWhiffCooldown; return; }

            _stealCooldown = stealCooldown;
            float steal = EffectiveStat(StatType.Steals);
            float handle = holder.EffectiveStat(StatType.BallHandling);
            float chance = Mathf.Clamp(stealBaseChance + stealStatScale * (steal - handle), stealMinChance, stealMaxChance);
            if (Random.value < chance)
            {
                gm.ball.PickUp(this);
                gm.OnPossessionGained(this);
                gm.RecordSteal(this);
            }
        }

        /// <summary>The B button: pass-icon select (LB held), dribble move (with
        /// the ball), or dive for a loose ball. (While posting, B is the spin move,
        /// handled by <see cref="OnPostEast"/>.)</summary>
        public void TriggerDive()
        {
            if (MatchPause.IsPaused || IsStunned) return;
            if (IconPassActive) { PassToSlot(1); return; }  // LB + B → pass to teammate 2
            if (IsPosting) return;                          // B is the spin while posting
            if (HasBall) { TriggerDribbleMove(); return; }  // with the ball, it's a dribble move

            if (_diveTimer > 0f || !_cc.isGrounded) return;
            _diveDir = transform.forward;
            var ball = Ball;
            if (ball != null && ball.CanBePickedUpBy(this))
            {
                Vector3 to = ball.transform.position - transform.position; to.y = 0f;
                if (to.sqrMagnitude > 0.01f && to.magnitude <= diveBallSeekRange) _diveDir = to.normalized;
            }
            _diveTimer = diveDuration;
        }

        /// <summary>AI hook for a dribble move.</summary>
        public void AttemptDribbleMove() => TriggerDribbleMove();

        /// <summary>Break the on-ball defender down — Ball Handling vs Perimeter
        /// Defense. Win → the defender is frozen ("ankles broken") and you get a
        /// burst of separation; a bad miss can get you stripped.</summary>
        void TriggerDribbleMove()
        {
            if (_dribbleCooldown > 0f || !HasBall || !_cc.isGrounded) return;
            var def = NearestOpponentTo(transform.position);
            if (def == null) { _dribbleCooldown = dribbleCooldownTime; return; }
            if (HorizontalDistance(transform.position, def.transform.position) > dribbleRange)
            {
                _dribbleCooldown = dribbleCooldownTime * 0.5f;
                return;
            }

            _dribbleCooldown = dribbleCooldownTime;
            StartDribbleMove(PickBreakdownMove(def)); // a flashy move + matching ball path
            float bh = Effective(StatType.BallHandling, 5f);
            float pd = def.EffectiveStat(StatType.PerimeterDefense);
            float chance = Mathf.Clamp(dribbleBaseChance + dribbleStatScale * (bh - pd), 0.05f, 0.95f);

            if (Random.value < chance)
            {
                def.Stun(ankleStun, fall: true);  // broken ankles — they hit the deck
                _dribbleBoostTimer = dribbleBoostTime; // separation
                if (RimDirection().sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(RimDirection().normalized, Vector3.up);
            }
            else
            {
                // Overhandled it — a good defender can poke it away.
                float strip = Mathf.Clamp(0.05f + 0.04f * (pd - bh), 0f, 0.4f);
                if (Random.value < strip && GameManager.Instance != null && GameManager.Instance.ball != null)
                {
                    GameManager.Instance.ball.PickUp(def);
                    GameManager.Instance.OnPossessionGained(def);
                    GameManager.Instance.RecordSteal(def); // defender poked it away
                }
            }
        }

        /// <summary>Kick off a dribble move: drives both the ball path and the body
        /// pose (the animator reads <see cref="CurrentDribbleMove"/>).</summary>
        void StartDribbleMove(DribbleMoveType type)
        {
            _dribbleMoveType = type;
            _dribbleMoveDuration = DribbleMoves.Duration(type);
            _dribbleMoveTimer = _dribbleMoveDuration;
            if (Ball != null) Ball.DribbleMove(type);
        }

        /// <summary>Pick a flashy breakdown move off the dribble button: throw it
        /// off a defender square in front of you, otherwise mix spins, behind-the-
        /// back, between-the-legs and crossovers.</summary>
        DribbleMoveType PickBreakdownMove(PlayerController def)
        {
            bool inFront = false;
            if (def != null)
            {
                Vector3 toDef = def.transform.position - transform.position; toDef.y = 0f;
                inFront = toDef.sqrMagnitude > 0.01f
                          && Vector3.Dot(toDef.normalized, transform.forward) > 0.4f
                          && toDef.magnitude < dribbleRange * 0.8f;
            }
            float r = Random.value;
            if (inFront && r < 0.2f) return DribbleMoveType.OffTheHead; // in their face — over the top
            if (r < 0.4f) return DribbleMoveType.Spin;
            if (r < 0.62f) return DribbleMoveType.BehindBack;
            if (r < 0.82f) return DribbleMoveType.BetweenLegs;
            return DribbleMoveType.Crossover;
        }

        /// <summary>A right-stick flick — a hard dribble in that direction to
        /// create separation. Read relative to the basket: away = step-back,
        /// toward = attacking burst, sideways = crossover (and two quick opposite
        /// sideways flicks chain into a hesitation cross, the big ankle-breaker).
        /// In the post it's a shimmy (<see cref="PostUpController.Shimmy"/>).
        /// Layers on top of the dribble-move button, it doesn't replace it.</summary>
        void OnDribbleFlick(Vector2 stick)
        {
            if (MatchPause.IsPaused || IsStunned || !HasBall || _shooting || _finishing) return;
            if (_flickCooldown > 0f || !_cc.isGrounded) return;
            Vector3 dir = new Vector3(stick.x, 0f, stick.y);
            if (dir.sqrMagnitude < 0.01f) return;
            dir.Normalize();
            _flickCooldown = flickCooldownTime;

            if (IsPosting)
            {
                _post.Shimmy(dir);
                StartDribbleMove(DribbleMoveType.Crossover); // hard shimmy dribble
                return;
            }

            // Read the flick relative to the basket.
            Vector3 toBasket = RimDirection();
            toBasket = toBasket.sqrMagnitude > 0.01f ? toBasket.normalized : transform.forward;
            float dot = Vector3.Dot(dir, toBasket);

            bool stepBack = dot <= -0.5f;
            bool attack = dot >= 0.5f;
            bool hesitationCross = false;
            if (!stepBack && !attack)
            {
                // Lateral: opposite flicks in quick succession = hesitation cross.
                float side = Mathf.Sign(Vector3.Cross(toBasket, dir).y);
                hesitationCross = Time.time - _lastLateralFlickTime <= hesitationWindow
                                  && side != _lastLateralFlickSign && _lastLateralFlickSign != 0f;
                _lastLateralFlickTime = Time.time;
                _lastLateralFlickSign = side;
            }

            // The dribble itself always happens: a burst in the flick direction,
            // with the move (and its ball path / pose) chosen from the gesture.
            ApplyShove(dir * (stepBack ? stepBackPower : flickBurstPower));
            _dribbleBoostTimer = attack || hesitationCross ? dribbleBoostTime : dribbleBoostTime * 0.5f;
            StartDribbleMove(stepBack ? DribbleMoveType.StepBack
                : hesitationCross ? DribbleMoveType.Hesitation
                : attack ? DribbleMoveType.BetweenLegs
                : Random.value < 0.5f ? DribbleMoveType.Crossover : DribbleMoveType.BehindBack);
            // A step-back squares you to the hoop for the shot; otherwise face the move.
            transform.rotation = Quaternion.LookRotation(stepBack ? toBasket : dir, Vector3.up);

            // Whether the on-ball defender keeps up is Ball Handling vs Perimeter
            // Defense. Beat them and they're shaken a beat (a hesitation cross breaks
            // ankles); otherwise they ride the move — and the higher their Perimeter
            // Defense, the tighter they stay attached.
            var def = NearestOpponentTo(transform.position);
            if (def == null || HorizontalDistance(transform.position, def.transform.position) > dribbleRange) return;

            float bh = Effective(StatType.BallHandling, 5f);
            float pd = def.EffectiveStat(StatType.PerimeterDefense);
            float shake = Mathf.Clamp(dribbleBaseChance + dribbleStatScale * (bh - pd), 0.05f, 0.85f);
            if (Random.value < shake)
            {
                if (hesitationCross) def.Stun(ankleStun, fall: true); // highlight: broken ankles
                else def.Stun(flickFreeze);                            // shaken off a beat
            }
            else
            {
                // Stayed in front — ride the flick to hold the matchup, tighter the
                // higher their Perimeter Defense.
                float keepUp = Mathf.Clamp01(flickKeepUpBase + flickKeepUpScale * (pd - bh));
                def.ApplyShove(dir * (stepBack ? stepBackPower : flickBurstPower) * keepUp);
            }
        }

        // RT tapped. Backing your man down (posting) and bumping a poster off
        // (defending one) are now continuous HOLDS handled in HandleBackDownHold,
        // so a tap here only matters in open space — a shove / reach foul attempt.
        public void TriggerBackDown()
        {
            if (MatchPause.IsPaused || IsStunned) return;
            if (IsPosting || FindPosterGuardingMe() != null) return; // those are holds now
            TryPush();                                              // push/foul in space
        }

        // Held RT: drive the back-down battle every frame. Backing down while you
        // post, bumping off while you defend a poster.
        void HandleBackDownHold()
        {
            if (IsStunned || _input == null || !_input.BackDownHeld) return;
            if (IsPosting)
            {
                if (!IsPostShooting) _post.OffensePush(Time.deltaTime);
                return;
            }
            var poster = FindPosterGuardingMe();
            if (poster != null) poster.DefenderPush(Time.deltaTime);
        }

        /// <summary>AI hook to commit a foul.</summary>
        public void AttemptPush() => TryPush();

        /// <summary>
        /// Shove the nearest opponent (Power vs Power). It's a team foul: below
        /// the penalty limit play continues and the shove just disrupts (and can
        /// knock the ball loose / knock a weaker player down); in the penalty it
        /// sends them to the line.
        /// </summary>
        void TryPush()
        {
            if (_pushCooldown > 0f || HasBall) return;
            var gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.Playing) return;

            var target = NearestOpponentTo(transform.position);
            if (target == null) return;
            if (HorizontalDistance(transform.position, target.transform.position) > pushRange)
            {
                _pushCooldown = pushWhiffCooldown;
                return;
            }

            _pushCooldown = pushCooldown;

            bool whistle = gm.RegisterFoul(team, target, target.HasBall);
            if (whistle) return; // free throws — don't play the contact out

            float myPower = EffectiveStat(StatType.Power);
            float targetPower = target.EffectiveStat(StatType.Power);

            Vector3 dir = target.transform.position - transform.position; dir.y = 0f;
            dir = dir.sqrMagnitude > 0.01f ? dir.normalized : transform.forward;

            float strength = Mathf.Clamp01((myPower - targetPower + 5f) / 10f);
            target.ApplyShove(dir * pushForce * strength);

            bool overpowered = myPower - targetPower >= pushKnockdownPowerGap;
            if (overpowered) target.Stun(0.7f);

            if (target.HasBall && gm.ball != null)
            {
                float gap = myPower - (targetPower + target.EffectiveStat(StatType.BallHandling)) * 0.5f;
                float knock = overpowered ? 1f : Mathf.Clamp(pushKnockLooseBase + pushKnockLooseScale * gap, 0.05f, 0.9f);
                if (Random.value < knock) gm.ball.Pass(dir, 3f); // pop it loose
            }
        }

        // The post face buttons map by POSITION; holding turbo (LT) upgrades the
        // Y and X moves to their advanced version. A is left alone as the pass.

        // Y — pump fake, or the hook shot with turbo held (LT + Y).
        void OnPostNorth() => TriggerPostMove(_sprintIntent ? PostMove.Hook : PostMove.Fake);

        // X — the turnaround (fadeaway) jumper, or the power drop step with turbo
        // held (LT + X). Pressed while a pump fake is live, it steps through under
        // the airborne defender for the up-and-under instead.
        void OnPostWest()
        {
            if (_sprintIntent) { TriggerPostMove(PostMove.PowerDropStep); return; }
            if (IsPosting && _post.FakeActive) { TriggerPostMove(PostMove.UpAndUnder); return; }
            TriggerPostMove(PostMove.TurnaroundJumper);
        }

        // B — the spin move.
        void OnPostEast() => TriggerPostMove(PostMove.Spin);

        // Letting go of a post button puts the shot up, timed like a jump shot: the
        // press starts the move and its release meter, the button-up fires it at that
        // point. (Pressing a post button again still releases it too, so either the
        // hold-and-release or a second tap works.)
        void OnPostButtonReleased()
        {
            if (IsPosting && _post != null && _post.PostShotActive) _post.ReleasePostShot();
        }

        public void TriggerPostMove(PostMove move)
        {
            if (MatchPause.IsPaused || IsStunned || !IsPosting) return;
            // Once a move's shot is charging, any post button releases it (timing
            // the shot) rather than starting a new move.
            if (_post.PostShotActive) { _post.ReleasePostShot(); return; }
            _post.DoMove(move);
        }

        /// <summary>A spin / power drop step beat the defender — break OUT of the
        /// post and drive at the rim (the move never scores on its own; the player
        /// finishes with a dunk/layup, pulls a shot, or passes). Called by
        /// <see cref="PostUpController"/> after it resolves the move's footwork.</summary>
        public void StartPostDrive(PostMove move)
        {
            Vector3 toRim = RimDirection();
            if (_post != null) _post.End();          // leave the post — now a live driver
            _postRepostBlocked = true;               // don't snap back into the post on the held button
            _postMoveType = move;
            _postMoveGestureTimer = postMoveDriveTime;
            if (toRim.sqrMagnitude > 0.01f)
            {
                Vector3 d = toRim.normalized;
                ApplyShove(d * postDriveBurstSpeed); // carry toward the rim
                transform.rotation = Quaternion.LookRotation(d, Vector3.up); // face up to finish
            }
        }

        // ---- AI hooks ------------------------------------------------------

        public void BeginPost()
        {
            if (HasBall && !IsStunned && _cc.isGrounded && !IsPosting)
                _post.Begin(NearestOpponentTo(transform.position));
        }

        public void EndPost() { if (IsPosting) _post.End(); }
        public void PostBackDown() { if (IsPosting) _post.OffenseTap(); }
        public void DoPostMove(PostMove move) { if (IsPosting) _post.DoMove(move); }

        // ---- State changes from other systems ------------------------------

        public void Stun(float seconds, bool fall = false)
        {
            _stunTimer = Mathf.Max(_stunTimer, seconds);
            if (fall) _fallTimer = Mathf.Max(_fallTimer, seconds);
            if (IsPosting) _post.End();
        }

        public void ApplyShove(Vector3 velocity)
        {
            _shoveVel = velocity;
            _shoveTimer = shoveDuration;
        }

        // ---- Helpers -------------------------------------------------------

        public PlayerController NearestOpponentTo(Vector3 point)
        {
            var gm = GameManager.Instance;
            if (gm == null) return null;
            var opponents = gm.TeamFor(GameManager.Opponent(team)).onCourt;
            PlayerController best = null;
            float bestD = Mathf.Infinity;
            foreach (var o in opponents)
            {
                if (o == null || !o.enabled) continue;
                float d = HorizontalDistance(o.transform.position, point);
                if (d < bestD) { bestD = d; best = o; }
            }
            return best;
        }

        /// <summary>The poster currently backing this player down (engaged as the
        /// post defender), or null — drives the defensive-stance animation.</summary>
        public PlayerController PostingMeOnD
        {
            get { var post = FindPosterGuardingMe(); return post != null ? post.transform.GetComponent<PlayerController>() : null; }
        }

        PostUpController FindPosterGuardingMe()
        {
            var gm = GameManager.Instance;
            if (gm == null) return null;
            foreach (var o in gm.TeamFor(GameManager.Opponent(team)).onCourt)
            {
                if (o == null || o.Post == null) continue;
                if (o.Post.IsPosting && o.Post.EngagedDefender == this) return o.Post;
            }
            return null;
        }

        PlayerController FindOpenTeammate()
        {
            var gm = GameManager.Instance;
            if (gm == null) return null;
            PlayerController best = null;
            float bestOpen = -1f;
            foreach (var m in gm.TeamFor(team).onCourt)
            {
                if (m == null || m == this || !m.enabled) continue;
                var opp = m.NearestOpponentTo(m.transform.position);
                float open = opp != null ? HorizontalDistance(opp.transform.position, m.transform.position) : 99f;
                if (open > bestOpen) { bestOpen = open; best = m; }
            }
            return best;
        }

        static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f; b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
