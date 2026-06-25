namespace MarioBasketball.Gameplay
{
    /// <summary>
    /// The flashy on-ball dribble moves. Each one drives a distinct ball path
    /// (<see cref="BallController"/>) and a distinct body pose
    /// (<c>ProceduralAnimator</c>); the gameplay effect (separation, ankle-breaks,
    /// strips) is the same regardless of which move it is.
    /// </summary>
    public enum DribbleMoveType
    {
        /// <summary>A long crossover: arms spread wide and the ball is ripped hard
        /// and low to the other hand.</summary>
        Crossover,
        /// <summary>Wrap the ball around the back to the other hand.</summary>
        BehindBack,
        /// <summary>Drop the ball low between the knees to the other hand.</summary>
        BetweenLegs,
        /// <summary>Spin move: whip the body around protecting the ball.</summary>
        Spin,
        /// <summary>Streetball: toss the ball up off the defender's head and catch it.</summary>
        OffTheHead,
        /// <summary>Hesitation crossover — a hitch, then a hard cross (the ankle-breaker).</summary>
        Hesitation,
        /// <summary>Step-back: push the ball back to create shooting space.</summary>
        StepBack
    }

    /// <summary>Per-move timing / hand-switch metadata, shared by the controller,
    /// the ball and the animator so they stay in lockstep.</summary>
    public static class DribbleMoves
    {
        /// <summary>How long the move animation / ball path runs (seconds).</summary>
        // Durations are deliberately unhurried: a breakdown move isn't about speed,
        // it's a beat you watch — the handle, then the defender bites (or breaks).
        public static float Duration(DribbleMoveType t)
        {
            switch (t)
            {
                case DribbleMoveType.BehindBack:  return 0.68f;
                case DribbleMoveType.BetweenLegs: return 0.62f;
                case DribbleMoveType.Spin:        return 0.8f;
                case DribbleMoveType.OffTheHead:  return 0.88f;
                case DribbleMoveType.Hesitation:  return 0.82f;
                case DribbleMoveType.StepBack:    return 0.5f;
                default:                          return 0.55f; // Crossover
            }
        }

        /// <summary>Whether the move ends with the ball on the other hand.</summary>
        public static bool SwitchesHands(DribbleMoveType t) =>
            t == DribbleMoveType.Crossover || t == DribbleMoveType.BehindBack
            || t == DribbleMoveType.BetweenLegs || t == DribbleMoveType.Hesitation;
    }
}
