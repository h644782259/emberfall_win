using System.Globalization;
namespace Emberfall
{
    public static class ChapterEntryPresentation
    {
        public static string DifficultyName(ChapterDifficulty difficulty)
        {return difficulty==ChapterDifficulty.Hard?"困难":difficulty==ChapterDifficulty.Heroic?"英雄":"普通";}
        public static string Story(ChapterNode node)
        {
            var definition=ChapterDefinition.Get(node);
            return "观星员的线索 · "+definition.StoryIntro+"\n目标 · "+definition.Mechanic+"\n完成后 · "+definition.Outcome+"\n下一线索 · "+definition.NextClue;
        }
        public static string TierEffect(ChapterNode node)
        {return node==ChapterNode.StarPlatform?"在当前最高阶通关星台，可解锁下一阶挑战。":"通关普通难度后可挑战困难，通关困难后可挑战英雄。";}
        public static string UnlockHint(ChapterNode node)
        {return node==ChapterNode.Redrock?"通关林庭复明后解锁":node==ChapterNode.StarPlatform?"通关赤岩断供后解锁":"第一段星路 · 随时出发";}
        public static string DifficultyHint(ChapterDifficulty difficulty,bool unlocked)
        {return unlocked?(difficulty==ChapterDifficulty.Normal?"标准敌人强度":"生命 ×"+ChapterDefinition.HealthMultiplier(difficulty).ToString("0.##",CultureInfo.InvariantCulture)+" · 伤害 ×"+ChapterDefinition.DamageMultiplier(difficulty).ToString("0.##",CultureInfo.InvariantCulture)):
            difficulty==ChapterDifficulty.Hard?"先通关本节点的普通难度":"先通关本节点的困难难度";}
        public static int RewardMaterials(GameProfile profile,ChapterNode node,ChapterDifficulty difficulty,int tier)
        {
            int bit=ChapterProgression.DifficultyRewardBit(node,difficulty);
            return ChapterProgression.CompletionMaterials(profile,node,tier)+
                (bit!=0&&(profile.chapterDifficultyRewardMask&bit)==0?ChapterProgression.DifficultyFirstRewardMaterials:0);
        }
        public static string RewardBreakdown(GameProfile profile,ChapterNode node,ChapterDifficulty difficulty,int tier)
        {
            int repeat=ChapterProgression.MaterialReward(node,tier),total=ChapterProgression.CompletionMaterials(profile,node,tier);
            int bonus=RewardMaterials(profile,node,difficulty,tier)-total;
            return "通关 "+repeat+(total>repeat?"  +  首次通关 1":"")+(bonus>0?"  +  难度首通 "+bonus:"")+"\n完成挑战后获得奖励";
        }
        public static string MasteryProgress(GameProfile profile,ChapterNode node,ChapterDifficulty difficulty)
        {
            if(profile==null||difficulty==ChapterDifficulty.Normal||(profile.chapterCompletedMask&(1<<(int)node))==0)return "";
            int mask=profile.chapterMasteryMask,required=node==ChapterNode.ForestCourt?1:node==ChapterNode.Redrock?2:12;
            string goal=node==ChapterNode.ForestCourt?"首房至少2敌仍存活时完成双封印":node==ChapterNode.Redrock?"首房其余5敌仍存活时击败断供目标，再成功撤离":
                "打断首领 "+((mask&4)!=0?"✓":"○")+" / 亲自破锚制造暴露 "+((mask&8)!=0?"✓":"○")+"（可分次完成）";
            bool earned=(mask&required)==required;int tier=int.MaxValue;
            for(int i=0;i<4;i++)if((required&(1<<i))!=0)tier=System.Math.Min(tier,profile.chapterMasteryTiers!=null&&profile.chapterMasteryTiers.Length>i?profile.chapterMasteryTiers[i]:0);
            string badge=node==ChapterNode.ForestCourt?"双印行者":node==ChapterNode.Redrock?"断供猎手":"星台破局者";
            return "\n可选精通 · "+goal+"。达成目标并完成挑战，获得纪念徽记与称号。"+
                (earned?"\n徽记 / 称号「"+badge+"」 · 记录第 "+tier+" 阶":"\n徽记 / 称号「"+badge+"」尚未取得");
        }
        public static string Preview(GameProfile profile,ChapterNode node,ChapterDifficulty difficulty,int tier,bool limited)
        {
            string health=ChapterDefinition.HealthMultiplier(difficulty).ToString("0.##",CultureInfo.InvariantCulture);
            string damage=ChapterDefinition.DamageMultiplier(difficulty).ToString("0.##",CultureInfo.InvariantCulture);
            int repeat=ChapterProgression.MaterialReward(node,tier),total=ChapterProgression.CompletionMaterials(profile,node,tier);
            int rewardBit=ChapterProgression.DifficultyRewardBit(node,difficulty);
            int firstDifficulty=rewardBit!=0&&(profile.chapterDifficultyRewardMask&rewardBit)==0?ChapterProgression.DifficultyFirstRewardMaterials:0;
            return DifficultyName(difficulty)+" · 敌人生命 ×"+health+" / 伤害 ×"+damage+"\n"+
                ChapterDefinition.DifficultyMechanic(node,difficulty)+"\n"+
                "难度解锁 · 通关普通后解锁困难，通关困难后解锁英雄。\n"+

                "完成奖励 "+(total+firstDifficulty)+" 碎片（通关 "+repeat+(total>repeat?" + 首次通关1":"")+(firstDifficulty>0?" + 难度首通4":"")+"）。\n"+
                "首次通关困难或英雄难度，各额外获得4碎片；每段星路的首通奖励仅领取一次。\n"+
                (!profile.firstClearRewardClaimed?(profile.pendingFirstClearReward?"首通宝石待领取：返回营地领取。\n":"首次通关全部三段星路，可在营地领取首通宝石。\n"):"")+TierEffect(node)+MasteryProgress(profile,node,difficulty);
        }
        private static string MechanismReport(ChapterResultSnapshot result)
        {
            if(result.EmberCreated+result.FrostCreated==0)return "";
            return "\n机制战绩\n烬地 · 释放 "+result.EmberCreated+" / 命中 "+result.EmberEffective+
                "\n霜环回响 · 释放 "+result.FrostCreated+" / 命中 "+result.FrostEffective+
                (result.EmberCreated>result.EmberEffective?"\n下次将烬地落点放在敌人推进路线上。":result.FrostCreated>result.FrostEffective?"\n下次留意霜环回响延迟与敌人位置。":"");
        }
        public static string Result(ChapterResultSnapshot result)
        {
            if(result==null)return "章节记录暂不可用";
            string text=ChapterDefinition.Get(result.Node).Name+" · "+DifficultyName(result.Difficulty)+" · 第 "+result.Tier+" 阶\n"+
                "携带药剂 "+result.EntryPotions+"\n\n";
            if(result.Failed)return text+"止步房间 "+(result.Room+1)+" / "+ChapterDefinition.RoomCount(result.Node)+"\n"+
                (result.Node==ChapterNode.ForestCourt?"封印 "+result.Seals+"/2 · 一 "+result.FirstSealSeconds.ToString("0.0")+"s · 二 "+result.SecondSealSeconds.ToString("0.0")+"s\n":"")+
                "最后受击："+(string.IsNullOrEmpty(result.LastHit)?"未记录":result.LastHit)+" · "+result.LastHitAmount.ToString("0.#")+"\n"+
                (string.IsNullOrEmpty(result.Failure)?"本次挑战未完成。":result.Failure)+"\n本次击杀经验 +"+result.KillExperience+"；未发通关经验。\n节点与难度未解锁；回营重试。"+MechanismReport(result);
            if(!result.Saved)return text+"挑战完成 · 进度尚未保存\n请重试保存，以领取奖励并记录通关进度。"+MechanismReport(result);
            if(result.RewardDetailsUnavailable)return text+"通关进度与奖励已保存，可返回营地继续冒险。"+MechanismReport(result);
            text=ChapterDefinition.Get(result.Node).Name+" · "+DifficultyName(result.Difficulty)+" · 第 "+result.Tier+" 阶\n奖励已保存";
            if(result.FirstCoreAvailable)text+="\n首通宝石已可领取：返回营地领取";
            if(result.UnlockedNode>=0)text+="\n新节点："+ChapterDefinition.Get((ChapterNode)result.UnlockedNode).Name;
            if(result.UnlockedDifficulty>=0)text+="\n本节点新难度："+DifficultyName((ChapterDifficulty)result.UnlockedDifficulty);
            if(result.SharedAfter>result.SharedBefore)text+="\n已解锁第 "+result.SharedAfter+" 阶挑战";
            return text;
        }
        public static string Result(ChapterNode node,bool failed,bool pending)
        {
            if(failed)return "本次没有完成节点，故事与难度进度未推进。\n可以回营整备，再次挑战。";
            if(pending)return "挑战完成，进度尚未保存。\n请重试保存，以领取奖励并记录通关进度。";
            var definition=ChapterDefinition.Get(node);
            return definition.Outcome+"\n\n下一线索 · "+definition.NextClue+"\n\n"+TierEffect(node)+"\n已解锁节点可回营后独立重玩。";
        }
    }
}
