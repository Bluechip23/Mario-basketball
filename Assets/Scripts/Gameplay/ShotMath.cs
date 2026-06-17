using UnityEngine;
using MarioBasketball.Characters;

namespace MarioBasketball.Gameplay
{
    /// <summary>
    /// Turns a shot into an explicit <b>make probability</b>, then into an aim
    /// offset (a make lands near dead-centre; a miss lands off the rim). This
    /// lets every modifier compose as a clean percentage:
    /// <list type="bullet">
    ///   <item>Base make% from the relevant scoring stat (1-10).</item>
    ///   <item><b>Distance falloff within the zone</b> — the deeper the shot,
    ///   the lower the make%. Threes lose ~4% per foot beyond 1 ft past the arc;
    ///   mid/inside fall off more gently.</item>
    ///   <item><b>Deep-three specialists</b> (Peach) instead <i>gain</i> on
    ///   step-back threes, peaking ~+10% around 4-5 ft behind the line, until
    ///   they finally get penalised past ~10 ft.</item>
    ///   <item>Defender contest (Perimeter/Post Defense) subtracts make%.</item>
    ///   <item>On fire adds a flat +30% (applied after the separate block roll,
    ///   so it never helps avoid a block — only helps the ball drop).</item>
    /// </list>
    /// Tunables are public statics so they're easy to dial from play-testing.
    /// </summary>
    public static class ShotMath
    {
        public static float BaseMin = 0.28f;   // make% at stat 1
        public static float BaseMax = 0.85f;   // make% at stat 10
        public static float MinChance = 0.03f;
        public static float MaxChance = 0.97f;
        public static float OnFireBonus = 0.30f;

        /// <summary>Flat make% knocked off every three-point attempt (on top of
        /// the per-foot distance falloff). Pulls threes a notch below mid-range
        /// for the same stat, while a wide-open, well-timed three from a real
        /// shooter still drops at a healthy clip.</summary>
        public static float ThreeBasePenalty = 0.08f;

        public static float ContestRange = 3f;
        public static float ContestMaxPenalty = 0.35f;

        /// <summary>Make% a full-strength fadeaway costs. Flat — the same for
        /// every shooter, regardless of stats (an Acrobat is the lone exception:
        /// he pays nothing). A fadeaway separately eases the block/contest in
        /// <c>PlayerController</c>; this is the difficulty side of that trade.</summary>
        public static float FadeMakePenalty = 0.18f;

        // Distance falloff per foot, beyond a grace zone (feet), from the zone's near edge.
        public static float ThreePerFoot = 0.04f, ThreeGraceFt = 1f;
        public static float MidPerFoot = 0.02f, MidGraceFt = 1f;
        public static float InsidePerFoot = 0.015f, InsideGraceFt = 0f;
        public static float DeepSpecialistMaxFt = 10f; // a deep specialist suffers past this

        const float MetersToFeet = 3.28084f;

        public static float BaseFromStat(float statEff)
            => Mathf.Lerp(BaseMin, BaseMax, Mathf.Clamp01((statEff - 1f) / 9f));

        /// <summary>Make probability for a jump shot from a court position.
        /// <paramref name="statOverride"/> (&gt;= 0) replaces the effective
        /// scoring stat — used by traits like quick catch-and-shoot.
        /// <paramref name="contestScale"/> (&lt; 1) eases the defender's contest,
        /// e.g. the separation a fadeaway creates.</summary>
        public static float MakeChance(PlayerController shooter, StatType zone, float distMeters,
                                       PlayerController defender, bool onFire, float statOverride = -1f,
                                       float contestScale = 1f)
        {
            float statEff = statOverride >= 0f ? statOverride : shooter.EffectiveStat(zone);
            float p = BaseFromStat(statEff);
            p += DistanceModifier(shooter, zone, distMeters);
            if (zone == StatType.ThreePoint) p -= ThreeBasePenalty;
            p -= ContestPenalty(shooter, defender, zone) * contestScale;
            if (onFire) p += OnFireBonus;
            return Mathf.Clamp(p, MinChance, MaxChance);
        }

        /// <summary>Make% a fadeaway subtracts, given how hard it faded (0-1).
        /// Identical for everyone — no stat softens it — except an
        /// <see cref="HiddenTrait.Acrobat"/>, who pays nothing for altering his
        /// shot in the air (Baby Mario).</summary>
        public static float FadePenalty(PlayerController shooter, float fadeAmount)
        {
            if (fadeAmount <= 0f) return 0f;
            if (IsAcrobat(shooter)) return 0f;
            return FadeMakePenalty * Mathf.Clamp01(fadeAmount);
        }

