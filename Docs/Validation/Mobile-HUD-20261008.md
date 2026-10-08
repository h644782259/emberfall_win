# Mobile HUD follow-up — 2026-10-08

Mobile-only presentation changes; desktop DrawHUD and skill/passive gameplay are unchanged. No save-schema, balance, Android project, logging, console visibility or build-option changes.

- Remove the upper-left player status card and persistent passive badges. Keep all eight active identities (0,1,2,4,5,6,7,9), opportunity feedback, HP numbers and energy.
- Center a 160×18 logical-unit vital strip four units above the safe-area bottom. Health is 12 units tall; energy is three. On widths below 700 logical units, lift the potion, skill cluster and companion commands by 22 units to clear this strip. Potion remains 48×48; attack/jump/dodge are unchanged.
- Replace the growth/class-practice/chapter/adventure objective card with measured, shadowed text at the minimap's lower-left information anchor. Progress and seal percentages remain visible. Only the measured title opens the travel map and registers an input blocker; body text and blank space have no HUD hitbox. No full transparent objective rectangle is registered.
- Preserve the preceding iPad title-only enlargement and collider-free preview recovery commits.

Validation executes real extracted HUD methods under managed drawing boundaries (612 assertions), pure layout across nine device geometries/three presets (16,121 assertions), active skill policy, production pointer/targeting, opportunity clocks, floating joystick and preview allocation/recovery. Runtime source compilation uses Unity API references, not Unity Editor execution. iOS also checks title drawing/hitboxes (2,466 assertions) and chapter UI wiring. Targeted reports are recorded by the delivery response; skipped checks are not passes.

QA still required: build the candidate in Unity on Mac; open inventory/wear/fashion/reward panels; test repeated panel switching after preview failure; check iPhone and iPad safe areas and bright/dark objective text; click title and drag/tap its surrounding blank area; verify HP/energy, potion, eight skills and feedback. Confirm no missing MeshCollider/CreateContact error. Do not hide Development Console or disable logs to pass QA. Keep main unchanged until actual UI/operation QA passes.
