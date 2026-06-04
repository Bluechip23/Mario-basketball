namespace MarioBasketball.Core
{
    /// <summary>
    /// Phases of a match. The clock only runs during <see cref="Playing"/>;
    /// every other phase is a stoppage where the game/shot clocks are paused.
    /// </summary>
    public enum GameState
    {
        /// <summary>Quarter start: ball at centre, about to go live.</summary>
        TipOff,
        /// <summary>Live ball, clocks running.</summary>
        Playing,
        /// <summary>A basket just went in; clock stopped until the inbound.</summary>
        BasketMade,
        /// <summary>The ball has been handed to an inbounder; resumes shortly.</summary>
        Inbounding,
        /// <summary>A team called timeout; brief stoppage, players catch a breath.</summary>
        Timeout,
        /// <summary>A fouled player is shooting free throws.</summary>
        FreeThrow,
        /// <summary>Between quarters.</summary>
        QuarterBreak,
        /// <summary>Final buzzer.</summary>
        GameOver
    }
}
