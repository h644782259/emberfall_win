# Round4 practice exception isolation

Only edited in /workspace/scratch/stability-round4-win:
- Assets/Scripts/Core/GameSession.Practice.cs
- Tests/CampPracticeSessionBoundary.cs
- Tests/CampPracticeSessionProductionTests.py

No commit, push, full-suite run, shared GameBootstrap/GameUI/PlayerController change, or engine-validation claim.

Concrete managed fault reproduction: while practice is active, inject an exception from scene.GetRootGameObjects during EndPractice. Existing finally restores the formal Player/Progression, but temporary Player and enemies stay active. The new assertion fails against the original implementation in exit-snapshot-before.log. This is an exception-recovery defect demonstrated at an explicit managed scene boundary, not an observed Unity engine failure or proof of an actual disk corruption incident.

Minimal fix: mark successful root retirement. Only if it failed, retire the separately owned temporary Player and enemy list before restoring formal ownership. Each actor cleanup has its own catch; Destroy is attempted even if SetActive throws. The original root-cleanup exception still propagates, so the actual SaveBeforeLeaving/CanQuitSafely chain refuses that save/quit attempt. No original player or original enemy list can enter this fallback, including incomplete BeginPractice construction. Normal cleanup flow is unchanged.

Evidence:
- exit-snapshot-before.log: original production lifecycle fails exactly at 'failed exit snapshot cannot leave temporary combat actors active beside formal owner'.
- exit-snapshot-after.log: 142 actual lifecycle assertions and 11 compiled negative controls pass. Added checks cover failed exit enumeration, failed OS-close cleanup/save veto and retry, per-root failure, failure inside fallback deactivation with guaranteed Destroy and continued cleanup, duplicate EndPractice, and retained original references. Existing creation failure, unsafe admission, prepare/start/death/background/quit, telemetry, real loot recipient guards, baseline freezing and random/vitals restoration checks remain.
- wiring.log: existing practice wiring audit passes (source audit only).

Run: python3 Tests/CampPracticeSessionProductionTests.py /workspace/shared/emberfall-tools/dotnet/dotnet
All tests use declared Unity and persistence recipient boundaries. No Unity, device, frame rendering or physics execution was performed. Parent owns final full-source compilation and integrated validation.

## Entry producer and companion-clock follow-up (final)

Final lifecycle evidence: `bond-entry-after-final.log` — **186 production lifecycle assertions + 17 compiled negative controls**, exit 0. This supersedes the earlier 142/160 checkpoints. The ordinary fixture warnings are unused boundary events/fields, not compiler errors.

- `producer-entry-before.log`: actual BeginPractice admitted an active CombatArea, executing the unchanged production OnDisable; the real CastFirstHitReceipt and ScheduledImpactBatch state was lost despite subsequent reactivation. CombatArea, AdvancedSkillSequence and SummonerSpell now each block entry before any suspension. The suite extracts all three real OnDisable bodies; only aura/arrow visual recipients are declared boundary doubles. Inactive producers remain legal and preserve inactivity. Three separate compiled guard-removal controls reproduce the lost-state assertion. `producer-entry-after.log` records an intermediate negative-oracle ordering mismatch, fixed by moving the additional cases after the existing restore assertion, without weakening any old assertion; `producer-entry-after-final.log` is the passing checkpoint.
- `bond-entry-before.log`: actual original-owner BondState/State/OnPerfectDodge/CommandOpportunityRemaining plus real CompanionRules show a 10-second trial ages the original 16-second opportunity and 3-second protection. Entry now consults core agent's readonly HasPracticeTimedState: wait while a command opportunity, protection, cooperation mark or cooperation cooldown remains. No rebasing, consumption, state rebuild or lifetime restoration occurs. Cases exercise 10/60 seconds, unconsumed and consumed command with remaining protection, actual cooperation marks/cooldown, no-consumption queries, and lawful expired-window admission. Three compiled negatives independently omit the whole guard, protection check and cooperation check.
- CompanionRules.cs and SummonedCompanion.cs were owned/edited/tested by the core agent; this agent only wires their readonly API in PracticeEntrySafe and adds the original-owner production host evidence. The core agent separately reported 313 tracker/command boundary assertions.

Compatibility scope: only CampPracticeSessionProductionTests.py previously compiled the entire GameSession.Practice.cs and needed the three producer types plus real companion source extraction. PracticeLocomotion/PracticePressure runners extract unrelated methods and do not consume EntrySafe. Venom agent's new PracticePotionInputProductionTests.py was notified and owns its independent current-source rerun. CampPracticeSessionBoundary includes venom's input API additions and root's HasCommittedWorldLoot unexpected-use boundary; those are preserved. No other agent's files were edited.

Final files owned here remain the same three; shared Practice.cs also contains venom agent's independent potion-input hunks, which this agent did not author. No commit/push/full-suite was performed. Evidence is managed production code execution with explicit engine recipients, not Unity play-mode or device validation.
