// Regression checks for the gameplay-upgrade contracts. Compile alongside the original
// ProgressionTests.cs Unity stubs; this suite does not edit or rely on its legacy assertions.
using System;
using System.IO;
using System.Text.Json.Nodes;
using Emberfall;

public static class UpgradeProgressionTests
{
    private static string root;
    private static int scenarios, assertions;

    public static string Run(string testDirectory)
    {
        root = testDirectory;
        scenarios = assertions = 0;
        SpecializationIsExclusiveAndCampOnly();
        EveryMechanicIsEquippedAndClassScoped();
        ProtectedOverflowSurvivesReloadAndRefusal();
        AutoSellAndBulkNeverConsumeProtectedGear();
        FirstClearAndExchangeHaveDurableBudgets();
        EqualSlotUpgradePricesCloseTransferDiscount();
        LegacyUpgradesAndRanksRemainIntact();
        MasteryUsesExactlyTheRemainingPointBudget();
        CorruptNewFieldsAreBoundedAndRepaired();
        ChestReceiptIsExactlyOnceAcrossReloadAndFailedWrites();
        ProtectedAcquisitionsRollbackFailedWrites();
        DeepCopiesPreserveBuildAndRewardMetadata();
        RecoveryMailboxProtectsRetiringGroundLoot();
        SlotTrainingMigratesEveryStorageAndCannotBeSold();
        SlotTrainingAndEquipmentWritesAreAtomic();
        return "PASS: " + assertions + " assertions across " + scenarios + " upgrade progression scenarios.";
    }

    private static ProgressionService Fresh(HeroClass hero = HeroClass.Arcanist)
    {
        string path = Path.Combine(root, "upgrade-case-" + (++scenarios));
        var service = new ProgressionService(path);
        service.NewGame(hero);
        Check(string.IsNullOrEmpty(service.LastError), "fresh save succeeds");
        return service;
    }

    private static ItemData Plain(Rarity rarity = Rarity.Common, ItemSlot slot = ItemSlot.Weapon, int level = 1)
    {
        return new ItemData { id = Guid.NewGuid().ToString("N"), name = "测试装备", rarity = rarity, slot = slot,
            level = level, attack = slot == ItemSlot.Weapon ? 12 : 3, defense = slot == ItemSlot.Armor ? 7 : 0, health = 20 };
    }

    private static void FillBag(ProgressionService service)
    {
        while (service.Profile.inventory.Count < ProgressionService.InventoryCapacity) service.Profile.inventory.Add(Plain());
        service.Save();
        Check(service.Profile.inventory.Count == ProgressionService.InventoryCapacity, "bag fixture reaches its exact bound");
    }

    private static void FillPending(ProgressionService service)
    {
        for(int i=0;i<ProgressionService.PendingLootCapacity;i++)service.Profile.pendingLoot.Add(Plain(Rarity.Epic));
        service.Save();
        Check(service.Profile.pendingLoot.Count==0,"legacy overflow automatically joins visible bag");
    }

    private static ProgressionService Reload(ProgressionService service)
    {
        var restored = new ProgressionService(service.SaveDirectory);
        Check(restored.Load(), "save roundtrip succeeds");
        return restored;
    }

    private static void SpecializationIsExclusiveAndCampOnly()
    {
        var service = Fresh();
        int gold = service.Profile.gold;
        Check(!service.SetSpecialization(ElementalistSpecialization.Shatter, false), "field switching refused");
        Check(service.Profile.specialization == ElementalistSpecialization.None, "refused switch preserves original");
        Check(service.SetSpecialization(ElementalistSpecialization.Shatter, true), "camp shatter switch succeeds");
        Check(service.SetSpecialization(ElementalistSpecialization.Burn, true), "camp burn replaces shatter");
        Check(service.Profile.specialization == ElementalistSpecialization.Burn && service.Profile.gold == gold, "specialization is exclusive and free");
        Check(Reload(service).Profile.specialization == ElementalistSpecialization.Burn, "specialization persists");
        Check(!service.SetSpecialization((ElementalistSpecialization)999, true), "invalid enum refused");
        Check(service.SetSpecialization(ElementalistSpecialization.None, true), "baseline can be restored");
        var other = Fresh(HeroClass.Ranger);
        Check(!other.SetSpecialization(ElementalistSpecialization.Burn, true), "other classes cannot activate elementalist specialization");
        foreach (ElementalistSpecialization value in Enum.GetValues(typeof(ElementalistSpecialization)))
            Check(!string.IsNullOrEmpty(BuildCatalog.SpecializationDescription(value)), "each specialization explains its contract");
    }

