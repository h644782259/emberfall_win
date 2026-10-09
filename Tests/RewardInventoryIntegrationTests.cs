using System;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using Emberfall;
public static class RewardInventoryIntegrationTests
{
    private static int checks, scenarios;
    private static string root;
    private static void Check(bool ok,string reason){checks++;if(!ok)throw new Exception(reason);}
    private static ItemData Item(string id,Rarity rarity=Rarity.Common,ItemSlot slot=ItemSlot.Weapon)
    {return new ItemData{id=id,name="奖励 "+id,slot=slot,rarity=rarity,level=1,attack=17,defense=11,health=21};}
    private static ProgressionService Fresh()
    {var p=new ProgressionService(Path.Combine(root,"scenario-"+(++scenarios)));Check(p.CreateNewSlot(HeroClass.Arcanist),"create isolated role");return p;}
    private static void Fill(ProgressionService p,int count)
    {while(p.Profile.inventory.Count<count)p.Profile.inventory.Add(Item("fill-"+p.Profile.inventory.Count));p.Save();Check(p.LastError=="","persist fixture");}
    public static string Run(string directory)
    {
        root=Path.Combine(directory,"reward-integration-"+Guid.NewGuid().ToString("N"));checks=scenarios=0;
        LegacyMigration(false);LegacyMigration(true);OverflowAndFailure();SafetyBoundary();LegacyClaimRollback();TrialAndFashion();GrowthIdlePolling();
        return "PASS: "+checks+" reward inventory integration assertions in "+scenarios+" isolated scenarios";
    }
    private static void LegacyMigration(bool overfull)
    {
        var p=Fresh();Fill(p,overfull?280:72);string worn=p.Profile.weaponId;
        var pending=Item("legacy-pending",Rarity.Epic,ItemSlot.Armor);pending.locked=true;
        var recovered=Item("legacy-recovered",Rarity.Legendary,ItemSlot.Relic);
        var doc=JsonNode.Parse(File.ReadAllText(p.SaveFilePath));var profile=doc["profile"];
        profile["rewardInventoryRevision"]=0;
        profile["pendingLoot"]=new JsonArray(JsonNode.Parse(UnityEngine.JsonUtility.ToJson(pending,true)),JsonNode.Parse(UnityEngine.JsonUtility.ToJson(pending,true)));
        profile["recoveryLoot"]=new JsonArray(JsonNode.Parse(UnityEngine.JsonUtility.ToJson(recovered,true)),JsonNode.Parse(UnityEngine.JsonUtility.ToJson(pending,true)));
        File.WriteAllText(p.SaveFilePath,doc.ToJsonString());string original=File.ReadAllText(p.SaveFilePath);
        var loader=new ProgressionService(p.SaveDirectory);string initial=UnityEngine.JsonUtility.ToJson(loader.Profile,true);
        Directory.CreateDirectory(p.SaveFilePath+".tmp");Check(!loader.LoadSlot(p.CurrentSlotId),"failed migration write refuses publication");
        Check(File.ReadAllText(p.SaveFilePath)==original&&UnityEngine.JsonUtility.ToJson(loader.Profile,true)==initial,"failed migration preserves disk and active role");
        Directory.Delete(p.SaveFilePath+".tmp");Check(loader.LoadSlot(p.CurrentSlotId),"migrate legacy rewards");
        int expected=(overfull?280:72)+2;Check(loader.Profile.inventory.Count==expected,"preserve every item including common overflow and collapse duplicate receipts");
        Check(loader.Profile.pendingLoot.Count==0&&loader.RecoveryLootCount==0&&loader.Profile.rewardInventoryRevision==1,"migration is durable before publication");
        Check(loader.Profile.weaponId==worn&&loader.Profile.inventory.Single(x=>x.id==pending.id).locked,"retain equipped identity and locks");
        string migrated=File.ReadAllText(p.SaveFilePath);Check(migrated!=original,"migration persisted without deleting source ownership");
        for(int i=0;i<4;i++){var restart=new ProgressionService(p.SaveDirectory);Check(restart.LoadSlot(p.CurrentSlotId)&&restart.Profile.inventory.Count==expected,"repeat process load does not duplicate");restart.Save();Check(File.ReadAllText(p.SaveFilePath)==migrated,"repeated save does not change migrated bytes");}
        Check(!loader.ClaimPendingLoot(pending.id)&&loader.ClaimAllPendingLoot()==0&&!loader.ClaimRecoveryLoot(recovered.id),"stale legacy claims cannot grant twice");
        Check(loader.Equip(pending.id)&&loader.Equip(recovered.id),"migrated items are immediately usable");
    }
    private static void OverflowAndFailure()
    {
        var p=Fresh();Fill(p,ProgressionService.InventoryCapacity);p.Profile.autoSellCommon=p.Profile.autoSellRare=true;
        int gold=p.Profile.gold;var drop=Item("full-common");string disk=File.ReadAllText(p.SaveFilePath);
        Directory.CreateDirectory(p.SaveFilePath+".tmp");Check(!p.CollectLoot(drop)&&!p.Profile.inventory.Any(x=>x.id==drop.id)&&File.ReadAllText(p.SaveFilePath)==disk,"failed pickup retains world ownership and disk");
        Directory.Delete(p.SaveFilePath+".tmp");Check(p.CollectLoot(drop)&&p.Profile.gold==gold&&p.Profile.inventory.Count==257,"full bag keeps common reward despite legacy autosell flags");
        Check(!p.CollectLoot(drop)&&p.Profile.inventory.Count==257,"same callback cannot pay again");
        var epic=Item("full-epic",Rarity.Epic);Check(p.PreserveGroundLoot(new[]{epic,epic,drop})&&p.Profile.inventory.Count==258,"transition capture deduplicates and goes directly into visible bag");
        Check(p.Profile.pendingLoot.Count==0&&p.RecoveryLootCount==0&&p.CanEnterDungeon,"no hidden manual claim blocks adventure");
        var loaded=new ProgressionService(p.SaveDirectory);Check(loaded.LoadSlot(p.CurrentSlotId)&&loaded.Profile.inventory.Count==258,"overflow ownership survives restart");
        Check(loaded.Sell(drop.id)&&loaded.Profile.inventory.Count==257,"explicit merchant sale remains available");
    }
    private static void SafetyBoundary()
    {
        var p=Fresh();Fill(p,ProgressionService.MaximumRetainedEquipment);var extra=Item("too-many");string disk=File.ReadAllText(p.SaveFilePath);
        Check(!p.CollectLoot(extra)&&!p.PreserveGroundLoot(new[]{extra})&&File.ReadAllText(p.SaveFilePath)==disk,"safety ceiling refuses acquisition without deleting/selling rewards");
        Check(p.Sell("fill-3")&&p.CollectLoot(extra),"same retained world item can retry after explicit cleanup");
    }
    private static void LegacyClaimRollback()
    {
        var p=Fresh();var item=Item("live-legacy");p.Profile.pendingLoot.Add(item);
        Directory.CreateDirectory(p.SaveFilePath+".tmp");Check(!p.ClaimPendingLoot(item.id)&&p.Profile.pendingLoot.Count==1&&!p.Profile.inventory.Any(x=>x.id==item.id),"compatibility claim rolls back all migration on failed save");
        Directory.Delete(p.SaveFilePath+".tmp");Check(p.ClaimPendingLoot(item.id)&&p.Profile.pendingLoot.Count==0&&p.Profile.inventory.Count(x=>x.id==item.id)==1,"legacy live reward moves once automatically");
    }
    private static void TrialAndFashion()
    {
        var p=Fresh();p.Profile.automaticGrowth=false;Check(p.SelectProgressionGoal(ProgressionGoalKind.CombatTrial),"trial is selectable goal");
        p.Profile.tutorialMask=11;p.Profile.classTutorialCompleted=true;p.Save();Check(p.SelectedProgressionGoal(true).Done&&ProgressionService.CombatTrialProgress(p.Profile)==4,"all four historical trial flags are used");
        int gold=p.Profile.gold, materials=p.Profile.mechanicMaterials;
        Directory.CreateDirectory(p.SaveFilePath+".tmp");Check(!p.AdvanceAutomaticGrowth()&&p.Profile.gold==gold,"trial reward save failure preserves ownership");Directory.Delete(p.SaveFilePath+".tmp");
        Check(p.AdvanceAutomaticGrowth()&&p.Profile.gold==gold+50&&p.Profile.mechanicMaterials==materials+1,"manual trial goal grants historical reward directly");
        Check(p.AdvanceAutomaticGrowth()&&p.Profile.gold==gold+50,"trial reward replay is idempotent");
        Fill(p,ProgressionService.InventoryCapacity);p.Profile.fashions.Add(new FashionData{id="fashion-0-2",slot=FashionSlot.Wings,rarity=Rarity.Epic});p.Save();
        var q=new ProgressionService(p.SaveDirectory);Check(q.LoadSlot(p.CurrentSlotId)&&q.Profile.fashions.Count==1&&q.SelectedProgressionGoal(true).Done,"fashion and trial ownership survive capacity migration");
    }
    private static void GrowthIdlePolling()
    {
        var p=Fresh();
        foreach(bool automatic in new[]{false,true})
        {
            p.Profile.automaticGrowth=automatic;
            string before=UnityEngine.JsonUtility.ToJson(p.Profile,true),disk=File.ReadAllText(p.SaveFilePath);
            int serializations=UnityEngine.JsonUtility.SerializationCount;
            for(int i=0;i<100;i++)Check(p.AdvanceAutomaticGrowth(),"idle growth poll succeeds");
            Check(UnityEngine.JsonUtility.SerializationCount==serializations,"idle growth does not serialize the save");
            Check(UnityEngine.JsonUtility.ToJson(p.Profile,true)==before&&File.ReadAllText(p.SaveFilePath)==disk,"idle growth preserves memory and disk");
        }
        p.Profile.automaticGrowth=false;p.Profile.classTutorialCompleted=true;
        int gold=p.Profile.gold;
        Check(p.AdvanceAutomaticGrowth()&&p.Profile.gold==gold+50,"live tutorial mutation still awards without an event");
        int after=UnityEngine.JsonUtility.SerializationCount;
        Check(p.AdvanceAutomaticGrowth()&&UnityEngine.JsonUtility.SerializationCount==after,"claimed reward polls do not copy the save");
        p.Profile.automaticGrowth=true;
        after=UnityEngine.JsonUtility.SerializationCount;
        Check(p.AdvanceAutomaticGrowth()&&UnityEngine.JsonUtility.SerializationCount==after,"incomplete next automatic goal does not copy the save");
    }

}
