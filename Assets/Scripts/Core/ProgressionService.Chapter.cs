using System;
namespace Emberfall
{
    public partial class ProgressionService
    {
        private ChapterRunReceipt chapterAttempt;
        private ChapterExperienceBudget chapterExperience;
        public int ChapterTotalExperience {get{return chapterExperience==null?0:chapterExperience.TotalExperience;}}
        public int ChapterCompletionExperience {get{return chapterExperience==null?0:chapterExperience.CompletionExperience;}}
        public int ChapterKillExperienceEarned {get{return chapterExperience==null?0:chapterExperience.EarnedKillExperience;}}
        public int ChapterExperienceEntryLevel {get{return chapterExperience==null?0:chapterExperience.EntryLevel;}}
        public bool RegisterChapterEnemy(ChapterRunReceipt receipt,int room,int index,bool boss)
        {return receipt!=null&&ReferenceEquals(receipt,chapterAttempt)&&receipt.SavePath==SaveFilePath&&chapterExperience!=null&&chapterExperience.Register(room,index,boss);}
        public bool TryClaimChapterEnemyExperience(ChapterRunReceipt receipt,int room,int index,out int experience)
        {experience=0;return receipt!=null&&ReferenceEquals(receipt,chapterAttempt)&&receipt.SavePath==SaveFilePath&&chapterExperience!=null&&chapterExperience.TryClaim(room,index,out experience);}
        public bool TryBeginChapterNode(ChapterNode node,ChapterDifficulty difficulty,int tier,out ChapterRunReceipt receipt)
        {
            receipt=null;
            if(!HasActiveSave||!ChapterProgression.CanEnter(Profile,node,difficulty)||tier<1||tier>UnlockedChapterTier(node)||Profile.chapterRewardSequence==long.MaxValue)return Fail("章节或难度尚未解锁。");
            int materials=ChapterProgression.CompletionMaterials(Profile,node,tier);
            receipt=new ChapterRunReceipt(node,difficulty,tier,materials,Profile.chapterRewardSequence+1,SaveFilePath);
            receipt.MasteryEligible=difficulty!=ChapterDifficulty.Normal&&(Profile.chapterCompletedMask&(1<<(int)node))!=0;
            chapterAttempt=receipt;chapterExperience=new ChapterExperienceBudget(node,Profile.level);LastError=string.Empty;return true;
        }
        internal void RecordChapterMastery(ChapterRunReceipt receipt,int evidence)
        {
            if(receipt==null||!ReferenceEquals(receipt,chapterAttempt)||receipt.SavePath!=SaveFilePath||!receipt.MasteryEligible)return;
            int allowed=receipt.Node==ChapterNode.ForestCourt?1:receipt.Node==ChapterNode.Redrock?2:12;
            receipt.MasteryEvidence|=evidence&allowed;
        }
        public void CancelChapterRun(){chapterAttempt=null;chapterExperience=null;}
        public bool TryCompleteChapterNode(ChapterRunReceipt receipt)
        {
            if(receipt==null||receipt.SavePath!=SaveFilePath)return Fail("章节结算已失效。");
            if(receipt.Sequence==Profile.chapterRewardSequence&&receipt.Id==Profile.lastChapterRewardId){LastError=string.Empty;return true;}
            if(!ReferenceEquals(receipt,chapterAttempt)||receipt.Sequence!=Profile.chapterRewardSequence+1||!ChapterProgression.CanEnter(Profile,receipt.Node,receipt.Difficulty))return Fail("章节结算已失效。");
            if(chapterExperience==null||!chapterExperience.AllRegistered)return Fail("章节敌人经验登记不完整，不能结算。");
            GameProfile candidate=Snapshot();ChapterProgression.Normalize(candidate);int oldLevel=candidate.level;
            long experience=(long)candidate.xp+chapterExperience.CompletionExperience;
            while(candidate.level<MaximumLevel&&experience>=GameBalance.XpToNext(candidate.level))
            {experience-=GameBalance.XpToNext(candidate.level);candidate.level++;candidate.skillPoints += GameBalance.SkillPointsGainedAtLevel(candidate.level);}
            candidate.xp=candidate.level>=MaximumLevel?0:(int)experience;
            int index=(int)receipt.Node,bit=1<<index;
            if(receipt.MasteryEligible)
            {
                candidate.chapterMasteryMask|=receipt.MasteryEvidence;
                for(int i=0;i<4;i++)if((receipt.MasteryEvidence&(1<<i))!=0)candidate.chapterMasteryTiers[i]=Math.Max(candidate.chapterMasteryTiers[i],receipt.Tier);
            }
            ChapterProgression.BackfillDifficultyRewards(candidate);
            ChapterProgression.GrantDifficultyRewards(candidate, ChapterProgression.DifficultyRewardBit(receipt.Node, receipt.Difficulty));
            candidate.chapterRevision=1;candidate.chapterCompletedMask|=bit;candidate.chapterFirstRewardMask|=bit;
            candidate.chapterHighestDifficulties[index]=Math.Max(candidate.chapterHighestDifficulties[index],(int)receipt.Difficulty+1);
            candidate.mechanicMaterials=(int)Math.Min(999999L,(long)candidate.mechanicMaterials+receipt.Materials);
            candidate.chapterBestTiers[index]=Math.Max(candidate.chapterBestTiers[index],receipt.Tier);
            if(candidate.pendingFashionChest||candidate.pendingChestReveal)return Fail("请先收下已有宝箱。");
            NewChestQualification(candidate,Math.Min(100,receipt.Tier+(int)receipt.Difficulty*5),Guid.NewGuid().ToString("N"));candidate.pendingChestMode=index==0?0:index==1?1:2;
            candidate.chapterRewardSequence=receipt.Sequence;candidate.lastChapterRewardId=receipt.Id;
            if(receipt.Node==ChapterNode.StarPlatform)
            {
                candidate.pendingFirstClearReward=!candidate.firstClearRewardClaimed;
                if(candidate.highestAdventureTier>candidate.chapterHighestAdventureTier)candidate.chapterPriorAdventureTier=Math.Max(candidate.chapterPriorAdventureTier,candidate.highestAdventureTier);
                candidate.chapterHighestAdventureTier=Math.Max(candidate.chapterHighestAdventureTier,receipt.Tier);
                candidate.highestAdventureTier=Math.Max(candidate.highestAdventureTier,receipt.Tier);
            }
            var detail=CaptureRewardPresentation(receipt.Id,Profile,candidate);
            detail.FirstCompletion=(Profile.chapterCompletedMask&bit)==0;
            detail.FirstCoreAvailable=!Profile.pendingFirstClearReward&&candidate.pendingFirstClearReward;
            detail.SharedBefore=HighestAdventureTier;detail.SharedAfter=candidate.highestAdventureTier;
            int highest=ChapterProgression.HighestCompletedDifficulty(candidate,receipt.Node);
            detail.UnlockedDifficulty=highest>ChapterProgression.HighestCompletedDifficulty(Profile,receipt.Node)&&highest<2?highest+1:-1;
            detail.UnlockedNode=index<2&&!ChapterProgression.IsUnlocked(Profile,(ChapterNode)(index+1))&&ChapterProgression.IsUnlocked(candidate,(ChapterNode)(index+1))?index+1:-1;
            candidate.lastChapterRewardDetails=detail;
            if(!CommitCandidate(candidate))return false;
            for(int level=oldLevel+1;level<=candidate.level;level++)RaiseLeveledUp(level);
            return true;
        }
    }