    private static void EveryMechanicIsEquippedAndClassScoped()
    {
        foreach (HeroClass hero in Enum.GetValues(typeof(HeroClass)))
        {
            var service = Fresh(hero);
            EquipmentMechanic[] mechanics = BuildCatalog.MechanicsFor(hero);
            Check(mechanics.Length == (hero == HeroClass.Arcanist ? 2 : 1), "class has its intended build choices");
            foreach (EquipmentMechanic mechanic in mechanics)
            {
                ItemData previous = service.Equipped(BuildCatalog.MechanicSlot(mechanic));
                ItemData item = service.CreateMechanicItem(mechanic);
                Check(item.mechanic == mechanic && item.locked && item.rarity == Rarity.Epic, "crafted mechanic is identified and protected");
                Check(BuildCatalog.MechanicClass(mechanic) == hero && item.slot == BuildCatalog.MechanicSlot(mechanic), "class and slot catalog agree");
                Check(BuildCatalog.MechanicDescription(mechanic).Length > 20 && BuildCatalog.MechanicSource(mechanic).Contains("12"), "codex includes real effect tradeoff and targeted source");
                Check(!service.HasMechanic(mechanic), "bag ownership does not activate effect");
                Check(service.CollectLoot(item) && service.HasDiscoveredMechanic(mechanic), "collection records discovery");
                Check(service.HasMechanic(mechanic)&&service.Attachment(mechanic)!=null, "new mechanism drop unlocks independent mounted attachment");
                Check(service.Equip(item.id) && service.HasMechanic(mechanic), "equipping old gear retains effect");
                Check(Reload(service).HasMechanic(mechanic), "mechanic effect survives save load");
                Check(service.Equip(previous.id) && service.HasMechanic(mechanic), "removing gear retains independent attachment effect");
                Check(service.SetAttachmentMounted(mechanic,false,true)&&!service.HasMechanic(mechanic),"explicit unmount disables even matching legacy gear");
                Check(!service.Sell(item.id), "automatic item lock protects individual sale");
                Check(service.SetItemLocked(item.id, false) && service.Sell(item.id), "explicit unlock permits intentional individual sale");
                Check(service.HasDiscoveredMechanic(mechanic), "codex discovery persists after intentional sale");
            }
            EquipmentMechanic incompatible = hero == HeroClass.Vanguard ? EquipmentMechanic.FrostEcho : EquipmentMechanic.ReturningBlade;
            ItemData foreign = service.CreateMechanicItem(incompatible);
            Check(service.CollectLoot(foreign) && service.Equip(foreign.id) && !service.HasMechanic(incompatible), "foreign mechanic never activates on wrong class");
            Check(!service.HasMechanic(EquipmentMechanic.None) && !service.HasMechanic((EquipmentMechanic)99), "invalid and none mechanics are inactive");
        }
    }

    private static void ProtectedOverflowSurvivesReloadAndRefusal()
    {
        var service=Fresh();FillBag(service);int gold=service.Profile.gold;
        var epic=Plain(Rarity.Epic);Check(service.CollectLoot(epic),"protected full-bag reward is admitted");
        Check(service.Profile.inventory.Exists(x=>x.id==epic.id)&&service.Profile.pendingLoot.Count==0&&service.Profile.gold==gold,"reward stays owned and visible with no sale");
        var restored=Reload(service);Check(restored.Profile.inventory.Exists(x=>x.id==epic.id)&&!restored.CollectLoot(epic),"durable identity prevents duplicate reward");
        Check(!restored.ClaimPendingLoot(epic.id)&&restored.ClaimAllPendingLoot()==0,"legacy claim never grants again");
        var common=Plain();Check(restored.CollectLoot(common)&&restored.Profile.gold==gold,"ordinary overflow is preserved too");
        Check(restored.BulkSellLowQuality()>0&&restored.Profile.inventory.Exists(x=>x.id==epic.id),"explicit cleanup retains precious reward");
    }

    private static void AutoSellAndBulkNeverConsumeProtectedGear()
    {
        var service=Fresh();Check(!service.SetAutoSell(Rarity.Common,true)&&!service.SetAutoSell(Rarity.Rare,true),"removed autosell cannot be enabled");
        service.Profile.autoSellCommon=service.Profile.autoSellRare=true;service.Save();Check(!service.Profile.autoSellCommon&&!service.Profile.autoSellRare,"old settings migrate disabled");
        int gold=service.Profile.gold;var common=Plain();var rare=Plain(Rarity.Rare);Check(service.CollectLoot(common)&&service.CollectLoot(rare)&&service.Profile.gold==gold,"all rewards stay items");
        Check(service.BulkSellLowQuality()==2,"only explicit cleanup sells ordinary idle rewards");
        var locked=Plain();locked.locked=true;var mechanic=service.CreateMechanicItem(EquipmentMechanic.FrostEcho);
        Check(service.CollectLoot(locked)&&service.CollectLoot(mechanic)&&service.BulkSellLowQuality()==0,"equipped, locks and mechanisms stay protected");
        Check(service.SetItemLocked(locked.id,false)&&service.Sell(locked.id),"explicit unlock and sale remain available");
        Check(Reload(service).Profile.inventory.Exists(x=>x.id==mechanic.id),"mechanism reward survives cleanup and reload");
    }

