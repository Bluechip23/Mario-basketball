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
- While on fire: the player gets a **stat increase**, but **stamina drains**
  and **does not refill to full** — being on fire *mitigates* the stamina
  penalty rather than removing it.
- **Open questions (need owner input):** exactly which stats get boosted and by
  how much; what ends the on-fire state (opponent scores? a miss? a timer?).
  Current code exposes tunable knobs and a `SetOnFire` hook but does **not**
  yet decide these — see Implementation status.

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

> The current prototype implements a reduced subset (move, sprint/burst, jump,
> shoot, pass) on keyboard + gamepad. See `docs/GETTING_STARTED.md`.

## Roster

> **Characters are designed collaboratively. Do not invent characters.**
> Only the demo character below exists.

### Bowser (demo)
Speed 2 · Ball Handling 2 · 3-Point 1 · Mid Range 2 · Inside Scoring 10 ·
Post Offense 9 · Dunk 3 · Power 10 · Rebounds 5 · Blocks 5 · Steals 8 ·
Post Defense 7 · Perimeter Defense 1 · Stamina 4 · Hidden: none.

A dominant interior force who gasses out and can't play on the perimeter. The
stat system should support diverse archetypes across the Mario roster.

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
  - **Game clock**: 4 × 4-minute quarters, alternating tip each quarter; clock
    **stops on a made basket until the inbound**; final buzzer = `GameOver`.
  - **20-second shot clock**: resets on possession change and on a rim touch
    (`Rim` trigger); expiry = turnover.
  - **Possession & inbounding**: contested tip-off to start (each quarter),
    opponent inbounds after a make, inbound after turnovers.
  - Scoring **2 / 3 / 1** (free throw via `RegisterFreeThrow`).
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
  - **Steal** strips a nearby handler — Steals vs Ball Handling, on a cooldown.
    Bound to **F / B**; the AI uses it on defense.
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
- [ ] Post-up: button-mash back-down (Power) + contextual post moves
      (Post Offense vs Post Defense).
- [ ] On-fire streak tracker (needs per-player shot attribution; needs owner
      decisions on boost magnitude and exit condition).
- [ ] Fouling + push mechanic; team-foul count → free throws at 10.
- [ ] Rebound catch-radius contests (Rebounds stat); steals/blocks polish.
- [ ] Dribble moves, speed burst as distinct mechanic, defensive stance.
- [ ] Passing: tap-loft vs hold-hard, teammate icon targeting.
- [ ] Dunks/alley-oops, mid-air shot adjust (double-tap), tricks + gamebreakers.
- [ ] Hidden-trait effects (e.g. catch-and-shoot penalty off the dribble).
- [ ] Free-throw flow (the +1 scoring and penalty count exist; shooting does not).
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
