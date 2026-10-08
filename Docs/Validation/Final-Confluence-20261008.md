# Final coordinated confluence

Frozen old-task commits replayed on the independent integration branch:
Windows 82595e9 + b89ad54 (final old-task b89ad54dff31bcb24e98ee33984851d69a218162);
iOS 1cc5259 + 279b917 (final old-task 279b917d53e27a11d7aea82b915e3d0803001c58).
No text conflicts. ProgressionService shared edits merged; class fashion names/old IDs, rarity meshes and 800-triangle weapon limit retained. Android and main untouched.

Broad regression investigation found two unrelated iOS behavior changes introduced by the compatibility port: ultimate energy constants/descriptions and adjacent-only hub travel. These were restored to the exact frozen iOS baseline. Other production corrections guide attachment upgrade/variant/ascension goals to the blacksmith rather than allowing direct goal-screen transactions. Merchant goals similarly guide to the merchant.

Old 72-capacity/manual-claim/autosale tests were updated to 256 regular capacity, visible overflow and explicit sale. Atomicity, world receipt replay, save failure/retry and legacy item-lock scenarios remain executable. Explicit-source iOS projects include the introduced attachment/automatic-growth partials and neutral multiplier shells where combat is external to the test. Goal UI fixtures compile the current production method bodies dynamically and verify NPC guidance never spends resources or grants directly.

Two original Windows fixture failures are preserved in baseline logs: tier-zero boss rarity expected Rare while production TierRewardRules specifies Common, and item level expected exact character level while production EquipmentGenerationLevel specifies ten-level bands. Tests now use an explicit dungeon tier for the boss rarity assertion and the existing level-band contract. No production loot balance changed. Both revised full progression suites pass 2651 assertions /151 scenarios.

Pre-confluence candidate-wide runs were stopped after freeze arrived; their partial logs are diagnostic evidence only, not full passes. Original baseline full runs and final confluence full runs are recorded separately. Full final validation is pending in this commit; follow-up evidence will record the exact frozen source commit and classify every failure against baseline. No Unity Editor/platform builds, real JsonUtility, rendered visuals, Mac or device/simulator interaction executed here.

## Frozen candidate validation

Frozen old-task commits have been replayed. Both runtime API compilation checks pass (zero errors). Targeted regression repairs cover independent attachment variants, direct reward receipts, NPC-count geometry, immutable old-reader rejection, and class UI transactions. iOS exact-level equipment generation and original ultimate/travel rules remain preserved. Full headless regression will run on the resulting clean commit; report path: `/workspace/validation/win-frozen-final/report.json`. No Unity Editor, rendering, device or visual acceptance has been performed here.
