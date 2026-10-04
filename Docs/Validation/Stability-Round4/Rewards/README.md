# Round4 save/reward stability audit

Base: 5b2e21d. Shared authorized tree; no commit, push, full-suite, engine run, or platform compilation performed by this agent.

## Concrete findings and minimal fixes

1. A failed chest draw was retained on same-slot/staged loads but lost on `SaveAsNewSlot`. The copied unopened chest could select another box and reroll in the same process. `before.log` demonstrates the real failed choice assertion before production changes. `ProgressionService.CreateSlot` now copies the immutable failed draw context to the new path only after a successful save-as write, validates it with existing clear/tier guards, and never carries it to a new character. Original slot remains independent. No save schema, reward amount, or balance change.
2. `TrySettleArenaReward` reserved its state-machine ticket before invoking the real persisted reward transaction. A Changed or LeveledUp exception after persistence stranded that reservation permanently. `arena-before.log` demonstrates the assertion before host changes. The host now captures original run/service/slot/receipt, reconciles exceptional outcomes against an independently loaded durable receipt, completes/releases the original ticket in finally, and avoids publishing the old result to a replaced host. No general event-dispatch suppression was added.

## Exact files owned by this agent

- Assets/Scripts/Core/ProgressionService.cs: CreateSlot only (9 added lines).
- Assets/Scripts/Core/GameSession.Modes.cs: TrySettleArenaReward only.
- Tests/ChestSnapshotRetryTests.cs and .py (new).
- Tests/ArenaRewardExceptionTests.cs and .py (new).
- Tests/AdventureIntegrationSourceTests.py: one exact-source contract updated to require original rewardRun completion in finally.

Other changes visible in the shared tree belong to root/other agents and were not modified here.

## Checks and raw logs

Commands use `/workspace/shared/emberfall-tools/dotnet/dotnet` as first script argument.

- `ChestSnapshotRetryTests.py`: 22 production save-as/snapshot/reload/isolation/capacity assertions; one compiled lost-context negative control. `after.log`.
- `ArenaRewardExceptionTests.py`: 364 assertions (including actual phase spawn/death registration to win real ExpeditionModeState), actual Changed/LeveledUp exceptions, disk receipt verification, precommit exception retry, ordinary disk failure retry, duplicate payout/notification prevention, and callback host replacement. Three compiled controls remove finally release, disable durable reconciliation, or complete the replaced run instead. `arena-after.log`.
- `RewardRevisionTests.py`: 1514 reward + 37 actual SaveSlotTransition + existing chapter/first-clear/load/save/idempotence/growth scenarios and controls. `reward-regression.log`.
- `AdventureIntegrationSourceTests.py`: pass. `adventure-source.log`.
- `arena-fixture-dependency.log`: first temporary fixture compile lacked RunBlessing boundary enum. Added only RiskContract enum boundary; real state/progression/host remain executed. This was before the actual behavioral failure, not passed off as a product regression.

No Unity engine, actual JsonUtility, GUI/rendering, or device acceptance claim. Standalone JSON/filesystem and UI/engine edge fixtures only. The new scripts need root's full-suite registration.
