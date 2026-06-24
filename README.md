# 🏀 Mario Street Basketball

### Instruction Booklet

An arcade-style **3-on-3 street basketball** game starring the Mushroom
Kingdom's finest — built in the spirit of **NBA Street**, **NBA Jam** and
**Looney Tunes Basketball**. Post up a Koopa, cross up a Boo, throw down a
poster over Bowser, and light the rim on fire. Welcome to the playground.

> Early prototype. Everything renders from placeholder "dev-art" models for
> now — gameplay first, glamour later. Built in **Unity 6** (URP, the new
> Input System).

---

## 🎮 The Game

Two teams of three play full-court, winner-stays-hot street ball. Sink shots
to score, crash the glass, pick pockets, and back the little guys down on the
block. There's no out-of-bounds — the court is walled, so the ball (and the
action) never stops. It's fast, it's physical, and it rewards reading the
floor over chucking.

**Object:** outscore the other squad before the clock runs out.

**Scoring:** 3 from behind the arc · 2 inside it · 1 per free throw.

---

## 🕹️ Controls

Controller-first, but fully playable on keyboard. Menus take the d-pad / left
stick / arrow keys, **A**/**Enter** to confirm, **B**/**Tab** to back out, and
the **right stick** scrolls long lists.

### General
| Action | Gamepad | Keyboard |
| --- | --- | --- |
| Move | Left stick | WASD |
| Turbo / sprint (hold) | LT | Left Shift |
| Pause | Start | Esc |
| Call timeout | D-pad Up | T |
| Substitute | D-pad Down | Y |

### Offense — with the ball
| Action | Gamepad | Keyboard |
| --- | --- | --- |
| Shoot jumper (release at the marker) | X | Space |
| Dunk / layup inside (hold) | X | Space |
| Air-adjust a finish (hold) | LT | Left Shift |
| Pass — tap = loft, hold = bullet | A | E |
| Aim the pass | Right stick | IJKL |
| Pass to a teammate icon | Hold LB + A/B | Hold C + A/B |
| Fade on a jumper | Hold Move | Hold Move |
| Dribble move (break the defender down) | B | X |
| Dribble flick — step-back / crossover | Right-stick flick | — |
| Post up (hold) | RB | R |
| Back down in the post (hold) | RT | B |
| Post: pump fake / spin / turnaround jumper | Y / B / X | T / V / H |
| Post advanced (hold LT) | hook (LT + Y) / power drop step (LT + X) | Shift + T / Shift + H |

### Offense — off the ball
| Action | Gamepad | Keyboard |
| --- | --- | --- |
| Move / cut to get open | Left stick | WASD |
| Sprint (hold) | LT | Left Shift |
| Jump for a rebound / alley-oop | Y | Left Ctrl |

### Defense
| Action | Gamepad | Keyboard |
| --- | --- | --- |
| Switch to the man nearest the ball | A / LB | Q |
| Steal | X | F |
| Jump / block / contest | Y | Left Ctrl |
| Push / foul (or bump a poster) | RT | B |
| Dive for a loose ball | B | X |

> The full reference is always in the game under **Settings ▸ Controls**.

---

## 🏟️ Game Modes

- **Exhibition** — Jump straight into a game. Draft five players per side on
  the Team Select screen (the first HOME pick is the player you control), then
  tip off. Randomize a squad or hand-pick your dream team.
- **Create-a-Player** — Build your own baller and add them to the Exhibition
  pool (see below).
- **Journey (Story Mode)** — *Coming soon.* Take a created player across the
  kingdom, beating teams to earn stat points and grow your legend.
- **Settings** — Tune master volume, quarter length, shot clock, timeouts,
  vibration and the on-court box score, and browse the full controls.

---

## ⛹️ The Roster

23 characters, each built around a real role — Guards run the show, Wings score
at all three levels, and Bigs own the paint. Flip any card on the select screen
to see the full stat sheet.

**Guards**
- **Toad** — Tiny, tireless and nearly impossible to strip; the best pure handle in the kingdom.
- **Diddy Kong** — Pure jet fuel; the fastest end-to-end menace on the floor.
- **Boo** — Barely moves or defends, but the spooky wide-open corner three never misses. *(Wide-Open Sniper)*
- **Baby Mario** — All the captain's craft (and a grown-up post game) in a knee-high package. *(Acrobat)*
- **Monty Mole** — A burrowing perimeter pest; there's no driving around, under or through him.
- **Koopa** — The shell-backed floor general; elite handle and vision. *(Playmaker)*
- **Shyguy** — A masked bucket-getter who scores at all three levels — but the tank empties fast.
- **Delfan** — A deliberate sharpshooter with real handle; give him a sliver and the three drops. *(Called Shot)*

**Wings**
- **Mario** — The all-around captain; scores from anywhere and never takes a possession off.
- **Luigi** — The dependable two-way wing with a sneaky knack for finishing inside.
- **Peach** — Royal range; buries threes from *way* downtown. *(Deep-Three Specialist)*
- **Yoshi** — An elite athlete who outruns everyone and defends everything — just don't ask him to shoot.
- **Birdo** — A sprinting flame-thrower of pull-up threes and transition daggers. *(Hot Hand)*
- **Daisy** — A pesky on-ball defender with the purest mid-range stroke in the kingdom. *(Killer Instinct)*
- **Qui-gon** — A blur in a mask; scales any defense for steals and rim attacks — allergic to jumpers.

