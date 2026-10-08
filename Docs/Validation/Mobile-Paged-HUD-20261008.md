# Mobile paged HUD and entrance follow-up — 2026-10-08

Latest user request replaces the always-visible eight-active mobile layout. Desktop hotbar mapping is unchanged.

- Seven normal active identities use pages [0,1,2,4] and [5,6,7,empty]; ultimate 9 occupies the same fixed location on both pages. Passives 3/8 remain passive. The second page has no invented fourth skill or invisible action in its empty slot.
- Four normal buttons follow an arc around the fixed basic/jump/dodge cluster. A 44-unit 1/2 switch sits outside the arc. Ultimate preserves the prior candidate's location. Safe-area geometry includes potion/vitals, companions, opportunity captions and wave/boss indicators.
- Skill and action glyph centers are transparent. Thin mint readiness rims are drawn only when the real availability query succeeds; cooldown/resource/target/state rejection dims skills. Stored uses do not bypass energy or state checks. Stock badges and cooldown text remain readable.
- Page presses cancel only an uncommitted touch capture. They do not mutate skill runtime, charge windup, target controller, energy or stock. Same-frame duplicate presses switch once. A release from the former page cannot cast a hidden skill. Hidden-page opportunity hints do not consume world input.
- Eligible nearby dungeon entrance appears in a compact top-center hitbox. Ordinary NPC interactions retain their right-side placement. Input uses the same dynamic rectangle as drawing. Mobile portal auto-notification is removed to avoid the redundant left-hand instruction box; desktop keyboard hint and error reporting remain.
- Root mobile skill tree no longer draws a duplicate bottom Return to Adventure button. Its list extends into the reclaimed space. Header X, route return and detail/learn navigation remain.

Validation: actual HUD/input extraction tests cover page identities, empty slot, stale release, duplicate press, resource/target preservation and ready/unready rim behavior (6,311 assertions including existing stock/vitals/text checks). Control geometry and production multi-pointer lifecycle/entrance tests pass. Both runtime branches compile against pinned Unity references with zero warnings/errors. These are managed boundaries, not Unity rendering, device event delivery or user visual acceptance. Full fixed-source regression is reported separately.
