# Icon artwork validation — 2026-10-10

Built-in ImageGen generated the transparent paintings. Exact prompts and full atlases are in `ArtSource/IconArt`. The final runtime set contains 170 textures at 128px: 40 skill paintings, 40 gray cooldown variants, 48 equipment rarity variants, 16 fashion variants, 16 resources, 4 gem variants, 4 class attack/jump icons and 2 shop/achievement icons. Assets and metadata are byte-identical in iOS and Windows.

## Actual rendered UI

`IconArtVisualReview.Run` used an isolated Unity project and QA save directory, rendered the production GameView on Metal, and captured 20 upright screenshots in `UnityUI/`. It exercised all four heroes' battle, bag and skills screens; arcanist fashion; cooldown restoration at 0/25/50/75/100%; and equipment/fashion unequip transitions. `UnityUI/evidence.txt` records these states. Screenshots establish actual Unity UI presentation, not Simulator finger-input acceptance.

- Skills fill the former outer-circle extent, with a single identity-colored inner frame; the common green/blue outer rim is absent. Cooldown uses a gray base and bottom-up color recovery. Ready artwork restores its material colors.
- Class-specific attack artwork is visible for Vanguard sword, Arcanist staff, Ranger bow and Summoner orb. Jump, shop and achievement use newly generated artwork.
- Combo feedback is above the ultimate; the combat cluster has increased right/bottom inset. HP and energy sit lower with equal heights.
- Equipment: 12 owned / 3 worn / 9 bag; first equipped-slot click retained it, second click unequipped it, bag 9 -> 10.
- Fashion: 8 owned / 2 worn / 6 bag; first click retained it, second click unequipped it, visible entries 6 -> 7.

The review completed all 20 captures and state assertions without a gameplay error in its evidence file. Unity subsequently emitted a Search startup-index exception and exited with code 1; this infrastructure failure is preserved in `unity-ui-review.log` and is not described as a clean editor exit. The 32/48/64px art contact sheet is a separate illustrative aid.

## Source and layout checks

Final iOS runtime/editor and Windows runtime/editor compilation against Unity 6000.6.4f1 assemblies passed; reports are saved here. Existing obsolete API/unused-field warnings remain.

405 focused checks executed the production `MobileControlLayout` across phone/tablet sizes and position presets: equal HP/energy height, combat edge margins, and vitals clearing skills. These checks exposed a 568px-wide phone overlap; health width now accounts for the first skill's left edge. The check source and output are retained here.

Two older source-contract suites still fail assertions on panel-layout strings absent from HEAD; those files were not modified here. The broader mobile-layout suite also stops on the existing 568px joystick/potion overlap (unchanged controls) and contains old exact geometry expectations. These suites were not rewritten or reported as passing.

## Import and simulator boundaries

The earlier 164-texture set passed `IconArtImport.Validate`, simulator export, Xcode build, installation and title-screen launch on iPhone 16 Pro / iOS 26.5. The final 170-texture set was imported and rendered during the actual UI review; its dedicated final import assertion run and latest simulator repackaging could not complete because Unity's licensing channel would not reconnect while another chat's editor was active. The installed simulator app therefore predates the final circle/layout/action refinements. Shared licensing services and other chats' editors were not reset.

Windows was source-compiled; no Windows player was executed on this Mac. No commit or push was requested or performed.
