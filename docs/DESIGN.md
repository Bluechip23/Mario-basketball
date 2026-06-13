# Mario Street Basketball — Design

> Authoritative design doc. Reflects the project owner's spec. A living
> document — update it as decisions are made.

## Pitch

An arcade-style **3-on-3** street basketball game starring Mario characters,
inspired by **NBA Street (2001)**, **NBA Jam**, and **Looney Tunes Basketball**.
It leans **much more toward NBA Street** than the other two: players can post
up, throw and finish alley-oops, pull off tricks, and build toward
gamebreakers. Flashy, stylish, score-heavy play is the point.

## Pillars

1. **Arcade, not simulation.** Readable, exaggerated, pick-up-and-play — but
   with real depth (post game, tricks, shot variety), NBA-Street-style.
2. **3v3.** Squads of three; each character's identity matters on the floor.
3. **Legible character identities.** A player's archetype should be obvious
   from their stat sheet alone (see Bowser).
4. **Risk/reward flash.** Tricks, gamebreakers, and "on fire" streaks reward
   aggressive, stylish play.

## Core mechanics

### Posting up
- Dedicated mechanic on a button press.
- Backing a defender down is a **button-mash battle** governed by the **Power**
  stat (offense pushes, defender resists).
- Once posted, the offensive player can press different buttons for different
  **post moves**; success depends on **Post Offense** vs the defender's
  **Post Defense** (see stat interactions).

### Tricks, gamebreakers, flash
- Trick points and flashy play accrue toward gamebreakers (NBA Street spirit).
- (Exact gamebreaker reward/UI TBD — to be designed together.)

### Fouling
- Fouling **is allowed with no downside** up to **10 team fouls**.
- At **10 fouls**, the fouled team **shoots free throws** from then on.
- **Team fouls reset at halftime.**
- **No fouling out** — players never get disqualified.
- Power drives both **delivering** fouls (pushes, knocking shooters off course)
  and **withstanding** them.

### Heating up / On fire
- A player gets **on fire** by either:
  - making **3 shots in a row without the opponent scoring**, OR
  - making **6 shots in a row regardless** of what the opponent does.
- The streak is **per-player**: **no other teammate can score** during it
  (a teammate's basket breaks it). A miss also breaks the streak.
- While on fire: **+2 to all stats**, and **+30% chance the shot just goes in**
  (applied *after* the block check — being on fire doesn't help you avoid a
  block, it only helps the ball drop). Stamina drains and **does not refill to
  full** — being on fire mitigates the stamina penalty rather than removing it.
- **On fire ends when the opposing team scores** (chosen rule; a miss does not
  put it out). Implemented in `GameManager` (streak bookkeeping) + the +30%
  make in `PlayerController`/`PostUpController`.

### Hidden stats / traits
- Characters can have **hidden traits** not shown on the stat sheet.
- Example: a player with high 3-Point who is only good **catch-and-shoot**,
  not off the dribble.
- Only the documented example trait exists in code so far; the full set will be
  designed with the roster.

### Passing
- **Tap** pass button → **loft pass**: slow, high arc that **sails over
  defenders' heads** (the loose-ball contest respects the ball's height) but
  hangs in the air longer. **Hold** (≥ `passHoldThreshold`, 0.25 s) → **hard
  pass**: fast and flat, but it travels through the **steal lane**.
- **Directed / icon passing (wired):** **hold LB** to bring up teammate **icons**
  labelled with face buttons (A/B), then press one to pass to that teammate; or
  push the **right stick** toward a teammate and press Pass. With neither, Pass
  goes to the most open teammate.
- Governed by **Ball Handling**: a weak handler's lead pass lands off-target.
  An **in-flight pass can be intercepted with Steals** (a defender jumping the
  lane); once the ball goes stale it becomes a true loose ball decided by
  Rebounds. (Wario's Smooth Passer trait throws as Ball Handling 8, 10 out of
  a post.)

