using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameSession
    {
        public bool CanChallengeNextTier {get{return HasStarted&&InDungeon&&!ChapterActive&&!IsDead&&DungeonCleared&&DungeonTier<100&&!changingZone&&!ModeRewardPending;}}
        public bool ChallengeNextTier()
        {
            if(!CanChallengeNextTier||Paused||pauseState.BackgroundPaused)return false;
            // Settle the single saved chest before acquiring another qualification.
            // A failed freeze/write leaves the completed world available for retry.
            if(Progression.Profile.pendingFashionChest){Progression.OpenDungeonChest();if(Progression.Profile.pendingFashionChest){Notify(Progression.LastError);return false;}}
            if(Progression.Profile.pendingChestReveal&&!Progression.AcknowledgeChestReward()){Notify(Progression.LastError);return false;}
            int next=Mathf.Min(DungeonTier+1,MaximumDungeonTier),previous=SelectedDungeonTier;SelectedDungeonTier=next;
            try {if(!ChangeZone(true)){SelectedDungeonTier=previous;return false;}}
            catch(System.Exception error){if(RoomChainRun!=null)FailRoomGeneration("下一阶构建异常",error);else Notify("下一阶生成异常："+error.GetType().Name);return false;}
            if(RoomChainRun!=null&&RoomChainRun.Failed)return false;
            Notify("直接挑战第 "+DungeonTier+" 阶 · "+ModeName);return true;
        }
        public bool CanChallengeNextChapterTier
        {get{return HasStarted&&InDungeon&&ChapterFinished&&!ChapterRun.Failed&&!ChapterRewardPending&&!IsDead&&chapterReceipt!=null&&chapterReceipt.Tier<MaximumDungeonTier&&!changingZone;}}
        public bool ChallengeNextChapterTier()
        {
            if(!CanChallengeNextChapterTier||Paused||BackgroundPaused)return false;
            ChapterNode node=ActiveChapterNode;ChapterDifficulty difficulty=ActiveChapterDifficulty;
            int tier=chapterReceipt.Tier+1;bool limited=ChallengeRun;
            ReturnToCamp();
            if(InDungeon||!IsInCamp)return false;
            SelectedChapterNode=node;SelectedChapterDifficulty=difficulty;
            SelectedChapterTier=tier;SelectedChapterLimitedHealing=limited;
            return ConfirmChapterEnter();
        }
    }
}
