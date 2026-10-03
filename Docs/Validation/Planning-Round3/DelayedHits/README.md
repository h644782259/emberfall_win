# Round 3 — lifetime-owned first-hit receipts

Baseline Windows `fce5efc4361614aed3eca070ed2574b7d7204a8e`.

## Runtime change

The numeric maximum-cast gate is removed from interruption and offense-core qualification. A cast has one `CastFirstHitReceipt`: one core-first-contact bit and a set of control-policy target identities. Thus 102 nova may arrive first and consume its own qualification; 101 delayed meteor's first eligible contact can still interrupt a later Boss warning. The same cast/target cannot retry on a subsequent warning, and first core contact during cooldown is consumed before checking that cooldown.

`PlayerController.NewCastId` issues a receipt in the actor's current epoch. `CaptureCastReceipt` is lookup-only: an old impact never mints a new receipt in the current epoch. `RegisterSkillHit` also requires the actual current session owner. Enemy interruption obtains the existing source receipt after its current-owner/living/LOS checks. Scope replacement invalidates retained old receipts, including transitions which only increment CombatEpoch. IDs are never recycled within the registry scope.

All friendly projectiles (including the separate BasicShot constructor), CombatArea, AdvancedSkillSequence and deferred SummonerSpell hold explicit strong leases. The actual Spawn/Friendly construction binds the shared receipt; complete OnDisable bodies release idempotently, with OnDestroy fallback where applicable. Child effects retain the same receipt before their parent finishes. The actor also holds bounded references for the newest issued cast, synchronous skill execution and the guard cast that may produce fields later. Epoch transitions invalidate these references through the lookup/issuance epoch gate. Existing producer owner/epoch/paused checks remain unchanged.

Memory is explicitly bounded: at most 256 live indexed casts per actor and 256 distinct interrupt targets per cast. No LRU or elapsed-time eviction occurs. Completed producer leases are reclaimable without waiting for GC. A full live table refuses qualification for a new cast; it never evicts an active old cast, recreates an old missing ID, changes damage, or touches skill energy/CD. These capacity limits are defensive saturation behavior, not a claim that a 257th concurrent cast receives its core/control proc. The old integer rule-only APIs remain for existing standalone consumers, using a bounded, non-evicting history; real actors exclusively use receipt overloads.

Offense `ComboDuration=6f` is now public for presentation. Damage coefficients, core internal cooldowns, skill timings and Boss recovery values are unchanged.

## Verified

- `runtime.log`: 30,284 assertions on actual actor NewCastId/RegisterSkillHit/Capture partial, actual Enemy.TrySkillInterrupt, actual core/control policy and registry. Includes reverse-order 101/102 hits, duplicate casts/targets, cooldown-first-contact consumption, owner/epoch rejection, death, 30,000 later cast pressure for both registry and actual actor, bounded saturation and no reissue. Compiled exact old max-ID gates fail their intended reverse-first-hit assertions.
- `lifetimes.log`: 14 assertions on real acquisition statements and complete producer OnDisable bodies, using engine-only construction shims. Covers strong retention across GC, immediate final-lease retirement, repeated disable and sequence-to-area handoff. Missing-release mutant fails. Also replays the runtime suite.
- `api-compile.log`: all runtime C# files compile against pinned UnityEngine 2021.3.33 APIs with zero warnings/errors. This is the default managed API branch, not three device builds.
- `guids.log`: authored asset/meta GUID audit passes.
- `source-*.log`: existing run-combat, large-boss and progression contracts updated for receipt-based qualification.

The teleport/entry/death/world-retirement epoch boundary is represented in the managed actor fixture; full Unity transitions, physics, coroutine scheduling and rendering were not executed. Unity is unavailable for this task. No engine or device acceptance is claimed.

## Integration and consumer audit

`consumer-audit.json` lists new APIs, all candidate explicit-source Python fixtures, fake actor consumers and runner requirements. The new shared `Core/CastFirstHitReceipt.cs` must accompany standalone compilations of MasteryCoreRuntime/EnemyControlPolicy and deferred producers. Unrelated fake actor/OnDisable fixtures need neutral receipt boundaries or the real partial when qualification itself is tested. The integrator owns those shared fixture registrations and the complete frozen suite; they have not been silently claimed passing here.

Register `Tests/DelayedCastFirstHitTests.py` and `Tests/DelayedCastProducerLifetimeTests.py`, passing the normal dotnet path. The new tests do not require Unity or external feeds. No ZIP, Library operation, PR or push was performed.

Follow-up consumer adaptation is complete: see Compatibility/README.md and Compatibility/results.json for 47 passing managed suites and repaired existing source contracts. Root owns the final combined full run and the separately edited OpportunityChannelsRound2 fixture.
