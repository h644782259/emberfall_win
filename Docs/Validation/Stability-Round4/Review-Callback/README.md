# Current per-subscriber isolation and defensive receipt recovery

This review adapts the existing arena and ground-loot callback tests to current production. It does not change production or suite registration.

Commands use `/workspace/shared/emberfall-tools/dotnet/dotnet` as the first argument:

- `python3 Tests/ArenaRewardExceptionTests.py <dotnet>`: 370 normal-path assertions, 368 explicitly labelled defensive-path assertions. Normal callback faults are warned, later listeners run, actual presentation helpers read saved reward details, and payout/logging do not repeat. Precommit faults, ordinary write failure, and replaced-run ownership checks remain. Four compiled negative controls reject removal of normal isolation, finally ticket completion, durable receipt reconciliation, or original-run ownership.
- `python3 Tests/GroundLootCallbackRecoveryTests.py <dotnet>`: 24 normal-path assertions, 22 defensive-path assertions. Checks item and auto-sale, persisted reload, later subscribers, warning visibility, precommit and actual write-failure retry, and callback role replacement. Two compiled controls reject removal of normal isolation and omission of committed pickup receipt reconciliation.

Defensive path definition: only the isolated temporary copy of ProgressionService has RaiseChanged/RaiseLeveledUp replaced with direct invocation, and the test receives an explicit `defense` argument. This models an exception after commit but before transaction return. It is **not** a claim that current normal observer exceptions propagate. This makes the old defensive recovery oracles reachable without removing or weakening them. Normal-path tests run the unmodified production isolation and fail when it is removed.

`arena-isolation-adapted.log` and `loot-isolation-adapted.log` preserve all normal, defensive, and negative-control output. Existing Rewards/GroundLoot before and historical after logs were not changed. The independent OnEnemyKilled suite was not rerun here; parent owns Review-EnemyKill evidence.

Only managed actual source plus explicit JSON/engine/UI recording boundaries ran. No Unity engine, GUI, physics or device acceptance claim. No commit or push by this agent.
