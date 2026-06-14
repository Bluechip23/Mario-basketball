using UnityEngine;

namespace MarioBasketball.Characters
{
    /// <summary>
    /// The live, in-game wrapper around a character's <see cref="CharacterStats"/>.
    /// It owns the two things that change minute to minute:
    /// <list type="bullet">
    ///   <item><b>Energy</b> (0-100) — the realised stamina. 100 energy means
    ///   100% of a stat is available; at 60 energy every stat performs at 60%.
    ///   A high Stamina stat makes energy fade more slowly.</item>
    ///   <item><b>On fire</b> — a temporary state that boosts stats and
    ///   <i>mitigates</i> the stamina penalty (effectiveness can't sag as low),
    ///   while energy keeps draining and won't refill to full.</item>
    /// </list>
    /// Gameplay code should always ask <see cref="GetEffective"/> rather than
    /// reading the raw stat, so stamina and fire are respected automatically.
    /// The streak logic that flips <see cref="OnFire"/> lives in a separate
    /// system (see roadmap) and calls <see cref="SetOnFire"/>.
    /// </summary>
    public class PlayerCharacter : MonoBehaviour
    {
        [Header("Identity")]
        public CharacterStats stats = new CharacterStats();

        [Header("Energy / stamina")]
        public float maxEnergy = 100f;
        [SerializeField] float energy = 100f;
        [Tooltip("Base energy lost per second of normal activity at Stamina 5.")]
        public float baseDrainPerSecond = 1.2f;
        [Tooltip("Extra drain multiplier while sprinting.")]
        public float sprintDrainMultiplier = 2.5f;
        [Tooltip("Energy recovered per second while idling on court.")]
        public float idleRecoverPerSecond = 3f;
        [Tooltip("Energy recovered per second while on the bench (30/min).")]
        public float benchRecoverPerSecond = 0.5f;

        /// <summary>While benched a player doesn't tire and slowly recovers.</summary>
        public bool IsBenched { get; set; }

        [Header("On fire tuning")]
        [Tooltip("Flat bonus added to every stat while on fire.")]
        public float onFireStatBonus = 2f;
        [Tooltip("Effectiveness can't drop below this fraction while on fire.")]
        [Range(0f, 1f)] public float onFireEffectivenessFloor = 0.85f;
        [Tooltip("Drain multiplier applied while on fire.")]
        public float onFireDrainMultiplier = 1.5f;
        [Tooltip("Energy can't recover above this fraction of max while on fire.")]
        [Range(0f, 1f)] public float onFireRecoverCap = 0.5f;

        public float Energy => energy;
        public float EnergyFraction => maxEnergy > 0f ? energy / maxEnergy : 0f;
        public bool OnFire { get; private set; }

        /// <summary>Daisy's Killer Instinct bonus: a flat boost the owning
        /// controller refreshes each frame, scaled by how gassed the opponents
        /// are (0 for everyone else). It only sharpens her scoring touch and
        /// on-ball defence — see <see cref="TraitBonusForStat"/>.</summary>
        public float KillerBonus { get; set; }

        // Heat-check streak state, driven by GameManager:
        /// <summary>This player's consecutive made shots (broken by a miss or a
        /// teammate scoring).</summary>
        public int ConsecutiveMakes;
        /// <summary>Whether an opponent has scored during the current make run
        /// (disqualifies the 3-in-a-row path to on-fire; the 6-in-a-row path
        /// ignores it).</summary>
        public bool OpponentScoredDuringRun;

        /// <summary>Birdo's Hot Hand running tally: +1 per made field goal, -1 per
        /// miss (a whole-game total). Her shooting bonus is half of this, truncated
        /// toward zero — see <see cref="HotHandBonus"/>.</summary>
        public int ShootingRhythm;

        /// <summary>The Hot Hand rating bonus: +1 for every two net makes, rounded
        /// toward zero (two misses to drop a tier, symmetric when negative).</summary>
        public int HotHandBonus => ShootingRhythm / 2;

        bool _movingThisFrame;
        bool _sprintingThisFrame;

        void Awake()
        {
            stats?.Validate();
            energy = maxEnergy;
        }

        /// <summary>
        /// Called by the controller each frame to describe what the player is
        /// doing, which drives how fast energy drains or recovers.
        /// </summary>
        public void ReportActivity(bool moving, bool sprinting)
        {
            _movingThisFrame = moving;
            _sprintingThisFrame = sprinting;
        }

        void Update()
        {
            UpdateEnergy(Time.deltaTime);
        }

