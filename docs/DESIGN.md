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
- **Tap** pass button → **loft pass**; **hold** → **hard pass**.
- A button brings up **teammate icons** to direct passes to a specific player.
- Governed by **Ball Handling** (pass success).

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
`minTimingMultiplier`. Layups/dunks (inside) fire instantly. The AI always
releases perfectly.

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

> **Controller-first.** The prototype uses a contextual gamepad layout (face
> buttons are shot/pass on offense and post moves while posting; D-pad handles
> timeout/sub), with keyboard fallbacks. Not yet wired: double-tap mid-air shot
> adjust, defensive stance, teammate-icon passing, dribble moves.
> See `docs/GETTING_STARTED.md` for the full map.

## Roster

> **Characters are designed collaboratively. Do not invent or tweak characters
> without sign-off.** Defined in code in `CharacterLibrary`.
> Stat columns: Spd · BH · 3PT · Mid · Ins · PostO · Dunk · Pow · Reb · Blk ·
> Stl · PostD · PerD · Sta.

| Character | Spd | BH | 3PT | Mid | Ins | PostO | Dunk | Pow | Reb | Blk | Stl | PostD | PerD | Sta |
|-----------|----|----|-----|-----|-----|-------|------|-----|-----|-----|-----|-------|------|-----|
| Bowser      | 2 | 2 | 1 | 2 | 10 | 9 | 3 | 10 | 5 | 5 | 8 | 7 | 1 | 4 |
| Donkey Kong | 7 | 2 | 1 | 1 | 5  | 5 | 10| 9  | 8 | 8 | 8 | 8 | 7 | 7 |
| Mario       | 7 | 7 | 7 | 8 | 7  | 7 | 7 | 6  | 6 | 6 | 6 | 4 | 6 | 7 |
| Luigi       | 7 | 5 | 6 | 6 | 8  | 6 | 8 | 6  | 7 | 7 | 7 | 7 | 8 | 8 |
| Peach       | 6 | 6 | 8 | 6 | 5  | 5 | 5 | 3  | 3 | 3 | 6 | 3 | 6 | 8 |
| Toad        | 8 | 10| 5 | 5 | 8  | 2 | 1 | 3  | 3 | 1 | 7 | 1 | 6 | 9 |
| Waluigi     | 6 | 3 | 3 | 3 | 7  | 9 | 6 | 6  | 7 | 8 | 8 | 8 | 1 | 6 |
| Diddy Kong  | 10| 7 | 2 | 3 | 4  | 1 | 6 | 6  | 6 | 5 | 8 | 3 | 9 | 8 |
| Yoshi       | 10| 2 | 2 | 1 | 2  | 2 | 8 | 7  | 7 | 7 | 8 | 7 | 9 | 10|
| Birdo       | 9 | 5 | 8 | 8 | 6  | 6 | 7 | 6  | 5 | 5 | 7 | 7 | 7 | 9 |
| Boo         | 3 | 1 | 10| 6 | 2  | 1 | 1 | 1  | 4 | 2 | 9 | 4 | 4 | 6 |
| Baby Mario  | 7 | 8 | 3 | 6 | 8  | 8 | 2 | 5  | 3 | 3 | 6 | 2 | 6 | 8 |
| Wario       | 4 | 8 | 7 | 10| 6  | 7 | 5 | 8  | 7 | 5 | 8 | 6 | 5 | 6 |
| Piranha Plant | 5 | 3 | 8 | 2 | 3 | 2 | 1 | 6 | 8 | 5 | 4 | 6 | 3 | 6 |
| Daisy       | 7 | 7 | 5 | 8 | 6  | 3 | 3 | 3  | 3 | 3 | 6 | 3 | 8 | 8 |

Hidden traits: **Peach = Deep-Three Specialist** (she gains make% stepping back
behind the arc — see Shooting below); everyone else is `None` for now. The stat
system supports diverse archetypes — Bowser the immobile bruiser, Toad the tiny
handle/motor guard, Diddy/Yoshi the perimeter speedsters, Boo the no-strength
sniper, etc.

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
  - **Timeouts**: 3 per team; calling one grants **+30 energy** to the on-court
    five (`CallTimeout`).
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
- [ ] Dribble moves, speed burst as distinct mechanic, defensive stance.
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
