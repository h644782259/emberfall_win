using System;
using System.Collections.Generic;
using UnityEngine;
namespace Emberfall
{
 public sealed partial class GameSession
 {
  public int SelectedArenaMode {get;set;}=-1;
  public ExpeditionModeState ModeRun {get;private set;}
  public bool ModeFinished {get{return ChapterActive?ChapterFinished:RoomChainRun!=null?RoomChainRun.Finished:ModeRun!=null&&ModeRun.IsTerminal&&ModeRun.Status!=ExpeditionModeStatus.Disposed;}}
  public bool ModeRewardPending {get{return ChapterActive?ChapterRewardPending:RoomChainRun!=null?RoomChainRun.Finished&&!RoomChainRun.Failed&&!RoomChainRun.RewardClaimed:ModeRun!=null&&ModeRun.RewardPending;}}
  public bool SpecialAdventure {get{return ChapterActive||ModeRun!=null||RoomChainRun!=null;}}
  public string ModeName {get{return ChapterActive?"首章 · "+ChapterDefinition.Get(ActiveChapterNode).Name:RoomChainRun!=null?"回廊远征":ModeRun==null?"沉星遗迹":ArenaModeName((int)ModeRun.Mode);}}
  public static string ArenaModeName(int mode){return mode==0?"守望林庭":mode==1?"烬河突围":mode==2?"蚀星斗场":"沉星遗迹";}
  public string ModeObjectiveStatus
  {get{if(ChapterActive)return ChapterObjectiveStatus;if(RoomChainRun!=null)return RoomObjectiveStatus;if(ModeRun==null)return "";if(ModeRun.Status==ExpeditionModeStatus.Won)return ModeRewardPending?"挑战完成 · 奖励待保存":"挑战完成 · 奖励已保存";
   if(ModeRun.Status==ExpeditionModeStatus.Failed)return ModeRun.Failure==ExpeditionModeFailure.TimeExpired?"时限已到 · 返回营地再试":"挑战结束 · 返回营地";
   return "阶段 "+(ModeRun.PhaseIndex+1)+" / 3  ·  "+Mathf.CeilToInt(ModeRun.RemainingSeconds)+"秒"+(ModeRun.Mode==ExpeditionModeKind.HoldPoint?"  ·  "+ModeRun.HoldStateLabel+" "+Mathf.RoundToInt(ModeRun.ObjectiveProgress*100)+"%":"");}}
  private sealed class ArenaEnemyReceipt {public ExpeditionPhasePlan Plan;public int Index;}
  private readonly Dictionary<EnemyController,ArenaEnemyReceipt> arenaEnemies=new Dictionary<EnemyController,ArenaEnemyReceipt>();
  private string modeReceipt;
  private int nextArenaEnemy;
  private float arenaSpawnDelay,arenaSpawnBlocked;
  private bool arenaAwaitingBlessing,arenaResultRecorded;
  private ArenaHazards arenaHazards;
  private int modeGoldReward,modeXpReward,modeMaterialReward;
  private bool modeRewardDetailsUnavailable;

