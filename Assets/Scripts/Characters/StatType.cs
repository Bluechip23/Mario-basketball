namespace MarioBasketball.Characters
{
    /// <summary>
    /// The fourteen visible attributes every character has, each rated 1-10.
    /// The enum lets systems and UI refer to a stat generically (e.g. "apply
    /// the stamina multiplier to <see cref="Speed"/>") and keeps the ordering
    /// consistent everywhere it is displayed.
    ///
    /// See <c>docs/DESIGN.md</c> for the full description of each stat and the
    /// interactions between them (e.g. Power vs Post Defense).
    /// </summary>
    public enum StatType
    {
        Speed,
        BallHandling,
        ThreePoint,
        MidRange,
        InsideScoring,
        PostOffense,
        Dunk,
        Power,
        Rebounds,
        Blocks,
        Steals,
        PostDefense,
        PerimeterDefense,
        Stamina
    }
}
