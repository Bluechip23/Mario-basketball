using UnityEngine;

namespace MarioBasketball.Core
{
    /// <summary>
    /// The game clock: counts a quarter down to zero, tracks which quarter it
    /// is, and reports when a quarter expires. Time only advances while
    /// <see cref="Running"/> is true (it is paused on every stoppage, including
    /// after a made basket until the ball is inbounded).
    /// </summary>
    public class MatchClock
    {
        public readonly float quarterLength;
        public readonly int totalQuarters;

        public int Quarter { get; private set; } = 1;
        public float TimeRemaining { get; private set; }
        public bool Running { get; set; }

        public MatchClock(float quarterLengthSeconds, int totalQuarters)
        {
            this.quarterLength = quarterLengthSeconds;
            this.totalQuarters = totalQuarters;
            TimeRemaining = quarterLengthSeconds;
        }

        public bool IsFinalQuarter => Quarter >= totalQuarters;

        /// <summary>Advance the clock. Returns true on the tick the quarter hits 0.</summary>
        public bool Tick(float dt)
        {
            if (!Running) return false;
            TimeRemaining -= dt;
            if (TimeRemaining <= 0f)
            {
                TimeRemaining = 0f;
                Running = false;
                return true;
            }
            return false;
        }

        /// <summary>Move to the next quarter and reload the clock.</summary>
        public void AdvanceQuarter()
        {
            Quarter = Mathf.Min(Quarter + 1, totalQuarters);
            TimeRemaining = quarterLength;
        }

        public string Display
        {
            get
            {
                int total = Mathf.CeilToInt(TimeRemaining);
                return $"{total / 60:0}:{total % 60:00}";
            }
        }
    }
}
