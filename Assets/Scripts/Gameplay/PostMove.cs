namespace MarioBasketball.Gameplay
{
    /// <summary>
    /// The back-to-the-basket moves a posting player can perform, each on its
    /// own button (the advanced moves layer onto the same buttons with the
    /// turbo modifier, or chain off a fake). Resolved by
    /// <see cref="PostUpController"/> using Post Offense vs the defender's
    /// Post Defense, plus how deep the player has backed in.
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
        Fake,
        /// <summary>Turbo + Hook: the sweeping Kareem hook — released so high it
        /// can never be blocked, but a tougher shot to convert.</summary>
        SkyHook,
        /// <summary>Turbo + Drop Step: a Power-driven bulldoze into the lane that
        /// shoves (or flattens) the defender on the way to the rim.</summary>
        PowerDropStep,
        /// <summary>Turbo + Spin: face up and fade away over the defender — a
        /// Mid Range shot that the fade makes very hard to block.</summary>
        TurnaroundJumper,
        /// <summary>Drop Step pressed while a fake is live: step through under the
        /// airborne defender for a nearly uncontested finish.</summary>
        UpAndUnder
    }
}