    private static void FirstClearAndExchangeHaveDurableBudgets()
    {
        var service = Fresh();
        Check(!service.ClaimFirstClearReward(EquipmentMechanic.FrostEcho), "no reward before first clear");
        service.Profile.clearedRuns = 1;
        service.PrepareDungeonChest();
        Check(service.Profile.pendingFirstClearReward && service.Profile.mechanicMaterials == 3, "first clear offers persistent choice plus materials");
        service.PrepareDungeonChest();
        Check(service.Profile.mechanicMaterials == 3, "repeating chest preparation cannot farm materials");
        service.OpenDungeonChest();
        service.PrepareDungeonChest();
        Check(service.Profile.mechanicMaterials == 4, "opening adds one chest fragment; repreparing does not repeat base clear materials");
        Check(!service.ClaimFirstClearReward(EquipmentMechanic.ReturningBlade) && service.Profile.pendingFirstClearReward, "foreign choice does not consume first reward");
        FillBag(service); FillPending(service);
        Check(service.ClaimFirstClearReward(EquipmentMechanic.CinderTrail) && service.Attachment(EquipmentMechanic.CinderTrail)!=null, "full storage allows independent first-clear attachment");
        service.BulkSellLowQuality();
        Check(!service.ClaimFirstClearReward(EquipmentMechanic.CinderTrail), "same first-clear attachment cannot be granted twice");
        Check(service.Profile.firstClearRewardClaimed && !service.Profile.pendingFirstClearReward, "first reward consumed only on accepted acquisition");
        Check(!service.ClaimFirstClearReward(EquipmentMechanic.FrostEcho), "first-clear reward cannot be claimed twice");
        service = Reload(service);
        Check(service.Profile.firstClearRewardClaimed && service.HasDiscoveredMechanic(EquipmentMechanic.CinderTrail), "first-clear state and codex persist");
        for (int clear = 2; clear <= 4; clear++) { service.Profile.clearedRuns = clear; service.PrepareDungeonChest(); }
        Check(service.Profile.mechanicMaterials == 13, "four base clears plus one opened box total13 materials");
        Check(!service.ExchangeMechanic(EquipmentMechanic.ReturningBlade) && service.Profile.mechanicMaterials == 13, "wrong-class exchange has no cost");
        service.Profile.level = 40; service.Save();
        Check(service.ExchangeMechanic(EquipmentMechanic.FrostEcho) && service.Profile.mechanicMaterials == 1, "targeted exchange spends12 once and retains added chest fragment");
        Check(service.Attachment(EquipmentMechanic.FrostEcho).level == 40, "exchange produces chosen current-level independent attachment");
        Check(!service.ExchangeMechanic(EquipmentMechanic.FrostEcho), "insufficient materials cannot go negative");
        FillBag(service);
        service.Profile.mechanicMaterials = 12; service.Save();
        Check(!service.ExchangeMechanic(EquipmentMechanic.FrostEcho) && service.Profile.mechanicMaterials == 12, "duplicate attachment exchange leaves currency intact even with full bag");
    }

    private static void EqualSlotUpgradePricesCloseTransferDiscount()
    {
        var service = Fresh();
        foreach (ItemSlot slot in Enum.GetValues(typeof(ItemSlot)))
        {
            ItemData cheap = Plain(Rarity.Common, slot, 1), expensive = Plain(Rarity.Legendary, slot, 100);
            for (int rank = 0; rank <= ProgressionService.MaximumUpgrade; rank++)
            {
                service.Profile.slotUpgradeRanks[(int)slot] = rank;
                cheap.upgradeLevel = 0; expensive.upgradeLevel = 10;
                Check(service.UpgradeCost(cheap) == service.UpgradeCost(expensive), "equal slot/rank costs ignore level and rarity");
            }
        }
        service.Profile.slotUpgradeRanks = new int[3];
        service.Profile.level = 100; service.Profile.gold = 100000; service.Save();
        ItemData donor = Plain(Rarity.Common, ItemSlot.Weapon, 1), target = Plain(Rarity.Legendary, ItemSlot.Weapon, 100);
        target.attack = 1000;
        Check(service.CollectLoot(donor) && service.CollectLoot(target), "transfer fixtures collected");
        int expected = service.UpgradeCost(target), before = service.Profile.gold;
        Check(service.Upgrade(donor.id) && service.Profile.gold == before - expected, "upgrading starter donor costs same as destination");
        Check(service.Equip(target.id) && donor.upgradeLevel == 0 && target.upgradeLevel == 1 && service.SlotUpgradeRank(ItemSlot.Weapon) == 1, "replacement automatically applies permanent slot rank");
        Check(service.Profile.gold == before - expected && target.attack > 1000, "free transfer applies normal target scaling with no discount exploit");
    }

