# Frozen v2 venom-visual dependency repair

Base d0378da2bfe6f7ebfd138e00c9e3b7eafe44dda5. The frozen failure is copied byte-for-byte from validation-v2/Tests/TestResults/Cloud-Latest/venom-visual.log.

VenomVisualProductionTests inherits its Unity/actor boundary prefix from AuthoredProjectileProductionTests. Delayed-cast integration added the real CastFirstHitReceipt type to that inherited actor boundary, but this runner's explicitly enumerated compilation sources omitted Core/CastFirstHitReceipt.cs. The compiler therefore failed before any visual assertion executed.

The single runner change adds the actual production source to its existing compilation list. No runtime code, boundary API, assertion or negative mutation changes. The full production visual component is still compiled and executed. The 13 lifecycle/contact assertions pass; both compiled mutations (physical nick incorrectly displaying three consumed seeds, and outward expansion) still fail at their intended assertions, so the runner exits successfully only after rejecting both.

Command: python3 Tests/VenomVisualProductionTests.py /workspace/shared/emberfall-tools/dotnet/dotnet

Raw output: venom-visual-passed.log. This is a managed fixture compile/run, not Unity rendering or device validation. Root and frozen worktrees were not modified.
