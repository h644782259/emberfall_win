# Emberfall icon artwork

Generated with the built-in ImageGen tool. Full transparent source atlases and exact prompts are retained in this directory. Runtime copies are in `Assets/Resources/IconArt`, shared by iOS and Windows.

## Runtime layout

- `skills-{vanguard,arcanist,ranger,summoner}/00..09`: the existing ten skill identities in game order. Every skill has a separate luminance `-cooldown` texture. Combat controls, skill trees and details share the same artwork.
- `equipment-{weapon,armor,relic}/00..15`: four hero families in enum order; each family has Common, Rare, Epic and Legendary variants in that order. Index = hero * 4 + rarity. Matching silhouettes and motifs establish the series; increasingly elaborate materials, settings and trim establish quality.
- `fashion/00..15`: wings, ceremonial sword, ceremonial bow and ceremonial staff, with four appearance tiers each.
- `resources/00..15`: potion, coin, shard, skill gem, refinement stone, magic thread, inventory bag, energy core, smith hammer, portal, normal attack, dodge, skill book, anvil, codex compass and experience star. Existing callers use the applicable resource artwork.
- `gems/00..03`: matching quartz, blue silver, violet ornate and golden legendary skill gems. Existing rarity-colored callers resolve to these painted variants before tint normalization.

- `actions/00..03`: arcanist staff, ranger bow, summoner orb and jump boot. Vanguard uses the authored sword.
- `toolbar/00..01`: merchant stall and achievement medal.

The original `equipment.png` is a retained style reference; it is not packaged into the player.

## Presentation

Runtime textures are 128 x 128 RGBA with an 8px transparent inset, bilinear sampling, clamp wrapping, no mipmaps and no CPU-readable backing copy. They remain approximately 10.625 MiB uncompressed GPU data for all 170 textures. Color artwork retains its own material colors; existing colored frames communicate rarity. Disabled artwork still dims. Very small navigation symbols retain their existing pictograms.

Cooling skills show a dim gray base with the color painting restored from bottom to top using the actual cooldown fraction. A ready skill restores its full painted colors and highlights one skill-colored inner circle. The common green/blue outer rim is removed. Readiness still depends on the existing cooldown and resource rules.

`Tools/slice-icon-atlas.swift` performs only deterministic cropping, downsampling and grayscale conversion; it does not synthesize the artwork. `Tools/icon-art-preview.swift` produces a contact sheet at 32/48/64px, including a cooldown recovery illustration. This sheet is an art review aid, not a screenshot of live gameplay.

Run `Emberfall.Editor.IconArtImport.Validate` in Unity to verify imports, shared skill mappings, separate cooldown textures and equipment rarity variants. The preview is `Docs/Validation/IconArt/contact-sheet.png`.

The combat cluster is inset from screen edges; combo feedback sits above the ultimate. HP and energy have equal heights and sit lower. Worn equipment/fashion is excluded from bag entries; double-clicking its equipped slot returns it to the bag.

See `Docs/Validation/IconArt/UnityUI` for actual Unity GameView screenshots, all four hero attacks, equipment/fashion state transitions and five cooldown fractions.
