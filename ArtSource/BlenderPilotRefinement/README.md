# Bounded Vanguard sleeve and mantle refinement

Base: merged Windows main `c5a734ebe43925eb46fee4c236bb863a1a0d5884` (identical tree to frozen `5244a985`). Only the authored Vanguard mesh changes. No gameplay, fallback, platform, skeleton, clip timing, atlas, material or prop asset changes.

## Shape changes

- Replace the two separate upper-arm sleeve rods with closed continuous sleeve tubes from inside the torso to the glove. Eight ten-vertex rings bridge the existing Spine, UpperArm, Forearm and Hand bones; no vertex has more than two influences. The elbow now has connected flexible cloth beneath the armor, rather than two independently capped pieces meeting at a joint.
- Start the existing vambrace slightly below the elbow and reduce its proximal radius from .12 to .113 metres. This exposes the sleeve transition without rebuilding the armor design.
- Replace the flat mantle outline with a closed 9-column × 6-row grid with 18mm thickness, three longitudinal folds, a bowed rear profile and a retained central hem notch. The existing Spine/Mantle two-bone hinge and action curves remain unchanged. This is authored deformation, not cloth simulation.

## Measured inventory

| Quantity | Before | After | Delta |
|---|---:|---:|---:|
| Body triangles (without sword) | 1,940 | 2,328 | +388 (+20%) |
| Sword triangles | 392 | 392 | 0 |
| Total hero triangles | 2,332 | 2,720 | +388 (+16.64%) |
| Clothes triangles | 304 | 560 | +256 |
| Back triangles | 80 | 212 | +132 |
| Hero vertices including sword | 1,278 | 1,472 | +194 |
| Vanguard FBX bytes | 1,006,236 | 1,018,460 | +12,224 |
| Packed source blend bytes | 3,224,400 | 3,255,424 | +31,024 |
| Hero mesh groups / bones / max influences | 6 / 19 / 2 | 6 / 19 / 2 | 0 |
| Runtime texture / material / FBX file counts | 2 / 1 / 5 | 2 / 1 / 5 | 0 |

All non-Vanguard runtime files are byte-identical to the frozen manifest. The existing source blend still embeds the same two atlas images. These counts are resource inventory, not measured FPS, memory residency or GPU cost.

## Reproduce and validate

Save the base blend, FBX and export manifest **outside Assets** before rebuilding, for example `git show c5a734e:ArtSource/BlenderPilot/Emberfall-Pilot-Vanguard.blend > BEFORE.blend`. Use Blender 4.3.2:

```sh
PILOT_SKIP_RENDER=1 blender -b --python-exit-code 1 --threads 1 --factory-startup --python ArtSource/BlenderPilot/build_vanguard.py
PILOT_RENDER_CLIPS=NONE blender -b --python-exit-code 1 --threads 1 --python ArtSource/BlenderPilot/validate_and_preview.py
blender -b --python-exit-code 1 --threads 1 --python ArtSource/BlenderPilot/validate_fbx_motion.py
blender -b --python-exit-code 1 --threads 1 --python ArtSource/BlenderPilotRefinement/validate_contract.py -- BEFORE.blend ArtSource/BlenderPilot/Emberfall-Pilot-Vanguard.blend CONTRACT.json
python3 ArtSource/BlenderPilotRefinement/update_inventory.py BEFORE-export-manifest.json
python3 Tools/test-blender-pilot.py
python3 Tests/BlenderPilotAdapterProductionTests.py DOTNET_EXECUTABLE
```

The contract check compares exact rest-bone matrices, all five action curves/keyframes/interpolations, packed atlas bytes and all six socket matrices over 240 frames. It additionally checks closed sleeve components, manifold mantle edges, normalized weights and existing material/UV contracts. The separate FBX check imports the newly generated FBX and compares every frame's deformed mesh and sockets against the new authored source.

## Executed checks

- 240 reimported FBX frames: maximum deformed-vertex error 1.20543e−6m and socket error 9.62943e−7m against the regenerated source.
- Source grip-to-Hand.R maximum error 2.66976e−7m; frozen/current socket matrices have zero difference over all 240 frames.
- Exact bone/action/packed-texture equality; Body, Head and Sword rest geometry hashes unchanged. Closed sleeve and mantle topology, finite vertices, material/UV and normalized two-weight checks pass.
- Actual managed production adapters: 97 assertions; loader readiness/fallback: 55 assertions; 14 compiled negative controls. Production pose policy: 141 assertions and two negative controls. Unity engine boundaries are doubles, not engine acceptance.
- Runtime manifest independently recalculated and all nonhero asset hashes verified unchanged.

## Matched native evidence

```sh
blender -b --python-exit-code 1 --threads 1 --python ArtSource/BlenderPilotRefinement/render_refinement.py -- BEFORE.blend OUTPUT/before
blender -b --python-exit-code 1 --threads 1 --python ArtSource/BlenderPilotRefinement/render_refinement.py -- REPOSITORY OUTPUT/after
blender -b --python-exit-code 1 --threads 1 --python ArtSource/BlenderPilotRefinement/render_refinement.py -- BEFORE.blend OUTPUT/before --skill
blender -b --python-exit-code 1 --threads 1 --python ArtSource/BlenderPilotRefinement/render_refinement.py -- REPOSITORY OUTPUT/after --skill
python3 ArtSource/BlenderPilotRefinement/assemble_comparison.py OUTPUT
```

Front/side/back: native Cycles single CPU, 128 samples, 640×800, original shared material. Additional Skill frame25 diagnostic: 64 samples, 480×600. Identical camera, lighting, pose and material per before/after pair; no denoising, no pixel retouching or scaling. Labels are outside the renders.

The actual before portraits reuse the previous native renders of a **byte-identical** source blend with identical camera/light/sample settings; source/image SHA256 verification is recorded in the evidence manifest. The before asset remains outside Assets at `/workspace/scratch/vanguard-refinement/before`. Final native outputs are under `/workspace/scratch/vanguard-refinement`. Committed matched comparisons: front, side, back, Skill frame25. Evidence manifest records input/output hashes, settings and reuse provenance; contract results and [inventory](Review/inventory.json) accompany them.

## Scope and remaining limitations

These are Blender source views and Blender's FBX roundtrip, **not Unity import/binding acceptance or device gameplay**. Review shows the narrow intended gain: a more continuous elbow transition and a modest but visible bowed mantle profile. This is not a full character redesign or quality GO. Existing angular helmet/armor, simple hands and tabard design are retained; the mantle still has visibly limited drape and rigidity from its two-bone setup. A two-bone mantle cannot establish physically simulated folds or collision-free cloth motion. Closed weighted sleeves prove connected topology, not collision-free armor in every pose. Actual engine lighting, interpolation, material mapping and mobile cost remain unverified. The floor is a comparison stage at z−.045; apparent foot clearance is not a Unity contact claim.