### Alley-oops
A **loft** thrown to a teammate **near the rim** (within `oopRange`) becomes an
alley-oop: the ball lobs high to the rim, the cutter rises to meet it (the
height-aware catch lets them get up over a grounded defender), and on the catch
they **finish immediately** — a dunk if they're a dunker — with an `alleyOopBonus`
to the make. The AI cuts and goes up for oops; a defender can still pick the lob
or block the finish.

### Dribble move (Ball Handling vs Perimeter Defense)
With the ball, **B** attempts a dribble move against the nearest defender. Win
(Ball Handling vs Perimeter Defense) and the defender's **ankles break** — they
freeze briefly and you get a burst of separation; overhandle it against a good
defender and you can get stripped.

## Stats (1-10)

| Stat | Meaning |
|------|---------|
| **Speed** | How fast the player moves. |
| **Ball Handling** | Resistance to steals, effectiveness of dribble moves, and pass success (also covers passing). |
| **3-Point** | Shot success behind the arc. |
| **Mid Range** | Shots outside the paint but inside the arc. |
| **Inside Scoring** | Scoring inside the paint, including layups off drives (not just post moves). |
| **Post Offense** | Success of post moves while down low. |
| **Dunk** | Dunking in traffic and pulling off fancy dunks. |
| **Power** | Withstanding fouls and delivering them; the push mechanic; fortitude going up for dunks/layups; resisting being knocked off course. |
| **Rebounds** | Effectively a **catch radius** for grabbing the ball; higher rebound usually wins a contested board. (Push/body/air-maneuver visuals come later.) |
| **Blocks** | Timing and contesting shots; makes it harder to shoot/dunk over the player. |
| **Steals** | Stealing passes and stripping dribblers. |
| **Post Defense** | Disrupting post moves. |
| **Perimeter Defense** | Quickness defending the perimeter; the guard against dribble moves. |
| **Stamina** | The grand-daddy stat. Sets how fast **energy** fades. |

### Energy model (Stamina)
- A fresh player has **100 energy**. **Energy = effectiveness**: at 100 energy
  the player performs at 100%, at 70 energy at 70%, etc.
- A higher **Stamina** stat makes energy fade **more slowly**.
- Implemented in `PlayerCharacter`: `GetEffective(stat)` scales the raw stat by
  the current energy fraction (and adds the on-fire bonus).

### Shooting model (`ShotMath`)
Shots resolve as an explicit **make probability**, not pure aim: base make% from
the relevant scoring stat (1-10 → ~28-85%), then modifiers add/subtract:
- **Distance falloff within the zone** — deeper = lower make%. Threes lose ~4%
  per foot beyond 1 ft past the arc; mid-range ~2%/ft past the paint; inside
  ~1.5%/ft. So a corner three beats a deep heave, and an elbow jumper beats a
  long two.
- **Deep-Three Specialist** (Peach) instead *gains* on step-backs:
  `+(e^(-0.1543·(x−4.5)²)·10)%` for x = feet behind the line (1-8; 9-10 ft hold
  the 8 ft value), peaking ~+10% around 4.5 ft, until she's finally penalised
  past ~10 ft.
- **Contest** (Perimeter/Post Defense, by proximity) subtracts up to ~35%.
- **On fire** adds +30% (after the block roll).
Blocks are a separate roll *before* the make check, so on-fire never helps you
avoid a block. All knobs are public statics on `ShotMath`.

**Shot timing (jump shots).** Mid-range and three-point shots use a hold-and-
release meter (`PlayerController`): press to rise into the jump, release at the
apex for a **perfect** shot (full make%); mistiming multiplies make% down toward
`minTimingMultiplier`. The AI always releases perfectly.

