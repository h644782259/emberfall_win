using System.Collections.Generic;
using UnityEngine;
namespace Emberfall
{
 public sealed partial class GameSession
 {
  public RoomChainState RoomChainRun {get;private set;}
  private sealed class RoomEnemyReceipt{public RoomChainPlan Plan;public int Index;}
  private readonly Dictionary<EnemyController,RoomEnemyReceipt> roomEnemies=new Dictionary<EnemyController,RoomEnemyReceipt>();
  private GameObject roomExitMarker;private bool roomResultRecorded;
  public bool NearRoomExit {get{return RoomChainRun!=null&&RoomChainRun.DoorUnlocked&&Player!=null&&Vector3.Distance(Player.transform.position,new Vector3(0,0,14))<3.8f;}}
  public static string ChainRoomName(int index){return new[]{"林缘前厅","断梁书库","沉桥水院","星泉休憩室","王座封印"}[Mathf.Clamp(index,0,4)];}
  public string RoomObjectiveStatus {get{return RoomChainRun==null?"":RoomChainRun.Finished?(RoomChainRun.Failed?"远征结束 · 返回营地":"远征完成 · 领取结算"):"房间 "+(RoomChainRun.Room.Index+1)+" / 5 · "+TacticalObjectiveStatus;}}
  private void ResetRoomChain(bool dungeon)
  {
   pendingRoomChoice.Cancel();
   if(RoomChainRun!=null)RoomChainRun.Dispose();RoomChainRun=null;roomEnemies.Clear();roomExitMarker=null;roomObjectiveMarker=null;roomSealMarkers[0]=roomSealMarkers[1]=null;roomSupplier=null;roomResultRecorded=false;
   if(dungeon&&!ChapterActive&&SelectedArenaMode==3){if(retryingRoomChain)runSeed=roomRetrySeed;else{runSeed=RoomTactics.NextSeed(runSeed,previousRoomSeed,beforePreviousRoomSeed);beforePreviousRoomSeed=previousRoomSeed;previousRoomSeed=runSeed;}RoomChainRun=new RoomChainState(runSeed,retryingRoomChain?roomRetryBranch:RoomBranch.None);roomRetryTier=DungeonTier;roomRetryLimited=ChallengeRun;DungeonLayout=RoomChainRun.Room.Layout;modeReceipt=System.Guid.NewGuid().ToString("N");}
  }
  private void BeginRoomChainScene()
  {
   var plan=RoomChainRun.Room;LogSystem("远征种子 "+runSeed+" · "+RoomTactics.Name(plan.Objective));DungeonWave=plan.Index+1;wavePopulation=Mathf.Max(1,plan.EnemyCount);objectiveHealedThisWave=false;
   roomExitMarker=WorldBuilder.MakeLootBeacon(new Vector3(0,0,14),new Color(.25f,.8f,1));roomExitMarker.name="Locked room gate";roomExitMarker.transform.SetParent(world.transform,true);roomExitMarker.SetActive(false);
   BuildRoomObjective();if(RoomChainRun.Finished){FinalizeRoomChain();return;}
   if(plan.Index==RoomTactics.EventRoom(runSeed))BuildSideEvent();
   if(plan.Interlude)
   {Player.Heal(Player.MaxHealth*.25f);RunChoices.PrepareRoomChoice(2,Progression.Profile,MobileControls.Active,runSeed);UpdateTimeScale();return;}
   var occupied=new List<Vector3>();
   for(int index=0;index<plan.EnemyCount;index++)
   {
    bool boss=plan.Boss&&index==0;EnemyKind kind=plan.Boss?(boss?EnemyKind.Guardian:index%3==1?EnemyKind.Goblin:EnemyKind.Guardian):index==0?EnemyKind.Wisp:index%3==1?EnemyKind.Guardian:index%3==2?EnemyKind.Goblin:EnemyKind.Slime;
    bool escape=plan.Objective==RoomObjective.Escape;
    if(plan.Branch==RoomBranch.Seal)kind=index%2==0?EnemyKind.Guardian:EnemyKind.Goblin;
    if(plan.Branch==RoomBranch.Supply)kind=index==0||index>=4?EnemyKind.Wisp:index<=2?EnemyKind.Guardian:EnemyKind.Goblin;
    if(escape)kind=index==0?EnemyKind.Wisp:index==1||index==3?EnemyKind.Guardian:index==2||index==4?EnemyKind.Goblin:EnemyKind.Slime;
    float angle=index*2.39996f+plan.Index*.42f;Vector3 desired=new Vector3(Mathf.Sin(angle)*10,0,Mathf.Cos(angle)*9+2),point;
    bool safe=plan.Boss?TrySafeSpawn(desired,boss?1.3f:.65f,5.5f,out point):plan.Branch!=RoomBranch.None?RoomBranchGeometry.TrySpawn(plan, index,occupied,out point):escape?EscapeRoomFormation.TrySpawn(runSeed,index,occupied,out point):TacticalRoomGeometry.TrySpawn(runSeed,plan.Index,index,occupied,out point);
    if(!safe||!WorldTraversal.CanReach(TacticalRoomGeometry.Entrance,point,boss?1.3f:.65f)||!RoomChainRun.Register(plan,index)){RoomChainRun.Fail(RoomFailureReason.GenerationOrPathFailure);FinalizeRoomChain();return;}
    try
    {
     occupied.Add(point);SpawnEnemy(kind,DungeonEntryLevel,point,boss);EnemyController enemy=Enemies[Enemies.Count-1];
     if(plan.Branch!=RoomBranch.None)enemy.ConfigureEscapePost(plan.Branch==RoomBranch.Supply&&kind==EnemyKind.Wisp?EscapeRole.GateSupplier:EscapeRole.GateGuard,point);
     if(escape)enemy.ConfigureEscapePost(EscapeRoomFormation.Role(index),point);
     if(index==0&&!plan.Boss&&plan.Branch!=RoomBranch.Seal){roomSupplier=enemy;Notify(RoomTactics.Name(plan.Objective)+" · 金环魔灵为6米内可见同伴减伤30%，引开或优先击败");}
     if(boss)LargeExpeditionBoss.Configure(enemy,DungeonTier,runSeed+plan.Index*911);
     TacticalEnemyVisual.Attach(enemy,this);
     roomEnemies.Add(enemy,new RoomEnemyReceipt{Plan=plan,Index=index});
    }
    catch(System.Exception error){RoomChainRun.Fail(RoomFailureReason.GenerationOrPathFailure);Notify("房间生成失败："+error.Message);FinalizeRoomChain();return;}
   }
  }
  private void RecordRoomDefeat(EnemyController enemy)
  {
   RoomEnemyReceipt receipt;if(RoomChainRun==null||!roomEnemies.TryGetValue(enemy,out receipt))return;
   bool wasOpen=RoomChainRun.DoorUnlocked;roomEnemies.Remove(enemy);RoomChainRun.Defeat(receipt.Plan,receipt.Index);
   if(!wasOpen&&RoomChainRun.DoorUnlocked)OpenRoomGate();
  }
  public bool EnterNextRoom()
  {
   if(RoomChainRun==null||!NearRoomExit||InputBlocked||pendingRoomChoice.Pending)return false;
   if(RoomChainRun.OpenBranchChoice()){SuspendInputs();UpdateTimeScale();return false;}
   if(!SaveBeforeLeaving())return false;
   return EnterNextRoomAfterSave();
  }
  // Both ordinary travel and branch confirmation arrive with one successful preflight.
  private bool EnterNextRoomAfterSave()
  {
   if(!RoomChainRun.Next(true,false))return false;
   SuspendInputs();changingZone=true;
   try
   {
   AbandonSideEvent();
   int previousCombatEpoch=Player.CombatEpoch;
   foreach(var enemy in Enemies)if(enemy!=null){enemy.gameObject.SetActive(false);Destroy(enemy.gameObject);}Enemies.Clear();roomEnemies.Clear();
   foreach(var obj in transientObjects)if(obj!=null){obj.SetActive(false);Destroy(obj);}transientObjects.Clear();
   if(world!=null){world.SetActive(false);Destroy(world);}
   Player.RetireCombatForWorldTransition();RetireWorldLootReceipts(previousCombatEpoch);
   DungeonLayout=RoomChainRun.Room.Layout;world=WorldBuilder.Build(ZoneKind.Dungeon,DungeonLayout,Progression.HighestAdventureTier);
   // Room travel cancels stale effects but deliberately keeps every skill cooldown.
   Player.Teleport(TacticalRoomGeometry.Entrance);Camera.main.GetComponent<AdventureCamera>().Snap();BeginRoomChainScene();
   Notify(RoomTactics.Name(RoomChainRun.Room.Objective)+" · 房间 "+(RoomChainRun.Room.Index+1)+" / 5");return !RoomChainRun.Failed;
   }
   catch(System.Exception error){RoomChainRun.Fail(RoomFailureReason.GenerationOrPathFailure);FinalizeRoomChain();Notify("房间生成失败："+error.Message);return false;}
   finally{changingZone=false;UpdateTimeScale();}
  }
  private bool ConfirmRoomInterlude(int index)
  {
   if(RoomChainRun==null||RoomChainRun.Finished||!HasStarted||!InDungeon||IsDead||Paused||pauseState.BackgroundPaused)return false;
   bool first=RoomChainRun.Room.Index==0&&RoomChainRun.DoorUnlocked&&RunChoices.CompletedWave==1;
   bool second=RoomChainRun.Room.Interlude&&!RoomChainRun.DoorUnlocked&&RunChoices.CompletedWave==2;
   if((!first&&!second)||!RunChoices.Choose(index))return false;
   if(second&&!RoomChainRun.ChooseInterlude())return false;
   if(roomExitMarker!=null)roomExitMarker.SetActive(true);UpdateTimeScale();Notify(second?"祝福已选 · 北门通往最终首领":"祝福已选 · 继续下一间");return true;
  }

