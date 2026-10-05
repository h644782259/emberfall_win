using System;using System.IO;using System.Linq;using System.Reflection;using System.Text.Json.Nodes;using Emberfall;using UnityEngine;
public static class RewardPolishFaults{public static bool GrantOnce;}
public static class RewardPolishServiceTests
{
 static int n;static void C(bool ok,string why){n++;if(!ok)throw new Exception(why);}static string Json(object o)=>JsonUtility.ToJson(o,true);
 static ProgressionService New(string root){var p=new ProgressionService(Path.Combine(root,Guid.NewGuid().ToString("N")));p.NewGame(HeroClass.Vanguard);return p;}
 sealed class Roll:Random{public override int Next(int max)=>0;}
 static ItemData Copy(ItemData item)=>JsonUtility.FromJson<ItemData>(Json(item));
 public static string Run(string root)
 {
  foreach(string broken in new[]{"future-pending","future-draw","slot","id","materials","malformed","envelope"})
  {
   var p=New(root);p.PrepareDungeonChest();typeof(ProgressionService).GetField("random",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(p,new Roll());
   RewardPolishFaults.GrantOnce=true;C(p.OpenDungeonChest()==null,"real grant failure leaves durable frozen primary");
   string path=p.SaveFilePath,backup=File.ReadAllText(path+".bak");var original=JsonNode.Parse(File.ReadAllText(path)).AsObject();var profile=original["profile"].AsObject();var draw=profile["pendingChestDraw"].AsObject();
   C(draw["id"].GetValue<string>()==p.Profile.pendingChestDraw.id&&JsonNode.Parse(backup)["profile"]["pendingChestDraw"]==null&&JsonNode.Parse(backup)["profile"]["pendingFashionChest"].GetValue<bool>(),"actual backup contains prior undrawn qualification");
   if(broken=="future-pending")profile["pendingChestRulesRevision"]=99;if(broken=="future-draw")draw["rulesRevision"]=99;if(broken=="slot")draw["slotIndex"]=99;if(broken=="id")draw["id"]="";if(broken=="materials")draw["materials"]=2;
   if(broken=="envelope")original["format"]="broken";string corrupt=original.ToJsonString();if(broken=="malformed")corrupt=corrupt.Replace("\"slotIndex\":0","\"slotIndex\":\"broken\"");File.WriteAllText(path,corrupt);
   var fresh=new ProgressionService(p.SaveDirectory);string live=Json(fresh.Profile);C(!fresh.Load()&&Json(fresh.Profile)==live,"nested frozen corruption must not recover undrawn backup: "+broken);
   fresh.Save();C(File.ReadAllText(path)==corrupt&&File.ReadAllText(path+".bak")==backup,"failed load/save preserves both protected documents: "+broken);
   int gold=p.Profile.gold,threads=p.Profile.fashionThreads,materials=p.Profile.mechanicMaterials;C(p.OpenDungeonChest()==null&&p.Profile.gold==gold&&p.Profile.fashionThreads==threads&&p.Profile.mechanicMaterials==materials,"live retry cannot overwrite malformed frozen primary: "+broken);
  }
  {
   var p=New(root);p.PrepareDungeonChest();RewardPolishFaults.GrantOnce=true;p.OpenDungeonChest();string id=p.Profile.pendingChestDraw.id;var fresh=new ProgressionService(p.SaveDirectory);C(fresh.Load()&&fresh.OpenDungeonChest()!=null&&fresh.LastChestReward.id==id,"valid durable frozen record still retries same draw");
  }
  {
   var p=New(root);p.Profile.level=45;p.Profile.gold=100000;p.Save();var current=p.Equipped(ItemSlot.Relic);for(int j=0;j<4;j++)p.Upgrade(current.id);
   var gain=Copy(current);gain.id=Guid.NewGuid().ToString("N");gain.baseAttack++;gain.upgradeAnchorAttack=gain.baseAttack;gain.upgradeAnchorLevel=0;gain.upgradeLevel=0;gain.attack=gain.baseAttack;gain.defense=gain.baseDefense;gain.health=gain.baseHealth;
   C(p.IsStrictEquipmentUpgrade(gain),"upgrade comparison applies permanent slot rank to candidate");var trade=Copy(gain);trade.baseHealth=current.baseHealth-1;trade.upgradeAnchorHealth=trade.baseHealth;trade.health=trade.baseHealth;C(!p.IsStrictEquipmentUpgrade(trade),"higher weighted score with health loss is not lossless");
   var locked=Copy(gain);locked.level=46;C(!p.IsStrictEquipmentUpgrade(locked),"level-locked candidate is not eligible upgrade");C(!p.IsStrictEquipmentUpgrade(Copy(current)),"equal stats do not celebrate");
   Directory.CreateDirectory(p.SaveFilePath+".tmp");C(!p.CollectLoot(gain)&&p.LastRewardMoment==null,"failed acquisition publishes no visual moment");Directory.Delete(p.SaveFilePath+".tmp");
   C(p.CollectLoot(gain)&&p.LastRewardMoment.Kind==RewardMomentKind.StrictUpgrade,"real committed lossless acquisition produces visual snapshot");long seq=p.LastRewardMoment.Sequence;C(!p.CollectLoot(gain)&&p.LastRewardMoment.Sequence==seq,"repeat acquisition cannot replay moment");p.Equip(gain.id);p.Equip(current.id);C(p.LastRewardMoment.Sequence==seq,"free equipment switching does not replay moment");
   var mail=Copy(gain);mail.id=Guid.NewGuid().ToString("N");mail.baseAttack+=5;p.Profile.pendingLoot.Add(mail);p.Save();C(p.ClaimPendingLoot(mail.id)&&p.LastRewardMoment.Sequence==seq,"mailbox transfer never replays acquisition celebration");
  }
  {
   var p=New(root);p.Profile.level=45;p.Profile.bestFloor=5;p.Profile.highestAdventureTier=5;p.Profile.clearedRuns=1;p.Profile.pendingFirstClearReward=true;p.Profile.mechanicMaterials=100;p.Save();
   var mechanic=Enum.GetValues(typeof(EquipmentMechanic)).Cast<EquipmentMechanic>().First(m=>m!=EquipmentMechanic.None&&BuildCatalog.MechanicClass(m)==p.Profile.heroClass);
   Directory.CreateDirectory(p.SaveFilePath+".tmp");C(!p.ClaimFirstClearReward(mechanic)&&p.LastRewardMoment==null,"failed first core has no presentation");Directory.Delete(p.SaveFilePath+".tmp");
   C(p.ClaimFirstClearReward(mechanic)&&p.LastRewardMoment.Kind==RewardMomentKind.FirstCore&&p.LastRewardMoment.Item.mechanic==mechanic,"first core publishes actual committed item");long seq=p.LastRewardMoment.Sequence;C(!p.ClaimFirstClearReward(mechanic)&&p.LastRewardMoment.Sequence==seq,"claimed core never replays");
   Directory.CreateDirectory(p.SaveFilePath+".tmp");C(!p.ExchangeMechanic(mechanic)&&p.LastRewardMoment.Sequence==seq,"failed mechanic exchange publishes no moment");Directory.Delete(p.SaveFilePath+".tmp");
   C(p.ExchangeMechanic(mechanic)&&p.LastRewardMoment.Kind==RewardMomentKind.MechanicExchange&&p.LastRewardMoment.MaterialsDelta==-12,"core exchange snapshot carries exact cost");string id=p.LastRewardMoment.Item.id;p.Equip(id);
   seq=p.LastRewardMoment.Sequence;Directory.CreateDirectory(p.SaveFilePath+".tmp");C(!p.AscendMechanic(id,true)&&p.LastRewardMoment.Sequence==seq,"failed ascension publishes no moment");Directory.Delete(p.SaveFilePath+".tmp");
   C(p.AscendMechanic(id,true)&&p.LastRewardMoment.Kind==RewardMomentKind.Ascension&&p.LastRewardMoment.Item.rarity==Rarity.Legendary&&p.LastRewardMoment.MaterialsDelta==-24,"ascension snapshot carries actual transformed identity and cost");seq=p.LastRewardMoment.Sequence;C(!p.AscendMechanic(id,true)&&p.LastRewardMoment.Sequence==seq,"repeat ascension cannot replay");
   var equipped=p.Profile.inventory.Find(x=>x.id==id);equipped.mechanicVariantUnlocked=true;equipped.mechanicVariant=1;var wrong=Copy(equipped);wrong.id=Guid.NewGuid().ToString("N");wrong.baseAttack+=10;wrong.mechanicVariant=0;C(!p.IsStrictEquipmentUpgrade(wrong),"loss of selected mechanism variant disqualifies upgrade");wrong.mechanic=EquipmentMechanic.None;C(!p.IsStrictEquipmentUpgrade(wrong),"loss of mechanic disqualifies upgrade");
   p.Profile.fashionThreads=66;p.Save();var quote=p.QuoteThreadMaterialExchange();C(p.ExchangeThreadsForMaterial(quote,true)&&p.LastRewardMoment.Kind==RewardMomentKind.MaterialExchange&&p.LastRewardMoment.ThreadsDelta==-6&&p.LastRewardMoment.MaterialsDelta==1,"material exchange exact signed resource snapshot");seq=p.LastRewardMoment.Sequence;C(p.ExchangeThreadsForMaterial(quote,true)&&p.LastRewardMoment.Sequence==seq,"idempotent exchange replay never replays sound or visual");
   Directory.CreateDirectory(p.SaveFilePath+".tmp");C(!p.ChooseLegendaryFashion(FashionSlot.Weapon,true)&&p.LastRewardMoment.Sequence==seq,"failed fashion exchange publishes no moment");Directory.Delete(p.SaveFilePath+".tmp");
   C(p.ChooseLegendaryFashion(FashionSlot.Weapon,true)&&p.LastRewardMoment.Fashion.slot==FashionSlot.Weapon&&p.LastRewardMoment.ThreadsDelta==-30,"legendary exchange uses actual fashion and cost");C(p.ChooseLegendaryFashion(FashionSlot.Wings,true),"both legendary gaps filled");p.Profile.fashionThreads=60;
   C(ChestRevealPresentation.LegendaryExchangeHint(p.Profile).Contains("两部位传说已收藏")&&!ChestRevealPresentation.ResultWithCollection(new ChestReward(),p.Profile).Contains("可在营地自选传说"),"full legendary collection cannot advertise a nonexistent exchange");
  }
  return "PASS "+n+" actual frozen-save recovery, committed reward moments, strict upgrade and collection eligibility assertions";
 }
}
