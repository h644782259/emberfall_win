using System;using System.IO;using System.Collections.Generic;using Emberfall;
public static class EnemyKillCallbackFaultTests
{
 static int checks;
 static void C(bool b,string why){checks++;if(!b)throw new Exception(why);}
 static void Scenario(string root,string mode,string fault,bool writeFailure,int count)
 {
  var p=new ProgressionService(Path.Combine(root,Guid.NewGuid().ToString("N")));C(p.CreateNewSlot(HeroClass.Ranger),"create real save");p.Profile.xp=GameBalance.XpToNext(1)-1;p.Save();
  string disk=File.ReadAllText(p.SaveFilePath);int gold=p.Profile.gold,kills=p.Profile.kills,oldLevel=p.Profile.level;int badCalls=0,goodChanged=0,goodLevels=0;
  p.Changed+=()=>{if(fault=="changed"){badCalls++;throw new InvalidOperationException("kill changed observer fault");}};p.Changed+=()=>goodChanged++;
  p.LeveledUp+=level=>{if(fault=="level"){badCalls++;throw new InvalidOperationException("kill level observer fault");}};p.LeveledUp+=level=>goodLevels++;
  var h=new GameSession{Progression=p,ChapterActive=mode=="chapter",ModeRun=mode=="arena"?new object():null,RoomChainRun=mode=="room"?new object():null};
  var enemies=new List<EnemyController>();for(int i=0;i<count;i++){var e=new EnemyController{IsBoss=true};enemies.Add(e);h.Enemies.Add(e);}
  p.BeginDungeonStage();if(writeFailure)Directory.CreateDirectory(p.SaveFilePath+".tmp");
  int serializations=UnityEngine.JsonUtility.SerializationCount;
  foreach(var enemy in enemies)h.OnEnemyKilled(enemy);
  C(UnityEngine.JsonUtility.SerializationCount==serializations,"real mass kill callback never serializes");
  C(File.ReadAllText(p.SaveFilePath)==disk&&p.Profile.level==oldLevel&&p.Profile.gold==gold,"real kill callback defers disk, XP and currency");
  C(p.Profile.kills==kills+count&&p.Profile.dungeonGold==105*count&&p.Profile.dungeonLoot.Count==count,"every admitted enemy contributes once to ledger");
  C(goodChanged==0&&goodLevels==0,"mass kill does not refresh stats or level feedback");
  foreach(var enemy in enemies){C(enemy.DeathCalls==1,"death presentation retained");h.OnEnemyKilled(enemy);}
  C(p.Profile.kills==kills+count&&h.LootDelivered==count,"duplicate callbacks do not double award");
  C(h.WavesScheduled==(mode=="normal"?1:0),"ordinary next wave schedules once");
  C(p.FinishDungeonRewards()==!writeFailure,"result reflects disk failure");
  if(writeFailure){C(File.ReadAllText(p.SaveFilePath)==disk&&p.Profile.dungeonLoot.Count==count,"failed result retains all pending drops");Directory.Delete(p.SaveFilePath+".tmp");C(p.FinishDungeonRewards(),"retry succeeds");}
  C(p.Profile.gold==gold+105*count&&p.Profile.dungeonLoot.Count==0&&p.Profile.level>oldLevel,"result pays all earned rewards");
  C(goodChanged==1&&goodLevels==p.Profile.level-oldLevel,"one result change and complete level sequence");
  C(fault=="none"?badCalls==0:badCalls>0,"actual result observer fault exercised without blocking later listeners");
  C(File.ReadAllText(p.SaveFilePath+".bak")==disk,"whole encounter rotates pre-stage backup once");
  var loaded=new ProgressionService(p.SaveDirectory);C(loaded.LoadSlot(p.CurrentSlotId)&&loaded.Profile.kills==kills+count&&loaded.Profile.gold==gold+105*count,"result reloads correctly");
  string paid=File.ReadAllText(p.SaveFilePath);C(p.FinishDungeonRewards()&&File.ReadAllText(p.SaveFilePath)==paid,"repeat result cannot pay again");
 }
 public static string Run(string root)
 {
  foreach(string mode in new[]{"normal","chapter","arena","room"})foreach(string fault in new[]{"none","changed","level"})foreach(bool writeFailure in new[]{false,true})foreach(int count in new[]{1,24})Scenario(root,mode,fault,writeFailure,count);
  return "PASS: "+checks+" actual kill callback assertions across 48 single/mass-kill scenarios";
 }
}
