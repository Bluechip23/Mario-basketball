using UnityEngine;
using MarioBasketball.Core;
using MarioBasketball.Characters;

namespace MarioBasketball.Gameplay
{
    /// <summary>
    /// The post-up game for one player. While posting, the player turns their
    /// back to the basket and a <b>back-down battle</b> plays out against the
    /// engaged defender:
    /// <list type="bullet">
    ///   <item>The offense taps Back Down (<see cref="OffenseTap"/>) to push in;
    ///   each tap is worth their effective <b>Power</b>.</item>
    ///   <item>The defender resists — a human defender taps
    ///   (<see cref="DefenderTap"/>); an AI defender resists automatically from
    ///   Power + Post Defense. More power means fewer taps are needed.</item>
    /// </list>
    /// A running <see cref="Leverage"/> value tilts toward whoever's winning:
    /// positive backs the offense toward the rim (better post shots); a big
    /// negative shoves the offense out of the post, and a bigger one knocks them
    /// down and turns it over. <see cref="DoMove"/> resolves the post moves.
    ///
    /// This component lives on the offensive player (paired with
    /// <see cref="PlayerController"/>); it is dormant until <see cref="Begin"/>
    /// is called by input (human) or the AI.
    /// </summary>
    public class PostUpController : MonoBehaviour
    {
        [Header("Back-down battle")]
        [Tooltip("Leverage gained per Power point on each offense tap.")]
        public float tapImpulse = 0.06f;
        [Tooltip("AI defender passive resist scale (× Power per second).")]
        public float autoDefenderResist = 0.45f;
        [Tooltip("Leverage bleeds back to zero at this rate per second.")]
        public float leverageDecay = 1.0f;
        public float maxLeverage = 10f;
        [Tooltip("Back-down speed (m/s) per unit of leverage.")]
        public float leverageToSpeed = 0.22f;
        public float maxBackdownSpeed = 1.8f;
        [Tooltip("Leverage at/below which the defender shoves the offense out.")]
        public float shoveThreshold = -6f;
        [Tooltip("Leverage at/below which the offense is knocked down (turnover).")]
        public float knockdownThreshold = -9f;
        public float shovePower = 6f;
        public float knockdownStun = 1.1f;
        public float faceLerp = 10f;

        [Header("Post moves")]
        public float moveFlightTime = 0.9f;
        public float dropStepLungeLeverage = 2f;
        public float blockBaseChance = 0.05f;
        public float blockStatScale = 0.05f;
        public float blockMaxChance = 0.55f;
        public float spinStripBaseChance = 0.12f;
        public float fakeBiteBaseChance = 0.5f;
        public float fakeWindow = 1.2f;
        public float fakeLeverageBonus = 1.5f;
        public float fakeQualityBonus = 3f;

        [Header("Advanced post moves")]
        [Tooltip("Power gap at/above which a power drop step flattens the defender.")]
        public float powerDropKnockdownGap = 3f;
        public float powerDropShove = 5f;
        [Tooltip("Block-chance multiplier on a turnaround jumper (the fade).")]
        [Range(0f, 1f)] public float turnaroundBlockMult = 0.4f;
        [Tooltip("Block-chance multiplier on an up-and-under off a bitten fake.")]
        [Range(0f, 1f)] public float upAndUnderBlockMult = 0.25f;

        [Header("Shimmy (right-stick hard dribble in the post)")]
        public float shimmyPower = 5f;
        public float shimmyCooldownTime = 0.6f;
        [Tooltip("How long the defender is frozen when the shimmy shakes them.")]
        public float shimmyFreeze = 0.45f;

        public bool IsPosting { get; private set; }
        /// <summary>True while a successful fake still has the defender in the air.</summary>
        public bool FakeActive => _fakeActive;
        public float Leverage => _leverage;
        public PlayerController EngagedDefender => _defender;
        public Vector3 DriveVelocity { get; private set; }

