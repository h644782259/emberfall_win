using System;
using System.Collections.Generic;
using Emberfall;

public static class RunRecapPresentationTests
{
    private static int checks;
    private static void Check(bool condition,string message){checks++;if(!condition)throw new Exception(message);}
    private static RunRecapSnapshot Snapshot(bool won=false,int materials=0,string source="未记录",float damage=0,
        IEnumerable<KeyValuePair<string,int>> actions=null,IEnumerable<string> mechanics=null,IEnumerable<string> blessings=null,int lost=0,bool rewards=false)
    { return new RunRecapSnapshot(won,true,false,11,2,3,321,materials,12,source,damage,actions,mechanics,blessings,rewards,rewards,lost); }
    public static string Run()
    {
        checks=0;
        var actualInstances=new[]{6,4,6,0};
        var measured=new RunRecapSnapshot(true,true,false,1,3,3,0,1,12,null,0,null,null,null,false,false,mechanismCounts:actualInstances);
        actualInstances[1]=6;var measuredView=new RunRecapPresentation(measured);
        Check(measured.EmberCreated==6&&measured.EmberEffective==4&&measured.FrostCreated==6&&measured.FrostEffective==0,"mechanism result snapshots copy actual instance counts");
        Check(measuredView.MechanismEvidence.Length==2&&measuredView.MechanismEvidence[0].Contains("生成 6 / 生效 4")&&measuredView.MechanismEvidence[1].Contains("生成 6 / 生效 0"),"structured ordinary recap distinguishes generated and effective instances");
        Check(measuredView.Tip.Contains("未造成生命损失")&&!measuredView.Tip.Contains("秒"),"one factual mechanic retry suggestion without invented timings");
        var empty=new RunRecapPresentation(Snapshot());
        Check(empty.Metrics.Length==0 && empty.ExtraActions.Length==0 && empty.Mechanics.Length==0 && empty.Blessings.Length==0,"hide empty sections");
        Check(!empty.HasDamage && !empty.HasProgress,"do not fabricate hit/reward rows");
        Check(!string.IsNullOrEmpty(empty.Tip)&&!empty.Tip.Contains("\n"),"one concise defeat tip");
        var actions=new List<KeyValuePair<string,int>>
        {
            new KeyValuePair<string,int>("普攻回能",12),new KeyValuePair<string,int>("职业能力",3),
            new KeyValuePair<string,int>("打断",2),new KeyValuePair<string,int>("完美闪避",1),
            new KeyValuePair<string,int>("碎冰连招",4),new KeyValuePair<string,int>("失败计数",0),
            new KeyValuePair<string,int>("坏记录",-4),new KeyValuePair<string,int>("换装",3)
        };
        var mechanics=new List<string>{"霜环回响","霜环回响","",null};
        var snapshot=Snapshot(false,7,"星蚀巨像",412,actions,mechanics,new[]{"闪避震荡","目标疗愈"},50,true);
        actions.Clear();mechanics.Clear();
        var data=new RunRecapPresentation(snapshot);
        Check(data.Metrics.Length==4 && data.Metrics[0].Key=="完美闪避" && data.Metrics[0].Value==1,"recorded metrics prioritized, not invented");
        Check(data.ExtraActions.Length==1 && data.ExtraActions[0]=="普攻回能 12","extra positive action retained as concise chip");
        Check(data.Mechanics.Length==1 && data.Blessings.Length==2,"copy snapshot and clean empty/duplicate chips");
        Check(data.HasDamage && data.Snapshot.LastDamageAmount==412 && data.HasProgress,"actual loss and last hit preserved");
        Check(Math.Abs(data.ExchangeProgress-7f/12)<.0001f,"actual exchange progress");
        Check(new RunRecapPresentation(Snapshot(true,999)).ExchangeProgress==1,"progress bar capped without changing material count");
        Check(new RunRecapPresentation(Snapshot(true)).Tip=="","no extra victory prose");
        Check(!new RunRecapPresentation(Snapshot(true,0,"敌人",50)).HasDamage,"do not call prior hit a victory defeat cause");
        Check(!new RunRecapPresentation(Snapshot(false,0,"敌人",float.NaN)).HasDamage,"invalid amount cannot display fabricated value");
        Check(new RunRecapPresentation(Snapshot(false,0,"魔灵",50)).Tip.Contains("横向"),"specific ranged retry tip");
        var negative=new RunRecapSnapshot(false,false,false,-2,-1,0,0,-6,0,null,float.PositiveInfinity,null,null,null,false,false,-2);
        Check(negative.Materials==0&&negative.ExchangeCost==1&&negative.GoldLost==0&&negative.LastDamageAmount==0,"invalid counters safely normalized");
        Check(negative.Wave==0&&negative.TotalWaves==1&&negative.Tier==1,"safe stage bounds");
        var unknown=new RunRecapPresentation(new RunRecapSnapshot(true,true,false,1,3,3,0,0,12,null,0,null,null,null,false,false,rewardGold:999,rewardExperience:999,rewardMaterials:999,rewardDetailsUnavailable:true));
        Check(unknown.Snapshot.RewardDetailsUnavailable&&unknown.HasProgress&&unknown.Rewards.Length==0,"legacy receipt keeps visible unknown state without fabricated reward rows");
        var modeWin=new RunRecapSnapshot(true,true,false,20,3,3,77,12,12,"旧受击",42,null,null,null,false,false,0,"烬河突围","None",500,0,2);
        var modeData=new RunRecapPresentation(modeWin);
        Check(modeData.Snapshot.ModeName=="烬河突围"&&modeData.Rewards.Length==2,"mode name and only actual positive grants retained");
        Check(modeData.Rewards[0].Key=="金币"&&modeData.Rewards[0].Value==500&&modeData.Rewards[1].Key=="碎片","zero XP at level cap is not fabricated");
        var timeout=new RunRecapPresentation(new RunRecapSnapshot(false,true,false,1,2,3,0,0,12,"旧受击",50,null,null,null,false,false,0,"守望林庭","TimeExpired"));
        Check(timeout.HasFailureBanner&&timeout.FailureLabel=="时限已到"&&!timeout.HasDamage,"timeout is not misrepresented as old damage death");
        Check(timeout.Tip.Contains("占领圈")&&timeout.Rewards.Length==0,"specific hold objective tip without failure rewards");
        var arenaDeath=new RunRecapPresentation(new RunRecapSnapshot(false,true,false,1,1,3,0,0,12,"星蚀巨像",90,null,null,null,false,false,7,"蚀星斗场","PlayerDefeated"));
        Check(arenaDeath.HasDamage&&!arenaDeath.HasFailureBanner&&arenaDeath.Snapshot.GoldLost==7,"real player defeat preserves last hit/loss card");
        var clampedReward=new RunRecapPresentation(new RunRecapSnapshot(true,true,false,1,3,3,0,0,12,null,0,null,null,null,false,false,0,"烬河突围",null,-4,-1,-2));
        Check(clampedReward.Rewards.Length==0&&!clampedReward.HasProgress,"negative or zero reward fields cannot make fake grant rows");
        foreach(bool mobile in new[]{true,false})
        foreach(var size in new[]{new[]{320f,568f},new[]{568f,320f},new[]{640f,360f},new[]{730f,375f},new[]{812f,375f},new[]{844f,390f},new[]{1024f,768f},new[]{1366f,1024f},new[]{1920f,1080f}})
        {
            var layout=new RunRecapLayout(size[0],size[1],mobile);
            Check(layout.Frame.X>=0&&layout.Frame.Y>=0&&layout.Frame.X+layout.Frame.Width<=size[0]&&layout.Frame.Y+layout.Frame.Height<=size[1],"window within safe canvas");
            Check(layout.Viewport.Height>0&&layout.Primary.Height>=44,"scroll body and touch target remain usable");
            Check(!layout.Viewport.Overlaps(layout.Primary)&&!layout.Header.Overlaps(layout.Primary),"fixed footer never overlaps scrolling cards");
            for(int i=0;i<4;i++)
            {
                var card=layout.Metric(i,0);
                Check(card.X>=0&&card.X+card.Width<=layout.ContentWidth+.01f,"metric horizontal bounds");
                for(int j=0;j<i;j++)Check(!card.Overlaps(layout.Metric(j,0)),"metric cards do not overlap");
            }
            var chips=RunRecapChipLayout.Pack(new[]{"冰霜","长名字的机制装备","闪避震荡 9999","断势回流","第三项自定义祝福","新增祝福"},layout.ContentWidth);
            foreach(var chip in chips)Check(chip.Width>0&&chip.X>=0&&chip.X+chip.Width<=layout.ContentWidth+.01f,"chips wrap inside content");
            for(int i=0;i<chips.Length;i++)for(int j=0;j<i;j++)Check(!chips[i].Overlaps(chips[j]),"wrapped chips do not overlap");
            Check(RunRecapChipLayout.Height(chips)>=30,"dynamic chip block height");
        }
        return "PASS: "+checks+" recap presentation/layout assertions";
    }
}
