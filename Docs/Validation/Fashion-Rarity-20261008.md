# Fashion rarity and class identity — 2026-10-08

Scope: existing owned-fashion UI and cosmetic models only. No goal/reward-system/skill-tree integration. No Android files, combat hitboxes, range, damage or ownership rules changed.

Wings use 3/4/5/6 paired feathers and root scales 0.48/0.72/0.96/1.22 for common/rare/epic/legendary. Epic uses faceted crystals, legendary adds two small rotating astrolabes. Mesh part counts are 9/11/13/17, bounded at 20 in the construction test. Colors progress from muted feather to blue/cyan, violet/crystal, gold/cyan. Actual transformed vertex extents must grow at each rarity for all four heroes; the test evaluates full hierarchy transforms, not just scale constants.

Trails: common/rare none; epic two; legendary desktop four and mobile two; reduced-effects construction none. Width 0.028, lifetime desktop 0.14 seconds/mobile 0.08 seconds, minimum vertex distance 0.15, no cap/corner subdivisions or shadows. Teleport movements over 1.5 units clear trail history; enabling reduced effects suppresses existing trails. These are authored limits, not measured GPU/frame-time results. Rendering and hazard/HUD readability need user device acceptance.

Weapon accents use low-poly faceted crests, leaves and antlers; sword guards use pointed crystals instead of dense rings. The unchanged <=800-triangle loaded-weapon budget also covers the starter sword without equipment.

Weapons retain class anchors and equipped weapon structure. Vanguard gains blade flares, Ranger outward limb crests, Arcanist crystal crown prisms, Summoner antler/leaf branches. Epic and legendary add two and three pairs. Expanded construction tests verify grip clearances across all classes, four equipment tiers and four rarities; adjustments also keep legendary guard rings out of the sword grip and bow hand opening.

Weapon names (common → legendary):
- Vanguard: 灰羽誓锋剑 / 霜岚仪典剑 / 星狱断章剑 / 曜冕天衡剑
- Arcanist: 萤砂引星杖 / 霜环奏鸣杖 / 紫曜织界杖 / 日冕司辰杖
- Ranger: 林露轻歌弓 / 月潮逐风弓 / 虹羽巡天弓 / 九曜破晓弓
- Summoner: 苔芽契灵杖 / 青枝唤魂杖 / 幽莲归梦杖 / 万灵祖庭杖

Wings: 雾羽轻翼 / 潮光双翼 / 晶虹幻翼 / 日冕天翼. Inventory popup explicitly labels 时装. Canonical IDs remain `fashion-{slot}-{rarity}`. Actual ProgressionService save/load tests cover all eight owned items, both equipped IDs, sixteen unique class weapon names, and unchanged damage/health/armor. The managed serializer boundary is not real Unity JsonUtility.

Validation: WeaponFashionStructureProductionTests executes production constructors and validates transformed geometry/trail state; EquipmentCompositionProductionTests covers equipment composition/resource lifetime; ExplicitActionPersistenceTests exercises save/load identities and action rollback; InventoryIconPopupProductionTests covers actual popup methods with GUI/service boundaries. Pinned-reference runtime compilation is not Unity Editor, platform build or device rendering.
