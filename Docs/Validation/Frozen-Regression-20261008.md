# Frozen regression and bounded-fashion follow-up — 2026-10-08

Full snapshot: `82595e9dc5ec5cc5a67381e48f2c5fcd73c1dd6e`.
Full run: 297 checks; 279 passed; 18 failed. `sourceChangedDuringRun` is empty. Runtime compile passed against pinned Unity reference DLLs.

## Failures in that full snapshot

- `progression`: also failed on frozen main; inspected failure reason matches baseline.
- `enemy-kill-rewards`: also failed on frozen main; inspected failure reason matches baseline.
- `reforgeselection`: also failed on frozen main; inspected failure reason matches baseline.
- `returning-counter-persistence`: also failed on frozen main; inspected failure reason matches baseline.
- `enemy-status-anchor`: introduced this round; corrected in follow-up below.
- `chest-choice-presentation`: also failed on frozen main; inspected failure reason matches baseline.
- `build-plan-page-geometry`: also failed on frozen main; inspected failure reason matches baseline.
- `scenery-presentation-production`: also failed on frozen main; inspected failure reason matches baseline.
- `fixed-scenery-production`: also failed on frozen main; inspected failure reason matches baseline.
- `fixed-scenery-enabled-integration`: also failed on frozen main; inspected failure reason matches baseline.
- `weapon-contact-production`: introduced this round; corrected in follow-up below.
- `concentrated-venom-production`: also failed on frozen main; inspected failure reason matches baseline.
- `camp-practice-session`: also failed on frozen main; inspected failure reason matches baseline.
- `opportunity-channels-round2`: also failed on frozen main; inspected failure reason matches baseline.
- `camp-practice-production`: also failed on frozen main; inspected failure reason matches baseline.
- `g07-factory-inventory`: also failed on frozen main; inspected failure reason matches baseline.
- `reward-presentation-exceptions`: also failed on frozen main; inspected failure reason matches baseline.
- `practice-potion-input`: also failed on frozen main; inspected failure reason matches baseline.

## Follow-up scope

The full run exposed a duplicate EffectPreferences test boundary and a real >800-triangle loaded weapon budget. The follow-up removes only the duplicate declaration; uses faceted low-poly bow crests, spirit leaves and antlers; and replaces dense sword guard rings with pointed crystals. It preserves the 800-triangle cap, weapon anchors, combat properties and saved identities.

Formal follow-up: **9/9 passed**, with empty `sourceChangedDuringRun`: weaponfashionstructureproduction, enemy-status-anchor, equipment-composition-production, weapon-contact-production, integrated-actor-art-production, authored-actor-modules, actor-silhouette-f1-production, final-body-envelope-production, and the platform runtime compile (zero warnings/errors). Reports are in `20261008-Frozen/follow-up-final.json` and `follow-up-geometry.json`. These are targeted reruns after the full snapshot, not a second all-check run on the follow-up commit. The unified integration candidate still needs its own final validation.

## Execution limits

Managed production-code fixtures and pinned-reference compilation only. No Unity Editor, actual JsonUtility, rendering, physics, Windows build, Xcode build or device run occurred here. The parent coordinates Unity/Xcode and simulator installation against the unified SHA; main remains held for user acceptance.

CI queries on the full-snapshot SHAs returned no commit statuses and no PR-triggered workflow runs. The wrapper filters PR events and its first page; a separate all-Actions API query was denied. This is not a CI pass.
