using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MarioBasketball.Gameplay;

namespace MarioBasketball.Core
{
    /// <summary>
    /// Owns match-wide state: the score for each side, the current phase of
    /// play and the references that gameplay objects need to find each other
    /// (the ball, the hoops). Lives as a lightweight singleton so the
    /// runtime <c>GameBootstrap</c> can wire everything up without a scene
    /// full of hand-placed references.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Match settings")]
        [Tooltip("First team to reach this score wins. 0 disables the win check.")]
        public int scoreToWin = 21;

        [Header("Scene references (auto-wired by GameBootstrap)")]
        public BallController ball;
        public readonly List<Hoop> hoops = new List<Hoop>();

        public int HomeScore { get; private set; }
        public int AwayScore { get; private set; }
        public GameState State { get; private set; } = GameState.TipOff;

        /// <summary>Raised whenever either score changes. UI listens to this.</summary>
        public event Action ScoreChanged;
        /// <summary>Raised when the match phase changes.</summary>
        public event Action<GameState> StateChanged;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void Start()
        {
            SetState(GameState.Playing);
        }

        /// <summary>Find the hoop the given team is attacking (the far one).</summary>
        public Hoop GetAttackingHoop(TeamSide team)
        {
            foreach (var hoop in hoops)
            {
                if (hoop.attackedBy == team)
                    return hoop;
            }
            // Fallback: nearest hoop to the ball if sides were not configured.
            return hoops.Count > 0 ? hoops[0] : null;
        }

        /// <summary>Called by a <see cref="ScoreZone"/> when a shot drops through.</summary>
        public void RegisterBasket(TeamSide scoringTeam, int points)
        {
            if (State != GameState.Playing)
                return;

            if (scoringTeam == TeamSide.Home)
                HomeScore += points;
            else
                AwayScore += points;

            ScoreChanged?.Invoke();

            if (scoreToWin > 0 && (HomeScore >= scoreToWin || AwayScore >= scoreToWin))
            {
                SetState(GameState.GameOver);
                return;
            }

            StartCoroutine(ResetAfterScore());
        }

        IEnumerator ResetAfterScore()
        {
            SetState(GameState.ScoredReset);
            yield return new WaitForSeconds(1.0f);
            if (ball != null)
                ball.ResetToCentre();
            SetState(GameState.Playing);
        }

        void SetState(GameState next)
        {
            State = next;
            StateChanged?.Invoke(next);
        }
    }
}
