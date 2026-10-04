# Ground-loot callback recovery

`before.log` executes the unmodified actual collection adapter and service. A one-shot Changed observer throws after the item/auto-sale write; the original adapter leaves the pending lock and pickup active. The assertion fails at `committed callback failure retires pickup and unlocks pending receipt`.

`after.log` contains 15 positive assertions and a compiled negative control removing receipt reconciliation. The adapter now releases its lock in finally, checks the original service receipt (which is issued only after write success, including auto-sales), and retires that exact pending record. Exceptions still propagate. A separate pre-commit invalid-dependency injection restores the dependency before retry and proves the same item remains collectible exactly once.

`death-regression.log` covers existing actual death/respawn persistence plus world-loot receipt tests. The tests use managed serialization and scene/audio boundaries, not Unity runtime.
