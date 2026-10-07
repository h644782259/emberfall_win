// Second half of the integrated journey: actual progression services, no profile injections.
using System;
using System.IO;
using System.Linq;
using UnityEngine;
namespace Emberfall
{
 public partial class GameSession
 {
  static ItemData JourneyBuildItem(ProgressionService p,string id)
  {
   var item=p.Profile.inventory.SingleOrDefault(x=>x.id==id);
   JourneyCheck(item!=null,"journey core remains a unique inventory identity");return item;
  }
  static void JourneyBuildIdentity(ProgressionService p,string id,EquipmentMechanic mechanic,ItemSlot slot,Rarity rarity)
  {
   var item=JourneyBuildItem(p,id);
   JourneyCheck(item.mechanic==mechanic&&item.slot==slot&&item.rarity==rarity&&item.locked,"core identity, rarity, slot and lock survive build transactions");
   JourneyCheck(p.Profile.firstClearRewardClaimed&&!p.Profile.pendingFirstClearReward,"shared first-core claim remains consumed");
   JourneyCheck(p.Profile.discoveredMechanics.Contains(mechanic),"core discovery survives build transactions");
   JourneyBudget(p,"build transaction shared point budget");
  }
  static void JourneyBuild(GameSession s,string coreId)
  {
   var p=s.Progression;var core=JourneyBuildItem(p,coreId);
   var mechanic=core.mechanic;var slot=core.slot;var rarity=core.rarity;int fromLevel=core.level;
   int gold=p.Profile.gold,materials=p.Profile.mechanicMaterials,inventory=p.Profile.inventory.Count;
   JourneyAtomicFailure(p,()=>p.Equip(coreId),"first core equip atomic failure");
   JourneyCheck(p.Equip(coreId),"equip actually claimed first core");
   JourneyCheck(p.Equipped(slot).id==coreId&&p.Profile.gold==gold&&p.Profile.mechanicMaterials==materials,"equip changes no currencies");
   int rank=p.SlotUpgradeRank(slot),cost=p.UpgradeCost(p.Equipped(slot));
   JourneyCheck(cost==(slot==ItemSlot.Weapon?60:slot==ItemSlot.Armor?50:45)*(rank+1),"slot upgrade uses independent slot price");
   JourneyAtomicFailure(p,()=>p.Upgrade(coreId),"slot upgrade atomic failure");
   JourneyCheck(p.Upgrade(coreId),"upgrade claimed core slot once");
   JourneyCheck(p.Profile.gold==gold-cost&&p.Profile.mechanicMaterials==materials&&p.SlotUpgradeRank(slot)==rank+1&&p.Equipped(slot).upgradeLevel==rank+1,"upgrade charges once and applies permanent slot rank");
   JourneyBuildIdentity(p,coreId,mechanic,slot,rarity);JourneyStage(s,"first-core equipped and slot upgraded");p=s.Progression;

   // Real zero-kill chapter repetitions provide XP; no level, gold or receipt fabrication.
   int repeats=0;
   while(p.Profile.level<11||p.Profile.gold<ProgressionService.ReforgeGoldCost(fromLevel,10))
   {
    JourneyCheck(repeats++<40,"bounded real chapter progression earns skill rank2 and a legal equipment generation band");
    int beforeGold=p.Profile.gold;bool needsGold=p.Profile.gold<ProgressionService.ReforgeGoldCost(fromLevel,10);JourneyChapter(s,needsGold?ChapterNode.Redrock:ChapterNode.ForestCourt);p=s.Progression;
    JourneyCheck(needsGold?p.Profile.gold>=beforeGold:p.Profile.gold==beforeGold,"chapter earnings use actual kill rewards; zero-kill Forest never injects gold");
    JourneyBuildIdentity(p,coreId,mechanic,slot,rarity);
   }
   JourneyCheck(p.Profile.skillRanks[0]==1&&p.LearnSkill(0),"learn starter skill rank2 through actual level and point gates");
   JourneyCheck(p.Profile.skillRanks[0]==2,"actual starter rank2 investment exists before respec");
   JourneyCheck(p.SaveBuildPreset(0,true),"save invested build before reforge");
   int[] investedSkills=(int[])p.Profile.skillRanks.Clone();int[] investedMastery=(int[])p.Profile.masteryRanks.Clone();
   int investedPoints=p.Profile.skillPoints;string hotbar=string.Join(",",p.Profile.equippedSkills);
   var quote=p.QuoteReforgeChoice(coreId,ReforgeTargetKind.Affordable);
   JourneyCheck(quote!=null&&quote.FromLevel==fromLevel&&quote.TargetLevel>fromLevel&&quote.TargetLevel<p.Profile.level,"earned gold offers a strictly partial core reforge");
   gold=p.Profile.gold;materials=p.Profile.mechanicMaterials;
   JourneyCheck(quote.GoldCost<=gold&&ProgressionService.ReforgeGoldCost(fromLevel,quote.TargetLevel+1)>gold,"affordable quote is maximal within actual earned gold");
   string before=JsonUtility.ToJson(p.Profile,true),disk=File.ReadAllText(p.SaveFilePath),backup=File.ReadAllText(p.SaveFilePath+".bak");
   var preview=p.PreviewReforge(quote);
   JourneyCheck(preview!=null&&preview.id==coreId&&preview.level==quote.TargetLevel&&preview.upgradeLevel==rank+1,"preview retains identity and inherited slot upgrade");
   JourneyCheck(JsonUtility.ToJson(p.Profile,true)==before&&File.ReadAllText(p.SaveFilePath)==disk&&File.ReadAllText(p.SaveFilePath+".bak")==backup,"reforge preview leaves complete live and disk state unchanged");
   JourneyAtomicFailure(p,()=>p.ReforgeMechanic(quote,true),"partial reforge atomic failure");
   JourneyCheck(p.ReforgeMechanic(quote,true),"retry original partial quote after storage recovers");
   JourneyCheck(p.Profile.gold==gold-quote.GoldCost&&p.Profile.mechanicMaterials==materials,"partial reforge charges exact gold once and no fragments");
   JourneyCheck(p.Equipped(slot).id==coreId&&p.Equipped(slot).level==quote.TargetLevel&&p.SlotUpgradeRank(slot)==rank+1&&p.Equipped(slot).upgradeLevel==rank+1,"partial reforge retains equipped identity and permanent upgrade");
   before=JsonUtility.ToJson(p.Profile,true);disk=File.ReadAllText(p.SaveFilePath);backup=File.ReadAllText(p.SaveFilePath+".bak");
   JourneyCheck(!p.ReforgeMechanic(quote,true)&&JsonUtility.ToJson(p.Profile,true)==before&&File.ReadAllText(p.SaveFilePath)==disk&&File.ReadAllText(p.SaveFilePath+".bak")==backup,"old quote cannot charge or rotate saved files again");
   JourneyBuildIdentity(p,coreId,mechanic,slot,rarity);JourneyStage(s,"partial gold reforge committed");p=s.Progression;

   gold=p.Profile.gold;int refund=p.RefundableBuildPoints,points=p.Profile.skillPoints;
   JourneyCheck(refund>0,"real skill investment is refundable");
   JourneyAtomicFailure(p,()=>p.ResetBuild(true),"joint respec atomic failure");
   JourneyCheck(p.ResetBuild(true),"retry real joint respec");
   JourneyCheck(p.Profile.skillPoints==points+refund&&p.Profile.skillRanks.Select((v,i)=>v==Math.Min(1,investedSkills[i])).All(x=>x)&&p.Profile.masteryRanks.All(v=>v==0),"joint respec refunds exactly advanced ranks and retains learned first ranks");
   JourneyCheck(string.Join(",",p.Profile.equippedSkills)==hotbar&&p.Equipped(slot).id==coreId,"respec retains hotbar and equipped identity");
   JourneyBudget(p,"after real joint respec");
   JourneyAtomicFailure(p,()=>p.ApplyBuildPreset(0,true),"preset apply atomic failure");
   JourneyCheck(p.ApplyBuildPreset(0,true),"apply pre-reforge invested preset after respec");
   JourneyCheck(p.Profile.skillRanks.SequenceEqual(investedSkills)&&p.Profile.masteryRanks.SequenceEqual(investedMastery)&&p.Profile.skillPoints==investedPoints,"preset restores exact investment without minting points");
   JourneyCheck(p.Equipped(slot).id==coreId&&p.Equipped(slot).level==quote.TargetLevel&&p.Equipped(slot).upgradeLevel==rank+1&&p.SlotUpgradeRank(slot)==rank+1,"old preset references live item rather than rolling back reforge or slot upgrade");
   JourneyCheck(p.Profile.gold==gold&&p.Profile.mechanicMaterials==materials&&p.Profile.inventory.Count==inventory,"respec and preset neither charge currency nor duplicate equipment");
   JourneyBuildIdentity(p,coreId,mechanic,slot,rarity);JourneyStage(s,"respec and preset reapplied");p=s.Progression;

   string saved=JsonUtility.ToJson(p.Profile,true),slotId=p.CurrentSlotId;var loaded=new ProgressionService(p.SaveDirectory);
   JourneyCheck(loaded.LoadSlot(slotId),"fresh progression service loads same character slot");
   JourneyCheck(JsonUtility.ToJson(loaded.Profile,true)==saved,"fresh reload preserves full normalized profile after entire journey");
   s.Progression=loaded;p=loaded;JourneyBuildIdentity(p,coreId,mechanic,slot,rarity);
   JourneyCheck(!p.ClaimFirstClearReward(mechanic)&&JsonUtility.ToJson(p.Profile,true)==saved,"fresh reload cannot claim shared first core twice");
   JourneyCheck(p.Equipped(slot).id==coreId&&p.Equipped(slot).level==quote.TargetLevel&&p.SlotUpgradeRank(slot)==rank+1&&p.Profile.gold==gold&&p.Profile.mechanicMaterials==materials,"fresh reload preserves grown core and exact currencies");
   JourneyStage(s,"fresh service reload verifies complete build journey");
  }
 }
}
