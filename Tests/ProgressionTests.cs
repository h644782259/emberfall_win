// Standalone checks for actual production progression code. These stubs only replace Unity's
// platform path, JSON transport, and colors; no gameplay behavior is copied into the tests.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using Emberfall;

namespace UnityEngine
{
    public static class Application { public static string persistentDataPath; }
    public static class Debug
    {
        public static void LogWarning(object message) { Console.WriteLine(message); }
        // These managed progression/chapter fixtures have no expected engine exceptions.
        // A caught production fault must fail the replay, not disappear into a no-op logger.
        public static void LogException(Exception exception)
        { throw new InvalidOperationException("Unexpected production exception logged at Unity boundary", exception); }
    }
    public struct Color { public Color(float r, float g, float b, float a = 1) { } }
    public static class JsonUtility
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions { IncludeFields = true, WriteIndented = true };
        public static string ToJson(object value, bool pretty) { return JsonSerializer.Serialize(value, value.GetType(), Options); }
        public static T FromJson<T>(string value)
        {
            try { return JsonSerializer.Deserialize<T>(value, Options); }
            catch (JsonException error) { throw new ArgumentException("Malformed JSON", error); }
        }
    }
}

public static class ProgressionTests
{
    private static string root;
    private static int cases;
    private static int assertions;

    public static string Run(string testDirectory)
    {
        root = testDirectory;
        cases = 0;
        assertions = 0;
        NewCharacterAndPersistence();
        FashionChestAndOdds();
        SkillPointsAndLeveling();
        InventoryAndEconomy();
        WorldLootIsCollectedOnce();
        HotbarDragMovesAndSwaps();
        ConsumableHotbarAndPersistence();
        SummonerHasDistinctStatsAndEquipment();
        UpgradeTransferAndPreview();
        LegacyUpgradesMigrateWithoutAttributeLoss();
        BackupAndCorruption();
        DamagedFieldsAreRepaired();
        UpperBoundsAndInvalidActions();
        EveryClassSkillUnlocksAtItsGate();
        EveryClassSkillRankRequiresItsLevel();
        SkillTreePrerequisitesAndBranches();
        PreviouslyLearnedSkillsSurviveNewPrerequisites();
        LegacyThreeSkillSaveMigratesWithoutLoss();
        MalformedSkillArraysAreRepaired();
        SkillLoadoutAssignmentAndPersistence();
        CustomHotbarKeysAndRepair();
        TenSkillRanksAndPointBudget();
        PassiveStatsAreImmediateAndPersistent();
        SaveTransfersBetweenIndependentDirectories();
        MultipleSaveSlotsAndSnapshots();
        SaveSlotRecoveryAndFailures();
        NewSlotsWithoutLegacyAreDiscoverable();
        return "PASS: " + assertions + " assertions across " + cases + " isolated progression scenarios.";
    }

    private static ProgressionService Fresh(HeroClass heroClass = HeroClass.Vanguard)
    {
        UnityEngine.Application.persistentDataPath = Path.Combine(root, "case-" + (++cases));
        var result = new ProgressionService();
        Check(!result.HasSave, "constructor does not create a save");
        result.NewGame(heroClass);
        return result;
    }

    private static void FashionChestAndOdds()
    {
        int[] counts = new int[5];
        for (int roll = 0; roll < 100; roll++)
        {
            Rarity? rarity = ProgressionService.RollFashionRarity(roll);
            counts[rarity.HasValue ? (int)rarity.Value : 4]++;
        }
        Check(counts[0] == 22 && counts[1] == 12 && counts[2] == 5 && counts[3] == 1 && counts[4] == 60,
            "chest rarity distribution uses absolute chances per opening, including 1% legendary");
        var service = Fresh();
        Check(service.OpenDungeonChest() == null, "chest cannot be opened before a clear");
        service.PrepareDungeonChest();
        Check(service.Load() && service.Profile.pendingFashionChest, "unopened chest survives save and reload");
        int priorGold = service.Profile.gold;
        string result = service.OpenDungeonChest();
        Check(!string.IsNullOrEmpty(result) && service.Profile.gold >= priorGold + 60 && !service.Profile.pendingFashionChest,
            "one chosen chest grants guaranteed gold and consumes the saved entitlement");
        Check(service.OpenDungeonChest() == null && service.Load() && !service.Profile.pendingFashionChest,
            "other chests cannot be claimed and entitlement stays consumed after reload");
        var wings = new FashionData { id = "fashion-0-3", slot = FashionSlot.Wings, rarity = Rarity.Legendary, name = "烬王之翼" };
        var weapon = new FashionData { id = "fashion-1-3", slot = FashionSlot.Weapon, rarity = Rarity.Legendary, name = "烬王兵装" };
        service.Profile.fashions.RemoveAll(value => value.id == wings.id || value.id == weapon.id);
        service.Profile.fashions.Clear();
        StatBlock baseStats = service.GetStats();
        service.Profile.fashions.Add(wings);
        service.Profile.fashions.Add(weapon);
        Check(service.EquipFashion(wings.id) && service.EquipFashion(weapon.id), "level-one character can wear both legendary fashion slots");
        StatBlock dressed = service.GetStats();
        Check(Math.Abs(dressed.MaxHealth / baseStats.MaxHealth - 1.12f) < .0001f &&
            Math.Abs(dressed.Armor / baseStats.Armor - 1.08f) < .0001f &&
            Math.Abs(dressed.Damage / baseStats.Damage - 1.09f) < .0001f &&
            Math.Abs(dressed.CritChance / baseStats.CritChance - 1.09f) < .0001f,
            "fashion bonuses multiply final stats by their listed percentages");
        Check(service.Load() && service.EquippedFashion(FashionSlot.Wings) != null &&
            service.EquippedFashion(FashionSlot.Weapon) != null, "fashion collection and both worn slots survive reload");
    }

    private static void MultipleSaveSlotsAndSnapshots()
    {
        var service = Fresh(HeroClass.Vanguard);
        service.AddGold(41);
        string legacyPath = service.SaveFilePath;
        string legacyBytes = File.ReadAllText(legacyPath);
        Check(service.GetSaveSlots().Count == 1 && service.GetSaveSlots()[0].Id == "legacy" && service.GetSaveSlots()[0].IsCurrent, "old save is listed as current legacy without renaming");
        int changes = 0;
        service.Changed += () => changes++;
        Check(service.CreateNewSlot(HeroClass.Ranger) && changes == 1 && service.Profile.heroClass == HeroClass.Ranger && service.Profile.gold == 60, "new role creates a selected independent save and emits one change");
        string rangerPath = service.SaveFilePath;
        string rangerId = service.GetSaveSlots().Find(slot => slot.IsCurrent).Id;
        Guid parsed;
        Check(Guid.TryParseExact(rangerId, "N", out parsed) && Path.GetFileName(rangerPath) == "emberfall-save-" + rangerId + ".json" && File.Exists(rangerPath + ".bak"), "new save uses canonical GuidN primary and its own backup");
        Check(File.ReadAllText(legacyPath) == legacyBytes && service.SaveDirectory == Path.GetDirectoryName(legacyPath), "creating a role preserves legacy bytes and the root save directory");
        service.AddGold(23);
        string rangerBytes = File.ReadAllText(rangerPath);
        Check(service.CreateNewSlot(HeroClass.Summoner), "another new role creates a third save");
        string summonerPath = service.SaveFilePath;
        string summonerId = service.GetSaveSlots().Find(slot => slot.IsCurrent).Id;
        Check(service.HasSave && service.GetSaveSlots().Count == 3 && summonerId != rangerId && service.Profile.heroClass == HeroClass.Summoner, "multiple roles coexist without a manifest");
        Check(File.ReadAllText(legacyPath) == legacyBytes && File.ReadAllText(rangerPath) == rangerBytes, "third role does not overwrite either existing character");
        Check(service.LoadSlot(rangerId.ToUpperInvariant()) && service.Profile.heroClass == HeroClass.Ranger && service.Profile.gold == 83 && service.SaveFilePath == rangerPath, "explicit slot selection loads and normalizes a valid GuidN");
        string summonerBytes = File.ReadAllText(summonerPath);
        service.AddGold(9);
        Check(File.ReadAllText(summonerPath) == summonerBytes && File.ReadAllText(legacyPath) == legacyBytes && service.Profile.gold == 92, "autosave writes only the chosen character");
        Check(service.Load() && service.Profile.gold == 92 && service.SaveFilePath == rangerPath, "parameterless Load reloads the selected slot within an active service");
        var freshReader = new ProgressionService(service.SaveDirectory);
        Check(freshReader.Load() && freshReader.Profile.heroClass == HeroClass.Vanguard && freshReader.Profile.gold == 101, "a fresh service parameterless Load preserves original legacy behavior");

        GameProfile oldProfile = service.Profile;
        string oldJson = UnityEngine.JsonUtility.ToJson(oldProfile, true);
        string oldSave = File.ReadAllText(rangerPath);
        changes = 0;
        Check(service.SaveAsNewSlot() && changes == 1 && service.GetSaveSlots().Count == 4, "Save As commits one new snapshot and selects it once");
        string snapshotPath = service.SaveFilePath;
        string snapshotId = service.GetSaveSlots().Find(slot => slot.IsCurrent).Id;
        Check(snapshotPath != rangerPath && !ReferenceEquals(oldProfile, service.Profile) && UnityEngine.JsonUtility.ToJson(service.Profile, true) == oldJson && File.ReadAllText(rangerPath) == oldSave, "snapshot deep-copies full progression and preserves the original slot bytes");
        service.Profile.inventory[0].name = "snapshot-specific item";
        service.AddGold(15);
        Check(oldProfile.inventory[0].name != service.Profile.inventory[0].name && File.ReadAllText(rangerPath) == oldSave, "later snapshot changes cannot mutate original items or file");

        DateTime start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        string[] paths = { legacyPath, rangerPath, summonerPath, snapshotPath };
        for (int i = 0; i < paths.Length; i++) { File.SetLastWriteTimeUtc(paths[i], start.AddDays(i)); File.SetLastWriteTimeUtc(paths[i] + ".bak", start.AddDays(i)); }
        File.WriteAllText(Path.Combine(service.SaveDirectory, "emberfall-save-not-a-guid.json"), "unrelated");
        File.WriteAllText(Path.Combine(service.SaveDirectory, "emberfall-save.json.tmp"), "interrupted temporary write");
        string currentJson = UnityEngine.JsonUtility.ToJson(service.Profile, true);
        changes = 0;
        List<SaveSlotInfo> slots = service.GetSaveSlots();
        Check(slots.Count == 4 && slots[0].Id == snapshotId && slots[1].Id == summonerId && slots[2].Id == rangerId && slots[3].Id == "legacy", "slot discovery ignores temp/unknown files and sorts newest first");
        Check(slots.TrueForAll(slot => slot.CanLoad && !slot.RecoveredFromBackup && slot.Level == 1 && slot.SavedAtUtc.Kind == DateTimeKind.Utc && !string.IsNullOrWhiteSpace(slot.DisplayName)) && slots.FindAll(slot => slot.IsCurrent).Count == 1, "slot metadata exposes readable character identity UTC dates and one current slot");
        Check(changes == 0 && currentJson == UnityEngine.JsonUtility.ToJson(service.Profile, true), "listing slots does not mutate the active profile or emit an event");

        string destination = Path.Combine(root, "case-" + (++cases));
        Directory.CreateDirectory(destination);
        foreach (string path in paths) { File.Copy(path, Path.Combine(destination, Path.GetFileName(path))); File.Copy(path + ".bak", Path.Combine(destination, Path.GetFileName(path) + ".bak")); }
        var migrated = new ProgressionService(destination);
        Check(migrated.GetSaveSlots().Count == 4 && migrated.HasSave, "copying only JSON and each backup discovers all slots on another installation");
        Check(migrated.LoadSlot(snapshotId) && migrated.Profile.gold == 107 && migrated.Profile.inventory[0].name == "snapshot-specific item", "copied snapshot retains all independent progress");
        Check(migrated.LoadSlot(rangerId) && migrated.Profile.gold == 92 && migrated.Profile.inventory[0].name != "snapshot-specific item" && migrated.LoadSlot("legacy") && migrated.Profile.gold == 101, "migration retains separate original and legacy characters without machine-bound index");
        Check(Directory.GetFiles(service.SaveDirectory, "*manifest*").Length == 0, "multiple saves require no manifest file");
    }

