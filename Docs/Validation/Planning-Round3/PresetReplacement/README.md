# Explicit single-slot preset replacement

Depends on character knowledge commit f41c220f6976db442fd4177ad56a91927931e306. No asset, platform, damage or skill timing changes.

The shared desktop/mobile plan page offers a manual picker for each slot. Candidates sort by the recorded/current mechanism, required variant eligibility, legal level, then descending level with stable ID ties. Selecting only opens a comparison; confirmation updates exactly one preset equipment ID, its selected variant and mechanism metadata. It never calls SaveBuildPreset or ApplyBuildPreset. Current worn gear, skill/mastery/core/hotbar data and the other preset are retained. The comparison uses PreviewEquippedItem (inherited slot reinforcement), mechanism gain/loss, level eligibility and explicit A/B choice. Required B is blocked when unavailable; changing to A/no mechanism requires an explicit separate preview.

A quote binds owner, source Profile identity and full serialized state. Both service and actual UI reject changes, including Upgrade modifying the same Profile object. Failed save remains retryable without publishing candidate data. Back/cancel writes nothing. Legacy presets retain null metadata; missing references are never automatically resolved. Newly recorded mechanism metadata supports sorting after an item disappears; missing old metadata is explicitly described as unknown.

Inventory detail and desktop rows show A/B references. Referenced single sale and low-quality bulk cleanup require a separate confirmation listing affected plans; the list scrolls with fixed buttons. Service methods default to refusal, with explicit confirmation arguments. Cancellation, stale state, changed owner, death/practice or wrong panel prevent the confirmation action. Confirmed sale retains the now-missing reference, forcing explicit repair rather than substituting a highest-score item.

## Evidence

- production.log: 36 actual production service and extracted actual UI request/preview/confirm/cancel/sale assertions; three compiled negative controls (remove sale reference guard; lose recorded B; revert to Profile-identity-only freshness checks). Unity input/rendering boundaries are doubles.
- knowledge-regression.log: 18 actual knowledge transactions and two compiled negatives.
- draft-regression.log: 295 actual service/desktop/mobile draft assertions, existing 136 preset assertions and four compiled negatives.
- reforge-regression.log: 29 actual UI/quote transactions and three compiled negatives.
- variant-effects-regression.log: 84 actual equipped/draft assertions and three compiled negatives.
- economy-regression.log: existing 5247 economy, 353 rebalance, 480 upgrade, 103 adventure, 52 goal, 136 preset and 2005 reward-truth assertions, plus two compiled negatives.
- api.log: Windows/iOS/Android API compilation, zero errors/warnings. Other *source.log files are supplemental static checks only.

Known baseline-only source-contract failure: growth-source.log expects ChestGoldMinimum(candidate.pendingChestTier), absent already in original fce5efc production. growth-source-baseline.txt proves that exact baseline absence. No chest code was changed to satisfy an obsolete string check. first-negative-oracle.log preserves the first fingerprint mutation hitting the earlier duplicate-quote assertion; the final mutation retains source identity and specifically fails the actual in-place-Upgrade UI assertion. variant-effects-first-oracle.log preserves an obsolete mutation needle; final runner targets the current selection-cache clause and retains the same behavioral oracle.

## Integration and consumer audit

New runner: Tests/PresetReplacementProductionTests.py (SDK path argument), for root registry integration. No new production partial or assembly dependency. Optional API parameters preserve existing call compilation: Sell(id, confirmPresetReferences=false), BulkSellLowQuality(confirmPresetReferences=false). Explicitly confirmed referenced sales use true.

BuildPresetTests now explicitly confirms its existing intentional referenced sale. HubTravelSourceTests signatures follow optional parameters without dropping protections. CampBuildDraftUIBoundary includes the Inventory enum, actual dead-state boundary and throw-on-unexpected sale helpers required when compiling the full shared UI partial. ReforgeSelectionTests, EconomyGrowthTests and ProgressionGrowthTests seed already-learned knowledge for their existing unlocked-equipment fixtures. Adventure's intentionally invalid-locked fixture removes both knowledge and item flag, then restores both. Real learning cost/migration is separately tested. SelectedMechanicB additionally checks its item selection cache, matching combat and preserving the existing forged-B rejection; HasVariant remains knowledge eligibility authority.

All checks are managed source/logic/API checks, not real Unity JsonUtility, GUI, shaders, engine or device acceptance. Root owns combined integration and full registered suite. No push, PR, package or Library action performed.
