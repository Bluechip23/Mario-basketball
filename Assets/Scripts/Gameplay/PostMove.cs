namespace MarioBasketball.Gameplay
{
    /// <summary>
    /// The back-to-the-basket moves a posting player can perform, each on its
    /// own button. Resolved by <see cref="PostUpController"/> using Post Offense
    /// vs the defender's Post Defense, plus how deep the player has backed in.
    /// </summary>
    public enum PostMove
    {
        /// <summary>High, hard-to-block jump hook.</summary>
        Hook,
        /// <summary>Quick step into the lane for a point-blank finish (blockable).</summary>
        DropStep,
        /// <summary>Spin off the defender for a layup; risks a strip if it fails.</summary>
        Spin,
        /// <summary>Shoulder/pump fake; if the defender bites, the next move is freer.</summary>
        Fake
    }
}
