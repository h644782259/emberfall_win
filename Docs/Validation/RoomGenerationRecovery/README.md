# Room generation recovery — review candidate

Fixes two reproduced defects: independent crystal enemy searches could select safe points less than 2.5 m apart; the supply corridor outer escort overlapped authored blocking rubble. The second crystal search now reserves the first position; the escort moves locally while preserving enemy count, role, rewards and seed behavior.

Seal corridor failure from the reported Unity run is **not yet reproduced or declared fixed**. Stage/seed/room/layout/branch/enemy-slot diagnostics and exception stacks now survive failure; the result snapshot retains the concise cause and success notices do not overwrite failure. No objective bypass or reward grant was added.

The branch and side regressions execute current `BuildLinkedRoom`, `BuildTacticalRoom`, `BuildBreakablePockets` and `Pillar` methods with real navigation. Rendering, prop objects, enemy models and disk IO remain explicit managed boundaries. This does not execute the Unity engine or prove device behavior.

Targeted commands (set `DOTNET_CLI_HOME` to a writable temporary directory):

```sh
python3 Tests/RoomBranchProductionTests.py /path/to/dotnet
python3 Tests/RoomSideBranchProductionTests.py /path/to/dotnet
python3 Tests/RoomFreeSealHostTests.py /path/to/dotnet
python3 Tests/RoomFirstChoiceProductionTests.py /path/to/dotnet
python3 Tests/RoomFailureEvidenceTests.py /path/to/dotnet
python3 Tests/RoomTacticalLosProductionTests.py /path/to/dotnet
python3 Tests/SideEventSourceTests.py
DOTNET=/path/to/dotnet python3 Tests/SideEventProductionTests.py
```

Expected exception output in negative controls is intentional; the runner requires the specific assertion, not a compilation error. Full managed suites were also started and have failures; they are not certified green. The reference compile uses pinned UnityEngine 2021.3.33 APIs, not the project's Unity 6000.6.3f1 editor/native toolchain.

Required native follow-up: tier 15 limited healing, direct Seal versus optional crystal then Seal; live/cleared/repeated/cancelled/retried entry, both mirror layouts, and actual enemy/art construction. Capture the new stage and Unity exception stack for any remaining failure. Draft only; independent review and native verification are still outstanding.

## Initial full run (diagnostic only)

287 checks: 252 passed, 35 failed. Source changed while this initial suite ran, so it is not a frozen acceptance result. `initial-full/report.json` preserves its source hashes and changed-file list. The final frozen result is recorded below.

Each unchanged failure was replayed on this platform main; `initial-full/baseline-comparison.json` and paired logs record the same diagnostic signatures. Windows room-failure-evidence was the only introduced fixture failure, repaired and passing in the final targeted run. No unrelated main gameplay/UI changes were replaced to make tests pass.

## Frozen full validation

Completed 2026-10-07T10:56:51.832139+00:00: **253/287 passed**, 34 failed; `sourceChangedDuringRun` is empty. Every final failure matches the archived main baseline: **True**. See `frozen-full/report.json` and `frozen-full/baseline-comparison.json`. This remains managed/reference-API evidence, not Unity/native/device acceptance.

## Independent review follow-up

Generation failures now take priority over ineffective ember/frost advice. The short cause and expandable readable detail use the same font height calculation as their card and scroll extent; seeds and stack traces remain developer diagnostics. First-entry generation failure cannot be overwritten by a success notice.

The frozen-full result above is for the **pre-review source** and is not a full validation of this follow-up. All nine affected suites and the platform reference-API compile passed on isolated commit `1d595308efe2e3d964d7cc127d725c86b7697a94`; the integrated runtime source hashes match exactly. See `review-followup/report.json` and logs. Checks include nonzero ineffective mechanisms, initial entry/transition/retry failures and mutation controls, repeated detail toggling, narrow widths, increased text scale and scroll containment. Font/GUI tests use managed boundaries; Unity/device rendering remains unverified.
