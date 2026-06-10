namespace MarioBasketball.Characters
{
    /// <summary>
    /// Hidden traits are special-case rules that aren't visible on a
    /// character's stat sheet — they make two players with identical numbers
    /// feel different. Example from the design: a player with a high 3-Point
    /// stat who is only reliable on <see cref="CatchAndShootOnly"/> looks, not
    /// off the dribble.
    ///
    /// Only the documented example is defined for now. The full set will be
    /// designed alongside the roster — do not invent new traits ad hoc.
    /// </summary>
    public enum HiddenTrait
    {
        None,

        /// <summary>
        /// 3-Point (and Mid Range) only fire at full effectiveness on a
        /// catch-and-shoot; pulling up off the dribble incurs a heavy penalty.
        /// </summary>
        CatchAndShootOnly,

        /// <summary>
        /// Doesn't suffer the usual deep-three penalty — actually <i>gains</i>
        /// make% stepping back behind the arc (peaking a few feet out) before
        /// finally falling off way out. See <c>ShotMath</c> (Peach).
        /// </summary>
        DeepThreeSpecialist,

        /// <summary>
        /// Catch-and-shoot sniper: shooting a three within a short window of
        /// catching the ball treats 3-Point as a 10. See <c>PlayerController</c>
        /// (Piranha Plant).
        /// </summary>
        QuickCatchShooter,

        /// <summary>
        /// Crashes the offensive glass — Rebounds counts as 9 while on offense.
        /// </summary>
        OffensiveRebounder,

        /// <summary>
        /// Gifted passer: passes throw with Ball Handling counted as 8 (10 when
        /// passing out of a post-up), regardless of the real rating (Wario).
        /// </summary>
        SmoothPasser
    }
}