    private static void LegacyUpgradesAndRanksRemainIntact()
    {
        var service = Fresh();
        service.Profile.level = 50; service.Profile.skillRanks[0] = 0; service.Profile.skillRanks[9] = 2;
        ItemData item = service.Equipped(ItemSlot.Weapon);
        item.upgradeLevel = 7; item.attack = 123; item.upgradeBaseInitialized = false; item.balanceRevision = 0;
        service.Profile.skillPoints = int.MaxValue;
        service.Profile.slotUpgradesInitialized = false;
        service.Save();
        string id = item.id;
        var restored = Reload(service);
        ItemData legacy = restored.Profile.inventory.Find(value => value.id == id);
        int migratedAttack = legacy.attack;
        Check(legacy.upgradeLevel == 7 && legacy.balanceRevision == 1 && migratedAttack > 0 && migratedAttack < 123, "legacy paid rank preserved while compounded inflation is migrated once");
        Check(restored.Profile.skillRanks[9] == 2 && restored.Profile.skillPoints == 47, "learned legacy ultimate and true earned point balance survive new prerequisites");
        ItemData target = Plain(); restored.CollectLoot(target);
        Check(restored.Equip(target.id) && restored.Equip(id), "legacy anchored enhancement is reversibly transferable");
        Check(legacy.upgradeLevel == 7 && legacy.attack == migratedAttack, "roundtrip restores exact legacy anchor without rounding loss");
        Check(Reload(restored).Profile.inventory.Find(value => value.id == id).attack == migratedAttack, "legacy anchor persists again");
    }

    private static void MasteryUsesExactlyTheRemainingPointBudget()
    {
        var service = Fresh();
        Check(!service.LearnMastery(MasteryType.Offense), "low-level mastery unavailable");
        service.Profile.level = 50; service.Save();
        for (int i=0;i<15;i++) Check(service.LearnMastery(MasteryType.Offense), "mastery opens in midgame before every skill is learned");
        Check(!service.LearnMastery(MasteryType.Offense), "level50 segment cap is15");
        Check(service.ResetMastery(true), "midgame refund available");
        service.Profile.level = 100;
        for (int skill = 0; skill < GameBalance.SkillCount; skill++) service.Profile.skillRanks[skill] = 3;
        service.Profile.skillPoints = int.MaxValue; service.Save();
        Check(service.Profile.skillPoints == 69, "lifetime budget still69 after all skills");
        StatBlock baseline = service.GetStats();
        for (int rank=0;rank<35;rank++) Check(service.LearnMastery(MasteryType.Offense), "offense consumes35 points");
        Check(!service.LearnMastery(MasteryType.Offense), "track capped35");
        for (int rank=0;rank<34;rank++) Check(service.LearnMastery(MasteryType.Vitality), "vitality competes for remaining34");
        Check(!service.LearnMastery(MasteryType.Guard) && !service.LearnMastery(MasteryType.Technique), "four directions cannot all be filled");
        Check(service.SelectMasteryCore(MasteryType.Offense,true)&&service.SelectMasteryCore(MasteryType.Vitality,true), "eligible cores can replace each other");
        Check(!service.HasMasteryCore(MasteryType.Offense)&&service.HasMasteryCore(MasteryType.Vitality), "one active core only");
        Check(!service.SelectMasteryCore(MasteryType.Guard,true)&&!service.SelectMasteryCore(MasteryType.Offense,false), "uninvested or out of camp core refused");
        StatBlock mastered=service.GetStats();
        Check(Near(mastered.Damage,baseline.Damage*1.105f)&&Near(mastered.MaxHealth,baseline.MaxHealth*1.17f), "bounded stat gains reflect investment");
        service=Reload(service); Check(service.Profile.skillPoints==0&&service.Profile.masteryRanks[1]==34, "load does not mint or lose spent points");
        service.GrantExperience(int.MaxValue); Check(service.Profile.skillPoints==0,"level cap cannot mint points");
        Check(!service.ResetMastery(false)&&service.ResetMastery(true)&&service.Profile.skillPoints==69,"camp refund exactly69");
        Check(service.Profile.masteryCore==-1&&Near(service.GetStats().Damage,baseline.Damage),"reset clears selected core and stats");
    }

    private static void CorruptNewFieldsAreBoundedAndRepaired()
    {
        var service = Fresh();
        JsonNode json = JsonNode.Parse(File.ReadAllText(service.SaveFilePath));
        JsonNode profile = json["profile"];
        profile["specialization"] = 99;
        profile["masteryRanks"] = new JsonArray(999, -8, 999);
        profile["skillPoints"] = int.MaxValue;
        profile["mechanicMaterials"] = -2;
        profile["materialRewardedClears"] = int.MaxValue;
        profile["pendingFirstClearReward"] = true;
        profile["discoveredMechanics"] = new JsonArray(1, 1, 999, 0);
        profile["pendingLoot"] = null;
        File.WriteAllText(service.SaveFilePath, json.ToJsonString());
        Check(service.Load(), "malformed new fields repair without discarding valid legacy character");
        Check(service.Profile.specialization == ElementalistSpecialization.None && service.Profile.skillPoints == 0, "invalid spec and forged free points neutralized");
        Check(service.Profile.masteryRanks.Length == 4 && Array.TrueForAll(service.Profile.masteryRanks, rank => rank == 0), "ineligible or corrupt mastery refunded into lawful budget only");
        Check(service.Profile.mechanicMaterials == 0 && service.Profile.materialRewardedClears == 0 && !service.Profile.pendingFirstClearReward, "negative materials and unearned first clear repaired");
        Check(service.Profile.pendingLoot.Count == 0 && service.Profile.discoveredMechanics.Count == 1, "null queue and duplicate/invalid codex entries sanitized");
    }

