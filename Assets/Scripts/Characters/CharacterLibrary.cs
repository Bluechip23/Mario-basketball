using System.Collections.Generic;

namespace MarioBasketball.Characters
{
    /// <summary>
    /// Source of truth for characters that exist in code (no asset wiring
    /// required), so the prototype always has something to spawn.
    ///
    /// IMPORTANT: only the demo character, <b>Bowser</b>, is defined here. The
    /// rest of the roster will be designed together — do not add characters
    /// here without sign-off.
    /// </summary>
    public static class CharacterLibrary
    {
        /// <summary>
        /// Bowser — a dominant interior force who gasses out and is helpless on
        /// the perimeter. The numbers alone should read as "big, slow bruiser".
        /// </summary>
        public static CharacterStats Bowser() => new CharacterStats
        {
            characterName    = "Bowser",
            speed            = 2,
            ballHandling     = 2,
            threePoint       = 1,
            midRange         = 2,
            insideScoring    = 10,
            postOffense      = 9,
            dunk             = 3,
            power            = 10,
            rebounds         = 5,
            blocks           = 5,
            steals           = 8,
            postDefense      = 7,
            perimeterDefense = 1,
            stamina          = 4,
            hiddenTrait      = HiddenTrait.None
        };

        /// <summary>Every character currently defined in code.</summary>
        public static IReadOnlyList<CharacterStats> All() => new List<CharacterStats> { Bowser() };
    }
}
