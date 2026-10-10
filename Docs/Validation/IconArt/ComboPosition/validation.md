# Combo placement — 2026-10-10

Moved the mobile combo label to the open field at 74% viewport width and 40% height, centered instead of right-aligned. Narrow screens clamp its center left of the ultimate, allowing the entire rotated 160x54 label plus a 12px gap. Desktop placement is preserved. Both platform sources are updated.

Actual production Unity GameView screenshots were captured for all four heroes in an isolated project. The final 170-texture import validation also passed in this run. Unity emitted its existing Search startup-index exception and exited with code 1; the requested screenshots were saved; this is recorded in unity-review.log. The narrow-screen clamp was added after captures and does not change the captured viewport's placement.