    private static void ChestReceiptIsExactlyOnceAcrossReloadAndFailedWrites()
    {
        var service = Fresh();
        service.PrepareDungeonChest();
        string saved = File.ReadAllText(service.SaveFilePath);
        int gold = service.Profile.gold, fashions = service.Profile.fashions.Count, changes = 0;
        service.Changed += () => changes++;
        string blockedTemp = service.SaveFilePath + ".tmp";
        Directory.CreateDirectory(blockedTemp);
        Check(service.OpenDungeonChest() == null, "failed durable write does not open chest");
        Check(service.Profile.pendingFashionChest && !service.Profile.pendingChestReveal && service.LastChestReward == null, "write failure publishes neither consumption nor animation receipt");
        Check(service.Profile.gold == gold && service.Profile.fashions.Count == fashions && changes == 0, "write failure grants nothing and emits no reward event");
        Check(File.ReadAllText(service.SaveFilePath) == saved, "failed open leaves prior durable character untouched");
        Directory.Delete(blockedTemp);
        string text = service.OpenDungeonChest();
        ChestReward receipt = service.LastChestReward;
        Check(text != null && receipt != null && receipt.Id.Length == 32 && receipt.choice == -1 && changes == 1, "successful open saves one typed receipt before one event");
        Check(service.Profile.gold == gold + receipt.Gold && receipt.Gold >= 60 && receipt.Gold <= 100, "first reward receipt matches gold actually granted");
        Check(!service.Profile.pendingFashionChest && service.Profile.pendingChestReveal, "chest availability and reveal are separate persistent states");
        Check(receipt.Rarity.HasValue == receipt.Slot.HasValue && receipt.Name.Length > 0, "gold-only receipt distinguishes missing rarity and slot");
        if (receipt.Rarity.HasValue) Check(service.Profile.fashions.Count == fashions + 1, "fashion typed receipt matches collection grant");
        else Check(service.Profile.fashions.Count == fashions, "gold-only receipt grants no fashion");
        gold = service.Profile.gold;
        Check(service.OpenDungeonChest() == null && service.OpenDungeonChest() == null && service.Profile.gold == gold && changes == 1, "duplicate chest clicks neither reroll nor grant again");
        service = Reload(service);
        Check(service.Profile.pendingChestReveal && service.LastChestReward.Id == receipt.Id && service.LastChestReward.Gold == receipt.Gold && service.LastChestReward.choice == -1, "interrupted animation resumes the identical saved receipt");
        Check(service.OpenDungeonChest() == null && service.Profile.gold == gold, "reload cannot reopen already granted chest");
        service.PrepareDungeonChest();
        Check(service.Profile.pendingFashionChest && service.OpenDungeonChest() == null, "new chest cannot overwrite previous unacknowledged reveal");
        Directory.CreateDirectory(blockedTemp);
        Check(!service.AcknowledgeChestReward() && service.Profile.pendingChestReveal, "failed skip/close save keeps reveal resumable");
        Directory.Delete(blockedTemp);
        Check(service.AcknowledgeChestReward() && !service.Profile.pendingChestReveal && service.LastChestReward.Id == receipt.Id, "skip/close acknowledges without deleting receipt or changing rewards");
        Check(!service.AcknowledgeChestReward() && service.Profile.gold == gold, "duplicate skip/close is a harmless refusal");
        service = Reload(service);
        Check(!service.Profile.pendingChestReveal && service.LastChestReward.Id == receipt.Id, "acknowledged animation stays closed after reload");
        Check(service.OpenDungeonChest() != null && service.LastChestReward.Id != receipt.Id, "next earned chest gets one distinct receipt");
        int[] counts = new int[5];
        for (int roll = 0; roll < 100; roll++)
        {
            Rarity? rarity = ProgressionService.RollFashionRarity(roll);
            counts[rarity.HasValue ? (int)rarity.Value : 4]++;
        }
        Check(counts[0] == 22 && counts[1] == 12 && counts[2] == 5 && counts[3] == 1 && counts[4] == 60, "animation support preserves exact original fashion odds");
    }

