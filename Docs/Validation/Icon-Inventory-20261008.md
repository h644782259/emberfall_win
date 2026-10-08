# Icon inventory and owned fashion — 2026-10-08

Latest user requirements supersede the earlier always-visible equip/compare cards and try-on catalogue.

- Both desktop and mobile use adaptive 44-unit icon cells with a 4-unit gap, independent hit areas, type glyphs, rarity borders plus counted pips, equipment level corner labels and visible lock/worn markers. Minimum tested landscape shows at least 24 complete cells; columns adapt to available width rather than fixing 30 slots.
- Clicking opens a small same-page popup for equip/compare/lock or potion use. Popup bounds leave at least one grid column visible and stay inside the inventory area. Outside presses consume the event before dismissing; wheel scrolling closes it; grid movement closes stale anchors. Opening events and a 250-ms action gate prevent accidental immediate/repeated use. Item IDs remain authoritative after sorting/equipping.
- All equipment sorts by score descending, then level, rarity and ordinal stable ID. No sort picker is exposed. Type filter remains. The left model and equipped slots remain visible.
- Equipment / supplies / fashion are sibling light tabs. Fashion lists only owned records in the same grid. The popup directly equips or removes an owned appearance; no stat comparison, unowned catalogue, trial button or trial UI state. Left preview reads actual equipped records. Reward acknowledgement opens owned fashion after durable acknowledgement; it no longer starts a trial. Saved fashion IDs, ownership, rewards, sources and collection bonuses are unchanged.
- Old inventory-only try-on pages were removed. Collider-free model rendering and visible error reporting remain.

Validation uses actual popup input/action/sort methods with managed GUI/service boundaries, pure adaptive grid/popup geometry, existing real persistence tests and pinned Unity-reference runtime compilation. It is not Unity/device/screenshot validation. Final full-suite results will be recorded separately. Wing/weapon art differentiation and class-specific naming are a separate subsequent change.
