H02 compact practice HUD and frozen A/B result table

Live practice reuses the existing Windows/mobile hotbar, companion, charge and
targeting helpers. Two 80x48 top actions start/restart and finish. Scenario/remaining
time and four measured stat lines occupy only the reserved top band, leaving the
camera combat view and all ten skill targets clear. Exact typography/input/render
acceptance on Unity and devices remains outstanding.

Ended sessions return to the original draft panel and display a measured three-
column result table: fixed A, current B, damage, DPS over actual elapsed seconds,
energy spent/restored, received damage, effective healing, survival/completion,
supply-break time, kill order, per-skill effective casts / casts and actual mechanism
counts. Skill names are frozen with the originating class, never read from the
current profile. The table has no aggregate score or arrow-hit-rate claim.

The first normally finished surviving record automatically pins A. Later B runs
and ordinary restarts cannot move A; an explicit button can replace it. A is a
separate deep copy with cloned skill names and read-only dictionary/list wrappers.
All late event mutators refuse finished results. Configuration JSON remains private
in memory; the expandable UI uses the frozen human-readable configuration only.
Different scene/time cap/seed/class/level/target-rule version and early exit/death
are explicitly not directly comparable. Normally completed objectives may compare
actual elapsed completion times under the same conditions.

Validation:
- PracticeHudProductionTests.py: 858 production GUI/presentation/record/actual
  MobileControlLayout checks across 5 sizes and 3 presets, plus 3 compiled negatives.
- CampPracticeSessionProductionTests.py: 130 actual Begin/Start/Restart/End, repeated B,
  frozen-A identity and explicit replacement, deferred death, teardown, background,
  save failure, telemetry and original-owner restoration, plus 10 compiled negatives.
- Existing profile/draft/apply/cancel, actual pressure/Slam, H01 and wiring regressions.
- Three runtime branches compile against pinned Unity API references (no engine run).

baseline-negative-calibration.log retains the first insufficient alias oracle:
restart had already moved the live record, so comparing only the new record did
not detect the alias. The final test additionally compares the exact pre-restart
source and current final record; both rolling-A and alias mutations are rejected.

Managed GUI text measurement and engine boundaries are explicitly substitutes,
not screenshots, Unity physics, real fonts, frame timing or device acceptance.
