using System;
using System.Collections.Generic;
using UnityEngine;
using MarioBasketball.Gameplay;

namespace MarioBasketball.Core
{
    /// <summary>
    /// The match orchestrator and single source of truth. It owns the score,
    /// the game and shot clocks, possession, the two teams' rosters, and the
    /// phase state machine that drives tip-offs, made-basket stoppages,
    /// inbounds, timeouts, quarter breaks and the final buzzer.
    ///
    /// Rules encoded here (see <c>docs/DESIGN.md</c>):
    /// full court 3v3, 4×4-minute quarters, 20-second shot clock (turnover if
    /// the rim isn't hit in time), made shots 2/3 and free throws 1, clock
    /// stops on a make until the inbound, three timeouts per team (+30 energy
    /// to the on-court five), and substitutions from a five-player roster.
    /// AI is not built yet, so non-human players stand; the inbound auto-
    /// resumes so the game still flows.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Match rules")]
        public float quarterLengthSeconds = 240f; // 4 minutes
        public int totalQuarters = 4;
        public float shotClockSeconds = 20f;
        public float timeoutEnergyBonus = 30f;

        [Header("Stoppage durations (seconds)")]
        public float tipOffDuration = 1.5f;
        public float inboundDuration = 1.5f;
        public float basketMadePause = 1.0f;
        public float timeoutDuration = 3f;
        public float quarterBreakDuration = 3f;

        [Header("Scene references (auto-wired by GameBootstrap)")]
        public BallController ball;
        public PlayerController humanPlayer;
        public readonly List<Hoop> hoops = new List<Hoop>();

        [Header("Substitution anchors (set by GameBootstrap)")]
        public Vector3 homeBenchAnchor;
        public Vector3 awayBenchAnchor;
        public Vector3 homeSubEntry;
        public Vector3 awaySubEntry;

        public TeamState Home { get; private set; }
        public TeamState Away { get; private set; }

        public int HomeScore { get; private set; }
        public int AwayScore { get; private set; }
        public TeamSide Possession { get; private set; } = TeamSide.Home;
        public GameState State { get; private set; } = GameState.TipOff;

        public MatchClock Clock { get; private set; }
        public ShotClock Shot { get; private set; }

        public event Action ScoreChanged;
        public event Action<GameState> StateChanged;

