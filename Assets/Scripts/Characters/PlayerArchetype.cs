namespace MarioBasketball.Characters
{
    /// <summary>
    /// The broad position group a character plays — used to organise the
    /// character-select cards (and later, lineup rules). <see cref="Unset"/>
    /// falls back to a height-based guess (see
    /// <see cref="CharacterStats.Archetype"/>) so created players are grouped
    /// sensibly without picking one.
    /// </summary>
    public enum PlayerArchetype
    {
        Unset,
        /// <summary>Small, quick ball-handlers and perimeter pests.</summary>
        Guard,
        /// <summary>Mid-sized all-rounders, scorers and athletes.</summary>
        Wing,
        /// <summary>The towers — post scorers, rebounders and rim protectors.</summary>
        Big
    }
}