    private static void ProtectedAcquisitionsRollbackFailedWrites()
    {
        var service=Fresh();FillBag(service);var mechanic=service.CreateMechanicItem(EquipmentMechanic.FrostEcho);
        string blocked=service.SaveFilePath+".tmp";int count=service.Profile.inventory.Count,gold=service.Profile.gold;
        Directory.CreateDirectory(blocked);Check(!service.CollectLoot(mechanic)&&service.Profile.inventory.Count==count&&!service.HasDiscoveredMechanic(mechanic.mechanic),"failed pickup preserves world ownership and discovery");
        Check(service.Profile.gold==gold,"failed pickup never sells reward");Directory.Delete(blocked);
        Check(service.CollectLoot(mechanic)&&service.Profile.inventory.Exists(x=>x.id==mechanic.id),"same reward retries directly into visible inventory");
        Check(!service.CollectLoot(mechanic),"replayed callback cannot pay twice");
    }

    private static void DeepCopiesPreserveBuildAndRewardMetadata()
    {
        var service = Fresh();
        Check(service.SetSpecialization(ElementalistSpecialization.Shatter, true), "copy fixture chooses specialization");
        ItemData mechanic = service.CreateMechanicItem(EquipmentMechanic.FrostEcho);
        Check(service.CollectLoot(mechanic) && service.Equip(mechanic.id), "copy fixture equips mechanic");
        ItemData preview = service.PreviewUpgrade(mechanic, 6);
        Check(preview != mechanic && preview.locked == mechanic.locked && preview.mechanic == mechanic.mechanic && preview.id == mechanic.id, "upgrade preview preserves mechanic and lock metadata in an independent copy");
        Check(mechanic.upgradeLevel == 0 && preview.upgradeLevel == 6, "preview does not mutate owned mechanic");
        FillBag(service);
        ItemData pending = service.CreateMechanicItem(EquipmentMechanic.CinderTrail);
        Check(service.CollectLoot(pending), "copy fixture has protected pending item");
        service.PrepareDungeonChest(); service.OpenDungeonChest();
        string oldPath = service.SaveFilePath, oldBytes = File.ReadAllText(oldPath), receipt = service.LastChestReward.Id;
        GameProfile oldProfile = service.Profile;
        Check(service.SaveAsNewSlot() && service.Profile != oldProfile && service.SaveFilePath != oldPath, "save-as makes a separate full-profile copy");
        Check(File.ReadAllText(oldPath) == oldBytes, "save-as leaves original character byte-for-byte intact");
        Check(service.HasMechanic(EquipmentMechanic.FrostEcho) && service.Profile.specialization == ElementalistSpecialization.Shatter, "copy preserves active build state");
        Check(service.Profile.pendingLoot.Count == 0 && service.Profile.inventory.Exists(x=>x.id==pending.id&&x.locked), "copy preserves pending identity and lock");
        Check(service.Profile.pendingChestReveal && service.LastChestReward.Id == receipt, "copy preserves unacknowledged receipt without rerolling");
        Check(service.Load() && service.LastChestReward.Id == receipt && service.HasMechanic(EquipmentMechanic.FrostEcho), "selected copied slot reload preserves full metadata");
    }

    private static void RecoveryMailboxProtectsRetiringGroundLoot()
    {
        var service=Fresh();FillBag(service);var first=Plain(Rarity.Epic);first.locked=true;var second=Plain(Rarity.Legendary);
        string blocked=service.SaveFilePath+".tmp";int count=service.Profile.inventory.Count;Directory.CreateDirectory(blocked);
        Check(!service.PreserveGroundLoot(new[]{first,second})&&service.Profile.inventory.Count==count,"failed ground capture is atomic");Directory.Delete(blocked);
        Check(service.PreserveGroundLoot(new[]{first,first,second,service.Profile.inventory[0]})&&service.Profile.inventory.Count==count+2,"capture goes directly to bag once per identity");
        var restored=Reload(service);Check(restored.Profile.inventory.Exists(x=>x.id==first.id&&x.locked)&&restored.Profile.inventory.Exists(x=>x.id==second.id)&&restored.RecoveryLootCount==0,"preserved ground rewards survive reload without claim gate");
        Check(restored.PreserveGroundLoot(new[]{first,second})&&restored.Profile.inventory.Count==count+2,"repeated capture never duplicates");
    }

