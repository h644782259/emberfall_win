using System;using System.IO;using Emberfall;
public static class ArenaRewardExceptionTests
{
 static int n;static void C(bool b,string why){n++;if(!b)throw new Exception(why);}
 static ExpeditionModeState Winner(){var s=new ExpeditionModeState(ExpeditionModeKind.TimedBreakthrough,1,7);for(int phase=0;phase<3;phase++){ExpeditionPhasePlan plan;C(s.TryBeginPhase(out plan),"phase begins");for(int i=0;i<plan.EnemyCount;i++){C(s.TryRegisterSpawn(plan,i),"spawn accepted");C(s.RecordDefeat(plan,i),"kill accepted");}}return s;}
 static GameSession Host(string root){var p=new ProgressionService(Path.Combine(root,Guid.NewGuid().ToString("N")));C(p.CreateNewSlot(HeroClass.Ranger),"create source save");return new GameSession{Progression=p,ModeRun=Winner(),modeReceipt=Guid.NewGuid().ToString("N")};}
 public static string Run(string root)
 {
  foreach(bool levelEvent in new[]{false,true})
  {
   var h=Host(root);var run=h.ModeRun;int gold=h.Progression.Profile.gold;
   int callbacks=0;
   Action changed=()=>{callbacks++;throw new InvalidOperationException("observer after durable commit");};Action<int> level=(_)=>{callbacks++;throw new InvalidOperationException("level observer after durable commit");};
   if(levelEvent)h.Progression.LeveledUp+=level;else h.Progression.Changed+=changed;
   bool returned=false;try{returned=h.TrySettleArenaReward();}catch(InvalidOperationException){}
   C(!run.RewardReserved,"committed subscriber exception must not strand reservation");C(returned&&run.RewardClaimed,"durable receipt reconciles original ticket after callback fault");
   C(callbacks==1,"actual selected postcommit callback was invoked and threw once");
   var disk=new ProgressionService(h.Progression.SaveDirectory);C(disk.LoadSlot(h.Progression.CurrentSlotId)&&disk.Profile.lastModeRewardId==h.modeReceipt,"independent durable receipt proves commit");
   C(disk.Profile.gold==gold+run.Reward.Gold,"actual reward saved once");int logs=h.Logs;C(h.TrySettleArenaReward()&&h.Logs==logs&&h.Progression.Profile.gold==disk.Profile.gold,"retry neither grants nor announces twice");
  }
  var failed=Host(root);string before=File.ReadAllText(failed.Progression.SaveFilePath);failed.ThrowBeforeGrant=true;
  try{failed.TrySettleArenaReward();}catch(InvalidOperationException){}
  C(!failed.ModeRun.RewardReserved&&failed.ModeRun.RewardPending&&File.ReadAllText(failed.Progression.SaveFilePath)==before,"precommit exception releases unchanged ticket for retry");
  failed.ThrowBeforeGrant=false;C(failed.TrySettleArenaReward()&&failed.ModeRun.RewardClaimed,"precommit exception retry commits");
  var full=Host(root);Directory.CreateDirectory(full.Progression.SaveFilePath+".tmp");C(!full.TrySettleArenaReward()&&!full.ModeRun.RewardReserved&&full.ModeRun.RewardPending,"ordinary write failure still releases reservation");Directory.Delete(full.Progression.SaveFilePath+".tmp");C(full.TrySettleArenaReward(),"write failure retry works");
  var swap=Host(root);var original=swap.ModeRun;var replacement=Winner();swap.Progression.Changed+=()=>{swap.ModeRun=replacement;throw new InvalidOperationException("observer replaced host run");};
  C(swap.TrySettleArenaReward()&&original.RewardClaimed&&!replacement.RewardClaimed&&!replacement.RewardReserved&&swap.Logs==0,"callback host replacement settles only reserved original run");
  return "PASS: "+n+" actual arena host/reservation/persistence/Changed/LeveledUp exception assertions";
 }
}
