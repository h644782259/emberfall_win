# Existing-work handoff — win / controls

WIP checkpoint only. Do not merge as a completed/approved feature. No new gameplay implementation was made for this handoff. Recovered original commit: `5e5c74ea8adcc7348b95f3d5af042c101bd87ad1`; original tree: `4a6bbe4077bd05ccbaa63c4733e8ad2a25a1196c`. Only this handoff document is added by the checkpoint commit.

## Validation provenance

Frozen full report: 251/288 passed, 37 failed. Source changed during full run: `[]`. These full results precede subsequent affected fixes; do not treat them as a full run of this checkpoint.

Original failures: progression, enemy-kill-rewards, progression-goal-identity, chest-pause-back-production, economy-growth, filled-vfx-allocation, milestone-goal-surface, elemental-field-continuity, mobile-blessing-preview-production, chapter-host-production, elemental-priority-production, integrated-player-journey, economy-goal-ui, reforgeselection, collection-compact-geometry, equipment-appearance-production, returning-counter-production, returning-counter-persistence, chest-choice-presentation, room-branch, practice-hud, build-plan-page-geometry, class-switch-ui, reward-polish-ui, practice-action-settlement, camp-build-draft-production, scenery-presentation-production, companion-path-allocation, fixed-scenery-production, fixed-scenery-enabled-integration, concentrated-venom-production, camp-practice-production, camp-practice-session, opportunity-channels-round2, g07-factory-inventory, reward-presentation-exceptions, practice-potion-input.

See `Docs/Validation/ControlsReadiness/` for original logs, source hashes, baseline comparisons and follow-up checks. No Unity Editor/native platform/device/rendering/performance acceptance. Pinned Unity2021 reference-API compilation is not Unity6000 native validation.

## Included work and known unfinished checks

Floating joystick ownership, shared read-only availability, desktop full glyph/single frame, potion quantity and removed title/gap are implemented. iOS additionally keeps 8 active mapping [0,1,2,4,5,6,7,9] and 2 nonclick passive identities (4392 assertions and negative control); seven existing geometry/policy checks pass. Actual returning-counter and practice-action fixture dependencies were repaired and affected tests/mutations pass. Full report preserves their original failures.

Other full failures remain unresolved in this handoff, including opportunity-channels-round2. Do not claim all remaining failures match main. Windows allocation measurement failed default candidate runs despite identical input file hashes; two default main replays pass, both candidate/main pass with DOTNET_TieredCompilation=0. The original failure remains, not a baseline-matched full pass. See allocation-investigation and fixture-followup. UI requires final independent review and Unity/device acceptance.

## Remaining queue / superseded rules

- Floating joystick/readiness/desktop icon frame/potion count/title: implemented in controls WIP; full failures retained; independent review and Unity acceptance incomplete.
- Blessing double-click/double-tap confirmation: not implemented; preserve single preview/button, scroll/drag/modal ownership and idempotency.
- Automatic growth/rewards/current-goal navigation: design only. Completed second-plan goal must open plan 2 for inspection, never save again or open map.
- Unique levelled mechanic attachments: not implemented. Latest requirement supersedes unranked/duplicate-core design: one character/mechanic across every storage/binding, upgrade items/numeric and mechanic milestones, capped level/cost/source/migration table first; preserve legacy stats/investment and no upgrade resource/cooldown reset.
- Exchange/pending item entry and badge: audited/design only. Presence differs from claimability; full inventory must retain pending indication; core wording must match shipped model.
- Equipment/fashion common cards/isolated preview/equip UX and high-tier wings: design only, no new assets.
- Mode reward identities/icons: audited/proposed, no economy change.
- Chapter/result simplification, explicit next tier after durable rewards/chest settlement: not implemented. Successful completion removes menu/save button and empty space, retains recovery on save failure; global save/pause and failure page unaffected.
- Reduced post-clear capture waiting, ultimate range/visual expansion, reachable NPC clearings: audited/proposed, not implemented.
- Three-town content audit, M map shortcut, explicit sequential portals: not implemented; preserve unlock rules, no claim towns have no existing code.
- Standable low platforms: design only, no collision/navigation change.
- Reported tier15 limited-healing Seal room3/5 native failure: not reproduced; diagnostics added, NOT declared fixed.

## Resume safely

Fetch this WIP branch, inspect status/history before integrating, and preserve user changes on main. The controls and room-review branches are separate alternatives based on earlier main; integration/conflict resolution is unfinished. Do not assume source equivalence across platforms. No automatic main merge is requested.

Full validation: `Tests/Run-CloudValidation.sh --dotnet <dotnet8>` plus `--compile --compile-ios` for iOS, `--compile --compile-android` for Android, and `--compile --compile-android --compile-ios` for Windows. Read archived failures first; no rerun was performed for this documentation-only checkpoint.

Image references: eight authorized consumer-local retries and the later current-goal image failed to materialize. They were not visually inspected. No build ZIP/video upload is part of delivery.