**Bigs**
- **Bowser** — An immovable monster on the low block; back him in deep and nothing stops him.
- **Donkey Kong** — The kingdom's most violent finisher; every dunk is a poster.
- **Waluigi** — A lanky shot-swatter who feasts on the offensive glass. *(Offensive Rebounder)*
- **Wario** — The bully with a silk mid-range jumper and slick (smug) passing. *(Smooth Passer)*
- **Piranha Plant** — A planted catch-and-shoot tower; swing it fast and it bites from three. *(Quick-Catch Shooter)*
- **Kritter** — A scaly wall under the rim: blocks, boards and bad intentions.
- **Laurentius** — A board-vacuuming enforcer who defends the paint and the perimeter alike.
- **Jonah Guy** — A whale of a worker who never tires and bangs inside. *(Energizer)*

---

## 📊 Player Stats

Every player is rated **1–10** across 14 stats. Everything you do on the floor
is driven by them, and a **stamina / energy** model quietly scales it all as
players tire (rest them on the bench or burn a timeout to recover).

`Speed` · `Ball Handling` · `3-Point` · `Mid Range` · `Inside Scoring` ·
`Post Offense` · `Dunk` · `Power` · `Rebounds` · `Blocks` · `Steals` ·
`Post Defense` · `Perimeter Defense` · `Stamina`

### ⭐ Special Abilities (hidden traits)
A handful of characters carry a signature trait:

- **Deep-Three Specialist** (Peach) — *gains* range the further behind the arc she steps.
- **Hot Hand** (Birdo) — builds a rolling heat bonus as she keeps hitting.
- **Wide-Open Sniper** (Boo) — a bonus on uncontested threes that vanishes the instant a defender closes out.
- **Offensive Rebounder** (Waluigi) — crashes the offensive glass far above his size.
- **Acrobat** (Baby Mario) — shrugs off shot-mistiming and fadeaway penalties in the air.
- **Smooth Passer** (Wario) — passes like an elite handler, especially out of the post.
- **Quick-Catch Shooter** (Piranha Plant) — a deadeye on the catch-and-shoot.
- **Killer Instinct** (Daisy) — punishes a tired opponent with bonus scoring and defense.
- **Playmaker** (Koopa) — his dimes make the finisher's shot easier.
- **Called Shot** (Delfan) — a couple of guaranteed buckets a game.
- **Energizer** (Jonah Guy) — refuels a teammate's legs whenever he assists a basket.

---

## 🛠️ Create-a-Player

Build your own baller from the main menu in one of two flavors:

- **Journey Character** — Spend a **point budget** across the 14 stats. Costs
  rise as a stat climbs (1–3 cost 1 each, 4–5 cost 2, 6–8 cost 3, 9 costs 4,
  10 costs 5), so you can't max everything — pick your identity. Earn more
  points in Journey mode (coming soon).
- **Standard Player** — **Unlimited** stats, set freely 1–10. Exhibition-only,
  for testing builds and dream-team chaos.

Saved players show up alongside the roster on the Team Select screen.

---

## 🔥 On the Court

- **Post game** — Hold to back your defender down (a Power-vs-Power battle).
  Your **shots** are the **turnaround fadeaway** (X) and the **hook** (LT + Y):
  each pops a release meter you time like a jumper — hold to rise, let go at the
  marker to bury it. Your **moves** never score on their own — they beat the
  defender and break you **out of the post on a drive to the rim**, where you
  finish it yourself (a dunk/layup) or kick it out: a **spin** (B) shakes the
  man, a **power drop step** (LT + X) bulldozes him aside. A **pump fake** (Y)
  gets him in the air first; **A** passes any time. Lose the back-down battle and
  you get shoved off or put on the floor.
- **Above-the-rim finishes** — Dunkers soar over the rim, grab iron and throw
  it down; everyone gets enough air to adjust a layup or dunk around a
  shot-blocker. Defenders can rise to the rim to swat it.
- **Alley-oops** — Sky for a lob near the rim and catch it in traffic.
- **Dribble moves & flicks** — Crossovers, behind-the-back, hesitations and
  right-stick step-backs to create separation (and break ankles).
- **Defense** — Contest and block shots, strip handlers (Steals vs Ball
  Handling), switch onto the ball (your controlled player wears a gold ring),
  push and bump, and dive for loose balls.
- **Fouls** — A push button knocks players around with no whistle until a team
  hits the penalty, then it's free throws.
- **On Fire** — Heat up and watch the rim get bigger.
- **Match flow** — Four quarters (4 minutes each by default), a 20-second shot
  clock, contested tip-off, timeouts (which refill energy), five-player rosters
  with substitutions, and walls instead of out-of-bounds.

---

## ▶️ Running it

This is a Unity 6 project that builds its whole playable court from code — no
fragile scene/prefab assets to wire up.

1. Open the folder in **Unity 6 LTS** (`6000.0.x`) and let it resolve packages.
2. If prompted, enable the **new Input System** backend (or set *Project
   Settings ▸ Player ▸ Active Input Handling* to **Both**).
3. New empty scene ▸ create an empty GameObject ▸ add the **Game Bootstrap**
   component ▸ press **Play**.

Full setup and project layout: [`docs/GETTING_STARTED.md`](docs/GETTING_STARTED.md).
Design vision and roadmap: [`docs/DESIGN.md`](docs/DESIGN.md).
Dropping in real character models: [`Assets/Art/Models/README.md`](Assets/Art/Models/README.md).
