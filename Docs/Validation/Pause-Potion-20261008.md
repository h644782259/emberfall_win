# Compact pause and potion follow-up — 2026-10-08

User screenshot follow-up; candidate awaits user UI acceptance.

- Pause root removes only the redundant 前往遗迹、城镇旅行地图、行囊、图鉴 / 待领 entries. External entry points are unchanged. Continue, save, load, main menu, camp/evacuation, guide and More Settings remain.
- Pause measures the scaled title, keeps header navigation outside scrolling content, and uses a safe-area-contained scroll viewport for all three pages. Six root actions occupy two rows. Remove layout explanation and font preview; retain actual text scale control.
- Potion uses a 36-unit circular visual (respects compact-icon preference), independent 44-unit touch area and quantity badge. Its center aligns with the compact HP/energy group; touch edge is six units left of the bar, visible edge ten units left at standard scale. Safe-area coordinates drive placement. Raise the group 13 units to contain the touch area and lift the narrow-screen skill cluster 14 units to avoid overlap. No rectangular group backdrop.
- Development Console and error logging are unchanged. Previous candidate already replaced decorative primitive creation with explicit collider-free meshes.

Validation: actual pause drawing/action methods execute in a managed GUI measurement fixture (1,080 assertions); control geometry tests cover small phones/iPad presets (16,148 assertions); runtime compiles against pinned Unity references. Tests do not reproduce device font rasterization or constitute screenshot/device acceptance. Full-suite results recorded separately.
