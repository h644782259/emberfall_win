# Actual OnEnemyKilled callback fault injection

Requested baseline: `6aeef550`; no production edits by this test agent.

New runner: `Tests/EnemyKillCallbackFaultTests.py <dotnet>` and `Tests/EnemyKillCallbackFaultTests.cs`.

`before.log`: full behavioral failure on the pre-fix production source. 24 scenarios = normal dungeon / chapter / arena / room × no fault / Changed throws / LeveledUp throws × successful writes / .tmp directory write failure. Eight no-fault controls pass; sixteen callback-fault scenarios fail `post-grant observer fault must not skip loot/death/ownership tail`.

Before that assertion each scenario executes and verifies:

- Actual full `GameSession.OnEnemyKilled` removes the admitted enemy and actual `GrantEnemyKillReward` adds one kill, exact boss gold, and earned level/XP.
- Calling actual `OnEnemyKilled` again rejects the removed identity and leaves reward unchanged.
- Successful persistence is verified by a separate real ProgressionService LoadSlot.
- A .tmp directory failure leaves the previous durable document and nonempty LastError, while keeping live earned rewards; removal of the obstacle and Save() persist exactly once without invoking the grant again.

The desired post-fix assertions additionally require loot delivery, BeginDeath and transient ownership once; later Changed and level subscribers still run; the appropriate last-enemy chapter/room/arena finalizer or ordinary next-wave scheduler is called exactly once.

Production scope: full OnEnemyKilled method is extracted without alteration. Actual ProgressionService, reward/XP progression, RollLoot, JSON save/load transaction, and AdventureResultPolicy execute. JSON transport uses existing ProgressionTests managed System.Text.Json fixture. Scene math/RNG, enemy BeginDeath, mode-specific finalizer recipients, ground-loot delivery, and coroutine scheduling are explicitly recording boundaries. This test verifies the real host reaches those recipients, not their internal Unity animation, world-drop creation, coroutine or mode reward implementation. No Unity/renderer/physics/device execution claim.

`fixture-alias.log` is an earlier fixture compile failure due to System.Random vs UnityEngine.Random ambiguity; fixed by the same explicit Unity RNG alias used by production. It is not the behavioral regression evidence.

No commit/push/full-suite registration was performed. Parent owns the production subscriber-isolation fix; rerun the new script afterward and preserve its distinct after log.

After centralized observer isolation, all 24 scenarios / 264 assertions pass (`after.log`). This uses the entire production OnEnemyKilled and actual progression/save/load/loot roll. Rendering, actual death animation and mode-finalizer bodies are recording boundaries. Persistence failures remain visible in LastError, keep earned live state and retry Save without a new grant.
