using UnityEngine;
namespace Emberfall
{
 public sealed partial class GameSession
 {
  private bool retryingRoomChain,roomRetryLimited;
  private int roomRetrySeed,roomRetryTier,lastRoomRetryFrame=-1;
  private RoomBranch roomRetryBranch;
  public bool RoomBranchChoiceOpen {get{return RoomChainRun!=null&&!RoomChainRun.Finished&&RoomChainRun.BranchChoiceOpen;}}
  public static string RoomBranchDescription(RoomBranch branch)
  {return branch==RoomBranch.Seal?"守印侧廊\n目标：双印净化\n敌人：4名近战守点者，可引离印记\n地形：中央石墙、左右两印\n第四房星泉汇合 · 原基础奖励不变":"断供侧廊\n目标：猎杀金环供能者\n敌人：前排守卫与后排魔灵，共6名\n地形：交错隔墙、侧路绕后\n第四房星泉汇合 · 原基础奖励不变";}
  public void CancelRoomBranchChoice(){if(RoomChainRun!=null)RoomChainRun.CancelBranchChoice();SuspendInputs();UpdateTimeScale();}
  public bool ConfirmRoomBranch(RoomBranch branch)
  {
   if(!RoomBranchChoiceOpen||!HasStarted||!InDungeon||ChapterActive||PracticeActive||IsDead||Paused||pauseState.BackgroundPaused||uiBlocking||!NearRoomExit)return false;
   if(branch!=RoomBranch.Seal&&branch!=RoomBranch.Supply)return false;
   if(!SaveBeforeLeaving())return false;
   if(!RoomChainRun.SelectBranch(branch))return false;
   UpdateTimeScale();return EnterNextRoomAfterSave();
  }
  public bool CanRetryRoomChain {get{return HasStarted&&InDungeon&&!ChapterActive&&!PracticeActive&&RoomChainRun!=null&&RoomChainRun.Finished&&RoomChainRun.Failed&&RoomChainRun.Room!=null&&Player!=null&&!changingZone&&!retryingRoomChain&&lastRoomRetryFrame!=Time.frameCount;}}
  public bool RetryFailedRoomChain()
  {
   if(!CanRetryRoomChain||Paused||pauseState.BackgroundPaused||!SaveBeforeLeaving())return false;
   roomRetrySeed=RoomChainRun.Room.Seed;roomRetryBranch=RoomChainRun.SelectedBranch;
   int previousSelection=SelectedArenaMode,previousTier=SelectedDungeonTier;
   bool previousChallenge=ChallengeRun;
   lastRoomRetryFrame=Time.frameCount;retryingRoomChain=true;
   SelectedArenaMode=3;SelectedDungeonTier=roomRetryTier;ChallengeRun=roomRetryLimited;
   try
   {
    if(!ChangeZone(true))return false;
    IsDead=false;Paused=false;uiBlocking=false;
    if(RoomChainRun.Failed)return false;
    Notify("已按原种子、阶数与分支从第一房重试");return !RoomChainRun.Failed;
   }
   catch(System.Exception error){IsDead=false;FailRoomGeneration("重试构建异常",error);return false;}
   finally{retryingRoomChain=false;changingZone=false;SelectedArenaMode=previousSelection;SelectedDungeonTier=previousTier;if(RoomChainRun==null)ChallengeRun=previousChallenge;UpdateTimeScale();}
  }
 }
}
