# 03:25 integration: first development stage

Exact branch point: 57868b223994eb2ed4eb5d8f14ddcc1c9102832e. Independent branch: codex/skill-goal-reward-integration. Android unchanged; no main merge.

- Skills contain learning/configuration plus class routes/mastery/reset/presets. Existing investments and save references remain.
- Goals have an independent top-right icon and include four historical combat-trial progress flags. Existing growth reward receipt identity is preserved.
- Mechanic exchange is at the merchant; old workshop claim tabs and its bottom adventure/skills/inventory shortcuts are removed.
- Inventory regular capacity is 256 (formerly 72). All owned overflow stays visible and directly usable in the existing inventory grid, including ordinary equipment. Safety ceiling 4096 plus the existing 4 MiB document limit rejects acquisitions without consuming ground items. No implicit overflow sale.
- Old pending/recovery equipment migrates by ID into inventory. Migration persists before role publication; write failure leaves the source file and current role intact. New save format 5 makes older readers reject these saves rather than truncate overflow.
- iOS receives the missing independent attachment compatibility layer. Old gear, upgrades, variants and preset references are retained; migration does not delete gear or compound bonuses. Windows already had this layer.
- Floating player “角色” occlusion label removed on iOS; Windows base already has no such label. NPC labels unchanged.

Validated 5 focused checks: reward inventory integration (59 assertions), save idempotence (149), build presets (136), goal identity (53), full runtime Unity API compilation (0 errors/warnings). Reference DLL compilation uses pinned Unity 2021.3 API references; it is not Unity Editor, platform build, real JsonUtility, rendering, or visual acceptance.

Known follow-up: old upgrade/loot tests still encode removed claim/autosell rules and will be revised with the new regressions. Added blacksmith/merchant requests are a separate following stage. No actual UI/device validation was run.
