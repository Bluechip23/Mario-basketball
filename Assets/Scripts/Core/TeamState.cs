using System.Collections.Generic;
using MarioBasketball.Gameplay;

namespace MarioBasketball.Core
{
    /// <summary>
    /// One team's match state: its five-player roster split into who is on the
    /// court (three at a time) and who is on the bench, plus team fouls and
    /// remaining timeouts. Score is kept by <see cref="GameManager"/>.
    /// </summary>
    public class TeamState
    {
        public const int StartingTimeouts = 2;
        /// <summary>Once a team has committed this many fouls, every further
        /// foul sends the fouled team to the free-throw line.</summary>
        public const int PenaltyFoulLimit = 10;

        public readonly TeamSide side;
        public readonly List<PlayerController> onCourt = new List<PlayerController>();
        public readonly List<PlayerController> bench = new List<PlayerController>();

        public int Fouls { get; private set; }
        public int TimeoutsRemaining { get; private set; } = StartingTimeouts;

        public TeamState(TeamSide side)
        {
            this.side = side;
        }

        public void AddFoul() => Fouls++;

        /// <summary>Team fouls reset at halftime.</summary>
        public void ResetFouls() => Fouls = 0;

        /// <summary>This team has fouled enough that its fouls now grant the
        /// opponent free throws.</summary>
        public bool InPenalty => Fouls >= PenaltyFoulLimit;

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
