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
        SmoothPasser,

        /// <summary>
        /// Dimer: a teammate who shoots/dunks directly off this player's pass
        /// (within ~1 s of the catch, before dribbling) gets +2 to the scoring
        /// attribute they use (Koopa).
        /// </summary>
        Playmaker,

        /// <summary>
        /// Acrobat: improvises shots with no cost. Pays <b>no</b> make penalty
        /// for altering a shot in mid-air (a fadeaway lean, or the L1 air-adjust
        /// on a dunk/layup), and suffers ~80% less from shot mistiming (firing
        /// the instant he goes up, or holding the release too long). The fade
        /// still buys him the same separation everyone gets — he just doesn't eat
        /// the difficulty. See <c>ShotMath</c> / <c>PlayerController</c> (Baby Mario).
        /// </summary>
        Acrobat,

        /// <summary>
        /// Killer Instinct: a closer who feeds on tired legs. As the opposing
        /// on-court team's energy drains, <i>every</i> one of her effective stats
        /// climbs (up to a cap at full opponent fatigue) — run them ragged and
        /// she takes over. See <c>PlayerController</c> (Daisy).
        /// </summary>
        KillerInstinct,

        /// <summary>
        /// Energizer: a glue guy whose connections lift his teammates. Whenever
        /// he scores off a teammate's assist, or assists a teammate's score, that
        /// teammate gets a small stamina boost. See <c>GameManager.RegisterBasket</c>
        /// (Jonah Guy).
        /// </summary>
        Energizer,

        /// <summary>
        /// Called Shot: twice a game, double-tap turbo while one of his shots
        /// (taken from within half court) is in the air to guarantee it drops.
        /// See <c>PlayerController</c> / <c>BallController.ForceMake</c> (Delfan).
        /// </summary>
        CalledShot,

        /// <summary>
        /// Hot Hand: a rhythm shooter who rides streaks. A running counter ticks
        /// up on each make and down on each miss; her <b>3-Point and Mid Range</b>
        /// gain +1 for every two net makes (truncated toward zero, so it takes two
        /// misses to drop a tier, and it works the same going negative). Those two
        /// stats can climb to 11. See <c>PlayerCharacter</c> /
        /// <c>GameManager</c> (Birdo).
        /// </summary>
        HotHand,

        /// <summary>
        /// Wide-Open Sniper: a spot-up specialist who only burns you on a lapse.
        /// Left <b>wide open</b> from three (no defender within the contest range)
        /// his 3-Point make% gets a flat bonus; the instant a defender closes
        /// within range the bonus vanishes and the three is contested at normal
        /// odds. See <c>ShotMath</c> (Boo).
        /// </summary>
        WideOpenSniper
    }
}
