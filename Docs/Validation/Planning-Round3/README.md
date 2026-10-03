# Planning Round 3 delivery

This batch completes the finite Planning1–5 and H01–H06 implementation scope. It builds on the previous authored-art batch; the validation below does not constitute Unity or visual acceptance.

## Reviewed behavior

| Item | Delivered behavior | Evidence directory |
| --- | --- | --- |
| Planning1 | Delayed casts retain first-hit qualification independently of the newest cast; cooldown consumption, birth epoch and producer lifetime remain authoritative. Bounded live receipt saturation fails closed for qualification without altering damage. | `DelayedHits/` |
| Planning2 | Preserves three sustained training scenarios and adds two normal-health, real-AI pressure scenarios with prepare/start and actual combat observations. Original world props and owner restoration remain isolated. | `Practice-Pressure/`, `Practice-Sustained-Fix/`, `Practice-Prop-Isolation/` |
| Planning3 | Per-character B mechanism knowledge costs four once; each item retains its selected variant. Migration uses confirmed owned/pending/recovered evidence and transactional rollback. | `MechanicKnowledge/` |
| Planning4 | Explicit replacement of one preset slot preserves other settings. Unknown legacy metadata remains unknown per slot; referenced sales disclose preset impacts. | `PresetReplacement/`, `PresetUnknownFix/` |
| Planning5 | Explicit third-room branch after room two; only the chosen layout is generated. Five-room reward structure is preserved. Branch confirmation and same-condition retry use one save preflight each. | `Expedition/` |
| H01 | Actual accepted voluntary target motion drives the ordinary gait; knockback is excluded. | `H01/` |
| H02 | Compact combat HUD, immutable pinned A and repeated current B, fixed configuration and actual-duration results. Start/finish buttons preserve at least 48 physical pixels in tested scale/DPI cases. Ratio wording explicitly describes casts that caused enemy health loss. | `H02/`, `H02-Pixel-Followup/`, `ActionSettlement/` |
| H03 | Distinct chest emblems, truthful probability/collection language and actual receipt presentation preserve old receipts. | `H03/` |
| H04 | Status overlays follow model body anchors while grounded markers remain separate. | `H04/` |
| H05 | One outer opportunity seal and true-duration arc; inner text reports inability to act. Touch hints consume input without world fallthrough; desktop ready borders remain. | `H05/`, `Desktop-Opportunity-Dedup/` |
| H06 | Consistent fan/single-arrow terminology and distinct actual impact versus consumed poison-stack explosion feedback. | `H06/` |

## Review corrections

Practice completion now seals at the end of the current synchronous combat action. The last hit's actual clipped energy restoration and outer mechanism callbacks are recorded. Guard retaliation may clear enemies before the same incoming hit finishes; death takes priority if that hit kills the player, without adding elapsed time. Later input, damage and delayed sequence events are rejected. Ordinary completed-mode companions remain present. The existing multi-target interrupt batch keeps its original resolution boundary.

All first-run failures are retained in `Frozen-V2-Failed/`: 267 checks, 253 passed, 14 failed. They exposed obsolete branch-selection fixture flows, missing extracted-host/source dependencies and stale source contracts. Repairs retain original assertions and negative controls. That failed report is not acceptance of later source. V1 was abandoned before running a full suite.

## Delivery boundary

Windows and iOS use the same shared blobs; Android retains synchronized source without a new repository or login changes. Existing GUIDs and platform-specific configuration are preserved. No credentials, permissions, paid assets, or external messaging were changed. No merge was performed; parent review owns merging. The known PR GraphQL access denial was not retried; exact pushed review branches are the review handoff.

Managed production-path checks use explicit engine boundaries. Pinned Unity API compilation is not a Unity Editor/player build, real JsonUtility, physics, shader/GPU or device test. The selected environment has Blender 4.3.2 but no usable Unity Editor. No new Unity screenshots or MP4 are claimed, and no Blender preview is substituted for engine acceptance. Engine/device visual, typography, input, occlusion and performance acceptance remains outstanding.

## Final immutable validation

Windows source: `8ca0e530d9645e042ff4116234960d98a36203fc`. Equivalent iOS source: `b602809ef054586d78f81f0ded966148cc6dcb41`. Full suite: **268/268 passed**, 20261003T164551943650Z through 2026-10-03T17:05:11.204447+00:00. Explicit Windows (`UNITY_STANDALONE;UNITY_STANDALONE_WIN`), iOS (`UNITY_IOS`) and Android (`UNITY_ANDROID`) API builds all passed against pinned Unity 2021.3.33 references using .NET 8.0.425.

All **7,020 tracked input files**, **204 pinned reference files**, and SDK executable stayed unchanged. The platform audit compared **1836 shared files**; 471 metadata files were checked, existing GUIDs retained, and the existing font metadata platform difference preserved. Android synchronized source remains at `/workspace/scratch/planning-round3/android-source` without a repository.

See `Final-Frozen/validation-summary.json`, `Final-Frozen/cloud/report.json`, all adjacent raw logs, `Final-Frozen/input-manifest.json`, `Final-Frozen/input-stability.json`, and `Final-Frozen/platform-sync-and-guid-audit.json`. This final delivery adds only validation evidence after the source heads above; production assets, tests and tools are byte-identical to the tested freeze. The exact pushed delivery heads are reported separately because a commit cannot contain its own hash.
