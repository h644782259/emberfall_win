using System;using System.IO;using System.Collections.Generic;using Emberfall;using UnityEngine;
class RewardPresentationExceptionCases{
 static int failures,checks,cases;static List<string> errors;
 static void C(bool b,string why){checks++;if(!b){failures++;errors.Add(why);}}
 static GameProfile Disk(ProgressionService p){var q=new ProgressionService(p.SaveDirectory);if(!q.LoadSlot(p.CurrentSlotId))throw new Exception("independent disk reload failed: "+q.LastError);return q.Profile;}
 static ExpeditionModeState Winner(){var s=new ExpeditionModeState(ExpeditionModeKind.TimedBreakthrough,1,7);for(int phase=0;phase<3;phase++){ExpeditionPhasePlan plan;if(!s.TryBeginPhase(out plan))throw new Exception("phase");for(int i=0;i<plan.EnemyCount;i++){s.TryRegisterSpawn(plan,i);s.RecordDefeat(plan,i);}}return s;}
 static void History(GameSession original,string kind,GameProfile committed,int gold,long xp,int materials){
  if(typeof(ProgressionService).GetMethod("GetRewardPresentation")==null)return;
  bool chapter=original.ChapterActive;string id=chapter?committed.lastChapterRewardId:kind=="dungeon"?committed.lastDungeonRewardId:committed.lastModeRewardId;
  string detailName=chapter?"lastChapterRewardDetails":kind=="dungeon"?"lastDungeonRewardDetails":"lastModeRewardDetails";
  var field=typeof(GameProfile).GetField(detailName);
  foreach(string mode in new[]{"known","legacy","wrong-id"}){
   var p=new ProgressionService(original.Progression.SaveDirectory);C(p.LoadSlot(original.Progression.CurrentSlotId),"reload receipt into distinct service");
   var detail=field.GetValue(p.Profile);
   if(mode=="legacy")field.SetValue(p.Profile,null);
   if(mode=="wrong-id"){
    // Previous loop persisted a missing detail; recover the real committed one for this mismatch case.
    detail=field.GetValue(committed);typeof(GameProfile).GetField(detailName).SetValue(p.Profile,detail);
    if(detail!=null)detail.GetType().GetField("Id").SetValue(detail,"wrong-"+id);
   }
   if(mode!="known"){p.Save();C(string.IsNullOrEmpty(p.LastError),"persist legacy/mismatched receipt fixture");C(p.Load(),"reload historical detail shape through actual normalization");}
   var h=new GameSession{Progression=p,modeReceipt=id,pendingDungeonRewardId=id,ChapterActive=chapter,ActiveChapterNode=original.ActiveChapterNode,chapterReceipt=original.chapterReceipt};
   if(chapter)h.ChapterRun=new CompletedChapterShell();else if(kind=="room")h.RoomChainRun=new CompletedRoomShell();else if(kind=="arena")h.ModeRun=Winner();
   string disk=File.ReadAllText(p.SaveFilePath);C(h.Settle(kind),"reloaded receipt host retries successfully");C(File.ReadAllText(p.SaveFilePath)==disk,"reloaded receipt never pays or writes again");
   if(mode=="known"){
    C((chapter?0:h.modeGoldReward)==gold&&(chapter?h.ChapterResult.CompletionExperience:h.modeXpReward)==xp&&(chapter?h.ChapterResult.Materials:h.modeMaterialReward)==materials,"reloaded display restores exact atomically saved increments");
    if(chapter)C(h.ChapterResult.FirstCompletion&&h.ChapterResult.FirstCoreAvailable==(original.ActiveChapterNode==ChapterNode.StarPlatform)&&h.ChapterResult.UnlockedNode==-1,"reloaded chapter receipt restores first completion unlock and core facts");
   }else{
    bool unknown=chapter?(bool)typeof(ChapterResultSnapshot).GetProperty("RewardDetailsUnavailable").GetValue(h.ChapterResult):h.modeRewardDetailsUnavailable;
    string display=chapter?ChapterEntryPresentation.Result(h.ChapterResult):h.ReadDisplay();
    C(unknown&&display.Contains("已保存"),"historical "+mode+" receipt explicitly shows unknown without regrant");
    C(!display.Contains("+0")&&!display.Contains("新节点：")&&!display.Contains("本节点新难度：")&&!display.Contains("首通核心已可领取"),"unknown receipt never fabricates zero rewards or unlock facts");
   }
   int announcements=h.Logs.Count+h.Notifications.Count;C(h.Settle(kind)&&h.Logs.Count+h.Notifications.Count==announcements&&File.ReadAllText(p.SaveFilePath)==disk,"reloaded duplicate neither announces nor writes twice");
  }
 }
 static void Run(string root,string kind,string observer,bool capped){
  cases++;errors=new List<string>();var p=new ProgressionService(Path.Combine(root,Guid.NewGuid().ToString("N")));if(!p.CreateNewSlot(HeroClass.Ranger))throw new Exception("create");
  p.Profile.level=capped?100:1;p.Profile.xp=capped?0:GameBalance.XpToNext(1)-1;p.Profile.gold=capped?999999998:100;p.Profile.mechanicMaterials=capped?999998:0;
  int node=kind=="forest"?0:kind=="redrock"?1:2;
  bool chapter=kind!="dungeon"&&kind!="room"&&kind!="arena";
  if(chapter){if(!capped){p.Profile.level=ChapterProgression.UnlockLevel((ChapterNode)node);p.Profile.xp=GameBalance.XpToNext(p.Profile.level)-1;}p.Profile.chapterCompletedMask=(1<<node)-1;p.Profile.chapterFirstRewardMask=(1<<node)-1;for(int i=0;i<node;i++)p.Profile.chapterHighestDifficulties[i]=1;}
  p.Save();var h=new GameSession{Progression=p,modeReceipt=Guid.NewGuid().ToString("N"),pendingDungeonRewardId=Guid.NewGuid().ToString("N")};
  if(kind=="arena")h.ModeRun=Winner();if(kind=="room")h.RoomChainRun=new CompletedRoomShell();
  if(chapter){h.ChapterActive=true;h.ActiveChapterNode=(ChapterNode)node;h.ChapterRun=new CompletedChapterShell();if(!p.TryBeginChapterNode((ChapterNode)node,ChapterDifficulty.Heroic,1,out h.chapterReceipt))throw new Exception(p.LastError);for(int room=0;room<(node==2?1:2);room++)for(int i=0;i<(node==2?7:10);i++)if(!p.RegisterChapterEnemy(h.chapterReceipt,room,i,node==2&&i==0))throw new Exception("register");}
  var before=Disk(p);GameProfile committed=null;int called=0,following=0,thrown=0;
  Action fault=()=>{if(called++!=0)return;committed=Disk(p);if(observer=="mutatingChanged"||observer=="mutatingOnly"){p.Profile.gold-=7;p.Profile.mechanicMaterials=Math.Max(0,p.Profile.mechanicMaterials-1);p.Save();if(observer=="mutatingOnly")return;}throw new InvalidOperationException("intentional once postcommit observer");};
  if(observer=="Changed"||observer=="mutatingChanged"||observer=="mutatingOnly"){p.Changed+=fault;p.Changed+=()=>following++;}
  if(observer=="LeveledUp"){p.LeveledUp+=_=>fault();p.LeveledUp+=_=>following++;}
  string beforeFile=File.ReadAllText(p.SaveFilePath);if(observer=="writeFailure")Directory.CreateDirectory(p.SaveFilePath+".tmp");
  bool result=false;try{result=h.Settle(kind);}catch(InvalidOperationException){thrown++;}
  if(observer=="writeFailure"){C(!result&&!string.IsNullOrEmpty(p.LastError)&&File.ReadAllText(p.SaveFilePath)==beforeFile,"failed atomic write preserves error and grants nothing");Directory.Delete(p.SaveFilePath+".tmp");}
  // Retry is the actual host method, never a reconstructed reward call.
  try{result=h.Settle(kind);}catch(Exception e){errors.Add("retry threw "+e.Message);failures++;}
  if(committed==null)committed=Disk(p);
  int expectedGold=committed.gold-before.gold,expectedMaterials=committed.mechanicMaterials-before.mechanicMaterials;long expectedXp=GameSession.Earned(committed)-GameSession.Earned(before);
  var afterRetry=Disk(p);int logs=h.Logs.Count,notes=h.Notifications.Count;string disk=File.ReadAllText(p.SaveFilePath);
  try{C(h.Settle(kind),"duplicate host settlement succeeds");}catch(Exception e){errors.Add("duplicate threw "+e.Message);failures++;}
  C(result,"settlement completes after retry");C(File.ReadAllText(p.SaveFilePath)==disk,"duplicate settlement never writes or grants again");C(h.Logs.Count==logs&&h.Notifications.Count==notes,"duplicate settlement never re-announces");
  int observedGold=chapter?0:h.modeGoldReward,observedMaterials=chapter?h.ChapterResult.Materials:h.modeMaterialReward,observedXp=chapter?h.ChapterResult.CompletionExperience:h.modeXpReward;
  C(observedGold==expectedGold,"DISPLAY gold="+observedGold+" atomic="+expectedGold);C(observedXp==expectedXp,"DISPLAY xp="+observedXp+" atomic="+expectedXp);C(observedMaterials==expectedMaterials,"DISPLAY materials="+observedMaterials+" atomic="+expectedMaterials);
  if(kind=="dungeon"||kind=="arena")C(h.Logs.Count==1&&h.Logs[0].Contains("+"+expectedGold+"金币")&&h.Logs[0].Contains("+"+expectedXp+"经验")&&h.Logs[0].Contains("+"+expectedMaterials+"碎片"),"actual reward log announces the committed increments exactly once");
  if(chapter){string rendered=ChapterEntryPresentation.Result(h.ChapterResult);C(h.ChapterResult.Saved&&rendered.Contains("奖励已保存"),"compact chapter result confirms durable reward settlement");}
  if(observer!="none"&&observer!="writeFailure"){C(called>=1,"selected observer executes");C(following>0,"later subscribed observer still executes");}
  if(chapter){C(h.ChapterResult.Saved&&h.ChapterResult.FirstCompletion,"DISPLAY first completion preserved");C(h.ChapterResult.UnlockedNode==-1,"completion does not bypass the next chapter level gate");C(h.ChapterResult.UnlockedDifficulty==-1,"Heroic completion does not fabricate another difficulty unlock");C(h.ChapterResult.FirstCoreAvailable==(node==2),"DISPLAY first-core marker matches committed chapter");C((afterRetry.chapterCompletedMask&(1<<node))!=0&&afterRetry.chapterRewardSequence==before.chapterRewardSequence+1,"chapter progress and sequence committed exactly once");C(afterRetry.pendingFirstClearReward==(node==2),"durable first-core eligibility correct");}
  if(observer=="none")History(h,kind,committed,expectedGold,expectedXp,expectedMaterials);
  Console.WriteLine("CASE "+kind+" / "+observer+" / capped="+capped+" throws="+thrown+" atomic="+expectedGold+"/"+expectedXp+"/"+expectedMaterials+" display="+observedGold+"/"+observedXp+"/"+observedMaterials+" failures="+errors.Count);
  foreach(var e in errors)Console.WriteLine("  FAIL "+e);
 }
 static int Main(string[]args){foreach(string kind in new[]{"dungeon","room","arena","forest","redrock","star"})foreach(string observer in new[]{"none","Changed","LeveledUp","mutatingChanged","mutatingOnly","writeFailure"})foreach(bool capped in new[]{false,true}){
  if(capped&&observer=="LeveledUp")continue;
  try{Run(args[0],kind,observer,capped);}catch(Exception e){failures++;Console.WriteLine("CASE FATAL "+kind+"/"+observer+"/"+capped+" "+e);}}
  Console.WriteLine("RESULT cases="+cases+" checks="+checks+" failures="+failures+" (actual reward hosts/service; scene-completion shells, not Unity)");return failures==0?0:1;}
}
