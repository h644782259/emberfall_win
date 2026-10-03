// Production service only. Fixtures never read or write a player's save directory.
using System;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using Emberfall;
using UnityEngine;

public static class BuildPresetTests
{
    private static int checks;
    private static void Check(bool value, string why) { checks++; if (!value) throw new InvalidOperationException(why); }
    private static string State(ProgressionService p) { return JsonUtility.ToJson(p.Profile, true); }
    private static string Files(ProgressionService p)
    { return File.ReadAllText(p.SaveFilePath) + "\nBACKUP\n" + File.ReadAllText(p.SaveFilePath + ".bak"); }
    private static ProgressionService Fresh(string root, HeroClass hero = HeroClass.Arcanist)
    {
        var p = new ProgressionService(Path.Combine(root, Guid.NewGuid().ToString("N")));
        Check(p.CreateNewSlot(hero), "isolated character created"); return p;
    }
    public static string Run(string directory)
    {
        checks = 0;
        string root = Path.Combine(directory, "build-presets-" + Guid.NewGuid().ToString("N"));
        CombinedRefund(root); AllClassesRoundTrip(root); EquipmentReferences(root); InvalidPresets(root); NewUnlockBudget(root); MigrationAndSlots(root);
        return "PASS: " + checks + " combined-respec/build-preset assertions in isolated fake saves";
    }
    private static void CombinedRefund(string root)
    {
        var p = Fresh(root); p.Profile.level = 100; p.Profile.skillRanks = Enumerable.Repeat(3, GameBalance.SkillCount).ToArray();
        p.Profile.masteryRanks = new[] { 20, 10, 5, 0 }; p.Profile.masteryCore = 0; p.Save();
        Check(p.RefundableSkillRanks == 20 && p.RefundableMasteryPoints == 35 && p.RefundableBuildPoints == 55, "combined preview counts both shared investments exactly");
        int points = p.Profile.skillPoints, events = 0; p.Changed += () => events++;
        string before = State(p), files = Files(p); GameProfile live = p.Profile;
        Check(!p.ResetBuild(false) && State(p) == before && Files(p) == files && events == 0, "respec outside camp is read-only");
        Directory.CreateDirectory(p.SaveFilePath + ".tmp");
        Check(!p.ResetBuild(true) && ReferenceEquals(live, p.Profile) && State(p) == before && Files(p) == files && events == 0,
            "a failed joint respec cannot partially refund skills or mastery");
        Directory.Delete(p.SaveFilePath + ".tmp");
        string hotbar = string.Join(",", p.Profile.equippedSkills), equipment = p.Profile.weaponId;
        Check(p.ResetBuild(true) && p.Profile.skillPoints == points + 55 && p.Profile.skillRanks.All(r => r == 1) && p.Profile.masteryRanks.All(r => r == 0) &&
            p.Profile.masteryCore == -1 && events == 1, "single successful respec returns exact points and disables core");
        Check(string.Join(",", p.Profile.equippedSkills) == hotbar && p.Profile.weaponId == equipment, "respec retains all learned first ranks, hotbar and equipment");
        before = State(p); files = Files(p);
        Check(!p.ResetBuild(true) && State(p) == before && Files(p) == files && events == 1, "repeat respec cannot create points or rotate files");
        Check(p.LoadSlot(p.CurrentSlotId) && State(p) == before && p.Profile.skillPoints == 89, "joint refund remains exact after reload");
    }
    private static void AllClassesRoundTrip(string root)
    {
        foreach (HeroClass hero in Enum.GetValues(typeof(HeroClass)))
        {
            var p = Fresh(root, hero); p.Profile.level = 100; p.Profile.skillRanks = Enumerable.Repeat(3, GameBalance.SkillCount).ToArray();
            p.Profile.masteryRanks = new[] { 20, 0, 0, 10 }; p.Profile.masteryCore = 0;
            p.Profile.specialization = hero == HeroClass.Arcanist ? ElementalistSpecialization.Burn : ElementalistSpecialization.None;
            p.Profile.summonerRoute = SummonerRoute.Bonded; p.Profile.slotUpgradeRanks[0] = 6; p.Save();
            int events = 0; p.Changed += () => events++;
            string before = State(p), files = Files(p); GameProfile original = p.Profile;
            Check(!p.SaveBuildPreset(0, false) && State(p) == before && !p.HasBuildPreset(0), "preset capture is camp-only");
            Check(!p.SaveBuildPreset(-1, true) && !p.SaveBuildPreset(2, true) && State(p) == before, "only two fixed preset slots are writable");
            Directory.CreateDirectory(p.SaveFilePath + ".tmp");
            Check(!p.SaveBuildPreset(0, true) && State(p) == before && Files(p) == files && ReferenceEquals(original, p.Profile) && events == 0,
                "failed preset capture publishes nothing");
            Directory.Delete(p.SaveFilePath + ".tmp");
            Check(p.SaveBuildPreset(0, true) && p.HasBuildPreset(0) && !p.HasBuildPreset(1) && events == 1, "first preset stored only after successful write");
            string preset = JsonUtility.ToJson(p.Profile.buildPresets[0], true), savedWeapon = p.Profile.weaponId;
            int inventoryCount = p.Profile.inventory.Count;
            Check(!string.IsNullOrEmpty(p.BuildPresetSummary(0)) && !string.IsNullOrEmpty(p.CurrentBuildSummary()) && p.BuildPresetLockReason(0, true) == "", "preset exposes stable preview and availability");
            Check(!p.ApplyBuildPreset(0, false) && !p.ApplyBuildPreset(1, true), "apply needs camp and an occupied slot");
            Check(p.ResetBuild(true), "prepare a different current build");
            p.Profile.masteryRanks[1] = 10; p.Profile.masteryCore = 1;
            p.Profile.specialization = hero == HeroClass.Arcanist ? ElementalistSpecialization.Shatter : ElementalistSpecialization.None;
            p.Profile.summonerRoute = SummonerRoute.Pack;
            int key = p.Profile.hotbarKeys[0]; p.Profile.hotbarKeys[0] = p.Profile.hotbarKeys[1]; p.Profile.hotbarKeys[1] = key;
            int skill = p.Profile.equippedSkills[0]; p.Profile.equippedSkills[0] = p.Profile.equippedSkills[1]; p.Profile.equippedSkills[1] = skill;
            p.Save();
            Check(JsonUtility.ToJson(p.Profile.buildPresets[0], true) == preset, "saved preset arrays do not alias the live build");
            Check(p.SaveBuildPreset(1, true), "second build captured independently");
            before = State(p); files = Files(p); original = p.Profile; events = 0;
            Directory.CreateDirectory(p.SaveFilePath + ".tmp");
            Check(!p.ApplyBuildPreset(0, true) && ReferenceEquals(original, p.Profile) && State(p) == before && Files(p) == files && events == 0,
                "failed apply rolls back skills, mastery, class choices, hotbar and equipment together");
            Directory.Delete(p.SaveFilePath + ".tmp");
            Check(p.ApplyBuildPreset(0, true) && events == 1 && p.Profile.skillRanks.All(r => r == 3) && p.Profile.masteryRanks[0] == 20 &&
                p.Profile.masteryCore == 0 && p.Profile.summonerRoute == SummonerRoute.Bonded && p.Profile.skillPoints == 39,
                "retry applies complete first build and exact free-point budget");
            Check(p.Profile.weaponId == savedWeapon && p.Profile.inventory.Count == inventoryCount && p.SlotUpgradeRank(ItemSlot.Weapon) == 6 &&
                p.Equipped(ItemSlot.Weapon).upgradeLevel == 6, "preset references retain existing items and permanent slot training without duplication");
            Check(p.Profile.specialization == (hero == HeroClass.Arcanist ? ElementalistSpecialization.Burn : ElementalistSpecialization.None), "specialization restored per class");
            Check(!ReferenceEquals(p.Profile.skillRanks, p.Profile.buildPresets[0].skillRanks) && !ReferenceEquals(p.Profile.masteryRanks, p.Profile.buildPresets[0].masteryRanks) &&
                !ReferenceEquals(p.Profile.hotbarKeys, p.Profile.buildPresets[0].hotbarKeys), "apply never publishes preset arrays as mutable live aliases");
            files = Files(p);
            Check(p.ApplyBuildPreset(0, true) && Files(p) == files && p.Profile.skillPoints == 39, "repeat apply cannot mint points or rotate backups");
            Check(p.LoadSlot(p.CurrentSlotId) && p.HasBuildPreset(0) && p.HasBuildPreset(1) && p.ApplyBuildPreset(1, true) && p.Profile.skillRanks.All(r => r == 1) &&
                p.Profile.masteryCore == 1 && p.Profile.summonerRoute == SummonerRoute.Pack && p.Profile.skillPoints == 79, "both distinct presets survive reload and remain reusable");
            Check(JsonUtility.ToJson(p.Profile.buildPresets[0], true) == preset, "switching never alters stored build or its stable equipment references");
        }
    }
    private static void EquipmentReferences(string root)
    {
        var p = Fresh(root); p.Profile.level = 20; p.Profile.skillRanks[0] = 1; p.Profile.slotUpgradeRanks[0] = 4; p.Save();
        string oldWeapon = p.Profile.weaponId;
        Check(p.SaveBuildPreset(0, true), "initial owned equipment captured");
        var newWeapon = new ItemData { id = Guid.NewGuid().ToString("N"), name = "Preset weapon", slot = ItemSlot.Weapon,
            rarity = Rarity.Rare, level = 20, attack = 70, defense = 3, health = 8 };
        Check(p.CollectLoot(newWeapon) && p.Equip(newWeapon.id) && p.SaveBuildPreset(1, true), "second preset references a distinct owned weapon");
        p.Profile.gold = 7777; p.Profile.mechanicMaterials = 123; p.Profile.xp = 17; p.Profile.potions = 9;
        p.Profile.slotUpgradeRanks[0] = 8; p.Save();
        string preset = JsonUtility.ToJson(p.Profile.buildPresets[0], true), before = State(p), files = Files(p);
        Directory.CreateDirectory(p.SaveFilePath + ".tmp");
        Check(!p.SaveBuildPreset(0, true) && State(p) == before && Files(p) == files && JsonUtility.ToJson(p.Profile.buildPresets[0], true) == preset,
            "failed overwrite preserves the old occupied preset and entire live character");
        Directory.Delete(p.SaveFilePath + ".tmp");
        string[] ids = p.Profile.inventory.Select(x => x.id).OrderBy(x => x).ToArray();
        Check(p.ApplyBuildPreset(0, true) && p.Profile.weaponId == oldWeapon && p.Equipped(ItemSlot.Weapon).upgradeLevel == 8 &&
            p.Profile.inventory.Find(x => x.id == newWeapon.id).upgradeLevel == 0, "apply reuses old weapon with current slot training and removes stale enhancement cache from unequipped item");
        Check(p.Profile.gold == 7777 && p.Profile.mechanicMaterials == 123 && p.Profile.xp == 17 && p.Profile.potions == 9 &&
            p.Profile.inventory.Select(x => x.id).OrderBy(x => x).SequenceEqual(ids), "build switching never restores older currency, XP, consumables or item copies");
        Check(p.ApplyBuildPreset(1, true) && p.Profile.weaponId == newWeapon.id && p.Equipped(ItemSlot.Weapon).upgradeLevel == 8,
            "second preset switches to exactly its referenced item without retraining cost");
        Check(p.ApplyBuildPreset(0, true) && p.Sell(newWeapon.id,true), "a no-longer-equipped preset reference may be explicitly sold");
        before = State(p); files = Files(p);
        Check(!p.ApplyBuildPreset(1, true) && State(p) == before && Files(p) == files && p.HasBuildPreset(1),
            "missing sold item blocks application while retaining the preset for inspection or overwrite");
        var sameName = new ItemData { id = Guid.NewGuid().ToString("N"), name = "Preset weapon", slot = ItemSlot.Weapon,
            rarity = Rarity.Rare, level = 20, attack = 70 };
        Check(p.CollectLoot(sameName) && !p.ApplyBuildPreset(1, true), "same-name new loot cannot impersonate the saved stable item ID");
        Check(p.SaveBuildPreset(1, true) && p.ApplyBuildPreset(1, true), "player can deliberately replace an unavailable preset with current build");
        SaveDeletionRequest request; string path = p.SaveFilePath;
        Check(p.PrepareSaveDeletion(p.CurrentSlotId, out request) && p.DeleteSaveSlot(request), "fixture explicitly deletes the active fake character");
        before = State(p);
        Check(!p.SaveBuildPreset(0, true) && !p.ApplyBuildPreset(0, true) && State(p) == before && !File.Exists(path) && !File.Exists(path + ".bak"),
            "preset operations cannot resurrect a deleted active character");
    }
    private static void InvalidPresets(string root)
    {
        var p = Fresh(root); p.Profile.level = 100; p.Profile.skillRanks = Enumerable.Repeat(3, GameBalance.SkillCount).ToArray();
        p.Profile.masteryRanks[0] = 20; p.Profile.masteryCore = 0; p.Save(); Check(p.SaveBuildPreset(0, true), "invalid fixtures have a lawful baseline");
        string valid = JsonUtility.ToJson(p.Profile.buildPresets[0], true);
        Action<string, Action<BuildPreset>> reject = (label, corrupt) =>
        {
            p.Profile.buildPresets[0] = JsonUtility.FromJson<BuildPreset>(valid); corrupt(p.Profile.buildPresets[0]);
            string state = State(p), files = Files(p); int events = 0; Action changed = () => events++; p.Changed += changed;
            Check(!string.IsNullOrEmpty(p.BuildPresetLockReason(0, true)) && !p.ApplyBuildPreset(0, true) && State(p) == state && Files(p) == files && events == 0,
                label + " fails explicitly without repairing or partially applying preset"); p.Changed -= changed;
        };
        reject("future schema", b => b.version = 2);
        reject("wrong class", b => b.heroClass = HeroClass.Ranger);
        reject("missing skill array", b => b.skillRanks = null);
        reject("oversized skill array", b => b.skillRanks = new int[100]);
        reject("negative skill rank", b => b.skillRanks[0] = -1);
        reject("excessive skill rank", b => b.skillRanks[0] = 4);
        reject("negative mastery", b => b.masteryRanks[0] = -1);
        reject("excessive mastery", b => b.masteryRanks[0] = 36);
        reject("shared overspend", b => b.masteryRanks = new[] { 35, 35, 35, 35 });
        reject("invalid core", b => b.masteryCore = 99);
        reject("underinvested core", b => b.masteryCore = 1);
        reject("invalid specialization", b => b.specialization = (ElementalistSpecialization)99);
        reject("invalid contract", b => b.summonerRoute = (SummonerRoute)99);
        reject("invalid hotbar size", b => b.equippedSkills = new int[1]);
        reject("passive hotbar entry", b => b.equippedSkills[0] = 3);
        reject("duplicated hotbar entry", b => b.equippedSkills[1] = b.equippedSkills[0]);
        reject("invalid hotbar entry", b => b.equippedSkills[0] = 999);
        reject("invalid hotbar page", b => b.hotbarPage = 3);
        reject("missing hotbar keys", b => b.hotbarKeys = null);
        reject("reserved hotbar key", b => b.hotbarKeys[0] = 119);
        reject("duplicate hotbar key", b => b.hotbarKeys[0] = b.hotbarKeys[1]);
        reject("missing item reference", b => b.weaponId = "missing-weapon");
        reject("wrong equipment slot", b => b.weaponId = b.armorId);
        p.Profile.buildPresets[0] = JsonUtility.FromJson<BuildPreset>(valid); p.Profile.level = 2;
        Check(!p.ApplyBuildPreset(0, true), "current level validates saved ranks before any changes");
        p.Profile.level = 100; p.Profile.skillRanks[9] = 0;
        Check(!p.ApplyBuildPreset(0, true), "preset cannot grant a currently unlearned skill or bypass its prerequisites");
        p.Profile.skillRanks[9] = 3; p.Profile.inventory.Find(x => x.id == p.Profile.weaponId).level = 101;
        Check(!p.ApplyBuildPreset(0, true), "equipment level validated before candidate repair");
    }
    private static void NewUnlockBudget(string root)
    {
        var p = Fresh(root); p.Profile.level = 50; p.Profile.skillRanks = Enumerable.Repeat(3, GameBalance.SkillCount).ToArray(); p.Profile.skillRanks[9] = 0;
        p.Profile.masteryRanks = new[] { 10, 10, 2, 0 }; p.Profile.masteryCore = 0; p.Save();
        Check(p.Profile.skillPoints == 0 && p.SaveBuildPreset(0, true), "capture old build consuming its exact point budget");
        Check(p.ResetBuild(true) && p.LearnSkill(9), "learn a new first rank after capture");
        string state = State(p), files = Files(p);
        Check(!p.ApplyBuildPreset(0, true) && State(p) == state && Files(p) == files && p.Profile.skillRanks[9] == 1,
            "new rank1 remains learned and blocks an otherwise overspent old preset");
        p.Profile.level = 51; p.Save();
        Check(p.ApplyBuildPreset(0, true) && p.Profile.skillRanks[9] == 1 && p.Profile.skillPoints == 0,
            "one newly earned point makes old preset lawful while retaining the new first rank");
    }
    private static void MigrationAndSlots(string root)
    {
        var p = Fresh(root); Check(p.Profile.buildPresets.Length == 2 && !p.HasBuildPreset(0) && !p.HasBuildPreset(1), "new character starts with two empty slots");
        JsonObject old = JsonNode.Parse(File.ReadAllText(p.SaveFilePath)).AsObject(); old["profile"].AsObject().Remove("buildPresets");
        File.WriteAllText(p.SaveFilePath, old.ToJsonString()); string bytes = File.ReadAllText(p.SaveFilePath);
        Check(p.LoadSlot(p.CurrentSlotId) && p.Profile.buildPresets.Length == 2 && !p.HasBuildPreset(0) && File.ReadAllText(p.SaveFilePath) == bytes,
            "legacy missing presets migrate empty without a load-time write");
        Check(p.SaveBuildPreset(0, true), "rank1 character can capture default locked placeholders without unlocking them");
        Check(p.Profile.skillRanks[0] == 1 && p.Profile.skillRanks.Skip(1).All(r => r == 0) && p.ApplyBuildPreset(0, true) && p.Profile.skillRanks[0] == 1 && p.Profile.skillRanks.Skip(1).All(r => r == 0) && p.Profile.skillPoints == 0,
            "default shortcut placeholders cannot grant skills or points");
        var other = Fresh(root, HeroClass.Ranger);
        Check(other.Profile.buildPresets.Length == 2 && !other.HasBuildPreset(0), "separate character never inherits another character's builds");
        string slot = p.CurrentSlotId; Check(p.SaveAsNewSlot() && p.CurrentSlotId != slot && p.HasBuildPreset(0), "explicit character snapshot retains build descriptions");
        Check(p.LoadSlot(slot) && p.HasBuildPreset(0), "snapshot leaves original character and preset intact");
        p.Profile.buildPresets = new[] { p.Profile.buildPresets[0], null, new BuildPreset { populated = true } };
        string files = Files(p); p.Save();
        Check(!string.IsNullOrEmpty(p.LastError) && Files(p) == files, "excess populated preset slots refuse storage without truncating original files");
    }
}
