using UnityEngine;
using MarioBasketball.Core;
using MarioBasketball.Characters;
using MarioBasketball.Gameplay;

namespace MarioBasketball.AI
{
    /// <summary>
    /// A basketball brain that drives a non-human <see cref="PlayerController"/>.
    /// It yields the instant the human takes this player over.
    ///
    /// Offense: the ball handler takes good, <b>stat-aware</b> shots (it won't
    /// settle for looks it's bad at — Bowser attacks the rim instead of
    /// chucking threes), kicks to a better/open teammate when smothered, and
    /// otherwise drives. Off-ball players space to the wings and occasionally
    /// cut to the rim. Defense: the closest defender pressures the ball and
    /// tries to strip it; the others guard their man goal-side while sagging to
    /// help. This is a sensible whole-game first pass meant to be tuned from
    /// real play, not a finished AI.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerAI : MonoBehaviour
    {
        [Header("Shot selection")]
        public float shootRange = 6.5f;
        [Tooltip("Shoot when (scoring stat + openness) clears this.")]
        public float shootQualityThreshold = 6.5f;
        public float lowShotClock = 4f;
        public float passUpgradeMargin = 1.5f;

        [Header("Spacing / cuts")]
        public float wingSpacing = 3.5f;
        public float cutTriggerDistance = 3.5f;
        [Range(0f, 1f)] public float cutChance = 0.4f;
        public float cutRepathInterval = 2.5f;
        public float cutReachDistance = 1.5f;

        [Header("Passing")]
        public float openThreshold = 2.6f;
        public float smotheredDistance = 1.6f;
        public float passCooldown = 1.0f;

        [Header("Dribble move")]
        public float dribbleGuardDistance = 1.6f;
        [Range(0f, 1f)] public float dribbleChance = 0.01f;

        [Header("Alley-oop catch")]
        public float oopChaseRange = 6f;
        public float oopJumpDistance = 2.2f;

        [Header("Defense")]
        public float onBallGap = 1.0f;
        public float offBallGap = 1.3f;
        [Range(0f, 1f)] public float helpSag = 0.25f;
        public float sprintDistance = 4f;
        public float stealRange = 1.0f;
        public float pushRange = 1.5f;
        [Tooltip("Per-frame chance the on-ball defender commits a foul.")]
        [Range(0f, 1f)] public float pushChance = 0.0015f;

        [Header("Post offense")]
        public float postRange = 4.5f;
        public float postGuardDist = 2.2f;
        public float aiPostSkillMin = 6f;
        [Range(0f, 1f)] public float aiPostStartChance = 0.015f;
        public float aiPostTapInterval = 0.18f;
        public float aiPostMaxTime = 3.5f;
        public float aiPostFinishLeverage = 5f;
        public float aiPostDeepLeverage = 7f;

        PlayerController _pc;
        float _passTimer;
        float _cutTimer;
        bool _cutting;
        float _postTapTimer;
        float _postDecisionTimer;

        void Awake()
        {
            _pc = GetComponent<PlayerController>();
        }

        void Update()
        {
            if (_passTimer > 0f) _passTimer -= Time.deltaTime;
            if (_cutTimer > 0f) _cutTimer -= Time.deltaTime;

            var gm = GameManager.Instance;
            if (gm == null || _pc == null) return;

            if (MatchPause.IsPaused) return;
            if (_pc.isHuman) return; // the human is driving this player

            if (!_pc.enabled || (_pc.Character != null && _pc.Character.IsBenched) ||
                gm.State != GameState.Playing)
            {
                _pc.SetMoveIntent(Vector2.zero, false);
                return;
            }

            if (_pc.IsStunned)
            {
                _pc.SetMoveIntent(Vector2.zero, false);
                return;
            }

            var ball = gm.ball;
            if (ball == null) return;

            // Cut to the rim and rise to catch an incoming alley-oop.
            if (ball.State == BallController.BallState.Free && ball.IsAlleyOop && ball.PassingTeam == _pc.team)
            {
                Hoop hoop = gm.GetAttackingHoop(_pc.team);
                if (hoop != null && IsClosestTeammateTo(gm, hoop.AimPoint)
                    && HDist(transform.position, hoop.AimPoint) <= oopChaseRange)
                {
                    MoveTo(hoop.AimPoint, sprint: true);
                    if (HDist(transform.position, hoop.AimPoint) < oopJumpDistance) _pc.TriggerJump();
                    return;
                }
            }

            if (ball.State == BallController.BallState.Free)
            {
                ChaseLooseBall(gm, ball);
                return;
            }

            var holder = ball.Holder;
            if (holder != null && holder.team == _pc.team)
            {
                if (holder == _pc)
                {
                    if (_pc.IsPosting) AIPostOffense(gm);
                    else OffenseWithBall(gm);
                }
                else OffenseOffBall(gm);
            }
            else
            {
                Defense(gm, holder);
            }
        }

        // ---- Offense -------------------------------------------------------

        void OffenseWithBall(GameManager gm)
        {
            Hoop hoop = gm.GetAttackingHoop(_pc.team);
            if (hoop == null) return;

            Vector3 aim = hoop.AimPoint;
            float dist = HDist(transform.position, aim);
            float nearestDef = NearestOpponentDistance(gm, transform.position);
            float shotClock = gm.Shot != null ? gm.Shot.Remaining : 20f;

            // Back a smaller, guarding defender down when we're a post threat.
            if (ShouldPostUp(dist, nearestDef))
            {
                _pc.BeginPost();
                if (_pc.IsPosting)
                {
                    _postDecisionTimer = aiPostMaxTime;
                    _postTapTimer = 0f;
                    return;
                }
            }

            float quality = ShotQuality(dist, nearestDef);
            bool forced = shotClock < lowShotClock;

            if (dist <= shootRange && (quality >= shootQualityThreshold || forced))
            {
                _pc.SetMoveIntent(Vector2.zero, false);
                _pc.TriggerShoot();
                return;
            }

            // Smothered → look for a meaningfully better shot elsewhere.
            if (_passTimer <= 0f && nearestDef < smotheredDistance)
            {
                var mate = BestPassTarget(gm, quality);
                if (mate != null)
                {
                    _pc.PassToward(mate.transform.position + Vector3.up * 0.6f);
                    _passTimer = passCooldown;
                    return;
                }
            }

            // Tightly guarded on the perimeter → try to break the defender down.
            if (nearestDef < dribbleGuardDistance && dist > _pc.paintRadius && Random.value < dribbleChance)
                _pc.AttemptDribbleMove();

            MoveTo(aim, sprint: dist > 5f);
        }

        bool ShouldPostUp(float distToHoop, float nearestDef)
        {
            return !_pc.IsPosting
                && distToHoop <= postRange
                && nearestDef <= postGuardDist
                && _pc.EffectiveStat(StatType.PostOffense) >= aiPostSkillMin
                && Random.value < aiPostStartChance;
        }

        void AIPostOffense(GameManager gm)
        {
            float dt = Time.deltaTime;
            _postTapTimer -= dt;
            _postDecisionTimer -= dt;

            _pc.SetMoveIntent(Vector2.zero, false); // PostUpController drives the back-down

            if (_postTapTimer <= 0f)
            {
                _pc.PostBackDown();
                _postTapTimer = aiPostTapInterval;
            }

            float lev = _pc.Post != null ? _pc.Post.Leverage : 0f;
            if (lev >= aiPostFinishLeverage || _postDecisionTimer <= 0f)
                _pc.DoPostMove(lev >= aiPostDeepLeverage ? PostMove.DropStep : PostMove.Hook);
        }

        void OffenseOffBall(GameManager gm)
        {
            if (_cutTimer <= 0f)
            {
                _cutTimer = cutRepathInterval;
                float myDef = NearestOpponentDistance(gm, transform.position);
                _cutting = myDef > cutTriggerDistance && Random.value < cutChance;
            }

            if (_cutting)
            {
                Hoop hoop = gm.GetAttackingHoop(_pc.team);
                Vector3 aim = hoop != null ? hoop.AimPoint : Vector3.zero;
                MoveTo(aim, sprint: true);
                if (HDist(transform.position, aim) < cutReachDistance) _cutting = false;
            }
            else
            {
                MoveTo(SpacingSpot(gm), sprint: false);
            }
        }

        /// <summary>How good a shot from here is: scoring stat for the range
        /// plus a bonus for being open.</summary>
        float ShotQuality(float dist, float nearestDef)
        {
            StatType stat = ShotStatFor(dist);
            float statEff = _pc.EffectiveStat(stat);
            // Inside, a dunker finishes off Dunk — value the better of the two.
            if (stat == StatType.InsideScoring)
                statEff = Mathf.Max(statEff, _pc.EffectiveStat(StatType.Dunk));
            float openness = Mathf.Clamp(nearestDef - 1f, 0f, 3f);
            return statEff + openness;
        }

        StatType ShotStatFor(float dist) =>
            dist >= _pc.threePointDistance ? StatType.ThreePoint :
            dist <= _pc.paintRadius ? StatType.InsideScoring :
            StatType.MidRange;

        PlayerController BestPassTarget(GameManager gm, float myQuality)
        {
            Hoop hoop = gm.GetAttackingHoop(_pc.team);
            Vector3 aim = hoop != null ? hoop.AimPoint : Vector3.zero;

            PlayerController best = null;
            float bestQuality = myQuality + passUpgradeMargin;
            foreach (var mate in gm.TeamFor(_pc.team).onCourt)
            {
                if (mate == null || mate == _pc || !mate.enabled) continue;
                float matesDef = NearestOpponentDistance(gm, mate.transform.position);
                if (matesDef < openThreshold) continue; // covered

                float dist = HDist(mate.transform.position, aim);
                StatType stat = ShotStatForDistance(mate, dist);
                float q = mate.EffectiveStat(stat) + Mathf.Clamp(matesDef - 1f, 0f, 3f);
                if (q > bestQuality) { bestQuality = q; best = mate; }
            }
            return best;
        }

        static StatType ShotStatForDistance(PlayerController p, float dist) =>
            dist >= p.threePointDistance ? StatType.ThreePoint :
            dist <= p.paintRadius ? StatType.InsideScoring :
            StatType.MidRange;

        Vector3 SpacingSpot(GameManager gm)
        {
            Hoop hoop = gm.GetAttackingHoop(_pc.team);
            Vector3 aim = hoop != null ? hoop.AimPoint : Vector3.zero;
            float side = (GetInstanceID() & 1) == 0 ? 1f : -1f;
            float towardCentre = aim.z >= 0f ? -1f : 1f;
            return new Vector3(
                Mathf.Clamp(side * wingSpacing, -6f, 6f),
                1.1f,
                aim.z + towardCentre * wingSpacing);
        }

        // ---- Defense -------------------------------------------------------

        void Defense(GameManager gm, PlayerController holder)
        {
            Vector3 defendHoop = DefendedHoop(gm);

            // If I'm being backed down, hold goal-side; my resistance is automatic.
            if (holder != null && holder.IsPosting && holder.Post != null && holder.Post.EngagedDefender == _pc)
            {
                Vector3 toHoop = defendHoop - holder.transform.position; toHoop.y = 0f;
                MoveTo(holder.transform.position + toHoop.normalized * 0.8f, sprint: false);
                return;
            }

            bool amOnBall = holder != null && IsClosestTeammateTo(gm, holder.transform.position);

            if (amOnBall)
            {
                Vector3 toHoop = (defendHoop - holder.transform.position);
                toHoop.y = 0f;
                Vector3 target = holder.transform.position + toHoop.normalized * onBallGap;
                MoveTo(target, sprint: HDist(transform.position, target) > sprintDistance);

                float onBall = HDist(transform.position, holder.transform.position);
                if (onBall <= stealRange) _pc.TriggerSteal();
                if (onBall <= pushRange && Random.value < pushChance) _pc.AttemptPush();
                return;
            }

            PlayerController man = NearestOpponent(gm, transform.position, exclude: holder)
                                   ?? NearestOpponent(gm, transform.position, exclude: null);
            if (man == null)
            {
                MoveTo(defendHoop, sprint: false);
                return;
            }

            Vector3 manToHoop = (defendHoop - man.transform.position);
            manToHoop.y = 0f;
            Vector3 basePos = man.transform.position + manToHoop.normalized * offBallGap;

            // Help: sag a fraction toward the ball.
            Vector3 ballPos = gm.ball != null ? gm.ball.transform.position : basePos;
            Vector3 help = ballPos - basePos; help.y = 0f;
            Vector3 target2 = basePos + help * helpSag;
            MoveTo(target2, sprint: HDist(transform.position, target2) > sprintDistance);
        }

        // ---- Shared queries ------------------------------------------------

        void ChaseLooseBall(GameManager gm, BallController ball)
        {
            if (IsClosestTeammateTo(gm, ball.transform.position))
            {
                MoveTo(ball.transform.position, sprint: true);
                // Go up for the board when we're on top of it.
                if (HDist(transform.position, ball.transform.position) < 1.6f)
                    _pc.TriggerJump();
            }
            else
            {
                _pc.SetMoveIntent(Vector2.zero, false);
            }
        }

        Vector3 DefendedHoop(GameManager gm)
        {
            Hoop hoop = gm.GetAttackingHoop(GameManager.Opponent(_pc.team));
            return hoop != null ? hoop.AimPoint : Vector3.zero;
        }

        bool IsClosestTeammateTo(GameManager gm, Vector3 point)
        {
            float mine = HDist(transform.position, point);
            foreach (var mate in gm.TeamFor(_pc.team).onCourt)
            {
                if (mate == null || mate == _pc || !mate.enabled) continue;
                float d = HDist(mate.transform.position, point);
                if (d < mine || (Mathf.Approximately(d, mine) && mate.GetInstanceID() < _pc.GetInstanceID()))
                    return false;
            }
            return true;
        }

        float NearestOpponentDistance(GameManager gm, Vector3 point)
        {
            var opp = NearestOpponent(gm, point, exclude: null);
            return opp != null ? HDist(opp.transform.position, point) : Mathf.Infinity;
        }

        PlayerController NearestOpponent(GameManager gm, Vector3 point, PlayerController exclude)
        {
            var opponents = gm.TeamFor(GameManager.Opponent(_pc.team));
            PlayerController best = null;
            float bestD = Mathf.Infinity;
            foreach (var o in opponents.onCourt)
            {
                if (o == null || o == exclude || !o.enabled) continue;
                float d = HDist(o.transform.position, point);
                if (d < bestD) { bestD = d; best = o; }
            }
            return best;
        }

        // ---- Helpers -------------------------------------------------------

        void MoveTo(Vector3 worldTarget, bool sprint)
        {
            Vector3 to = worldTarget - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude < 0.04f)
            {
                _pc.SetMoveIntent(Vector2.zero, false);
                return;
            }
            Vector3 dir = to.normalized;
            _pc.SetMoveIntent(new Vector2(dir.x, dir.z), sprint);
        }

        static float HDist(Vector3 a, Vector3 b)
        {
            a.y = 0f; b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
