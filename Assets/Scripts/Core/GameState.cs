namespace MarioBasketball.Core
{
    /// <summary>
    /// High-level phases of a match. The initial core loop only exercises
    /// <see cref="Playing"/>, but the state machine is here so possession
    /// resets, tip-off and end-of-game logic have a home as the game grows.
    /// </summary>
    public enum GameState
    {
        TipOff,
        Playing,
        ScoredReset,
        GameOver
    }
}
