using System;using System.IO;using System.Linq;using System.Reflection;using Emberfall;using UnityEngine;
public static class SingleChestFaults{public static bool GrantOnce;}
public static class SingleChestTests{
 static int n;static void Check(bool v,string why){n++;if(!v)throw new Exception(why);}static string Json(object x)=>x==null?"null":JsonUtility.ToJson(x,true);
 static ProgressionService New(string root){var p=new ProgressionService(Path.Combine(root,Guid.NewGuid().ToString("N")));p.NewGame(HeroClass.Vanguard);return p;}
 static ChestReward Pending(ProgressionService p)=>(ChestReward)typeof(ProgressionService).GetField("pendingChestRoll",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(p);
 static void FrozenLegacy(ProgressionService p,int choice,int rarity){p.Profile.pendingFashionChest=true;p.Profile.pendingChestRulesRevision=1;p.Save();foreach(var pair in new[]{("pendingChestRoll",(object)new ChestReward{rulesRevision=1,id=Guid.NewGuid().ToString("N"),choice=choice,gold=80,rarityIndex=rarity}),("pendingChestRollPath",p.SaveFilePath),("pendingChestRollClears",(object)p.Profile.clearedRuns),("pendingChestRollTier",(object)p.Profile.pendingChestTier)})typeof(ProgressionService).GetField(pair.Item1,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(p,pair.Item2);}
 public static string Run(string root){Directory.CreateDirectory(root);
  var counts=new int[5];for(int roll=0;roll<100;roll++){var r=ProgressionService.RollFashionRarity(roll);counts[r.HasValue?(int)r.Value:4]++;}Check(counts.SequenceEqual(new[]{22,12,5,1,60}),"all100 quality buckets retain22/12/5/1/60");
  foreach(int quality in new[]{0,1,6,18,40})foreach(int ownedMask in new[]{0,1,2,3})foreach(int coin in new[]{0,1}){
   var p=New(root);var rarity=ProgressionService.RollFashionRarity(quality);if(rarity.HasValue)for(int slot=0;slot<2;slot++)if((ownedMask&(1<<slot))!=0)p.Profile.fashions.Add(new FashionData{id="fashion-"+slot+"-"+(int)rarity.Value,slot=(FashionSlot)slot,rarity=rarity.Value,name="test"});
   // Higher-quality ownership does not hide a lower-quality missing slot.
   if(rarity.HasValue&&rarity.Value!=Rarity.Legendary)p.Profile.fashions.Add(new FashionData{id="fashion-0-3",slot=FashionSlot.Wings,rarity=Rarity.Legendary,name="higher"});
   var draw=ProgressionService.BuildSingleChestRoll(p.Profile,quality,coin,0,false,Guid.NewGuid().ToString("N"));
   Check(draw.gold==60&&draw.materials==1&&draw.materialKind==RewardMaterialKind.StarAshFragment&&draw.choice==-1&&draw.rulesRevision==2,"explicit single-chest kinds and stable resources");
   if(rarity.HasValue){int slot=ownedMask==1?1:ownedMask==2?0:coin;Check(draw.slotIndex==slot&&draw.duplicate==(ownedMask==3),"same-quality missing slot wins; complete pair duplicates50/50");}else Check(draw.slotIndex==-1&&!draw.duplicate,"no-fashion branch still carries material kind");
  }
  foreach(int tier in new[]{1,5,10,20,40}){
   var p=New(root);p.Profile.clearedRuns=1;p.Save();Check(p.PrepareDungeonChest(tier),"prepare new qualification");int baseMaterials=p.Profile.mechanicMaterials,gold=p.Profile.gold,threads=p.Profile.fashionThreads;
   Check(baseMaterials==TierRewardRules.ClearMaterials(tier),"base clear material rules unchanged");
   Check(p.OpenDungeonChest()!=null,"one direct open");var r=p.LastChestReward;
   Check(r.rulesRevision==2&&r.rewardKind==ChestRewardKind.SingleChest&&!r.legacyGoldProtection&&r.baseGold>=TierRewardRules.ChestGoldMinimum(tier)&&r.baseGold<=TierRewardRules.ChestGoldMinimum(tier)+40,"new box is1xG not supply1.5x");
   Check(p.Profile.mechanicMaterials==baseMaterials+1&&r.materialsDelta==1&&p.Profile.gold==gold+r.goldDelta&&p.Profile.fashionThreads==threads+r.threadsDelta,"committed ledger exactly matches actual resource deltas");
   Check(r.gold==r.baseGold+r.duplicateGold&&r.threadsDelta==r.baseThreads+r.duplicateThreads,"planned ledger is separately recomputable");
   string saved=Json(r);Check(p.OpenDungeonChest()==null&&Json(p.LastChestReward)==saved,"duplicate opening cannot consume or reroll");Check(p.AcknowledgeChestReward()&&Json(p.LastChestReward)==saved,"ack only changes presentation");
  }
  {
   var p=New(root);p.Profile.pendingFashionChest=true;p.Profile.pendingChestRulesRevision=0;p.Save();Check(p.OpenDungeonChest()!=null,"undrawn legacy qualification migrates directly");var r=p.LastChestReward;Check(r.rulesRevision==2&&r.legacyGoldProtection&&r.baseGold>=90&&r.baseGold<=150&&r.materialsDelta==1,"one legacy qualification protects1.5x base gold and gains single-chest material");
   p.AcknowledgeChestReward();p.PrepareDungeonChest();p.OpenDungeonChest();Check(!p.LastChestReward.legacyGoldProtection&&p.LastChestReward.baseGold<=100,"legacy protection never leaks into next box");
  }
  foreach(int choice in new[]{0,1,2}){
   var p=New(root);FrozenLegacy(p,choice,choice==2?-1:0);var roll=Pending(p);Check(p.OpenDungeonChest()!=null,"legacy frozen choice has one continue operation");var r=p.LastChestReward;
   Check(r.id==roll.id&&r.rulesRevision==1&&r.choice==choice&&r.gold==80&&r.materialsDelta==0&&r.rarityIndex==roll.rarityIndex,"frozen legacy result is not upgraded or redrawn");
   if(choice<2)Check(r.slotIndex==(choice==0?(int)FashionSlot.Weapon:(int)FashionSlot.Wings),"old0/1 slot meaning retained");
  }
  {
   var p=New(root);p.PrepareDungeonChest();string live=Json(p.Profile);Directory.CreateDirectory(p.SaveFilePath+".tmp");Check(p.OpenDungeonChest()==null&&Json(p.Profile)==live,"failed freeze saves no reward and changes no profile fields");Directory.Delete(p.SaveFilePath+".tmp");var roll=Pending(p);string id=roll.id,source=p.CurrentSlotId;
   Check(p.SaveAsNewSlot(),"save-as carries exact pending draw");string copy=p.CurrentSlotId;var reloaded=new ProgressionService(p.SaveDirectory);Check(reloaded.LoadSlot(copy)&&reloaded.Profile.pendingChestDraw.id==id,"fresh service reload retains frozen result");
   Check(reloaded.OpenDungeonChest()!=null&&reloaded.LastChestReward.id==id,"copy grants original frozen result");
   Check(p.LoadSlot(source)&&p.OpenDungeonChest()!=null&&p.LastChestReward.id==id,"source also retains own original draw");
  }
  {
   var p=New(root);p.Profile.gold=999999999;p.Profile.fashionThreads=999999;p.Profile.mechanicMaterials=999999;p.Save();p.PrepareDungeonChest();Check(p.OpenDungeonChest()==null&&p.Profile.pendingFashionChest,"all-resource cap rejects zero guaranteed gain before draw");
   p.Profile.gold--;p.Profile.fashionThreads--;p.Profile.mechanicMaterials--;p.Save();Check(p.OpenDungeonChest()!=null,"partial caps allow real gains");Check(p.LastChestReward.goldDelta==1&&p.LastChestReward.threadsDelta==1&&p.LastChestReward.materialsDelta==1,"actual cap-limited ledger never overstates gains");
  }
  {
   var p=New(root);p.PrepareDungeonChest();p.OpenDungeonChest();p.AcknowledgeChestReward();p.Profile.fashionThreads=18;p.Profile.mechanicMaterials=0;p.Save();var quote=p.QuoteThreadMaterialExchange();string chest=Json(p.LastChestReward);Directory.CreateDirectory(p.SaveFilePath+".tmp");Check(!p.ExchangeThreadsForMaterial(quote,true)&&p.Profile.fashionThreads==18&&p.Profile.mechanicMaterials==0,"failed exchange consumes neither currency nor receipt");Directory.Delete(p.SaveFilePath+".tmp");
   Check(p.ExchangeThreadsForMaterial(quote,true)&&p.Profile.fashionThreads==12&&p.Profile.mechanicMaterials==1,"manual6 threads grants exactly1 fragment");Check(p.ExchangeThreadsForMaterial(quote,true)&&p.Profile.fashionThreads==12&&p.Profile.mechanicMaterials==1,"same quote is idempotent");
   Check(Json(p.LastChestReward)==chest&&p.Profile.lastThreadMaterialReceipt.threadsDelta==-6,"exchange receipt does not overwrite chest receipt");var q2=p.QuoteThreadMaterialExchange();Check(p.ExchangeThreadsForMaterial(q2,true)&&!p.ExchangeThreadsForMaterial(quote,true),"older sequence cannot replay after another exchange");
   p.Profile.mechanicMaterials=999999;p.Save();Check(!p.ExchangeThreadsForMaterial(p.QuoteThreadMaterialExchange(),true)&&p.Profile.fashionThreads==6,"material cap forbids zero-yield exchange");
   var reload=new ProgressionService(p.SaveDirectory);Check(reload.Load()&&reload.Profile.threadMaterialSequence==2,"exchange sequence is durable across reload");
  }
  {
   var p=New(root);p.PrepareDungeonChest();int gold=p.Profile.gold,materials=p.Profile.mechanicMaterials,threads=p.Profile.fashionThreads;
   SingleChestFaults.GrantOnce=true;Check(p.OpenDungeonChest()==null&&p.Profile.pendingFashionChest&&!p.Profile.pendingChestReveal,"injected grant-write failure retains frozen eligibility");
   string id=p.Profile.pendingChestDraw.id;Check(p.Profile.gold==gold&&p.Profile.mechanicMaterials==materials&&p.Profile.fashionThreads==threads,"durable freeze grants no resources");
   var restart=new ProgressionService(p.SaveDirectory);Check(restart.Load()&&restart.Profile.pendingChestDraw.id==id,"grant-write failure survives process replacement");
   Check(restart.OpenDungeonChest()!=null&&restart.LastChestReward.id==id&&restart.LastChestReward.materialsDelta==1,"restart retries original frozen draw exactly once");
  }
  {
   var p=New(root);
   for(int clear=1;clear<=3;clear++){Check(p.TryCompleteDungeonRun(Guid.NewGuid().ToString("N"),1,0,0),"real new clear admits one qualification");Check(p.Profile.lastDungeonRewardDetails.Materials==3,"base clear ledger remains3");Check(p.OpenDungeonChest()!=null&&p.LastChestReward.materialsDelta==1&&p.AcknowledgeChestReward(),"box ledger adds1 separately");}
   Check(p.Profile.mechanicMaterials==12,"three low-tier clears plus three boxes now fund12, previously four base clears");
  }
  return "PASS "+n+" single chest probability, gap, transaction, migration, cap, snapshot and exchange assertions";
 }
}