    private static void SaveSlotRecoveryAndFailures()
    {
        var service = Fresh(HeroClass.Arcanist);
        string legacyPath = service.SaveFilePath;
        Check(service.CreateNewSlot(HeroClass.Summoner), "recovery fixture creates independent slot");
        string slotPath = service.SaveFilePath;
        string slotId = service.GetSaveSlots().Find(slot => slot.IsCurrent).Id;
        service.AddGold(40);
        string goodBackup = File.ReadAllText(slotPath + ".bak");
        File.WriteAllText(slotPath, "damaged primary");
        SaveSlotInfo recoverable = service.GetSaveSlots().Find(slot => slot.Id == slotId);
        Check(recoverable.CanLoad && recoverable.RecoveredFromBackup && recoverable.HeroClass == HeroClass.Summoner && recoverable.Level == 1, "slot preview uses valid backup metadata when primary is damaged");
        Check(service.LoadSlot(slotId) && service.Profile.gold == 60 && service.LastError.Contains("备份"), "selected damaged slot restores its own backup with an explanation");
        service.AddGold(5);
        Check(File.ReadAllText(slotPath + ".bak") == goodBackup && service.Profile.gold == 65, "saving recovered slot preserves valid backup instead of corrupt primary");
        File.Delete(slotPath);
        Check(service.GetSaveSlots().Find(slot => slot.Id == slotId).RecoveredFromBackup && service.LoadSlot(slotId) && service.Profile.gold == 60, "backup-only slots remain discoverable and loadable");
        File.WriteAllText(slotPath, "bad");
        File.WriteAllText(slotPath + ".bak", "also bad");
        SaveSlotInfo broken = service.GetSaveSlots().Find(slot => slot.Id == slotId);
        Check(!broken.CanLoad && !broken.RecoveredFromBackup && broken.Level == 0 && service.HasSave, "fully damaged slot stays visible but unreadable");
        Check(service.LoadSlot("legacy") && service.Profile.heroClass == HeroClass.Arcanist, "a damaged slot does not prevent loading a good independent slot");

        GameProfile original = service.Profile;
        string before = UnityEngine.JsonUtility.ToJson(original, true);
        string saved = File.ReadAllText(legacyPath);
        int changes = 0;
        service.Changed += () => changes++;
        foreach (string invalid in new[] { null, "", "../emberfall-save", "..\\outside", "legacy/../outside", "Legacy", Guid.NewGuid().ToString("D"), slotId + ".json", Guid.NewGuid().ToString("N") })
            Check(!service.LoadSlot(invalid), "invalid or missing slot ID is rejected without path traversal");
        Check(!service.LoadSlot(slotId) && !service.CreateNewSlot((HeroClass)999), "bad slot and invalid new class are rejected");
        Check(ReferenceEquals(service.Profile, original) && UnityEngine.JsonUtility.ToJson(service.Profile, true) == before && service.SaveFilePath == legacyPath && changes == 0 && File.ReadAllText(legacyPath) == saved, "failed selection/new role leaves current profile path file and events untouched");
        string movedDirectory = service.SaveDirectory + "-preserved";
        Directory.Move(service.SaveDirectory, movedDirectory);
        File.WriteAllText(service.SaveDirectory, "blocks directory creation");
        try
        {
            Check(!service.SaveAsNewSlot() && !service.CreateNewSlot(HeroClass.Ranger), "real write failures reject Save As and new role creation");
            Check(ReferenceEquals(service.Profile, original) && UnityEngine.JsonUtility.ToJson(service.Profile, true) == before && service.SaveFilePath == legacyPath && changes == 0 && service.LastError.Contains("保存失败"), "write failure does not switch or destroy active state");
        }
        finally
        {
            File.Delete(service.SaveDirectory);
            Directory.Move(movedDirectory, service.SaveDirectory);
        }
        Check(File.ReadAllText(legacyPath) == saved && service.GetSaveSlots().Count == 2, "failed creation leaves originals intact and no phantom new save");
        File.WriteAllText(legacyPath, "broken legacy");
        File.WriteAllText(legacyPath + ".bak", "broken legacy backup");
        Check(service.HasSave && service.GetSaveSlots().TrueForAll(slot => !slot.CanLoad), "HasSave includes a directory containing only damaged slots");
    }

