using System.Collections.Generic;

namespace MarioBasketball.Characters
{
    /// <summary>
    /// Source of truth for characters that exist in code (no asset wiring
    /// required), so the prototype always has a roster to spawn.
    ///
    /// These are the project owner's stat sheets — do not invent or tweak
    /// characters here without sign-off. (heightMeters is presentation only.)
    /// </summary>
    public static class CharacterLibrary
    {
        public static CharacterStats Bowser() => new CharacterStats
        {
            characterName = "Bowser",
            speed = 2, ballHandling = 2, threePoint = 1, midRange = 2, insideScoring = 10, postOffense = 9, dunk = 3, power = 10, rebounds = 5, blocks = 5, steals = 8, postDefense = 7, perimeterDefense = 2, stamina = 4,
            hiddenTrait = HiddenTrait.None, heightMeters = 2.6f,
            archetype = PlayerArchetype.Big,
            description = "An immovable monster on the low block. Back him in deep and nothing in the kingdom keeps him from the rim — just don't ask him to chase guards."
        };

        public static CharacterStats DonkeyKong() => new CharacterStats
        {
            characterName = "Donkey Kong",
            speed = 7, ballHandling = 2, threePoint = 1, midRange = 1, insideScoring = 4, postOffense = 4, dunk = 10, power = 9, rebounds = 9, blocks = 8, steals = 5, postDefense = 8, perimeterDefense = 5, stamina = 7,
            hiddenTrait = HiddenTrait.None, heightMeters = 2.45f,
            archetype = PlayerArchetype.Big,
            description = "The kingdom's most violent finisher. Throw it anywhere near the rim and he'll hammer it home — every dunk is a poster."
        };

        public static CharacterStats Mario() => new CharacterStats
        {
            characterName = "Mario",
            speed = 7, ballHandling = 8, threePoint = 7, midRange = 8, insideScoring = 7, postOffense = 7, dunk = 7, power = 6, rebounds = 7, blocks = 6, steals = 6, postDefense = 4, perimeterDefense = 5, stamina = 8,
            hiddenTrait = HiddenTrait.None, heightMeters = 1.8f,
            archetype = PlayerArchetype.Wing,
            description = "The all-around captain. Scores from anywhere, handles the ball, and never takes a possession off at either end."
        };

        public static CharacterStats Luigi() => new CharacterStats
        {
            characterName = "Luigi",
            speed = 7, ballHandling = 5, threePoint = 3, midRange = 6, insideScoring = 6, postOffense = 4, dunk = 7, power = 6, rebounds = 7, blocks = 6, steals = 6, postDefense = 7, perimeterDefense = 7, stamina = 8,
            hiddenTrait = HiddenTrait.None, heightMeters = 1.9f,
            archetype = PlayerArchetype.Wing,
            description = "The dependable two-way wing living in his brother's shadow — solid everywhere, with a sneaky knack for finishing inside."
        };

        /// <summary>Peach — Deep-Three Specialist (long-range bonus).</summary>
        public static CharacterStats Peach() => new CharacterStats
        {
            characterName = "Peach",
            speed = 6, ballHandling = 6, threePoint = 8, midRange = 6, insideScoring = 4, postOffense = 5, dunk = 5, power = 3, rebounds = 3, blocks = 5, steals = 6, postDefense = 3, perimeterDefense = 6, stamina = 8,
            hiddenTrait = HiddenTrait.DeepThreeSpecialist, heightMeters = 1.9f,
            archetype = PlayerArchetype.Wing,
            description = "Royal range. A graceful shooter who buries threes from way, way downtown — sag off her at your peril."
        };

        public static CharacterStats Toad() => new CharacterStats
        {
            characterName = "Toad",
            speed = 8, ballHandling = 10, threePoint = 5, midRange = 5, insideScoring = 8, postOffense = 2, dunk = 1, power = 3, rebounds = 3, blocks = 1, steals = 7, postDefense = 1, perimeterDefense = 6, stamina = 9,
            hiddenTrait = HiddenTrait.None, heightMeters = 1.25f,
            archetype = PlayerArchetype.Guard,
            description = "Tiny, tireless and nearly impossible to strip. The best pure handle in the kingdom runs the whole show."
        };

