# Delayed-hit API consumer compatibility

47 managed suites passed: 43 production-host Python suites and four existing registered C# rule suites. `results.json` maps each suite to its final raw log; logs retain compiled historical negative-control failures where those suites deliberately expect failure. These checks do not execute Unity, render frames, run engine physics, or build a player.

The shared runner closes the pure `CastFirstHitReceipt.cs` source dependency when selected source consumes it. Explicit temporary-project scripts use `Tests/CastReceiptFixtureSources.py` (ChestTrial's existing explicit core list includes it directly). The mobile/targeting/healing hosts compile the actual `PlayerController.CastReceipts` partial and actual `NewCastId`, without fake issuance or retention. Chapter interrupt tests explicitly issue known casts before calling the actual enemy method; contact only looks up the receipt. The core payoff host also explicitly issues before actual area contact. Unrelated art/guard/hostile-bolt projections keep clearly neutral receipt fields or methods; lifetime correctness remains covered by the dedicated Round3 production receipt suites.

`OpportunityChannelsRound2Tests.py` is owned by the root integration task and was intentionally not edited here. Root also owns the final frozen all-suite run. The root's simultaneous opportunity-input edits to MobilePinnedTargetProductionTests must be preserved when integrating this commit.

Additional source contracts now follow their production ownership:

- BalanceIntegration: actual actor calls `SettleMasteryCombo`, whose partial calls `masteryCore.BasicHit`; the other three distinct triggers remain required (32 checks).
- FilledVfx: actual pooled generation-guarded lease eviction callback plus per-effect cap and immediate OnDisable release (18 checks).
- ProgressionGrowth: actual OpenDungeonChest uses the saved Profile tier, rejects a stale pending roll tier, and migration bounds that tier (19 checks).
- CombatTiming: area and deferred summon OnDisable release the cast receipt and clear pending targets (29 checks).

The first three contracts already failed on `fce5efc4361614aed3eca070ed2574b7d7204a8e`; `*-baseline.log` executes that baseline's original scripts against its original production files and reproduces each exact obsolete assertion. `*-fixed.log` records the repaired current contracts. Production code was not altered to satisfy these legacy assertions.

`attempt1` records initial dependency failures and the first invocation's missing writable DOTNET_CLI_HOME. `attempt2` and `attempt3` retain subsequent adaptation failures and successes. They are historical attempts, not the final result. The final manifest chooses only the successful latest run for each suite. Commands use `/workspace/shared/emberfall-tools/dotnet/dotnet`; scripts requiring environment lookup also receive DOTNET and a writable temporary DOTNET_CLI_HOME. The four registered subsets use unchanged suite source selection and `write_project` from Tools/cloud-validation.py.
