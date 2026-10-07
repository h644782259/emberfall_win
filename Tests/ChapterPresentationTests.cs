using System;
using Emberfall;
public static class ChapterPresentationTests
{
    static int checks;
    static void Check(bool value,string reason){checks++;if(!value)throw new Exception(reason);}
    public static string Run()
    {
        checks=0;var p=new GameProfile();
        foreach(ChapterNode node in Enum.GetValues(typeof(ChapterNode)))
        {
            var d=ChapterDefinition.Get(node);string story=ChapterEntryPresentation.Story(node);
            Check(story.Contains(d.StoryIntro)&&story.Contains(d.Outcome)&&story.Contains(d.NextClue),"shared cause, outcome and next clue");
            Check(ChapterEntryPresentation.TierEffect(node).Contains(node==ChapterNode.StarPlatform?"解锁下一阶":"通关困难后可挑战英雄"),"tier and difficulty unlocks use player language");
            foreach(int tier in new[]{1,4,5,9,10,19,20,39,40,100})
            foreach(ChapterDifficulty diff in Enum.GetValues(typeof(ChapterDifficulty)))
            foreach(bool limited in new[]{false,true})
            {
                p.chapterFirstRewardMask=0;p.chapterDifficultyRewardMask=0;
                string first=ChapterEntryPresentation.Preview(p,node,diff,tier,limited);
                int repeat=ChapterProgression.MaterialReward(node,tier);
                Check(ChapterEntryPresentation.RewardMaterials(p,node,diff,tier)==repeat+1+(diff==ChapterDifficulty.Normal?0:4),"compact reward card matches first-clear grant");
                Check(ChapterEntryPresentation.RewardBreakdown(p,node,diff,tier).Contains("首次通关 1"),"compact reward breakdown separates first completion");
                Check(first.Contains("完成奖励 "+(repeat+1+(diff==ChapterDifficulty.Normal?0:4))+" 碎片")&&first.Contains("首次通关1"),"first reward follows shared tier bands plus one fixed shard");
                Check(first.Contains(ChapterDefinition.DifficultyMechanic(node,diff)),"exact production difficulty mechanic described");
                Check(first.Contains("各额外获得4碎片")&&first.Contains("首次通关全部三段星路")&&!first.Contains("旧副本")&&!first.Contains("旧档")&&!first.Contains("合法")&&!first.Contains("共享最高阶"),"player rules explain rewards and unlocks without implementation notes");
                Check(first.Contains(limited?"初始3次":"携带药剂"),"healing rules are independent");
                p.chapterFirstRewardMask=1<<(int)node;p.chapterDifficultyRewardMask=63;
                string replay=ChapterEntryPresentation.Preview(p,node,diff,tier,limited);
                Check(ChapterEntryPresentation.RewardMaterials(p,node,diff,tier)==repeat&&!ChapterEntryPresentation.RewardBreakdown(p,node,diff,tier).Contains("首次通关"),"compact replay reward never re-promises consumed bonuses");
                Check(replay.Contains("完成奖励 "+repeat+" 碎片")&&!replay.Contains("首次通关1"),"replay does not promise first shard again");
            }
            Check(!ChapterEntryPresentation.Result(node,false,true).Contains(d.Outcome)&&!ChapterEntryPresentation.Result(node,true,false).Contains(d.NextClue),"failed/pending outcomes never claim story success");
            Check(ChapterEntryPresentation.Result(node,false,false).Contains(d.Outcome)&&ChapterEntryPresentation.Result(node,false,false).Contains(d.NextClue),"committed completion displays shared consequences");
        }
        Check(ChapterEntryPresentation.Preview(p,ChapterNode.ForestCourt,ChapterDifficulty.Hard,1,false).Contains("生命 ×1.2 / 伤害 ×1.15"),"hard multipliers match actual core values");
        Check(ChapterEntryPresentation.Preview(p,ChapterNode.ForestCourt,ChapterDifficulty.Heroic,1,false).Contains("生命 ×1.35 / 伤害 ×1.25"),"heroic multipliers match actual core values");
        Check(ChapterDefinition.DifficultyMechanic(ChapterNode.StarPlatform,ChapterDifficulty.Hard).Contains("追加无新锚，只能打断"),"boss follow-up preview matches actual counter opportunities");
        p.pendingFirstClearReward=true;
        Check(ChapterEntryPresentation.Preview(p,ChapterNode.ForestCourt,ChapterDifficulty.Normal,1,false).Contains("首通核心待领取：返回营地领取"),"pending first core offers the actual next action");
        p.firstClearRewardClaimed=true;
        Check(!ChapterEntryPresentation.Preview(p,ChapterNode.StarPlatform,ChapterDifficulty.Normal,1,false).Contains("首通核心"),"claimed shared core not advertised again");
        return "PASS: "+checks+" chapter presentation, shared definitions and reward-boundary checks";
    }
}