        /// <summary>Waluigi — Offensive Rebounder (Rebounds = 9 on his missed-shot boards).</summary>
        public static CharacterStats Waluigi() => new CharacterStats
        {
            characterName = "Waluigi",
            speed = 6, ballHandling = 3, threePoint = 3, midRange = 3, insideScoring = 8, postOffense = 9, dunk = 6, power = 6, rebounds = 7, blocks = 8, steals = 4, postDefense = 8, perimeterDefense = 1, stamina = 6,
            hiddenTrait = HiddenTrait.OffensiveRebounder, heightMeters = 2.35f,
            archetype = PlayerArchetype.Big,
            description = "A lanky low-post menace who swats shots and feasts on the offensive glass. Wah. Keep a body on him or pay for it."
        };

        public static CharacterStats DiddyKong() => new CharacterStats
        {
            characterName = "Diddy Kong",
            speed = 10, ballHandling = 7, threePoint = 2, midRange = 3, insideScoring = 6, postOffense = 6, dunk = 4, power = 6, rebounds = 6, blocks = 5, steals = 8, postDefense = 3, perimeterDefense = 7, stamina = 8,
            hiddenTrait = HiddenTrait.None, heightMeters = 1.5f,
            archetype = PlayerArchetype.Guard,
            description = "Pure jet fuel. The fastest player on any floor, hounding the ball from baseline to baseline."
        };

        public static CharacterStats Yoshi() => new CharacterStats
        {
            characterName = "Yoshi",
            speed = 10, ballHandling = 1, threePoint = 1, midRange = 1, insideScoring = 2, postOffense = 1, dunk = 6, power = 7, rebounds = 7, blocks = 6, steals = 7, postDefense = 7, perimeterDefense = 9, stamina = 10,
            hiddenTrait = HiddenTrait.None, heightMeters = 1.95f,
            archetype = PlayerArchetype.Wing,
            description = "An elite athlete who outruns everyone and defends everything. Just don't ask him to shoot it."
        };

        public static CharacterStats Birdo() => new CharacterStats
        {
            characterName = "Birdo",
            speed = 9, ballHandling = 6, threePoint = 8, midRange = 8, insideScoring = 7, postOffense = 4, dunk = 7, power = 6, rebounds = 5, blocks = 5, steals = 4, postDefense = 3, perimeterDefense = 3, stamina = 9,
            hiddenTrait = HiddenTrait.None, heightMeters = 1.9f,
            archetype = PlayerArchetype.Wing,
            description = "A sprinting flame-thrower — pull-up threes and mid-range daggers in transition, all game long."
        };

        public static CharacterStats Boo() => new CharacterStats
        {
            characterName = "Boo",
            speed = 3, ballHandling = 1, threePoint = 10, midRange = 6, insideScoring = 2, postOffense = 1, dunk = 1, power = 1, rebounds = 4, blocks = 2, steals = 9, postDefense = 4, perimeterDefense = 4, stamina = 6,
            hiddenTrait = HiddenTrait.None, heightMeters = 1.4f,
            archetype = PlayerArchetype.Guard,
            description = "Barely moves, barely defends... but leave it open in the corner and the spooky catch-and-shoot three never misses."
        };

        public static CharacterStats BabyMario() => new CharacterStats
        {
            characterName = "Baby Mario",
            speed = 7, ballHandling = 8, threePoint = 3, midRange = 6, insideScoring = 8, postOffense = 8, dunk = 2, power = 5, rebounds = 3, blocks = 3, steals = 4, postDefense = 2, perimeterDefense = 6, stamina = 8,
            hiddenTrait = HiddenTrait.Acrobat, heightMeters = 1.15f,
            archetype = PlayerArchetype.Guard,
            description = "All of the captain's craft in a knee-high package, with a shockingly grown-up post game for a baby."
        };