        PlayerController _pc;
        float _leverage;
        PlayerController _defender;
        bool _fakeActive;
        float _fakeTimer;
        float _shimmyCooldown;

        void Awake()
        {
            _pc = GetComponent<PlayerController>();
        }

        public void Begin(PlayerController defender)
        {
            if (IsPosting) return;
            IsPosting = true;
            _defender = defender;
            _leverage = 0f;
            _fakeActive = false;
            DriveVelocity = Vector3.zero;
        }

        public void End()
        {
            IsPosting = false;
            _defender = null;
            _fakeActive = false;
            DriveVelocity = Vector3.zero;
        }

        public void OffenseTap()
        {
            if (!IsPosting) return;
            float power = _pc.EffectiveStat(StatType.Power);
            _leverage += power * tapImpulse * (_fakeActive ? 1.5f : 1f);
            _leverage = Mathf.Min(_leverage, maxLeverage);
        }

        /// <summary>Called when the (human) defender taps to bump the poster.</summary>
        public void DefenderTap()
        {
            if (!IsPosting || _defender == null) return;
            _leverage -= _defender.EffectiveStat(StatType.Power) * tapImpulse;
        }

        /// <summary>A right-stick hard dribble while posting: a quick shimmy in
        /// <paramref name="dir"/> to create separation. Toward the basket it also
        /// gains a little leverage; if the move shakes the defender (Post Offense
        /// vs Post Defense) they're frozen for a beat.</summary>
        public void Shimmy(Vector3 dir)
        {
            if (!IsPosting || _shimmyCooldown > 0f) return;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) return;
            _shimmyCooldown = shimmyCooldownTime;
            dir.Normalize();

            _pc.ApplyShove(dir * shimmyPower);

            var gm = GameManager.Instance;
            Hoop hoop = gm != null ? gm.GetAttackingHoop(_pc.team) : null;
            if (hoop != null)
            {
                Vector3 toBasket = hoop.AimPoint - transform.position; toBasket.y = 0f;
                if (toBasket.sqrMagnitude > 0.01f && Vector3.Dot(dir, toBasket.normalized) > 0.5f)
                    _leverage = Mathf.Min(maxLeverage, _leverage + 1f);
            }

            if (_defender != null)
            {
                float offense = _pc.EffectiveStat(StatType.PostOffense);
                float defense = _defender.EffectiveStat(StatType.PostDefense);
                float shake = Mathf.Clamp(0.4f + 0.05f * (offense - defense), 0.1f, 0.85f);
                if (Random.value < shake) _defender.Stun(shimmyFreeze);
            }
        }

        void Update()
        {
            if (!IsPosting) return;

            var gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.Playing || !_pc.HasBall)
            {
                End();
                return;
            }

            if (_defender == null || !_defender.enabled)
                _defender = NearestOpponent(gm);

            float dt = Time.deltaTime;
            _leverage = Mathf.MoveTowards(_leverage, 0f, leverageDecay * dt);

            // AI defenders resist automatically; a human defender taps instead.
            if (_defender != null && !_defender.isHuman)
            {
                float resist = _defender.EffectiveStat(StatType.Power) * autoDefenderResist * dt;
                resist *= 1f + _defender.EffectiveStat(StatType.PostDefense) / 20f;
                _leverage -= resist;
            }
            _leverage = Mathf.Min(_leverage, maxLeverage);

            // Drive toward the basket (or get shoved back) and keep the back to it.
            Hoop hoop = gm.GetAttackingHoop(_pc.team);
            Vector3 toBasket = hoop != null ? hoop.AimPoint - transform.position : transform.forward;
            toBasket.y = 0f;
            Vector3 dir = toBasket.sqrMagnitude > 0.01f ? toBasket.normalized : transform.forward;
            float speed = Mathf.Clamp(_leverage * leverageToSpeed, -maxBackdownSpeed, maxBackdownSpeed);
            DriveVelocity = dir * speed;