        void UpdateEnergy(float dt)
        {
            if (IsBenched)
            {
                // Resting players don't tire; they recover 30/min to full.
                energy = Mathf.Min(maxEnergy, energy + benchRecoverPerSecond * dt);
                return;
            }

            if (_movingThisFrame)
            {
                // Higher Stamina stat → slower fade. Stat 1 ≈ 2x, stat 10 ≈ 0.5x.
                float staminaFactor = Mathf.Lerp(2f, 0.5f, (stats.stamina - CharacterStats.Min) / 9f);
                float drain = baseDrainPerSecond * staminaFactor;
                if (_sprintingThisFrame) drain *= sprintDrainMultiplier;
                if (OnFire) drain *= onFireDrainMultiplier;
                energy -= drain * dt;
            }
            else
            {
                float recovered = energy + idleRecoverPerSecond * dt;
                float cap = OnFire ? maxEnergy * onFireRecoverCap : maxEnergy;
                // Never claw energy *down* via the cap if it's already higher.
                energy = Mathf.Min(recovered, Mathf.Max(energy, cap));
            }

            energy = Mathf.Clamp(energy, 0f, maxEnergy);
        }

        /// <summary>
        /// Fraction (roughly 0-1) by which raw stats are scaled right now. While
        /// on fire the penalty is mitigated by <see cref="onFireEffectivenessFloor"/>.
        /// </summary>
        public float EffectivenessMultiplier =>
            OnFire ? Mathf.Max(EnergyFraction, onFireEffectivenessFloor) : EnergyFraction;

        /// <summary>
        /// The in-game value of a stat after stamina scaling, the on-fire bonus,
        /// and any Killer Instinct boost. This is what gameplay systems consume.
        /// </summary>
        public float GetEffective(StatType stat) => GetEffectiveForStat(stats.Get(stat), stat);

        /// <summary>Apply the same stamina + on-fire scaling to an arbitrary base
        /// rating with no trait boost — used by traits that override a stat
        /// (quick catch-and-shoot, offensive rebound, smooth passer).</summary>
        public float GetEffectiveFor(float rawStat)
        {
            float value = rawStat * EffectivenessMultiplier + (OnFire ? onFireStatBonus : 0f);
            return Mathf.Clamp(value, 0f, CharacterStats.Max + onFireStatBonus);
        }

        /// <summary>Scale a raw rating for a specific stat, folding in any hidden-
        /// trait shooting boost (Daisy's Killer Instinct, Birdo's Hot Hand). A
        /// trait may let one of its stats climb to 11; everything else caps at 10.</summary>
        public float GetEffectiveForStat(float rawRating, StatType stat)
        {
            rawRating += TraitBonusForStat(stat);
            float ceiling = TraitCeilingForStat(stat);
            rawRating = Mathf.Min(rawRating, ceiling);
            float value = rawRating * EffectivenessMultiplier + (OnFire ? onFireStatBonus : 0f);
            return Mathf.Clamp(value, 0f, ceiling + onFireStatBonus);
        }

        /// <summary>Hidden-trait bonus to a stat's raw rating: Daisy's Killer
        /// Instinct (scaled by opponent fatigue) on Mid/3PT/Inside/Perimeter-D,
        /// or Birdo's Hot Hand (her make/miss rhythm) on 3PT/Mid.</summary>
        float TraitBonusForStat(StatType stat)
        {
            if (stats == null) return 0f;
            switch (stats.hiddenTrait)
            {
                case HiddenTrait.KillerInstinct: return IsKillerStat(stat) ? KillerBonus : 0f;
                case HiddenTrait.HotHand:        return IsHotHandStat(stat) ? HotHandBonus : 0f;
                default:                         return 0f;
            }
        }

        /// <summary>A trait may lift one of its stats to 11; otherwise stats cap at 10.</summary>
        float TraitCeilingForStat(StatType stat)
        {
            if (stats != null)
            {
                if (stats.hiddenTrait == HiddenTrait.KillerInstinct && stat == StatType.MidRange) return 11f;
                if (stats.hiddenTrait == HiddenTrait.HotHand && IsHotHandStat(stat)) return 11f;
            }
            return CharacterStats.Max;
        }

        // Killer Instinct sharpens shot-making and on-ball defence; Hot Hand only
        // her jump shot.
        static bool IsKillerStat(StatType s) =>
            s == StatType.MidRange || s == StatType.ThreePoint
            || s == StatType.InsideScoring || s == StatType.PerimeterDefense;
        static bool IsHotHandStat(StatType s) =>
            s == StatType.ThreePoint || s == StatType.MidRange;

        public void SetOnFire(bool value) => OnFire = value;

        /// <summary>Add (or remove) energy, clamped to the legal range. Used by
        /// timeouts (+30) and any other instantaneous stamina effects.</summary>
        public void AddEnergy(float amount) => energy = Mathf.Clamp(energy + amount, 0f, maxEnergy);
    }
}
