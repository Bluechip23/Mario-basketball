using System.Collections.Generic;
using MarioBasketball.Gameplay;

namespace MarioBasketball.Core
{
    /// <summary>One player's accumulated stat line for the current match.</summary>
    public class PlayerStatLine
    {
        public int Points;
        public int Rebounds;
        public int Blocks;
        public int Steals;
        public int TwoMade, TwoAttempts;
        public int ThreeMade, ThreeAttempts;

        public void AddTo(PlayerStatLine other)
        {
            Points += other.Points;
            Rebounds += other.Rebounds;
            Blocks += other.Blocks;
            Steals += other.Steals;
            TwoMade += other.TwoMade;
            TwoAttempts += other.TwoAttempts;
            ThreeMade += other.ThreeMade;
            ThreeAttempts += other.ThreeAttempts;
        }
    }

    /// <summary>
    /// Live per-player box score for a match (points, rebounds, blocks, steals,
    /// and 2PT / 3PT makes vs attempts), owned by <see cref="GameManager"/>.
    /// Lines are keyed by the <see cref="PlayerController"/> so they follow a
    /// player across substitutions. Team totals are summed on demand from a
    /// roster. Built so testers can see what's actually happening on the floor.
    /// </summary>
    public class BoxScore
    {
        readonly Dictionary<PlayerController, PlayerStatLine> _lines =
            new Dictionary<PlayerController, PlayerStatLine>();

        /// <summary>This player's line, created on first access.</summary>
        public PlayerStatLine For(PlayerController p)
        {
            if (p == null) return null;
            if (!_lines.TryGetValue(p, out var line))
            {
                line = new PlayerStatLine();
                _lines[p] = line;
            }
            return line;
        }

        public void AddPoints(PlayerController p, int points) { var l = For(p); if (l != null) l.Points += points; }
        public void AddRebound(PlayerController p) { var l = For(p); if (l != null) l.Rebounds++; }
        public void AddBlock(PlayerController p) { var l = For(p); if (l != null) l.Blocks++; }
        public void AddSteal(PlayerController p) { var l = For(p); if (l != null) l.Steals++; }

        /// <summary>A field-goal attempt (2 or 3 by its point value). Counted even
        /// when the shot is blocked — a blocked shot is still a missed attempt.</summary>
        public void AddFieldGoalAttempt(PlayerController p, int points)
        {
            var l = For(p);
            if (l == null) return;
            if (points >= 3) l.ThreeAttempts++; else l.TwoAttempts++;
        }

        /// <summary>A made field goal (recorded when the ball drops through).</summary>
        public void AddFieldGoalMade(PlayerController p, int points)
        {
            var l = For(p);
            if (l == null) return;
            if (points >= 3) l.ThreeMade++; else l.TwoMade++;
        }

        /// <summary>Sum the lines of every player in <paramref name="roster"/>.</summary>
        public PlayerStatLine TeamTotal(IEnumerable<PlayerController> roster)
        {
            var total = new PlayerStatLine();
            if (roster == null) return total;
            foreach (var p in roster)
            {
                if (p == null) continue;
                if (_lines.TryGetValue(p, out var line)) total.AddTo(line);
            }
            return total;
        }
    }
}
