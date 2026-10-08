# Full Unity progression validation — test revision 2026-10-08

Production scripts and platform behavior are unchanged from the installed unified candidate.

## Budget evidence

Both original candidate baselines already use `B(L)=max(1,L-1)` and `G(L)=B(L)-B(L-1)` for L>1 (zero at L=1). New profiles own root skill rank 1, spending the initial budget point. Thus B(1)=B(2)=1, G(2)=0; G(3)..G(100)=1. At level 100, total budget is 99, spent is 1 and unspent is 98. After all ten skills reach rank 3, spent is 30, unspent 69 and there are 29 new learning transactions. The old Unity test incorrectly expected 99 unspent and 30 new transactions.

The revised test independently checks the explicit formula at all 100 levels, XP and save/load at 1/2/3/99/100, and repeated capped XP. Existing rank, prerequisite, save, preset and equipment assertions remain active. Missing-parent tests explicitly create a missing-root fixture rather than assuming fresh characters lack root rank 1.

Other stale full-suite fixtures now use 256-slot direct visible overflow without sale, the owned post-normalization item, the actual starting skill in hotbar tests, and platform-specific affordability/clamped energy regeneration. Windows boss rarity fixture explicitly selects tier 1 (tier 0 is not the rare reward band).

## Actual Unity entry on Mac

Use Unity 2021.3 and the intended project checkout. The complete entry is:

```sh
"$UNITY_EXECUTABLE" -batchmode -nographics -quit -projectPath "$PROJECT_DIRECTORY" -executeMethod Emberfall.Editor.ProgressionValidation.ValidateFull -logFile "$VALIDATION_LOG"
```

Menu: `Emberfall/Validate Full Progression and Skills`.

Report: `<project>/Tests/TestResults/unity-progression-<unique-id>/validation-report.json`. Require `status=PASS` and verify `passedStages` contains all four budget/class/tree stages plus legacy JSON, damaged JSON, backup, runtime, portable transfer, upgrades, legacy equipment, loot, hotbar, consumables, multiple saves and presets. Do not call only `Validate`: Windows version 0.4.0 intentionally uses its existing short release validation path there. `ValidateFull` always executes the full suite. All save fixtures are under this unique project-local output directory; it does not open or write user saves, scenes, devices, or game UI.

## Managed preflight (not actual Unity)

```sh
python3 Tests/UnityProgressionManagedPreflight.py "$DOTNET_EXECUTABLE" "$PREFLIGHT_OUTPUT_DIRECTORY"
```

Both platforms completed all 2404 assertions with a managed boundary in this environment. Compiled temporary negative controls on both platforms rejected an off-by-one budget at level 2 and removed starting-root ownership at level 1 (both the initializer and its production repair were removed in that latter mutant). No checked-out production source was mutated for these controls. Real Unity/JsonUtility execution of the revised full suite remains for the Mac coordinator. The earlier real JsonUtility migration 7/7 result is parent-provided, not this preflight result.
