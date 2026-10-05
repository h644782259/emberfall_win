using System;
using System.IO;
using System.Reflection;
using System.Text.Json.Nodes;
using Emberfall;
public static class RewardRevisionTests
{
    static int checks;
    static void Check(bool value,string reason) { checks++; if(!value)throw new Exception(reason); }
    sealed class Roll : Random
    {
        readonly int gold, rarity;
        public Roll(int gold,int rarity){this.gold=gold;this.rarity=rarity;}
        public override int Next(int max){if(max==41)return gold;if(max==100)return rarity;if(max==2)return 0;throw new Exception("unexpected random roll");}
    }
    static void Rng(ProgressionService p,int gold,int rarity)
    {typeof(ProgressionService).GetField("random",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(p,new Roll(gold,rarity));}
    static ProgressionService Fresh(string root)
    {var p=new ProgressionService(Path.Combine(root,Guid.NewGuid().ToString("N")));Check(p.CreateNewSlot(HeroClass.Ranger),"create isolated profile");return p;}
    static ChapterRunReceipt Begin(ProgressionService p,ChapterNode node,ChapterDifficulty difficulty)
    {
        ChapterRunReceipt r;Check(p.TryBeginChapterNode(node,difficulty,1,out r),"begin legal difficulty");
        for(int room=0;room<(node==ChapterNode.StarPlatform?1:2);room++)for(int i=0;i<(node==ChapterNode.StarPlatform?3:6);i++)
            Check(p.RegisterChapterEnemy(r,room,i,node==ChapterNode.StarPlatform&&i==0),"register exact roster");
        return r;
    }
    static void Legacy(ProgressionService p,int completed,int[] highest,bool missing=false)
    {
        p.Profile.chapterCompletedMask=completed;p.Profile.chapterHighestDifficulties=highest;p.Save();
        var json=JsonNode.Parse(File.ReadAllText(p.SaveFilePath));var profile=json["profile"].AsObject();
        profile.Remove("chapterDifficultyRewardMask");profile.Remove("chapterDifficultyRewardRevision");
        if(missing)profile.Remove("chapterHighestDifficulties");
        File.WriteAllText(p.SaveFilePath,json.ToJsonString());
    }
    public static void BaselineSupply(string root)
    {
        var p=Fresh(root);p.Profile.pendingFashionChest=true;Rng(p,0,0);
        Check(p.OpenDungeonChest(2)!=null&&!p.LastChestReward.Rarity.HasValue,"baseline supply must not roll fashion");
    }
    // Legacy replay fixture: an already-saved draw, not a live type selector.
    static void FreezeLegacy(ProgressionService p,int choice,int goldRoll,int qualityRoll)
    {
        var rarity=choice==2?(Rarity?)null:ProgressionService.RollFashionRarity(qualityRoll);
        p.Profile.pendingFashionChest=true;p.Profile.pendingChestRulesRevision=1;
        int gold=TierRewardRules.ChestGoldMinimum(p.Profile.pendingChestTier)+goldRoll;
        p.Profile.pendingChestDraw=new ChestReward{rulesRevision=1,id=Guid.NewGuid().ToString("N"),choice=choice,gold=choice==2?gold*3/2:gold,rarityIndex=rarity.HasValue?(int)rarity.Value:-1};p.Save();
    }
    static string LegacyOpen(ProgressionService p,int choice,int goldRoll,int qualityRoll)
    {FreezeLegacy(p,choice,goldRoll,qualityRoll);return p.OpenDungeonChest(choice);}
    public static string Run(string root)
    {
        // Enumerate the actual production rarity roll for each directional chest.
        var p=Fresh(root);
        for(int choice=0;choice<2;choice++)
        {
            int[] counts=new int[5];
            for(int roll=0;roll<100;roll++)
            {
                p.Profile.pendingFashionChest=true;p.Profile.fashions.Clear();Rng(p,1,roll);
                Check(LegacyOpen(p,choice,1,roll)!=null,"frozen directional chest commits");var r=p.LastChestReward;
                counts[r.rarityIndex+1]++;Check(r.rulesRevision==1,"new receipt uses explicit directional rules revision");
                Check(!r.Slot.HasValue || r.Slot==(choice==0?FashionSlot.Weapon:FashionSlot.Wings),"fashion slot follows weapon/wings choice");
                Check(r.gold==TierRewardRules.ChestGoldMinimum(1)+1&&!r.duplicate,"directional gold roll unchanged");
                Check(p.AcknowledgeChestReward(),"acknowledge actual receipt");
            }
            Check(counts[0]==60&&counts[1]==22&&counts[2]==12&&counts[3]==5&&counts[4]==1,"absolute fashion and rarity chances unchanged; not guaranteed");
        }
        for(int goldRoll=0;goldRoll<=40;goldRoll++)
        {
            p.Profile.pendingFashionChest=true;Rng(p,goldRoll,0);int threads=p.Profile.fashionThreads,materials=p.Profile.mechanicMaterials,fashions=p.Profile.fashions.Count;
            Check(LegacyOpen(p,2,goldRoll,0)!=null,"frozen supply opens");var r=p.LastChestReward;
            Check(!r.Rarity.HasValue&&!r.Slot.HasValue&&p.Profile.fashions.Count==fashions,"supply never rolls fashion");
            Check(r.gold==(TierRewardRules.ChestGoldMinimum(1)+goldRoll)*3/2,"supply floors each original gold roll times 1.5");
            Check(r.threadsDelta==1&&p.Profile.fashionThreads==threads+1&&p.Profile.mechanicMaterials==materials,"one base thread and no extra clear fragments in chest");
            Check(p.AcknowledgeChestReward(),"supply acknowledge");
        }
        p.Profile.fashions.Clear();p.Profile.pendingFashionChest=true;Rng(p,0,18);LegacyOpen(p,0,0,18);p.AcknowledgeChestReward();
        int beforeThreads=p.Profile.fashionThreads;p.Profile.pendingFashionChest=true;LegacyOpen(p,0,0,18);
        Check(p.LastChestReward.duplicate&&p.LastChestReward.gold==TierRewardRules.ChestGoldMinimum(1)+40&&p.Profile.fashionThreads==beforeThreads+2,"duplicate common conversion plus base thread preserved");p.AcknowledgeChestReward();
        p.Profile.pendingFashionChest=true;FreezeLegacy(p,2,0,0);p.Save();int beforeGold=p.Profile.gold;beforeThreads=p.Profile.fashionThreads;string disk=File.ReadAllText(p.SaveFilePath);int events=0;p.Changed+=()=>events++;
        Directory.CreateDirectory(p.SaveFilePath+".tmp");Check(p.OpenDungeonChest(2)==null,"failed chest save rejected");
        Check(p.Profile.gold==beforeGold&&p.Profile.fashionThreads==beforeThreads&&p.Profile.pendingFashionChest&&events==0&&File.ReadAllText(p.SaveFilePath)==disk,"failed chest exposes no reward or mutation");
        var frozen=(ChestReward)typeof(ProgressionService).GetField("pendingChestRoll",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(p);
        string frozenId=frozen.id;int frozenGold=frozen.gold;Rng(p,40,0);
        Check(p.OpenDungeonChest(0)==null,"failed chest cannot reroll by switching choice");
        Directory.Delete(p.SaveFilePath+".tmp");Check(p.Load()&&p.OpenDungeonChest(2)!=null,"retry chest commits once");
        Check(p.LastChestReward.id==frozenId&&p.LastChestReward.gold==frozenGold,"failed write reload/retry preserves exact draw despite changed rng");beforeGold=p.Profile.gold;string id=p.LastChestReward.id;
        Check(p.OpenDungeonChest(0)==null&&p.Load()&&p.LastChestReward.id==id&&p.Profile.gold==beforeGold,"retry/reload cannot reroll");
        for(int goldRoll=0;goldRoll<=40;goldRoll++)
        {
            var migrated=Fresh(root);migrated.Profile.pendingFashionChest=true;migrated.Profile.pendingChestRulesRevision=0;Rng(migrated,goldRoll,99);
            Check(migrated.OpenDungeonChest()!=null&&migrated.LastChestReward.baseGold==(60+goldRoll)*3/2,"legacy undrawn qualification floors protected base gold times1.5");
        }
        // Old choice 2 could legitimately contain Wings. New supply rules must not alter it.
        p.Profile.lastChestReward=new ChestReward{id=Guid.NewGuid().ToString("N"),choice=2,gold=93,slotIndex=0,rarityIndex=3,name="legacy wings",summary="legacy exact text",hasCurrencyDeltas=true,goldDelta=93,threadsDelta=1};
        p.Profile.pendingChestReveal=true;p.Save();var old=UnityEngine.JsonUtility.ToJson(p.LastChestReward,true);
        Check(p.Load()&&UnityEngine.JsonUtility.ToJson(p.LastChestReward,true)==old&&p.OpenDungeonChest(2)==null,"legacy saved result retained byte-for-byte semantically, never rerolled");
        Check(p.LastChestReward.rulesRevision==0&&ProgressionService.DungeonChestRules(1,p.LastChestReward).Contains("旧版已保存奖励"),"legacy rules explain new-only probabilities without remapping old choice");
        // Every fresh node/difficulty first bonus and transaction retry.
        p=Fresh(root);int bonus=0;
        for(int n=0;n<3;n++)for(int d=0;d<3;d++)
        {
            var receipt=Begin(p,(ChapterNode)n,(ChapterDifficulty)d);int initial=p.Profile.mechanicMaterials,mask=p.Profile.chapterDifficultyRewardMask;
            disk=File.ReadAllText(p.SaveFilePath);Directory.CreateDirectory(p.SaveFilePath+".tmp");
            Check(!p.TryCompleteChapterNode(receipt)&&p.Profile.mechanicMaterials==initial&&p.Profile.chapterDifficultyRewardMask==mask&&File.ReadAllText(p.SaveFilePath)==disk,"difficulty failed save pays nothing and leaves eligibility");
            Directory.Delete(p.SaveFilePath+".tmp");Check(p.TryCompleteChapterNode(receipt),"difficulty receipt retry succeeds");
            int extra=d==0?0:4;bonus+=extra;
            Check(p.Profile.mechanicMaterials==initial+receipt.Materials+extra,"base fragments independent plus exact 4 first difficulty bonus");
            initial=p.Profile.mechanicMaterials;Check(p.TryCompleteChapterNode(receipt)&&p.Load()&&p.TryCompleteChapterNode(receipt)&&p.Profile.mechanicMaterials==initial,"same receipt reload replay grants no extra");
            var repeat=Begin(p,(ChapterNode)n,(ChapterDifficulty)d);Check(p.TryCompleteChapterNode(repeat)&&p.Profile.mechanicMaterials==initial+repeat.Materials,"real repeat grants only base fragments");
        }
        Check(bonus==24&&p.Profile.chapterDifficultyRewardMask==63,"six first difficulty rewards bounded at 24");
        // Legal highest Heroic proves preceding Hard under the historical strict gate.
        p=Fresh(root);Legacy(p,7,new[]{3,2,1});int prior=p.Profile.mechanicMaterials;disk=File.ReadAllText(p.SaveFilePath);var active=p.Profile;
        Directory.CreateDirectory(p.SaveFilePath+".tmp");Check(!p.Load()&&ReferenceEquals(active,p.Profile)&&p.Profile.mechanicMaterials==prior&&File.ReadAllText(p.SaveFilePath)==disk,"migration failed save does not publish loaded role or bonus");
        Directory.Delete(p.SaveFilePath+".tmp");Check(p.Load()&&p.Profile.mechanicMaterials==prior+12&&p.Profile.chapterDifficultyRewardMask==7,"backfill only valid historic node difficulty chain");
        for(int i=0;i<3;i++)Check(p.Load()&&p.Profile.mechanicMaterials==prior+12,"backfill durable once across reloads");
        var heroic=Begin(p,ChapterNode.Redrock,ChapterDifficulty.Heroic);prior=p.Profile.mechanicMaterials;
        Check(p.TryCompleteChapterNode(heroic)&&p.Profile.mechanicMaterials==prior+heroic.Materials+4,"missing uncompleted heroic awarded on actual completion");
        p=Fresh(root);Legacy(p,7,new[]{3,3,3});prior=p.Profile.mechanicMaterials;
        Check(p.Load()&&p.Profile.mechanicMaterials==prior+24&&p.Profile.chapterDifficultyRewardMask==63,"maximum historical backfill is 24");
        foreach(bool missing in new[]{false,true})
        {
            p=Fresh(root);Legacy(p,7,new[]{0,4,-1},missing);prior=p.Profile.mechanicMaterials;
            Check(p.Load()&&p.Profile.mechanicMaterials==prior&&p.Profile.chapterDifficultyRewardMask==0,"missing or illegal highest never fabricates difficulty completion");
            ChapterRunReceipt rejected;Check(!p.TryBeginChapterNode(ChapterNode.ForestCourt,ChapterDifficulty.Heroic,1,out rejected),"normalized invalid evidence cannot unlock heroic");
            var hard=Begin(p,ChapterNode.ForestCourt,ChapterDifficulty.Hard);
            Check(p.TryCompleteChapterNode(hard)&&p.Profile.mechanicMaterials==prior+hard.Materials+4,"unknown old difficulty rewarded only upon actual completion");
        }
        p=Fresh(root);Legacy(p,0,new[]{3,3,3});prior=p.Profile.mechanicMaterials;
        Check(p.Load()&&p.Profile.mechanicMaterials==prior&&p.Profile.chapterDifficultyRewardMask==0,"highest without completed node is not evidence");
        p=Fresh(root);Legacy(p,1,new[]{2});prior=p.Profile.mechanicMaterials;
        Check(p.Load()&&p.Profile.mechanicMaterials==prior+4&&p.Profile.chapterDifficultyRewardMask==1,"short historical array does not infer missing nodes");
        p=Fresh(root);Legacy(p,1,new[]{3,0,0});prior=p.Profile.mechanicMaterials;
        string recovery=File.ReadAllText(p.SaveFilePath);File.WriteAllText(p.SaveFilePath+".bak",recovery);File.WriteAllText(p.SaveFilePath,"corrupt");
        Directory.CreateDirectory(p.SaveFilePath+".tmp");
        Check(!p.Load()&&p.Profile.mechanicMaterials==prior&&File.ReadAllText(p.SaveFilePath+".bak")==recovery,"backup recovery migration failed write preserves backup and pays nothing");
        Directory.Delete(p.SaveFilePath+".tmp");
        Check(p.Load()&&p.Profile.mechanicMaterials==prior+8&&p.Load()&&p.Profile.mechanicMaterials==prior+8,"backup recovery commits migration once");
        p=Fresh(root);p.Profile.pendingFashionChest=true;p.Save();Rng(p,0,18);
        Directory.CreateDirectory(p.SaveFilePath+".tmp");Check(p.OpenDungeonChest()==null,"freeze before new game");
        Directory.Delete(p.SaveFilePath+".tmp");string samePath=p.SaveFilePath;p.NewGame(HeroClass.Ranger);p.Profile.pendingFashionChest=true;
        Check(p.SaveFilePath==samePath&&p.OpenDungeonChest()!=null,"new game same path/count/tier cannot inherit old roll or choice lock");p.AcknowledgeChestReward();
        p.Profile.pendingFashionChest=true;p.Save();Directory.CreateDirectory(p.SaveFilePath+".tmp");Check(p.OpenDungeonChest()==null,"freeze before slot change");
        Directory.Delete(p.SaveFilePath+".tmp");string oldSlot=p.CurrentSlotId;Check(p.CreateNewSlot(HeroClass.Ranger),"switch to new role");p.Profile.pendingFashionChest=true;
        Check(p.OpenDungeonChest()!=null,"other save cannot inherit failed choice");
        Check(p.LoadSlot(oldSlot)&&p.OpenDungeonChest(1)==null&&p.OpenDungeonChest()!=null,"returning to slot preserves its unpublished draw without leaking another role choice");
        return "PASS: "+checks+" reward revision production assertions (all rolls, receipts, failure/retry/reload, six first clears and legacy migration)";
    }
}
