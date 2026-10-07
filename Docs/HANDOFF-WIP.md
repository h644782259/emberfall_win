# Design-only handoff

Original commit: `a7a3e2853d828f2710685c4f05ec8d3b236937d1`. No mechanic migration or new gameplay implemented. The earlier unranked/duplicate-core suggestions in Mechanic-Core-And-Queued-Changes-Proposal.md are superseded by the latest requirements below; do not implement them as-is. Latest attempted expanded design write failed before recovery and is absent.

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
