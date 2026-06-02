namespace MarioBasketball.Core
{
    /// <summary>
    /// The 20-second shot clock. The offense must make the ball touch the rim
    /// before it expires, or it's a turnover. It resets on a change of
    /// possession and whenever the ball hits the rim (a fresh attempt window).
    /// Paused whenever the game clock is paused.
    /// </summary>
    public class ShotClock
    {
        public readonly float length;
        public float Remaining { get; private set; }
        public bool Running { get; set; }

        public ShotClock(float length = 20f)
        {
            this.length = length;
            Remaining = length;
        }

        public void Reset()
        {
            Remaining = length;
        }

        /// <summary>Advance the clock. Returns true on the tick it expires.</summary>
        public bool Tick(float dt)
        {
            if (!Running) return false;
            Remaining -= dt;
            if (Remaining <= 0f)
            {
                Remaining = 0f;
                return true;
            }
            return false;
        }

        public string Display => Remaining.ToString("0");
    }
}
