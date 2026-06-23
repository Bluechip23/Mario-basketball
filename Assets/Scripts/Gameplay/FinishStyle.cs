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

    /// <summary>Which foot (feet) a finish leaves the floor from. A one-foot
    /// layup drives the opposite knee and arm up and finishes with the hand
    /// opposite the takeoff foot (left foot → right hand, and vice-versa); a
    /// two-foot gather goes up with both hands and releases right.</summary>
    public enum TakeoffFoot
    {
        Left,
        Right,
        Both
    }

    /// <summary>How a finisher contorts a shot in the air to beat the defender
    /// (chosen from where the defender is). <see cref="None"/> = a straight
    /// finish, no air-adjust.</summary>
    public enum AdjustMove
    {
        None,
        /// <summary>Switch the ball to the other hand and finish away from a
        /// defender sitting on one side.</summary>
        SwitchHands,
        /// <summary>Drop the ball and scoop it in from a lower release, under a
        /// defender contesting high.</summary>
        LowRelease,
        /// <summary>Windmill the ball way around to clear a rim protector going
        /// straight up in front of you.</summary>
        Windmill
    }
}
