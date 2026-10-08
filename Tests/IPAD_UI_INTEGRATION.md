# Tablet UI and item presentation integration — 2026-10-08

## Scope and rendering entry audit

Every GameUI.OnGUI path uses RefreshLayout's root matrix. Touch paths use the same MobileControlLayout.UiZoom and TouchRatio; input ScreenToUI uses the inverse root transform. UiZoom is applied once to each transform, so TouchRatio does not apply it twice. iPad detection requires an active mobile platform and an iPad device model. Desktop and iPhone keep their previous density; user text size, opacity, placement and control visual size preferences remain effective.

| Entry/path | Scale and responsive content |
| --- | --- |
| HUD, hotbar, companion commands, interaction, minimap, target/charge/notice | Root matrix and shared touch geometry; command icons use real roster/order/target state |
| Inventory equipment/supply/fashion tabs, wear map, popup, comparison | Shared touch panel, measured scroll and wrapping grid; identity icons carry slot, class, ten-level band, quality or preserved fashion design |
| Skills, mastery, points, reset, profession/build plans | Same root/touch transform; existing measured panels and scrolling retained |
| Merchant, exchange, sale and smith | iPad panel caps expanded to 1040×780 / 1100×780 logical units; smith equipment cards capped at 108 units |
| Dungeon, chapter, goals/trial selection | Responsive selection columns; measured slot-by-rarity preview rows; enter footer remains fixed outside scroll |
| Pause, settings, save location, save selection | Shared transform; tablet save list widens up to 900 units with proportional action columns |
| Title, class creation, continue/other saves/new game | Full physical viewport background before safe-area/root transform; foreground retains existing routes and save metadata |
| Confirmation, NPC dialogue, blessing/interlude, chest, recap and results | Shared matrix/panel/dialog sizing; single full-width blessing confirmation; unopened chest reclaims 52 footer units |
| Help/guide, input bindings and potion assignment | Shared matrix and touch transform; existing compact text forms retained |

## Scale and dimensions

The landscape tablet factor is 1.4, clamped only when the minimum 568×320 logical viewport would otherwise overflow. With no safe-area inset, a 48-unit target changes from 96 to 134.4 physical pixels on the 2266×1488 mini (326 DPI), and from 77.74 to 108.84 pixels on the 264-DPI iPads. A 14-unit touch font changes from 28 to 39.2 pixels on the mini, and 22.67 to 31.74 pixels at 264 DPI, before existing integer/font preference rounding. Logical gaps and icon sizes use the same factor. Existing title/class-creation fit-to-viewport zoom can reduce the effective enlargement of that foreground when constrained; the shared tablet factor remains 1.4. This is a fitting exception, not a claim that every final pixel rectangle is exactly 40% larger. Merchant and smith grid columns fill available width instead of keeping large centered empty margins.

Executable geometry cases cover 2266×1488, 2048×1536, 2360×1640, 2388×1668, 2732×2048 and 2752×2064, both orientations, three placement presets, and 0/24/48-pixel symmetric safe-area insets. Production supports landscape; portrait is a fallback geometry check, not a supported portrait product claim. Portrait may clamp below 1.3 (for example mini with 48-pixel insets: approximately 1.225) to preserve the minimum viewport. Font preference cases include 100%, 110% and 120% measured bounds; these are managed geometry checks, not Unity font rasterization.

## Visual identity and budgets

Equipment atlas cache keys include slot, class, level band and rarity. Ten-level detail layers, quality facets and two legendary crown edges change actual meshes as well as raster silhouettes. Fashion uses its stored appearanceTier and remains Legendary quality; old designs are never relabeled as lower quality. Bag, equipped item, loot, merchant, smith, preview and receipt paths use the same identity atlas. Action pictograms retain tooltips and text state explanations.

Title textures are built once at 256×128 and 128×32 with CPU storage released, with three mist layers and 28 embers (eight static embers with reduced effects). The drawing budget is at most 34 calls; no camp scene is constructed or simulated for the title. Animation freezes on background suspension; textures release on leaving title and destruction. Opaque coverage is independent of safe area. Gear adds at most five solid inlays, two quality facets and two crown edges, with no new trails, lights or particles. Fashion wings stay within twenty meshes and retain existing mobile trail limits.

## Reward and progression protection

The existing visible equipment capacity is 256; protected saved equipment limit is 4098, including two reserved clear-reward slots. These values and the save schema are unchanged by this presentation follow-up. Full normal bags retain rewards visibly. Absolute capacity/write failure retains the durable qualification and frozen reward identity; an explicit merchant recovery action allows manual capacity management, never automatic sale or silent loss. Pending unopened chests render before results and choices; blessing confirmation rejects pending chest state. Global pause, safe save/exit and receipt animation skip/acknowledgment remain available. No unopened return/close action bypasses reward commitment.

## Executable validation

- UnifiedUiIntegrationTests.py: 12,545 actual layout/input, backdrop lifecycle, roll reachability and atlas pixel assertions, managed engine boundaries.
- EquipmentVisualIdentityProductionTests.py: 3,296 actual gear geometry, legacy fashion identity and budget assertions.
- MandatoryChestServiceTests.py: 23 actual save transaction/capacity/restart/fault assertions.
- SingleChestUIProductionTests.py: 120 actual desktop/mobile opener and acknowledgment assertions plus compiled negative controls.
- ChestPauseBackProductionTests.py: 85 production Update pause/back assertions; engine/session/persistence are explicit boundaries.
- MobileBlessingPreviewProductionTests.py: measured fixed confirmation and preview/notification behavior plus a compiled occlusion negative control.
- CompanionIntentProductionTests.py: production order execution/state transitions, target and roster invalidation, spam and existing compiled negative controls.
- Existing equipment composition, class topology and fashion structure tests execute actual model builders and compiled negative controls.

Runtime API compilation does not produce an EXE/IPA and is not a Unity Editor build, rendered UI acceptance, simulator or device test. No user Mac or physical device is used. Final full-suite counts and baseline comparison are recorded separately in INTEGRATION_VALIDATION.json; unrelated baseline failures are not represented as passing.
