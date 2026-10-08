// Production-service fixtures under unique fake-save roots. No player saves or Unity runtime.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Emberfall;
using UnityEngine;

public static class EnemyKillRewardTests
{
    private static int checks, cases;
    private static string root;

    public static string Run(string directory)
    {
        checks = cases = 0;
        root = Path.Combine(directory, "enemy-kill-reward-" + Guid.NewGuid().ToString("N"));
        foreach (HeroClass hero in Enum.GetValues(typeof(HeroClass)))
        {
            CompleteKillKeepsOneRecoveryPoint(hero, false);
            CompleteKillKeepsOneRecoveryPoint(hero, true);
        }
        SingleLevelAndPostRewardLoot();
        CapsNeverWrapOrMintPoints();
        ZeroAmountsStillRecordOneKill();
        InvalidAmountsAreReadOnly();
        FailedStorageKeepsLiveReward(false);
        FailedStorageKeepsLiveReward(true);
        FailedStorageKeepsLiveRewardWhenBothDocumentsAreDamaged();
        SelectedSlotAndDeletedDestinationStayIsolated(false);
        SelectedSlotAndDeletedDestinationStayIsolated(true);
        CallbackMutationsRequireTheFinalSave();
        return "PASS: " + checks + " earned-kill reward assertions in " + cases + " isolated scenarios";
    }

    private static void Check(bool value, string message)
    { checks++; if (!value) throw new InvalidOperationException(message); }

    private static ProgressionService Fresh(HeroClass hero = HeroClass.Ranger)
    {
        var p = new ProgressionService(Path.Combine(root, "case-" + (++cases)));
        Check(p.CreateNewSlot(hero), "create isolated character");
        return p;
    }

    private static string State(ProgressionService p) { return JsonUtility.ToJson(p.Profile, true); }
    private static string Files(string path)
    {
        return string.Join("\n", new[] { path, path + ".bak", path + ".tmp", path + ".delete-pending" }.Select(file =>
            File.Exists(file) ? file + ":" + File.GetLastWriteTimeUtc(file).Ticks + ":" + Convert.ToBase64String(File.ReadAllBytes(file)) :
            Directory.Exists(file) ? file + ":directory" : file + ":missing"));
    }
    private static void Stamp(string path)
    {
        File.SetLastWriteTimeUtc(path, new DateTime(2001, 2, 3, 4, 5, 6, DateTimeKind.Utc));
        File.SetLastWriteTimeUtc(path + ".bak", new DateTime(2002, 3, 4, 5, 6, 7, DateTimeKind.Utc));
    }
    private static void CheckReload(ProgressionService p, string expected)
    {
        var restored = new ProgressionService(p.SaveDirectory);
        Check(restored.LoadSlot(p.CurrentSlotId) && State(restored) == expected, "the entire reward reloads from the selected slot");
    }

    private static void CompleteKillKeepsOneRecoveryPoint(HeroClass hero, bool multipleLevels)
    {
        var p = Fresh(hero);
        p.Profile.kills = 8; p.Profile.gold = 123; p.Save();
        string path = p.SaveFilePath, slot = p.CurrentSlotId, before = File.ReadAllText(path);
        GameProfile live = p.Profile;
        int xp = multipleLevels ? GameBalance.XpToNext(1) + GameBalance.XpToNext(2) + GameBalance.XpToNext(3) + 13 : 13;
        int finalLevel = multipleLevels ? 4 : 1;
        var events = new List<string>();
        Action inspect = () =>
        {
            Check(ReferenceEquals(live, p.Profile) && p.CurrentSlotId == slot && p.SaveFilePath == path,
                "reward callbacks retain the live profile and selected save identity");
            Check(p.Profile.kills == 9 && p.Profile.gold == 160 && p.Profile.level == finalLevel && p.Profile.xp == 13 &&
                p.Profile.skillPoints + p.Profile.skillRanks[0] == GameBalance.SkillPointBudget(finalLevel), "every callback sees all final earned values together");
            Check(string.IsNullOrEmpty(p.LastError) && File.ReadAllText(path + ".bak") == before,
                "successful callbacks observe the durable complete reward and exact pre-kill backup");
        };
        p.Changed += () => { events.Add("changed"); inspect(); };
        p.LeveledUp += level => { events.Add("level:" + level); inspect(); };
        p.GrantEnemyKillReward(37, xp);
        Check(string.Join(",", events) == (multipleLevels ? "changed,level:2,level:3,level:4" : "changed"),
            "exactly one Changed precedes each earned level notification in ascending order");
        Check(File.ReadAllText(path) != before && File.ReadAllText(path + ".bak") == before,
            "one coupled reward leaves the backup before both currency and experience changes");
        string earned = State(p); CheckReload(p, earned);
        Stamp(path); string after = Files(path);
        p.Save(); p.Save();
        Check(Files(path) == after && State(p) == earned && events.Count == finalLevel,
            "trailing and repeated saves neither rotate the recovery point nor repeat callbacks");
    }