    private static void SlotTrainingMigratesEveryStorageAndCannotBeSold()
    {
        var service = Fresh();
        JsonNode json = JsonNode.Parse(File.ReadAllText(service.SaveFilePath));
        JsonObject profile = json["profile"].AsObject();
        profile["level"] = 100; profile["gold"] = 100000;
        profile.Remove("slotUpgradeRanks"); profile.Remove("slotUpgradesInitialized");
        ItemData oldWeapon = Plain(); oldWeapon.upgradeLevel = 7; oldWeapon.attack = 123;
        ItemData oldArmor = Plain(Rarity.Epic, ItemSlot.Armor); oldArmor.upgradeLevel = 9; oldArmor.defense = 99; oldArmor.health = 500;
        ItemData oldRelic = Plain(Rarity.Legendary, ItemSlot.Relic); oldRelic.upgradeLevel = 6; oldRelic.attack = 30; oldRelic.health = 150;
        profile["inventory"].AsArray().Add(JsonNode.Parse(UnityEngine.JsonUtility.ToJson(oldWeapon, true)));
        profile["pendingLoot"] = new JsonArray(JsonNode.Parse(UnityEngine.JsonUtility.ToJson(oldArmor, true)));
        profile["recoveryLoot"] = new JsonArray(JsonNode.Parse(UnityEngine.JsonUtility.ToJson(oldRelic, true)));
        File.WriteAllText(service.SaveFilePath, json.ToJsonString());
        Check(service.Load() && service.Profile.slotUpgradesInitialized, "legacy item investments initialize permanent slots once");
        Check(service.SlotUpgradeRank(ItemSlot.Weapon) == 7 && service.SlotUpgradeRank(ItemSlot.Armor) == 9 && service.SlotUpgradeRank(ItemSlot.Relic) == 6, "migration takes independent maxima across bag, pending and recovery sources");
        ItemData weapon = service.Profile.inventory.Find(item => item.id == oldWeapon.id);
        Check(weapon.upgradeLevel == 0 && weapon.locked && weapon.balanceRevision == 1 && weapon.upgradeAnchorLevel == 0, "unequipped legacy cache resets but immutable anchor and protective lock survive");
        Check(service.PreviewEquippedItem(weapon).attack == CombatBalance.UpgradeValue(weapon.baseAttack,7) && service.PreviewEquippedItem(weapon).upgradeLevel == 7, "candidate preview restores exact legacy stats at migrated slot rank");
        Check(service.Equip(weapon.id) && weapon.attack == CombatBalance.UpgradeValue(weapon.baseAttack,7) && weapon.upgradeLevel == 7, "equipping migrated item restores original exact anchored strength");
        Check(!service.ClaimPendingLoot(oldArmor.id) && service.Equip(oldArmor.id), "pending migration source can be claimed and automatically equipped");
        Check(service.Equipped(ItemSlot.Armor).defense == CombatBalance.UpgradeValue(service.Equipped(ItemSlot.Armor).baseDefense,9) && service.Equipped(ItemSlot.Armor).health == CombatBalance.UpgradeValue(service.Equipped(ItemSlot.Armor).baseHealth,9,2) && service.Equipped(ItemSlot.Armor).upgradeLevel == 9, "pending item's own distinct armor baseline determines enhancement");
        Check(!service.ClaimRecoveryLoot(oldRelic.id) && service.Equip(oldRelic.id), "recovery migration source automatically inherits its slot after claim");
        Check(service.Equipped(ItemSlot.Relic).attack == CombatBalance.UpgradeValue(service.Equipped(ItemSlot.Relic).baseAttack,6) && service.Equipped(ItemSlot.Relic).health == CombatBalance.UpgradeValue(service.Equipped(ItemSlot.Relic).baseHealth,6,2), "recovery item's exact anchor remains intact");
        for (int cycle = 0; cycle < 4; cycle++)
        {
            service.Save(); service = Reload(service);
            Check(service.SlotUpgradeRank(ItemSlot.Weapon) == 7 && service.SlotUpgradeRank(ItemSlot.Armor) == 9 && service.SlotUpgradeRank(ItemSlot.Relic) == 6, "repeated loads never sum or remigrate existing training");
        }
        ItemData weak = Plain(), strong = Plain(Rarity.Legendary); strong.attack = 1000;
        Check(service.CollectLoot(weak) && service.CollectLoot(strong), "two different gear bases remain freely comparable");
        ItemData weakPreview = service.PreviewEquippedItem(weak), strongPreview = service.PreviewEquippedItem(strong);
        Check(weakPreview.upgradeLevel == 7 && strongPreview.upgradeLevel == 7 && strongPreview.attack - strong.attack > weakPreview.attack - weak.attack, "same slot rank gives different absolute boosts according to each item's own base");
        int expectedSale = service.SellValue(weak);
        Check(service.SellValue(weakPreview) == expectedSale, "effective training rank never increases sale value");
        Check(service.Equip(weak.id) && service.Profile.inventory.Find(x=>x.id==weak.id).upgradeLevel == 7 && service.Equip(strong.id) && service.Profile.inventory.Find(x=>x.id==weak.id).upgradeLevel == 0, "replacement applies and releases cache without transferring an investment");
        int gold = service.Profile.gold;
        Check(service.Sell(weak.id) && service.Profile.gold == gold + expectedSale && service.SlotUpgradeRank(ItemSlot.Weapon) == 7, "selling former equipment cannot sell or refund the permanent slot rank");
        for (int cycle = 0; cycle < 6; cycle++)
        {
            ItemData cheap = Plain(); int sale = service.SellValue(cheap);
            Check(service.CollectLoot(cheap) && service.Equip(cheap.id) && service.Equip(strong.id) && service.SellValue(cheap) == sale && service.Sell(cheap.id), "repeated cheap-item equip/sale cannot monetize free inheritance");
        }
        Check(service.SlotUpgradeRank(ItemSlot.Weapon) == 7 && service.Equipped(ItemSlot.Weapon).attack == strongPreview.attack, "sell cycles do not compound damage or reduce training");
        service.SetAutoSell(Rarity.Common, true);
        ItemData ordinary = Plain(); int count = service.Profile.inventory.Count;
        Check(service.CollectLoot(ordinary) && service.Profile.inventory.Count == count+1 && service.SlotUpgradeRank(ItemSlot.Weapon) == 7, "trained slots do not disable ordinary drop autosell");
        service.Profile.slotUpgradeRanks = new[] { int.MaxValue, -1, 6 }; service.Save();
        Check(service.SlotUpgradeRank(ItemSlot.Weapon) == 10 && service.SlotUpgradeRank(ItemSlot.Armor) == 0 && service.SlotUpgradeRank(ItemSlot.Relic) == 6, "corrupt slot ranks are independently clamped to legal bounds");
        Check(service.UpgradeCost(strong) == 0 && !service.Upgrade(strong.id), "maximum slot rank rejects upgrades independent of item cache");
    }

