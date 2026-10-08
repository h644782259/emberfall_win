# Mobile HUD and inline inventory candidate — 2026-10-08

Baseline: the unified production candidate plus the independent Unity validation test revision (`4c82c725` Windows / `5aabb12c` iOS). Android and main are unchanged.

Parent-reviewed screenshot evidence places the fourth shield skill 21 pixels below the preceding spiral. Its normalized vertical offset changes 118→153, lifting it 35 units and continuing the arc. Ultimate, attack, jump, dodge and page hitbox positions remain unchanged. The contextual interaction target becomes 48×48 at the right edge to avoid overlap on the smallest layout/preset. Four regular slots, two pages, fixed ultimate and existing pointer ownership remain unchanged.

All skill/action buttons now have an antialiased continuous thin circle, approximately 4.5% background fill, and an available-state green rim with faint blue halo. Unavailable icons remain dim with existing cooldown/resource captions. Skill glyphs are white; page switching uses a cached white two-arrow raster graphic with no font character or page numbers. Input rectangles remain independent of visual scale.

Inventory keeps the equipped panel and equipment/supply/fashion tabs. The four filters (all/weapon/armor/relic) are always visible in a 60-unit right rail, after the scrollbar. Selected filters show colored text/underline and a subtle fill. Clicking resets the grid scroll and immediately rebuilds items. No picker page, header entry or sorting entry remains. Existing descending-score/stable-tie comparator and compact 44-unit item cells remain. The former horizontal PC filter row is reclaimed for grid height.

Reference images were not downloaded successfully in this environment through the Library supported flow. Coordinates/style are grounded in the parent’s actual pixel review; no permission workaround or local visual acceptance is claimed.

Validation: device/preset touch non-overlap, rising arc/fixed ultimate, rail/grid/popup containment, full-circle alpha coverage, white graphic/caching, popup ownership, scrolling, score order, mobile input and runtime API compilation. Final frozen-SHA targeted reports are `/workspace/validation/win-hud-inventory-final/report.json` and `/workspace/validation/ios-hud-inventory-final/report.json`. Skipped full-suite tests are not represented as newly passed. No Unity Editor, device, simulator interaction, or rendered visual acceptance is performed here; Mac compilation/install and user visual verification remain external.
