# Character models — drop them here

This is where authored 3D character models live. Today every player is built
from primitives at runtime by
[`CharacterModelBuilder`](../../Scripts/Presentation/CharacterModelBuilder.cs)
and posed by
[`ProceduralAnimator`](../../Scripts/Presentation/ProceduralAnimator.cs). Real
models will replace that placeholder art.

## Where to put a model

One folder per character, named to match the roster name in
[`CharacterLibrary`](../../Scripts/Characters/CharacterLibrary.cs):

```
Assets/Art/Models/
  Mario/
    Mario.fbx          ← the mesh (rig + skin)
    textures/          ← albedo / normal / etc., next to the model
  Bowser/
    Bowser.fbx
    textures/
  ...
```

Commit the model **and** its generated `.meta` files (Unity creates them on
import). Don't commit `Library/` — it's already git-ignored.

## Formats Unity 6 imports cleanly

- **`.fbx`** — best supported; carries the rig, skin weights and baked
  animation clips. Prefer this.
- **`.glb` / `.gltf`** — works well too (good for Sketchfab downloads).
- `.obj` — geometry only, no rig/animation.
- `.blend` — only imports if Blender is installed on the machine.

Keep them **humanoid-rigged** (a standard biped skeleton) if you can — that
lets us retarget shared animation clips across every character.

## Good sources

- **Mixamo** (mixamo.com, free, Adobe account) — auto-rigs a humanoid mesh and
  gives a big free library of basketball-ish animations (jump, run, idle) you
  can retarget. This is the fastest way to unblock animation, which is the
  current bottleneck.
- **Sketchfab** (sketchfab.com) — filter by *Downloadable* + a permissive
  license; lots of Mario fan models. Grab the glTF/FBX.
- **The Models Resource** (models-resource.com) — ripped, on-model Mario
  characters. Closest to the real look; treat as fan-project reference art.
- **Quaternius / Kenney** (quaternius.com, kenney.nl) — free CC0 stylized
  low-poly characters if you'd rather ship original cartoon art with no IP
  questions.

Once a model is in, tell me the character and file and I'll wire the importer
(scale, rig type, materials) and swap that character off the primitive builder.