    private static void SlotTrainingAndEquipmentWritesAreAtomic()
    {
        var service = Fresh(); service.Profile.level = 100; service.Profile.gold = 100000; service.Save();
        ItemData original = service.Equipped(ItemSlot.Weapon), candidate = Plain(Rarity.Legendary); candidate.attack = 200;
        Check(service.CollectLoot(candidate) && service.Upgrade(original.id), "atomic fixture owns one trained slot and a replacement");
        original = service.Profile.inventory.Find(x=>x.id==original.id);
        string originalId = original.id;
        int oldAttack = original.attack, oldGold = service.Profile.gold, oldRank = service.SlotUpgradeRank(ItemSlot.Weapon), events = 0;
        service.Changed += () => events++;
        string blocked = service.SaveFilePath + ".tmp", saved = File.ReadAllText(service.SaveFilePath);
        Directory.CreateDirectory(blocked);
        Check(!service.Upgrade(candidate.id) && service.Profile.gold == oldGold && service.SlotUpgradeRank(ItemSlot.Weapon) == oldRank && original.attack == oldAttack && events == 0, "failed slot-training write rolls back charge, rank, equipped stats and event");
        Check(!service.Equip(candidate.id) && service.Equipped(ItemSlot.Weapon).id == originalId && original.attack == oldAttack && original.upgradeLevel == oldRank && candidate.upgradeLevel == 0 && candidate.attack == 200 && events == 0, "failed equip write restores both enhancement caches and equipment identity");
        Check(!service.Unequip(ItemSlot.Weapon) && service.Equipped(ItemSlot.Weapon).id == originalId && service.SlotUpgradeRank(ItemSlot.Weapon) == oldRank && events == 0, "failed unequip preserves equipment, training and event");
        Check(File.ReadAllText(service.SaveFilePath) == saved, "failed training or equip leaves previous durable save intact");
        Directory.Delete(blocked);
        int price = service.UpgradeCost(candidate);
        Check(service.Upgrade(candidate.id) && service.SlotUpgradeRank(ItemSlot.Weapon) == oldRank + 1 && service.Profile.gold == oldGold - price && events == 1, "retry trains slot exactly once after disk recovery");
        ItemData expected = service.PreviewEquippedItem(candidate);
        Check(service.Equip(candidate.id) && candidate.attack == expected.attack && candidate.upgradeLevel == expected.upgradeLevel && events == 2, "retry applies prospective exact stats once");
        service = Reload(service);
        Check(service.Equipped(ItemSlot.Weapon).id == candidate.id && service.SlotUpgradeRank(ItemSlot.Weapon) == oldRank + 1 && service.Equipped(ItemSlot.Weapon).attack == expected.attack, "atomic slot operation survives another load without duplicated growth");
        candidate=service.Equipped(ItemSlot.Weapon); int count=service.Profile.inventory.Count;
        Check(service.Unequip(ItemSlot.Weapon) && service.Equipped(ItemSlot.Weapon)==null && service.Profile.inventory.Count==count && service.SlotUpgradeRank(ItemSlot.Weapon)==oldRank+1, "unequip preserves ownership and permanent training");
        Check(service.Profile.inventory.Find(x=>x.id==candidate.id).upgradeLevel==0, "unequipped item returns to its base stats");
        service=Reload(service);
        Check(service.Equipped(ItemSlot.Weapon)==null && service.Equip(candidate.id) && service.Equipped(ItemSlot.Weapon).attack==expected.attack, "empty slot persists and re-equip inherits training exactly once");

    }

    private static bool Near(float first, float second) { return Math.Abs(first - second) < .01f; }
    private static void Check(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new Exception("Upgrade progression assertion " + assertions + " failed: " + message);
    }
}
