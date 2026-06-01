namespace MarioBasketball.Core
{
    /// <summary>
    /// Which basket a team is attacking. In a full 2v2 match each player
    /// belongs to one of these sides; for the initial single-player core
    /// loop only <see cref="Home"/> is used.
    /// </summary>
    public enum TeamSide
    {
        Home,
        Away
    }
}
