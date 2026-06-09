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
        public float maxSpread = 1.4f;
        public float minSpread = 0.05f;
        public float dropStepLungeLeverage = 2f;
        public float blockBaseChance = 0.05f;
        public float blockStatScale = 0.05f;
        public float blockMaxChance = 0.55f;
        public float spinStripBaseChance = 0.12f;
        public float fakeBiteBaseChance = 0.5f;
        public float fakeWindow = 1.2f;
        public float fakeLeverageBonus = 1.5f;
        public float fakeQualityBonus = 3f;

        public bool IsPosting { get; private set; }
        public float Leverage => _leverage;
        public PlayerController EngagedDefender => _defender;
        public Vector3 DriveVelocity { get; private set; }

        PlayerController _pc;
        float _leverage;
        PlayerController _defender;
        bool _fakeActive;
        float _fakeTimer;

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
                _pc.Stun(knockdownStun);
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
            }
        }

        void ResolveShot(float quality, bool blockable)
        {
            var gm = GameManager.Instance;
            Hoop hoop = gm.GetAttackingHoop(_pc.team);
            if (hoop == null) { End(); return; }

            if (blockable && _defender != null)
            {
                float blk = _defender.EffectiveStat(StatType.Blocks);
                float chance = Mathf.Clamp(blockBaseChance + blockStatScale * (blk - quality), 0f, blockMaxChance);
                if (Random.value < chance)
                {
                    Vector3 away = transform.position - hoop.AimPoint; away.y = 0f;
                    gm.ball.Pass(away.sqrMagnitude > 0.01f ? away : -transform.forward, shovePower * 0.6f);
                    gm.OnShotMissed(_pc); // blocked → streak broken
                    End();
                    return;
                }
            }

            float t = Mathf.Clamp01((quality - 1f) / 9f);
            float spread = Mathf.Lerp(maxSpread, minSpread, t);
            if (_pc.Character != null && _pc.Character.OnFire && Random.value < _pc.onFireMakeBonus)
                spread = minSpread; // heat check
            gm.ball.Shoot(hoop.AimPoint, _pc.team, 2, moveFlightTime, spread, _pc);
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
