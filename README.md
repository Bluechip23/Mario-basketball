# Mario Street Basketball

An arcade-style **3-on-3** street basketball game starring Mario characters,
inspired by **NBA Street (2001)**, **NBA Jam**, and **Looney Tunes Basketball**
— leaning heavily toward NBA Street (post-ups, alley-oops, tricks,
gamebreakers). Built in **Unity 6** (3D, URP, new Input System).

See [`docs/DESIGN.md`](docs/DESIGN.md) for the full, authoritative design.

## Status

Early prototype. Implemented so far:

- **Playable core loop** — move a player in 3D, scoop up the ball, shoot an arc
  at the hoop (2s/3s by distance), scoreboard, first-to-21.
- **Stat framework** — 14 stats (1-10) + hidden traits, with **Bowser** as the
  demo character. Movement speed and shot accuracy are driven by effective
  stats, and a **stamina/energy** model scales everything as players tire.

The big mechanics (3v3, post-ups, fouling, on-fire streaks, tricks) are
designed in `docs/DESIGN.md` and on the roadmap there.

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
