# Expanded world and combat readability review (2026-10-11)

## Implemented in iOS and Windows source

- Camp playable radius 22m -> 32m (2.12 times area), with five additional outer hills, three extended trails, perimeter trees and northern ruins. Original camp services retain their positions.
- Standard dungeon radius 18m -> 28m (2.42 times area). Ordinary expedition now has five waves: 26–32 ordinary enemies per wave, then 14–16 escorts and one boss. Reinforcements remain queued with the original live population limit.
- Camp initial target 12 -> 24 (up to 32 by level), steady live cap 26/36, respawn interval 4 -> 1.8 seconds. Initial population is spread over frames and retries rejected placements.
- Terrain height fields now cover every dungeon, including chapter/tactical rooms. Authored special-room dimensions and mission geometry remain unchanged. Central approaches, water crossings, entrances and portals retain flat clearances.
- Dense floor geometry and sampled paths, weathered stone detail, rubble/moss/timber batches; expanded forest tree crowns and mine perimeter strata/supports.
- CPU height, navigation/platform surface, shader displacement and skill boundary height use matching fields. Static scenery follows the field; already grounded dynamic effects/loot do not receive duplicate displacement.
- HUD and equipment/material tags use opaque dark backing and brighter text; character level is at preview upper-left. Touch action icons are larger with opaque discs; skill cooldown recovery uses a contrasting moving boundary and progress strip.
- One screen-space damage caption per hit uses captured contact position, retains its admitted lane during turning, and cannot also draw a world text renderer.

## Validation boundaries

`review.txt` and screenshots are from actual isolated Unity Editor Metal rendering. Route, shader and grounding assertions passed. Warm return timings cover synchronous ChangeZone work in the editor, not complete native-frame latency or physical-device FPS.

`damage-review.txt` uses actual TakeDamage and samples the fading caption across player/enemy/camera rotation. Cooldown screenshots show 25/50/75 percent recovery of learned skills.

Source/API compilation is performed independently for both platforms. Native install/launch and screenshots use a separate iOS simulator bundle; Windows binary runtime and physical iOS devices are not measured. Increasing encounter volume is verified; actual clear time depends on character strength and has not been asserted as a fixed duration.

No commit or push was requested.

## Final execution evidence

- iOS Unity export and Xcode simulator build both succeeded for final source, including the final weathered-rock mesh change.
- Separate simulator bundle: `com.h644782259.emberfall.worldreview`.
- Latest Unity warm camp return synchronous samples: 64.40, 51.19, 56.57 ms. These are editor measurements.
- Latest ground-route/render assertions passed with no captured runtime errors.
- iOS/Windows editor/API source compile reports and Windows final runtime compile report are in `Compile/`. Final iOS runtime also compiled in the native export.
- Previous HUD/inventory simulator screenshots are in `../Readability/`; final expanded-world simulator interaction is pending because CUA reports the Mac is locked and automatic unlock failed. Installation and launch commands are separate evidence from interaction acceptance.
- Terrain production tests passed 1044 assertions independently on both sources, minimap numeric tests 2408, encounter tests 6236748 per platform and combat text geometry 11735 per platform. Those logic checks precede the final decorative-only mesh changes.
