using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using UnityEngine;
using Emberfall;
public static class ClassSwitchProductionTests
{
    static int checks;
    static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
    static string Json(object value)=>JsonUtility.ToJson(value,true);
    static int Cost(GameProfile p)=>p.skillRanks.Sum()+p.masteryRanks.Sum();
    static ProgressionService New(string root,HeroClass hero,int level=45)
    {
        var p=new ProgressionService(Path.Combine(root,Guid.NewGuid().ToString("N")));p.NewGame(hero);
        p.Profile.level=level;p.Save();Check(string.IsNullOrEmpty(p.LastError),"setup saves");return p;
    }
    static void Switch(ProgressionService p,HeroClass target)
    {
        var before=p.Profile;int observed=0;Action handler=()=>{Check(p.Profile.heroClass==target,"publish sees target class");observed++;};p.Changed+=handler;
        var tx=p.PrepareClassSwitch(target,true);Check(tx!=null&&ReferenceEquals(before,p.Profile)&&observed==0,"prepare is isolated");
        Check(p.CommitClassSwitch(tx,true)&&observed==0,"durable commit defers observer publication");p.PublishClassSwitch(tx);p.PublishClassSwitch(tx);
        Check(observed==1,"publication exactly once");Check(!p.CommitClassSwitch(tx,true),"transaction cannot replay");p.Changed-=handler;
    }
    public static string Run(string root)
    {
        Directory.CreateDirectory(root);
        foreach(HeroClass from in Enum.GetValues(typeof(HeroClass)))foreach(HeroClass to in Enum.GetValues(typeof(HeroClass)))
        {
            if(from==to)continue;
            var p=New(root,from);p.Profile.skillRanks=Enumerable.Repeat(1,10).ToArray();p.Profile.skillRanks[0]=3;
            p.Profile.masteryRanks=new[]{10,0,0,0};p.Profile.masteryCore=0;p.Profile.tutorialMask=15;p.Profile.classTutorialCompleted=true;
            p.Profile.specialization=from==HeroClass.Arcanist?ElementalistSpecialization.Burn:ElementalistSpecialization.None;
            p.Profile.summonerRoute=SummonerRoute.Bonded;p.Profile.slotUpgradeRanks=new[]{3,2,1};p.Profile.slotUpgradesInitialized=true;
            p.Profile.gold=420;p.Profile.xp=17;p.Profile.fashionThreads=11;p.Profile.mechanicMaterials=22;p.Profile.firstClearRewardClaimed=true;
            p.Profile.chapterFirstRewardMask=1;p.Profile.chapterDifficultyRewardMask=1;p.Profile.chapterDifficultyRewardRevision=1;
            p.Profile.lastDungeonRewardId=Guid.NewGuid().ToString("N");p.Profile.lastModeRewardId=Guid.NewGuid().ToString("N");
            p.Profile.variantKnowledge.Add(EquipmentMechanic.VenomSpread);p.Profile.variantKnowledgeRevision=1;
            var gear=p.Profile.inventory.First(x=>x.slot==BuildCatalog.MechanicSlot(EquipmentMechanic.VenomSpread));gear.mechanic=EquipmentMechanic.VenomSpread;gear.mechanicVariant=1;gear.mechanicVariantUnlocked=true;
            p.Save();Check(p.SaveBuildPreset(0,true),"save source plan");
            string inventory=Json(p.Profile.inventory),pending=Json(p.Profile.pendingLoot),recovery=Json(p.Profile.recoveryLoot),knowledge=Json(p.Profile.variantKnowledge);
            string ranks=Json(p.Profile.skillRanks),mastery=Json(p.Profile.masteryRanks),keys=Json(p.Profile.hotbarKeys),loadout=Json(p.Profile.equippedSkills);
            string weapon=p.Profile.weaponId,armor=p.Profile.armorId,relic=p.Profile.relicId,receipt=p.Profile.lastDungeonRewardId;
            int cost=Cost(p.Profile),points=p.Profile.skillPoints,level=p.Profile.level,xp=p.Profile.xp,chapter=p.Profile.chapterDifficultyRewardMask;
            Switch(p,to);
            Check(p.Profile.heroClass==to&&p.Profile.level==level&&p.Profile.xp==xp&&p.Profile.gold==420,"class switch preserves shared level XP gold");
            Check(Json(p.Profile.inventory)==inventory&&Json(p.Profile.pendingLoot)==pending&&Json(p.Profile.recoveryLoot)==recovery,"all item IDs bases quality variants and upgrade caches preserved");
            Check(p.Profile.weaponId==weapon&&p.Profile.armorId==armor&&p.Profile.relicId==relic&&p.Profile.slotUpgradeRanks.SequenceEqual(new[]{3,2,1}),"equipped identities and slot training remain shared");
            Check(Json(p.Profile.variantKnowledge)==knowledge&&p.Profile.variantKnowledge.Contains(EquipmentMechanic.VenomSpread),"foreign mechanism knowledge retained");
            Check(Json(p.Profile.skillRanks)==ranks&&Json(p.Profile.masteryRanks)==mastery&&Json(p.Profile.hotbarKeys)==keys&&Json(p.Profile.equippedSkills)==loadout,"first visit maps legal indices and hotbar without extra points");
            Check(Cost(p.Profile)==cost&&p.Profile.skillPoints==points&&!p.Profile.classTutorialCompleted&&p.Profile.tutorialMask==0,"first visit does not copy tutorials or mint points");
            Check(!p.HasBuildPreset(0)&&!p.HasBuildPreset(1)&&p.Profile.specialization==ElementalistSpecialization.None,"first visit has independent empty plans and default specialization");
            Check(p.Profile.firstClearRewardClaimed&&p.Profile.chapterDifficultyRewardMask==chapter&&p.Profile.lastDungeonRewardId==receipt&&p.Profile.fashionThreads==11&&p.Profile.mechanicMaterials==22,"shared first rewards and receipts unchanged");
            p.Profile.skillRanks[0]=2;p.Save();Check(p.SaveBuildPreset(1,true),"target plan independent");
            Switch(p,from);Check(p.Profile.skillRanks[0]==3&&p.HasBuildPreset(0)&&!p.HasBuildPreset(1)&&p.Profile.classTutorialCompleted,"return restores original allocations plans and tutorial");
            Check(p.Profile.specialization==(from==HeroClass.Arcanist?ElementalistSpecialization.Burn:ElementalistSpecialization.None),"return restores class specialization");
            var reload=new ProgressionService(p.SaveDirectory);Check(reload.Load(),"multi-class reload");Switch(reload,to);
            Check(reload.Profile.skillRanks[0]==2&&!reload.HasBuildPreset(0)&&reload.HasBuildPreset(1),"target independent state survives reload");
            foreach(string path in new[]{reload.SaveFilePath,reload.SaveFilePath+".bak"})
                using(var doc=JsonDocument.Parse(File.ReadAllText(path)))Check(doc.RootElement.GetProperty("version").GetInt32()==4,"primary and recovery envelope reject old readers");
        }
        {
            var p=New(root,HeroClass.Vanguard);p.Changed+=()=>{p.Profile.tutorialMask|=1;p.Profile.classTutorialCompleted=true;};
            p.AddGold(1);string live=Json(p.Profile);p.Save();
            Check(Json(p.Profile)==live,"active class fields have one authority after callback and save");
            var reload=new ProgressionService(p.SaveDirectory);Check(reload.Load()&&Json(reload.Profile)==live,"active marker reload is identical after callbacks");
            Switch(reload,HeroClass.Ranger);Switch(reload,HeroClass.Vanguard);
            Check(reload.Profile.tutorialMask==1&&reload.Profile.classTutorialCompleted,"departure captures current callback fields into dormant class");
        }
        foreach(int level in new[]{1,2,4,6,10,13,20,29,30,40,60})
        {
            var p=New(root,HeroClass.Vanguard,level);int budget=GameBalance.SkillPointBudget(level),cost=Cost(p.Profile),points=p.Profile.skillPoints;
            foreach(HeroClass target in new[]{HeroClass.Arcanist,HeroClass.Ranger,HeroClass.Summoner,HeroClass.Vanguard})
            {Switch(p,target);Check(Cost(p.Profile)==cost&&p.Profile.skillPoints==points&&cost+points==budget,"threshold switches conserve lifetime point budget");}
        }
        {
            var p=New(root,HeroClass.Vanguard);string oldId=p.Profile.weaponId;
            var replacement=new ItemData{id="class-shared-replacement",name="test",slot=ItemSlot.Weapon,rarity=Rarity.Common,level=1,attack=1};
            p.Profile.inventory.Add(replacement);p.Save();Check(p.SaveBuildPreset(0,true)&&p.Equip(replacement.id),"prepare dormant plan reference");
            Switch(p,HeroClass.Arcanist);Check(p.PresetReferences(oldId).Contains("剑卫"),"inactive class reference is visible");
            Check(!p.Sell(oldId)&&p.Profile.inventory.Any(x=>x.id==oldId),"sale protects inactive class plan");
            Check(p.BulkSalePresetImpact().Contains("剑卫")&&p.BulkSellLowQuality()==0,"bulk sale protects inactive class plan");
        }
        {
            var p=New(root,HeroClass.Ranger);var tx=p.PrepareClassSwitch(HeroClass.Arcanist,true);var before=p.Profile;
            string json=Json(before),disk=File.ReadAllText(p.SaveFilePath);int changes=0;p.Changed+=()=>changes++;
            Directory.CreateDirectory(p.SaveFilePath+".tmp");Check(!p.CommitClassSwitch(tx,true),"storage failure rejects switch");Directory.Delete(p.SaveFilePath+".tmp");
            Check(ReferenceEquals(before,p.Profile)&&Json(p.Profile)==json&&File.ReadAllText(p.SaveFilePath)==disk&&changes==0,"failed storage leaves source profile document and observers intact");
            Check(p.PrepareClassSwitch(HeroClass.Ranger,true)==null&&p.PrepareClassSwitch(HeroClass.Arcanist,false)==null&&p.PrepareClassSwitch((HeroClass)99,true)==null,"same-class unsafe-camp and invalid targets rejected");
            p.Profile.gold++;Check(!p.CommitClassSwitch(tx,true),"stale source mutation rejected");
            var practice=p.CreatePracticeCopy();Check(practice.PrepareClassSwitch(HeroClass.Arcanist,true)==null,"practice profile cannot switch real classes");
        }
        {
            var p=New(root,HeroClass.Vanguard);p.Profile.pendingFashionChest=true;p.Save();
            Directory.CreateDirectory(p.SaveFilePath+".tmp");Check(p.OpenDungeonChest()==null,"failed pending draw created");Directory.Delete(p.SaveFilePath+".tmp");
            var field=typeof(ProgressionService).GetField("pendingChestRoll",BindingFlags.Instance|BindingFlags.NonPublic);var pending=field.GetValue(p);int gold=p.Profile.gold,threads=p.Profile.fashionThreads;
            Switch(p,HeroClass.Summoner);Check(ReferenceEquals(pending,field.GetValue(p))&&p.Profile.pendingFashionChest&&!p.Profile.pendingChestReveal&&p.Profile.gold==gold&&p.Profile.fashionThreads==threads,"class switch neither rerolls nor grants pending chest");
            Check(p.OpenDungeonChest(1)==null,"pending choice identity survives class switch");
            Check(p.OpenDungeonChest()!=null&&p.Profile.pendingChestReveal,"original pending draw can commit after class switch");
            string reward=Json(p.Profile.lastChestReward);Switch(p,HeroClass.Arcanist);Check(Json(p.Profile.lastChestReward)==reward&&p.Profile.pendingChestReveal,"unseen durable receipt survives another class switch");
        }
        // A real legacy document migrates exactly its current class; other slots remain uninitialized.
        {
            string dir=Path.Combine(root,"legacy"),path=Path.Combine(dir,"emberfall-save.json");Directory.CreateDirectory(dir);
            File.WriteAllText(path,"{\"format\":\"emberfall-character\",\"version\":1,\"profile\":{\"version\":1,\"heroClass\":1,\"level\":12,\"xp\":7,\"skillRanks\":[1,1,1]}}");
            var p=new ProgressionService(dir);Check(p.Load(),"legacy v1 loads");Check(p.Profile.classStateRevision==1&&p.Profile.classStates.Count(x=>x.initialized)==1&&p.Profile.classStates[1].initialized,"only current legacy class initialized once");
            Switch(p,HeroClass.Vanguard);File.WriteAllText(Path.Combine(root,"old-reader-directory.txt"),dir);
        }
        return "PASS "+checks+" class-state transaction, 12 directed switches, thresholds, reload, shared gear/reward, cross-class sale and legacy assertions";
    }
}
