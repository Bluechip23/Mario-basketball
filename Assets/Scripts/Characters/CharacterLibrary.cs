using System.Collections.Generic;

namespace MarioBasketball.Characters
{
    /// <summary>
    /// Source of truth for characters that exist in code (no asset wiring
    /// required), so the prototype always has a roster to spawn.
    ///
    /// These are the project owner's stat sheets — do not invent or tweak
    /// characters here without sign-off.
    /// </summary>
    public static class CharacterLibrary
    {
        /// <summary>Bowser — dominant interior force who gasses out and is
        /// helpless on the perimeter.</summary>
        public static CharacterStats Bowser() => new CharacterStats
        {
            characterName = "Bowser",
            speed = 2, ballHandling = 2, threePoint = 1, midRange = 2,
            insideScoring = 10, postOffense = 9, dunk = 3, power = 10,
            rebounds = 5, blocks = 5, steals = 8, postDefense = 7,
            perimeterDefense = 1, stamina = 4, hiddenTrait = HiddenTrait.None,
            heightMeters = 2.6f
        };

        /// <summary>Donkey Kong — athletic rim-wrecker and rugged defender who
        /// can't shoot a lick.</summary>
        public static CharacterStats DonkeyKong() => new CharacterStats
        {
            characterName = "Donkey Kong",
            speed = 7, ballHandling = 2, threePoint = 1, midRange = 1,
            insideScoring = 5, postOffense = 5, dunk = 10, power = 9,
            rebounds = 8, blocks = 8, steals = 8, postDefense = 8,
            perimeterDefense = 7, stamina = 7, hiddenTrait = HiddenTrait.None,
            heightMeters = 2.45f
        };

        /// <summary>Mario — the do-everything all-rounder with no real weakness
        /// besides post defense.</summary>
        public static CharacterStats Mario() => new CharacterStats
        {
            characterName = "Mario",
            speed = 7, ballHandling = 7, threePoint = 7, midRange = 8,
            insideScoring = 7, postOffense = 7, dunk = 7, power = 6,
            rebounds = 6, blocks = 6, steals = 6, postDefense = 4,
            perimeterDefense = 6, stamina = 7, hiddenTrait = HiddenTrait.None,
            heightMeters = 1.8f
        };

        /// <summary>Luigi — well-rounded two-way wing who finishes and defends.</summary>
        public static CharacterStats Luigi() => new CharacterStats
        {
            characterName = "Luigi",
            speed = 7, ballHandling = 5, threePoint = 6, midRange = 6,
            insideScoring = 8, postOffense = 6, dunk = 8, power = 6,
            rebounds = 7, blocks = 7, steals = 7, postDefense = 7,
            perimeterDefense = 8, stamina = 8, hiddenTrait = HiddenTrait.None,
            heightMeters = 1.9f
        };

        /// <summary>Peach — sharpshooter with high stamina but no strength or
        /// presence on the glass.</summary>
        public static CharacterStats Peach() => new CharacterStats
        {
            characterName = "Peach",
            speed = 6, ballHandling = 6, threePoint = 8, midRange = 6,
            insideScoring = 5, postOffense = 5, dunk = 5, power = 3,
            rebounds = 3, blocks = 3, steals = 6, postDefense = 3,
            perimeterDefense = 6, stamina = 8, hiddenTrait = HiddenTrait.DeepThreeSpecialist,
            heightMeters = 1.9f
        };

        /// <summary>Toad — elite handle and motor, crafty finisher, but tiny and
        /// no help in the paint.</summary>
        public static CharacterStats Toad() => new CharacterStats
        {
            characterName = "Toad",
            speed = 8, ballHandling = 10, threePoint = 5, midRange = 5,
            insideScoring = 8, postOffense = 2, dunk = 1, power = 3,
            rebounds = 3, blocks = 1, steals = 7, postDefense = 1,
            perimeterDefense = 6, stamina = 9, hiddenTrait = HiddenTrait.None,
            heightMeters = 1.25f
        };

        /// <summary>Waluigi — disruptive post defender and back-to-the-basket
        /// scorer who can't guard the perimeter at all.</summary>
        public static CharacterStats Waluigi() => new CharacterStats
        {
            characterName = "Waluigi",
            speed = 6, ballHandling = 3, threePoint = 3, midRange = 3,
            insideScoring = 7, postOffense = 9, dunk = 6, power = 6,
            rebounds = 7, blocks = 8, steals = 8, postDefense = 8,
            perimeterDefense = 1, stamina = 6, hiddenTrait = HiddenTrait.None,
            heightMeters = 2.35f
        };

        /// <summary>Diddy Kong — blazing-fast perimeter pest; great speed and
        /// on-ball defense, poor scoring touch.</summary>
        public static CharacterStats DiddyKong() => new CharacterStats
        {
            characterName = "Diddy Kong",
            speed = 10, ballHandling = 7, threePoint = 2, midRange = 3,
            insideScoring = 4, postOffense = 1, dunk = 6, power = 6,
            rebounds = 6, blocks = 5, steals = 8, postDefense = 3,
            perimeterDefense = 9, stamina = 8, hiddenTrait = HiddenTrait.None,
            heightMeters = 1.5f
        };

