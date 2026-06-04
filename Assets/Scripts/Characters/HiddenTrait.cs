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
        CatchAndShootOnly
    }
}
