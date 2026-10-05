using System;using System.IO;using System.Reflection;using Emberfall;
public static class ChestSnapshotRetryTests
{
 static int n;static void Check(bool b,string why){n++;if(!b)throw new Exception(why);}
 sealed class Roll:Random{readonly int gold,rarity;public Roll(int g,int r){gold=g;rarity=r;}public override int Next(int max)=>max==41?gold:max==100?rarity:0;}
 static void Rng(ProgressionService p,int gold,int rarity){typeof(ProgressionService).GetField("random",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(p,new Roll(gold,rarity));}
 static ChestReward Pending(ProgressionService p)=>(ChestReward)typeof(ProgressionService).GetField("pendingChestRoll",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(p);
 static ChestReward FailDraw(ProgressionService p){Directory.CreateDirectory(p.SaveFilePath+".tmp");Check(p.OpenDungeonChest()==null,"write failure leaves chest unopened");Directory.Delete(p.SaveFilePath+".tmp");return Pending(p);}
 public static string Run(string directory)
 {
  var p=new ProgressionService(directory);Check(p.CreateNewSlot(HeroClass.Ranger),"source slot created");p.Profile.pendingFashionChest=true;p.Save();Rng(p,1,18);
  string source=p.CurrentSlotId,sourcePath=p.SaveFilePath;int gold=p.Profile.gold,threads=p.Profile.fashionThreads;var frozen=FailDraw(p);string disk=File.ReadAllText(sourcePath);
  // Snapshot the same character through the actual public UI service entry.
  Check(p.SaveAsNewSlot(),"save as commits copied character");string copy=p.CurrentSlotId;Rng(p,40,0);
  Check(p.OpenDungeonChest(1)==null&&Pending(p)!=null&&Pending(p).id==frozen.id,"save-as snapshot must preserve failed draw identity");
  Check(p.Profile.gold==gold&&p.Profile.fashionThreads==threads&&p.Profile.fashions.Count==0,"copy transfers no uncommitted reward");
  ProgressionService staged;string error;Check(SaveSlotTransition.TryStage(p,copy,out staged,out error),"actual reload copied slot");p=staged;Rng(p,40,0);var retry=FailDraw(p);
  Check(retry.id==frozen.id&&retry.gold==frozen.gold&&retry.rarityIndex==frozen.rarityIndex,"copy/reload/retry preserves original draw id amount rarity");
  Check(p.OpenDungeonChest()!=null&&p.LastChestReward.Id==frozen.Id,"copy commits exact draw");
  Check(p.Profile.gold==gold+frozen.gold&&p.Profile.fashionThreads==threads+1,"copy applies one reward");Check(p.OpenDungeonChest()==null,"copy cannot settle twice");
  Check(File.ReadAllText(sourcePath)==disk,"copy leaves source save unchanged");Check(SaveSlotTransition.TryStage(p,source,out staged,out error),"return source through real transition");p=staged;Rng(p,40,0);
  Check(p.OpenDungeonChest(1)==null&&p.OpenDungeonChest()!=null&&p.LastChestReward.Id==frozen.Id,"source retained independent frozen snapshot");
  Check(p.AcknowledgeChestReward()&&p.SaveAsNewSlot(),"saved receipt copy succeeds");Check(p.LastChestReward.Id==frozen.Id&&!p.Profile.pendingFashionChest,"copy saved receipt never resurrects chest");
  p.Profile.pendingFashionChest=true;p.Profile.clearedRuns++;p.Save();FailDraw(p);
  Check(p.CreateNewSlot(HeroClass.Vanguard),"new character remains distinct");p.Profile.pendingFashionChest=true;p.Save();Check(p.OpenDungeonChest()!=null,"new character never inherits snapshot draw");
  // Failed snapshot at capacity must preserve source and its retry context.
  p.AcknowledgeChestReward();p.Profile.pendingFashionChest=true;p.Save();frozen=FailDraw(p);string active=p.CurrentSlotId;var live=p.Profile;
  for(int i=p.GetSaveSlots().Count;i<ProgressionService.MaximumSaveSlots;i++)File.WriteAllText(Path.Combine(directory,"emberfall-save-"+Guid.NewGuid().ToString("N")+".json"),"corrupt capacity fixture");
  Check(!p.SaveAsNewSlot()&&p.CurrentSlotId==active&&ReferenceEquals(p.Profile,live),"failed snapshot leaves active character intact");
  Check(p.OpenDungeonChest(1)==null&&p.OpenDungeonChest()!=null&&p.LastChestReward.Id==frozen.Id,"failed snapshot preserves exact retry");
  return "PASS: "+n+" actual SaveAsNewSlot failed-draw snapshot/reload/isolation/capacity checks";
 }
}
