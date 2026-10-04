# Follow-up: companion absolute-clock entry guard

This follow-up was requested after the core allocation audit. SummonedCompanion stores command/protection and cooperation deadlines against global Time.time. Deactivating original actor roots does not pause that clock while practice actors run. The practice agent owns the failing actual BondState/command replay and entry integration; this change adds only the narrow read-only observation API.

- CompanionCooperationTracker.HasPending(now) checks an unexpired proc cooldown or a still-valid target mark. The cooperation mark boundary is inclusive, matching RegisterHit; proc cooldown expires at equality.
- SummonedCompanion.HasPracticeTimedState(owner) uses bonds.TryGetValue and matching CombatEpoch. It does not call State, create a bond, consume a command, reset a tracker or alter a deadline. It includes command Remaining, IsProtected and cooperation HasPending. Owners without existing timed state are not rejected merely for being summoners.
- PracticeEntrySafe integration and real BondState owner/epoch/10s/60s scenarios belong to the practice agent. No clock rebasing or restoration of spent opportunities/cooldowns is performed.

Targeted command: `python3 Tests/CompanionPracticeTimedStateTests.py /workspace/shared/emberfall-tools/dotnet/dotnet` (exit 0). `companion-clock-boundaries.log` records 313 actual rule assertions: empty tracker permitted, inclusive 1.5s marks, repeated observations leave marks consumable, exact 3s cooldown deadline, expired marks not revived, explicit reset, command consumption leaving 3s protection, and no restoration of spent opportunities. Repeated queries cannot extend deadlines.

Scope is managed actual rule execution. It does not validate Unity deactivation or real frame scheduling. No commit/push/full-suite run by this agent.
