using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameSession
    {
        public bool CanChallengeNextTier {get{return HasStarted&&InDungeon&&!IsDead&&DungeonResultsReady&&!changingZone&&!ModeRewardPending&&!DungeonRewardPending&&(ChapterActive?CanAdvanceChapterTier:DungeonTier<MaximumDungeonTier);}}
        public bool ChallengeNextTier()
        {
            if(!CanChallengeNextTier||Paused||pauseState.BackgroundPaused)return false;
            if(ChapterActive)return RestartChapterAttempt(chapterRetryTier+1);
            int next=Mathf.Min(DungeonTier+1,MaximumDungeonTier),previous=SelectedDungeonTier;SelectedDungeonTier=next;
            try {if(!ChangeZone(true)){SelectedDungeonTier=previous;return false;}}
            catch(System.Exception error){if(RoomChainRun!=null)FailRoomGeneration("下一阶构建异常",error);else Notify("下一阶生成异常："+error.GetType().Name);return false;}
            if(RoomChainRun!=null&&RoomChainRun.Failed)return false;
            Notify("直接挑战第 "+DungeonTier+" 阶 · "+ModeName);return true;
        }
        public bool CanChallengeNextChapterTier {get{return ChapterActive&&CanChallengeNextTier;}}
        public bool ChallengeNextChapterTier(){return ChapterActive&&ChallengeNextTier();}
    }
}
