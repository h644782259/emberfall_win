# Existing-work handoff — win / progression

WIP checkpoint only. Do not merge as a completed/approved feature. No new gameplay implementation was made for this handoff. Recovered original commit: `d5d04bfd28e58cca7803e84a5d8f72ef2089145f`; original tree: `8d11fb62fd83801692e9a485bc5fc15cfc2d18a8`. Only this handoff document is added by the checkpoint commit.

## Validation provenance

Frozen full report: 253/287 passed, 34 failed. Source changed during full run: `[]`. These full results precede subsequent affected fixes; do not treat them as a full run of this checkpoint.

Original failures: progression, enemy-kill-rewards, progression-goal-identity, chest-pause-back-production, economy-growth, filled-vfx-allocation, milestone-goal-surface, elemental-field-continuity, mobile-blessing-preview-production, chapter-host-production, elemental-priority-production, integrated-player-journey, economy-goal-ui, reforgeselection, desktop-opportunity-hotbar, collection-compact-geometry, equipment-appearance-production, returning-counter-persistence, chest-choice-presentation, practice-hud, build-plan-page-geometry, class-switch-ui, reward-polish-ui, camp-build-draft-production, scenery-presentation-production, fixed-scenery-production, fixed-scenery-enabled-integration, concentrated-venom-production, camp-practice-production, camp-practice-session, g07-factory-inventory, reward-presentation-exceptions, practice-hotbar-navigation, practice-potion-input.

See `Docs/Validation/RoomGenerationRecovery/` for original logs, source hashes, baseline comparisons and follow-up checks. No Unity Editor/native platform/device/rendering/performance acceptance. Pinned Unity2021 reference-API compilation is not Unity6000 native validation.

## Included work

Optional crystal spawn reservation and supply corridor escort placement, generation diagnostics, then three independent review fixes: generation failure overrides ineffective ember/frost advice; short cause plus expandable readable details uses matching font/card/scroll sizing with developer-only seed/stack; first-entry failure cannot be overwritten by a success notice. All nine affected suites plus platform reference compile passed; integrated runtime hashes match validated isolated source. Frozen failures match archived same-main baseline. Final fixes still need independent review; original Seal native issue is not reproduced.

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