        float _stateTimer;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            Home = new TeamState(TeamSide.Home);
            Away = new TeamState(TeamSide.Away);
            Clock = new MatchClock(quarterLengthSeconds, totalQuarters);
            Shot = new ShotClock(shotClockSeconds);
        }

        void Start()
        {
            // Q1 tip-off to the home side (contested tip arrives with AI).
            BeginTipOff(TeamSide.Home);
        }

        // ---- Public API used by gameplay objects and debug controls --------

        public Hoop GetAttackingHoop(TeamSide team)
        {
            foreach (var hoop in hoops)
                if (hoop.attackedBy == team) return hoop;
            return hoops.Count > 0 ? hoops[0] : null;
        }

        public TeamState TeamFor(TeamSide side) => side == TeamSide.Home ? Home : Away;
        public static TeamSide Opponent(TeamSide side) => side == TeamSide.Home ? TeamSide.Away : TeamSide.Home;

        /// <summary>A player grabbed a loose ball — update possession/shot clock.</summary>
        public void OnPossessionGained(PlayerController player)
        {
            if (player == null) return;
            if (player.team != Possession)
            {
                Possession = player.team;
                Shot.Reset();
            }
            // Inbounding the ball to the possessing team makes it live.
            if (State == GameState.Inbounding && player.team == Possession)
                ResumePlay();
        }

        /// <summary>A live shot touched the rim — the attempt counts; reset clock.</summary>
        public void OnRimHit()
        {
            if (State == GameState.Playing)
                Shot.Reset();
        }

        public void RegisterBasket(TeamSide scoringTeam, int points)
        {
            if (State != GameState.Playing) return;

            AddPoints(scoringTeam, points);

            // Clock stops; the other team will inbound under the basket.
            State = GameState.BasketMade;
            StateChanged?.Invoke(State);
            SetClocksRunning(false);
            _stateTimer = basketMadePause;
        }

        public void RegisterFreeThrow(TeamSide scoringTeam)
        {
            AddPoints(scoringTeam, 1);
        }

        public bool CallTimeout(TeamSide side)
        {
            if (State == GameState.GameOver) return false;
            var team = TeamFor(side);
            if (!team.UseTimeout()) return false;

            foreach (var p in team.onCourt)
                if (p != null && p.Character != null)
                    p.Character.AddEnergy(timeoutEnergyBonus);

            SetClocksRunning(false);
            State = GameState.Timeout;
            StateChanged?.Invoke(State);
            _stateTimer = timeoutDuration;
            return true;
        }

        public void Substitute(TeamSide side, int onCourtIndex, int benchIndex)
        {
            var team = TeamFor(side);
            var (leaving, entering) = team.Substitute(onCourtIndex, benchIndex);
            if (leaving == null || entering == null) return;

            if (ball != null && ball.Holder == leaving)
                ball.ResetToCentre();

            BenchPlayer(leaving, side);
            ActivatePlayer(entering, side == TeamSide.Home ? homeSubEntry : awaySubEntry);
        }

        // ---- State machine -------------------------------------------------

        void Update()
        {
            float dt = Time.deltaTime;
            switch (State)
            {
                case GameState.Playing:
                    if (Clock.Tick(dt)) { OnQuarterExpired(); return; }
                    if (Shot.Tick(dt)) { Turnover(Opponent(Possession)); }
                    break;

                case GameState.TipOff:
                case GameState.Inbounding:
                    if (CountdownDone(dt)) ResumePlay();
                    break;

                case GameState.BasketMade:
                    if (CountdownDone(dt))
                    {
                        Hoop scoredOn = GetAttackingHoop(Possession);
                        BeginInbound(Opponent(Possession), InboundSpotNear(scoredOn));
                    }
                    break;

                case GameState.Timeout:
                    if (CountdownDone(dt))
                        BeginInbound(Possession, MidCourtInbound());
                    break;

                case GameState.QuarterBreak:
                    if (CountdownDone(dt))
                    {
                        Clock.AdvanceQuarter();
                        // Alternate the tip each quarter.
                        TeamSide starter = (Clock.Quarter % 2 == 1) ? TeamSide.Home : TeamSide.Away;
                        BeginTipOff(starter);
                    }
                    break;
            }
        }

        bool CountdownDone(float dt)
        {
            _stateTimer -= dt;
            return _stateTimer <= 0f;
        }

        void OnQuarterExpired()
        {
            if (Clock.IsFinalQuarter)
            {
                State = GameState.GameOver;
                StateChanged?.Invoke(State);
                SetClocksRunning(false);
            }
            else
            {
                State = GameState.QuarterBreak;
                StateChanged?.Invoke(State);
                SetClocksRunning(false);
                _stateTimer = quarterBreakDuration;
            }
        }

        void Turnover(TeamSide toTeam)
        {
            BeginInbound(toTeam, MidCourtInbound());
        }

        void BeginTipOff(TeamSide team)
        {
            Possession = team;
            if (ball != null) ball.transform.position = new Vector3(0f, 1.1f, 0f);
            GiveBallToInbounder(team, new Vector3(0f, 1.1f, team == TeamSide.Home ? -2f : 2f));
            Shot.Reset();
            SetClocksRunning(false);
            State = GameState.TipOff;
            StateChanged?.Invoke(State);
            _stateTimer = tipOffDuration;
        }

        void BeginInbound(TeamSide team, Vector3 spot)
        {
            Possession = team;
            GiveBallToInbounder(team, spot);
            Shot.Reset();
            SetClocksRunning(false);
            State = GameState.Inbounding;
            StateChanged?.Invoke(State);
            _stateTimer = inboundDuration;
        }

        void ResumePlay()
        {
            State = GameState.Playing;
            StateChanged?.Invoke(State);
            SetClocksRunning(true);
        }

        // ---- Helpers -------------------------------------------------------

        void AddPoints(TeamSide team, int points)
        {
            if (team == TeamSide.Home) HomeScore += points; else AwayScore += points;
            ScoreChanged?.Invoke();
        }

        void SetClocksRunning(bool running)
        {
            Clock.Running = running;
            Shot.Running = running;
        }

        PlayerController GetInbounder(TeamSide team)
        {
            var ts = TeamFor(team);
            foreach (var p in ts.onCourt)
                if (p != null && p.isHuman) return p;
            return ts.onCourt.Count > 0 ? ts.onCourt[0] : null;
        }

        void GiveBallToInbounder(TeamSide team, Vector3 spot)
        {
            var inbounder = GetInbounder(team);
            if (inbounder == null || ball == null) return;
            inbounder.Teleport(spot);
            ball.PickUp(inbounder);
        }

        Vector3 InboundSpotNear(Hoop hoop)
        {
            if (hoop == null) return MidCourtInbound();
            Vector3 p = hoop.transform.position;
            float towardCentre = p.z >= 0f ? -1f : 1f;
            return new Vector3(1.5f, 1.1f, p.z + towardCentre * 2.0f);
        }

        Vector3 MidCourtInbound() => new Vector3(2f, 1.1f, 0f);

        void BenchPlayer(PlayerController player, TeamSide side)
        {
            if (player == null) return;
            if (player.Character != null) player.Character.IsBenched = true;
            player.enabled = false;
            player.Teleport(side == TeamSide.Home ? homeBenchAnchor : awayBenchAnchor);
        }

        void ActivatePlayer(PlayerController player, Vector3 entrySpot)
        {
            if (player == null) return;
            if (player.Character != null) player.Character.IsBenched = false;
            player.enabled = true;
            player.Teleport(entrySpot);
        }
    }
}
