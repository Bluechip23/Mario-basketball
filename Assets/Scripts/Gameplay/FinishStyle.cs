namespace MarioBasketball.Gameplay
{
    /// <summary>
    /// How an inside finish plays out — drives the body animation (and a rim hang
    /// on the big slam). The make math is the same; this is the look of it.
    /// </summary>
    public enum FinishStyle
    {
        /// <summary>One-legged layup off a driving stride — one arm lays it in.</summary>
        Layup,
        /// <summary>Explode off one foot and flush it one-handed (no hang).</summary>
        OneFootOneHandDunk,
        /// <summary>Off one foot, two-handed flush (no hang).</summary>
        OneFootTwoHandDunk,
        /// <summary>Two-foot gather into a two-hand slam: grab the rim and hang.</summary>
        TwoHandSlam
    }
}