  private void FinalizeRoomChain()
  {
   if(RoomChainRun!=null&&RoomChainRun.Finished)pendingRoomChoice.Cancel();
   if(RoomChainRun==null||!RoomChainRun.Finished||roomResultRecorded)return;
   roomResultRecorded=true;DungeonCleared=!RoomChainRun.Failed;
   if(DungeonCleared){TrySettleRoomReward();GameAudio.Play(SoundCue.Victory);}
   LastRunSummary=BuildRunSummary(DungeonCleared);SuspendInputs();UpdateTimeScale();
  }
  private bool TrySettleRoomReward()
  {
   if(RoomChainRun==null||!RoomChainRun.Finished||RoomChainRun.Failed||RoomChainRun.RewardClaimed)return true;
   int beforeGold=Progression.Profile.gold,beforeMaterials=Progression.Profile.mechanicMaterials;long xp=TotalEarnedExperience(Progression.Profile);
   int reward=200+DungeonTier*35;if(HasBlessing(RunBlessing.RiskContract))reward=Mathf.RoundToInt(reward*1.3f);
   bool saved=Progression.TryGrantModeReward(modeReceipt,reward,160+DungeonTier*25,TierRewardBand.Materials(4,DungeonTier),DungeonTier);
   if(!saved){Notify(Progression.LastError);return false;}
   RoomChainRun.ClaimReward(true);modeGoldReward=Progression.Profile.gold-beforeGold;modeMaterialReward=Progression.Profile.mechanicMaterials-beforeMaterials;modeXpReward=(int)System.Math.Max(0,TotalEarnedExperience(Progression.Profile)-xp);
   LastRunSummary=BuildRunSummary(true);return true;
  }
 }
}
