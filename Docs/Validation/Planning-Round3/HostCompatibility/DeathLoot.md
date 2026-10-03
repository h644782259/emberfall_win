# Death-loot fixture compatibility

Base d0378da. The frozen v2 failure was a missing PracticeRecord member in the ordinary-run GameSession test double. Actual OnPlayerDied now references PracticeRecord.PlayerDefeated inside its PracticeActive branch. This host deliberately keeps PracticeActive false and tests ordinary deaths, failed writes, retained pickups, Respawn retry and durable receipt identity.

The fixture now supplies an explicit practice boundary whose getter and defeat method both throw. Any accidental practice access in the ordinary-run cases therefore fails instead of silently succeeding. No production code, retry policy or existing test assertions changed.

Command: `python3 Tests/DeathLootPersistenceProductionTests.py /workspace/shared/emberfall-tools/dotnet/dotnet` (exit 0).

- 184 loot retry/receipt assertions pass.
- 27 actual death/pending/Respawn adapter and durable file-write assertions pass.
- The compiled old direct-death-Save negative control still fails at the intended lost-pending-identity oracle.

`death-loot-original-failure.log` preserves the frozen v2 failure. `death-loot-fixed.log` records the targeted pass. This is managed production-host verification, not Unity or device execution. The frozen tree was not modified.
