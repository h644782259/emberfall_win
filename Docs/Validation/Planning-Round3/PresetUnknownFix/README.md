# Per-slot unknown mechanism preservation

Fixes the reviewed zero-filled metadata bug in 4b3e74c. BuildPreset now records equipmentMechanicKnownMask. Missing metadata or an unset slot bit means unknown, independently of the stored enum value; None is known only when supported by a present owned item or an explicit known bit. New captures record all three known slots. Legacy arrays without a mask are conservatively unknown when the actual referenced item is missing.

During a one-slot repair, still-owned references may supply real mechanism evidence. Missing other slots are never inferred from default enum zero. Candidate sorting skips same-mechanism priority for an unknown original. Comparison continues showing “未知（旧方案未记录）” after another slot has been repaired and after reload.

production.log: 45 actual service/extracted-UI assertions and four compiled negatives. Added two-missing legacy weapon/relic case: actual UI repairs relic, then weapon preview remains unknown; candidate ordering does not prefer ordinary None gear; other references/allocations/other plan remain unchanged; known bits cover only actual owned armor and new relic; durable reload preserves unknown weapon. Setting all known bits reproduces the old assumption that zero-filled slots are known and fails the named actual-UI assertion.

preset-regression.log: existing 136 assertions. api.log: Windows/iOS/Android API compilation, zero warnings/errors. No new runner or production partial dependency; existing PresetReplacementProductionTests covers the change. Managed checks are not Unity execution or visual acceptance. No push or platform/package operations.