        /// <summary>Yoshi — relentless athlete and lockdown defender with a
        /// bottomless tank, but no offensive skill outside of dunking.</summary>
        public static CharacterStats Yoshi() => new CharacterStats
        {
            characterName = "Yoshi",
            speed = 10, ballHandling = 2, threePoint = 2, midRange = 1,
            insideScoring = 2, postOffense = 2, dunk = 8, power = 7,
            rebounds = 7, blocks = 7, steals = 8, postDefense = 7,
            perimeterDefense = 9, stamina = 10, hiddenTrait = HiddenTrait.None,
            heightMeters = 1.95f
        };

        /// <summary>Birdo — fast, high-stamina scoring wing who shoots it well
        /// from everywhere.</summary>
        public static CharacterStats Birdo() => new CharacterStats
        {
            characterName = "Birdo",
            speed = 9, ballHandling = 5, threePoint = 8, midRange = 8,
            insideScoring = 6, postOffense = 6, dunk = 7, power = 6,
            rebounds = 5, blocks = 5, steals = 7, postDefense = 7,
            perimeterDefense = 7, stamina = 9, hiddenTrait = HiddenTrait.None,
            heightMeters = 1.9f
        };

        /// <summary>Boo — pure sniper and pickpocket, but slow and utterly
        /// powerless everywhere else.</summary>
        public static CharacterStats Boo() => new CharacterStats
        {
            characterName = "Boo",
            speed = 3, ballHandling = 1, threePoint = 10, midRange = 6,
            insideScoring = 2, postOffense = 1, dunk = 1, power = 1,
            rebounds = 4, blocks = 2, steals = 9, postDefense = 4,
            perimeterDefense = 4, stamina = 6, hiddenTrait = HiddenTrait.None,
            heightMeters = 1.4f
        };

        /// <summary>Baby Mario — slick little handle-and-post scorer who can't
        /// shoot from outside or hold up on defense.</summary>
        public static CharacterStats BabyMario() => new CharacterStats
        {
            characterName = "Baby Mario",
            speed = 7, ballHandling = 8, threePoint = 3, midRange = 6,
            insideScoring = 8, postOffense = 8, dunk = 2, power = 5,
            rebounds = 3, blocks = 3, steals = 6, postDefense = 2,
            perimeterDefense = 6, stamina = 8, hiddenTrait = HiddenTrait.None,
            heightMeters = 1.15f
        };

        /// <summary>Wario — strong, skilled mid-range/post bully with a great
        /// handle; just slow and a step behind on D.</summary>
        public static CharacterStats Wario() => new CharacterStats
        {
            characterName = "Wario",
            speed = 4, ballHandling = 8, threePoint = 7, midRange = 10,
            insideScoring = 6, postOffense = 7, dunk = 5, power = 8,
            rebounds = 7, blocks = 5, steals = 8, postDefense = 6,
            perimeterDefense = 5, stamina = 6, hiddenTrait = HiddenTrait.None,
            heightMeters = 2.0f
        };

        /// <summary>Piranha Plant — quirky stretch big: knock-down three and a
        /// monster on the glass, but no handle, drive, or finishing.</summary>
        public static CharacterStats PiranhaPlant() => new CharacterStats
        {
            characterName = "Piranha Plant",
            speed = 5, ballHandling = 3, threePoint = 8, midRange = 2,
            insideScoring = 3, postOffense = 2, dunk = 1, power = 6,
            rebounds = 8, blocks = 5, steals = 4, postDefense = 6,
            perimeterDefense = 3, stamina = 6, hiddenTrait = HiddenTrait.None,
            heightMeters = 2.1f
        };

        /// <summary>Daisy — quick two-way guard: smooth mid-range and a real
        /// perimeter pest, but slight and a non-factor inside.</summary>
        public static CharacterStats Daisy() => new CharacterStats
        {
            characterName = "Daisy",
            speed = 7, ballHandling = 7, threePoint = 5, midRange = 8,
            insideScoring = 6, postOffense = 3, dunk = 3, power = 3,
            rebounds = 3, blocks = 3, steals = 6, postDefense = 3,
            perimeterDefense = 8, stamina = 8, hiddenTrait = HiddenTrait.None,
            heightMeters = 1.85f
        };

        /// <summary>Every character currently defined in code.</summary>
        public static IReadOnlyList<CharacterStats> All() => new List<CharacterStats>
        {
            Bowser(), DonkeyKong(), Mario(), Luigi(), Peach(), Toad(), Waluigi(), DiddyKong(),
            Yoshi(), Birdo(), Boo(), BabyMario(), Wario(), PiranhaPlant(), Daisy()
        };
    }
}
