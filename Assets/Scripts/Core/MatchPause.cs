namespace MarioBasketball.Core
{
    /// <summary>
    /// Global pause flag, set by the pause menu. Gameplay freezes via
    /// <c>Time.timeScale = 0</c>, but input callbacks aren't time-scaled, so
    /// action triggers (shoot, steal, switch, timeout…) check this flag to stay
    /// inert while paused.
    /// </summary>
    public static class MatchPause
    {
        public static bool IsPaused;
    }
}
