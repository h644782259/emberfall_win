# Round 4 core stability audit

Scope: character-owned mechanism variant knowledge, one-slot preset equipment repair, allocation-draft shared budgets, and current-resource preservation through the actual Changed -> OnProgressChanged -> RefreshStats path. Initial inspected HEAD: 5b2e21da0af6aff3d7a131c234237665015709bd. This is a shared working tree; other agents' changes are outside this audit.

No concrete production regression was reproduced in this scope. No production code was changed. Only Tests/MechanicKnowledgeTests.cs and Tests/CampBuildDraftTests.cs gained focused boundary assertions.

Final targeted runs, all exit 0:

- MechanicKnowledgeProductionTests-final.log: 23 actual knowledge transaction assertions and two compiled negative controls. Added: learn B, explicitly unlock/sell the final teaching item, reload with no remaining matching item, acquire a later copy, select B for zero additional materials, reload the selection. Existing cases cover cross-character isolation, new-copy growth independence, legacy inventory/pending/recovery migration and failed migration persistence.
- CampBuildDraftProductionTests-final.log: 461 service/desktop/mobile draft assertions, 136 combined-respec/preset assertions, and four compiled negative controls. Added levels 1/2/3/10/20/35/100 to verify starter cost, joint point budget, separate mastery caps, no overspend, preservation of unspent points, and exact durable budget accounting. Existing actual callback tests cover fractional living HP, dead zero HP, max-health decreases, failed/no-op/apply-save transactions, energy and cooldown preservation, and the legacy non-draft refresh behavior.
- PresetReplacementProductionTests-baseline.log: 45 service/UI assertions and four negative controls, unchanged. Includes two missing legacy references, repairing only one slot, preserving the other unknown slot/other preset/allocations/keys/current equipment, failed writes, stale quote rejection, deliberate A fallback and sale confirmation.

Total: 665 positive assertions and 10 compiled negative controls across these targeted suites. Commands: `python3 Tests/<runner>.py /workspace/shared/emberfall-tools/dotnet/dotnet`. No full suite, commit or push performed by this agent.

Historical baseline logs are retained. The new test-writing attempts in *-boundaries.log and *-locked-fixture.log are not production regressions: the first knowledge extension referenced private FindItem, the initial budget extension incorrectly assumed mastery could consume every high-level point, and the first sale setup omitted explicitly unlocking the protected teaching item. Those fixture assumptions were corrected without changing production behavior; the final logs above are authoritative.

These are managed production-host/service tests with explicit Unity/session boundaries. They do not establish engine GUI, real JsonUtility, device, rendering or build acceptance. No shared GameBootstrap/GameUI/PlayerController patch is proposed.