        /// <summary>Wario — Smooth Passer (passes as Ball Handling 8, or 10 out of the post).</summary>
        public static CharacterStats Wario() => new CharacterStats
        {
            characterName = "Wario",
            speed = 4, ballHandling = 6, threePoint = 6, midRange = 10, insideScoring = 6, postOffense = 7, dunk = 5, power = 8, rebounds = 7, blocks = 5, steals = 5, postDefense = 6, perimeterDefense = 5, stamina = 6,
            hiddenTrait = HiddenTrait.SmoothPasser, heightMeters = 2.0f,
            archetype = PlayerArchetype.Big,
            description = "The bully with a silk mid-range jumper. Doubles down low only feed his smooth (and smug) passing."
        };

        /// <summary>Piranha Plant — Quick-Catch Shooter (catch-and-shoot 3 = 10).</summary>
        public static CharacterStats PiranhaPlant() => new CharacterStats
        {
            characterName = "Piranha Plant",
            speed = 5, ballHandling = 3, threePoint = 8, midRange = 2, insideScoring = 3, postOffense = 2, dunk = 1, power = 6, rebounds = 8, blocks = 5, steals = 3, postDefense = 7, perimeterDefense = 3, stamina = 6,
            hiddenTrait = HiddenTrait.QuickCatchShooter, heightMeters = 2.1f,
            archetype = PlayerArchetype.Big,
            description = "A planted catch-and-shoot tower — swing it fast and it bites from three. Slow getting back the other way."
        };

        public static CharacterStats Daisy() => new CharacterStats
        {
            characterName = "Daisy",
            speed = 7, ballHandling = 7, threePoint = 5, midRange = 9, insideScoring = 6, postOffense = 3, dunk = 5, power = 3, rebounds = 3, blocks = 3, steals = 6, postDefense = 3, perimeterDefense = 8, stamina = 8,
            hiddenTrait = HiddenTrait.KillerInstinct, heightMeters = 1.85f,
            archetype = PlayerArchetype.Wing,
            description = "Hi, I'm Daisy! A pesky on-ball defender with the purest mid-range stroke in the kingdom."
        };

        public static CharacterStats MontyMole() => new CharacterStats
        {
            characterName = "Monty Mole",
            speed = 7, ballHandling = 4, threePoint = 5, midRange = 5, insideScoring = 5, postOffense = 3, dunk = 3, power = 7, rebounds = 7, blocks = 7, steals = 4, postDefense = 3, perimeterDefense = 10, stamina = 8,
            hiddenTrait = HiddenTrait.None, heightMeters = 1.45f,
            archetype = PlayerArchetype.Guard,
            description = "A burrowing perimeter pest. There is no driving around him, under him or through him."
        };

        public static CharacterStats Koopa() => new CharacterStats
        {
            characterName = "Koopa",
            speed = 6, ballHandling = 10, threePoint = 5, midRange = 5, insideScoring = 5, postOffense = 3, dunk = 3, power = 8, rebounds = 6, blocks = 5, steals = 6, postDefense = 3, perimeterDefense = 7, stamina = 9,
            hiddenTrait = HiddenTrait.Playmaker, heightMeters = 1.7f,
            archetype = PlayerArchetype.Guard,
            description = "The shell-backed floor general. Elite handle, elite vision — teammates eat well off his passes."
        };

        public static CharacterStats Kritter() => new CharacterStats
        {
            characterName = "Kritter",
            speed = 6, ballHandling = 1, threePoint = 1, midRange = 2, insideScoring = 5, postOffense = 3, dunk = 4, power = 8, rebounds = 8, blocks = 10, steals = 3, postDefense = 10, perimeterDefense = 4, stamina = 8,
            hiddenTrait = HiddenTrait.None, heightMeters = 2.15f,
            archetype = PlayerArchetype.Big,
            description = "A scaly wall under the rim. Nothing gets through — just blocks, boards and bad intentions."
        };

