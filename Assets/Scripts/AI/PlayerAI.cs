using UnityEngine;
using MarioBasketball.Core;
using MarioBasketball.Characters;
using MarioBasketball.Gameplay;

namespace MarioBasketball.AI
{
    /// <summary>
    /// A lightweight basketball brain that drives a non-human
    /// <see cref="PlayerController"/> each frame by setting its move intent and
    /// firing its shoot/pass actions. It reads the world from
    /// <see cref="GameManager"/> and behaves by role:
    /// <list type="bullet">
    ///   <item><b>Loose ball</b>: the closest teammate chases it.</item>
    ///   <item><b>Offense, on ball</b>: drive to the rim, shoot in range,
    ///   kick to an open teammate when smothered.</item>
    ///   <item><b>Offense, off ball</b>: spread to a wing spot for spacing.</item>
    ///   <item><b>Defense</b>: guard the ball or the nearest man, goal-side.</item>
    /// </list>
    /// This is intentionally simple — contests, steals, blocks, screens and
    /// smarter shot selection are later systems. It exists so the other five
    /// players actually play.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerAI : MonoBehaviour
    {
        [Header("Shot selection")]
        public float shootRange = 6.5f;
        public float preferredShootDistance = 4.5f;
        public float contestShootDistance = 1.8f;
        public float lowShotClock = 4f;

        [Header("Spacing / passing")]
        public float wingSpacing = 3.5f;
        public float openThreshold = 2.6f;
        public float smotheredDistance = 1.6f;
        public float passCooldown = 1.2f;

        [Header("Defense")]
        public float guardGap = 1.3f;
        public float sprintDistance = 4f;

        PlayerController _pc;
        float _passTimer;

        void Awake()
        {
            _pc = GetComponent<PlayerController>();
        }

        void Update()
        {
            if (_passTimer > 0f) _passTimer -= Time.deltaTime;

            var gm = GameManager.Instance;
            if (gm == null || _pc == null) return;

            // Benched / disabled, or play stopped → stand still.
            if (!_pc.enabled || (_pc.Character != null && _pc.Character.IsBenched) ||
                gm.State != GameState.Playing)
            {
                _pc.SetMoveIntent(Vector2.zero, false);
                return;
            }

            var ball = gm.ball;
            if (ball == null) return;

            if (ball.State == BallController.BallState.Free)
            {
                ChaseLooseBall(gm, ball);
                return;
            }

            var holder = ball.Holder;
            if (holder != null && holder.team == _pc.team)
            {
                if (holder == _pc) OffenseWithBall(gm);
                else OffenseOffBall(gm, holder);
            }
            else
            {
                Defense(gm, holder);
            }
        }

        // ---- Roles ---------------------------------------------------------

        void ChaseLooseBall(GameManager gm, BallController ball)
        {
            // Only the closest teammate commits to the ball; others hold spacing.
            if (IsClosestTeammateTo(gm, ball.transform.position))
                MoveTo(ball.transform.position, sprint: true);
            else
                _pc.SetMoveIntent(Vector2.zero, false);
        }

        void OffenseWithBall(GameManager gm)
        {
            Hoop hoop = gm.GetAttackingHoop(_pc.team);
            if (hoop == null) return;

            Vector3 aim = hoop.AimPoint;
            float dist = HDist(transform.position, aim);
            float nearestDef = NearestOpponentDistance(gm, transform.position);
            float shotClock = gm.Shot != null ? gm.Shot.Remaining : 20f;

            bool inRange = dist <= shootRange;
            bool goodLook = dist <= preferredShootDistance || nearestDef < contestShootDistance;
            if (inRange && (goodLook || shotClock < lowShotClock))
            {
                _pc.SetMoveIntent(Vector2.zero, false);
                _pc.TriggerShoot();
                return;
            }

            // Smothered far from the rim → kick to an open teammate.
            if (nearestDef < smotheredDistance && _passTimer <= 0f)
            {
                var mate = FindOpenTeammate(gm);
                if (mate != null)
                {
                    _pc.PassToward(mate.transform.position + Vector3.up * 0.6f);
                    _passTimer = passCooldown;
                    return;
                }
            }

            MoveTo(aim, sprint: dist > 5f);
        }

        void OffenseOffBall(GameManager gm, PlayerController holder)
        {
            MoveTo(SpacingSpot(gm), sprint: false);
        }

        void Defense(GameManager gm, PlayerController holder)
        {
            Vector3 defendHoop = DefendedHoop(gm);

            PlayerController mark;
            if (holder != null && IsClosestTeammateTo(gm, holder.transform.position))
                mark = holder; // I'm the on-ball defender
            else
                mark = NearestOpponent(gm, transform.position, exclude: null);

            if (mark == null)
            {
                MoveTo(defendHoop, sprint: false);
                return;
            }

            // Stand goal-side, between my man and the basket we defend.
            Vector3 toHoop = (defendHoop - mark.transform.position);
            toHoop.y = 0f;
            Vector3 target = mark.transform.position + toHoop.normalized * guardGap;
            MoveTo(target, sprint: HDist(transform.position, target) > sprintDistance);
        }

        // ---- Spatial queries ----------------------------------------------

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

        Vector3 DefendedHoop(GameManager gm)
        {
            Hoop hoop = gm.GetAttackingHoop(GameManager.Opponent(_pc.team));
            return hoop != null ? hoop.AimPoint : Vector3.zero;
        }

        bool IsClosestTeammateTo(GameManager gm, Vector3 point)
        {
            var team = gm.TeamFor(_pc.team);
            float mine = HDist(transform.position, point);
            foreach (var mate in team.onCourt)
            {
                if (mate == null || mate == _pc || !mate.enabled) continue;
                float d = HDist(mate.transform.position, point);
                // Tie-break on instance id so exactly one player commits.
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

        PlayerController FindOpenTeammate(GameManager gm)
        {
            var team = gm.TeamFor(_pc.team);
            PlayerController best = null;
            float bestD = Mathf.Infinity;
            foreach (var mate in team.onCourt)
            {
                if (mate == null || mate == _pc || !mate.enabled) continue;
                if (NearestOpponentDistance(gm, mate.transform.position) < openThreshold) continue;
                float d = HDist(transform.position, mate.transform.position);
                if (d < bestD) { bestD = d; best = mate; }
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
