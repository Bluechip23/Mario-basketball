using System;
using UnityEngine;

namespace MarioBasketball.Characters
{
    /// <summary>
    /// The complete attribute sheet for one character: a name, the fourteen
    /// 1-10 stats, and an optional <see cref="HiddenTrait"/>. This is a plain
    /// serializable class so it can live inside a <see cref="CharacterDefinition"/>
    /// asset, be built in code (see <c>CharacterLibrary</c>), or be shown in
    /// the inspector.
    ///
    /// These are the <b>base</b> ratings. In-game effectiveness is derived by
    /// <c>PlayerCharacter</c>, which scales them by current stamina/energy and
    /// applies the "on fire" bonus.
    /// </summary>
    [Serializable]
    public class CharacterStats
    {
        public const int Min = 1;
        public const int Max = 10;

        public string characterName = "Unnamed";

        [Range(Min, Max)] public int speed = 5;
        [Range(Min, Max)] public int ballHandling = 5;
        [Range(Min, Max)] public int threePoint = 5;
        [Range(Min, Max)] public int midRange = 5;
        [Range(Min, Max)] public int insideScoring = 5;
        [Range(Min, Max)] public int postOffense = 5;
        [Range(Min, Max)] public int dunk = 5;
        [Range(Min, Max)] public int power = 5;
        [Range(Min, Max)] public int rebounds = 5;
        [Range(Min, Max)] public int blocks = 5;
        [Range(Min, Max)] public int steals = 5;
        [Range(Min, Max)] public int postDefense = 5;
        [Range(Min, Max)] public int perimeterDefense = 5;
        [Range(Min, Max)] public int stamina = 5;

        public HiddenTrait hiddenTrait = HiddenTrait.None;

        /// <summary>Visual/physical height in metres — presentation, not a
        /// gameplay stat. NBA-Street-style exaggeration is encouraged (Bowser
        /// towers, Toad is tiny). Drives the body capsule and controller size.</summary>
        [Range(1f, 2.8f)] public float heightMeters = 1.9f;

        /// <summary>Read a stat by its <see cref="StatType"/>.</summary>
        public int Get(StatType stat)
        {
            switch (stat)
            {
                case StatType.Speed:            return speed;
                case StatType.BallHandling:     return ballHandling;
                case StatType.ThreePoint:       return threePoint;
                case StatType.MidRange:         return midRange;
                case StatType.InsideScoring:    return insideScoring;
                case StatType.PostOffense:      return postOffense;
                case StatType.Dunk:             return dunk;
                case StatType.Power:            return power;
                case StatType.Rebounds:         return rebounds;
                case StatType.Blocks:           return blocks;
                case StatType.Steals:           return steals;
                case StatType.PostDefense:      return postDefense;
                case StatType.PerimeterDefense: return perimeterDefense;
                case StatType.Stamina:          return stamina;
                default:                        return Min;
            }
        }

        /// <summary>Clamp every value into the legal 1-10 range.</summary>
        public void Validate()
        {
            speed            = Mathf.Clamp(speed, Min, Max);
            ballHandling     = Mathf.Clamp(ballHandling, Min, Max);
            threePoint       = Mathf.Clamp(threePoint, Min, Max);
            midRange         = Mathf.Clamp(midRange, Min, Max);
            insideScoring    = Mathf.Clamp(insideScoring, Min, Max);
            postOffense      = Mathf.Clamp(postOffense, Min, Max);
            dunk             = Mathf.Clamp(dunk, Min, Max);
            power            = Mathf.Clamp(power, Min, Max);
            rebounds         = Mathf.Clamp(rebounds, Min, Max);
            blocks           = Mathf.Clamp(blocks, Min, Max);
            steals           = Mathf.Clamp(steals, Min, Max);
            postDefense      = Mathf.Clamp(postDefense, Min, Max);
            perimeterDefense = Mathf.Clamp(perimeterDefense, Min, Max);
            stamina          = Mathf.Clamp(stamina, Min, Max);
            heightMeters     = Mathf.Clamp(heightMeters, 1f, 2.8f);
        }

        /// <summary>A deep copy, so runtime tweaks never mutate shared data.</summary>
        public CharacterStats Clone()
        {
            return (CharacterStats)MemberwiseClone();
        }
    }
}