        public static CharacterStats Shyguy() => new CharacterStats
        {
            characterName = "Shyguy",
            speed = 6, ballHandling = 6, threePoint = 9, midRange = 9, insideScoring = 9, postOffense = 7, dunk = 4, power = 5, rebounds = 6, blocks = 6, steals = 5, postDefense = 3, perimeterDefense = 5, stamina = 2,
            hiddenTrait = HiddenTrait.None, heightMeters = 1.55f,
            archetype = PlayerArchetype.Guard,
            description = "A masked bucket-getter who scores at all three levels. Ride him while you can — the tank empties fast."
        };

        public static CharacterStats Delfan() => new CharacterStats
        {
            characterName = "Delfan",
            speed = 4, ballHandling = 8, threePoint = 9, midRange = 7, insideScoring = 3, postOffense = 4, dunk = 2, power = 7, rebounds = 2, blocks = 2, steals = 4, postDefense = 6, perimeterDefense = 6, stamina = 8,
            hiddenTrait = HiddenTrait.CalledShot, heightMeters = 1.6f,
            archetype = PlayerArchetype.Guard,
            description = "A sturdy, deliberate sharpshooter with real handle. Slow up the floor, but give him a sliver of space and the three is going down."
        };

        public static CharacterStats Laurentius() => new CharacterStats
        {
            characterName = "Laurentius",
            speed = 6, ballHandling = 4, threePoint = 1, midRange = 6, insideScoring = 4, postOffense = 3, dunk = 4, power = 8, rebounds = 10, blocks = 8, steals = 5, postDefense = 7, perimeterDefense = 8, stamina = 8,
            hiddenTrait = HiddenTrait.None, heightMeters = 2.2f,
            archetype = PlayerArchetype.Big,
            description = "The glass belongs to him. A board-vacuuming enforcer who defends the paint and the perimeter alike — just never ask for a three."
        };

        public static CharacterStats CliffyGuy() => new CharacterStats
        {
            characterName = "Cliffy Guy",
            speed = 10, ballHandling = 6, threePoint = 1, midRange = 1, insideScoring = 8, postOffense = 3, dunk = 8, power = 5, rebounds = 2, blocks = 2, steals = 9, postDefense = 3, perimeterDefense = 4, stamina = 8,
            hiddenTrait = HiddenTrait.None, heightMeters = 1.55f,
            archetype = PlayerArchetype.Guard,
            description = "A blur in a mask. Scales any defense for steals and rim attacks — allergic to jump shots and rebounds."
        };

        public static CharacterStats JonahGuy() => new CharacterStats
        {
            characterName = "Jonah Guy",
            speed = 3, ballHandling = 4, threePoint = 7, midRange = 4, insideScoring = 7, postOffense = 6, dunk = 5, power = 7, rebounds = 7, blocks = 6, steals = 2, postDefense = 8, perimeterDefense = 5, stamina = 10,
            hiddenTrait = HiddenTrait.Energizer, heightMeters = 2.05f,
            archetype = PlayerArchetype.Big,
            description = "A whale of a worker. Never tires, bangs inside, guards the post — and surprises from deep when left alone."
        };

        /// <summary>Every character currently defined in code.</summary>
        public static IReadOnlyList<CharacterStats> All() => new List<CharacterStats>
        {
            Bowser(), DonkeyKong(), Mario(), Luigi(), Peach(), Toad(), Waluigi(), DiddyKong(),
            Yoshi(), Birdo(), Boo(), BabyMario(), Wario(), PiranhaPlant(), Daisy(),
            MontyMole(), Koopa(), Kritter(), Shyguy(),
            Delfan(), Laurentius(), CliffyGuy(), JonahGuy()
        };
    }
}
