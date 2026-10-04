# Ground-loot callback recovery

`before.log` executes the unmodified actual collection adapter and service. A one-shot Changed observer throws after the item/auto-sale write; the original adapter leaves the pending lock and pickup active. The assertion fails at `committed callback failure retires pickup and unlocks pending receipt`.

Historical `after.log` contains 15 positive assertions and a compiled negative control removing receipt reconciliation. The adapter now releases its lock in finally, checks the original service receipt (which is issued only after write success, including auto-sales), and retires that exact pending record. That historical test preceded centralized observer isolation; it is not the current normal callback contract. A separate pre-commit invalid-dependency injection restores the dependency before retry and proves the same item remains collectible exactly once.

`death-regression.log` covers existing actual death/respawn persistence plus world-loot receipt tests. The tests use managed serialization and scene/audio boundaries, not Unity runtime.

Current production isolates each Changed/LeveledUp subscriber, logs its fault, and continues later subscribers. Normal collection succeeds after a postcommit observer fault; no exception reaches the adapter. Persistence failure still returns false with LastError and leaves the world item retryable.

Current evidence is [Review-Callback](../Review-Callback/README.md): 24 normal-path assertions verify warning recording, later subscribers, item/auto-sale exactly once, real reload, failed write/retry and callback role replacement. A separately labelled defensive subcase temporarily bypasses observer isolation **only in copied test source** to model a transaction fault after persistence but before return. Its 22 assertions retain exact receipt retirement/recovery, and the original missing-reconciliation negative control remains meaningful. Removing isolation also fails the normal-path assertions. All original before/after logs remain unchanged.
