# Concentrated venom launch fixture dependency

Base 7c15fa162cc32025a524ebb331c9f62f9b672ff9 already includes the separate venom-visual fix. The original frozen v2 failure is preserved byte-for-byte in frozen-v2-failure.log.

ConcentratedVenomLaunchTests inherits the AuthoredProjectileProductionTests fixture prefix and actual projectile construction methods. Delayed-cast integration introduced CastFirstHitReceipt into both the production projectile and inherited player boundary, but this runner's explicit compilation source list omitted its definition. The failure was compilation dependency resolution, before gameplay assertions.

The sole test change adds actual Core/CastFirstHitReceipt.cs to the existing source list. Runtime code, engine boundaries, all 24 cast/factory assertions and the compiled piercing negative remain unchanged. The runner now passes with zero compiler warnings/errors, then deliberately rejects piercing at the original nonpiercing/zero-explosion/no-volley assertion. Both runners importing the AuthoredProjectileProductionTests prefix are now covered: this launch runner and the already repaired VenomVisualProductionTests.

Command: python3 Tests/ConcentratedVenomLaunchTests.py /workspace/shared/emberfall-tools/dotnet/dotnet

Raw passing output is launch-passed.log. This is managed production-method execution with Unity boundaries, not engine/device rendering. No root or frozen worktree was modified.
