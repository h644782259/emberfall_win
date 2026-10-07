# Controls/readiness review candidate

Left-third safe-area touches outside the HUD start a floating joystick at the actual touch point. Ownership and origin persist outside the starting region; release, cancellation, pause and lifecycle reset clear movement and hide the stick. Explicit controls retain priority.

Skill readiness uses the actual learned rank, cooldown, energy, player lifecycle, charge, limited-healing and movement gates. The observation does not change pin, facing, companion command state, resources or cast state. Empty ground remains legal where casting allows it. Desktop and mobile use a stable readiness border; opportunity clocks remain a separate channel. Desktop glyphs fill the slot interior without an additional icon frame, potion quantities use ×, and the redundant heading and its reserved space are removed.

Platform-specific main changes are preserved. In particular, iOS retains eight active buttons [0,1,2,4,5,6,7,9], 60/72 sizing and ultimate at button seven. Two noninteractive passive identity cards are still pending layout work; this checkpoint does not claim completion of the ten-visible-identities request.

`report.json` records targeted results and production source hashes. Recorded exception output in negative tests is expected; the runners require specific assertion failures. The Windows mobile interaction source test also fails on unchanged main at the NPC destination assertion; its paired baseline log is included. Other targeted checks pass. Frozen full managed validation is recorded below.

The compile uses pinned UnityEngine 2021.3.33 reference assemblies, not the project's Unity 6000.6.3f1 Editor/native toolchain. No Unity rendering, device touch delivery, native build, screenshot alignment or visual acceptance was executed. Library reference-image downloads failed, and no local image was viewed. Native follow-up must cover narrow phone/tablet safe areas, large text, simultaneous movement/casting, focus/pause/scene transitions, and actual icon/quantity/readiness legibility.

## Frozen full validation

Completed 2026-10-07T11:17:31.770711+00:00: **251/288 passed**, 37 failed; `sourceChangedDuringRun` is empty. Every final failure matches the archived main baseline: **False**. See `full/report.json` and `full/baseline-comparison.json`. False requires investigation; it is never a passing certification. No Unity/native/device acceptance is claimed.
