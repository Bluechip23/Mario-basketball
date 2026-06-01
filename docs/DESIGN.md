# Design Notes

> A living document. The pitch is an arcade basketball game — **NBA Jam's**
> 2-on-2, over-the-top energy and **NBA Street's** trick/flash sensibility —
> dressed in a colourful **Mario-sports** style.

## Pillars

1. **Arcade, not sim.** Fast, readable, exaggerated. Big dunks, deep threes,
   no fouls-as-bookkeeping. Pick-up-and-play in under a minute.
2. **2-on-2.** Small squads keep the screen readable and let each character's
   personality matter.
3. **Character flavour.** Roster of distinct archetypes (speedy, powerful,
   sharpshooter, trickster) with light signature abilities — the
   "Mario sports" hook.
4. **Risk/reward flash.** Trick moves and "on fire" style streaks reward
   aggressive, stylish play.

## Current status — initial core loop

Implemented in this push (single player, runtime-built court):

- 3D character movement on a `CharacterController` (move, sprint, jump).
- Pick up a loose ball by walking over it.
- Shoot on a computed ballistic arc at the attacking hoop; 2 vs 3 points by
  distance; small accuracy spread that grows with range.
- Score detection via a trigger under the rim; scoreboard HUD; auto-reset
  after a make; first-to-21 win check.

## Roadmap (rough order)

- [ ] **Second hoop possession logic** — switch which hoop a team attacks,
      give the defence the ball after a make (currently resets to centre).
- [ ] **2v2** — `PlayerInput`-based device assignment for local multiplayer,
      using `Controls.inputactions`.
- [ ] **AI opponents/teammates** — steering, defending, simple shot selection.
- [ ] **Steal / block / shove** contact interactions.
- [ ] **Charged shots & dunks** — hold-to-charge meter, sweet-spot timing,
      dunk animations when close + airborne.
- [ ] **"On fire" streak** mechanic.
- [ ] **Character roster** with signature abilities and stat spreads.
- [ ] **Art pass** — real models/animations/court art; retire primitive
      bootstrap in favour of authored scenes/prefabs.
- [ ] **Proper UI** — UGUI / UI Toolkit scoreboard, shot clock, menus.
- [ ] **Audio** — crowd, commentary, SFX.

## Architecture intent

- `GameManager` is the single source of match truth (scores, state, shared
  references). Gameplay objects ask it for what they need rather than holding
  cross-references.
- The ball carries shot intent (`ShooterTeam`, `PendingPoints`) so scoring is
  decoupled — the `ScoreZone` just reads what dropped through it.
- Input is abstracted behind `InputReader`, so swapping to per-device
  `PlayerInput` for 2v2 won't touch `PlayerController`'s movement code.
- `GameBootstrap` is scaffolding: it exists so the prototype runs from an
  empty scene. As authored content arrives, it should shrink and disappear.
