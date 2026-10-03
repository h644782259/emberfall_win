# H05: one mobile opportunity clock

Mobile skills and basic counter/combo opportunities now use a shared outer mechanism seal, authoritative remaining seconds and a true grant-duration arc. Button interiors retain the skill identity and at most one ordinary rejection/charge/cooldown caption. The old duplicated top mobile opportunity text is removed; the existing actual-result receipt channel remains independent. Real timer metadata is observational only: no damage, range, cooldown, status duration, movement, or resource changes.

The 48-unit skill hit boxes remain unchanged. Hint rectangles are explicit layout members, with separate counter/combo strips, in safe-area coordinates. The lower layout preset shift is limited to +2 rather than +8 units to reserve 14-unit clocks above the attack controls; standard/up presets retain their positions. Twelve layouts (568x320 included), ten slots, two basic windows, compact/standard visual sizes and 1/1.5/2 presentation scales are recorded by actual production drawing methods. Display opacity is applied once. Acquisition emphasis is a single .35-second steady underline, not a periodic blink; pausing hides the clock without resetting its observed acquisition.

Production checks:
- `presentation-current.log`: actual skill/basic/shared-meter draw recorders, six compiled negative controls (pause replay, double opacity, uniform denominator, repeated acquisition, duplicate inner caption, overlap). Original development failures retained separately.
- `Duration/`: 41 actual status/counter/query assertions and four compiled negatives. Covers 1.8/2/3-second counter grants, boss frost, timer decay, short and long refreshes, actual consume/reapply, and multi-target remaining/duration pairing.
- `shatter.log`, `channels.log`, `counter.log`, `status.log`, `source-contracts.log`: preserved release footprint, owner, resource, pause/result, actual counter/melee and status-consumption regressions.
- `api-*`: current development-stage three-macro pinned Unity2021.3 API compilation (not final source freeze).

`CombatOpportunitySlotProductionTests.py` and `MobileBasicWindowDrawTests.py` retain their registered suite entry points and now invoke the richer shared real-renderer fixture plus the existing source contracts. Other consumers add only the new observational metadata fields.

The raw logs use managed GUI and engine boundaries. They are not screenshots, Unity GPU, font rasterization, input/device acceptance, or Unity engine validation. Final complete frozen validation is required after integration. Outer-hint pointer isolation is a separately verified follow-up in this package.