    private static void SingleLevelAndPostRewardLoot()
    {
        var p = Fresh(); p.Profile.xp = GameBalance.XpToNext(1) - 1; p.Save();
        var events = new List<string>();
        p.Changed += () => events.Add("changed");
        p.LeveledUp += level => events.Add("level:" + level);
        p.GrantEnemyKillReward(1, 1);
        Check(p.Profile.level == 2 && p.Profile.xp == 0 && p.Profile.skillPoints == 0 &&
            string.Join(",", events) == "changed,level:2", "exact threshold grants one level, no duplicate starting point and one ordered callback");
        string beforeRoll = Files(p.SaveFilePath);
        ItemData ordinary = p.RollLoot(p.Profile.level, false);
        ItemData boss = p.RollLoot(p.Profile.level + 1, true);
        Check(ordinary.level == ProgressionService.EquipmentGenerationLevel(p.Profile.level) && boss.level == ProgressionService.EquipmentGenerationLevel(p.Profile.level+1) && Files(p.SaveFilePath) == beforeRoll,
            "post-reward loot uses the existing generation-level bands while rolling remains read-only");
        string rewarded = File.ReadAllText(p.SaveFilePath);
        Check(p.CollectLoot(boss) && p.Profile.inventory.Count(item => item.id == boss.id) == 1 &&
            File.ReadAllText(p.SaveFilePath + ".bak") == rewarded,
            "actual loot collection retains its independent durable transaction and rolled identity");
    }

    private static void CapsNeverWrapOrMintPoints()
    {
        var p = Fresh();
        p.Profile.level = ProgressionService.MaximumLevel - 1;
        p.Profile.xp = GameBalance.XpToNext(p.Profile.level) - 1;
        p.Profile.gold = 999999998; p.Profile.kills = int.MaxValue - 1; p.Save();
        int free = p.Profile.skillPoints; var events = new List<string>();
        p.Changed += () => events.Add("changed"); p.LeveledUp += level => events.Add("level:" + level);
        p.GrantEnemyKillReward(int.MaxValue, int.MaxValue);
        Check(p.Profile.gold == 999999999 && p.Profile.kills == int.MaxValue && p.Profile.level == ProgressionService.MaximumLevel &&
            p.Profile.xp == 0 && p.Profile.skillPoints == free + 1, "large valid amounts safely saturate gold, kills and XP at the level cap");
        Check(string.Join(",", events) == "changed,level:" + ProgressionService.MaximumLevel, "entering the cap emits only the last earned level");
        CheckReload(p, State(p));
        Stamp(p.SaveFilePath); string capped = Files(p.SaveFilePath); events.Clear();
        p.GrantEnemyKillReward(int.MaxValue, int.MaxValue);
        Check(p.Profile.gold == 999999999 && p.Profile.kills == int.MaxValue && p.Profile.xp == 0 && p.Profile.skillPoints == free + 1 &&
            string.Join(",", events) == "changed", "a capped kill cannot overflow kills to zero, add points or emit a level event");
        Check(Files(p.SaveFilePath) == capped, "fully saturated reward still respects unchanged-document save deduplication");
        p = Fresh(); var allLevels = new List<int>();
        p.LeveledUp += level => allLevels.Add(level);
        p.GrantEnemyKillReward(int.MaxValue, int.MaxValue);
        Check(p.Profile.gold == 999999999 && p.Profile.level == ProgressionService.MaximumLevel && p.Profile.skillPoints + p.Profile.skillRanks[0] == ProgressionService.MaximumLevel - 1 &&
            p.Profile.xp == 0 && p.Profile.kills == 1, "int maximum XP can safely traverse the complete level range");
        Check(allLevels.SequenceEqual(Enumerable.Range(2, ProgressionService.MaximumLevel - 1)),
            "even a maximum-sized grant emits every earned level once and in order");
        p = Fresh(); p.Profile.level = ProgressionService.MaximumLevel; p.Save();
        int changes = 0, cappedLevels = 0, capPoints = p.Profile.skillPoints;
        p.Changed += () => changes++; p.LeveledUp += _ => cappedLevels++;
        p.GrantEnemyKillReward(24, int.MaxValue);
        Check(p.Profile.kills == 1 && p.Profile.gold == 84 && p.Profile.xp == 0 && p.Profile.skillPoints == capPoints &&
            changes == 1 && cappedLevels == 0, "an already maximum-level character still earns its normal kill and gold without extra skill points");
        CheckReload(p, State(p));
    }

