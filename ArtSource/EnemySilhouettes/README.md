# F2 — enemy and encounter silhouette modules

Nine original Blender rigid modules replace existing visible meshes at the end of the actual `CombatModel.Enemy` and `LargeExpeditionBoss.CreateAnchors` factories. One assembled rig owns every action; no clip swaps bodies. Existing node names, hierarchy, local TRS, palette materials, hit shapes, control clocks, anchor count/mask/level/radius and encounter phases remain unchanged. No purchases or external source art.

## Finite scope and evidence

- Goblin: pointed folded ears, asymmetric leather cap/rim and curved beveled knife. Original face, limbs and E04 animation remain.
- Guardian and GuardianBoss: beveled chest, layered stone shoulders, open toothed crown. Original hammer, exposed crown crystal, boss horns/proportions, tactical sockets and E04 down/death ownership remain.
- Slime and Wisp: retained. `Before/After/Enemies-front.png` and `Enemies-back.png` show the five complete factories. Slime has a flat base and readable eyes; Wisp retains core, horns, face and trailing forms. Exact geometry/palette equality is checked for both, including four actual animation phases.
- Large astrolabe: complete existing body retained (`Assemblies/Astrolabe-retained.png`, actual `LargeBossRig.Build` rest pose). Only anchor plinth, crystal and cage claws change. `Assemblies/Anchor-pair.png` compares the real `CreateAnchors` output. Both anchors are translated by their own root origin for inspection, with the same camera/light/scale. Placement/state remain tested at original world coordinates.
- `Assemblies/Action-{walk,windup,contact,recovery}-{before,after}.png`: actual factory + production `Animate` output with real locomotion advancement, same camera and labels. Managed TRS is exported then rendered in Blender. These are static phase samples, not Unity frames or a timing video.
- `Down/GuardianDown-Low.png` and `GuardianDown-Side.png`: same real down pose, two cameras matching the prior E04 low and side before samples. New shoulder minimum y=0.00101137 m; feet remain y=0.022183 and 0.00283885 m; hammer y=0.299751 m. Thick shoulder armor supports the torso; chest/back are elevated, not flush to the ground. See `support-evidence.json`.

## Budget

| Module | Triangles | Source bytes |
|---|---:|---:|
| GoblinEar | 36 | 3,900 |
| GoblinCap | 140 | 15,132 |
| GoblinKnife | 76 | 8,220 |
| GuardianChest | 60 | 6,492 |
| GuardianShoulder | 44 | 4,764 |
| GuardianCrown | 228 | 24,636 |
| AnchorPlinth | 136 | 14,700 |
| AnchorCrystal | 56 | 6,060 |
| AnchorClaw | 56 | 6,060 |

Total: **832 triangles across nine reusable modules, 89,964 resource source bytes; each ≤512 triangles; zero new textures or runtime materials.** All use the existing shared surface palette. GUIDs are deterministic on first creation and preserved on rebuild. `budget.json` records exact hashes and bounds. `EnemySilhouettes.blend` is editable source, excluded from Unity Assets. Runtime import uses the existing strict EFM1 decoder.

| Complete visible assembly | Before tri | After tri | Parts | Shared materials after |
|---|---:|---:|---:|---:|
| Slime | 2,768 | 2,768 | 8 | 6 |
| Wisp | 2,392 | 2,392 | 9 | 5 |
| Goblin | 9,772 | 8,812 | 31 | 14 |
| Guardian | 10,328 | 9,504 | 34 | 16 |
| GuardianBoss | 10,424 | 9,600 | 36 | 17 |
| One anchor | 1,644 | 360 | 5 | Existing palette |
| Large astrolabe body | 9,532 | 9,532 | 34 | Existing palette |

Counts include existing non-F2 pieces. Material counts are shared references, not measured device draw calls. Low quality uses these same smaller meshes, no extra lights/materials/textures; there is no separate low quality geometry tier. Resource source bytes are not a measured IPA/EXE increment. Managed/GPU allocation, compressed build size and device frame time remain unmeasured.

## Fallback and lifecycle

`EnemySilhouetteArt.Enabled=false` before construction retains the complete prior factory presentation. Missing, malformed or over-envelope individual modules retain their original mesh; failures cache once. Envelope and ≤512-triangle checks supplement the strict decoder. Cache shares meshes and resets at subsystem registration. This is a startup/construction switch, not a live toggle that restores already-replaced meshes. Old mesh ownership is unchanged; no gameplay component or collider is replaced.

## Reproduce and validate

Run from repository root (Blender 4.3.2, .NET 8):

```sh
blender -b -t 2 --factory-startup --python ArtSource/EnemySilhouettes/build.py
python3 ArtSource/EnemySilhouettes/export_validate.py . /tmp/f2-production /path/to/dotnet
# Tests/EnemySilhouetteProductionTests.py is the discoverable equivalent using a temporary directory.
DOTNET=/path/to/dotnet python3 Tests/EnemySilhouetteAnimationTests.py --output /tmp/f2-animation.json
DOTNET=/path/to/dotnet python3 Tests/EnemyKnockdownGeometryTests.py --output /tmp/f2-down.json
DOTNET=/path/to/dotnet python3 Tests/EnemyKnockdownProductionTests.py
blender -b -t 2 --factory-startup --python ArtSource/EnemySilhouettes/render_assemblies.py -- /tmp/f2-production/f2-assemblies.json /tmp/f2-animation.json /tmp/f2-review
blender -b -t 2 --factory-startup --python ArtSource/EnemyKnockdown/render_support_review.py -- /tmp/f2-down.json /tmp/f2-support
```

The `render_review.py` overview takes actual tactical exporter `enemies.json`. Before was captured from the unchanged base factory; after is the same factory plus F2. `verify_review.py` accepts before/after enemy JSON and animation JSON after the images exist in this folder.

Raw checks:

- `production-validation.log`: 108 actual factory/strict-decoder/cache/fallback/anchor assertions; two removed-hook mutations rejected. Raw negative output in `missing-*-hook.log`. Anchor placement/path queries are explicit permissive fixture boundaries; actual CreateAnchors geometry, properties and state calls execute unchanged.
- `animation-validation.log`: 215,960 production factory/Animate assertions over five identities, four phases, both presentations; mesh and palette references stable. Counts include finite vertex checks, not that many distinct scenarios.
- `down-geometry.log`: 1,900 actual pose/geometry samples, ground negative control. `death-regression.log`: 1,551 overlay/status assertions +240 actual controller/death capture assertions, eight mutations; includes Update→kill→LateUpdate, pause, airborne, repeated control and expired stale cache.
- `tactical-clearance.log`: 40 actual factory/state combinations and two clearance mutations; original socket names preserved.
- `large-boss-regression.log`: 19 source contracts for existing phase/beam alignment; source checks only.
- `rebuild.log`: nine resource hashes/budgets identical on second Blender build.
- `review-checks.json` / `review-validation.log`: retained-species geometry equality, reduced full-instance triangles, measurable image changes. Pixel differences are not aesthetic acceptance.

All images here are **Blender previews** of managed-exported production geometry. The palette for these enemy/anchor/body paths comes directly from production inline colors; unused hero ClassColor/RarityColor fixture stand-ins do not enter these samples. Unity shaders, camera/occlusion, particles/dissolve, runtime batching and Windows/iOS/Android device behavior have **not** been engine-validated. Beam rotation/exposed/stopped large-rig phases are not depicted by the rest-pose image. Parent aggregate checks and device acceptance remain separate.
