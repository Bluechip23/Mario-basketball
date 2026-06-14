using UnityEngine;
using MarioBasketball.Core;
using MarioBasketball.Gameplay;

namespace MarioBasketball.UI
{
    /// <summary>
    /// Shared IMGUI renderer for the match box score (points, rebounds, blocks,
    /// steals, and 2PT / 3PT makes-vs-attempts) per player and as a team total.
    /// Drawn inside a <c>GUILayout</c> area by both the live on-court overlay
    /// (<see cref="BoxScoreHUD"/>) and the pause-menu Box Score page.
    /// </summary>
    public static class BoxScoreView
    {
        /// <summary>The column header line (matches the per-row column widths).</summary>
        public static string HeaderLine =>
            Row("", "PTS", "REB", "BLK", "STL", "2PM/A", "3PM/A");

        /// <summary>Lay out both teams' tables. <paramref name="header"/> styles the
        /// team / column headings, <paramref name="row"/> the player rows.</summary>
        public static void Draw(GameManager gm, GUIStyle header, GUIStyle row)
        {
            if (gm == null) { GUILayout.Label("No match in progress.", row); return; }
            GUILayout.Label(HeaderLine, header);
            DrawTeam(gm, "HOME", gm.Home, header, row);
            GUILayout.Space(8);
            DrawTeam(gm, "AWAY", gm.Away, header, row);
        }

        static void DrawTeam(GameManager gm, string label, TeamState team, GUIStyle header, GUIStyle row)
        {
            GUILayout.Label($"== {label} ==", header);
            var total = new PlayerStatLine();
            DrawSide(gm, team.onCourt, total, row, false);
            DrawSide(gm, team.bench, total, row, true);
            GUILayout.Label(FormatLine("TEAM", total), header);
        }

        static void DrawSide(GameManager gm, System.Collections.Generic.List<PlayerController> players,
            PlayerStatLine total, GUIStyle row, bool bench)
        {
            foreach (var p in players)
            {
                if (p == null || p.Character == null) continue;
                var line = gm.Box.For(p);
                line.AddTo(total);
                string name = p.Character.stats.characterName + (bench ? " (b)" : "");
                GUILayout.Label(FormatLine(name, line), row);
            }
        }

        static string FormatLine(string name, PlayerStatLine s) =>
            Row(Trim(name, 13),
                s.Points.ToString(), s.Rebounds.ToString(), s.Blocks.ToString(), s.Steals.ToString(),
                $"{s.TwoMade}/{s.TwoAttempts}", $"{s.ThreeMade}/{s.ThreeAttempts}");

        static string Row(string name, string pts, string reb, string blk, string stl, string two, string three) =>
            string.Format("{0,-15}{1,4}{2,5}{3,5}{4,5}{5,9}{6,9}", name, pts, reb, blk, stl, two, three);

        static string Trim(string s, int max) => s != null && s.Length > max ? s.Substring(0, max) : s;
    }
}
