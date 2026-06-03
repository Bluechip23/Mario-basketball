using System.Collections.Generic;
using MarioBasketball.Gameplay;

namespace MarioBasketball.Core
{
    /// <summary>
    /// One team's match state: its five-player roster split into who is on the
    /// court (three at a time) and who is on the bench, plus remaining timeouts.
    /// Score is kept by <see cref="GameManager"/>. (Team fouls / free-throw
    /// penalty land with the fouling system — see the roadmap.)
    /// </summary>
    public class TeamState
    {
        public const int StartingTimeouts = 3;

        public readonly TeamSide side;
        public readonly List<PlayerController> onCourt = new List<PlayerController>();
        public readonly List<PlayerController> bench = new List<PlayerController>();

        public int TimeoutsRemaining { get; private set; } = StartingTimeouts;

        public TeamState(TeamSide side)
        {
            this.side = side;
        }

        public bool UseTimeout()
        {
            if (TimeoutsRemaining <= 0) return false;
            TimeoutsRemaining--;
            return true;
        }

        /// <summary>
        /// Swap an on-court player for a bench player. Returns the pair that
        /// changed, or (null, null) if the indices were invalid.
        /// </summary>
        public (PlayerController leaving, PlayerController entering) Substitute(int onCourtIndex, int benchIndex)
        {
            if (onCourtIndex < 0 || onCourtIndex >= onCourt.Count) return (null, null);
            if (benchIndex < 0 || benchIndex >= bench.Count) return (null, null);

            var leaving = onCourt[onCourtIndex];
            var entering = bench[benchIndex];
            onCourt[onCourtIndex] = entering;
            bench[benchIndex] = leaving;
            return (leaving, entering);
        }
    }
}
