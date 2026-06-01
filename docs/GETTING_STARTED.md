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

You should see a hardwood court with two hoops, a red player capsule holding
the ball, and a scoreboard in the top-left.

> Tip: save that scene as `Assets/Scenes/Sandbox.unity` so you can just press
> Play next time. (Scenes aren't committed yet to avoid binary merge pain on
> an empty project — add one once the team is set up.)

## Controls

| Action | Keyboard      | Gamepad        |
|--------|---------------|----------------|
| Move   | WASD          | Left stick     |
| Sprint | Left Shift    | Left trigger   |
| Shoot  | Space         | A / South      |
| Pass   | E             | X / West       |
| Jump   | Left Ctrl     | Y / North      |

Walk over the loose ball to scoop it up, then press **Shoot** to launch an arc
at the hoop your team attacks. Shots from beyond ~7 m count for 3.

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