            if (toBasket.sqrMagnitude > 0.01f)
            {
                Quaternion want = Quaternion.LookRotation(-dir, Vector3.up); // back to basket
                transform.rotation = Quaternion.Slerp(transform.rotation, want, faceLerp * dt);
            }

            if (_fakeActive)
            {
                _fakeTimer -= dt;
                if (_fakeTimer <= 0f) _fakeActive = false;
            }
            if (_shimmyCooldown > 0f) _shimmyCooldown -= dt;

            if (_leverage <= knockdownThreshold) DefenderWins(knockdown: true);
            else if (_leverage <= shoveThreshold) DefenderWins(knockdown: false);
        }

        void DefenderWins(bool knockdown)
        {
            var gm = GameManager.Instance;
            Vector3 away = -DriveVelocity; away.y = 0f;
            if (away.sqrMagnitude < 0.01f) away = -transform.forward;

            if (knockdown)
            {
                _pc.Stun(knockdownStun, fall: true);
                if (gm != null && gm.ball != null && gm.ball.Holder == _pc && _defender != null)
                {
                    gm.ball.PickUp(_defender);
                    gm.OnPossessionGained(_defender);
                }
            }
            else
            {
                _pc.ApplyShove(away.normalized * shovePower);
            }
            End();
        }

        public void DoMove(PostMove move)
        {
            if (!IsPosting) return;
            var gm = GameManager.Instance;
            if (gm == null || !_pc.HasBall) { End(); return; }

            float offense = _pc.EffectiveStat(StatType.PostOffense);
            float defense = _defender != null ? _defender.EffectiveStat(StatType.PostDefense) : 0f;
            float deep = Mathf.Clamp01(_leverage / maxLeverage);
            float fakeBonus = _fakeActive ? fakeQualityBonus : 0f;

            switch (move)
            {
                case PostMove.Fake:
                    ResolveFake(offense, defense);
                    return;

                case PostMove.Hook:
                    ResolveShot(offense - 0.4f * defense + 4f * deep + 1f + fakeBonus, blockable: false);
                    break;

                case PostMove.DropStep:
                    _leverage = Mathf.Min(maxLeverage, _leverage + dropStepLungeLeverage);
                    deep = Mathf.Clamp01(_leverage / maxLeverage);
                    float finish = Mathf.Max(offense, _pc.EffectiveStat(StatType.InsideScoring));
                    ResolveShot(finish - 0.5f * defense + 5f * deep + fakeBonus, blockable: true);
                    break;

                case PostMove.Spin:
                    float spinQuality = offense - 0.6f * defense + 3f * deep + fakeBonus;
                    float strip = Mathf.Clamp(spinStripBaseChance + 0.04f * (defense - offense), 0f, 0.6f);
                    if (Random.value < strip && _defender != null)
                    {
                        // Spun into trouble — stripped.
                        gm.ball.PickUp(_defender);
                        gm.OnPossessionGained(_defender);
                        gm.OnShotMissed(_pc); // lost it — streak broken
                        End();
                        return;
                    }
                    ResolveShot(spinQuality, blockable: true);
                    break;

                case PostMove.SkyHook:
                    // Released above everything — unblockable, but a tougher make.
                    ResolveShot(offense - 0.3f * defense + 3f * deep + fakeBonus - 0.5f, blockable: false);
                    break;

                case PostMove.PowerDropStep:
                    ResolvePowerDropStep(offense, defense, fakeBonus);
                    break;

                case PostMove.TurnaroundJumper:
                    // Face up and fade — lives on Mid Range, the fade kills the block.
                    float mid = _pc.EffectiveStat(StatType.MidRange);
                    float fadeQuality = 0.5f * offense + 0.7f * mid - 0.35f * defense + 2f * deep + fakeBonus;
                    ResolveShot(fadeQuality, blockable: true, blockMult: turnaroundBlockMult);
                    break;

                case PostMove.UpAndUnder:
                    // Step through under the (ideally airborne) defender. Without a
                    // bitten fake first it's just a slow, contestable step-through.
                    float inside = _pc.EffectiveStat(StatType.InsideScoring);
                    if (_fakeActive)
                        ResolveShot(offense + 0.5f * inside - 0.3f * defense + 4f * deep + fakeQualityBonus,
                            blockable: true, blockMult: upAndUnderBlockMult);
                    else
                        ResolveShot(offense - 0.5f * defense + 3f * deep, blockable: true);
                    break;
            }
        }

        /// <summary>Bulldoze into the lane off the Power stat: shoves the defender
        /// aside (or flattens an overpowered one) before the finish.</summary>
        void ResolvePowerDropStep(float offense, float defense, float fakeBonus)
        {
            float power = _pc.EffectiveStat(StatType.Power);
            _leverage = Mathf.Min(maxLeverage, _leverage + dropStepLungeLeverage + 0.2f * power);
            float deep = Mathf.Clamp01(_leverage / maxLeverage);

            if (_defender != null)
            {
                Vector3 aside = _defender.transform.position - transform.position; aside.y = 0f;
                if (aside.sqrMagnitude < 0.01f) aside = transform.right;
                _defender.ApplyShove(aside.normalized * powerDropShove);
                if (power - _defender.EffectiveStat(StatType.Power) >= powerDropKnockdownGap)
                    _defender.Stun(0.7f, fall: true); // run clean over them
            }

            float finish = Mathf.Max(offense, _pc.EffectiveStat(StatType.InsideScoring), _pc.EffectiveStat(StatType.Dunk));
            ResolveShot(finish + 0.25f * power - 0.5f * defense + 5f * deep + fakeBonus, blockable: true);
        }

        void ResolveShot(float quality, bool blockable, float blockMult = 1f)
        {
            var gm = GameManager.Instance;
            Hoop hoop = gm.GetAttackingHoop(_pc.team);
            if (hoop == null) { End(); return; }

            if (blockable && _defender != null)
            {
                float blk = _defender.EffectiveStat(StatType.Blocks);
                float chance = Mathf.Clamp(blockBaseChance + blockStatScale * (blk - quality), 0f, blockMaxChance) * blockMult;
                if (Random.value < chance)
                {
                    Vector3 away = transform.position - hoop.AimPoint; away.y = 0f;
                    gm.ball.Pass(away.sqrMagnitude > 0.01f ? away : -transform.forward, shovePower * 0.6f);
                    gm.OnShotMissed(_pc); // blocked → streak broken
                    End();
                    return;
                }
            }

            bool onFire = _pc.Character != null && _pc.Character.OnFire;
            float makeChance = ShotMath.MakeChanceFromQuality(quality, onFire);
            bool make = Random.value < makeChance;
            gm.ball.Shoot(hoop.AimPoint, _pc.team, 2, moveFlightTime, ShotMath.AimOffset(make), _pc);
            End();
        }

        void ResolveFake(float offense, float defense)
        {
            float bite = Mathf.Clamp(fakeBiteBaseChance + 0.05f * (offense - defense), 0.1f, 0.9f);
            if (Random.value < bite)
            {
                _fakeActive = true;
                _fakeTimer = fakeWindow;
                _leverage = Mathf.Min(maxLeverage, _leverage + fakeLeverageBonus);
            }
        }

        PlayerController NearestOpponent(GameManager gm)
        {
            var opponents = gm.TeamFor(GameManager.Opponent(_pc.team)).onCourt;
            PlayerController best = null;
            float bestD = Mathf.Infinity;
            foreach (var o in opponents)
            {
                if (o == null || !o.enabled) continue;
                float d = Vector3.Distance(o.transform.position, transform.position);
                if (d < bestD) { bestD = d; best = o; }
            }
            return best;
        }
    }
}
