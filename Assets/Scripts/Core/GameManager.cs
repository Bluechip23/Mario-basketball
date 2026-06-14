using System;
using System.Collections.Generic;
using UnityEngine;
using MarioBasketball.Characters;
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
        [Tooltip("Stamina an Energizer's assist partner gains (Jonah Guy) on a made, assisted basket.")]
        public float assistEnergyBonus = 8f;

        [Header("Stoppage durations (seconds)")]
        public float tipOffDuration = 1.5f;
        public float inboundDuration = 1.5f;
        public float basketMadePause = 1.0f;
        public float timeoutDuration = 3f;
        public float quarterBreakDuration = 3f;

        [Header("Free throws (success scales with Mid Range)")]
        public int freeThrowCount = 2;
        public float freeThrowInterval = 1.1f;
        [Range(0f, 1f)] public float freeThrowMinPct = 0.4f;  // at Mid Range 1
        [Range(0f, 1f)] public float freeThrowMaxPct = 0.95f; // at Mid Range 10

        [Header("Rebounding / loose balls")]
        [Tooltip("Base catch radius; grows with Rebounds, height, jumping and diving.")]
        public float reboundBaseRadius = 1.2f;
        public float reboundRadiusPerStat = 0.06f;
        public float reboundHeightRadius = 0.25f;
        public float reboundJumpReach = 0.7f;
        public float reboundDiveReach = 1.0f;
        public float reboundHeightScore = 1.5f;
        public float reboundJumpScore = 2.0f;
        public float reboundRandom = 1.5f;
        [Tooltip("Wario's offensive-rebound trait rating.")]
        public int offensiveReboundRating = 9;
        [Tooltip("A defender must be within this (arms reach) to pick off a pass.")]
        public float passInterceptRadius = 1.1f;

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

        public bool IsFreeThrow => State == GameState.FreeThrow;
        public PlayerController FreeThrowShooter { get; private set; }
        public int FreeThrowsRemaining { get; private set; }

        public event Action ScoreChanged;
        public event Action<GameState> StateChanged;

        float _stateTimer;
        float _ftTimer;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            Home = new TeamState(TeamSide.Home);
            Away = new TeamState(TeamSide.Away);
            // The Settings screen overrides the inspector defaults per match.
            quarterLengthSeconds = GameSettings.QuarterMinutes * 60f;
            shotClockSeconds = GameSettings.ShotClockSeconds;
            Clock = new MatchClock(quarterLengthSeconds, totalQuarters);
            Shot = new ShotClock(shotClockSeconds);
        }

        void Start()
        {
            BeginTipOff(ContestTip());
        }

        /// <summary>Decide the jump ball, weighted by each team's best
        /// (Power + Rebounds) on the floor.</summary>
        TeamSide ContestTip()
        {
            float home = BestTipScore(Home);
            float away = BestTipScore(Away);
            float total = home + away;
            if (total <= 0f) return TeamSide.Home;
            return UnityEngine.Random.value < home / total ? TeamSide.Home : TeamSide.Away;
        }

        static float BestTipScore(TeamState team)
        {
            float best = 0f;
            foreach (var p in team.onCourt)
            {
                if (p == null || p.Character == null) continue;
                float s = p.Character.GetEffective(StatType.Power) + p.Character.GetEffective(StatType.Rebounds);
                if (s > best) best = s;
            }
            return best;
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

        // ---- Rebounding / loose balls --------------------------------------

        /// <summary>
        /// Resolve a loose ball as a contest: any on-court player within their
        /// effective catch radius competes, and the highest rebound score wins.
        /// Catch radius and score scale with <b>Rebounds</b>, body <b>height</b>,
        /// and whether the player is jumping or diving; missed-shot rebounds give
        /// an offensive-rebound bonus to the trait holder.
        /// </summary>
        void ResolveLooseBall()
        {
            if (ball == null || ball.State != BallController.BallState.Free) return;

            Vector3 ballPos = ball.transform.position;
            PlayerController best = null;
            float bestScore = float.NegativeInfinity;

            best = BestRebounderOn(Home, ballPos, ref bestScore, best);
            best = BestRebounderOn(Away, ballPos, ref bestScore, best);

            if (best != null)
            {
                // Capture pass info before PickUp clears it.
                bool oop = ball.IsAlleyOop && best.team == ball.PassingTeam && NearOwnRim(best);
                PlayerController passer = (ball.IsPass && best.team == ball.PassingTeam) ? ball.Passer : null;

                ball.PickUp(best);
                OnPossessionGained(best);
                if (passer != null) best.OnCaughtPass(passer);
                if (oop) best.CatchAlleyOop();
            }
        }

        bool NearOwnRim(PlayerController p)
        {
            Hoop hoop = GetAttackingHoop(p.team);
            return hoop != null && Horizontal(p.transform.position, hoop.AimPoint) <= reboundBaseRadius + 3f;
        }

        PlayerController BestRebounderOn(TeamState team, Vector3 ballPos, ref float bestScore, PlayerController best)
        {
            foreach (var p in team.onCourt)
            {
                if (p == null || !p.enabled || !ball.CanBePickedUpBy(p)) continue;

                float dist = Horizontal(p.transform.position, ballPos);
                if (dist > ReboundCatchRadius(p)) continue;

                // Vertical reach: a lofted pass sails over a defender's head
                // (their centre is at half height; arms add roughly the rest).
                float reachTop = p.transform.position.y + p.BodyHeight;
                if (ballPos.y > reachTop) continue;

                float score = GrabStat(p) + reboundHeightScore * p.BodyHeight
                            + (p.IsAirborne ? reboundJumpScore : 0f)
                            + (p.IsDiving ? reboundJumpScore * 0.5f : 0f)
                            - dist
                            + UnityEngine.Random.value * reboundRandom;

                if (score > bestScore) { bestScore = score; best = p; }
            }
            return best;
        }

        float ReboundCatchRadius(PlayerController p)
        {
            // Intercepting a live pass is an arms-reach play, not a wide
            // rebound box — a defender across the lane shouldn't pick it.
            if (ball.IsPass && p.team != ball.PassingTeam)
                return passInterceptRadius + reboundHeightRadius * Mathf.Max(0f, p.BodyHeight - 1.6f);

            float r = reboundBaseRadius + reboundRadiusPerStat * GrabStat(p)
                    + reboundHeightRadius * Mathf.Max(0f, p.BodyHeight - 1.6f);
            if (p.IsAirborne) r += reboundJumpReach;
            if (p.IsDiving) r += reboundDiveReach;
            return r;
        }

        /// <summary>The rating that decides who wins a loose ball: a defender
        /// jumping an <b>in-flight pass</b> uses <b>Steals</b> (an interception);
        /// otherwise it's <b>Rebounds</b> (with the offensive-rebound bonus on a
        /// missed-shot board).</summary>
        float GrabStat(PlayerController p)
        {
            if (p.Character == null) return 5f;

            if (ball.IsPass && p.team != ball.PassingTeam)
                return p.Character.GetEffective(StatType.Steals); // pick off the pass

            float reb = p.Character.GetEffective(StatType.Rebounds);
            if (ball.IsRebound && p.team == ball.ShooterTeam
                && p.Character.stats != null && p.Character.stats.hiddenTrait == HiddenTrait.OffensiveRebounder)
                reb = Mathf.Max(reb, p.Character.GetEffectiveFor(offensiveReboundRating));
            return reb;
        }

        static float Horizontal(Vector3 a, Vector3 b)
        {
            a.y = 0f; b.y = 0f;
            return Vector3.Distance(a, b);
        }

        /// <summary>A live shot touched the rim — the attempt counts; reset clock.</summary>
        public void OnRimHit()
        {
            if (State == GameState.Playing)
                Shot.Reset();
        }

        public void RegisterBasket(TeamSide scoringTeam, int points, PlayerController shooter, PlayerController assister = null)
        {
            if (State != GameState.Playing) return;

            UpdateStreaksOnMake(scoringTeam, shooter);
            ApplyAssistEnergy(shooter, assister);
            if (shooter != null && shooter.Character != null) shooter.Character.ShootingRhythm++; // Birdo's Hot Hand
            AddPoints(scoringTeam, points);

            // Clock stops; the other team will inbound under the basket.
            State = GameState.BasketMade;
            StateChanged?.Invoke(State);
            SetClocksRunning(false);
            _stateTimer = basketMadePause;
        }

        /// <summary>Energizer trait (Jonah Guy): when he's on either end of an
        /// assisted make — the scorer off a teammate's pass, or the passer of a
        /// teammate's bucket — that teammate ("the other player") gets a small
        /// stamina boost.</summary>
        void ApplyAssistEnergy(PlayerController shooter, PlayerController assister)
        {
            if (assister == null || shooter == null) return;
            if (IsEnergizer(shooter)) assister.Character?.AddEnergy(assistEnergyBonus);
            if (IsEnergizer(assister)) shooter.Character?.AddEnergy(assistEnergyBonus);
        }

        static bool IsEnergizer(PlayerController p)
            => p != null && p.Character != null && p.Character.stats != null
               && p.Character.stats.hiddenTrait == HiddenTrait.Energizer;

        /// <summary>A field-goal miss (rim time-out or a block) — break the
        /// shooter's make streak. Being on fire is unaffected (only the opponent
        /// scoring puts the fire out).</summary>
        public void OnShotMissed(PlayerController shooter)
        {
            if (shooter == null || shooter.Character == null) return;
            shooter.Character.ConsecutiveMakes = 0;
            shooter.Character.OpponentScoredDuringRun = false;
            shooter.Character.ShootingRhythm--; // Birdo's Hot Hand cools off
        }

        /// <summary>
        /// Heat-check bookkeeping on a made basket. On fire after 6 makes in a
        /// row, or 3 in a row with no opponent basket in between. A teammate
        /// scoring breaks your run; an opponent scoring only blocks the 3-path
        /// and puts out the opponents' fire.
        /// </summary>
        void UpdateStreaksOnMake(TeamSide scoringTeam, PlayerController shooter)
        {
            foreach (var p in TeamFor(scoringTeam).onCourt)
            {
                var c = p != null ? p.Character : null;
                if (c == null) continue;
                if (p == shooter)
                {
                    if (c.ConsecutiveMakes == 0) c.OpponentScoredDuringRun = false; // fresh run
                    c.ConsecutiveMakes++;
                }
                else
                {
                    c.ConsecutiveMakes = 0; // a teammate scored — your run is over
                    c.OpponentScoredDuringRun = false;
                }
            }

            if (shooter != null && shooter.Character != null)
            {
                var sc = shooter.Character;
                if (sc.ConsecutiveMakes >= 6 || (sc.ConsecutiveMakes >= 3 && !sc.OpponentScoredDuringRun))
                    sc.SetOnFire(true);
            }

            // The opponents were just scored on: their fire is out, and any run
            // they have can no longer reach on-fire via the 3-in-a-row path.
            foreach (var o in TeamFor(Opponent(scoringTeam)).onCourt)
            {
                var c = o != null ? o.Character : null;
                if (c == null) continue;
                c.OpponentScoredDuringRun = true;
                c.SetOnFire(false);
            }
        }

        /// <summary>
        /// Record a foul by <paramref name="foulingTeam"/> on
        /// <paramref name="fouled"/>. Below the penalty limit a foul has no
        /// whistle (play continues — the push just disrupts). Once the fouling
        /// team is in the penalty, the fouled player shoots free throws.
        /// Returns true if it blew the whistle (free throws), so the caller
        /// knows not to keep playing the contact out.
        /// </summary>
        public bool RegisterFoul(TeamSide foulingTeam, PlayerController fouled, bool shootingFoul)
        {
            if (State != GameState.Playing) return false;

            var team = TeamFor(foulingTeam);
            team.AddFoul();

            if (team.InPenalty && fouled != null)
            {
                BeginFreeThrows(fouled);
                return true;
            }
            return false;
        }

        void BeginFreeThrows(PlayerController shooter)
        {
            FreeThrowShooter = shooter;
            FreeThrowsRemaining = freeThrowCount;
            SetClocksRunning(false);
            State = GameState.FreeThrow;
            StateChanged?.Invoke(State);
            _ftTimer = freeThrowInterval;
        }

        void ResolveOneFreeThrow()
        {
            if (FreeThrowShooter != null)
            {
                float mid = FreeThrowShooter.EffectiveStat(StatType.MidRange);
                float pct = Mathf.Lerp(freeThrowMinPct, freeThrowMaxPct, Mathf.Clamp01((mid - 1f) / 9f));
                if (UnityEngine.Random.value < pct)
                    AddPoints(FreeThrowShooter.team, 1);
            }
            FreeThrowsRemaining--;
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

        /// <summary>Subs are only allowed during a timeout or a quarter break.</summary>
        public bool CanSubstitute => State == GameState.Timeout || State == GameState.QuarterBreak;

        public void Substitute(TeamSide side, int onCourtIndex, int benchIndex)
        {
            if (!CanSubstitute) return;
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
                    if (Shot.Tick(dt)) { Turnover(Opponent(Possession)); break; }
                    ResolveLooseBall();
                    break;

                case GameState.TipOff:
                case GameState.Inbounding:
                    if (CountdownDone(dt)) ResumePlay();
                    break;

                case GameState.BasketMade:
                    if (CountdownDone(dt))
                    {
                        Hoop scoredOn = GetAttackingHoop(Possession);
                        BeginMadeBasketInbound(scoredOn);
                    }
                    break;

                case GameState.Timeout:
                    if (CountdownDone(dt))
                        BeginInbound(Possession, MidCourtInbound());
                    break;

                case GameState.FreeThrow:
                    _ftTimer -= dt;
                    if (_ftTimer <= 0f)
                    {
                        ResolveOneFreeThrow();
                        if (FreeThrowsRemaining > 0)
                        {
                            _ftTimer = freeThrowInterval;
                        }
                        else
                        {
                            // Fouling team takes the ball out after the attempts.
                            TeamSide inbound = Opponent(FreeThrowShooter != null ? FreeThrowShooter.team : Possession);
                            FreeThrowShooter = null;
                            BeginInbound(inbound, MidCourtInbound());
                        }
                    }
                    break;

                case GameState.QuarterBreak:
                    if (CountdownDone(dt))
                    {
                        Clock.AdvanceQuarter();
                        if (Clock.Quarter == 3) // halftime: team fouls reset
                        {
                            Home.ResetFouls();
                            Away.ResetFouls();
                        }
                        BeginTipOff(ContestTip());
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

        /// <summary>After a made basket the scored-on team takes it out from
        /// under that basket: the <b>big</b> inbounds from the baseline, the
        /// <b>guard</b> sets at the free-throw elbow, and the <b>wing</b> spaces
        /// to the opposite side just beyond the three-point line. The clock is
        /// already stopped (it resumes when play does, after the inbound beat).</summary>
        void BeginMadeBasketInbound(Hoop hoop)
        {
            TeamSide team = Opponent(Possession);
            Possession = team;

            PositionInboundFormation(team, hoop, out PlayerController inbounder);

            if (inbounder != null && ball != null) ball.PickUp(inbounder);
            else GiveBallToInbounder(team, InboundSpotNear(hoop));

            Shot.Reset();
            SetClocksRunning(false);
            State = GameState.Inbounding;
            StateChanged?.Invoke(State);
            _stateTimer = inboundDuration;
        }

        /// <summary>Place the inbounding team's three on-court players into the
        /// made-basket set, by archetype, relative to the hoop they're under.
        /// Roles fall back gracefully so every spot is filled even with an
        /// off-beat lineup. Reports the chosen inbounder (the big).</summary>
        void PositionInboundFormation(TeamSide side, Hoop hoop, out PlayerController inbounder)
        {
            inbounder = null;
            var pool = new List<PlayerController>();
            foreach (var p in TeamFor(side).onCourt)
                if (p != null && p.enabled) pool.Add(p);
            if (pool.Count == 0) return;

            // Geometry from the hoop being inbounded under (it sits 1.6 m in from
            // the baseline; the free-throw line is 5.8 m in; lane half-width 2.45).
            Vector3 hp = hoop != null ? hoop.transform.position : Vector3.zero;
            float dir = hp.z >= 0f ? 1f : -1f;
            float baselineZ = hp.z + dir * 1.6f;
            float ftZ = baselineZ - dir * 5.8f;

            Vector3 inboundSpot = new Vector3(0.6f, 1.1f, baselineZ - dir * 0.5f); // under the rim, inside the baseline wall
            Vector3 elbowSpot   = new Vector3(2.45f, 1.1f, ftZ);                   // free-throw elbow
            Vector3 wingSpot    = new Vector3(-4.7f, 1.1f, hp.z - dir * 5.1f);     // opposite wing, beyond the arc

            PlayerController big   = Take(pool, PlayerArchetype.Big)   ?? TakeAny(pool);
            PlayerController guard = Take(pool, PlayerArchetype.Guard) ?? TakeAny(pool);
            PlayerController wing  = Take(pool, PlayerArchetype.Wing)  ?? TakeAny(pool);

            if (big != null)   { big.Teleport(inboundSpot); inbounder = big; }
            if (guard != null) guard.Teleport(elbowSpot);
            if (wing != null)  wing.Teleport(wingSpot);
        }

        static PlayerController Take(List<PlayerController> pool, PlayerArchetype arch)
        {
            for (int i = 0; i < pool.Count; i++)
            {
                var c = pool[i].Character;
                if (c != null && c.stats != null && c.stats.Archetype == arch)
                {
                    var p = pool[i];
                    pool.RemoveAt(i);
                    return p;
                }
            }
            return null;
        }

        static PlayerController TakeAny(List<PlayerController> pool)
        {
            if (pool.Count == 0) return null;
            var p = pool[0];
            pool.RemoveAt(0);
            return p;
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