    private static void ZeroAmountsStillRecordOneKill()
    {
        var p = Fresh(); int events = 0, levels = 0;
        p.Changed += () => events++; p.LeveledUp += _ => levels++;
        p.GrantEnemyKillReward(0, 0);
        Check(p.Profile.kills == 1 && p.Profile.gold == 60 && p.Profile.xp == 0 && p.Profile.level == 1 && events == 1 && levels == 0,
            "zero currency and XP remain a valid admitted kill with exactly one notification");
        CheckReload(p, State(p));
    }

    private static void InvalidAmountsAreReadOnly()
    {
        var p = Fresh(); string path = p.SaveFilePath, slot = p.CurrentSlotId, state = State(p), files = Files(path);
        GameProfile live = p.Profile; int events = 0, levels = 0;
        p.Changed += () => events++; p.LeveledUp += _ => levels++;
        foreach (int[] input in new[] { new[] { -1, 10 }, new[] { 10, -1 }, new[] { int.MinValue, int.MaxValue }, new[] { int.MaxValue, int.MinValue } })
        {
            p.GrantEnemyKillReward(input[0], input[1]);
            Check(State(p) == state && ReferenceEquals(live, p.Profile) && p.CurrentSlotId == slot && p.SaveFilePath == path &&
                Files(path) == files && events == 0 && levels == 0 && !string.IsNullOrEmpty(p.LastError),
                "negative reward argument refuses before any live mutation, save or callback");
        }
    }

    private static void FailedStorageKeepsLiveReward(bool recoverableTemporary)
    {
        var p = Fresh(); string path = p.SaveFilePath, slot = p.CurrentSlotId, before = File.ReadAllText(path);
        GameProfile live = p.Profile;
        if (recoverableTemporary) File.Copy(path, path + ".tmp"); else Directory.CreateDirectory(path + ".tmp");
        string blocked = Files(path); var events = new List<string>();
        Action inspect = () =>
        {
            Check(ReferenceEquals(live, p.Profile) && p.CurrentSlotId == slot && p.SaveFilePath == path,
                "failed reward keeps the original live profile and selected slot");
            Check(p.Profile.kills == 1 && p.Profile.gold == 85 && p.Profile.level == 3 && p.Profile.xp == 7 && p.Profile.skillPoints == 1 &&
                p.LastError.StartsWith("保存失败") && Files(path) == blocked,
                "failure callbacks see complete live earnings and a visible error with every storage artifact preserved");
        };
        p.Changed += () => { events.Add("changed"); inspect(); };
        p.LeveledUp += level => { events.Add("level:" + level); inspect(); };
        p.GrantEnemyKillReward(25, GameBalance.XpToNext(1) + GameBalance.XpToNext(2) + 7);
        string earned = State(p), error = p.LastError;
        Check(string.Join(",", events) == "changed,level:2,level:3", "failed storage still delivers one live refresh and the same ordered earned levels");
        p.Save();
        Check(State(p) == earned && p.LastError == error && Files(path) == blocked && events.Count == 3,
            "trailing blocked save neither grants again nor clears the failure");
        if (recoverableTemporary) File.Delete(path + ".tmp"); else Directory.Delete(path + ".tmp");
        p.Save();
        Check(string.IsNullOrEmpty(p.LastError) && State(p) == earned && events.Count == 3 &&
            ReferenceEquals(live, p.Profile) && p.CurrentSlotId == slot && File.ReadAllText(path + ".bak") == before,
            "one Save retry commits the entire retained reward and clears failure without duplicate earnings or events");
        CheckReload(p, earned);
        Stamp(path); string committed = Files(path); p.Save();
        Check(Files(path) == committed && events.Count == 3, "a later Save is a true no-op after the one successful retry");
    }

