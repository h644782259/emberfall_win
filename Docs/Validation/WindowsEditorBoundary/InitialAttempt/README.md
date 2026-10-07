# Windows Editor assembly access fix

Baseline: Windows main `e1397a26049e624d98daa0b20159526e045f6076` fetched before branching. GroundLootValidation is copied byte-for-byte from independently reviewed Android `f3901ea17f69dbefcb4f121c7560767eef4e858f` (equivalent iOS `d8f091cccca54649aa8fe733aa53b1743536aeec`). Only the two CombatEpoch reads and a private reflection helper change. Runtime, packages, gameplay, fonts and other platform repositories remain unchanged.

`Tests/EditorAssemblyBoundaryTests.py` builds all actual runtime sources into a DLL, then compiles the entire actual GroundLootValidation into a separate DLL referencing it. Windows/editor compilation symbols are enabled. Only UnityEditor timing/session APIs use explicit tiny substitutes. A negative control replaces the two reads with the old code and must fail CS1061/CS0122. The runtime property remains internal; the validator remains intact.

Three existing relevant regressions cover ground-loot callback recovery, world-loot receipt/room-transition ordering and room failure evidence. Logs include intentional failing negative controls; process exit zero means each script verified the expected failures. Source inputs are hashed before and after all checks. Fixed UnityEngine 2021.3.33 references are verified against their pinned package.

Run: `python3 Docs/Validation/WindowsEditorBoundary/run.py /path/to/dotnet`. Verify frozen evidence: `python3 Docs/Validation/WindowsEditorBoundary/verify.py`.

This environment lacks Unity Editor and native build tooling. These are managed/API boundary checks, not actual Unity 6000.6.3f1 import, full Editor compilation, player build or device acceptance. No packages or videos produced.
