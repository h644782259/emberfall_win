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