**Inside finishing (dunk / layup).** Inside the paint, hold Shoot to **go up**
and glide to the rim. It's a **dunk** if effective Dunk ≥ `dunkThreshold`
(scores off the **Dunk** stat, with **Power** resisting blocks), otherwise a
**layup** (off **Inside Scoring**). In the air you can **L1 air-adjust** — it
dodges the block (block chance ×`adjustBlockReduction`) but costs make%, fully
mitigated at **Inside Scoring** 10 — or **pass** out of the air to the most open
teammate. Release (or auto after `finishAirTime`) to commit. The AI finishes
immediately without adjusting.

### Stat interactions
Some stats reinforce or gate one another. Example from the spec:
- **Power vs Post Defense.** A defender with high **Power** but low **Post
  Defense** can hold their ground while being backed down — but the moment the
  offense executes a **post move**, a strong-enough **Post Offense** gets past
  them. Power resists the *push*; Post Defense resists the *move*.

These relationships will be encoded as the post-up and defense systems are
built.

## Controls

Pass · Jump (block / rebound) · Steal · Shoot (double-tap mid-air to alter the
shot, e.g. adjust a layup/dunk) · Push/Foul · Post up · Post move (contextual
buttons while posted) · Defensive stance · Bring up teammate icons · Speed
burst · Dribble move · Timeout.

### Fadeaway / leaning jump shots
On a jump shot, the **move stick** shapes the jump. Hold it in a direction
and the shooter **fades that way** — the body leans and drifts off the
square-up while staying faced to the rim; let go and they rise **straight up**
for a normal shot. A fadeaway is a trade: the separation **lowers the
defender's block and contest** (scaled by how hard you fade), but it's a
**harder shot** — a **flat make penalty** (`ShotMath.FadeMakePenalty`, scaled
by how hard you fade) that is **the same for every shooter**; no stat softens
it. The lone exception is the **Acrobat** trait (Baby Mario), who pays nothing.
Inside finishes (dunks/layups) are unaffected; this is the jump-shot game.

**Momentum matters.** The fade you actually get depends on the direction you
were already running. Leaning **with** your momentum gives the full lean;
leaning **against** it — e.g. driving left and trying to plant and fade right
at the last second — barely leans at all, and the faster you were going, the
harder it is to reverse. From a standstill you can fade freely either way.

### Right-stick dribble flicks
With the ball, **flicking the right stick** (a sharp push that returns to
neutral — distinct from holding it to aim a pass) is a **hard dribble in that
direction**, used to create separation. The move is read relative to the
basket — Ball Handling vs Perimeter Defense decides whether the defender is
shaken (a botched move can be stripped):
- **Away from the basket** → **step-back**: burst backward and square up for
  the shot.
- **Toward the basket** → **quick first-step burst** to attack.
- **Sideways** → **crossover**; two quick **opposite** sideways flicks chain
  into a **hesitation cross**, the full ankle-breaker (defender hits the deck).
- **While posting** → a **shimmy** hop for post separation (toward the hoop it
  also gains a little back-down leverage).
This layers on top of the dribble-move button; it does not replace it.

### Post moves (buttons while posted)
Base: **Hook** (Y) · **Drop Step** (A) · **Spin** (B) · **Fake** (LB). Holding
**turbo (LT)** upgrades each to its advanced version, and chaining off a fake
unlocks the step-through:
- **Sky Hook** (LT+Y): released too high to ever be blocked, but a tougher make.
- **Power Drop Step** (LT+A): a Power-driven bulldoze that shoves (or flattens)
  the defender on the way to the rim.
- **Turnaround Jumper** (LT+B): face up and fade — scored off **Mid Range**;
  the fade nearly kills the block.
- **Up & Under** (A while a bitten fake is live): step through under the
  airborne defender for a nearly free finish.

**Timing the shot.** The footwork (the lunge, spin, power bulldoze, shove) is
instant — but the **shot at the end is timed**, like a jump shot. Triggering a
move plants the player and starts a short release meter (the arms and ball rise
to a peak); press a post button again near the top to release. A clean release
shoots at full make%, an early or late one drops it (down to
`postMinTimingMultiplier`). The AI releases on time automatically. Baby Mario's
**Acrobat** trait applies here too — he barely suffers from a mistimed post
release (so a hook fired the instant he goes up still drops).