  private void ResetArenaMode(bool dungeon)
  {
   if(ModeRun!=null)ModeRun.Dispose();ModeRun=null;arenaEnemies.Clear();arenaHazards=null;
   modeRewardDetailsUnavailable=false;modeGoldReward=modeXpReward=modeMaterialReward=0;arenaAwaitingBlessing=arenaResultRecorded=false;nextArenaEnemy=0;arenaSpawnDelay=arenaSpawnBlocked=0;
   ResetRoomChain(dungeon);
   if(!dungeon||ChapterActive||SelectedArenaMode<0||RoomChainRun!=null)return;
   SelectedArenaMode=Mathf.Clamp(SelectedArenaMode,0,2);
   ModeRun=new ExpeditionModeState((ExpeditionModeKind)SelectedArenaMode,DungeonTier,runSeed);
   modeReceipt=Guid.NewGuid().ToString("N");DungeonLayout=SelectedArenaMode+2;
  }
  private void BeginArenaScene()
  {
   var go=new GameObject("Arena objective and hazards");go.transform.SetParent(world.transform,false);
   arenaHazards=go.AddComponent<ArenaHazards>();arenaHazards.Initialize(this,(int)ModeRun.Mode);
   BeginArenaPhase();
  }
  private void BeginArenaPhase()
  {
   ExpeditionPhasePlan plan;if(ModeRun==null||!ModeRun.TryBeginPhase(out plan))return;
   DungeonWave=plan.PhaseIndex+1;wavePopulation=plan.EnemyCount;objectiveHealedThisWave=false;nextArenaEnemy=0;arenaSpawnDelay=arenaSpawnBlocked=0;
   SpawnArenaEnemies();Notify(ModeName+" · 第 "+DungeonWave+" 阶段");
  }
  private void SpawnArenaEnemies()
  {
   if(ModeRun==null||ModeRun.Status!=ExpeditionModeStatus.Active)return;var plan=ModeRun.CurrentPhase;
   while(nextArenaEnemy<plan.EnemyCount&&ModeRun.AvailableSpawnSlots>0&&Enemies.Count<plan.MaxSimultaneous)
   {
    int index=nextArenaEnemy;var role=plan.Enemies[index];bool boss=role==ExpeditionEnemyRole.Boss;
    EnemyKind kind=boss||role==ExpeditionEnemyRole.Guardian?EnemyKind.Guardian:role==ExpeditionEnemyRole.Ranged?EnemyKind.Wisp:index%2==0?EnemyKind.Goblin:EnemyKind.Slime;
    float angle=(index*2.39996f+plan.PhaseIndex*.7f)+(plan.Seed%100)*.0314f;
    Vector3 desired=new Vector3(Mathf.Sin(angle)*12,0,Mathf.Cos(angle)*12),point;
    if(!TrySafeSpawn(desired,boss?1f:kind==EnemyKind.Guardian?.65f:.5f,5.5f,out point))break;
    if(!ModeRun.TryRegisterSpawn(plan,index))break;
    try
    {
     SpawnEnemy(kind,DungeonEntryLevel,point,boss);
     EnemyController enemy=Enemies[Enemies.Count-1];arenaEnemies.Add(enemy,new ArenaEnemyReceipt{Plan=plan,Index=index});
     if(boss)enemy.ConfigureArenaBoss((int)plan.BossPattern);
     nextArenaEnemy++;arenaSpawnBlocked=0;
    }
    catch(System.Exception error){ModeRun.CancelSpawnRegistration(plan,index);ModeRun.Fail(ExpeditionModeFailure.SpawnBlocked);Notify("挑战生成失败："+error.Message);FinalizeArenaResult();return;}
   }
  }
  private void RecordArenaDefeat(EnemyController enemy)
  {
   ArenaEnemyReceipt receipt;if(ModeRun==null||!arenaEnemies.TryGetValue(enemy,out receipt))return;
   arenaEnemies.Remove(enemy);ModeRun.RecordDefeat(receipt.Plan,receipt.Index);
  }
  private void TickArenaRun()
  {
   if(ModeRun==null)return;
   if(ModeRun.Status==ExpeditionModeStatus.Active)
   {
    int pressure=0;foreach(var enemy in Enemies)if(enemy!=null&&!enemy.IsDead&&enemy.gameObject.activeInHierarchy&&ExpeditionModeState.ContestsHoldPoint(CombatFx.Flat(enemy.transform.position).sqrMagnitude,enemy.NavigationRadius))pressure++;
    ModeRun.Advance(Time.deltaTime,!InputBlocked,Player!=null&&ExpeditionModeState.InsideHoldPoint(CombatFx.Flat(Player.transform.position).sqrMagnitude),pressure,!IsDead);
    if(arenaHazards!=null&&!ModeRun.IsTerminal)arenaHazards.Advance(Time.deltaTime);
    arenaSpawnDelay-=Time.deltaTime;
    if(arenaSpawnDelay<=0){arenaSpawnDelay=1.4f;SpawnArenaEnemies();}
    if(ModeRun.PendingSpawnCount>0&&ModeRun.AliveEnemies==0){arenaSpawnBlocked+=Time.deltaTime;if(arenaSpawnBlocked>10)ModeRun.Fail(ExpeditionModeFailure.SpawnBlocked);}
   }
   if(ModeRun.Status==ExpeditionModeStatus.AwaitingSpawn&&!arenaAwaitingBlessing)
   {
    if(!ChallengeRun)Player.Heal(Player.MaxHealth*.2f);else HealingCharges=Mathf.Min(3,HealingCharges+1);
    // Short arena challenges flow directly into the next phase without a blessing prompt.
    BeginArenaPhase();UpdateTimeScale();
   }
   FinalizeArenaResult();
  }
  private void FinalizeArenaResult()
  {
   if(ModeRun!=null&&ModeFinished&&!arenaResultRecorded)
   {
    arenaResultRecorded=true;DungeonCleared=ModeRun.Status==ExpeditionModeStatus.Won;
    if(DungeonCleared){TrySettleArenaReward();GameAudio.Play(SoundCue.Victory);}else Notify(ModeObjectiveStatus);
    LastRunSummary=BuildRunSummary(DungeonCleared);SuspendInputs();if(DungeonCleared)DismissFinishedResult();UpdateTimeScale();
   }
  }
  private bool ConfirmArenaBlessing(int index)
  {
   if(ModeRun==null||!arenaAwaitingBlessing||ModeRun.Status!=ExpeditionModeStatus.AwaitingSpawn||!RunChoices.Choose(index))return false;
   arenaAwaitingBlessing=false;BeginArenaPhase();UpdateTimeScale();return true;
  }
  private static long TotalEarnedExperience(GameProfile profile)
  {long result=profile.xp;for(int level=1;level<profile.level;level++)result+=GameBalance.XpToNext(level);return result;}
  private void ApplyRewardPresentation(string receipt)
  {
   var detail=Progression.GetRewardPresentation(receipt);modeRewardDetailsUnavailable=detail==null;
   modeGoldReward=detail==null?0:detail.Gold;modeXpReward=detail==null?0:detail.Experience;modeMaterialReward=detail==null?0:detail.Materials;
  }
  private string RewardPresentationText(string label)
  {return modeRewardDetailsUnavailable?label+" · 奖励已保存，旧回执缺少奖励明细，无法恢复准确数额（不会重复发放）":label+" · +"+modeGoldReward+"金币 · +"+modeXpReward+"经验 · +"+modeMaterialReward+"碎片";}
  public bool TrySettleArenaReward()
  {
   if(ChapterActive)return TrySettleChapterReward();
   if(RoomChainRun!=null)return TrySettleRoomReward();
   if(ModeRun==null||!ModeRun.RewardPending)return true;
   ExpeditionRewardTicket ticket;if(!ModeRun.TryReserveReward(out ticket))return false;
   var rewardRun=ModeRun;var progression=Progression;string rewardSlot=progression.CurrentSlotId,receipt=modeReceipt;
   bool saved=false;
   try
   {
    int goldReward=HasBlessing(RunBlessing.RiskContract)?Mathf.RoundToInt(ticket.Reward.Gold*1.3f):ticket.Reward.Gold;
    saved=progression.TryGrantModeReward(receipt,goldReward,ticket.Reward.Experience,ticket.Reward.Materials,rewardRun.Tier,(int)rewardRun.Mode);
   }
   catch(Exception error)
   {
    // Changed/LeveledUp run after persistence and can throw. Reconcile the
    // original slot's durable receipt before completing its reserved ticket.
    var verification=new ProgressionService(progression.SaveDirectory);
    saved=verification.LoadSlot(rewardSlot)&&verification.Profile.lastModeRewardId==receipt;
    Debug.LogWarning("挑战奖励回调异常，已核对存档回执："+error);
   }
   finally {rewardRun.CompleteReward(ticket,saved);}
   // A callback may replace the active host; never publish the old result there.
   if(ModeRun!=rewardRun||Progression!=progression||progression.CurrentSlotId!=rewardSlot)return saved;
   if(saved){ApplyRewardPresentation(receipt);LogSystem(RewardPresentationText("挑战结算"));LastRunSummary=BuildRunSummary(true);}
   else Notify(Progression.LastError);
   return saved;
  }
 }
}
