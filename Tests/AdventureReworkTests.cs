using System;
using System.IO;
using System.Linq;
using Emberfall;
public static class AdventureReworkTests
{
    static int checks;
    static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
    static ProgressionService Fresh(string root){var p=new ProgressionService(Path.Combine(root,Guid.NewGuid().ToString("N")));Check(p.CreateNewSlot(HeroClass.Arcanist),"create");return p;}
    public static string Run(string root)
    {
        foreach(int mode in new[]{-1,0,1,2,3})foreach(int tier in new[]{1,10,40,100})
        {
            var p=Fresh(root);p.Profile.level=27;p.Save();string id=Guid.NewGuid().ToString("N");int before=p.Profile.inventory.Count;
            Func<bool> grant=()=>mode==-1?p.TryCompleteDungeonRun(id,tier,0,0,true):p.TryGrantModeReward(id,0,0,0,tier,mode);
            Check(grant(),"clear: "+p.LastError);Check(p.Profile.inventory.Count==before&&p.Profile.pendingFashionChest,"equipment awaits chest");
            for(int other=-1;other<4;other++)Check(p.UnlockedAdventureTier(other)==(other==mode?Math.Min(100,tier+1):1),"independent tier "+other);
            string disk=File.ReadAllText(p.SaveFilePath);Check(grant()&&disk==File.ReadAllText(p.SaveFilePath),"clear replay is idempotent");
            var loaded=new ProgressionService(p.SaveDirectory);Check(loaded.LoadSlot(p.CurrentSlotId),"format 7 reload: "+loaded.LastError);p=loaded;
            Directory.CreateDirectory(p.SaveFilePath+".tmp");Check(p.OpenDungeonChest()==null&&p.Profile.inventory.Count==before&&p.Profile.pendingFashionChest,"failed chest write grants nothing");Directory.Delete(p.SaveFilePath+".tmp");
            Check(p.OpenDungeonChest()!=null,"open: "+p.LastError);Check(p.Profile.inventory.Count==before+AdventureRewardRules.EquipmentCount(mode,tier),"tier item count");
            foreach(var item in p.Profile.inventory.Skip(before)){Check(item.level==27,"character item level");Check(item.rarity>=AdventureRewardRules.MinimumRarity(mode),"quality floor");}
            Check(p.LastChestReward.materialsDelta==AdventureRewardRules.Materials(mode,tier),"material specialization");
            Check(mode==-1?p.LastChestReward.Rarity>=Rarity.Rare:!p.LastChestReward.Rarity.HasValue,"fashion specialization");
            int after=p.Profile.inventory.Count;Check(p.OpenDungeonChest()==null&&p.Profile.inventory.Count==after,"cannot reopen");
            loaded=new ProgressionService(p.SaveDirectory);Check(loaded.LoadSlot(p.CurrentSlotId)&&loaded.Profile.inventory.Count==after&&loaded.Profile.pendingChestReveal,"committed chest survives restart");
            Check(loaded.LastChestReward.equipmentIds.Length==AdventureRewardRules.EquipmentCount(mode,tier),"receipt keeps icon identities");
        }
        var legacy=Fresh(root);var document=System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(legacy.SaveFilePath));
        document["version"]=3;var profile=document["profile"].AsObject();profile.Remove("independentTierRevision");profile.Remove("adventureBestTiers");profile.Remove("chapterBestTiers");profile["highestAdventureTier"]=7;profile["bestFloor"]=3;profile["adventureRewardRevision"]=0;
        File.WriteAllText(legacy.SaveFilePath,document.ToJsonString());Check(legacy.LoadSlot(legacy.CurrentSlotId),"legacy shared tier migration");
        for(int mode=-1;mode<4;mode++)Check(legacy.UnlockedAdventureTier(mode)==8,"preserve previously unlocked access");
        Check(legacy.TryGrantModeReward(Guid.NewGuid().ToString("N"),0,0,0,8,0),"new independent clear");
        Check(legacy.UnlockedAdventureTier(0)==9&&legacy.UnlockedAdventureTier(1)==8,"migration baseline never reshares later progress");
        var chapter=Fresh(root);
        for(int node=0;node<3;node++) {
            ChapterRunReceipt receipt;Check(chapter.TryBeginChapterNode((ChapterNode)node,ChapterDifficulty.Normal,1,out receipt),"chapter entry");
            for(int room=0;room<(node==2?1:2);room++)for(int i=0;i<(node==2?3:6);i++)Check(chapter.RegisterChapterEnemy(receipt,room,i,node==2&&i==0),"chapter enemy budget");
            Check(chapter.TryCompleteChapterNode(receipt),"chapter completion: "+chapter.LastError);
            Check(chapter.UnlockedChapterTier((ChapterNode)node)==2&&chapter.UnlockedAdventureTier(-1)==1,"chapter tier independent from relic");
            if(node<2)Check(chapter.UnlockedChapterTier((ChapterNode)(node+1))==1,"next chapter has independent tier");
            Check(chapter.Profile.pendingFashionChest&&chapter.OpenDungeonChest()!=null&&chapter.AcknowledgeChestReward(),"every chapter gets chest");
        }
        var supplies=Fresh(root);int gold=supplies.Profile.gold,potions=supplies.Profile.potions;
        supplies.GrantEnemyKillReward(37,2,ground:true,potions:2);Check(supplies.Profile.gold==gold&&supplies.Profile.potions==potions,"ground supplies are not credited early");
        var reload=new ProgressionService(supplies.SaveDirectory);Check(reload.LoadSlot(supplies.CurrentSlotId)&&reload.Profile.groundGold==37&&reload.Profile.groundPotions==2,"uncollected supplies survive restart");
        Directory.CreateDirectory(reload.SaveFilePath+".tmp");Check(!reload.CollectGroundSupplies(37,2)&&reload.Profile.groundGold==37,"failed pickup is retryable");Directory.Delete(reload.SaveFilePath+".tmp");
        Check(reload.CollectGroundSupplies(37,2)&&reload.Profile.gold==gold+37&&reload.Profile.potions==potions+2,"pickup commits");
        Check(reload.CollectGroundSupplies(37,2)&&reload.Profile.gold==gold+37,"pickup replay empty escrow");
        Check(AdventureRewardRules.PotionChance(40)>AdventureRewardRules.PotionChance(1),"difficulty improves potion chance");
        return "PASS "+checks+" independent progression, chest specialization, save failures, restart and ground supply assertions";
    }
}
