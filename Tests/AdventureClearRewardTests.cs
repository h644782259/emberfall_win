using System;
using System.IO;
using System.Linq;
using Emberfall;
public static class AdventureClearRewardTests
{
 static int n;static void C(bool b,string s){n++;if(!b)throw new Exception(s);}
 static ProgressionService Fresh(string dir){var p=new ProgressionService(Path.Combine(dir,Guid.NewGuid().ToString("N")));C(p.CreateNewSlot(HeroClass.Arcanist),"fresh owner");return p;}
 static bool Grant(ProgressionService p,string receipt,int mode,int tier){
  bool saved=mode==-1?p.TryCompleteDungeonRun(receipt,tier,AdventureRewardRules.Gold(mode,tier,false),AdventureRewardRules.Experience(mode,tier),true):p.TryGrantModeReward(receipt,AdventureRewardRules.Gold(mode,tier,false),AdventureRewardRules.Experience(mode,tier),AdventureRewardRules.Materials(mode,tier),tier,mode);
  if(!saved)return false;if(p.Profile.pendingFashionChest&&p.OpenDungeonChest()==null)return false;
  return !p.Profile.pendingChestReveal||p.AcknowledgeChestReward();
 }

 public static string Run(string dir)
 {
  foreach(int mode in new[]{-1,0,1,2,3})foreach(int tier in new[]{1,5,10,20,40,100})
  {
   int upgraded=0;for(int roll=0;roll<100;roll++){var rarity=AdventureRewardRules.EquipmentRarity(mode,tier,roll);C(rarity>=AdventureRewardRules.MinimumRarity(mode),"never below promised rarity");if(rarity>AdventureRewardRules.MinimumRarity(mode))upgraded++;}
   C(upgraded==AdventureRewardRules.LegendaryChance(tier),"every probability bucket matches advertised percent");
   var p=Fresh(dir);string receipt=Guid.NewGuid().ToString("N");int before=p.Profile.inventory.Count,gold=p.Profile.gold,materials=p.Profile.mechanicMaterials;
   C(Grant(p,receipt,mode,tier),"typed clear settles atomically");var items=p.Profile.inventory.Where(i=>i.id.StartsWith(receipt+"-clear-")).ToArray();
   C(items.Length==AdventureRewardRules.EquipmentCount(mode,tier)&&p.Profile.inventory.Count==before+items.Length,"all guaranteed gear directly owned");
   for(int i=0;i<items.Length;i++)C(items[i].slot==AdventureRewardRules.EquipmentSlot(mode,i)&&items[i].rarity>=AdventureRewardRules.MinimumRarity(mode)&&items[i].level==ProgressionService.EquipmentGenerationLevel(p.Profile.level),"real slot rarity and generation level match config");
   C(p.Profile.gold==gold+AdventureRewardRules.Gold(mode,tier,false)+p.LastChestReward.goldDelta&&p.Profile.mechanicMaterials==materials+AdventureRewardRules.Materials(mode,tier),"actual currencies share preview rules");
   string bytes=File.ReadAllText(p.SaveFilePath);C(Grant(p,receipt,mode,tier)&&File.ReadAllText(p.SaveFilePath)==bytes,"receipt callback replay does not pay twice");
   var q=new ProgressionService(p.SaveDirectory);C(q.LoadSlot(p.CurrentSlotId)&&Grant(q,receipt,mode,tier)&&q.Profile.inventory.Count==before+items.Length,"load/restart receipt remains idempotent");
  }
  var full=Fresh(dir);while(full.Profile.inventory.Count<ProgressionService.InventoryCapacity)full.Profile.inventory.Add(new ItemData{id=Guid.NewGuid().ToString("N"),name="full",level=1,attack=1});full.Save();string r=Guid.NewGuid().ToString("N"),disk=File.ReadAllText(full.SaveFilePath);
  Directory.CreateDirectory(full.SaveFilePath+".tmp");C(!Grant(full,r,3,10)&&full.Profile.inventory.Count==256&&File.ReadAllText(full.SaveFilePath)==disk,"failed save publishes no gear or currency");Directory.Delete(full.SaveFilePath+".tmp");
  C(Grant(full,r,3,10)&&full.Profile.inventory.Count==259,"soft-full bag preserves both rewards, no sale or hidden claim");
  while(full.Profile.inventory.Count<ProgressionService.MaximumSavedEquipment)full.Profile.inventory.Add(new ItemData{id=Guid.NewGuid().ToString("N"),name="limit",level=1,attack=1});full.Save();string pending=Guid.NewGuid().ToString("N");disk=File.ReadAllText(full.SaveFilePath);
  C(!Grant(full,pending,3,10)&&full.Profile.pendingFashionChest&&!full.Profile.inventory.Any(i=>i.id.StartsWith(pending)),"hard ceiling refuses entire receipt, retains retry eligibility");
  full.Profile.inventory.RemoveRange(full.Profile.inventory.Count-3,3);full.Save();C(Grant(full,pending,3,10)&&full.Profile.inventory.Count==ProgressionService.MaximumSavedEquipment,"same reward retries after explicit room made");
  var reserve=Fresh(dir);while(reserve.Profile.inventory.Count<ProgressionService.MaximumRetainedEquipment)reserve.Profile.inventory.Add(new ItemData{id=Guid.NewGuid().ToString("N"),name="reserve",level=1,attack=1});reserve.Save();C(System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(reserve.SaveFilePath))["version"].GetValue<int>()==7,"independent progression uses format7");string reserveDisk=File.ReadAllText(reserve.SaveFilePath),reserveBackup=File.ReadAllText(reserve.SaveFilePath+".bak"),reserveReceipt=Guid.NewGuid().ToString("N");Directory.CreateDirectory(reserve.SaveFilePath+".tmp");C(!Grant(reserve,reserveReceipt,3,1)&&File.ReadAllText(reserve.SaveFilePath)==reserveDisk&&File.ReadAllText(reserve.SaveFilePath+".bak")==reserveBackup&&reserve.Profile.inventory.Count==4096,"failed format5-to6 write retains primary, backup and ownership");Directory.Delete(reserve.SaveFilePath+".tmp");C(Grant(reserve,reserveReceipt,3,1)&&reserve.Profile.inventory.Count==4098,"reserved slots protect clear rewards at pickup safety ceiling");C(System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(reserve.SaveFilePath))["version"].GetValue<int>()==7&&new FileInfo(reserve.SaveFilePath).Length<=4*1024*1024,"reserved rewards use format7 within byte safety bound");var reload=new ProgressionService(reserve.SaveDirectory);C(reload.LoadSlot(reserve.CurrentSlotId)&&reload.Profile.inventory.Count==4098&&!reload.CanEnterDungeon,"reserved overflow survives load and blocks another run until explicit cleanup");
  C(reload.Sell(reload.Profile.inventory[reload.Profile.inventory.Count-1].id)&&reload.Sell(reload.Profile.inventory[reload.Profile.inventory.Count-1].id)&&System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(reload.SaveFilePath))["version"].GetValue<int>()==7,"explicit cleanup retains format7 progression without losing remaining items");
  var bad=Fresh(dir);C(!bad.TryGrantModeReward(Guid.NewGuid().ToString("N"),1,1,1,1,4)&&!bad.TryGrantModeReward(Guid.NewGuid().ToString("N"),1,1,1,0,0),"invalid typed mode/tier rejected");
  return "PASS "+n+" authoritative adventure loot probabilities, owned gear, receipt replay, restart, write failure and capacity assertions";
 }
}