> **Controller-first.** The prototype uses a contextual gamepad layout (face
> buttons are shot/pass on offense and post moves while posting; D-pad handles
> timeout/sub), with keyboard fallbacks. Not yet wired: double-tap mid-air shot
> adjust, defensive stance, teammate-icon passing, dribble moves.
> See `docs/GETTING_STARTED.md` for the full map.

## Presentation (NBA Street Vol 1 target)

The current look targets **NBA Street Vol 1**: an elevated **sideline camera**,
fairly low and close so players read big on screen, panning along the court
with the **ball** (not the controlled player — the gold ring marks control);
the court runs left-right with a hoop at each side of the screen. Tunables on
`CameraRig` (sideX/height/FOV/pan range).

Each character has a **placeholder silhouette model** (`CharacterModelBuilder`)
assembled from primitives + a procedural cone — color-coded with the team
**jersey** and signature features (Bowser's shell/horns, Toad's mushroom cap,
Peach's dress/crown, Yoshi's snout/tail, Boo's floating ghost, Piranha Plant's
pipe/bulb, plumber caps/'staches, etc.). Clearly dev-art, swappable for authored
models later. The team jersey distinguishes sides; the gold ring marks who you
control.

Player bodies have **exaggerated, per-character sizes** via
`CharacterStats.heightMeters` (the model + collider scale with it):
Bowser 2.6 m, DK 2.45, Waluigi 2.35, Kritter 2.15, Piranha Plant 2.1,
Wario 2.0, Yoshi 1.95, Luigi/Peach/Birdo 1.9, Daisy 1.85, Mario 1.8,
Koopa 1.7, Shyguy 1.55, Diddy 1.5, Monty Mole 1.45, Boo 1.4, Toad 1.25,
Baby Mario 1.15.
> Heights are a first pass by the implementer for the NBA-Street feel —
> **owner should review/adjust** (they're presentation, not gameplay stats).

## Roster

> **Characters are designed collaboratively. Do not invent or tweak characters
> without sign-off.** Defined in code in `CharacterLibrary`.
> Stat columns: Spd · BH · 3PT · Mid · Ins · PostO · Dunk · Pow · Reb · Blk ·
> Stl · PostD · PerD · Sta.

| Character | Spd | BH | 3PT | Mid | Ins | PostO | Dunk | Pow | Reb | Blk | Stl | PostD | PerD | Sta |
|-----------|----|----|-----|-----|-----|-------|------|-----|-----|-----|-----|-------|------|-----|
| Bowser      | 2 | 2 | 1 | 2 | 10 | 9 | 3 | 10 | 5 | 5 | 8 | 7 | 2 | 4 |
| Donkey Kong | 7 | 2 | 1 | 1 | 4  | 4 | 10| 9  | 9 | 8 | 5 | 8 | 7 | 7 |
| Mario       | 7 | 8 | 7 | 8 | 8  | 7 | 7 | 6  | 7 | 6 | 6 | 4 | 6 | 8 |
| Luigi       | 7 | 5 | 3 | 6 | 7  | 6 | 7 | 6  | 7 | 7 | 6 | 7 | 7 | 8 |
| Peach       | 6 | 6 | 8 | 6 | 4  | 5 | 5 | 3  | 3 | 5 | 6 | 3 | 6 | 8 |
| Toad        | 8 | 10| 5 | 5 | 8  | 2 | 1 | 3  | 3 | 1 | 7 | 1 | 6 | 9 |
| Waluigi     | 6 | 3 | 3 | 3 | 8  | 9 | 6 | 6  | 7 | 8 | 6 | 8 | 1 | 6 |
| Diddy Kong  | 10| 7 | 2 | 3 | 6  | 6 | 4 | 6  | 6 | 5 | 8 | 3 | 9 | 8 |
| Yoshi       | 10| 1 | 1 | 1 | 2  | 1 | 6 | 7  | 7 | 6 | 7 | 7 | 9 | 10|
| Birdo       | 9 | 6 | 8 | 8 | 7  | 4 | 7 | 6  | 5 | 5 | 4 | 3 | 3 | 9 |
| Boo         | 3 | 1 | 10| 6 | 2  | 1 | 1 | 1  | 4 | 2 | 9 | 4 | 4 | 6 |
| Baby Mario  | 7 | 8 | 3 | 6 | 8  | 8 | 2 | 5  | 3 | 3 | 6 | 2 | 6 | 8 |
| Wario       | 4 | 6 | 7 | 10| 6  | 7 | 5 | 8  | 7 | 5 | 6 | 6 | 5 | 6 |
| Piranha Plant | 5 | 3 | 8 | 2 | 3 | 2 | 1 | 6 | 8 | 5 | 4 | 7 | 3 | 6 |
| Daisy       | 7 | 7 | 5 | 9 | 6  | 3 | 3 | 3  | 3 | 3 | 6 | 3 | 8 | 8 |
| Monty Mole  | 7 | 4 | 5 | 5 | 5  | 3 | 3 | 7  | 4 | 7 | 4 | 3 | 10| 8 |
| Koopa       | 6 | 10| 5 | 5 | 5  | 3 | 3 | 8  | 6 | 5 | 6 | 3 | 7 | 9 |
| Kritter     | 6 | 1 | 1 | 2 | 5  | 3 | 4 | 8  | 8 | 10| 3 | 10| 4 | 8 |
| Shyguy      | 6 | 6 | 9 | 9 | 9  | 7 | 4 | 5  | 6 | 6 | 5 | 3 | 5 | 2 |

Hidden traits (wired):
- **Peach — Deep-Three Specialist**: gains make% stepping back behind the arc.
- **Piranha Plant — Quick-Catch Shooter**: a three within `quickCatchWindow`
  (0.3 s) of catching the ball shoots as if 3-Point were 10.
- **Waluigi — Offensive Rebounder**: Rebounds counts as 9 on his own missed-shot
  boards.
- **Wario — Smooth Passer**: passes throw with Ball Handling counted as 8 (10
  out of a post-up), despite his real 6.
- **Koopa — Playmaker**: a teammate who shoots/dunks **directly off his pass**
  (within ~1 s, before driving/dribbling) gets **+2** to the scoring attribute
  they use.
- **Baby Mario — Acrobat**: pays **no** make penalty for altering a shot in the
  air — neither the fadeaway lean nor the L1 air-adjust on a dunk/layup — and
  suffers **~80% less** from a mistimed release (firing the instant he leaves
  the floor or holding it too long). He still gets only the same fade
  *separation* everyone does; he just skips the difficulty. Everyone else `None`.

Lineups are chosen on the pre-match **team select** screen (see below).

## Implementation status

**Done**
- 3D character movement core loop (move, speed-burst sprint, jump) on a
  `CharacterController`; runtime-built court via `GameBootstrap`.
- Pick up loose ball; shoot a ballistic arc at the attacking hoop; 2s/3s by
  distance.
- **Stat framework**: `StatType`, `CharacterStats` (14 stats + hidden trait),
  `CharacterDefinition` (ScriptableObject for editor authoring),
  `CharacterLibrary` (Bowser in code).
- **Energy/effectiveness**: `PlayerCharacter` scales stats by energy, drains
  faster when sprinting, recovers when idle (and 30/min on the bench); on-fire
  knobs + `SetOnFire` hook; `AddEnergy` for timeouts.
- **Stats drive play**: movement speed (Speed) and shot accuracy (3-Point /
  Mid Range / Inside Scoring by distance) come from effective stats.
- **3v3 match structure** (`GameManager` orchestrator):
  - Full court, two hoops with rims; teams attack the far basket.
  - Five-player rosters per team (3 on court, 2 bench); **substitutions**
    (`Substitute`) move players on/off, bench players recover 30/min.
  - **Game clock**: 4 × 4-minute quarters; clock **stops on a made basket
    until the inbound**; final buzzer = `GameOver`.
  - **20-second shot clock**: resets on possession change and on a rim touch
    (`Rim` trigger); expiry = turnover.
  - **Possession & inbounding**: contested tip-off to start (each quarter),
    opponent inbounds after a make, inbound after turnovers.
  - Scoring **2 / 3** by distance (the **1**-point free throw arrives with the
    fouling system).
  - **Timeouts**: **2 per game** per team; calling one grants **+30 energy** to
    the on-court players (`CallTimeout`).
  - **Substitutions only during timeouts and quarter breaks**
    (`GameManager.CanSubstitute`).
  - **No out of bounds**: perimeter walls — ball bounces, players are stopped.
  - Painted lane, three-point arcs, and centre circle.
  - Debug keys: **T** home timeout, **Y** home substitution.
- **AI** (`PlayerAI` drives every non-human player — a whole-game first pass):
  - Offense on ball: **stat-aware shot selection** (shoots only looks it's good
    at + openness + shot clock, so Bowser attacks the rim rather than chucking
    threes), kicks to a meaningfully better/open teammate, otherwise drives.
  - Offense off ball: space to the wings and occasionally **cut** to the rim.
  - Defense: the closest defender pressures the ball and **attempts steals**;
    the others guard their man goal-side while **sagging to help**.
  - Loose balls: the closest teammate chases the rebound.
  - **Contested tip-off** weighted by each team's best (Power + Rebounds).
- **Shot contests, blocks, steals** (in `PlayerController`, so the human is
  subject to them too):
  - A defender near the shooter widens the miss (Perimeter/Post Defense), and a
    point-blank defender can **block** (Blocks vs the finisher's stat).
  - **Steal** strips a nearby handler — Steals vs Ball Handling, on a cooldown
    (Steal button); the AI uses it on defense.
- **Fouling** (push, the RT "muscle" button): shove the nearest opponent —
  Power vs Power knockback, can knock a weaker player down and pop the ball
  loose. Each push is a **team foul**. Below **10 team fouls** there's no
  whistle (play continues, NBA-Jam style); at the limit the fouled team is **in
  the penalty** and every further foul sends them to the line. **Free throws**
  auto-resolve (2 attempts) with make% scaling off the shooter's **Mid Range**;
  the fouling team inbounds afterward. The AI fouls occasionally on the ball.
  (No fouling out; team fouls accumulate over the whole game — tunable.)
- **Rebounding / loose balls** (`GameManager.ResolveLooseBall`): a missed shot
  becomes a live ball as it falls (below `reboundHeight`), then it's a **contest**
  — every on-court player within their **catch radius** competes and the highest
  **rebound score** wins it. Catch radius and score scale with **Rebounds**,
  body **height**, and whether the player is **jumping** or **diving** (so go up
  for the board); the release lockout still stops the shooter insta-grabbing.
  **Wario's Offensive Rebounder** trait now activates — Rebounds counts as 9 on
  his own missed-shot boards. The AI crashes and jumps for boards too.
- **On fire** (heat-check streaks): a player ignites on **3 makes in a row with
  no opponent basket between**, or **6 in a row regardless**; a teammate scoring
  or your own miss breaks the run. While lit: **+2 to all stats** and **+30%**
  the shot drops (after the block check). It goes out when the **opponent
  scores**. Per-player shot attribution via the ball's `Shooter`; the HUD flags
  who's hot.
- **Post-up** (`PostUpController`): hold Post Up to turn your back to the basket
  and start a **back-down battle** — offense taps Back Down (worth their
  **Power**), the defender resists (human taps the same button; AI resists from
  Power + Post Defense). Running `Leverage` backs you toward the rim; a big loss
  **shoves you out**, a bigger one **knocks you down and turns it over**. Post
  moves: **Hook** (high, hard to block), **Drop Step** (lunge + point-blank
  finish, blockable), **Spin** (layup; risks a strip), **Fake** (if the defender
  bites, the next move is freer). Resolution uses **Post Offense vs Post
  Defense** + leverage + **Blocks**. You can still **pass out** of the post
  (kicks to the most open teammate). The AI both posts up and defends the post.
- **Dive for loose balls** (Dive button): a lunge with extended pickup reach.
- **Start menu + Create-a-Player** (`MainMenu`, `CreatePlayerMenu`): the start
  menu routes to an exhibition game or to Create-a-Player, which offers a
  **Journey Character** (limited stats — a 10-point budget with escalating costs:
  reach 1-3 = 1 each, 4-5 = 2, 6-8 = 3, 9 = 4, 10 = 5; earns more in story mode,
  not yet built) or a **Standard Player** (unlimited stats, exhibition only),
  each with an info box. Created players are saved (`CreatedPlayerStore`,
  PlayerPrefs) and appear in team select. (Journey/Story mode itself is a stub.)
- **Team select** (`TeamSelectMenu`): pre-match screen to draft five characters
  per side from the roster — library characters plus created players — (first
  home pick is the player you control), with randomize and sensible defaults;
  `GameBootstrap.StartMatch` then spawns the game. Restart returns to the menu.
- **Pause menu** (`PauseMenu`, Esc / Start): freezes the game and inputs;
  Resume / **Stats** (full stat sheet for all ten players) / Restart / Quit.
- **Player switching** (`PlayerSwitchManager`): exactly one human-controlled
  player at a time; control auto-follows the ball on offense, and the Switch
  button (Q / left shoulder) grabs the nearest man on defense. Camera and HUD
  follow the controlled player, who is marked with a gold ring; control
  recovers automatically if that player is substituted out.

**Assumptions made (confirm / adjust)**
- Timeout's +30 energy is applied to **all on-court players** of the calling
  team (spec said "a player" — flag if it should be one player only).
- Steal/contest/block odds are first-pass numbers exposed as tunable fields on
  `PlayerController`. Because every player is currently Bowser (Ball Handling 2),
  expect plenty of steals — that should settle once real guards exist.

**Not yet built (roadmap, rough order)**
- [ ] AI polish from play-testing: screens, double-teams, smarter help
      rotations, better cut timing, contest jump animations.
- [ ] Local multiplayer device assignment via `Controls.inputactions`.
- [ ] Post-up polish: animations, distinct move feel, jump-to-contest timing.
- [ ] Rebound catch-radius contests (Rebounds stat); steals/blocks polish.
- [ ] Speed burst as distinct mechanic, defensive stance. (Dribble moves are
      in: the B-button move plus right-stick flicks — step-back, burst,
      crossover, hesitation cross, post shimmy.)
- [ ] Passing: tap-loft vs hold-hard, teammate icon targeting.
- [ ] Dunks/alley-oops, mid-air shot adjust (double-tap), tricks + gamebreakers.
- [ ] Hidden-trait effects (e.g. catch-and-shoot penalty off the dribble).
- [ ] Art/animation pass; proper UI (replace the IMGUI HUD); audio.

## Architecture intent

- `GameManager` is the single source of match truth (scores, state, refs).
- The ball carries shot intent (`ShooterTeam`, `PendingPoints`) so scoring is
  decoupled; shooter attribution will be added for the streak system.
- `CharacterStats` is the base sheet; `PlayerCharacter` is the live wrapper that
  applies stamina + on-fire. **Gameplay always reads `GetEffective`**, never the
  raw stat, so stamina/fire are respected everywhere automatically.
- Input is abstracted behind `InputReader` so moving to per-device `PlayerInput`
  for local multiplayer won't touch controller logic.
- `GameBootstrap` is scaffolding so the prototype runs from an empty scene; it
  shrinks and disappears as authored scenes/prefabs arrive.
