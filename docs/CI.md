# Continuous Integration (Unity)

Every push and pull request runs [`.github/workflows/unity-ci.yml`](../.github/workflows/unity-ci.yml):
it opens the project in **Unity 6000.0.23f1** headlessly (via [GameCI](https://game.ci)),
which **compiles every script** and runs the **EditMode unit tests** in
[`Assets/Tests/EditMode`](../Assets/Tests/EditMode). A red check means the code
didn't compile or a test failed — so compile breaks get caught automatically
instead of only at hand-review.

## One-time setup: add a Unity license secret

The headless editor needs a license to activate, so the workflow **fails until
you add one** as a repository secret. The free **Unity Personal** license is
fine.

Go to **GitHub ▸ repo ▸ Settings ▸ Secrets and variables ▸ Actions ▸ New
repository secret**.

### Option A — Personal license (free) → secret `UNITY_LICENSE`
1. Run GameCI's activation once to get a request file: the simplest path is the
   [GameCI activation guide](https://game.ci/docs/github/activation). It has you
   run a tiny workflow that produces a `Unity_v6000.x.alf` artifact.
2. Upload that `.alf` at <https://license.unity3d.com/manual> and download the
   resulting **`.ulf`** file.
3. Create a secret named **`UNITY_LICENSE`** and paste the **entire contents**
   of the `.ulf` file as the value.

### Option B — Pro/Plus license → three secrets
Create **`UNITY_SERIAL`**, **`UNITY_EMAIL`**, **`UNITY_PASSWORD`**. The workflow
already passes all of these through; provide whichever set matches your license.

That's it — once the secret exists, pushes to your branch will show a green/red
**Unity CI** check, and I can read the run, see failures, and fix them.

## Running the same tests locally
In the Unity Editor: **Window ▸ General ▸ Test Runner ▸ EditMode ▸ Run All**.

## Adding more tests
Drop `*.cs` test files in `Assets/Tests/EditMode` (they compile into the
`MarioBasketball.EditMode.Tests` assembly, which references the game's
`MarioBasketball` assembly). Pure-logic tests — math, rosters, stat rules — run
fast and need no scene. Anything that needs live GameObjects belongs in a
PlayMode test assembly (we can add one when needed).
