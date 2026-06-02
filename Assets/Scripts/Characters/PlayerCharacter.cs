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
        /// The in-game value of a stat after stamina scaling and the on-fire
        /// bonus. This is what gameplay systems should consume.
        /// </summary>
        public float GetEffective(StatType stat)
        {
            float raw = stats.Get(stat);
            float value = raw * EffectivenessMultiplier + (OnFire ? onFireStatBonus : 0f);
            return Mathf.Clamp(value, 0f, CharacterStats.Max + onFireStatBonus);
        }

        public void SetOnFire(bool value) => OnFire = value;

        /// <summary>Add (or remove) energy, clamped to the legal range. Used by
        /// timeouts (+30) and any other instantaneous stamina effects.</summary>
        public void AddEnergy(float amount) => energy = Mathf.Clamp(energy + amount, 0f, maxEnergy);

        public void RefillEnergy() => energy = maxEnergy;
    }
}
