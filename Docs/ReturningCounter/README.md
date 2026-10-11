# ReturningBlade counter variant B

The PR45 follow-up fixes and validates the combined pinned-target admission and desktop workshop entry. Its logs supersede the earlier isolated admission evidence below.

Variant A stays the default for existing saves and keeps the original 8% basic damage penalty, bounce, return bonus and kill rebounce. Variant B uses the existing mutually exclusive index 1 and four-shard first unlock; switching thereafter is free in camp. Build presets, save validation, ascension and reforge retain the choice. No new save fields or parallel unlock system.

Only a confirmed perfect dodge grants B's three-second opportunity (A remains two seconds). The next counter basic uses the existing 175% coefficient, with no additional multiplier and no 8% penalty. Its hit shape is a forward narrow strip with 0.35m half-width, existing 2.8m reach and existing enemy footprint allowance; the old 110-degree cleave remains for ordinary basics. B disables the entire bounce/return/rebounce proc branch. Basic interval, energy handling, crit, hit stun and missed-counter retention follow the existing paths.

When a living aim target is aligned with the thrust, the hero can approach along the facing direction up to two metres, stopping 1.8m short of its center. Each 0.05m step checks the existing ground path and 0.45m player clearance and validates the endpoint. The approach never searches beyond an obstruction or relocates an invalid origin. No target means no approach. It grants no invulnerability and changes no dodge cooldown. The counter uses a dedicated existing-rig pose selected by internal action marker -2; recovery, cancellation and actual sword anchors remain shared. The default authored basic overlay is omitted only for this thrust, preserving the other authored layers.

Validation on merged PR35 main `4214b88` ancestry:

- `Validation/counter.log`: actual BasicAttack, Melee, NotifyPerfectDodge, full bounce proc branch, enemy dodge registration/confirmation, timer assignments, WorldTraversal and CombatSight. Managed engine/recipient boundaries; negative controls remove the proc exclusion, revert the window, or widen the hit shape.
- `Validation/persistence.log`: real service unlock/toggle, build capture/apply, reload, ascension, reforge and locked-data sanitation, plus existing progression-growth checks.
- `Validation/recovery.log`: real factory, authored pose, contact phase, sword tip, cancellation/recovery and paused death; includes counter-thrust contact and cancellation plus compiled negative controls.
- `Validation/mobile.log`, `facing.log`: adjacent actual mobile target/readiness and facing/pose commit suites, retaining their negative controls. ReturningBlade is explicitly disabled in these older fixture boundaries; the counter suite owns the new gameplay.
- `Validation/*-compile.log`: Windows, iOS and Android source/API compilation against pinned Unity 2021.3.33 reference assemblies. This is not Unity engine execution, device collision, visual acceptance or a player build.

Run the two `Tests/ReturningCounter*Tests.py` scripts with the .NET SDK path; the cloud validation runner includes both. No remote push or merge performed. Unity/device visual and input acceptance remain open.
