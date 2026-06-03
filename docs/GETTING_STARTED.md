# Getting Started

This is a Unity project for an arcade basketball game in the spirit of
**NBA Jam** / **NBA Street**, with a Nintendo "Mario sports" flavour.

## Requirements

- **Unity 6 LTS** (`6000.0.x`). The exact version is pinned in
  `ProjectSettings/ProjectVersion.txt`; any close `6000.0` patch is fine.
- The **Input System** and **Universal Render Pipeline (URP)** packages are
  listed in `Packages/manifest.json` and will be resolved automatically when
  Unity first opens the project.

## First open

1. Open the project folder in Unity Hub. Unity will import packages and
   generate the `Library/`, `Logs/`, and `ProjectSettings/*` files that are
   intentionally **not** committed (see `.gitignore`).
2. When prompted to **enable the new Input System backend**, choose **Yes**
   (this restarts the editor). If you are not prompted, set it manually:
   **Edit ▸ Project Settings ▸ Player ▸ Active Input Handling = Both**.
   This must include the new Input System or the controls won't respond.

## Run the playable core loop

There is **no committed scene** — the playable court is built at runtime by
`GameBootstrap`, so there are no fragile scene/prefab GUIDs to keep in sync.

1. Create a new empty scene (**File ▸ New Scene ▸ Empty**).
2. Create an empty GameObject (**GameObject ▸ Create Empty**), name it
   `Bootstrap`.
3. Add the **Game Bootstrap** component to it (Add Component ▸ search
   "Game Bootstrap").
4. Press **Play**.

You'll first see the **Team Select** screen over the empty court: draft five
characters for each side (your first HOME pick — marked ★ — is the player you
control), then **Start Game**. The match spawns and tips off.

> Tip: save that scene as `Assets/Scenes/Sandbox.unity` so you can just press
> Play next time. (Scenes aren't committed yet to avoid binary merge pain on
> an empty project — add one once the team is set up.)

## Controls

**This is a controller-first game** (Xbox layout below; PlayStation equivalents
in parentheses). The buttons are **contextual** — the face buttons are your
shot/pass on offense and your post moves while you hold Post Up. Keyboard
bindings exist as a fallback.

| Action | Gamepad | Keyboard |
|--------|---------|----------|
| Move | Left stick | WASD |
| Turbo / speed burst | LT (L2) | Left Shift |
| **Shoot** | A (✕) | Space |
| **Pass** (to most open teammate) | X (□) | E |
| **Jump / contest / rebound** | Y (△) | Left Ctrl |
| **Steal** | X (□) — when you have no ball | F |
| **Switch player** (defense) | A (✕) or LB (L1) | Q |
| **Dive** for a loose ball | B (○) | X |
| **Post up** (hold) | RB (R1) | R |
| **Back down** / bump a poster | RT (R2) | B |
| Post move — **Hook** | Y (△) | H |
| Post move — **Drop step** | A (✕) | G |
| Post move — **Spin** | B (○) | V |
| Post move — **Fake** | LB (L1) | C |
| Timeout (home) | D-pad Up | T |
| Substitute (home) | D-pad Down | Y |
| Pause / Stats | Start | Esc |

**Post game:** with the ball, hold **RB** to turn your back to the basket, then
tap **RT** (Back down) to bulldoze your defender toward the rim — a
Power-vs-Power tap battle; if they win they shove you off or knock you down.
With position, hit a post move: **Y** Hook, **A** Drop step, **B** Spin, **LB**
Fake. You can still **pass out** with **X**. On defense against a poster, mash
**RT** to bump them off.

> Because the buttons are contextual, the same face button does different things
> by situation (e.g. **A** shoots with the ball, switches on defense, and is the
> Drop step while posting). The on-screen HUD lists the current scheme.

It's a full-court **3v3** game. You control the player marked with the **gold
ring**: on offense control follows the ball automatically, and on defense you
press **Switch** to take the teammate nearest the ball. Walk over a loose ball
to scoop it up, then **Shoot** to launch an arc at the basket your team attacks;
shots from beyond the arc count for 3.

The other five players are AI-controlled: your two teammates space and defend,
the three opponents drive, shoot, pass and guard. The tip-off is contested by
Power + Rebounds. The HUD shows quarter, game clock, shot clock, possession,
timeouts and your energy.

## Where things live

```
Assets/Scripts/
  Core/        GameManager, match state, team enum
  Gameplay/    PlayerController, BallController, Hoop, ScoreZone
  Input/       InputReader (new Input System, defined in code)
  Camera/      CameraRig (chase camera)
  UI/          DebugHUD (temporary IMGUI scoreboard)
  Bootstrap/   GameBootstrap (builds the runtime court)
Assets/Settings/
  Controls.inputactions   Editable action asset for future rebinding
docs/
  DESIGN.md    Vision and roadmap
```
