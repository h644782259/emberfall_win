using System;using System.IO;using System.Collections.Generic;using Emberfall;
public static class EnemyKillCallbackFaultTests
{
 static int checks;static readonly List<string> failures=new List<string>();
 static void C(bool b,string why){checks++;if(!b)throw new Exception(why);}
 static void Scenario(string root,string mode,string fault,bool writeFailure)
 {
  string label=mode+"/"+fault+"/writeFailure="+writeFailure;
  try
  {
   var p=new ProgressionService(Path.Combine(root,Guid.NewGuid().ToString("N")));C(p.CreateNewSlot(HeroClass.Ranger),"create real save");p.Profile.xp=GameBalance.XpToNext(1)-1;p.Save();
   string disk=File.ReadAllText(p.SaveFilePath);int gold=p.Profile.gold,kills=p.Profile.kills;int badCalls=0,goodChanged=0,goodLevels=0;
   p.Changed+=()=>{if(fault=="changed"){badCalls++;throw new InvalidOperationException("kill changed observer fault");}};
   p.Changed+=()=>goodChanged++;
   p.LeveledUp+=level=>{if(fault=="level"){badCalls++;throw new InvalidOperationException("kill level observer fault");}};
   p.LeveledUp+=level=>goodLevels++;
   var h=new GameSession{Progression=p,ChapterActive=mode=="chapter",ModeRun=mode=="arena"?new object():null,RoomChainRun=mode=="room"?new object():null};
   var e=new EnemyController{IsBoss=true};h.Enemies.Add(e);
   if(writeFailure)Directory.CreateDirectory(p.SaveFilePath+".tmp");
   bool escaped=false;try{h.OnEnemyKilled(e);}catch(InvalidOperationException){escaped=true;}
   int rewardedGold=p.Profile.gold,rewardedKills=p.Profile.kills,rewardedXp=p.Profile.xp,rewardedLevel=p.Profile.level;
   C(rewardedKills==kills+1&&rewardedGold==gold+105&&rewardedLevel>1,"real kill reward stays live once");
   C(h.Enemies.Count==0,"real host removed admitted enemy");
   h.OnEnemyKilled(e);
   C(p.Profile.kills==rewardedKills&&p.Profile.gold==rewardedGold&&p.Profile.xp==rewardedXp&&p.Profile.level==rewardedLevel,"repeated admission refuses duplicate grant after callback failure");
   if(writeFailure)
   {
    C(File.ReadAllText(p.SaveFilePath)==disk&&!string.IsNullOrEmpty(p.LastError),"write failure retains durable old document and LastError with live reward");
    Directory.Delete(p.SaveFilePath+".tmp");p.Save();C(string.IsNullOrEmpty(p.LastError),"retry Save persists without another grant");
   }
   var loaded=new ProgressionService(p.SaveDirectory);C(loaded.LoadSlot(p.CurrentSlotId)&&loaded.Profile.gold==rewardedGold&&loaded.Profile.kills==rewardedKills,"real disk contains exactly one kill after successful write or retry");
   C(!escaped&&h.LootDelivered==1&&e.DeathCalls==1&&h.transientObjects.Contains(e.gameObject),"post-grant observer fault must not skip loot/death/ownership tail");
   C(goodChanged==1&&goodLevels==rewardedLevel-1,"later listeners and earned level notifications still run");
   C(fault=="none"?badCalls==0:badCalls>0,"requested actual callback fault was exercised");
   C(h.ChapterFinalized==(mode=="chapter"?1:0)&&h.RoomFinalized==(mode=="room"?1:0)&&h.ArenaFinalized==(mode=="arena"?1:0)&&h.WavesScheduled==(mode=="normal"?1:0),"last enemy dispatches exactly the correct mode completion tail");
   C(h.ExpeditionRecorded==1&&h.ArenaRecorded==1&&h.RoomRecorded==1&&h.LootDelivered==1&&e.DeathCalls==1,"duplicate callback does not repeat death reward or completion delivery");
   Console.WriteLine("PASS scenario "+label);
  }
  catch(Exception error){failures.Add(label+": "+error.Message);Console.WriteLine("FAIL scenario "+label+": "+error);}
 }
 public static string Run(string root)
 {
  foreach(string mode in new[]{"normal","chapter","arena","room"})foreach(string fault in new[]{"none","changed","level"})foreach(bool writeFailure in new[]{false,true})Scenario(root,mode,fault,writeFailure);
  if(failures.Count>0)throw new Exception("Enemy kill callback completion regressions: "+string.Join(" | ",failures));
  return "PASS: "+checks+" real OnEnemyKilled/progression assertions across 24 scenarios; managed delivery boundaries";
 }
}
