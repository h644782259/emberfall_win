using System;
namespace Emberfall
{
    public enum ChapterNode { ForestCourt=0, Redrock=1, StarPlatform=2 }
    public enum ChapterDifficulty { Normal=0, Hard=1, Heroic=2 }
    public sealed class ChapterDefinition
    {
        public readonly string Name,Story,Mechanic,Outcome,NextClue;
        public readonly int BaseMaterials;
        public string StoryIntro {get{return Story;}}
        private ChapterDefinition(string name,int materials,string story,string mechanic,string outcome,string next){Name=name;BaseMaterials=materials;Story=story;Mechanic=mechanic;Outcome=outcome;NextClue=next;}
        static readonly ChapterDefinition[] Nodes={
            new ChapterDefinition("林庭复明",1,"净化林庭残火，沿旧径撤离。","先净化，再寻找出口","林庭封印重归稳定，撤离的通路已经打开。","循残余火迹前往赤岩，寻找封锁线的供能者。"),
            new ChapterDefinition("赤岩断供",1,"截断赤岩守军，穿越封锁线。","先猎杀，再突破封锁","赤岩供能被切断，星台的外缘防线出现缺口。","穿过封锁线，在入口确认整备后直面守望者。"),
            new ChapterDefinition("星台封印",2,"在入口确认整备，直入星台迎战守望者。","直入首领场，击败守望者与护卫","守望者退去，第一章的星路重新连通。","可以重访三处节点，挑战已解锁的更高阶数。")};
        public static ChapterDefinition Get(ChapterNode node){if(!ChapterProgression.Valid(node))throw new ArgumentOutOfRangeException(nameof(node));return Nodes[(int)node];}
        public static int RoomCount(ChapterNode node){Get(node);return node==ChapterNode.StarPlatform?1:2;}
        public static RoomObjective RoomKind(ChapterNode node,int index)
        {Get(node);if(index<0||index>=RoomCount(node))throw new ArgumentOutOfRangeException(nameof(index));return node==ChapterNode.ForestCourt?(index==0?RoomObjective.Purify:RoomObjective.Escape):node==ChapterNode.Redrock?(index==0?RoomObjective.Hunt:RoomObjective.Escape):RoomObjective.Boss;}
        public static string DifficultyMechanic(ChapterNode node,ChapterDifficulty difficulty)
        {
            Get(node);
            if((int)difficulty<0||(int)difficulty>2)throw new ArgumentOutOfRangeException(nameof(difficulty));
            if(node==ChapterNode.ForestCourt)return difficulty==ChapterDifficulty.Normal?"两点净化后撤离，无护援":
                "金环护援使6米内可见同伴减伤30%；引开、遮挡或击杀可断供"+(difficulty==ChapterDifficulty.Heroic?"；单个2.1米荆棘脉冲圈，可绕行":"");
            if(node==ChapterNode.Redrock)return difficulty==ChapterDifficulty.Normal?"猎杀金环目标后撤离，无护援":
                "守岗敌人形成交火，需要选择突破路线"+(difficulty==ChapterDifficulty.Heroic?"；单条热涌短线受墙阻挡，可绕端点":"");
            return difficulty==ChapterDifficulty.Normal?"70%与35%血量阶段的单束扫射，破坏安全锚可制造暴露":
                "首段有效打断进入2秒恢复、无易伤；破坏全部锚进入6秒暴露、受伤+35%，两者均取消本阶段追加。未反制则扫射恢复后追加：2.2秒预警锁向、4秒单束扫射、2秒恢复；追加无新锚，只能打断"+(difficulty==ChapterDifficulty.Heroic?"；扫向、起始角与安全锚位置按本局种子变化，始终单束":"");
        }
        public static float HealthMultiplier(ChapterDifficulty difficulty){return difficulty==ChapterDifficulty.Hard?1.2f:difficulty==ChapterDifficulty.Heroic?1.35f:1f;}
        public static float DamageMultiplier(ChapterDifficulty difficulty){return difficulty==ChapterDifficulty.Hard?1.15f:difficulty==ChapterDifficulty.Heroic?1.25f:1f;}
    }
    public static class ChapterProgression
    {
        public static bool Valid(ChapterNode node){return (int)node>=0&&(int)node<3;}
        public static int UnlockLevel(ChapterNode node){return Valid(node)?30+(int)node*10:101;}
        public static int LevelTier(int level){return Math.Max(1,Math.Min(10,level/10));}
        public static int AvailableTier(GameProfile profile,ChapterNode node,ChapterDifficulty difficulty=ChapterDifficulty.Heroic)
        {
            if(difficulty!=ChapterDifficulty.Heroic)return 1;
            int best=CompletedTier(profile,node,difficulty);return best==int.MaxValue?int.MaxValue:best+1;
        }
        public static int CompletedTier(GameProfile profile,ChapterNode node,ChapterDifficulty difficulty)
        {
            if(profile==null||!Valid(node)||(int)difficulty<0||(int)difficulty>2)return 0;
            int index=(int)node*3+(int)difficulty;
            int best=profile.chapterDifficultyBestTiers!=null&&profile.chapterDifficultyBestTiers.Length>index?Math.Max(0,profile.chapterDifficultyBestTiers[index]):0;
            if(difficulty==ChapterDifficulty.Heroic&&profile.chapterDifficultyBestTiers!=null)
                for(int d=0;d<3;d++){int legacy=(int)node*3+d;if(legacy<profile.chapterDifficultyBestTiers.Length)best=Math.Max(best,profile.chapterDifficultyBestTiers[legacy]);}
            return best;
        }
        public static ChapterDifficulty AvailableDifficulty(GameProfile profile,ChapterNode node)
        {return LevelDifficulty(node,profile==null?0:profile.level);}
        public static bool CanEnter(GameProfile profile,ChapterNode node,ChapterDifficulty difficulty,int tier)
        {return CanEnter(profile,node,difficulty)&&tier>=1&&tier<=AvailableTier(profile,node,difficulty);}
        public static float TierHealthMultiplier(int tier)
        {double step=Math.Max(1,tier)-1d;return (float)(1d+.009d*step+.000035d*step*step);}
        public static float TierDamageMultiplier(int tier)
        {double step=Math.Max(1,tier)-1d;return (float)(1d+.014d*step+.00008d*step*step);}
        public static ChapterDifficulty LevelDifficulty(ChapterNode node,int level)
        {return ChapterDifficulty.Heroic;}
        public static bool IsUnlocked(GameProfile profile,ChapterNode node){return profile!=null&&Valid(node)&&profile.level>=UnlockLevel(node);}
        public static int HighestCompletedDifficulty(GameProfile profile,ChapterNode node)
        {return profile==null||!Valid(node)||profile.chapterHighestDifficulties==null||profile.chapterHighestDifficulties.Length<3?-1:Math.Max(-1,Math.Min(2,profile.chapterHighestDifficulties[(int)node]-1));}
        public static bool CanEnter(GameProfile profile,ChapterNode node,ChapterDifficulty difficulty)
        {return IsUnlocked(profile,node)&&difficulty==ChapterDifficulty.Heroic;}
        public static int MaterialReward(ChapterNode node,int tier){return TierRewardBand.Materials(ChapterDefinition.Get(node).BaseMaterials,tier);}
        public static int CompletionMaterials(GameProfile profile,ChapterNode node,int tier)
        {return MaterialReward(node,tier)+((profile.chapterFirstRewardMask&(1<<(int)node))==0?1:0);}
        public const int DifficultyFirstRewardMaterials = 4;
        public static int DifficultyRewardBit(ChapterNode node, ChapterDifficulty difficulty)
        { return Valid(node) && (difficulty == ChapterDifficulty.Hard || difficulty == ChapterDifficulty.Heroic)
            ? 1 << ((int)node * 2 + (int)difficulty - 1) : 0; }
        // Legacy service admitted only highest+1 at both begin and completion. Thus a
        // valid highest Heroic proves Hard; absent/invalid fields are never evidence.
        internal static bool BackfillDifficultyRewards(GameProfile profile)
        {
            if (profile.chapterDifficultyRewardRevision >= 1) return false;
            int proven = 0;
            for (int n = 0; n < 3; n++)
            {
                if ((profile.chapterCompletedMask & (1 << n)) == 0) continue;
                int highest = profile.chapterHighestDifficulties != null && n < profile.chapterHighestDifficulties.Length
                    ? profile.chapterHighestDifficulties[n] : 0;
                if (highest == 2 || highest == 3) proven |= DifficultyRewardBit((ChapterNode)n, ChapterDifficulty.Hard);
                if (highest == 3) proven |= DifficultyRewardBit((ChapterNode)n, ChapterDifficulty.Heroic);
            }
            GrantDifficultyRewards(profile, proven);
            profile.chapterDifficultyRewardRevision = 1;
            return true;
        }
        internal static void GrantDifficultyRewards(GameProfile profile, int proven)
        {
            int unpaid = proven & 63 & ~profile.chapterDifficultyRewardMask, count = 0;
            for (int i = 0; i < 6; i++) if ((unpaid & (1 << i)) != 0) count++;
            profile.mechanicMaterials = (int)Math.Min(999999L, (long)profile.mechanicMaterials + count * DifficultyFirstRewardMaterials);
            profile.chapterDifficultyRewardMask |= unpaid;
        }
        internal static void Normalize(GameProfile profile)
        {
            profile.chapterDifficultyRewardMask &= 63;
            profile.chapterDifficultyRewardRevision = Math.Max(0, Math.Min(1, profile.chapterDifficultyRewardRevision));
            profile.chapterMasteryMask&=15;
            var oldMastery=profile.chapterMasteryTiers;profile.chapterMasteryTiers=new int[4];
            for(int i=0;i<4;i++)if((profile.chapterMasteryMask&(1<<i))!=0)profile.chapterMasteryTiers[i]=Math.Max(1,oldMastery!=null&&i<oldMastery.Length?oldMastery[i]:1);
            profile.chapterRevision=Math.Max(0,Math.Min(1,profile.chapterRevision));
            var oldLevels=profile.chapterBestLevels;profile.chapterBestLevels=new int[3];
            for(int i=0;i<3;i++)profile.chapterBestLevels[i]=Math.Max(0,Math.Min(100,oldLevels!=null&&i<oldLevels.Length?oldLevels[i]:0));
            profile.chapterCompletedMask&=7;profile.chapterFirstRewardMask&=profile.chapterCompletedMask;
            var previous=profile.chapterHighestDifficulties;profile.chapterHighestDifficulties=new int[3];
            for(int i=0;i<3;i++)if((profile.chapterCompletedMask&(1<<i))!=0)profile.chapterHighestDifficulties[i]=previous!=null&&i<previous.Length&&previous[i]>=1&&previous[i]<=3?previous[i]:1;
            if(profile.chapterTierRevision>1)throw new ArgumentException("章节阶数版本不受支持，原文件保留。");
            var oldTiers=profile.chapterDifficultyBestTiers;profile.chapterDifficultyBestTiers=new int[9];
            for(int i=0;i<9;i++)profile.chapterDifficultyBestTiers[i]=Math.Max(0,oldTiers!=null&&i<oldTiers.Length?oldTiers[i]:0);
            if(profile.chapterTierRevision<1)
            {
                for(int node=0;node<3;node++)if((profile.chapterCompletedMask&(1<<node))!=0)
                {
                    int difficulty=Math.Max(0,profile.chapterHighestDifficulties[node]-1);
                    int best=profile.chapterBestTiers!=null&&node<profile.chapterBestTiers.Length?Math.Max(1,profile.chapterBestTiers[node]):1;
                    profile.chapterDifficultyBestTiers[node*3+difficulty]=Math.Max(profile.chapterDifficultyBestTiers[node*3+difficulty],best);
                }
                profile.chapterTierRevision=1;profile.chapterTierMigrationPending=true;
            }
            profile.chapterRewardSequence=Math.Max(0,profile.chapterRewardSequence);
            Guid receipt;profile.lastChapterRewardId=Guid.TryParseExact(profile.lastChapterRewardId,"N",out receipt)?receipt.ToString("N"):null;
            profile.chapterHighestAdventureTier=Math.Max(0,Math.Min(100,profile.chapterHighestAdventureTier));
            profile.chapterPriorAdventureTier=Math.Max(0,Math.Min(100,profile.chapterPriorAdventureTier));
        }
    }
    public sealed class ChapterRunReceipt
    {
        public ChapterNode Node {get;private set;} public ChapterDifficulty Difficulty {get;private set;}
        public int Tier {get;private set;} public int Materials {get;private set;}
        public string Id {get;private set;} public long Sequence {get;private set;}
        internal int MasteryEvidence;
        internal bool MasteryEligible;
        internal readonly string SavePath;
        internal ChapterRunReceipt(ChapterNode node,ChapterDifficulty difficulty,int tier,int materials,long sequence,string path)
        {Node=node;Difficulty=difficulty;Tier=tier;Materials=materials;Sequence=sequence;SavePath=path;Id=Guid.NewGuid().ToString("N");}
    }
}
