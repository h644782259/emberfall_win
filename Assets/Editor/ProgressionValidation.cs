using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Emberfall.Editor
{
    /// <summary>Runs production code inside Unity, including Unity's real JsonUtility.</summary>
    public static class ProgressionValidation
    {
        [Serializable]
        private sealed class ValidationReport
        {
            public string status = "RUNNING";
            public string unityVersion;
            public string startedUtc;
            public string completedUtc;
            public string outputDirectory;
            public int assertions;
            public List<string> passedStages = new List<string>();
            public string failure = "";
        }

        [Serializable]
        private sealed class LegacyProfile
        {
            public int version = 1;
            public HeroClass heroClass = HeroClass.Arcanist;
            public int level = 12;
            public int xp = 37;
            public int gold = 321;
            public int potions = 7;
            public int skillPoints = 5;
            public int[] skillRanks = { 3, 2, 1 };
            public int kills = 45;
            public int clearedRuns = 3;
            public int bestFloor = 3;
            public List<ItemData> inventory;
            public string weaponId;
            public string armorId;
            public string relicId;
        }

        [Serializable]
        private sealed class LegacyEquipmentItem
        {
            public string id, name;
            public ItemSlot slot;
            public Rarity rarity;
            public int level, attack, defense, health, upgradeLevel;
        }

        [Serializable]
        private sealed class LegacyEquipmentProfile
        {
            public int version = 1;
            public HeroClass heroClass;
            public int level, xp, gold, potions;
            public int[] skillRanks;
            public string weaponId, armorId, relicId;
            public List<LegacyEquipmentItem> inventory = new List<LegacyEquipmentItem>();
        }

        private static ValidationReport report;
        [Serializable] private sealed class SaveEnvelopeView { public GameProfile profile; }

        [MenuItem("Emberfall/Validate Progression and Skills")]
        public static void Validate()
        {
            string workspace = Path.GetFullPath(Path.GetDirectoryName(Application.dataPath));
            string output = Path.GetFullPath(Path.Combine(workspace, "Tests", "TestResults", "unity-progression-" + Guid.NewGuid().ToString("N")));
            string workspacePrefix = workspace.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!output.StartsWith(workspacePrefix, StringComparison.OrdinalIgnoreCase)) throw new Exception("Validation directory must be inside this Unity project.");
            Directory.CreateDirectory(output);
            report = new ValidationReport
            {
                unityVersion = Application.unityVersion,
                startedUtc = DateTime.UtcNow.ToString("o"),
                outputDirectory = output
            };
            try
            {
                if (Application.version == "0.4.0")
                {
                    ValidateCurrentRelease();
                    report.status = "PASS";
                    Debug.Log("Emberfall Unity validation PASS: " + report.assertions + " assertions. Report: " + Path.Combine(output, "validation-report.json"));
                    return;
                }
                for (int hero = 0; hero < Enum.GetValues(typeof(HeroClass)).Length; hero++)
                {
                    ValidateClass((HeroClass)hero);
                    ValidateSkillTreeGates((HeroClass)hero);
                    ValidateSkillTreeBranches((HeroClass)hero);
                    ValidatePreviouslyLearnedTreeSkill((HeroClass)hero);
                }
                ValidateLegacyJson();
                ValidateDamagedJson();
                ValidateBackupRecovery();
                ValidateSkillRuntime();
                ValidatePortableSaveTransfer();
                ValidateUpgradeTransfers();
                ValidateLegacyEquipment();
                ValidateLootCollection();
                ValidateHotbarDrag();
                ValidateConsumableHotbar();
                ValidateMultipleSaveSlots();
                ValidateBuildPresets();
                report.status = "PASS";
                Debug.Log("Emberfall Unity validation PASS: " + report.assertions + " assertions. Report: " + Path.Combine(output, "validation-report.json"));
            }
            catch (Exception exception)
            {
                report.status = "FAIL";
                report.failure = exception.ToString();
                Debug.LogError("Emberfall Unity validation failed. Report: " + Path.Combine(output, "validation-report.json") + "\n" + exception);
                throw;
            }
            finally
            {
                report.completedUtc = DateTime.UtcNow.ToString("o");
                File.WriteAllText(Path.Combine(output, "validation-report.json"), JsonUtility.ToJson(report, true), new UTF8Encoding(false));
                var summary = new StringBuilder();
                summary.AppendLine(report.status + ": " + report.assertions + " assertions; Unity " + report.unityVersion);
                foreach (string stage in report.passedStages) summary.AppendLine("PASS " + stage);
                if (!string.IsNullOrEmpty(report.failure)) summary.AppendLine(report.failure);
                File.WriteAllText(Path.Combine(output, "validation-report.txt"), summary.ToString(), new UTF8Encoding(false));
            }
        }

        // The historical suite below assumes no learned starter skill and old reward rules.
        // Exercise current budgets and durable receipt paths with Unity's real serializer.
        private static void ValidateCurrentRelease()
        {
            foreach (HeroClass hero in Enum.GetValues(typeof(HeroClass)))
            {
                string name = "release040-" + hero;
                ProgressionService service = Fresh(name, hero);
                Check(service.Profile.pendingChestDraw == null && service.Profile.lastChestReward == null,
                    name + ": absent receipts remain absent before and after serialization");
                Check(File.ReadAllText(service.SaveFilePath).Contains("\"pendingChestDraw\": null"),
                    name + ": null draw is explicit in the real Unity JSON document");
                service.GrantExperience(int.MaxValue);
                int spent = 0;
                foreach (int rank in service.Profile.skillRanks) spent += rank;
                foreach (int rank in service.Profile.masteryRanks) spent += rank;
                Check(service.Profile.level == ProgressionService.MaximumLevel &&
                    service.Profile.skillPoints + spent == GameBalance.SkillPointBudget(service.Profile.level) &&
                    string.IsNullOrEmpty(service.LastError), name + ": maximum XP conserves current starter-point budget and saves successfully");
                Check(service.Profile.equippedSkills.Length == GameBalance.HotbarSize * GameBalance.HotbarPages &&
                    service.Profile.hotbarKeys.Length == GameBalance.HotbarSize,
                    name + ": saved loadout and key slots match the current schema");
                int gold = service.Profile.gold;
                service.AddGold(123);
                Check(service.Profile.gold == gold + 123 && string.IsNullOrEmpty(service.LastError), name + ": later saves accept the original empty receipts");
                var restored = new ProgressionService(Path.GetDirectoryName(service.SaveFilePath));
                Check(restored.Load() && restored.Profile.level == service.Profile.level && restored.Profile.gold == service.Profile.gold,
                    name + ": real Unity reload preserves progress and currency");
                Check(restored.Profile.pendingChestDraw == null && restored.Profile.lastThreadMaterialReceipt == null,
                    name + ": Unity deserialization restores absent optional receipts");
                Check(SameStats(service.GetStats(), restored.GetStats()), name + ": equipped stats survive a real JSON roundtrip");
                restored.Profile.clearedRuns++;
                Check(restored.PrepareDungeonChest(), name + ": a new chest qualification commits through a cloned profile");
                Check(restored.Load() && restored.Profile.pendingFashionChest && restored.Profile.pendingChestDraw == null,
                    name + ": unopened qualification persists without a phantom frozen draw");
                gold = restored.Profile.gold;
                int threads = restored.Profile.fashionThreads, materials = restored.Profile.mechanicMaterials;
                Check(!string.IsNullOrEmpty(restored.OpenDungeonChest()) && string.IsNullOrEmpty(restored.LastError),
                    name + ": actual random draw freezes and grants through durable writes");
                string receiptId = restored.LastChestReward.id;
                Check(restored.Profile.gold > gold && restored.Profile.fashionThreads > threads &&
                    restored.Profile.mechanicMaterials == materials + 1 && restored.Profile.pendingChestReveal,
                    name + ": guaranteed resources and committed reveal are present");
                gold = restored.Profile.gold; threads = restored.Profile.fashionThreads; materials = restored.Profile.mechanicMaterials;
                Check(restored.Load() && restored.LastChestReward.id == receiptId && restored.Profile.pendingChestDraw == null,
                    name + ": durable reward survives restart without a frozen draw");
                Check(restored.OpenDungeonChest() == null && restored.Profile.gold == gold &&
                    restored.Profile.fashionThreads == threads && restored.Profile.mechanicMaterials == materials,
                    name + ": retrying a committed chest does not grant twice");
                Check(restored.AcknowledgeChestReward() && restored.Load() && !restored.Profile.pendingChestReveal,
                    name + ": acknowledgement persists");
                string primary = File.ReadAllText(restored.SaveFilePath), backup = File.ReadAllText(restored.SaveFilePath + ".bak");
                string temporary = restored.SaveFilePath + ".tmp";
                Directory.CreateDirectory(temporary);
                try
                {
                    Check(!restored.BuyPotion() && File.ReadAllText(restored.SaveFilePath) == primary &&
                        File.ReadAllText(restored.SaveFilePath + ".bak") == backup,
                        name + ": blocked write preserves both durable documents");
                }
                finally { Directory.Delete(temporary); }
                Check(restored.BuyPotion() && restored.Load(), name + ": transaction retries after storage recovery");
                // A real frozen record is still protected; a malformed primary cannot
                // silently fall back to an older backup and reroll its reward.
                Check(restored.PrepareDungeonChest(), name + ": corruption fixture creates a fresh qualification");
                primary = File.ReadAllText(restored.SaveFilePath);
                string corrupt = primary.Replace("\"pendingChestDraw\": null", "\"pendingChestDraw\": {\"id\":\"frozen-invalid\",\"rulesRevision\":2}");
                Check(corrupt != primary, name + ": fixture replaces only the absent draw");
                File.WriteAllText(restored.SaveFilePath, corrupt);
                backup = File.ReadAllText(restored.SaveFilePath + ".bak");
                Check(!new ProgressionService(Path.GetDirectoryName(restored.SaveFilePath)).Load() &&
                    File.ReadAllText(restored.SaveFilePath) == corrupt && File.ReadAllText(restored.SaveFilePath + ".bak") == backup,
                    name + ": malformed frozen reward preserves primary and backup without reroll recovery");
                foreach (string invalidDraw in new[] { "0", "\"invalid\"", "[]" })
                {
                    corrupt = primary.Replace("\"pendingChestDraw\": null", "\"pendingChestDraw\": " + invalidDraw);
                    File.WriteAllText(restored.SaveFilePath, corrupt);
                    Check(!new ProgressionService(Path.GetDirectoryName(restored.SaveFilePath)).Load() &&
                        File.ReadAllText(restored.SaveFilePath) == corrupt && File.ReadAllText(restored.SaveFilePath + ".bak") == backup,
                        name + ": non-object frozen draw remains protected");
                }
                report.passedStages.Add(name + ": current loadout, real JSON null receipts, progression, chest exactly-once grant and storage guards");
            }
            string existingDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "AppData", "LocalLow", "EmberfallStudio", "Emberfall");
            if (Directory.Exists(existingDirectory))
            {
                int index = 0;
                foreach (string source in Directory.GetFiles(existingDirectory, "emberfall-save*.json"))
                {
                    string original = File.ReadAllText(source);
                    GameProfile before = JsonUtility.FromJson<SaveEnvelopeView>(original).profile;
                    string directory = CaseDirectory("existing-save-" + index++);
                    Directory.CreateDirectory(directory);
                    string filename = Path.GetFileName(source);
                    File.Copy(source, Path.Combine(directory, filename));
                    if (File.Exists(source + ".bak")) File.Copy(source + ".bak", Path.Combine(directory, filename + ".bak"));
                    string id = filename == "emberfall-save.json" ? "legacy" : filename.Substring("emberfall-save-".Length, 32);
                    var imported = new ProgressionService(directory);
                    Check(imported.LoadSlot(id), "existing save copy: current Unity loads the previous installed version's role");
                    Check(imported.Profile.heroClass == before.heroClass && imported.Profile.level == before.level && imported.Profile.gold == before.gold,
                        "existing save copy: class, level and gold survive migration");
                    Check(imported.Profile.inventory.Count == before.inventory.Count &&
                        before.inventory.TrueForAll(item => imported.Profile.inventory.Exists(value => value.id == item.id)),
                        "existing save copy: every inventory identity survives migration");
                    imported.Save();
                    Check(string.IsNullOrEmpty(imported.LastError) && imported.LoadSlot(id), "existing save copy: migrated role saves and reloads successfully");
                    Check(File.ReadAllText(source) == original, "existing save copy: original personal save remains untouched");
                }
                report.passedStages.Add("isolated copies of " + index + " existing personal saves: migration, inventory retention and real JSON re-save");
            }
        }

        private static void ValidateLootCollection()
        {
            const string name = "world-loot";
            ProgressionService service = Fresh(name);
            int changed = 0;
            service.Changed += () => changed++;
            string before = JsonUtility.ToJson(service.Profile, true);
            string saved = File.ReadAllText(service.SaveFilePath);
            DateTime written = File.GetLastWriteTimeUtc(service.SaveFilePath);
            ItemData loot = service.RollLoot(0, false);
            // Explicit ordinary sale fixture; randomized mechanic locks have separate coverage.
            loot.mechanic = EquipmentMechanic.None;
            loot.locked = false;
            ItemData boss = service.RollLoot(int.MaxValue, true);
            Check(loot.id != boss.id && loot.level == 1 && boss.level == 100 && boss.rarity >= Rarity.Rare, "world loot: unique IDs, level caps and boss rarity");
            Check(loot.upgradeBaseInitialized && boss.upgradeBaseInitialized && loot.upgradeLevel == 0, "world loot: initialized permanent upgrade metadata");
            Check(JsonUtility.ToJson(service.Profile, true) == before && changed == 0, "world loot: rolling does not insert gear, award gold or emit events");
            Check(File.ReadAllText(service.SaveFilePath) == saved && File.GetLastWriteTimeUtc(service.SaveFilePath) == written, "world loot: rolling does not write a save");
            string originalItem = JsonUtility.ToJson(loot, true);
            int gold = service.Profile.gold;
            Check(service.CollectLoot(loot) && service.Profile.inventory.Count == 4 && changed == 1 && service.Profile.gold == gold, "world loot: first pickup grants exactly one item and event");
            Check(JsonUtility.ToJson(service.Profile.inventory.Find(item => item.id == loot.id), true) == originalItem, "world loot: pickup retains identity and every item attribute");
            var restored = new ProgressionService(service.SaveDirectory);
            Check(restored.Load() && restored.Profile.inventory.Exists(item => item.id == loot.id), "world loot: collected item survives real JsonUtility round trip");
            Check(!restored.CollectLoot(JsonUtility.FromJson<ItemData>(originalItem)) && restored.Profile.inventory.Count == 4, "world loot: saved inventory identity rejects repeated pickup after reload");
            before = JsonUtility.ToJson(service.Profile, true);
            saved = File.ReadAllText(service.SaveFilePath);
            written = File.GetLastWriteTimeUtc(service.SaveFilePath);
            Check(!service.CollectLoot(loot) && !service.CollectLoot(JsonUtility.FromJson<ItemData>(originalItem)), "world loot: object and copied-ID duplicate pickup rejected");
            Check(JsonUtility.ToJson(service.Profile, true) == before && changed == 1 && File.ReadAllText(service.SaveFilePath) == saved && File.GetLastWriteTimeUtc(service.SaveFilePath) == written, "world loot: duplicate causes no profile, event or save changes");
            Check(service.Sell(loot.id), "world loot: picked-up unequipped item can be sold");
            gold = service.Profile.gold;
            Check(!service.CollectLoot(loot) && service.Profile.gold == gold && service.Profile.inventory.Count == 3 && changed == 2, "world loot: sold receipt cannot be picked up again");
            before = JsonUtility.ToJson(service.Profile, true);
            Check(!service.CollectLoot(null) && !service.CollectLoot(new ItemData()) && !service.CollectLoot(new ItemData { id = "invalid-slot", slot = (ItemSlot)99 }) && !service.CollectLoot(new ItemData { id = "invalid-rarity", rarity = (Rarity)99 }), "world loot: malformed drops rejected");
            Check(JsonUtility.ToJson(service.Profile, true) == before && changed == 2, "world loot: invalid pickup cannot mutate progression");
            while (service.Profile.inventory.Count < ProgressionService.InventoryCapacity) service.CreateLoot(1, false);
            changed = 0;
            gold = service.Profile.gold;
            saved = File.ReadAllText(service.SaveFilePath);
            written = File.GetLastWriteTimeUtc(service.SaveFilePath);
            ItemData overflow = service.RollLoot(8, true);
            Check(service.Profile.inventory.Count == 72 && service.Profile.gold == gold && changed == 0 && File.ReadAllText(service.SaveFilePath) == saved && File.GetLastWriteTimeUtc(service.SaveFilePath) == written, "world loot: full bag does not convert an uncollected drop");
            bool protectedOverflow = ProgressionService.IsProtectedLoot(overflow);
            int value = protectedOverflow ? 0 : service.SellValue(overflow);
            Check(service.CollectLoot(overflow) && service.Profile.inventory.Count == 72 && service.Profile.gold == gold + value && changed == 1 && service.LastError.Contains(protectedOverflow ? "待领取" : "自动出售"), "world loot: full-bag pickup protects valuable drops or sells ordinary ones exactly once");
            Check(!service.Profile.inventory.Exists(item => item.id == overflow.id) && (!protectedOverflow || service.Profile.pendingLoot.Exists(item => item.id == overflow.id)), "world loot: overflow preserves existing equipment and protected rewards");
            saved = File.ReadAllText(service.SaveFilePath);
            written = File.GetLastWriteTimeUtc(service.SaveFilePath);
            Check(!service.CollectLoot(overflow) && !service.CollectLoot(JsonUtility.FromJson<ItemData>(JsonUtility.ToJson(overflow, true))), "world loot: overflow receipt rejects object and copied-ID duplicates");
            Check(service.Profile.gold == gold + value && changed == 1 && File.ReadAllText(service.SaveFilePath) == saved && File.GetLastWriteTimeUtc(service.SaveFilePath) == written, "world loot: duplicate overflow cannot grant more gold or write saves");
            restored = new ProgressionService(service.SaveDirectory);
            Check(restored.Load() && restored.Profile.inventory.Count == 72 && restored.Profile.gold == gold + value && (!protectedOverflow || restored.Profile.pendingLoot.Exists(item => item.id == overflow.id)), "world loot: full bag and pending reward or ordinary sale persist");
            report.passedStages.Add("world loot: delayed collection, identity deduplication, protected overflow and real JSON persistence");
        }

        private static void ValidateMultipleSaveSlots()
        {
            ProgressionService service = Fresh("multiple-slots", HeroClass.Arcanist);
            service.AddGold(21);
            string legacyPath = service.SaveFilePath, legacyBytes = File.ReadAllText(service.SaveFilePath);
            Check(service.GetSaveSlots().Count == 1 && service.GetSaveSlots()[0].Id == "legacy", "slots: old save discovered without rename");
            int changes = 0;
            service.Changed += () => changes++;
            Check(service.CreateNewSlot(HeroClass.Ranger) && changes == 1, "slots: new independent character commits once");
            string rangerPath = service.SaveFilePath;
            string rangerId = service.GetSaveSlots().Find(slot => slot.IsCurrent).Id;
            Guid guid;
            Check(Guid.TryParseExact(rangerId, "N", out guid) && File.Exists(rangerPath + ".bak") && File.ReadAllText(legacyPath) == legacyBytes, "slots: GuidN primary and own backup preserve legacy file");
            service.AddGold(35);
            string rangerBytes = File.ReadAllText(rangerPath);
            Check(service.CreateNewSlot(HeroClass.Summoner), "slots: fourth class can create another independent slot");
            string summonerPath = service.SaveFilePath;
            string summonerId = service.GetSaveSlots().Find(slot => slot.IsCurrent).Id;
            Check(service.GetSaveSlots().Count == 3 && service.Profile.heroClass == HeroClass.Summoner && File.ReadAllText(rangerPath) == rangerBytes && File.ReadAllText(legacyPath) == legacyBytes, "slots: all three characters remain separate");
            Check(service.LoadSlot(rangerId) && service.Profile.heroClass == HeroClass.Ranger && service.Profile.gold == 95, "slots: selecting named slot restores its exact character");
            string summonerBytes = File.ReadAllText(summonerPath);
            service.AddGold(5);
            Check(File.ReadAllText(summonerPath) == summonerBytes && File.ReadAllText(legacyPath) == legacyBytes && service.Load() && service.Profile.gold == 100, "slots: autosave/reload target only selected slot");
            var legacyReader = new ProgressionService(service.SaveDirectory);
            Check(legacyReader.Load() && legacyReader.Profile.heroClass == HeroClass.Arcanist && legacyReader.Profile.gold == 81, "slots: fresh parameterless Load retains legacy compatibility");
            GameProfile sourceProfile = service.Profile;
            string originalJson = JsonUtility.ToJson(sourceProfile, true);
            rangerBytes = File.ReadAllText(rangerPath);
            changes = 0;
            Check(service.SaveAsNewSlot() && changes == 1 && service.GetSaveSlots().Count == 4, "slots: Save As publishes a fourth durable slot");
            string snapshotPath = service.SaveFilePath;
            string snapshotId = service.GetSaveSlots().Find(slot => slot.IsCurrent).Id;
            Check(!ReferenceEquals(sourceProfile, service.Profile) && JsonUtility.ToJson(service.Profile, true) == originalJson && File.ReadAllText(rangerPath) == rangerBytes, "slots: real JsonUtility snapshot is deep and original stays intact");
            service.Profile.inventory[0].name = "independent snapshot";
            service.AddGold(7);
            Check(sourceProfile.inventory[0].name != service.Profile.inventory[0].name && File.ReadAllText(rangerPath) == rangerBytes, "slots: editing snapshot cannot alter old items or save");
            string[] paths = { legacyPath, rangerPath, summonerPath, snapshotPath };
            DateTime time = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc);
            for (int i=0;i<paths.Length;i++) { File.SetLastWriteTimeUtc(paths[i],time.AddDays(i)); File.SetLastWriteTimeUtc(paths[i]+".bak",time.AddDays(i)); }
            File.WriteAllText(Path.Combine(service.SaveDirectory,"emberfall-save-unknown.json"),"ignore");
            File.WriteAllText(Path.Combine(service.SaveDirectory,"emberfall-save.json.tmp"),"ignore");
            List<SaveSlotInfo> slots = service.GetSaveSlots();
            Check(slots.Count == 4 && slots[0].Id == snapshotId && slots[1].Id == summonerId && slots[2].Id == rangerId && slots[3].Id == "legacy", "slots: newest first and unknown/temp files excluded");
            Check(slots.TrueForAll(slot => slot.CanLoad && !slot.RecoveredFromBackup && slot.SavedAtUtc.Kind == DateTimeKind.Utc) && slots.FindAll(slot => slot.IsCurrent).Count == 1, "slots: metadata identifies readable/current slots and UTC dates");
            string destination = CaseDirectory("multiple-slots-migrated");
            Directory.CreateDirectory(destination);
            foreach (string path in paths) { File.Copy(path,Path.Combine(destination,Path.GetFileName(path))); File.Copy(path+".bak",Path.Combine(destination,Path.GetFileName(path)+".bak")); }
            var migrated = new ProgressionService(destination);
            Check(migrated.GetSaveSlots().Count == 4 && migrated.LoadSlot(snapshotId) && migrated.Profile.gold == 107 && migrated.Profile.inventory[0].name == "independent snapshot", "slots: all JSON and backups migrate without a manifest");
            Check(migrated.LoadSlot(rangerId) && migrated.Profile.gold == 100 && migrated.LoadSlot("legacy") && migrated.Profile.gold == 81, "slots: migrated original and legacy remain independent");

            string backup = File.ReadAllText(summonerPath+".bak");
            File.WriteAllText(summonerPath,"broken primary");
            Check(service.GetSaveSlots().Find(slot => slot.Id == summonerId).RecoveredFromBackup && service.LoadSlot(summonerId) && service.Profile.heroClass == HeroClass.Summoner && service.LastError.Contains("备份"), "slots: damaged primary falls back only to its own backup");
            service.AddGold(1);
            Check(File.ReadAllText(summonerPath+".bak") == backup, "slots: resave does not replace valid backup with corrupt primary");
            File.Delete(summonerPath);
            Check(service.GetSaveSlots().Find(slot => slot.Id == summonerId).CanLoad && service.LoadSlot(summonerId), "slots: backup-only save is still visible and loadable");
            File.WriteAllText(summonerPath,"bad"); File.WriteAllText(summonerPath+".bak","bad");
            Check(!service.GetSaveSlots().Find(slot => slot.Id == summonerId).CanLoad && service.HasSave && service.LoadSlot("legacy"), "slots: unreadable save remains listed without preventing good selection");
            GameProfile current = service.Profile;
            string currentJson = JsonUtility.ToJson(current,true);
            changes=0;
            foreach (string id in new[] { null,"","../outside","..\\outside",Guid.NewGuid().ToString("D"),summonerId,Guid.NewGuid().ToString("N") })
                Check(!service.LoadSlot(id), "slots: missing/bad/unsafe selection rejected");
            Check(!service.CreateNewSlot((HeroClass)99) && ReferenceEquals(service.Profile,current) && service.SaveFilePath == legacyPath && changes == 0, "slots: failed actions leave current character and path unchanged");
            string preserved = service.SaveDirectory + "-preserved";
            Directory.Move(service.SaveDirectory,preserved);
            File.WriteAllText(service.SaveDirectory,"blocks directory creation");
            try
            {
                Check(!service.SaveAsNewSlot() && !service.CreateNewSlot(HeroClass.Vanguard), "slots: real filesystem write failure rejects new files");
                Check(ReferenceEquals(service.Profile,current) && JsonUtility.ToJson(service.Profile,true)==currentJson && service.SaveFilePath==legacyPath && changes==0, "slots: write failure preserves full in-memory state and selected path");
            }
            finally { File.Delete(service.SaveDirectory); Directory.Move(preserved,service.SaveDirectory); }
            Check(File.ReadAllText(legacyPath)==legacyBytes && service.GetSaveSlots().Count==4, "slots: failed new saves leave originals intact without phantom slots");
            report.passedStages.Add("multiple save slots: legacy compatibility, new roles, snapshot isolation, backup recovery, failures and cross-directory migration");
        }

        private static void ValidateHotbarDrag()
        {
            foreach (HeroClass hero in Enum.GetValues(typeof(HeroClass)))
            {
                ProgressionService service = Fresh("hotbar-drag-" + hero, hero);
                string saved = File.ReadAllText(service.SaveFilePath);
                int changes = 0;
                service.Changed += () => changes++;
                Check(!service.MoveHotbarSkill(0, 1) && !service.MoveHotbarSkill(-1, 1) && !service.MoveHotbarSkill(0, 10) && changes == 0 && File.ReadAllText(service.SaveFilePath) == saved, hero + ": unlearned and invalid drags do not save");
                ReachLevel(service, 4);
                Check(service.LearnSkill(0), hero + ": drag fixture learns root through actual progression");
                int[] pages = (int[])service.Profile.equippedSkills.Clone();
                changes = 0;
                Check(service.MoveHotbarSkill(0, 1) && service.Profile.equippedSkills[0] == -1 && service.Profile.equippedSkills[1] == 0 && changes == 1, hero + ": drag onto unlearned preset treats it as empty");
                for (int i = 10; i < pages.Length; i++) Check(service.Profile.equippedSkills[i] == pages[i], hero + ": other pages survive first-page drag");
                Check(service.MoveHotbarSkill(1, 9) && service.Profile.equippedSkills[1] == -1 && service.Profile.equippedSkills[9] == 0, hero + ": empty-slot drag moves learned skill");
                Check(service.LearnSkill(1) && service.AssignSkill(0, 1), hero + ": second learned skill assigned");
                changes = 0;
                Check(service.MoveHotbarSkill(9, 0) && service.Profile.equippedSkills[0] == 0 && service.Profile.equippedSkills[9] == 1 && changes == 1, hero + ": occupied learned slots swap atomically");
                saved = File.ReadAllText(service.SaveFilePath);
                DateTime written = File.GetLastWriteTimeUtc(service.SaveFilePath);
                Check(!service.MoveHotbarSkill(0, 0) && !service.MoveHotbarSkill(1, 2) && changes == 1 && File.ReadAllText(service.SaveFilePath) == saved && File.GetLastWriteTimeUtc(service.SaveFilePath) == written, hero + ": no-op drags do not mutate saved state");
                Check(service.SetHotbarPage(2) && service.AssignSkill(0, 0), hero + ": selected third page with independent learned skill");
                pages = (int[])service.Profile.equippedSkills.Clone();
                Check(service.MoveHotbarSkill(0, 9) && service.Profile.equippedSkills[20] == -1 && service.Profile.equippedSkills[29] == 0, hero + ": selected third page receives move");
                for (int i = 0; i < 20; i++) Check(service.Profile.equippedSkills[i] == pages[i], hero + ": first two pages survive third-page drag");
                var restored = new ProgressionService(service.SaveDirectory);
                Check(restored.Load() && restored.Profile.heroClass == hero && restored.Profile.hotbarPage == 2 && restored.Profile.equippedSkills[0] == 0 && restored.Profile.equippedSkills[9] == 1 && restored.Profile.equippedSkills[20] == -1 && restored.Profile.equippedSkills[29] == 0, hero + ": real JsonUtility retains swapped and moved skills");
                service.Profile.equippedSkills[20] = 3;
                Check(!service.MoveHotbarSkill(0, 8), hero + ": passive source cannot be dragged onto active bar");
            }
            report.passedStages.Add("hotbar drag: four-class learned moves, swaps, selected-page isolation and real JSON persistence");
        }

        private static void ValidateConsumableHotbar()
        {
            foreach (HeroClass hero in Enum.GetValues(typeof(HeroClass)))
            {
                string name = "consumable-hotbar-" + hero;
                ProgressionService service = Fresh(name, hero);
                int changes = 0;
                service.Changed += () => changes++;
                int quantity = service.Profile.potions;
                Check(service.AssignConsumable(8) && service.Profile.equippedSkills[8] == GameBalance.HotbarPotion && changes == 1 && service.Profile.potions == quantity,
                    hero + ": level-one potion assignment has one event and consumes no inventory");
                string saved = File.ReadAllText(service.SaveFilePath);
                DateTime written = File.GetLastWriteTimeUtc(service.SaveFilePath);
                Check(!service.AssignConsumable(-1) && !service.AssignConsumable(10) && !service.AssignConsumable(8) && !service.AssignSkill(9, GameBalance.HotbarPotion),
                    hero + ": invalid/no-op assignment rejected");
                Check(changes == 1 && File.ReadAllText(service.SaveFilePath) == saved && File.GetLastWriteTimeUtc(service.SaveFilePath) == written,
                    hero + ": rejected assignment does not change save or event count");
                Check(service.MoveHotbarSkill(8, 0) && service.Profile.equippedSkills[0] == GameBalance.HotbarPotion && service.Profile.equippedSkills[8] == -1,
                    hero + ": unlearned target behaves as empty for consumable drag");
                Check(service.AssignConsumable(9) && service.Profile.equippedSkills[0] == -1,
                    hero + ": assigning same consumable elsewhere moves existing shortcut");
                ReachLevel(service, 2);
                Check(service.LearnSkill(0) && service.AssignSkill(0, 0), hero + ": learns and assigns real skill");
                changes = 0;
                Check(service.MoveHotbarSkill(9, 0) && service.Profile.equippedSkills[0] == GameBalance.HotbarPotion && service.Profile.equippedSkills[9] == 0 && changes == 1,
                    hero + ": skill-potion drag swap commits exactly once");
                Check(service.AssignConsumable(9) && service.Profile.equippedSkills[0] == 0 && service.AssignSkill(9, 0) && service.Profile.equippedSkills[0] == GameBalance.HotbarPotion,
                    hero + ": assignment APIs preserve displaced learned skill and consumable");
                while (service.Profile.potions > 0) Check(service.UsePotion(), hero + ": consumes actual supply");
                Check(service.Profile.equippedSkills[0] == GameBalance.HotbarPotion && !service.UsePotion() && service.MoveHotbarSkill(0, 8) && service.AssignConsumable(0),
                    hero + ": exhausted supply retains movable and assignable shortcut");
                Check(service.SetHotbarPage(1) && service.AssignConsumable(4) && service.SetHotbarPage(2) && service.AssignConsumable(6),
                    hero + ": consumables assigned on all pages");
                var restored = new ProgressionService(service.SaveDirectory);
                Check(restored.Load() && restored.Profile.potions == 0 && restored.Profile.hotbarPage == 2 && restored.Profile.equippedSkills[0] == GameBalance.HotbarPotion && restored.Profile.equippedSkills[14] == GameBalance.HotbarPotion && restored.Profile.equippedSkills[26] == GameBalance.HotbarPotion,
                    hero + ": real JsonUtility retains all zero-quantity shortcuts");
                string originalPath = service.SaveFilePath;
                saved = File.ReadAllText(originalPath);
                Check(service.SaveAsNewSlot() && service.Profile.equippedSkills[26] == GameBalance.HotbarPotion && service.AssignSkill(6, -1) && service.Profile.equippedSkills[26] == -1,
                    hero + ": saved snapshot preserves potion entry and allows independent removal");
                Check(File.ReadAllText(originalPath) == saved && restored.Load() && restored.Profile.equippedSkills[26] == GameBalance.HotbarPotion && service.Profile.equippedSkills[0] == GameBalance.HotbarPotion,
                    hero + ": removal changes neither source save nor other pages");
                Check(service.BuyPotion() && service.Profile.potions == 1 && service.Profile.equippedSkills[0] == GameBalance.HotbarPotion,
                    hero + ": supply refill retains old shortcut");
            }
            const string repairName = "consumable-hotbar-repair";
            Fresh(repairName);
            var damaged = new GameProfile { potions = 0, equippedSkills = new[] { -2, -2, -3, 99, 3, 0, -1, -1, -1, -1, -2, -2 } };
            WriteEnvelope(repairName, JsonUtility.ToJson(damaged));
            var repaired = new ProgressionService(CaseDirectory(repairName));
            Check(repaired.Load() && repaired.Profile.equippedSkills[0] == GameBalance.HotbarPotion && repaired.Profile.equippedSkills[1] == -1 && repaired.Profile.equippedSkills[2] == -1 && repaired.Profile.equippedSkills[3] == -1 && repaired.Profile.equippedSkills[4] == -1 && repaired.Profile.equippedSkills[5] == 0,
                "consumable repair: first valid shortcut retained, duplicates/passives/unknown IDs removed");
            Check(repaired.Profile.equippedSkills[10] == GameBalance.HotbarPotion && repaired.Profile.equippedSkills[11] == -1 && repaired.Profile.potions == 0,
                "consumable repair: independent page shortcut retained at quantity zero");
            report.passedStages.Add("consumable hotbar: assignment, mixed swaps, zero supply, page isolation, snapshot, JSON persistence and repair");
        }

        private static void ValidateBuildPresets()
        {
            foreach (HeroClass hero in Enum.GetValues(typeof(HeroClass)))
            {
                string name = "build-presets-" + hero;
                ProgressionService service = Fresh(name, hero);
                Check(service.Profile.buildPresets.Length == 2 && !service.HasBuildPreset(0) && !service.HasBuildPreset(1),
                    name + ": empty preset slots survive real JsonUtility defaults");
                service.Profile.level = 100;
                for (int skill = 0; skill < GameBalance.SkillCount; skill++) service.Profile.skillRanks[skill] = 3;
                service.Profile.masteryRanks = new[] { 20, 0, 0, 10 }; service.Profile.masteryCore = 0;
                service.Profile.specialization = hero == HeroClass.Arcanist ? ElementalistSpecialization.Burn : ElementalistSpecialization.None;
                service.Save();
                Check(service.SaveBuildPreset(0, true), name + ": first preset durably recorded");
                string preset = JsonUtility.ToJson(service.Profile.buildPresets[0]);
                Check(service.ResetBuild(true) && service.Profile.skillPoints == 89 && service.Profile.masteryCore == -1,
                    name + ": joint reset refunds skills and mastery while retaining ten first ranks");
                service.Profile.masteryRanks[1] = 10; service.Profile.masteryCore = 1; service.Save();
                Check(service.SaveBuildPreset(1, true) && service.Load(), name + ": distinct second preset and both real JSON slots reload");
                Check(service.HasBuildPreset(0) && service.HasBuildPreset(1) && JsonUtility.ToJson(service.Profile.buildPresets[0]) == preset,
                    name + ": nested arrays and saved item references remain exact after JSON roundtrip");
                Check(service.ApplyBuildPreset(0, true) && service.Profile.skillPoints == 39 && service.Profile.masteryCore == 0,
                    name + ": first build reapplies its original shared-point budget");
                for (int skill = 0; skill < GameBalance.SkillCount; skill++)
                    Check(service.Profile.skillRanks[skill] == 3, name + ": restored rank remains exact for skill " + skill);
                Check(service.ApplyBuildPreset(1, true) && service.Profile.skillPoints == 79 && service.Profile.masteryCore == 1,
                    name + ": second build remains independent");
                GameProfile before = service.Profile;
                string state = JsonUtility.ToJson(before), primary = File.ReadAllText(service.SaveFilePath), backup = File.ReadAllText(service.SaveFilePath + ".bak");
                string temporary = service.SaveFilePath + ".tmp";
                Directory.CreateDirectory(temporary);
                try
                {
                    Check(!service.ApplyBuildPreset(0, true) && ReferenceEquals(before, service.Profile) && JsonUtility.ToJson(service.Profile) == state,
                        name + ": failed real-JSON apply retains complete live identity and state");
                    Check(File.ReadAllText(service.SaveFilePath) == primary && File.ReadAllText(service.SaveFilePath + ".bak") == backup,
                        name + ": failed apply preserves both real JSON documents");
                }
                finally { Directory.Delete(temporary); }
                Check(service.ApplyBuildPreset(0, true) && service.Load() && service.Profile.skillPoints == 39,
                    name + ": failed apply retries and reloads exactly once");
            }
            report.passedStages.Add("two build presets: four-class real JSON nested-array roundtrip, joint respec, independent application and failed-write retry");
        }

        private static ProgressionService Fresh(string name, HeroClass hero = HeroClass.Vanguard)
        {
            var service = new ProgressionService(CaseDirectory(name));
            Check(!service.HasSave, name + ": injected directory initially has no save");
            service.NewGame(hero);
            Check(string.IsNullOrEmpty(service.LastError) && service.HasSave, name + ": real JsonUtility writes new character");
            return service;
        }

        private static void ValidateClass(HeroClass hero)
        {
            string name = "class-" + hero;
            ProgressionService service = Fresh(name, hero);
            Check(service.Profile.inventory.Count == 3 && service.Equipped(ItemSlot.Weapon) != null, name + ": starter equipment exists");
            Check(!service.LearnSkill(0), name + ": level-one skill learning is gated");
            service.GrantExperience(int.MaxValue);
            Check(service.Profile.level == 100 && service.Profile.skillPoints == 99, name + ": bulk XP reaches level cap safely");
            StatBlock baseline = service.GetStats();
            int changed = 0;
            service.Changed += () => changed++;
            for (int skill = 0; skill < GameBalance.SkillCount; skill++)
            {
                Check(!string.IsNullOrWhiteSpace(GameBalance.SkillName(hero, skill)), name + ": skill catalog entry " + skill);
                for (int rank = 1; rank <= 3; rank++)
                    Check(service.LearnSkill(skill) && service.Profile.skillRanks[skill] == rank, name + ": learn skill " + skill + " rank " + rank);
                Check(!service.LearnSkill(skill), name + ": fourth rank rejected for skill " + skill);
            }
            Check(changed == 30 && service.Profile.skillPoints == 69, name + ": every rank emits an update and consumes exactly one point");
            Check(!service.AssignSkill(8, 3) && !service.AssignSkill(8, 8), name + ": learned passives cannot enter active bar");
            StatBlock stats = service.GetStats();
            if (hero == HeroClass.Vanguard)
                Check(Near(stats.Damage, baseline.Damage * 1.22f) && Near(stats.Armor, baseline.Armor + 7), name + ": rank-three passive includes equipment damage and armor");
            else if (hero == HeroClass.Arcanist)
                Check(Near(stats.Damage, baseline.Damage * 1.18f) && Near(stats.MaxHealth, baseline.MaxHealth * 1.1f), name + ": rank-three passive includes equipped damage and health");
            else if (hero == HeroClass.Summoner)
                Check(Near(stats.Damage, baseline.Damage * 1.18f) && Near(stats.MaxHealth, baseline.MaxHealth) && Near(stats.CritChance, baseline.CritChance), name + ": summoner passive improves damage without ranger or mage bonuses");
            else
                Check(Near(stats.CritChance, baseline.CritChance + .12f) && Near(stats.MoveSpeed, baseline.MoveSpeed * 1.1f), name + ": rank-three passive improves critical chance and movement");
            Check(service.AssignSkill(0, 9) && service.Profile.equippedSkills[7] == 0, name + ": active assignment swaps same-page skill");
            Check(service.SetHotbarPage(1) && service.AssignSkill(0, 9) && service.AssignSkill(9, 9), name + ": second page accepts and swaps same skill independently");
            Check(service.Profile.equippedSkills[0] == 9 && service.Profile.equippedSkills[10] == -1 && service.Profile.equippedSkills[19] == 9, name + ": page swap preserves first page");
            Check(service.SetHotbarPage(2) && service.AssignSkill(0, 4) && service.AssignSkill(9, 7) && service.AssignSkill(9, -1), name + ": third page assign and clear");
            Check(!service.SetHotbarPage(3) && service.Profile.hotbarPage == 2, name + ": invalid page is rejected");
            Check(service.SetHotbarKey(0, 101) && service.SetHotbarKey(1, 101), name + ": keyboard remapping supports duplicate swap");
            Check(service.Profile.hotbarKeys[0] == 120 && service.Profile.hotbarKeys[1] == 101 && !service.SetHotbarKey(0, 97), name + ": keyboard swap preserves uniqueness and reserves movement");
            string weaponId = service.Profile.weaponId;
            service.Save();
            Check(string.IsNullOrEmpty(service.LastError) && File.Exists(SavePath(name) + ".bak"), name + ": flushed primary and backup exist");
            string json = File.ReadAllText(SavePath(name));
            Check(json.Contains("emberfall-character") && json.Contains("skillRanks"), name + ": private serializable save envelope produces valid document");
            var restored = new ProgressionService(CaseDirectory(name));
            Check(restored.Load(), name + ": private save envelope deserializes with real JsonUtility");
            Check(restored.Profile.heroClass == hero && restored.Profile.level == 100 && restored.Profile.skillPoints == 69 && restored.Profile.weaponId == weaponId, name + ": identity, points and gear persist");
            for (int skill = 0; skill < 10; skill++) Check(restored.Profile.skillRanks[skill] == 3, name + ": saved rank " + skill);
            Check(restored.Profile.hotbarPage == 2 && restored.Profile.equippedSkills.Length == 30 && restored.Profile.equippedSkills[20] == 4 && restored.Profile.equippedSkills[19] == 9 && restored.Profile.equippedSkills[29] == -1, name + ": all hotbar pages persist");
            Check(restored.Profile.hotbarKeys[0] == 120 && restored.Profile.hotbarKeys[1] == 101, name + ": custom keys persist");
            Check(SameStats(stats, restored.GetStats()), name + ": passive stats survive save round trip");
            report.passedStages.Add(name + ": ten skills, ranks, passives, pages, keys, real JSON round trip");
        }

        private static void ValidateLegacyJson()
        {
            const string name = "legacy-json";
            ProgressionService seed = Fresh(name, HeroClass.Arcanist);
            var legacy = new LegacyProfile
            {
                inventory = seed.Profile.inventory,
                weaponId = seed.Profile.weaponId,
                armorId = seed.Profile.armorId,
                relicId = seed.Profile.relicId
            };
            string payload = JsonUtility.ToJson(legacy);
            WriteEnvelope(name, payload);
            var migrated = new ProgressionService(CaseDirectory(name));
            Check(migrated.Load(), "legacy: original three-rank JSON loads without new fields");
            Check(migrated.Profile.level == 12 && migrated.Profile.xp == 37 && migrated.Profile.gold == 321 && migrated.Profile.potions == 7 && migrated.Profile.kills == 45 && migrated.Profile.clearedRuns == 3, "legacy: other character progress remains intact");
            Check(migrated.Profile.inventory.Count == 3 && migrated.Profile.weaponId == legacy.weaponId && migrated.Profile.armorId == legacy.armorId && migrated.Profile.relicId == legacy.relicId, "legacy: all equipment IDs survive");
            Check(migrated.Profile.skillRanks.Length == 10 && migrated.Profile.skillRanks[0] == 2 && migrated.Profile.skillRanks[1] == 2 && migrated.Profile.skillRanks[2] == 1, "legacy: ranks pad to ten and newly gated rank is repaired");
            Check(migrated.Profile.skillPoints == 6 && migrated.LastError.Contains("返还"), "legacy: locked rank refunded with player-facing information");
            Check(migrated.Profile.skillPoints + migrated.Profile.skillRanks[0] + migrated.Profile.skillRanks[1] + migrated.Profile.skillRanks[2] == 11, "legacy: all earned skill points conserved");
            Check(migrated.Profile.equippedSkills.Length == 30 && migrated.Profile.equippedSkills[0] == 0 && migrated.Profile.equippedSkills[3] == 4 && migrated.Profile.equippedSkills[29] == -1, "legacy: missing loadout initializes active-only defaults");
            Check(migrated.Profile.hotbarKeys.Length == 10 && migrated.Profile.hotbarKeys[0] == 122, "legacy: missing key bindings repaired");
            string threeSlotPayload = payload.Substring(0, payload.Length - 1) + ",\"equippedSkills\":[2,0,1]}";
            WriteEnvelope(name, threeSlotPayload);
            Check(migrated.Load() && migrated.Profile.equippedSkills[0] == 2 && migrated.Profile.equippedSkills[1] == 0 && migrated.Profile.equippedSkills[2] == 1 && migrated.Profile.equippedSkills[3] == 4, "legacy: existing three-slot order is migrated without resetting it");
            migrated.Save();
            var reloaded = new ProgressionService(CaseDirectory(name));
            Check(reloaded.Load() && reloaded.Profile.skillPoints == 6 && string.IsNullOrEmpty(reloaded.LastError), "legacy: migration persists and does not refund a second time");
            report.passedStages.Add("legacy JSON migration and conserved rank refunds");
        }

        private static void ValidateSkillTreeGates(HeroClass hero)
        {
            int[] expectedLevels = { 2, 4, 6, 4, 10, 6, 13, 20, 13, 30 };
            for (int skill = 0; skill < GameBalance.SkillCount; skill++)
            {
                string name = "tree-gates-" + hero + "-" + skill;
                var service = Fresh(name, hero);
                Check(GameBalance.SkillRequiredLevels[skill] == expectedLevels[skill], name + ": first-rank tree level matches design");
                int prerequisiteRanks = 0;
                for (int rank = 1; rank <= 3; rank++)
                {
                    int level = expectedLevels[skill] + (rank == 1 ? 0 : rank == 2 ? 8 : 18);
                    ReachLevel(service, level - 1);
                    if (rank == 1)
                    {
                        LearnPrerequisites(service, skill);
                        prerequisiteRanks = SpentPoints(service);
                    }
                    int points = service.Profile.skillPoints;
                    Check(!service.LearnSkill(skill) && service.Profile.skillRanks[skill] == rank - 1 && service.Profile.skillPoints == points, name + ": rank " + rank + " is rejected one level early without spending");
                    Check(service.SkillLockReason(skill).Contains(level.ToString()), name + ": lock explains exact next-rank level");
                    ReachLevel(service, level);
                    points = service.Profile.skillPoints;
                    Check(service.LearnSkill(skill) && service.Profile.skillRanks[skill] == rank && service.Profile.skillPoints == points - 1, name + ": rank learned at its exact level through real prerequisites");
                }
                Check(!service.LearnSkill(skill) && SpentPoints(service) == prerequisiteRanks + 3 && service.Profile.skillPoints + SpentPoints(service) == service.Profile.level - 1, name + ": rank cap and lifetime point budget survive branch learning");
                var restored = new ProgressionService(CaseDirectory(name));
                Check(restored.Load() && restored.Profile.skillRanks[skill] == 3 && SpentPoints(restored) == prerequisiteRanks + 3, name + ": evolved skill and all prerequisite ranks persist");
            }
            report.passedStages.Add(hero + ": exact level boundaries for all thirty ranks with recursively learned prerequisites");
        }

        private static void ValidateSkillTreeBranches(HeroClass hero)
        {
            var locked = Fresh("tree-locked-" + hero, hero);
            ReachLevel(locked, 100);
            int changes = 0;
            locked.Changed += () => changes++;
            for (int skill = 1; skill < GameBalance.SkillCount; skill++)
                Check(!locked.PrerequisitesMet(skill) && !locked.LearnSkill(skill) && locked.SkillLockReason(skill).Contains("前置") && locked.Profile.skillRanks[skill] == 0 && locked.Profile.skillPoints == 99 && changes == 0, hero + ": high level cannot bypass unlearned predecessor for skill " + skill);
            Check(locked.PrerequisitesMet(0) && !locked.PrerequisitesMet(-1) && !locked.PrerequisitesMet(GameBalance.SkillCount), hero + ": root and invalid prerequisite queries are safe");
            int[][] pairs = { new[] { 1, 3, 4 }, new[] { 2, 5, 6 }, new[] { 6, 8, 13 } };
            foreach (int[] pair in pairs)
            {
                var branches = Fresh("tree-pair-" + hero + "-" + pair[2], hero);
                ReachLevel(branches, pair[2]);
                LearnPrerequisites(branches, pair[0]);
                LearnPrerequisites(branches, pair[1]);
                int points = branches.Profile.skillPoints;
                Check(branches.LearnSkill(pair[0]) && branches.Profile.skillRanks[pair[1]] == 0, hero + ": learning one branch leaves same-level sibling unlearned");
                Check(branches.LearnSkill(pair[1]) && branches.Profile.level == pair[2] && branches.Profile.skillPoints == points - 2, hero + ": both same-level nodes accept their own skill point");
                Check(branches.Profile.skillPoints + SpentPoints(branches) == branches.Profile.level - 1, hero + ": same-level branches conserve earned points");
            }
            foreach (int first in new[] { 7, 6 })
            {
                int missing = first == 7 ? 6 : 7;
                var ultimate = Fresh("tree-ultimate-" + hero + "-" + first, hero);
                ReachLevel(ultimate, 30);
                LearnBranch(ultimate, first);
                int points = ultimate.Profile.skillPoints;
                Check(ultimate.Profile.skillRanks[missing] == 0 && !ultimate.PrerequisitesMet(9) && !ultimate.LearnSkill(9) && ultimate.Profile.skillPoints == points, hero + ": ultimate rejects either missing predecessor without spending");
                Check(ultimate.SkillLockReason(9).Contains(GameBalance.SkillName(hero, missing)), hero + ": ultimate lock names its missing branch");
                LearnBranch(ultimate, missing);
                Check(ultimate.PrerequisitesMet(9) && ultimate.LearnSkill(9) && ultimate.Profile.skillRanks[9] == 1, hero + ": ultimate unlocks with both real branches learned");
                Check(ultimate.Profile.skillRanks[8] == 0 && ultimate.Profile.skillPoints + SpentPoints(ultimate) == 29, hero + ": ultimate leaves optional passive unlearned and conserves points");
            }
            report.passedStages.Add(hero + ": high-level prerequisite rejection, same-level siblings and both ultimate predecessor directions");
        }

        private static void ValidatePreviouslyLearnedTreeSkill(HeroClass hero)
        {
            string name = "legacy-tree-" + hero;
            var seed = Fresh(name, hero);
            ReachLevel(seed, 50);
            // This is an old-save fixture from before prerequisite gates existed.
            // Ordinary learning fixtures always use LearnBranch instead.
            var legacy = JsonUtility.FromJson<GameProfile>(JsonUtility.ToJson(seed.Profile));
            legacy.skillRanks = new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 };
            legacy.skillPoints = 48;
            WriteEnvelope(name, JsonUtility.ToJson(legacy));
            var restored = new ProgressionService(CaseDirectory(name));
            Check(restored.Load() && restored.Profile.skillRanks[9] == 1 && restored.Profile.skillPoints == 48 && !restored.PrerequisitesMet(9), name + ": previously learned ultimate survives without auto-learning parents");
            Check(restored.Profile.heroClass == hero && restored.Profile.weaponId == seed.Profile.weaponId && restored.AssignSkill(0, 9), name + ": class equipment and active assignment survive");
            Check(restored.LearnSkill(9) && restored.Profile.skillRanks[9] == 2 && restored.Profile.skillPoints == 47, name + ": grandfathered learned skill evolves at its real rank level");
            Check(!restored.LearnSkill(7) && restored.Profile.skillRanks[7] == 0, name + ": grandfathering does not bypass an unlearned branch");
            restored.Save();
            var roundTrip = new ProgressionService(CaseDirectory(name));
            Check(roundTrip.Load() && roundTrip.Profile.skillRanks[9] == 2 && roundTrip.Profile.skillPoints == 47 && roundTrip.Profile.equippedSkills[0] == 9 && SpentPoints(roundTrip) == 2, name + ": learned skill and point balance survive another real JsonUtility round trip");
            report.passedStages.Add(name + ": learned legacy skill retained and evolved without retroactive prerequisite grants");
        }

        private static void ValidateDamagedJson()
        {
            const string name = "repair-json";
            Fresh(name);
            var damaged = new GameProfile
            {
                level = 40,
                skillRanks = null,
                equippedSkills = new[] { 3, 0, 0, 99, -99, 8, 4, 5 },
                hotbarKeys = new[] { 120, 120, 97, 999, 101 },
                hotbarPage = 50
            };
            WriteEnvelope(name, JsonUtility.ToJson(damaged));
            var restored = new ProgressionService(CaseDirectory(name));
            Check(restored.Load() && restored.Profile.skillRanks.Length == 10 && restored.Profile.skillPoints == 39, "repair: actual JSON null rank array restores earned points");
            Check(restored.Profile.hotbarPage == 0 && restored.Profile.equippedSkills.Length == 30, "repair: invalid page and short loadout repaired");
            Check(restored.Profile.equippedSkills[0] == -1 && restored.Profile.equippedSkills[1] == 0 && restored.Profile.equippedSkills[2] == -1 && restored.Profile.equippedSkills[5] == -1 && restored.Profile.equippedSkills[6] == 4, "repair: passive, duplicate and invalid assignments cleared individually");
            var keys = new HashSet<int>();
            Check(restored.Profile.hotbarKeys.Length == 10, "repair: ten keyboard slots restored");
            foreach (int key in restored.Profile.hotbarKeys) Check(GameBalance.IsBindableKey(key) && keys.Add(key), "repair: restored keyboard key is bindable and unique");
            Check(restored.Profile.hotbarKeys[0] == 120 && restored.Profile.hotbarKeys[4] == 101, "repair: valid user keyboard choices retained");
            report.passedStages.Add("real JSON null, malformed array, loadout and keyboard repair");
        }

        private static void ValidateBackupRecovery()
        {
            const string name = "backup-json";
            ProgressionService service = Fresh(name);
            service.AddGold(20);
            File.WriteAllText(SavePath(name), "{ truncated document");
            var restored = new ProgressionService(CaseDirectory(name));
            Check(restored.Load() && restored.Profile.gold == 60 && restored.LastError.Contains("备份"), "backup: Unity JSON parse failure recovers previous valid copy");
            restored.AddGold(7);
            var repaired = new ProgressionService(CaseDirectory(name));
            Check(repaired.Load() && repaired.Profile.gold == 67, "backup: recovered profile safely replaces damaged primary");
            File.WriteAllText(SavePath(name), "again corrupt");
            Check(repaired.Load() && repaired.Profile.gold == 60, "backup: prior recovery did not replace valid backup with corrupt primary");
            report.passedStages.Add("real JSON corruption, atomic save and preserved backup recovery");
        }

        private static void ValidateSkillRuntime()
        {
            const string name = "runtime";
            ProgressionService service = Fresh(name);
            service.GrantExperience(int.MaxValue);
            LearnPrerequisites(service, 9);
            for (int rank = 1; rank <= 3; rank++) Check(service.LearnSkill(9), "runtime: ultimate learned through production progression");
            Check(service.SetHotbarPage(0) && service.AssignSkill(0, 9), "runtime: ultimate mapped on first page");
            var runtime = new SkillRuntime(service.Profile.heroClass);
            float ultimateCooldown = GameBalance.EffectiveCooldown(service.Profile.heroClass, 9, 3);
            float remainingEnergy = 100 - GameBalance.SkillEnergyCost(service.Profile.heroClass, 9);
            Check(runtime.TryConsume(9, service.Profile.skillRanks[9]) && Near(runtime.Energy, remainingEnergy) && Near(runtime.Remaining(9), ultimateCooldown), "runtime: rank-three ultimate spends class-specific energy and starts its independent cooldown");
            Check(service.SetHotbarPage(1) && service.AssignSkill(5, 9) && service.SetHotbarKey(5, 101), "runtime: same skill can be mapped and rebound on second page");
            int mapped = service.Profile.equippedSkills[service.Profile.hotbarPage * 10 + 5];
            Check(!runtime.TryConsume(mapped, service.Profile.skillRanks[mapped]) && Near(runtime.Remaining(9), ultimateCooldown), "runtime: changing pages and bindings cannot bypass cooldown");
            Check(!runtime.TryConsume(7, 1) && Near(runtime.Remaining(7), 0) && Near(runtime.Energy, remainingEnergy), "runtime: insufficient energy changes no resources");
            remainingEnergy -= GameBalance.SkillEnergyCost(service.Profile.heroClass, 0);
            Check(runtime.TryConsume(0, 1) && Near(runtime.Energy, remainingEnergy), "runtime: a low-cost skill remains usable after ultimate");
            runtime.Advance(5);
            Check(Near(runtime.Remaining(0), 0) && Near(runtime.Remaining(9), ultimateCooldown - 5) && Near(runtime.Energy, remainingEnergy + 20), "runtime: energy regeneration and cooldown passage are independent");
            runtime.FillEnergy();
            Check(Near(runtime.Energy, 100) && Near(runtime.Remaining(9), ultimateCooldown - 5), "runtime: resource refill cannot reset cooldown");
            Check(!runtime.TryConsume(3, 3) && !runtime.TryConsume(8, 3), "runtime: both learned passive IDs cannot cast");
            Check(!runtime.TryConsume(-1, 1) && !runtime.TryConsume(10, 1) && !runtime.TryConsume(0, 0) && !runtime.TryConsume(0, 4), "runtime: empty slots and invalid ranks safely rejected");
            float beforeCooldown = runtime.Remaining(9);
            runtime.Advance(float.NaN);
            runtime.Advance(float.PositiveInfinity);
            runtime.Advance(-1);
            runtime.RestoreEnergy(float.NaN);
            Check(Near(runtime.Energy, 100) && Near(runtime.Remaining(9), beforeCooldown), "runtime: non-finite and negative time cannot mutate state");
            runtime.Advance(1000);
            Check(runtime.TryConsume(9, 3), "runtime: ultimate becomes usable after real cooldown expires");
            report.passedStages.Add("skill runtime energy, passive rejection and cooldown integrity across pages and bindings");
        }

        private static void ValidatePortableSaveTransfer()
        {
            const string sourceName = "portable-source";
            ProgressionService source = Fresh(sourceName, HeroClass.Ranger);
            int experience = 23;
            for (int level = 1; level < 50; level++) experience += GameBalance.XpToNext(level);
            source.GrantExperience(experience);
            source.AddGold(987);
            for (int skill = 0; skill < GameBalance.SkillCount; skill++)
                for (int rank = 1; rank <= 3; rank++) Check(source.LearnSkill(skill), "portable: source learns skill " + skill + " rank " + rank);
            ItemData item = source.CreateLoot(1, true);
            Check(source.Equip(item.id) && source.Upgrade(item.id), "portable: source equips upgraded loot");
            source.CreateLoot(40, true);
            source.UsePotion();
            source.Profile.kills = 123;
            source.Profile.clearedRuns = 5;
            source.Profile.bestFloor = 5;
            Check(source.SetHotbarPage(2) && source.AssignSkill(0, 9) && source.AssignSkill(9, 7), "portable: source configures independent skill page");
            Check(source.SetHotbarKey(0, 101) && source.SetHotbarKey(9, 282), "portable: source configures custom keyboard mapping");
            source.Save();
            Check(source.SaveDirectory == CaseDirectory(sourceName) && source.SaveFilePath == SavePath(sourceName), "portable: exposed save paths point to injected test directory");
            string sourceJson = File.ReadAllText(source.SaveFilePath);
            string expectedProfile = JsonUtility.ToJson(source.Profile, true);
            string unescapedJson = sourceJson.Replace("\\\\", "\\").Replace("\\/", "/");
            Check(unescapedJson.IndexOf(source.SaveDirectory, StringComparison.OrdinalIgnoreCase) < 0 && unescapedJson.IndexOf("\"saveDirectory\"", StringComparison.OrdinalIgnoreCase) < 0 && unescapedJson.IndexOf("\"machineId\"", StringComparison.OrdinalIgnoreCase) < 0, "portable: player JSON carries no absolute source path or machine identity");
            string destination = CaseDirectory("portable-destination");
            Directory.CreateDirectory(destination);
            File.Copy(source.SaveFilePath, Path.Combine(destination, "emberfall-save.json"));
            Check(Directory.GetFiles(destination).Length == 1, "portable: only the primary JSON is copied to new installation");
            var migrated = new ProgressionService(destination);
            Check(migrated.HasSave && migrated.Load(), "portable: fresh service loads copied JSON without source backup or registry state");
            Check(migrated.Profile.heroClass == HeroClass.Ranger && migrated.Profile.level == 50 && migrated.Profile.xp == 23 && migrated.Profile.skillPoints == 19, "portable: copied class, level, XP and point balance survive");
            Check(migrated.Profile.inventory.Count == source.Profile.inventory.Count && migrated.Profile.weaponId == source.Profile.weaponId && migrated.Profile.armorId == source.Profile.armorId && migrated.Profile.relicId == source.Profile.relicId, "portable: copied inventory and equipped item identities survive");
            Check(migrated.Profile.hotbarPage == 2 && migrated.Profile.equippedSkills[20] == 9 && migrated.Profile.equippedSkills[29] == 7 && migrated.Profile.hotbarKeys[0] == 101 && migrated.Profile.hotbarKeys[9] == 282, "portable: skill pages and custom key bindings survive");
            Check(JsonUtility.ToJson(migrated.Profile, true) == expectedProfile, "portable: all profile fields exactly match source after real JsonUtility load");
            migrated.AddGold(1);
            Check(File.ReadAllText(source.SaveFilePath) == sourceJson, "portable: destination writes leave source save untouched");
            string emptyDirectory = CaseDirectory("portable-empty-installation");
            var empty = new ProgressionService(emptyDirectory);
            Check(!empty.HasSave && !Directory.Exists(emptyDirectory), "portable: third installation begins without save or copied state");
            empty.NewGame(HeroClass.Arcanist);
            Check(empty.HasSave && empty.Profile.heroClass == HeroClass.Arcanist && empty.Profile.level == 1 && empty.Profile.skillPoints == 0 && File.Exists(empty.SaveFilePath), "portable: third installation creates independent new character");
            Check(File.ReadAllText(source.SaveFilePath) == sourceJson && migrated.Profile.heroClass == HeroClass.Ranger, "portable: new independent character does not overwrite other installations");
            report.passedStages.Add("portable primary-JSON-only transfer, complete profile equality and independent clean installation");
        }

            private static void ValidateUpgradeTransfers()
        {
            foreach (ItemSlot slot in new[] { ItemSlot.Weapon, ItemSlot.Armor, ItemSlot.Relic })
            {
                string name = "slot-upgrade-" + slot;
                var service = Fresh(name); ReachLevel(service, 50); service.AddGold(1000000);
                ItemData source = service.Equipped(slot), target = TransferItem(service, slot);
                UpgradeTo(service, source, 5);
                ItemData sourceFive = service.PreviewEquippedItem(source), targetFive = service.PreviewEquippedItem(target);
                string before = JsonUtility.ToJson(service.Profile, true), saved = File.ReadAllText(service.SaveFilePath);
                int events = 0; service.Changed += () => events++;
                for (int rank = 0; rank <= 10; rank++)
                    Check(service.PreviewUpgrade(target, rank).upgradeLevel == rank, name + ": preview supports every valid rank");
                Check(!service.TransferUpgrade(source.id, target.id) && service.LastError.Contains("自动继承"), name + ": obsolete manual transfer reports automatic inheritance");
                Check(before == JsonUtility.ToJson(service.Profile, true) && saved == File.ReadAllText(service.SaveFilePath) && events == 0, name + ": previews and obsolete transfer mutate nothing");
                int gold = service.Profile.gold;
                for (int cycle = 0; cycle < 6; cycle++)
                    Check(service.Equip(target.id) && SameEquipment(target, targetFive) && source.upgradeLevel == 0 && service.Equip(source.id) && SameEquipment(source, sourceFive) && target.upgradeLevel == 0 && service.Profile.gold == gold, name + ": automatic replacement preserves individual baseline without compounding");
                ItemData next = service.PreviewUpgrade(source, 6);
                int cost = service.UpgradeCost(target);
                Check(service.Upgrade(target.id) && SameEquipment(source, next) && service.SlotUpgradeRank(slot) == 6 && target.upgradeLevel == 0 && service.Profile.gold == gold - cost, name + ": bag item trains same slot and updates worn item");
                var restored = new ProgressionService(CaseDirectory(name));
                Check(restored.Load() && restored.SlotUpgradeRank(slot) == 6 && restored.Equip(target.id), name + ": slot rank survives real JSON and applies on equip");
                ItemData equipped = restored.Equipped(slot);
                UpgradeTo(restored, equipped, 10);
                Check(!restored.Upgrade(source.id) && restored.UpgradeCost(restored.Profile.inventory.Find(item => item.id == source.id)) == 0, name + ": fresh bag item cannot bypass slot cap");
                report.passedStages.Add(name + ": slot-bound training, automatic replacement, preview, repeat stability and real JSON persistence");
            }
        }

        private static void ValidateLegacyEquipment()
        {
            foreach (bool unusual in new[] { false, true })
            {
                string name = unusual ? "equipment-legacy-caps" : "equipment-legacy-normal";
                var seed = Fresh(name);
                ReachLevel(seed, 50);
                seed.AddGold(1000000);
                var sources = new List<string>();
                var targets = new List<string>();
                foreach (ItemSlot slot in new[] { ItemSlot.Weapon, ItemSlot.Armor, ItemSlot.Relic })
                {
                    ItemData source = seed.Equipped(slot);
                    UpgradeTo(seed, source, 5);
                    sources.Add(source.id);
                    targets.Add(TransferItem(seed, slot).id);
                }
                var legacy = new LegacyEquipmentProfile { heroClass = seed.Profile.heroClass, level = seed.Profile.level, xp = seed.Profile.xp, gold = seed.Profile.gold, potions = seed.Profile.potions,
                    skillRanks = seed.Profile.skillRanks, weaponId = seed.Profile.weaponId, armorId = seed.Profile.armorId, relicId = seed.Profile.relicId };
                var expected = new Dictionary<string, ItemData>();
                foreach (ItemData item in seed.Profile.inventory)
                {
                    var oldItem = new LegacyEquipmentItem { id = item.id, name = item.name, slot = item.slot, rarity = item.rarity, level = item.level, attack = item.attack, defense = item.defense, health = item.health, upgradeLevel = item.upgradeLevel };
                    if (unusual && sources.Contains(item.id))
                    {
                        oldItem.upgradeLevel = item.slot == ItemSlot.Weapon ? 8 : 7;
                        oldItem.attack = item.slot == ItemSlot.Weapon ? 10000 : item.slot == ItemSlot.Relic ? 1 : 0;
                        oldItem.defense = item.slot == ItemSlot.Armor ? 10000 : 0;
                        oldItem.health = item.slot == ItemSlot.Weapon ? 0 : item.slot == ItemSlot.Relic ? 100000 : 3;
                    }
                    legacy.inventory.Add(oldItem);
                    if (sources.Contains(item.id)) expected[item.id] = JsonUtility.FromJson<ItemData>(JsonUtility.ToJson(oldItem));
                }
                string payload = JsonUtility.ToJson(legacy);
                Check(!payload.Contains("baseAttack") && !payload.Contains("upgradeAnchorLevel") && !payload.Contains("upgradeBaseInitialized"), name + ": legacy JSON actually omits new equipment metadata");
                WriteEnvelope(name, payload);
                string untouched = File.ReadAllText(SavePath(name));
                foreach (ItemData item in expected.Values)
                {
                    string itemJson = JsonUtility.ToJson(item, true);
                    ItemData preview = seed.PreviewUpgrade(item, item.upgradeLevel);
                    Check(preview != null && preview.upgradeBaseInitialized && preview.upgradeLevel == item.upgradeLevel && preview.balanceRevision == 1 && BoundedEquipment(preview) && !item.upgradeBaseInitialized && JsonUtility.ToJson(item, true) == itemJson, name + ": legacy preview initializes only its independent copy");
                }
                Check(File.ReadAllText(SavePath(name)) == untouched, name + ": legacy preview never rewrites the saved file");
                var restored = new ProgressionService(CaseDirectory(name));
                Check(restored.Load(), name + ": old upgraded items load through real JsonUtility");
                var metadata = new Dictionary<string, string>();
                for (int i = 0; i < sources.Count; i++)
                {
                    ItemData item = restored.Profile.inventory.Find(entry => entry.id == sources[i]);
                    Check(item.upgradeLevel == expected[item.id].upgradeLevel && item.upgradeBaseInitialized && item.balanceRevision == 1 && BoundedEquipment(item), name + ": migration preserves paid rank and applies bounded linear stats");
                    expected[item.id] = JsonUtility.FromJson<ItemData>(JsonUtility.ToJson(item));
                    metadata[item.id] = JsonUtility.ToJson(item, true);
                }
                for (int cycle = 0; cycle < 2; cycle++)
                    for (int i = 0; i < sources.Count; i++)
                    {
                        int gold = restored.Profile.gold;
                        Check(restored.Equip(targets[i]), name + ": legacy slot rank applies on equipment replacement before reload");
                        var away = new ProgressionService(CaseDirectory(name));
                        Check(away.Load() && away.Equip(sources[i]), name + ": legacy slot rank returns to original equipment after an intervening save/load");
                        restored = new ProgressionService(CaseDirectory(name));
                        Check(restored.Load() && restored.Profile.gold == gold && JsonUtility.ToJson(restored.Profile.inventory.Find(entry => entry.id == sources[i]), true) == metadata[sources[i]], name + ": repeated reload and equipment replacement preserve all three stats and all eight metadata fields");
                    }
                for (int i = 0; i < sources.Count; i++)
                {
                    ItemData item = restored.Profile.inventory.Find(entry => entry.id == sources[i]);
                    ItemData before = expected[item.id];
                    ItemData next = restored.PreviewUpgrade(item, item.upgradeLevel + 1);
                    int cost = restored.UpgradeCost(item);
                    int gold = restored.Profile.gold;
                    Check(restored.Upgrade(item.id) && SameEquipment(item, next) && item.attack >= before.attack && item.defense >= before.defense && item.health >= before.health && BoundedEquipment(item) && restored.Profile.gold == gold - cost, name + ": migrated anchors support continued paid upgrades without decreases or overflow");
                }
                report.passedStages.Add(name + ": missing base fields, exact anchored legacy values, immutable preview and repeated automatic-inheritance JSON replacements");
            }
        }

        private static ItemData TransferItem(ProgressionService service, ItemSlot slot)
        {
            var item = new ItemData { id = Guid.NewGuid().ToString("N"), name = "Transfer fixture " + slot, slot = slot, rarity = Rarity.Epic, level = 35,
                attack = slot == ItemSlot.Weapon ? 47 : slot == ItemSlot.Relic ? 18 : 0,
                defense = slot == ItemSlot.Armor ? 26 : 0, health = slot == ItemSlot.Armor ? 180 : slot == ItemSlot.Relic ? 90 : 0 };
            service.Profile.inventory.Add(item);
            service.Save();
            Check(item.upgradeBaseInitialized, "equipment fixture receives persistent metadata through production validation");
            return item;
        }

        private static ItemData CloneEquipment(ProgressionService service, ItemData item)
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
            Check(service.LearnSkill(skill) && service.Profile.skillRanks[skill] == 1, "fixture: prerequisite learned through production LearnSkill: " + skill);
        }

        private static int SpentPoints(ProgressionService service)
        {
            int spent = 0;
            foreach (int rank in service.Profile.skillRanks) spent += rank;
            return spent;
        }

        private static string CaseDirectory(string name) { return Path.Combine(report.outputDirectory, name); }
        private static string SavePath(string name) { return Path.Combine(CaseDirectory(name), "emberfall-save.json"); }
        private static void WriteEnvelope(string name, string profileJson)
        {
            File.WriteAllText(SavePath(name), "{\"format\":\"emberfall-character\",\"version\":1,\"profile\":" + profileJson + "}", new UTF8Encoding(false));
        }
        private static bool Near(float a, float b) { return Math.Abs(a - b) < .003f; }
        private static bool SameStats(StatBlock a, StatBlock b)
        {
            return Near(a.Damage, b.Damage) && Near(a.Armor, b.Armor) && Near(a.MaxHealth, b.MaxHealth) && Near(a.MoveSpeed, b.MoveSpeed) && Near(a.CritChance, b.CritChance);
        }
        private static void Check(bool condition, string description)
        {
            report.assertions++;
            if (!condition) throw new Exception("Assertion " + report.assertions + " failed: " + description);
        }
    }
}
