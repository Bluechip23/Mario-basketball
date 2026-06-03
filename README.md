# Mario Street Basketball

An arcade-style **3-on-3** street basketball game starring Mario characters,
inspired by **NBA Street (2001)**, **NBA Jam**, and **Looney Tunes Basketball**
— leaning heavily toward NBA Street (post-ups, alley-oops, tricks,
gamebreakers). Built in **Unity 6** (3D, URP, new Input System).

See [`docs/DESIGN.md`](docs/DESIGN.md) for the full, authoritative design.

## Status

Early prototype. Implemented so far:

- **Full-court 3v3 match structure** — 4×4-minute quarters, 20-second shot
  clock, contested tip-off, possession/inbounding, made-basket clock stops,
  scoring (2/3/1), three timeouts (+30 energy), five-player rosters with
  substitutions and bench recovery, and walls instead of out-of-bounds.
- **AI** — a whole-game first pass: stat-aware shot selection (Bowser attacks
  the rim, doesn't chuck threes), kick-out passing, off-ball cuts, help
  defense, and steals. The tip is contested by Power + Rebounds.
- **Shot contests, blocks and steals** — defenders widen misses, swat
  point-blank looks, and strip handlers (Steals vs Ball Handling); the human is
  subject to all of it too.
- **Player switching** — control auto-follows the ball on offense; a Switch
  button grabs the nearest defender. The controlled player wears a gold ring.
- **Team select** — a pre-match screen to draft five characters per side from
  a 13-strong roster (Bowser, DK, Mario, Luigi, Peach, Toad, Waluigi, Diddy,
  Yoshi, Birdo, Boo, Baby Mario, Wario).
- **Stat framework** — 14 stats (1-10) + hidden traits, with **Bowser** as the
  demo character. Movement speed and shot accuracy are driven by effective
  stats, and a **stamina/energy** model scales everything as players tire.

The remaining mechanics (post-ups, fouling, on-fire streaks, tricks, smarter
AI) are designed in `docs/DESIGN.md` and on the roadmap there.

## Quick start

1. Open the folder in **Unity 6 LTS** (`6000.0.x`). Let it resolve packages.
2. If prompted, enable the **new Input System backend** (or set
   *Project Settings ▸ Player ▸ Active Input Handling* to **Both**).
3. New empty scene ▸ create an empty GameObject ▸ add the **Game Bootstrap**
   component ▸ press **Play**.

Full setup, controls, and project layout: [`docs/GETTING_STARTED.md`](docs/GETTING_STARTED.md).
Vision and roadmap: [`docs/DESIGN.md`](docs/DESIGN.md).

## Controls

Move **WASD** / left stick · Sprint **Shift** · Shoot **Space** · Pass **E** ·
Jump **Ctrl** (gamepad supported).
