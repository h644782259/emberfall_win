using System;using System.IO;using System.Linq;using Emberfall;
public static class LegendaryEquipmentPityTests
{
 static int checks;static void C(bool ok,string reason){checks++;if(!ok)throw new Exception(reason);}
 public static string Run(string dir){
 var p=new ProgressionService(Path.Combine(dir,"single"));C(p.CreateNewSlot(HeroClass.Arcanist),"create");
 for(int mode=-1;mode<=3;mode++)foreach(int tier in new[]{1,5,10})foreach(bool gemSource in new[]{false,true}){
 p.Profile.pendingChestRulesRevision=3;p.Profile.pendingChestMode=mode;p.Profile.pendingChestTier=tier;p.Profile.pendingChestGemSource=gemSource;
 int legends=0,epics=0,gems=0;for(int roll=0;roll<100;roll++){
 var r=ProgressionService.BuildSingleChestRoll(p.Profile,roll,roll%2,roll%41,false,Guid.NewGuid().ToString("N"));
 C(r.rulesRevision==3&&r.gold==0&&r.materials==0&&r.baseThreads==0,"one reward without bonus wallet items");
 C(r.primaryKind>=1&&r.primaryKind<=5&&r.primaryCount>=1,"exactly one typed item or stack");
 if(r.primaryKind==1){C(r.primaryCount==1&&r.primaryRarity>=Rarity.Epic,"gear quality floor");if(r.primaryRarity==Rarity.Legendary)legends++;else epics++;}
 if(r.primaryKind==4){gems++;C(gemSource&&r.gemRarity>=Rarity.Epic,"gems exclusive and epic minimum");}
 if(r.primaryKind==3||r.primaryKind==5)C(r.primaryCount>=ProgressionService.ChestStackMinimum(r.primaryKind==5,tier)&&r.primaryCount<=ProgressionService.ChestStackMaximum(r.primaryKind==5,tier),"material stack matches preview");
 C(TierRewardRules.DropRarity(true,tier,roll)<Rarity.Epic,"ordinary dungeon boss below chest gear");
 }C(legends==4&&epics==36&&gems==(gemSource?35:0),"absolute four percent legendary and diverse pool");
 }
 p.Profile.pendingChestGemSource=false;p.Profile.legendaryEquipmentMisses=29;p.Save();
 C(p.TryGrantModeReward(Guid.NewGuid().ToString("N"),100,100,3,1,0),"qualify");
 int gold=p.Profile.gold,materials=p.Profile.mechanicMaterials,threads=p.Profile.fashionThreads,stones=p.Profile.refinementStones,before=p.Profile.inventory.Count;
 Directory.CreateDirectory(p.SaveFilePath+".tmp");C(p.OpenChosenDungeonChest(2)==null,"failed freeze");C(p.Profile.inventory.Count==before&&p.Profile.legendaryEquipmentMisses==29,"failed write grants nothing");Directory.Delete(p.SaveFilePath+".tmp");p.Save();
 var reload=new ProgressionService(p.SaveDirectory);C(reload.LoadSlot(p.CurrentSlotId)&&reload.SelectedRewardChest==2,"frozen single draw survives restart");C(reload.OpenChosenDungeonChest(1)==null,"cannot switch choice");C(reload.OpenChosenDungeonChest(2)!=null,"retry frozen grant");
 var receipt=reload.LastChestReward;C(receipt.rulesRevision==3&&receipt.primaryKind==1&&receipt.equipmentIds.Length==1&&reload.Profile.inventory.Count==before+1,"exactly one equipped item granted");C(reload.Profile.inventory.Last().rarity==Rarity.Legendary&&reload.Profile.legendaryEquipmentMisses==0,"thirtieth box guarantees legendary");
 C(reload.Profile.gold==gold&&reload.Profile.mechanicMaterials==materials&&reload.Profile.fashionThreads==threads&&reload.Profile.refinementStones==stones,"equipment box has no extra materials or gold");
 C(reload.OpenChosenDungeonChest(2)==null&&reload.Profile.inventory.Count==before+1,"no duplicate grant");
 var again=new ProgressionService(p.SaveDirectory);C(again.LoadSlot(p.CurrentSlotId)&&again.Profile.pendingChestReveal&&again.LastChestReward.primaryKind==1,"single receipt survives reload");C(again.AcknowledgeChestReward(),"ack");
 // Exercise every typed result through the production grant and save validator.
 foreach(bool source in new[]{false,true})foreach(int roll in new[]{0,20,45,72,90}){
 C(again.TryGrantModeReward(Guid.NewGuid().ToString("N"),100,100,3,1,0),"qualify pool fixture");again.Profile.pendingChestGemSource=source;
 var frozen=ProgressionService.BuildSingleChestRoll(again.Profile,roll,0,10,false,Guid.NewGuid().ToString("N"));frozen.selectedChest=0;again.Profile.pendingChestDraw=frozen;again.Save();
 var test=new ProgressionService(again.SaveDirectory);C(test.LoadSlot(again.CurrentSlotId),"load each typed frozen draw");int g=test.Profile.gold,t=test.Profile.fashionThreads,m=test.Profile.mechanicMaterials,s=test.Profile.refinementStones,eq=test.Profile.inventory.Count,f=test.Profile.fashions.Count,a=test.Profile.attachments.Count;
 C(test.OpenChosenDungeonChest(0)!=null,"grant each pool type");var r=test.LastChestReward;
 int changed=(test.Profile.gold!=g?1:0)+(test.Profile.fashionThreads!=t?1:0)+(test.Profile.mechanicMaterials!=m?1:0)+(test.Profile.refinementStones!=s?1:0)+(test.Profile.inventory.Count!=eq?1:0)+(test.Profile.fashions.Count!=f?1:0)+(test.Profile.attachments.Count!=a||r.primaryKind==4?1:0);
 C(changed==1&&test.Profile.gold==g,"exactly one inventory/resource kind changes");
 C(test.AcknowledgeChestReward(),"ack pool type");again=test;
 }
 return "PASS "+checks+" single-item reward pool and persistent pity assertions";
 }
}
