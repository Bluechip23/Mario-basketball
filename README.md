# Mario-basketball

An arcade basketball game in the spirit of **NBA Jam** and **NBA Street**,
with a colourful **Mario-sports** style. Built in **Unity 6** (3D, URP, new
Input System).

> Mario style nba jam, maybe something like nba street.

## Status

Early prototype. The first playable **core loop** is in: move a player in 3D,
scoop up the ball, and shoot an arc at the hoop to score (2s and 3s, with a
scoreboard and a first-to-21 win check).

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