    // One fixed character-entry-level budget per attempt. Registration spans both rooms;
    // escaping past an enemy forfeits only its death share, never reallocates that share.
    internal sealed class ChapterExperienceBudget
    {
        private readonly ChapterNode node;
        private readonly bool[] registered,claimed;
        private int registeredCount,registeredDeathShares;
        public int EntryLevel {get;private set;}
        public int TotalExperience {get;private set;}
        public int EarnedKillExperience {get;private set;}
        public bool AllRegistered {get{return registeredCount==registered.Length;}}
        public int CompletionExperience {get{return AllRegistered?TotalExperience-registeredDeathShares:0;}}
        internal ChapterExperienceBudget(ChapterNode node,int level)
        {
            if(!ChapterProgression.Valid(node))throw new ArgumentOutOfRangeException(nameof(node));
            this.node=node;EntryLevel=Math.Max(1,Math.Min(100,level));
            registered=new bool[node==ChapterNode.StarPlatform?3:12];claimed=new bool[registered.Length];
            for(int i=0;i<registered.Length;i++)TotalExperience+=Standard(node==ChapterNode.StarPlatform&&i==0);
        }
        private int Standard(bool boss){return boss?100+EntryLevel*12:22+EntryLevel*2;}
        private int Slot(int room,int index)
        {return node==ChapterNode.StarPlatform?(room==0&&index>=0&&index<3?index:-1):(room>=0&&room<2&&index>=0&&index<6?room*6+index:-1);}
        public bool Register(int room,int index,bool boss)
        {
            int slot=Slot(room,index);if(slot<0||registered[slot]||boss!=(node==ChapterNode.StarPlatform&&slot==0))return false;
            registered[slot]=true;registeredCount++;registeredDeathShares+=Standard(boss)*3/10;return true;
        }
        public bool TryClaim(int room,int index,out int experience)
        {
            experience=0;int slot=Slot(room,index);if(slot<0||!registered[slot]||claimed[slot])return false;
            claimed[slot]=true;experience=Standard(node==ChapterNode.StarPlatform&&slot==0)*3/10;EarnedKillExperience+=experience;return true;
        }
    }
}