    private static void NewSlotsWithoutLegacyAreDiscoverable()
    {
        string directory = Path.Combine(root, "case-" + (++cases));
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory,"emberfall-save-legacy.json"),"not a canonical slot");
        File.WriteAllText(Path.Combine(directory,"emberfall-save.json.tmp"),"incomplete write");
        var service = new ProgressionService(directory);
        Check(!service.HasSave && service.GetSaveSlots().Count == 0, "unknown new-format legacy aliases and temporary files are not save slots");
        Check(service.CreateNewSlot(HeroClass.Summoner) && !File.Exists(Path.Combine(directory,"emberfall-save.json")), "first modern character needs no legacy placeholder file");
        string id = service.GetSaveSlots()[0].Id;
        var restarted = new ProgressionService(directory);
        Check(restarted.HasSave && restarted.GetSaveSlots().Count == 1 && restarted.LoadSlot(id) && restarted.Profile.heroClass == HeroClass.Summoner, "fresh process discovers and loads modern slots even when no legacy file exists");
    }

    private static void NewCharacterAndPersistence()
    {
        var service = Fresh(HeroClass.Ranger);
        Check(service.HasSave, "new game saves");
        Check(service.Profile.level == 1 && service.Profile.gold == 60 && service.Profile.potions == 5, "initial progression");
        Check(service.Profile.inventory.Count == 3, "three starter items");
        Check(service.Equipped(ItemSlot.Weapon) != null && service.Equipped(ItemSlot.Armor) != null && service.Equipped(ItemSlot.Relic) != null, "all starter slots equipped");
        service.AddGold(41);
        service.UsePotion();
        var restored = new ProgressionService();
        Check(restored.Load(), "load saved character");
        Check(restored.Profile.heroClass == HeroClass.Ranger && restored.Profile.gold == 101 && restored.Profile.potions == 4, "class and currencies persist");
        Check(restored.Equipped(ItemSlot.Weapon).id == service.Equipped(ItemSlot.Weapon).id, "equipment IDs persist");
    }

    private static void SkillPointsAndLeveling()
    {
        var service = Fresh();
        int levelEvents = 0;
        int changedEvents = 0;
        service.LeveledUp += level => levelEvents++;
        service.Changed += () => changedEvents++;
        Check(service.Profile.skillRanks[0] == 1 && !service.LearnSkill(0), "starting skill rank one available; rank two locked");
        service.GrantExperience(60);
        Check(service.Profile.level == 2 && service.Profile.xp == 0 && service.Profile.skillPoints == 0, "level 2 adds no extra point");
        Check(levelEvents == 1 && changedEvents == 1, "progression events fire");
        Check(!service.LearnSkill(0) && service.Profile.skillRanks[0] == 1 && service.Profile.skillPoints == 0, "starting rank retains exactly one spent point");
        Check(!service.LearnSkill(0) && !service.LearnSkill(1), "cannot overspend points or bypass level lock");
        service.GrantExperience(90 + 120 + 150 + 180);
        Check(service.Profile.level == 6 && levelEvents == 5 && service.Profile.skillPoints == 4, "multi-level gain and points");
        Check(service.LearnSkill(1) && service.LearnSkill(2), "first branch unlocks through its learned predecessors");
        Check(!service.LearnSkill(0) && service.Profile.skillRanks[0] == 1 && service.Profile.skillPoints == 2, "rank two requires its higher character level");
        ReachLevel(service, 10);
        Check(service.LearnSkill(0) && service.Profile.skillRanks[0] == 2 && !service.LearnSkill(0), "rank two unlocks at ten while rank three stays locked");
        ReachLevel(service, 20);
        Check(service.LearnSkill(0) && service.Profile.skillRanks[0] == 3 && !service.LearnSkill(0), "rank three unlocks at twenty and remains the cap");
        var restored = new ProgressionService();
        Check(restored.Load() && restored.Profile.skillRanks[0] == 3 && restored.Profile.skillRanks[1] == 1 && restored.Profile.skillRanks[2] == 1, "learned ranks persist");
        Check(!restored.LearnSkill(-1) && !restored.LearnSkill(GameBalance.SkillCount), "invalid skill slots rejected");
    }

    private static void InventoryAndEconomy()
    {
        var service = Fresh();
        ItemData weapon = service.Equipped(ItemSlot.Weapon);
        Check(!service.Sell(weapon.id), "equipped item cannot be sold");
        int cost = service.UpgradeCost(weapon);
        float before = service.GetStats().Damage;
        Check(service.Upgrade(weapon.id), "upgrade with enough gold");
        Check(service.Profile.gold == 60 - cost && weapon.upgradeLevel == 1 && service.GetStats().Damage > before, "upgrade charges and immediately improves stats");
        ItemData highLevel = service.CreateLoot(20, true);
        Check(highLevel.rarity >= Rarity.Rare, "boss loot has at least rare quality");
        Check(!service.Equip(highLevel.id), "high-level loot cannot be equipped early");
        int gold = service.Profile.gold;
        int price = service.SellValue(highLevel);
        Check(service.SetItemLocked(highLevel.id, false) && service.Sell(highLevel.id) && service.Profile.gold == gold + price, "intentional unlock and selling grant exact gold");
        Check(!service.Sell(highLevel.id), "item cannot be sold twice");
        ItemData reward = service.CreateLoot(1, false);
        Check(service.Equip(reward.id) && service.Equipped(reward.slot).id == reward.id, "eligible loot can replace equipment");
        for (int i = service.Profile.inventory.Count; i < ProgressionService.InventoryCapacity; i++) service.CreateLoot(1, false);
        gold = service.Profile.gold;
        ItemData overflow = service.CreateLoot(1, true);
        Check(service.Profile.inventory.Count == 72 && !service.Profile.inventory.Exists(item => item.id == overflow.id), "inventory is capped without losing existing gear");
        bool protectedOverflow = ProgressionService.IsProtectedLoot(overflow);
        Check(protectedOverflow
            ? service.Profile.gold == gold && service.Profile.pendingLoot.Exists(item => item.id == overflow.id) && service.LastError.Contains("待领取")
            : service.Profile.gold == gold + service.SellValue(overflow) && service.LastError.Contains("自动出售"),
            "protected overflow preserves the item for claiming; ordinary overflow grants gold and a notification");
        service.AddGold(100);
        gold = service.Profile.gold;
        int potions = service.Profile.potions;
        Check(service.BuyPotion() && service.Profile.potions == potions + 1 && service.Profile.gold == gold - 20, "potion purchase");
        Check(service.UsePotion() && service.Profile.potions == potions, "potion consumption");
    }

    private static void WorldLootIsCollectedOnce()
    {
        var service = Fresh();
        int changed = 0;
        service.Changed += () => changed++;
        string before = UnityEngine.JsonUtility.ToJson(service.Profile, true);
        string saved = File.ReadAllText(service.SaveFilePath);
        DateTime written = File.GetLastWriteTimeUtc(service.SaveFilePath);
        ItemData loot = service.RollLoot(0, false);
        // This case exercises explicit ordinary item sale; mechanic protection is covered
        // independently, so randomized mechanic rolls must not change its event contract.
        loot.mechanic = EquipmentMechanic.None;
        loot.locked = false;
        ItemData boss = service.RollLoot(int.MaxValue, true);
        Check(loot.id != boss.id && loot.level == 1 && boss.level == 100 && boss.rarity >= Rarity.Rare, "world loot has unique IDs, capped levels and guaranteed boss rarity");
        Check(loot.upgradeBaseInitialized && boss.upgradeBaseInitialized && loot.upgradeLevel == 0, "world loot initializes permanent upgrade metadata");
        Check(UnityEngine.JsonUtility.ToJson(service.Profile, true) == before && changed == 0, "rolling world drops never inserts gear or awards gold/events");
        Check(File.ReadAllText(service.SaveFilePath) == saved && File.GetLastWriteTimeUtc(service.SaveFilePath) == written, "rolling world drops does not write the save");
        string originalItem = UnityEngine.JsonUtility.ToJson(loot, true);
        int gold = service.Profile.gold;
        Check(service.CollectLoot(loot) && service.Profile.inventory.Count == 4 && changed == 1 && service.Profile.gold == gold, "first pickup inserts once, emits one event and grants no extra gold");
        Check(UnityEngine.JsonUtility.ToJson(service.Profile.inventory.Find(item => item.id == loot.id), true) == originalItem, "pickup preserves the generated identity and complete equipment stats");
        var restored = new ProgressionService(service.SaveDirectory);
        Check(restored.Load() && restored.Profile.inventory.Exists(item => item.id == loot.id), "picked-up equipment persists");
        Check(!restored.CollectLoot(UnityEngine.JsonUtility.FromJson<ItemData>(originalItem)) && restored.Profile.inventory.Count == 4, "saved inventory IDs reject a repeated pickup after reload");

        before = UnityEngine.JsonUtility.ToJson(service.Profile, true);
        saved = File.ReadAllText(service.SaveFilePath);
        written = File.GetLastWriteTimeUtc(service.SaveFilePath);
        Check(!service.CollectLoot(loot) && !service.CollectLoot(UnityEngine.JsonUtility.FromJson<ItemData>(originalItem)), "same-object and copied-ID duplicate pickups are rejected");
        Check(UnityEngine.JsonUtility.ToJson(service.Profile, true) == before && changed == 1 && File.ReadAllText(service.SaveFilePath) == saved && File.GetLastWriteTimeUtc(service.SaveFilePath) == written, "duplicate pickup changes no profile, event or saved data");
        Check(service.Sell(loot.id), "collected unequipped gear can be sold");
        gold = service.Profile.gold;
        Check(!service.CollectLoot(loot) && service.Profile.gold == gold && service.Profile.inventory.Count == 3 && changed == 2, "selling a drop does not make its receipt collectible again");
        before = UnityEngine.JsonUtility.ToJson(service.Profile, true);
        Check(!service.CollectLoot(null) && !service.CollectLoot(new ItemData()) && !service.CollectLoot(new ItemData { id = "invalid-slot", slot = (ItemSlot)99 }) && !service.CollectLoot(new ItemData { id = "invalid-rarity", rarity = (Rarity)99 }), "null, unidentified and invalid-enum drops are rejected");
        Check(UnityEngine.JsonUtility.ToJson(service.Profile, true) == before && changed == 2, "invalid drops cannot mutate progression");

        while (service.Profile.inventory.Count < ProgressionService.InventoryCapacity) service.CreateLoot(1, false);
        changed = 0;
        gold = service.Profile.gold;
        saved = File.ReadAllText(service.SaveFilePath);
        written = File.GetLastWriteTimeUtc(service.SaveFilePath);
        ItemData overflow = service.RollLoot(8, true);
        Check(service.Profile.inventory.Count == 72 && service.Profile.gold == gold && changed == 0 && File.ReadAllText(service.SaveFilePath) == saved && File.GetLastWriteTimeUtc(service.SaveFilePath) == written, "full bag still waits for actual pickup before converting a world drop");
        bool protectedOverflow = ProgressionService.IsProtectedLoot(overflow);
        int value = protectedOverflow ? 0 : service.SellValue(overflow);
        Check(service.CollectLoot(overflow) && service.Profile.inventory.Count == 72 && service.Profile.gold == gold + value && changed == 1 && service.LastError.Contains(protectedOverflow ? "待领取" : "自动出售"), "full-bag pickup either protects valuable loot or converts ordinary loot exactly once");
        Check(!service.Profile.inventory.Exists(item => item.id == overflow.id) && (!protectedOverflow || service.Profile.pendingLoot.Exists(item => item.id == overflow.id)), "overflow does not replace existing gear and protected rewards remain claimable");
        saved = File.ReadAllText(service.SaveFilePath);
        written = File.GetLastWriteTimeUtc(service.SaveFilePath);
        Check(!service.CollectLoot(overflow) && !service.CollectLoot(UnityEngine.JsonUtility.FromJson<ItemData>(UnityEngine.JsonUtility.ToJson(overflow, true))), "overflow receipt blocks object and copied-ID duplicates");
        Check(service.Profile.gold == gold + value && changed == 1 && File.ReadAllText(service.SaveFilePath) == saved && File.GetLastWriteTimeUtc(service.SaveFilePath) == written, "repeated overflow pickup grants no gold, event or save write");
        restored = new ProgressionService(service.SaveDirectory);
        Check(restored.Load() && restored.Profile.inventory.Count == 72 && restored.Profile.gold == gold + value && (!protectedOverflow || restored.Profile.pendingLoot.Exists(item => item.id == overflow.id)), "full bag and correct pending-item or gold outcome persist through reload");
    }

    private static void HotbarDragMovesAndSwaps()
    {
        foreach (HeroClass hero in Enum.GetValues(typeof(HeroClass)))
        {
            var service = Fresh(hero);
            string saved = File.ReadAllText(service.SaveFilePath);
            int changes = 0;
            service.Changed += () => changes++;
            Check(!service.MoveHotbarSkill(1, 2) && !service.MoveHotbarSkill(-1, 1) && !service.MoveHotbarSkill(0, 10) && changes == 0 && File.ReadAllText(service.SaveFilePath) == saved, hero + " rejects unlearned or invalid drag without saving");
            ReachLevel(service, 4);
            Check(service.Profile.skillRanks[0]==1, hero + " drag fixture starts with actual root skill");
            int[] allPages = (int[])service.Profile.equippedSkills.Clone();
            changes = 0;
            Check(service.MoveHotbarSkill(0, 1) && service.Profile.equippedSkills[0] == -1 && service.Profile.equippedSkills[1] == 0 && changes == 1, hero + " dragging onto unlearned preset treats target as empty");
            for (int i = 10; i < allPages.Length; i++) Check(service.Profile.equippedSkills[i] == allPages[i], hero + " first-page drag preserves other pages");
            Check(service.MoveHotbarSkill(1, 9) && service.Profile.equippedSkills[1] == -1 && service.Profile.equippedSkills[9] == 0, hero + " dragging onto empty slot moves and clears source");
            Check(service.LearnSkill(1) && service.AssignSkill(0, 1), hero + " drag fixture learns and assigns another active skill");
            changes = 0;
            Check(service.MoveHotbarSkill(9, 0) && service.Profile.equippedSkills[0] == 0 && service.Profile.equippedSkills[9] == 1 && changes == 1, hero + " dragging onto learned active skill swaps both positions atomically");
            saved = File.ReadAllText(service.SaveFilePath);
            DateTime written = File.GetLastWriteTimeUtc(service.SaveFilePath);
            Check(!service.MoveHotbarSkill(0, 0) && !service.MoveHotbarSkill(1, 2) && changes == 1 && File.ReadAllText(service.SaveFilePath) == saved && File.GetLastWriteTimeUtc(service.SaveFilePath) == written, hero + " same-slot or empty-source drag does not save or emit changes");
            Check(service.SetHotbarPage(2) && service.AssignSkill(0, 0), hero + " drag switches to third page and assigns an independent learned skill");
            allPages = (int[])service.Profile.equippedSkills.Clone();
            Check(service.MoveHotbarSkill(0, 9) && service.Profile.equippedSkills[20] == -1 && service.Profile.equippedSkills[29] == 0, hero + " selected third page receives the move");
            for (int i = 0; i < 20; i++) Check(service.Profile.equippedSkills[i] == allPages[i], hero + " third-page drag preserves first and second pages");
            var restored = new ProgressionService(service.SaveDirectory);
            Check(restored.Load() && restored.Profile.heroClass == hero && restored.Profile.hotbarPage == 2 && restored.Profile.equippedSkills[0] == 0 && restored.Profile.equippedSkills[9] == 1 && restored.Profile.equippedSkills[20] == -1 && restored.Profile.equippedSkills[29] == 0, hero + " moved/swapped loadouts and selected page persist");
            int oldSlot = service.Profile.equippedSkills[20];
            service.Profile.equippedSkills[20] = 3;
            Check(!service.MoveHotbarSkill(0, 8), hero + " a passive cannot become a draggable active source");
            service.Profile.equippedSkills[20] = oldSlot;
        }
    }

    private static void ConsumableHotbarAndPersistence()
    {
        foreach (HeroClass hero in Enum.GetValues(typeof(HeroClass)))
        {
            var service = Fresh(hero);
            int changes = 0;
            service.Changed += () => changes++;
            int originalPotions = service.Profile.potions;
            Check(service.AssignConsumable(8) && service.Profile.equippedSkills[8] == GameBalance.HotbarPotion && changes == 1,
                hero + " level-one character can assign a potion with its starting skill");
            Check(service.Profile.potions == originalPotions && service.Profile.skillPoints == 0 && service.Profile.skillRanks[0] == 1 && SpentPoints(service) == 1,
                hero + " assigning a potion does not consume inventory or spend progression");
            string saved = File.ReadAllText(service.SaveFilePath);
            DateTime written = File.GetLastWriteTimeUtc(service.SaveFilePath);
            Check(!service.AssignConsumable(-1) && !service.AssignConsumable(10) && !service.AssignConsumable(8) && !service.AssignSkill(9, GameBalance.HotbarPotion),
                hero + " invalid consumable slots, same-slot assignment and skill API sentinel misuse reject");
            Check(changes == 1 && File.ReadAllText(service.SaveFilePath) == saved && File.GetLastWriteTimeUtc(service.SaveFilePath) == written,
                hero + " rejected consumable changes do not notify or write");
            Check(service.MoveHotbarSkill(8, 0) && service.Profile.equippedSkills[0] == GameBalance.HotbarPotion && service.Profile.equippedSkills[8] == 0,
                hero + " potion drag swaps the starting skill into the source slot");
            Check(service.AssignConsumable(9) && service.Profile.equippedSkills[9] == GameBalance.HotbarPotion && service.Profile.equippedSkills[0] == -1,
                hero + " repeated potion assignment relocates instead of duplicating");
            ReachLevel(service, 2);
            Check(service.Profile.skillRanks[0] == 1 && service.AssignSkill(0, 0), hero + " assigns the starting skill for mixed hotbar swaps");
            changes = 0;
            Check(service.MoveHotbarSkill(9, 0) && service.Profile.equippedSkills[0] == GameBalance.HotbarPotion && service.Profile.equippedSkills[9] == 0 && changes == 1,
                hero + " potion and learned skill drag swap atomically");
            Check(service.AssignConsumable(9) && service.Profile.equippedSkills[9] == GameBalance.HotbarPotion && service.Profile.equippedSkills[0] == 0,
                hero + " assigning potion to learned skill swaps the displaced skill back");
            Check(service.AssignSkill(9, 0) && service.Profile.equippedSkills[9] == 0 && service.Profile.equippedSkills[0] == GameBalance.HotbarPotion,
                hero + " skill assignment onto potion preserves the displaced potion");
            while (service.Profile.potions > 0) Check(service.UsePotion(), hero + " exhausts real potion inventory");
            Check(service.Profile.equippedSkills[0] == GameBalance.HotbarPotion && !service.UsePotion(), hero + " last potion consumption retains empty hotbar entry");
            Check(service.MoveHotbarSkill(0, 8) && service.AssignConsumable(0) && service.Profile.potions == 0,
                hero + " zero-quantity potion can still move and be configured");
            Check(service.SetHotbarPage(1) && service.AssignConsumable(4) && service.SetHotbarPage(2) && service.AssignConsumable(6),
                hero + " every hotbar page can carry its own potion shortcut");
            Check(service.Profile.equippedSkills[0] == GameBalance.HotbarPotion && service.Profile.equippedSkills[14] == GameBalance.HotbarPotion && service.Profile.equippedSkills[26] == GameBalance.HotbarPotion,
                hero + " configuring another page preserves earlier potion positions");
            CheckLoadout(service.Profile, hero + " potion entries remain unique within each page");
            var restored = new ProgressionService(service.SaveDirectory);
            Check(restored.Load() && restored.Profile.potions == 0 && restored.Profile.hotbarPage == 2 && restored.Profile.equippedSkills[0] == GameBalance.HotbarPotion && restored.Profile.equippedSkills[14] == GameBalance.HotbarPotion && restored.Profile.equippedSkills[26] == GameBalance.HotbarPotion,
                hero + " zero-count consumable entries persist on all three pages");
            saved = File.ReadAllText(service.SaveFilePath);
            string originalPath = service.SaveFilePath;
            Check(service.SaveAsNewSlot() && service.Profile.equippedSkills[26] == GameBalance.HotbarPotion && File.ReadAllText(originalPath) == saved,
                hero + " snapshot preserves potion entries and original save bytes");
            Check(service.AssignSkill(6, -1) && service.Profile.equippedSkills[26] == -1 && service.Profile.potions == 0,
                hero + " empty-slot assignment removes shortcut without affecting inventory");
            Check(service.Profile.equippedSkills[0] == GameBalance.HotbarPotion && service.Profile.equippedSkills[14] == GameBalance.HotbarPotion,
                hero + " clearing one potion page preserves the others");
            Check(restored.Load() && restored.Profile.equippedSkills[26] == GameBalance.HotbarPotion,
                hero + " modifying snapshot cannot change source potion layout");
            Check(service.BuyPotion() && service.Profile.potions == 1 && service.Profile.equippedSkills[0] == GameBalance.HotbarPotion,
                hero + " restocking preserves existing zero-count shortcut");
        }
        var repair = Fresh();
        WriteSkillFixture("null", "[-2,-2,-3,99,3,0,-1,-1,-1,-1,-2,-2]", 1);
        Check(repair.Load() && repair.Profile.equippedSkills[0] == GameBalance.HotbarPotion && repair.Profile.equippedSkills[1] == -1 && repair.Profile.equippedSkills[2] == -1 && repair.Profile.equippedSkills[3] == -1 && repair.Profile.equippedSkills[4] == -1 && repair.Profile.equippedSkills[5] == 0,
            "loadout repair preserves first potion while removing duplicate, passive and invalid entries");
        Check(repair.Profile.equippedSkills[10] == GameBalance.HotbarPotion && repair.Profile.equippedSkills[11] == -1,
            "repair permits a separate potion shortcut per page");
        CheckLoadout(repair.Profile, "repaired mixed shortcut array obeys per-page uniqueness");
        int[] validLoadout = repair.Profile.equippedSkills;
        repair.Profile.equippedSkills = null;
        Check(!repair.AssignConsumable(0) && !repair.MoveHotbarSkill(0, 1), "broken in-memory loadout rejects potion operations without exception");
        repair.Profile.equippedSkills = validLoadout;
        repair.Profile.hotbarPage = 3;
        Check(!repair.AssignConsumable(0), "out-of-range page rejects potion operation");
    }

    private static void SummonerHasDistinctStatsAndEquipment()
    {
        var service = Fresh(HeroClass.Summoner);
        StatBlock initial = service.GetStats();
        int equippedAttack = 0, equippedArmor = 0, equippedHealth = 0;
        foreach (ItemData item in service.Profile.inventory) { equippedAttack += item.attack; equippedArmor += item.defense; equippedHealth += item.health; }
        Check(Near(initial.Damage, 15 + equippedAttack) && Near(initial.MaxHealth, 125 + equippedHealth) && Near(initial.Armor, 3 + equippedArmor) && Near(initial.MoveSpeed, 5.5f) && Near(initial.CritChance, .10f), "summoner starts with its own class stats including starter gear");
        Check(service.Equipped(ItemSlot.Weapon).name.Contains("法器"), "summoner uses a focus rather than another class weapon");
        ReachLevel(service, 10);
        StatBlock leveled = service.GetStats();
        Check(Near(leveled.Damage, initial.Damage + 9 * 2.3f) && Near(leveled.MaxHealth, initial.MaxHealth + 9 * 13) && Near(leveled.Armor, initial.Armor + 9 * .75f), "summoner level growth follows its own damage health and armor curves");
        var restored = new ProgressionService(service.SaveDirectory);
        Check(restored.Load() && restored.Profile.heroClass == HeroClass.Summoner && Near(restored.GetStats().Damage, leveled.Damage), "fourth class identity and stats survive saved reload");
    }

    private static void UpgradeTransferAndPreview()
    {
        foreach (ItemSlot slot in new[] { ItemSlot.Weapon, ItemSlot.Armor, ItemSlot.Relic })
        {
            var service = Fresh(); ReachLevel(service, 50); service.AddGold(1000000);
            ItemData source = service.Equipped(slot), target = TransferFixture(service, slot);
            UpgradeTo(service, source, 5);
            ItemData targetFive = service.PreviewEquippedItem(target), sourceFive = service.PreviewEquippedItem(source);
            ItemData sourceZero = service.PreviewUpgrade(source, 0);
            Check(service.SlotUpgradeRank(slot) == 5 && source.upgradeLevel == 5 && target.upgradeLevel == 0, slot + " training belongs to slot while bag item keeps baseline cache");
            string saved = File.ReadAllText(service.SaveFilePath), profile = UnityEngine.JsonUtility.ToJson(service.Profile, true);
            int events = 0; service.Changed += () => events++;
            for (int rank = 0; rank <= 10; rank++)
            {
                ItemData preview = service.PreviewUpgrade(target, rank);
                Check(preview != null && !ReferenceEquals(preview, target) && preview.upgradeLevel == rank && preview.id == target.id && BoundedEquipment(preview), slot + " rank preview is independent, identified and bounded");
            }
            Check(service.PreviewEquippedItem(null) == null && service.PreviewUpgrade(target, -1) == null && service.PreviewUpgrade(target, 11) == null, slot + " invalid preview inputs rejected");
            Check(!service.TransferUpgrade(source.id, target.id) && service.LastError.Contains("自动继承"), slot + " obsolete transfer explains automatic inheritance");
            Check(profile == UnityEngine.JsonUtility.ToJson(service.Profile, true) && saved == File.ReadAllText(service.SaveFilePath) && events == 0, slot + " preview and obsolete transfer do not mutate state or saves");
            int gold = service.Profile.gold;
            Check(service.Equip(target.id) && SameEquipment(target, targetFive) && SameEquipment(source, sourceZero), slot + " equipping applies trained rank to candidate own baseline and releases old cache");
            Check(service.Profile.gold == gold && events == 1 && service.SlotUpgradeRank(slot) == 5, slot + " automatic inheritance is free and does not change training");
            for (int cycle = 0; cycle < 10; cycle++)
                Check(service.Equip(source.id) && SameEquipment(source, sourceFive) && service.Equip(target.id) && SameEquipment(target, targetFive), slot + " repeated replacement cannot compound attributes");
            ItemData next = service.PreviewUpgrade(target, 6);
            int cost = service.UpgradeCost(source);
            Check(service.Upgrade(source.id) && service.SlotUpgradeRank(slot) == 6 && SameEquipment(target, next) && source.upgradeLevel == 0 && service.Profile.gold == gold - cost, slot + " upgrading bag item trains its slot and improves currently worn item");
            Check(service.Equip(source.id) && source.upgradeLevel == 6 && target.upgradeLevel == 0, slot + " old gear automatically receives newest slot rank");
            service.Save();
            var restored = new ProgressionService(); Check(restored.Load(), slot + " slot training persists");
            ItemData restoredSource = restored.Profile.inventory.Find(item => item.id == source.id), restoredTarget = restored.Profile.inventory.Find(item => item.id == target.id);
            Check(restored.SlotUpgradeRank(slot) == 6 && restoredSource.upgradeLevel == 6 && restoredTarget.upgradeLevel == 0, slot + " load reconstructs only worn enhancement cache");
            UpgradeTo(restored, restoredSource, 10);
            gold = restored.Profile.gold;
            Check(!restored.Upgrade(restoredTarget.id) && restored.UpgradeCost(restoredTarget) == 0 && restored.SlotUpgradeRank(slot) == 10 && restored.Profile.gold == gold, slot + " slot cap applies even to a fresh unenhanced bag item");
            Check(restored.Equip(restoredTarget.id) && restoredTarget.upgradeLevel == 10 && BoundedEquipment(restoredTarget), slot + " any replacement inherits maximum slot rank safely");
        }
    }

    private static void LegacyUpgradesMigrateWithoutAttributeLoss()
    {
        foreach (bool unusual in new[] { false, true })
        {
            var service = Fresh();
            ReachLevel(service, 50);
            service.AddGold(1000000);
            var sourceIds = new List<string>();
            var targetIds = new List<string>();
            foreach (ItemSlot slot in new[] { ItemSlot.Weapon, ItemSlot.Armor, ItemSlot.Relic })
            {
                ItemData source = service.Equipped(slot);
                UpgradeTo(service, source, 5);
                sourceIds.Add(source.id);
                targetIds.Add(TransferFixture(service, slot).id);
            }
            JsonNode legacy = JsonNode.Parse(File.ReadAllText(service.SaveFilePath));
            legacy["profile"].AsObject().Remove("slotUpgradeRanks");
            legacy["profile"].AsObject().Remove("slotUpgradesInitialized");
            var expected = new Dictionary<string, ItemData>();
            foreach (JsonObject item in legacy["profile"]["inventory"].AsArray())
            {
                string id = item["id"].GetValue<string>();
                if (unusual && sourceIds.Contains(id))
                {
                    int slot = item["slot"].GetValue<int>();
                    item["upgradeLevel"] = slot == 0 ? 8 : 7;
                    item["attack"] = slot == 0 ? 10000 : slot == 2 ? 1 : 0;
                    item["defense"] = slot == 1 ? 10000 : 0;
                    item["health"] = slot == 0 ? 0 : slot == 2 ? 100000 : 3;
                }
                foreach (string field in new[] { "upgradeBaseInitialized", "baseAttack", "baseDefense", "baseHealth", "upgradeAnchorLevel", "upgradeAnchorAttack", "upgradeAnchorDefense", "upgradeAnchorHealth", "balanceRevision" }) item.Remove(field);
                if (sourceIds.Contains(id)) expected[id] = UnityEngine.JsonUtility.FromJson<ItemData>(item.ToJsonString());
            }
            File.WriteAllText(service.SaveFilePath, legacy.ToJsonString());
            string untouchedLegacySave = File.ReadAllText(service.SaveFilePath);
            foreach (ItemData oldItem in expected.Values)
            {
                string untouchedItem = UnityEngine.JsonUtility.ToJson(oldItem, true);
                ItemData preview = service.PreviewUpgrade(oldItem, oldItem.upgradeLevel);
                Check(preview != null && preview.upgradeBaseInitialized && preview.upgradeLevel == oldItem.upgradeLevel && preview.balanceRevision == 1 && BoundedEquipment(preview) && !oldItem.upgradeBaseInitialized && UnityEngine.JsonUtility.ToJson(oldItem, true) == untouchedItem, "preview initializes only its copy of legacy equipment and preserves exact current attributes");
            }
            Check(File.ReadAllText(service.SaveFilePath) == untouchedLegacySave, "legacy equipment preview never rewrites a save to add base fields");
            var migrated = new ProgressionService();
            Check(migrated.Load(), "pre-base-field legacy equipment save loads");
            var metadata = new Dictionary<string, string>();
            for (int index = 0; index < sourceIds.Count; index++)
            {
                ItemData source = migrated.Profile.inventory.Find(item => item.id == sourceIds[index]);
                ItemData target = migrated.Profile.inventory.Find(item => item.id == targetIds[index]);
                ItemData original = expected[source.id];
                Check(source.id == original.id && source.upgradeLevel == original.upgradeLevel && source.upgradeBaseInitialized && source.balanceRevision == 1 && BoundedEquipment(source), "legacy migration preserves identity and paid rank while recalculating bounded linear attributes");
                expected[source.id] = UnityEngine.JsonUtility.FromJson<ItemData>(UnityEngine.JsonUtility.ToJson(source, true));
                original = expected[source.id];
                string stable = UnityEngine.JsonUtility.ToJson(source, true);
                metadata[source.id] = stable;
                int gold = migrated.Profile.gold;
                for (int repeat = 0; repeat < 4; repeat++)
                    Check(migrated.Equip(target.id) && migrated.Equip(source.id) && SameEquipment(source, original) && migrated.Profile.gold == gold, "legacy equipment replacement preserves its anchored old stats and all gold");
                Check(UnityEngine.JsonUtility.ToJson(source, true) == stable, "legacy anchors remain unchanged after repeated transfers");
            }
            migrated.Save();
            var reloaded = new ProgressionService();
            Check(reloaded.Load(), "migrated equipment bases persist through another load");
            for (int cycle = 0; cycle < 2; cycle++)
            {
                for (int index = 0; index < sourceIds.Count; index++)
                {
                    Check(reloaded.Equip(targetIds[index]), "legacy slot enhancement applies on replacement before an intervening save/load");
                    var away = new ProgressionService();
                    Check(away.Load() && away.Equip(sourceIds[index]), "legacy slot enhancement applies on replacement back after an intervening load");
                    reloaded = new ProgressionService();
                    Check(reloaded.Load() && UnityEngine.JsonUtility.ToJson(reloaded.Profile.inventory.Find(item => item.id == sourceIds[index]), true) == metadata[sourceIds[index]], "interleaved equipment replacements and reloads preserve all three stats and all eight base/anchor fields exactly");
                }
            }
            for (int index = 0; index < sourceIds.Count; index++)
            {
                ItemData source = reloaded.Profile.inventory.Find(item => item.id == sourceIds[index]);
                ItemData target = reloaded.Profile.inventory.Find(item => item.id == targetIds[index]);
                ItemData original = expected[source.id];
                Check(SameEquipment(source, original) && source.upgradeBaseInitialized, "reloading migrated equipment does not lower or inflate visible attributes");
                Check(reloaded.Equip(target.id) && reloaded.Equip(source.id) && SameEquipment(source, original), "reloaded legacy equipment can still be replaced in both directions");
                ItemData next = reloaded.PreviewUpgrade(source, source.upgradeLevel + 1);
                int cost = reloaded.UpgradeCost(source);
                int gold = reloaded.Profile.gold;
                Check(reloaded.Upgrade(source.id) && SameEquipment(source, next) && source.attack >= original.attack && source.defense >= original.defense && source.health >= original.health && BoundedEquipment(source) && reloaded.Profile.gold == gold - cost, "old anchored equipment continues normal paid upgrades without attribute loss or cap overflow");
            }
        }
    }

    private static ItemData TransferFixture(ProgressionService service, ItemSlot slot)
    {
        var item = new ItemData { id = Guid.NewGuid().ToString("N"), name = "Transfer fixture " + slot, slot = slot, rarity = Rarity.Epic, level = 35,
            attack = slot == ItemSlot.Weapon ? 47 : slot == ItemSlot.Relic ? 18 : 0,
            defense = slot == ItemSlot.Armor ? 26 : 0,
            health = slot == ItemSlot.Armor ? 180 : slot == ItemSlot.Relic ? 90 : 0 };
        service.Profile.inventory.Add(item);
        service.Save();
        Check(item.upgradeBaseInitialized, "equipment fixture receives persistent upgrade metadata through production validation");
        return item;
    }

    private static ItemData CloneEquipmentFixture(ProgressionService service, ItemData item)
    {
        ItemData copy = service.PreviewUpgrade(item, 0);
        copy.id = Guid.NewGuid().ToString("N");
        copy.name += " paid reference";
        service.Profile.inventory.Add(copy);
        service.Save();
        return copy;
    }

    private static void UpgradeTo(ProgressionService service, ItemData item, int rank)
    {
        while (service.SlotUpgradeRank(item.slot) < rank) Check(service.Upgrade(item.id), "fixture purchases an actual upgrade: " + item.slot + " +" + rank);
    }

    private static bool SameEquipment(ItemData a, ItemData b)
    {
        return a != null && b != null && a.attack == b.attack && a.defense == b.defense && a.health == b.health && a.upgradeLevel == b.upgradeLevel;
    }

    private static bool BoundedEquipment(ItemData item)
    {
        return item.upgradeLevel >= 0 && item.upgradeLevel <= 10 && item.attack >= 0 && item.attack <= 10000 && item.defense >= 0 && item.defense <= 10000 && item.health >= 0 && item.health <= 100000;
    }

    private static void BackupAndCorruption()
    {
        var service = Fresh();
        service.AddGold(15); // backup retains initial 60 gold
        string path = Path.Combine(UnityEngine.Application.persistentDataPath, "emberfall-save.json");
        File.WriteAllText(path, "{ damaged file");
        var recovery = new ProgressionService();
        Check(recovery.Load() && recovery.Profile.gold == 60 && recovery.LastError.Contains("备份"), "corrupt primary recovers previous snapshot");
        recovery.AddGold(7);
        var roundTrip = new ProgressionService();
        Check(roundTrip.Load() && roundTrip.Profile.gold == 67, "recovered character saves to repaired primary");
        File.WriteAllText(path, "bad again");
        Check(roundTrip.Load() && roundTrip.Profile.gold == 60, "saving recovery does not replace backup with corruption");
        File.WriteAllText(path + ".bak", "also corrupt");
        Check(!new ProgressionService().Load(), "two corrupt copies return false without throwing");
    }

    private static void DamagedFieldsAreRepaired()
    {
        var service = Fresh();
        string path = Path.Combine(UnityEngine.Application.persistentDataPath, "emberfall-save.json");
        File.WriteAllText(path, "{\"format\":\"emberfall-character\",\"version\":1,\"profile\":{\"version\":1,\"heroClass\":99,\"level\":2,\"xp\":-50,\"gold\":-30,\"potions\":500,\"skillPoints\":500,\"skillRanks\":[99,99,99],\"inventory\":[null,{\"id\":\"x\",\"name\":\"\",\"slot\":99,\"rarity\":0}]}}");
        Check(service.Load(), "valid envelope with damaged fields can be repaired");
        Check(service.Profile.heroClass == HeroClass.Vanguard && service.Profile.gold == 0 && service.Profile.potions == 99 && service.Profile.xp == 0, "invalid enums and currency repaired");
        Check(service.Profile.skillRanks[0] == 1 && service.Profile.skillRanks[1] == 0 && service.Profile.skillRanks[2] == 0 && service.Profile.skillPoints == 0, "skill ranks respect level budget and unlocks");
        Check(service.Profile.inventory.Count == 3 && service.Equipped(ItemSlot.Weapon) != null, "missing inventory repaired with valid starter gear");
        service.Profile.inventory[1].id = service.Profile.inventory[0].id;
        service.Save();
        Check(service.Profile.inventory[0].id != service.Profile.inventory[1].id, "duplicate item IDs repaired");
        Check(service.Equipped(ItemSlot.Weapon).slot == ItemSlot.Weapon && service.Equipped(ItemSlot.Armor).slot == ItemSlot.Armor, "repaired IDs retain slot integrity");
    }

    private static void UpperBoundsAndInvalidActions()
    {
        var service = Fresh(HeroClass.Arcanist);
        service.GrantExperience(int.MaxValue);
        Check(service.Profile.level == 100 && service.Profile.xp == 0 && service.Profile.skillPoints == 98, "large XP input safely reaches level cap");
        service.GrantExperience(int.MaxValue);
        Check(service.Profile.level == 100 && service.Profile.skillPoints == 98, "level cap cannot mint extra points");
        service.AddGold(int.MaxValue);
        Check(service.Profile.gold == 999999999, "gold overflow is clamped");
        service.AddGold(int.MinValue);
        Check(service.Profile.gold == 0, "gold loss clamps at zero");
        Check(!service.Upgrade(service.Equipped(ItemSlot.Weapon).id) && !service.BuyPotion(), "insufficient funds rejected");
        Check(!service.Equip(null) && !service.Sell("unknown") && !service.Upgrade("unknown"), "unknown IDs rejected");
        for (int i = 0; i < 5; i++) Check(service.UsePotion(), "existing potion available");
        Check(!service.UsePotion() && service.Profile.potions == 0, "empty potion count never becomes negative");
        service.AddGold(1000000);
        ItemData weapon = service.Equipped(ItemSlot.Weapon);
        for (int i = 0; i < 10; i++) Check(service.Upgrade(weapon.id), "upgrade within cap succeeds");
        Check(!service.Upgrade(weapon.id) && weapon.upgradeLevel == 10, "upgrade cap enforced");
    }

    private static void EveryClassSkillUnlocksAtItsGate()
    {
        int[] expectedLevels = { 1, 4, 6, 4, 10, 6, 13, 20, 13, 30 };
        Check(GameBalance.SkillCount == 10, "eight active and two passive skills per class");
        Check(GameBalance.ClassNames.Length == Enum.GetValues(typeof(HeroClass)).Length, "every playable class has balance metadata");
        for (int hero = 0; hero < Enum.GetValues(typeof(HeroClass)).Length; hero++)
        {
            var names = new HashSet<string>();
            for (int skill = 0; skill < 10; skill++)
            {
                var service = Fresh((HeroClass)hero);
                Check(service.Profile.skillRanks.Length == 10, "new characters have ten skill ranks");
                int required = expectedLevels[skill];
                Check(GameBalance.SkillRequiredLevels[skill] == required, "expected skill unlock milestone");
                string name = GameBalance.SkillName((HeroClass)hero, skill);
                Check(!string.IsNullOrWhiteSpace(name) && names.Add(name) && !string.IsNullOrWhiteSpace(GameBalance.SkillDescription((HeroClass)hero, skill)), "every class skill has a distinct name and description");
                if (skill != 0) {
                ReachLevel(service, required - 1);
                LearnPrerequisites(service, skill);
                int points = service.Profile.skillPoints;
                Check(!service.LearnSkill(skill) && service.Profile.skillRanks[skill] == 0 && service.Profile.skillPoints == points, "cannot learn one level before requirement");
                Check(service.SkillLockReason(skill).Contains(required.ToString()), "lock reason communicates required level");
                ReachLevel(service, required);
                points = service.Profile.skillPoints;
                Check(service.LearnSkill(skill) && service.Profile.skillRanks[skill] == 1 && service.Profile.skillPoints == points - 1, "skill unlocks and spends one point exactly at requirement");
                } else Check(service.Profile.skillRanks[0]==1&&service.Profile.skillPoints==0,"first active starts learned");
                if (GameBalance.IsPassive(skill))
                    Check(GameBalance.SkillCooldown((HeroClass)hero, skill) == 0 && GameBalance.SkillEnergyCost((HeroClass)hero, skill) == 0, "passive skills have no active cooldown or energy cost");
                else
                {
                    float cooldown = GameBalance.SkillCooldown((HeroClass)hero, skill);
                    float cost = GameBalance.SkillEnergyCost((HeroClass)hero, skill);
                    Check(cooldown > 0 && cooldown <= 90 && (skill == 9 ? cost == 0 : cost > 0 && cost <= 100), "ultimate is free while other active skills have finite usable costs");
                    Check(GameBalance.EffectiveCooldown((HeroClass)hero, skill, 3) < cooldown, "evolving each active skill improves its class-specific cooldown");
                }
            }
            var complete = Fresh((HeroClass)hero);
            ReachLevel(complete, 30);
            for (int skill = 0; skill < 10; skill++) LearnBranch(complete, skill);
            Check(complete.Profile.level == 30 && complete.Profile.skillPoints == 19, "all ten first ranks consume exactly ten earned points");
        }
    }

    private static void EveryClassSkillRankRequiresItsLevel()
    {
        for (int hero = 0; hero < Enum.GetValues(typeof(HeroClass)).Length; hero++)
            for (int skill = 0; skill < 10; skill++)
            {
                var service = Fresh((HeroClass)hero);
                string originalName = GameBalance.SkillName((HeroClass)hero, skill);
                int assignedSlot = (skill + 3) % 10;
                int prerequisiteRanks = 0;
                for (int rank = 1; rank <= 3; rank++)
                {
                    int required = rank == 1 ? GameBalance.SkillRequiredLevels[skill] : Math.Max(2,GameBalance.SkillRequiredLevels[skill]) + (rank == 2 ? 8 : 18);
                    if (skill == 0 && rank == 1) { Check(service.Profile.skillRanks[0]==1,"first active already learned");service.AssignSkill(assignedSlot,skill);continue; }
                    Check(GameBalance.SkillRankRequiredLevel(skill, rank) == required, "rank gate uses original skill milestone plus evolution offset");
                    ReachLevel(service, required - 1);
                    if (rank == 1)
                    {
                        LearnPrerequisites(service, skill);
                        prerequisiteRanks = SpentPoints(service);
                    }
                    int points = service.Profile.skillPoints;
                    Check(!service.LearnSkill(skill) && service.Profile.skillRanks[skill] == rank - 1 && service.Profile.skillPoints == points, "rank cannot be purchased one level early even with spare points");
                    Check(service.SkillLockReason(skill).Contains(required.ToString()), "next-rank lock reason explains higher level requirement");
                    ReachLevel(service, required);
                    points = service.Profile.skillPoints;
                    Check(service.LearnSkill(skill) && service.Profile.skillRanks[skill] == rank && service.Profile.skillPoints == points - 1, "rank evolves at exact required level for one point");
                    if (rank == 1)
                        Check(service.AssignSkill(assignedSlot, skill) == !GameBalance.IsPassive(skill), "only active skills can be placed before evolving");
                    bool placementPreserved = GameBalance.IsPassive(skill) ? Array.IndexOf(service.Profile.equippedSkills, skill) < 0 : service.Profile.equippedSkills[assignedSlot] == skill;
                    Check(placementPreserved && GameBalance.SkillName((HeroClass)hero, skill) == originalName, "evolution preserves skill identity and active or passive placement");
                }
                Check(!service.LearnSkill(skill) && service.Profile.skillRanks[skill] == 3, "evolved original skill remains capped at rank three");
                int spent = 0;
                foreach (int learned in service.Profile.skillRanks) spent += learned;
                Check(spent == prerequisiteRanks + 3 && service.Profile.skillPoints + spent == service.Profile.level - 1, "evolving one skill preserves its real prerequisite ranks and conserves points");
            }
    }

    private static void SkillTreePrerequisitesAndBranches()
    {
        int[][] expected = { new int[0], new[] { 0 }, new[] { 1 }, new[] { 0 }, new[] { 2 }, new[] { 3 }, new[] { 5 }, new[] { 4 }, new[] { 5 }, new[] { 7, 6 } };
        for (int skill = 0; skill < expected.Length; skill++)
        {
            Check(GameBalance.SkillPrerequisites[skill].Length == expected[skill].Length, "tree prerequisite count matches branch design");
            for (int i = 0; i < expected[skill].Length; i++)
                Check(GameBalance.SkillPrerequisites[skill][i] == expected[skill][i], "tree prerequisite edge matches branch design");
        }
        for (int hero = 0; hero < Enum.GetValues(typeof(HeroClass)).Length; hero++)
        {
            var locked = Fresh((HeroClass)hero);
            ReachLevel(locked, 100);
            locked.Profile.skillRanks[0]=0;locked.Save(); // Explicit legacy unlearned-root fixture.
            int changes = 0;
            locked.Changed += () => changes++;
            for (int skill = 1; skill < 10; skill++)
                Check(!locked.PrerequisitesMet(skill) && !locked.LearnSkill(skill) && locked.SkillLockReason(skill).Contains("前置") && locked.Profile.skillRanks[skill] == 0 && locked.Profile.skillPoints == 99 && changes == 0, "high level and spare points cannot bypass an unlearned predecessor");
            Check(locked.PrerequisitesMet(0) && !locked.PrerequisitesMet(-1) && !locked.PrerequisitesMet(10), "root and invalid prerequisite queries are handled");

            int[][] pairs = { new[] { 1, 3, 4 }, new[] { 2, 5, 6 }, new[] { 6, 8, 13 } };
            foreach (int[] pair in pairs)
            {
                var branches = Fresh((HeroClass)hero);
                ReachLevel(branches, pair[2]);
                LearnPrerequisites(branches, pair[0]);
                LearnPrerequisites(branches, pair[1]);
                int points = branches.Profile.skillPoints;
                Check(branches.LearnSkill(pair[0]) && branches.Profile.skillRanks[pair[1]] == 0, "learning one same-level branch does not grant its sibling");
                Check(branches.LearnSkill(pair[1]) && branches.Profile.level == pair[2] && branches.Profile.skillPoints == points - 2, "both same-level branches may be learned for separate points");
                Check(branches.Profile.skillPoints + SpentPoints(branches) == branches.Profile.level - 1, "branch purchases conserve earned points");
            }

            foreach (int first in new[] { 7, 6 })
            {
                int missing = first == 7 ? 6 : 7;
                var ultimate = Fresh((HeroClass)hero);
                ReachLevel(ultimate, 30);
                LearnBranch(ultimate, first);
                int points = ultimate.Profile.skillPoints;
                Check(ultimate.Profile.skillRanks[missing] == 0 && !ultimate.PrerequisitesMet(9) && !ultimate.LearnSkill(9) && ultimate.Profile.skillPoints == points, "either missing ultimate predecessor blocks purchase without spending");
                Check(ultimate.SkillLockReason(9).Contains(GameBalance.SkillName((HeroClass)hero, missing)), "ultimate lock explains the missing branch by name");
                LearnBranch(ultimate, missing);
                Check(ultimate.PrerequisitesMet(9) && ultimate.LearnSkill(9) && ultimate.Profile.skillRanks[9] == 1, "ultimate unlocks after learning both real branches");
                Check(ultimate.Profile.skillRanks[8] == 0 && ultimate.Profile.skillPoints + SpentPoints(ultimate) == 29, "ultimate does not auto-grant optional passive or mint points");
            }
        }
    }

    private static void PreviouslyLearnedSkillsSurviveNewPrerequisites()
    {
        for (int hero = 0; hero < Enum.GetValues(typeof(HeroClass)).Length; hero++)
        {
            var service = Fresh((HeroClass)hero);
            ReachLevel(service, 50);
            string weapon = service.Profile.weaponId;
            JsonNode legacy = JsonNode.Parse(File.ReadAllText(SavePath()));
            legacy["profile"]["skillRanks"] = JsonNode.Parse("[0,0,0,0,0,0,0,0,0,1]");
            legacy["profile"]["skillPoints"] = 48;
            File.WriteAllText(SavePath(), legacy.ToJsonString());
            Check(service.Load() && service.Profile.skillRanks[9] == 1 && service.Profile.skillPoints == 48 && !service.PrerequisitesMet(9), "legacy learned ultimate survives new prerequisite rules without inventing parents");
            Check(service.Profile.heroClass == (HeroClass)hero && service.Profile.weaponId == weapon && service.AssignSkill(0, 9), "legacy identity equipment and active skill assignment survive");
            Check(service.LearnSkill(9) && service.Profile.skillRanks[9] == 2 && service.Profile.skillPoints == 47, "grandfathered skill can evolve at its rank level while spending a point");
            Check(!service.LearnSkill(7) && service.Profile.skillRanks[7] == 0, "grandfathering does not unlock another unlearned branch");
            service.Save();
            var restored = new ProgressionService();
            Check(restored.Load() && restored.Profile.skillRanks[9] == 2 && restored.Profile.skillPoints == 47 && restored.Profile.equippedSkills[0] == 9 && SpentPoints(restored) == 2, "legacy learned skill and point balance survive another save round trip");
        }
    }

    private static void LegacyThreeSkillSaveMigratesWithoutLoss()
    {
        var service = Fresh(HeroClass.Arcanist);
        ReachLevel(service, 12);
        for (int i = 0; i < 3; i++) service.LearnSkill(0);
        for (int i = 0; i < 2; i++) service.LearnSkill(1);
        service.LearnSkill(2);
        service.GrantExperience(37);
        service.AddGold(174);
        service.UsePotion();
        service.Profile.kills = 44;
        service.Profile.clearedRuns = 2;
        service.Profile.bestFloor = 2;
        service.Save();
        string weaponId = service.Profile.weaponId;
        string armorId = service.Profile.armorId;
        string relicId = service.Profile.relicId;
        string path = SavePath();
        JsonNode legacy = JsonNode.Parse(File.ReadAllText(path));
        legacy["profile"]["skillRanks"] = JsonNode.Parse("[3,2,1]");
        legacy["profile"]["skillPoints"] = 5;
        legacy["profile"]["equippedSkills"] = JsonNode.Parse("[2,0,1]");
        ((JsonObject)legacy["profile"]).Remove("hotbarKeys");
        ((JsonObject)legacy["profile"]).Remove("hotbarPage");
        File.WriteAllText(path, legacy.ToJsonString());
        var migrated = new ProgressionService();
        Check(migrated.Load(), "legacy version-1 three-skill save loads");
        GameProfile profile = migrated.Profile;
        Check(profile.version == 1 && profile.heroClass == HeroClass.Arcanist && profile.level == 12 && profile.xp == 37, "migration retains class level XP and version");
        Check(profile.gold == 234 && profile.potions == 4 && profile.kills == 44 && profile.clearedRuns == 2 && profile.bestFloor == 2, "migration retains currencies and completed progress");
        Check(profile.inventory.Count == 3 && profile.weaponId == weaponId && profile.armorId == armorId && profile.relicId == relicId, "migration retains inventory and equipped item IDs");
        Check(profile.skillRanks.Length == 10 && profile.skillRanks[0] == 2 && profile.skillRanks[1] == 2 && profile.skillRanks[2] == 1 && profile.skillPoints == 6, "newly locked old rank is refunded while eligible learned ranks survive");
        Check(migrated.LastError.Contains("返还"), "legacy refund is explained to the player after loading");
        Check(profile.skillPoints + profile.skillRanks[0] + profile.skillRanks[1] + profile.skillRanks[2] == 11, "migration conserves every level-earned skill point");
        for (int i = 3; i < 10; i++) Check(profile.skillRanks[i] == 0, "new skills remain unlearned after migration");
        Check(profile.equippedSkills.Length == 30 && profile.equippedSkills[0] == 2 && profile.equippedSkills[1] == 0 && profile.equippedSkills[2] == 1, "legacy three-slot order survives migration");
        int[] migratedDefault = { 0, 1, 2, 4, 5, 6, 7, 9, -1, -1 };
        for (int i = 3; i < 10; i++) Check(profile.equippedSkills[i] == migratedDefault[i], "remaining active default skills fill first legacy page");
        for (int i = 10; i < 30; i++) Check(profile.equippedSkills[i] == -1, "additional migrated hotbar pages start empty");
        Check(profile.hotbarPage == 0, "legacy character starts on first hotbar page");
        CheckHotbarKeys(profile.hotbarKeys, "legacy character receives ten valid unique hotkeys");
        migrated.Save();
        var reloaded = new ProgressionService();
        Check(reloaded.Load() && reloaded.Profile.skillRanks.Length == 10 && reloaded.Profile.skillRanks[0] == 2 && reloaded.Profile.skillPoints == 6, "migrated ten-skill profile and refunded points persist");
        Check(string.IsNullOrEmpty(reloaded.LastError), "already migrated save does not repeat a refund warning");
        ReachLevel(reloaded, 20);
        Check(reloaded.LearnSkill(0) && reloaded.Profile.skillRanks[0] == 3 && reloaded.Profile.equippedSkills[1] == 0, "refunded original skill can be evolved at its new gate without changing its identity or loadout");
    }

    private static void MalformedSkillArraysAreRepaired()
    {
        var service = Fresh();
        WriteSkillFixture("[-4,5,2,3,99,1,0,8,3,2,9,9]", "[9,8,7]", 40);
        Check(service.Load(), "oversized malformed skill array loads with repair");
        int[] expected = { 0, 3, 2, 3, 3, 1, 0, 3, 3, 2 };
        Check(service.Profile.skillRanks.Length == 10, "excess skill entries removed");
        for (int i = 0; i < expected.Length; i++) Check(service.Profile.skillRanks[i] == expected[i], "each rank bounded to zero through three");
        Check(service.Profile.skillPoints == 19, "point budget reconstructed after rank and level-gate repair");
        Check(service.Profile.equippedSkills[0] == 9 && service.Profile.equippedSkills[1] == -1 && service.Profile.equippedSkills[2] == 7, "valid active assignments survive while a mapped passive is cleared");
        string[] malformedLoadouts = { "null", "[]", "[0]", "[0,1,2,3]", "[0,0,2]", "[-1,1,2]", "[10,1,2]" };
        for (int i = 0; i < malformedLoadouts.Length; i++)
        {
            WriteSkillFixture(i % 2 == 0 ? "null" : "[]", malformedLoadouts[i], 40);
            Check(service.Load() && service.Profile.skillRanks.Length == 10 && service.Profile.skillPoints == 39, "missing ranks safely repaired without dropping earned points");
            CheckLoadout(service.Profile, "malformed loadout repaired into three pages without duplicates");
            if (i == 0) CheckDefaultLoadout(service.Profile, "null loadout gets complete default first page");
            if (i == 1) Check(Array.TrueForAll(service.Profile.equippedSkills, value => value == -1), "empty corrupted loadout stays empty rather than inventing assignments");
            if (i == 4) Check(service.Profile.equippedSkills[0] == 0 && service.Profile.equippedSkills[1] == -1 && service.Profile.equippedSkills[2] == 2, "duplicate same-page assignment clears only the duplicate");
            if (i >= 5) Check(service.Profile.equippedSkills[0] == -1 && service.Profile.equippedSkills[1] == 1, "invalid assignment is cleared while valid neighbors survive");
        }
        WriteSkillFixture("[1]", "[0,1,2]", 2);
        Check(service.Load() && service.Profile.skillRanks.Length == 10 && service.Profile.skillRanks[0] == 1 && service.Profile.skillPoints == 0, "short rank array pads missing entries");
        CheckDefaultLoadout(service.Profile, "default mappings remain valid while later skills are still locked");
        WriteSkillFixture("[3,3,3,3,3,3,3,3,3,3]", "[0,1,2]", 2);
        Check(service.Load() && service.Profile.skillRanks[0] == 1 && service.Profile.skillPoints == 0, "rank spending cannot exceed level-earned point budget");
        for (int i = 1; i < 10; i++) Check(service.Profile.skillRanks[i] == 0, "locked higher skills cannot survive corrupted ranks");
    }

    private static void SkillLoadoutAssignmentAndPersistence()
    {
        var service = Fresh(HeroClass.Ranger);
        CheckDefaultLoadout(service.Profile, "new character has eight default active mappings and two empty pages");
        Check(!service.AssignSkill(1, 1), "cannot assign an unlearned default skill");
        ReachLevel(service, 2);
        service.LearnSkill(0);
        Check(service.AssignSkill(1, 0), "learned skill can move onto a locked default slot");
        Check(service.Profile.equippedSkills[0] == 1 && service.Profile.equippedSkills[1] == 0 && service.Profile.equippedSkills[2] == 2, "occupied skill assignment swaps instead of duplicating");
        Check(service.AssignSkill(1, 0) && service.Profile.equippedSkills[0] == 1, "assigning to same slot is idempotent");
        Check(!service.AssignSkill(-1, 0) && !service.AssignSkill(10, 0) && !service.AssignSkill(0, -2) && !service.AssignSkill(0, 10), "invalid loadout slots and skill IDs rejected");
        Check(!service.AssignSkill(2, 9) && service.Profile.equippedSkills[2] == 2, "unlearned higher skill rejected without changing loadout");
        Check(service.AssignSkill(2, -1) && service.Profile.equippedSkills[2] == -1, "negative-one assignment clears a locked or learned slot");
        ReachLevel(service, 30);
        LearnBranch(service, 9);
        LearnBranch(service, 7);
        int points = service.Profile.skillPoints;
        int changes = 0;
        service.Changed += () => changes++;
        Check(service.AssignSkill(0, 9) && service.AssignSkill(2, 7), "learned high-tier skills can replace unused defaults");
        Check(service.AssignSkill(1, 9), "equipped high-tier skill can swap slots");
        Check(service.Profile.equippedSkills[0] == 0 && service.Profile.equippedSkills[1] == 9 && service.Profile.equippedSkills[2] == 7, "swap preserves previous target skill and uniqueness");
        Check(service.Profile.skillPoints == points && changes == 3, "loadout changes spend no points and notify observers");
        var restored = new ProgressionService();
        Check(restored.Load() && restored.Profile.equippedSkills[0] == 0 && restored.Profile.equippedSkills[1] == 9 && restored.Profile.equippedSkills[2] == 7, "loadout automatically persists to disk");
        Check(service.SetHotbarPage(1) && service.Profile.hotbarPage == 1, "second skill page can be selected");
        Check(service.AssignSkill(0, 9) && service.Profile.equippedSkills[10] == 9 && service.Profile.equippedSkills[1] == 9, "same skill is allowed on different pages");
        Check(service.AssignSkill(9, 9) && service.Profile.equippedSkills[10] == -1 && service.Profile.equippedSkills[19] == 9, "duplicate swap stays within selected page");
        Check(service.SetHotbarPage(2) && service.AssignSkill(0, 7) && service.AssignSkill(9, 0), "third page can hold an independent loadout");
        Check(service.AssignSkill(9, -1) && service.Profile.equippedSkills[29] == -1, "clearing third-page slot leaves first-page assignment intact");
        Check(!service.SetHotbarPage(-1) && !service.SetHotbarPage(3) && service.Profile.hotbarPage == 2, "invalid page requests preserve current page");
        restored = new ProgressionService();
        Check(restored.Load() && restored.Profile.hotbarPage == 2 && restored.Profile.equippedSkills[20] == 7 && restored.Profile.equippedSkills[19] == 9 && restored.Profile.equippedSkills[1] == 9, "all pages and selected page persist across reload");
        CheckLoadout(restored.Profile, "multi-page assignments remain unique within each page");
    }

    private static void CustomHotbarKeysAndRepair()
    {
        var service = Fresh();
        CheckHotbarKeys(service.Profile.hotbarKeys, "new character receives ten distinct bindable keys");
        for (int i = 0; i < 10; i++) Check(service.Profile.hotbarKeys[i] == GameBalance.DefaultHotbarKeys[i], "default key order is preserved");
        Check(service.SetHotbarKey(0, 120) && service.Profile.hotbarKeys[0] == 120 && service.Profile.hotbarKeys[1] == 122, "binding an occupied key swaps the two bindings");
        Check(GameBalance.DefaultHotbarKeys[0] == 122 && GameBalance.DefaultHotbarKeys[1] == 120, "user bindings cannot mutate global defaults");
        Check(service.SetHotbarKey(0, 120) && service.Profile.hotbarKeys[1] == 122, "same-slot key reassignment is idempotent");
        Check(service.SetHotbarKey(9, 101) && service.Profile.hotbarKeys[9] == 101, "unused letter key can be assigned");
        Check(service.SetHotbarKey(3, 49) && service.Profile.hotbarKeys[3] == 49 && service.Profile.hotbarKeys[5] == 118, "digit binding swaps existing hotkey");
        int[] invalid = { 0, 27, 32, 97, 100, 102, 105, 106, 107, 115, 119, 999 };
        foreach (int key in invalid)
            Check(!service.SetHotbarKey(0, key) && service.Profile.hotbarKeys[0] == 120, "reserved and unsupported keys cannot replace a valid binding");
        Check(!service.SetHotbarKey(-1, 101) && !service.SetHotbarKey(10, 101), "hotkey slot bounds enforced");
        if (GameBalance.IsBindableKey(282)) Check(service.SetHotbarKey(8, 282), "F1 can be assigned when enabled by key policy");
        if (GameBalance.IsBindableKey(293)) Check(service.SetHotbarKey(7, 293), "F12 can be assigned when enabled by key policy");
        int[] expected = (int[])service.Profile.hotbarKeys.Clone();
        var restored = new ProgressionService();
        Check(restored.Load(), "custom keyboard layout loads");
        for (int i = 0; i < 10; i++) Check(restored.Profile.hotbarKeys[i] == expected[i], "custom key order persists");
        string[] damaged = { "null", "[]", "[120]", "[120,120,97,0,999,282,101,101,49,49,50]" };
        for (int i = 0; i < damaged.Length; i++)
        {
            WriteSkillFixture("null", "null", 1, damaged[i], i % 2 == 0 ? -1 : 100);
            Check(service.Load() && service.Profile.hotbarPage == 0, "invalid selected page returns to page one");
            CheckHotbarKeys(service.Profile.hotbarKeys, "short or corrupt key array repaired to ten valid unique keys");
            if (i == 2) Check(service.Profile.hotbarKeys[0] == 120 && service.Profile.hotbarKeys[1] == 122, "short key repair preserves chosen key and fills defaults without duplication");
            if (i == 3) Check(service.Profile.hotbarKeys[0] == 120 && service.Profile.hotbarKeys[6] == 101 && service.Profile.hotbarKeys[8] == 49, "key repair preserves first valid occurrence in each duplicate group");
        }
        WriteSkillFixture("null", "null", 1, "[113,120,99,118,98,49,50,51,52,53]", 0);
        Check(service.Load() && service.Profile.hotbarKeys[0] == 113, "Q remains a valid skill binding while Space controls jumping");
    }

    private static void TenSkillRanksAndPointBudget()
    {
        var service = Fresh();
        service.GrantExperience(int.MaxValue);
        for (int skill = 0; skill < 10; skill++)
        {
            for (int rank = service.Profile.skillRanks[skill]+1; rank <= 3; rank++)
                Check(service.LearnSkill(skill) && service.Profile.skillRanks[skill] == rank, "all ten skills support three learned ranks");
            Check(!service.LearnSkill(skill) && service.Profile.skillRanks[skill] == 3, "every skill rejects a fourth rank");
        }
        Check(service.Profile.skillPoints == 69, "ten max-rank skills spend exactly thirty lifetime points");
        var restored = new ProgressionService();
        Check(restored.Load() && restored.Profile.skillRanks.Length == 10 && restored.Profile.skillPoints == 69, "all-ten-rank cap persists");
        for (int skill = 0; skill < 10; skill++) Check(restored.Profile.skillRanks[skill] == 3, "each maxed skill survives reload");
    }

    private static void PassiveStatsAreImmediateAndPersistent()
    {
        Check(GameBalance.IsPassive(3) && GameBalance.IsPassive(8), "exactly designated passive IDs are marked passive");
        int activeCount = 0;
        for (int i = 0; i < 10; i++) if (!GameBalance.IsPassive(i)) activeCount++;
        Check(activeCount == 8, "each class exposes eight active skills");
        for (int hero = 0; hero < Enum.GetValues(typeof(HeroClass)).Length; hero++)
        {
            var service = Fresh((HeroClass)hero);
            int updates = 0;
            service.Changed += () => updates++;
            for (int rank = 1; rank <= 3; rank++)
            {
                ReachLevel(service, GameBalance.SkillRankRequiredLevel(3, rank));
                if (rank == 1) LearnPrerequisites(service, 3);
                int oldRank = service.Profile.skillRanks[3];
                service.Profile.skillRanks[3] = 0;
                StatBlock baseline = service.GetStats();
                service.Profile.skillRanks[3] = oldRank;
                int beforeUpdates = updates;
                Check(service.LearnSkill(3) && updates == beforeUpdates + 1, "passive learning immediately emits stat refresh notification");
                Check(!service.AssignSkill(9, 3), "learned passive cannot be assigned to an empty active slot");
                StatBlock actual = service.GetStats();
                if (hero == 0)
                {
                    float multiplier = rank == 1 ? 1.08f : rank == 2 ? 1.14f : 1.22f;
                    float armor = rank == 1 ? 2f : rank == 2 ? 4f : 7f;
                    Check(Near(actual.Damage, baseline.Damage * multiplier) && Near(actual.Armor, baseline.Armor + armor), "sword passive multiplies equipped damage and adds armor by rank");
                    Check(Near(actual.MaxHealth, baseline.MaxHealth) && Near(actual.MoveSpeed, baseline.MoveSpeed), "sword passive leaves unrelated stats unchanged");
                }
                else if (hero == 1)
                {
                    float damage = rank == 1 ? 1.06f : rank == 2 ? 1.11f : 1.18f;
                    float health = rank == 1 ? 1.04f : rank == 2 ? 1.07f : 1.10f;
                    Check(Near(actual.Damage, baseline.Damage * damage) && Near(actual.MaxHealth, baseline.MaxHealth * health), "mage passive multiplies equipped damage and health by rank");
                    Check(Near(actual.Armor, baseline.Armor) && Near(actual.CritChance, baseline.CritChance), "mage passive leaves unrelated stats unchanged");
                }
                else if ((HeroClass)hero == HeroClass.Summoner)
                {
                    float damage = rank == 1 ? 1.06f : rank == 2 ? 1.11f : 1.18f;
                    Check(Near(actual.Damage, baseline.Damage * damage), "summoner pact passive multiplies equipped damage by rank");
                    Check(Near(actual.MaxHealth, baseline.MaxHealth) && Near(actual.MoveSpeed, baseline.MoveSpeed) && Near(actual.CritChance, baseline.CritChance), "summoner passive does not inherit ranger or mage extra stats");
                }
                else
                {
                    float crit = rank == 1 ? .04f : rank == 2 ? .07f : .12f;
                    float speed = rank == 1 ? 1.03f : rank == 2 ? 1.06f : 1.10f;
                    Check(Near(actual.CritChance, baseline.CritChance + crit) && Near(actual.MoveSpeed, baseline.MoveSpeed * speed), "ranger passive grants critical chance and movement speed by rank");
                    Check(Near(actual.Damage, baseline.Damage) && Near(actual.MaxHealth, baseline.MaxHealth), "ranger passive leaves unrelated stats unchanged");
                }
                var restored = new ProgressionService();
                Check(restored.Load() && restored.Profile.skillRanks[3] == rank, "passive rank persists immediately");
                StatBlock savedStats = restored.GetStats();
                Check(Near(savedStats.Damage, actual.Damage) && Near(savedStats.Armor, actual.Armor) && Near(savedStats.MaxHealth, actual.MaxHealth) && Near(savedStats.MoveSpeed, actual.MoveSpeed) && Near(savedStats.CritChance, actual.CritChance), "passive-adjusted stats survive save reload");
            }
            ReachLevel(service, GameBalance.SkillRankRequiredLevel(8, 3));
            LearnPrerequisites(service, 8);
            StatBlock beforeReactive = service.GetStats();
            for (int rank = 1; rank <= 3; rank++) Check(service.LearnSkill(8), "reactive passive can evolve through three ranks");
            Check(!service.AssignSkill(9, 8), "reactive passive cannot be placed on an active hotbar");
            StatBlock afterReactive = service.GetStats();
            Check(Near(beforeReactive.Damage, afterReactive.Damage) && Near(beforeReactive.Armor, afterReactive.Armor) && Near(beforeReactive.MaxHealth, afterReactive.MaxHealth) && Near(beforeReactive.MoveSpeed, afterReactive.MoveSpeed) && Near(beforeReactive.CritChance, afterReactive.CritChance), "reactive passive is handled in combat without a permanent stat bonus");
            CheckLoadout(service.Profile, "learning passives never inserts them into any active page");
        }
    }

    private static bool Near(float actual, float expected) { return Math.Abs(actual - expected) < .002f; }

    private static void SaveTransfersBetweenIndependentDirectories()
    {
        var source = Fresh(HeroClass.Ranger);
        ReachLevel(source, 50);
        source.GrantExperience(23);
        source.AddGold(987);
        for (int skill = 0; skill < 10; skill++)
            for (int rank = source.Profile.skillRanks[skill]; rank < 3; rank++) Check(source.LearnSkill(skill), "portable source learns each skill rank");
        ItemData equipment = source.CreateLoot(1, true);
        Check(source.Equip(equipment.id) && source.Upgrade(equipment.id), "portable source receives and upgrades equipped loot");
        source.CreateLoot(40, true);
        source.UsePotion();
        source.Profile.kills = 123;
        source.Profile.clearedRuns = 5;
        source.Profile.bestFloor = 5;
        Check(source.SetHotbarPage(2) && source.AssignSkill(0, 9) && source.AssignSkill(9, 7), "portable source configures third-page skills");
        Check(source.SetHotbarKey(0, 101) && source.SetHotbarKey(9, 282), "portable source configures custom keys");
        source.Save();
        string sourceJson = File.ReadAllText(source.SaveFilePath);
        string profileJson = UnityEngine.JsonUtility.ToJson(source.Profile, true);
        Check(source.SaveDirectory == Path.GetDirectoryName(source.SaveFilePath) && Path.GetFileName(source.SaveFilePath) == "emberfall-save.json", "save directory and file path are exposed for export instructions");
        string unescaped = sourceJson.Replace("\\\\", "\\").Replace("\\/", "/");
        Check(unescaped.IndexOf(source.SaveDirectory, StringComparison.OrdinalIgnoreCase) < 0 && unescaped.IndexOf("\"saveDirectory\"", StringComparison.OrdinalIgnoreCase) < 0 && unescaped.IndexOf("\"machineId\"", StringComparison.OrdinalIgnoreCase) < 0, "save JSON contains no source path or machine identity");
        string destination = Path.Combine(root, "case-" + (++cases));
        Directory.CreateDirectory(destination);
        File.Copy(source.SaveFilePath, Path.Combine(destination, "emberfall-save.json"));
        Check(Directory.GetFiles(destination).Length == 1, "migration copies only the primary JSON file");
        var imported = new ProgressionService(destination);
        Check(imported.SaveDirectory == destination && imported.HasSave && imported.Load(), "fresh service discovers transferred save in its own directory");
        Check(imported.Profile.heroClass == HeroClass.Ranger && imported.Profile.level == 50 && imported.Profile.xp == 23 && imported.Profile.skillPoints == 19, "transferred class, level, XP and unspent points remain intact");
        Check(imported.Profile.inventory.Count == source.Profile.inventory.Count && imported.Profile.weaponId == source.Profile.weaponId && imported.Profile.armorId == source.Profile.armorId && imported.Profile.relicId == source.Profile.relicId, "transferred inventory and equipped IDs remain intact");
        Check(imported.Profile.hotbarPage == 2 && imported.Profile.equippedSkills[20] == 9 && imported.Profile.equippedSkills[29] == 7 && imported.Profile.hotbarKeys[0] == 101 && imported.Profile.hotbarKeys[9] == 282, "transferred pages and custom key bindings remain intact");
        Check(UnityEngine.JsonUtility.ToJson(imported.Profile, true) == profileJson, "every serialized profile field survives cross-directory migration exactly");
        imported.AddGold(1);
        Check(File.ReadAllText(source.SaveFilePath) == sourceJson, "saving imported character cannot modify source installation save");
        string cleanDirectory = Path.Combine(root, "case-" + (++cases));
        var clean = new ProgressionService(cleanDirectory);
        Check(!clean.HasSave && !Directory.Exists(cleanDirectory), "third untouched installation has no save and constructor creates no files");
        clean.NewGame(HeroClass.Arcanist);
        Check(clean.HasSave && File.Exists(clean.SaveFilePath) && clean.Profile.heroClass == HeroClass.Arcanist && clean.Profile.level == 1 && clean.Profile.skillPoints == 0, "third installation can start a new independent character");
        Check(File.ReadAllText(source.SaveFilePath) == sourceJson && imported.Profile.heroClass == HeroClass.Ranger, "independent new game leaves source and imported characters intact");
    }

    private static void ReachLevel(ProgressionService service, int level)
    {
        int xp = -service.Profile.xp;
        for (int current = service.Profile.level; current < level; current++) xp += GameBalance.XpToNext(current);
        if (xp > 0) service.GrantExperience(xp);
    }

    private static void LearnPrerequisites(ProgressionService service, int skill)
    {
        foreach (int parent in GameBalance.SkillPrerequisites[skill]) LearnBranch(service, parent);
    }

    private static void LearnBranch(ProgressionService service, int skill)
    {
        if (service.Profile.skillRanks[skill] > 0) return;
        LearnPrerequisites(service, skill);
        Check(service.LearnSkill(skill) && service.Profile.skillRanks[skill] == 1, "fixture learns prerequisite through production LearnSkill: " + skill);
    }

    private static int SpentPoints(ProgressionService service)
    {
        int spent = 0;
        foreach (int rank in service.Profile.skillRanks) spent += rank;
        return spent;
    }

    private static string SavePath()
    {
        return Path.Combine(UnityEngine.Application.persistentDataPath, "emberfall-save.json");
    }

    private static void WriteSkillFixture(string ranks, string loadout, int level, string keys = "null", int page = 0)
    {
        File.WriteAllText(SavePath(), "{\"format\":\"emberfall-character\",\"version\":1,\"profile\":{\"version\":1,\"level\":" + level + ",\"skillRanks\":" + ranks + ",\"equippedSkills\":" + loadout + ",\"hotbarKeys\":" + keys + ",\"hotbarPage\":" + page + "}}");
    }

    private static void CheckDefaultLoadout(GameProfile profile, string description)
    {
        int[] expected = { 0, 1, 2, 4, 5, 6, 7, 9, -1, -1 };
        bool valid = profile.equippedSkills != null && profile.equippedSkills.Length == 30;
        if (valid)
            for (int i = 0; i < 30; i++) if (profile.equippedSkills[i] != (i < 10 ? expected[i] : -1)) valid = false;
        Check(valid, description);
    }

    private static void CheckLoadout(GameProfile profile, string description)
    {
        bool valid = profile.equippedSkills != null && profile.equippedSkills.Length == 30;
        if (valid)
            for (int page = 0; page < 3; page++)
            {
                var used = new HashSet<int>();
                for (int slot = 0; slot < 10; slot++)
                {
                    int skill = profile.equippedSkills[page * 10 + slot];
                    if (skill == -1) continue;
                    if ((skill != GameBalance.HotbarPotion && (skill < 0 || skill >= 10 || GameBalance.IsPassive(skill))) || !used.Add(skill)) valid = false;
                }
            }
        Check(valid, description);
    }

    private static void CheckHotbarKeys(int[] keys, string description)
    {
        bool valid = keys != null && keys.Length == 10;
        if (valid)
        {
            var used = new HashSet<int>();
            foreach (int key in keys) if (!GameBalance.IsBindableKey(key) || !used.Add(key)) valid = false;
        }
        Check(valid, description);
    }

    private static void Check(bool condition, string description)
    {
        assertions++;
        if (!condition) throw new Exception("Assertion " + assertions + " failed: " + description);
    }
}
