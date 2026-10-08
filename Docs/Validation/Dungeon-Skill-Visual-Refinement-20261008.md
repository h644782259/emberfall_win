# Dungeon split-list and differentiated clear rewards — 2026-10-08

This follow-up branch starts from the frozen, merged HUD/inventory candidate. It does not change main.

All six entries now use a scrolling 30% list and scrolling 70% details; tier, healing restriction and enter remain outside both scroll bodies. The sixth entry opens the existing chapter chooser on explicit confirmation. The iOS five-entry guard exception and overlapping sixth-entry goal card are removed. Supported interface text sizes 100/110/120%, tier 100 and logical canvases 568×320 through 1920×1080 have managed geometry coverage.

Authoritative `AdventureRewardRules` drives clear-equipment preview and atomic owned-item generation: ruins rare weapon + existing cosmetic chest; hold rare armor; timed rare relic; boss epic weapon; room chain rare armor and rare relic. Rare rewards have 15/20/25/30/35% epic upgrade chances at existing tier bands; boss weapons have 8/11/14/17/20% legendary chances. Enemy loot remains separate. Existing gold/XP/material formulas are retained. Restricted healing has no invented gold bonus: Risk Contract is the actual bonus source.

Receipt-derived item IDs/rolls are stable on retries. Item generation keeps each platform's existing level generation policy. Existing receipt and commit paths own all granted items, never a hidden claim or automatic sale. Ordinary pickup safety remains 4096; two reserved clear slots allow committed ownership through 4098, visibly in the bag. Such saves use format 6; older format-5 readers reject without falling back over rewards. Reload retains reserved overflow and entry is blocked until explicit cleanup. The normal inventory capacity stays 256; the reserve is a safety guarantee, not a gameplay capacity increase.

Milestone targeted validation: both 5/5 checks, source unchanged during each run. Reports `/workspace/validation/win-dungeon-milestone-fixed/report.json` and `/workspace/validation/ios-dungeon-milestone-fixed/report.json`; six-entry actual production-method replay 848 assertions, authoritative clear-loot/atomic ownership 3256 assertions, existing reward migration 62 assertions, split/footer/readability geometry, plus Windows/iOS Unity API source compilation. Tests use managed GUI/serialization boundaries, not Unity rendering or device acceptance. Final full suite will follow the mastery visual refinement.

## Compact skill and mastery milestone

The profession/mastery page now uses one compact row of graphical class/save/apply/reset actions and four distinct raster mastery branch icons, ranks, progress and 44-unit upgrade targets. All four branches fit the 188-unit smallest body viewport. A/B actions retain existing save/overwrite/apply confirmations, class switching retains its captured transaction, combined reset retains confirmation, and selective refunds and unique-core/camp gates call their existing service methods. Only selected-node details are expanded. Font metrics are measured for scroll extent at configured text scale; full paragraphs no longer push the nodes below the first screen.

Touch skill selection opens a bounded same-page scrolling hint over the still-present tree, with graphical close and learn actions. Outside dismissal consumes input, Back closes the hint at every touch width, the tree scroll position remains, distinct profession-route return is retained, and failed learning shows the actual persistence error without spending. Skills also retain normal repaint style during their input transition latch.

Milestone targeted checks: both 8/8, including 368 production mastery measure/dispatch/confirmation/gate assertions, 605 actual same-page tree-popup assertions, 1075 small-screen/tablet/tool/popup/enlarged-font geometry assertions, expanded raster/cache checks for four unique mastery and six management silhouettes, actual ClosePanel and route feedback checks, 319 service integration assertions and Windows/iOS API compile. Reports `/workspace/validation/win-skill-milestone-freeze/report.json` and `/workspace/validation/ios-skill-milestone-freeze/report.json`. These remain managed checks, not rendered or device acceptance.

## Combined main preservation and final review

Normal merges preserve Windows main fb6291a31d5eed90972a47dff03eea1d4e07a074 and iOS main b2927a16864b68f8a4150b0b530e425e1be7140b, including direct pause-category tabs. Combined targeted validation passes 9/9 on each platform, with no source changes during execution. Updated actual-production mastery replay has 389 assertions, lifecycle replay 218, and clear-reward atomic/migration coverage 3260. Desktop available points, actual level-30 mastery unlock, and shape-based selected-node markers are included.

Managed cross-version probes compile the current writer, frozen stage-one format-5 reader, and current reader separately. Both platforms write 4098 owned items as format 6; old-reader load and save attempts refuse and preserve exact primary and backup bytes; the current reader subsequently retains every item. Additional tests cover a failed format-5 to format-6 write preserving profile and both files, bounded file size, successful retry, and explicit cleanup returning saves to format 5. This is managed serialization coverage, not actual Unity JsonUtility migration or device acceptance.

Final full-suite results are recorded after the frozen combined-source run. Windows stage-one baseline has 14 failures (13 existing plus obsolete merchant title guard); iOS baseline has only that obsolete guard. The merchant guard is now checked against the actual three-tab interface.

## Final frozen-source result

Tested source commit: `dada047884151b1a5cd3e9e96959eb4328ace222`. 290/304 full checks passed; sourceChangedDuringRun is empty. Both platform runtime API compilations pass. Evidence is in `Dungeon-Skill-Final-Evidence-20261008/full-report.json` and `managed-format6-oldreader.log`. The subsequent evidence commit changes Docs only; Assets/Tests/Tools trees are identical to the tested commit.

Windows retains 13 stage-one baseline failures with identical normalized failure signatures (see baseline-failure-comparison.json). The full run additionally failed companion-path-allocation: legacy allocation was 256 bytes above its expected multiple, with all exact target/direction traces unchanged. Its source and fixture dependencies are identical to stage one. An unchanged-source isolated rerun passes 1/1; original failure, rerun log and separate report are preserved. The raw full count remains 290/304, not rewritten as 291/304.

No actual Unity Editor, Unity JsonUtility migration, rendered UI, GPU validation, Windows player build or iOS package/device execution was performed. UI acceptance and real-engine save migration remain user validation requirements; main is not updated by this follow-up.