        static bool IsAcrobat(PlayerController shooter)
            => shooter != null && shooter.Character != null && shooter.Character.stats != null
               && shooter.Character.stats.hiddenTrait == HiddenTrait.Acrobat;

        /// <summary>Make probability for a post move, driven by its quality score.</summary>
        public static float MakeChanceFromQuality(float quality, bool onFire)
        {
            float p = Mathf.Lerp(BaseMin, BaseMax, Mathf.Clamp01((quality - 1f) / 9f));
            if (onFire) p += OnFireBonus;
            return Mathf.Clamp(p, MinChance, MaxChance);
        }

        static float DistanceModifier(PlayerController shooter, StatType zone, float distMeters)
        {
            float nearEdge =
                zone == StatType.ThreePoint ? shooter.threePointDistance :
                zone == StatType.MidRange ? shooter.paintRadius : 0f;
            float depthFt = Mathf.Max(0f, (distMeters - nearEdge) * MetersToFeet);

            if (zone == StatType.ThreePoint)
            {
                if (IsDeepSpecialist(shooter)) return DeepSpecialistBonus(depthFt);
                return -PerFootPenalty(depthFt, ThreePerFoot, ThreeGraceFt);
            }
            if (zone == StatType.MidRange) return -PerFootPenalty(depthFt, MidPerFoot, MidGraceFt);
            return -PerFootPenalty(depthFt, InsidePerFoot, InsideGraceFt);
        }

        static float PerFootPenalty(float depthFt, float perFoot, float graceFt)
            => perFoot * Mathf.Max(0f, depthFt - graceFt);

        static bool IsDeepSpecialist(PlayerController shooter)
            => shooter.Character != null && shooter.Character.stats != null
               && shooter.Character.stats.hiddenTrait == HiddenTrait.DeepThreeSpecialist;

        /// <summary>Peach's curve: y = e^(-0.1543 (x-4.5)^2) * 10 percent, for x
        /// (feet behind the line) in 1-8; 9-10 ft hold the 8 ft value; past 10 ft
        /// the normal deep penalty kicks in.</summary>
        static float DeepSpecialistBonus(float depthFt)
        {
            if (depthFt <= 0f) return 0f;
            if (depthFt > DeepSpecialistMaxFt)
                return -PerFootPenalty(depthFt - DeepSpecialistMaxFt, ThreePerFoot, 0f);
            float x = Mathf.Min(depthFt, 8f); // 9-10 ft same as 8 ft
            float y = Mathf.Exp(-0.1543f * (x - 4.5f) * (x - 4.5f)) * 10f; // percent
            return y / 100f;
        }

        static float ContestPenalty(PlayerController shooter, PlayerController defender, StatType zone)
        {
            if (defender == null) return 0f;
            Vector3 a = shooter.transform.position; a.y = 0f;
            Vector3 b = defender.transform.position; b.y = 0f;
            float dd = Vector3.Distance(a, b);
            if (dd >= ContestRange) return 0f;

            float closeness = 1f - dd / ContestRange;
            bool outside = zone == StatType.ThreePoint || zone == StatType.MidRange;
            float defStat = defender.EffectiveStat(outside ? StatType.PerimeterDefense : StatType.PostDefense);
            return ContestMaxPenalty * closeness * Mathf.Clamp01(defStat / 10f);
        }

        /// <summary>Where the shot lands: dead-centre-ish for a make, clearly off
        /// the rim for a miss (outside the score zone + ball radius, so a rolled
        /// miss clanks iron or airballs instead of dropping anyway).</summary>
        public static Vector3 AimOffset(bool make)
        {
            if (make)
            {
                Vector2 j = Random.insideUnitCircle * 0.04f;
                return new Vector3(j.x, 0f, j.y);
            }
            // Off the rim — mostly clanks iron / rolls out (feeds rebounds),
            // sometimes a clean miss. Tuned to the rim's iron-ring radius.
            float ang = Random.value * Mathf.PI * 2f;
            float r = Random.Range(0.33f, 0.62f);
            return new Vector3(Mathf.Cos(ang) * r, 0f, Mathf.Sin(ang) * r);
        }
    }
}