    private static void FailedStorageKeepsLiveRewardWhenBothDocumentsAreDamaged()
    {
        var p = Fresh(); string path = p.SaveFilePath; GameProfile live = p.Profile;
        File.WriteAllText(path, "damaged primary"); File.WriteAllText(path + ".bak", "damaged backup");
        string before = Files(path); int events = 0;
        p.Changed += () => events++;
        p.GrantEnemyKillReward(11, 9);
        Check(ReferenceEquals(live, p.Profile) && p.Profile.gold == 71 && p.Profile.xp == 9 && p.Profile.kills == 1 && events == 1 &&
            p.LastError.StartsWith("保存失败") && Files(path) == before,
            "ambiguous storage refuses writes while retaining earned values and both damaged originals");
    }

    private static void SelectedSlotAndDeletedDestinationStayIsolated(bool deletedThroughService)
    {
        var p = Fresh(); string firstId = p.CurrentSlotId, firstPath = p.SaveFilePath;
        Check(p.CreateNewSlot(HeroClass.Vanguard), "create an unrelated same-directory slot");
        string otherPath = p.SaveFilePath, other = Files(otherPath);
        Check(p.LoadSlot(firstId), "select the original character explicitly");
        GameProfile live = p.Profile;
        p.GrantEnemyKillReward(17, 4);
        Check(p.CurrentSlotId == firstId && p.SaveFilePath == firstPath && ReferenceEquals(live, p.Profile) && Files(otherPath) == other,
            "earned reward cannot write a different slot after a load selection");
        CheckReload(p, State(p));
        if (deletedThroughService)
        {
            SaveDeletionRequest request;
            Check(p.PrepareSaveDeletion(firstId, out request) && p.DeleteSaveSlot(request), "delete only the isolated active character");
        }
        else { File.Delete(firstPath); File.Delete(firstPath + ".bak"); }
        int gold = p.Profile.gold, kills = p.Profile.kills, xp = p.Profile.xp, events = 0;
        p.Changed += () => events++;
        p.GrantEnemyKillReward(3, 2);
        Check(ReferenceEquals(live, p.Profile) && p.Profile.gold == gold + 3 && p.Profile.kills == kills + 1 && p.Profile.xp == xp + 2 && events == 1,
            "missing destination retains exactly the new live earnings and one refresh");
        p.Save();
        Check(p.CurrentSlotId == null && !p.HasActiveSave && p.SaveFilePath == firstPath && p.LastError.StartsWith("保存失败") &&
            !File.Exists(firstPath) && !File.Exists(firstPath + ".bak") && Files(otherPath) == other,
            "deleted or moved attached destination is never resurrected or redirected into another character");
    }

    private static void CallbackMutationsRequireTheFinalSave()
    {
        var p = Fresh(); string path = p.SaveFilePath, before = File.ReadAllText(path);
        int changes = 0, levels = 0;
        p.Changed += () => { changes++; p.Profile.gold += 7; p.Profile.tutorialMask |= 1; };
        p.LeveledUp += level => { levels++; p.Profile.mechanicMaterials += level; };
        p.GrantEnemyKillReward(5, GameBalance.XpToNext(1) + GameBalance.XpToNext(2));
        string reward = File.ReadAllText(path), expected = State(p);
        var disk = new ProgressionService(p.SaveDirectory);
        Check(disk.LoadSlot(p.CurrentSlotId) && disk.Profile.gold == 65 && disk.Profile.mechanicMaterials == 0 && disk.Profile.tutorialMask == 0 &&
            File.ReadAllText(path + ".bak") == before, "reward commits before callbacks, retaining the pre-entire-kill recovery point");
        p.Save();
        Check(File.ReadAllText(path) != reward && File.ReadAllText(path + ".bak") == reward && p.Profile.gold == 72 &&
            p.Profile.mechanicMaterials == 5 && p.Profile.tutorialMask == 1 && changes == 1 && levels == 2,
            "necessary final save persists direct Changed and ordered LeveledUp mutations without refiring either event");
        CheckReload(p, expected);
        Stamp(path); string durable = Files(path); p.Save();
        Check(Files(path) == durable, "callback changes are written once and later saves deduplicate again");
    }
}
