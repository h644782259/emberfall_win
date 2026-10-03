H02 physical-pixel follow-up

The original 858 checks covered touch-unit geometry and actual overlay drawing with
TouchRatio=1. They did not establish physical-pixel target size after DPI fallback.
At 568x320, DPI=0, the actual MobileControlLayout scale is 320/390; an old 48-unit
button rendered at 39.38 pixels high. That old behavior now fails a compiled negative.

The follow-up executes actual GameUI.RefreshLayout, TouchRatio, BuildPlanRect,
DrawPracticeOverlay, and the actual OnGUI matrix assignment. The managed matrix
boundary applies the production scale and safe-area translation to emitted button
rectangles. Four viewport sizes, five DPI inputs including zero fallback, both
mobile/desktop paths and nonzero safe-area offsets verify actual pixel rectangles,
48-pixel minimum dimensions, safe bounds and transformed ten-skill/foot-view clearance.
Production PracticeHudLayout expands only action dimensions when pixel scale is
small; the countdown/sidebar remain in their existing top-band locations.

The log includes the 568x320 DPI-fallback emitted rectangle. Tests continue to use
managed GUI/matrix/font boundaries; this is not a Unity screen or device recording.
