using System;
using System.IO;
using Emberfall;
public static class ProgressionAttentionTests
{
 static int n;static void Check(bool b,string m){n++;if(!b)throw new Exception(m);}
 public static string Run(string root)
 {
  n=0;var p=new ProgressionService(Path.Combine(root,"attention-"+Guid.NewGuid().ToString("N")));p.CreateNewSlot(HeroClass.Vanguard);
  Check(!ProgressionAttention.Evaluate(p,true).Equipment&&!ProgressionAttention.Evaluate(p,true).Skills,"empty initial actions no badges");
  ItemData high=new ItemData{id=Guid.NewGuid().ToString("N"),name="等级锁测试",slot=ItemSlot.Weapon,rarity=Rarity.Epic,level=2,attack=100,defense=10,health=100};p.Profile.inventory.Add(high);
  string before=p.Profile.weaponId;Check(!p.Equip(high.id)&&p.Profile.weaponId==before&&p.Profile.inventory.Contains(high),"level-gated equip service preserves original gear and candidate");
  Check(!ProgressionAttention.Evaluate(p,true).Equipment,"ineligible higher-score item excluded");
  p.GrantExperience(GameBalance.XpToNext(1));var state=ProgressionAttention.Evaluate(p,true);Check(state.Equipment&&state.HigherScoreItems.Contains(high.id),"level-up unlocks badge immediately");Check(!state.Skills,"level two adds no duplicate point or skill badge");
  p.GrantExperience(GameBalance.XpToNext(2)+GameBalance.XpToNext(3));Check(ProgressionAttention.Evaluate(p,true).Skills,"level four branch and available points enable skill badge");
  Check(p.LearnSkill(1)&&p.LearnSkill(3),"eligible branch skills can be learned");Check(!ProgressionAttention.Evaluate(p,true).Skills,"spent last point clears badge");
  Check(p.Equip(high.id),"exact required level equips");Check(!ProgressionAttention.Evaluate(p,true).HigherScoreItems.Contains(high.id),"equipped candidate no longer badge");
  Check(!ProgressionAttention.HigherScore(100.05f,100)&&ProgressionAttention.HigherScore(101,100),"score threshold avoids rounded-equal nags");
  Check(!ProgressionAttention.HigherScore(float.NaN,100),"invalid score excluded");
  var alt=new ItemData{id=Guid.NewGuid().ToString("N"),name="机制取舍",slot=ItemSlot.Weapon,rarity=Rarity.Epic,level=2,attack=150,defense=12,health=120,mechanic=EquipmentMechanic.ReturningBlade};p.Profile.inventory.Add(alt);
  state=ProgressionAttention.Evaluate(p,true);Check(state.HigherScoreItems.Contains(alt.id)&&!state.MechanismTradeoffs.Contains(alt.id),"independent attachment is preserved when equipment score increases");
  p.Profile.pendingLoot.Add(new ItemData{id=Guid.NewGuid().ToString("N"),name="待领",slot=ItemSlot.Relic,level=1});Check(ProgressionAttention.Evaluate(p,true).LootClaimable,"claimable loot alerts");
  while(p.Profile.inventory.Count<ProgressionService.InventoryCapacity)p.Profile.inventory.Add(new ItemData{id=Guid.NewGuid().ToString("N"),name="容量",slot=ItemSlot.Relic,level=1});
  Check(!ProgressionAttention.Evaluate(p,true).LootClaimable,"full inventory is not a currently claimable action");
  Check(ProgressionAttention.Evaluate(p,true).LootPending&&ProgressionAttention.Evaluate(p,true).Rewards,"pending reward badge remains while the bag is full");
  p.Profile.pendingFirstClearReward=true;Check(!ProgressionAttention.Evaluate(p,false).FirstClearClaimable&&ProgressionAttention.Evaluate(p,true).FirstClearClaimable,"camp-only reward availability");
  p.Profile.pendingFirstClearReward=false;p.Profile.pendingLoot.Clear();p.Profile.pendingChestReveal=false;p.Profile.pendingFashionChest=false;Check(!ProgressionAttention.Evaluate(p,true).Rewards,"handled rewards disappear");
  // Stable destination across new slot, load, repeated writes and deletion.
  string a=p.CurrentSlotId,pathA=p.SaveFilePath;p.Save();Check(p.CreateNewSlot(HeroClass.Ranger),"new character obtains one fresh ID");string b=p.CurrentSlotId,pathB=p.SaveFilePath;for(int x=0;x<5;x++)p.Save();Check(p.CurrentSlotId==b&&p.GetSaveSlots().Count==2,"repeated saves do not duplicate new slot");
  Check(p.LoadSlot(a),"reload original slot A");p.Profile.gold+=7;p.Save();Check(p.SaveFilePath==pathA&&File.Exists(pathB),"loaded A saves only A while B preserved");
  SaveDeletionRequest request;Check(p.PrepareSaveDeletion(a,out request)&&p.DeleteSaveSlot(request),"delete active A");p.Save();Check(!File.Exists(pathA)&&File.Exists(pathB),"later lifecycle-style save cannot resurrect A");
  return n+" attention, equipment-level and stable save destination assertions passed";
 }
}
