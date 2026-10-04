# Stability round 4

Scope: existing save/reward transactions, character build state, practice isolation, delayed combat lifetime and mobile input. No new gameplay, balance changes, asset regeneration, PR creation or Library upload.

Baseline: Windows `5b2e21da0af6aff3d7a131c234237665015709bd`; iOS `a88df71c785d5c59beb2e0ea12393005c454025b`. Shared runtime sources matched before this round. Both use branch `codex/stability-round4`.

## Confirmed managed reproductions and fixes

- A failed chest draw lost its frozen choice/result when the same character was saved as a new slot. The snapshot now inherits that pending context only after the new slot write commits; a new character does not inherit it.
- Arena reward observers throwing after a successful write stranded the reserved ticket. The host now completes/releases the original ticket in `finally`; exceptions reconcile the original slot's durable reward receipt, without posting the old result to a replacement run.
- Ground-loot observer exceptions left the collection lock/pickup pending after its item or auto-sale had committed. The adapter always releases the lock, retires only accepted/committed identities, and preserves pre-commit failures for retry. The observer exception remains visible to the caller.
- Failed mobile pause-menu travel resumed combat. Camp travel now remains paused until a successful transition; rejected or exceptional dungeon selection restores the prior pause.
- Practice hotbar right-click and empty-slot navigation could open a hidden configuration panel and block play. These practice-only navigation/drag gestures are rejected while normal skill and potion actions remain available.
- Practice input skipped F/mobile potion consumption. The practice path drains input under prepare/live/blocked/terminal guards; F plus touch in one frame consumes once, and begin/start/end clear stale potion requests. Only the temporary inventory/health changes.
- Practice shutdown with injected scene enumeration failure restored formal ownership while leaving temporary actors active. Explicit temporary actor cleanup now runs before restoring ownership even on that failure path; a failed exit still vetoes the immediate save.
- Practice admission previously disabled live persistent cast producers, whose actual `OnDisable` irreversibly retired receipts/targets. Admission now refuses active projectile, area, advanced sequence and summon-spell producers before suspending roots.

- Practice advanced the shared clock while original companion opportunity/protection/cooperation windows were suspended. Admission now observes the existing owner/epoch state and refuses entry until those windows/cooldown expire, without consuming, extending or recreating them.

## Evidence and limits

`Core/`, `Rewards/`, `GroundLoot/`, `UI/`, `Combat/` and `Practice/` contain targeted raw logs and explicit boundaries. Negative controls intentionally fail; a failed mutation is not a failed positive test. New transaction/input tests execute extracted production adapters with actual state/service implementations. Engine scene, input, audio and JSON boundaries are managed doubles; these are not Unity/device execution.

The core audit found no new confirmed production defect in variant knowledge, single-slot repair, point budgets or current-resource preservation. It expanded boundary coverage instead. The combat audit retained existing owner/epoch behavior: a callback replacing the owner without advancing epoch has a synthetic probe, but current gameplay reachability was not established and no speculative combat change was made.

`WorldLootReceiptSourceTests.py` was previously unregistered and referenced the old unsplit room transition. Its ordering assertions now follow the public preflight into the existing transition helper, and its collection assertion explicitly requires receipt reconciliation in `finally`. No safety assertion was removed to hide a production failure.

The final frozen suite and Windows/iOS/Android conditional API compilation results are recorded separately after completion. Compile-only references are the cached pinned UnityEngine 2021.3.33 assemblies. Unity Editor is unavailable here; API compatibility checks do not validate engine behavior, rendering, device input, real JsonUtility, app packaging or performance.

## Final frozen validation

The second full suite passed all 277 checks, including Windows, iOS and Android conditional API compilation, with no source changes during the run. `Final-Suite/` preserves every raw check log, full console output and the report with input hashes. `final-tree-verification.json` verifies those hashes against both final worktrees and the 769-file shared freeze. The independent iOS mirror API report also covers all 296 runtime files with hashes identical to the final tree; the later correction changed only a test fixture.

`First-Attempt/` retains the failed restricted-healing fixture log and first full report. That attempt is not acceptance: its fixture lacked the new practice guard dependency and one test file changed while it ran. The corrected fixture throws if a formal-only test accesses practice state; existing assertions remain in place.

Additional reviewed boundaries: companion admission includes the existing cooperation `nextProc` cooldown, with exact-expiry and repeated-read tests (`Core/CompanionClockGuard.md` and the practice lifecycle suite). Ground-loot recovery uses post-write receipts for both inventory and automatic sale, with durable reload and duplicate-retry checks (`GroundLoot/`).

## Independent review followup — frozen validation passed

Review reproduced two omissions in the initial accepted tree. Four settlement hosts could lose or misstate their actual reward presentation after observer failure or observer balance changes; the actual enemy-death host could skip loot/death/finalization after the same post-write observer failure.

Reward transactions now atomically persist optional, identity-bound actual gold/experience/material amounts after caps, plus chapter first-completion/unlock/shared-tier/core facts. All four settlement hosts read those details rather than infer grants from mutable post-callback balances. Existing ID-only receipts remain deduplicated and explicitly report unavailable details; they are never re-granted or displayed as a known zero. This is an additive save field change with no reward or growth-rule changes.

Changed and LeveledUp notifications dispatch each captured subscriber independently, logging individual exceptions while allowing later subscribers and the host completion tail to run. The storage operation is outside that catch: candidate-write failures still return false; kill progress retains its existing live-state/LastError/Save-retry behavior. The earlier reservation and world-loot receipt guards remain as defensive recovery for exceptions outside notification dispatch.

Review evidence: `Core/RewardPresentation/`, `Review-EnemyKill/`, `Review-Callback/`, and `Review-Recap/`. Prior `Final-Suite/` and its verification describe the initial published revision, not this followup. The followup frozen full suite and mirror compilation are complete; the results and input-hash verification are recorded below. Unity/device acceptance remains unavailable.

Latest followup acceptance: `Review-Final-Suite/` passed all 279 checks, including all three platform API compilations, with zero input changes. `review-final-tree-verification.json` matches the suite inputs and 773 shared frozen files against both final trees; `Review-iOS-Mirror-API/` independently compiles all 296 final runtime files under each platform define. Earlier reports and failure logs remain available. No Unity/device execution occurred.
