using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Emberfall
{
    public sealed class RunFailureEvidence
    {
        public readonly float DamageTaken,HealingReceived;
        public readonly int Room;
        public readonly string Objective;
        public RunFailureEvidence(float damage,float healing,int room,string objective)
        {DamageTaken=Finite(damage);HealingReceived=Finite(healing);Room=Math.Max(0,room);Objective=objective??"";}
        private static float Finite(float value){return float.IsNaN(value)||float.IsInfinity(value)?0:Math.Max(0,value);}
    }
    public sealed class RunRecapSnapshot
    {
        public readonly int EmberCreated,EmberEffective,FrostCreated,FrostEffective;
        public readonly bool Won, InDungeon, Challenge, PendingChest, FirstClearChoice;
        public readonly int Tier, Wave, TotalWaves, Seed, Materials, ExchangeCost, GoldLost;
        public readonly string LastDamageSource, ModeName, FailureReason, GenerationFailureDetail;
        public readonly int RewardGold, RewardExperience, RewardMaterials,RewardRefinementStones;
        public readonly bool RewardDetailsUnavailable;
        public readonly float LastDamageAmount;
        public readonly RunFailureEvidence Evidence;
        public readonly ReadOnlyCollection<KeyValuePair<string,int>> Actions;
        public readonly ReadOnlyCollection<string> Mechanics, Blessings;
        public RunRecapSnapshot(bool won, bool dungeon, bool challenge, int tier, int wave, int totalWaves, int seed,
            int materials, int exchangeCost, string damageSource, float damageAmount,
            IEnumerable<KeyValuePair<string,int>> actions, IEnumerable<string> mechanics, IEnumerable<string> blessings,
            bool pendingChest, bool firstClearChoice, int goldLost = 0, string modeName = null, string failureReason = null,
            int rewardGold = 0, int rewardExperience = 0, int rewardMaterials = 0,RunFailureEvidence evidence=null,int[] mechanismCounts=null,bool rewardDetailsUnavailable=false,string generationFailureDetail=null,int rewardRefinementStones=0)
        {
            EmberCreated=mechanismCounts!=null&&mechanismCounts.Length==4?mechanismCounts[0]:0;EmberEffective=mechanismCounts!=null&&mechanismCounts.Length==4?mechanismCounts[1]:0;FrostCreated=mechanismCounts!=null&&mechanismCounts.Length==4?mechanismCounts[2]:0;FrostEffective=mechanismCounts!=null&&mechanismCounts.Length==4?mechanismCounts[3]:0;
            RewardRefinementStones=rewardDetailsUnavailable?0:Math.Max(0,rewardRefinementStones);GenerationFailureDetail=generationFailureDetail??"";Evidence=evidence;RewardDetailsUnavailable=rewardDetailsUnavailable;
            Won=won; InDungeon=dungeon; Challenge=challenge; Tier=Math.Max(1,tier);
            TotalWaves=Math.Max(1,totalWaves); Wave=Math.Max(0,Math.Min(TotalWaves,wave)); Seed=seed;
            Materials=Math.Max(0,materials); ExchangeCost=Math.Max(1,exchangeCost); GoldLost=Math.Max(0,goldLost);
            LastDamageSource=damageSource??""; ModeName=modeName??""; FailureReason=failureReason??"";
            RewardGold=rewardDetailsUnavailable?0:Math.Max(0,rewardGold);RewardExperience=rewardDetailsUnavailable?0:Math.Max(0,rewardExperience);RewardMaterials=rewardDetailsUnavailable?0:Math.Max(0,rewardMaterials);
            LastDamageAmount=float.IsNaN(damageAmount)||float.IsInfinity(damageAmount)?0:Math.Max(0,damageAmount);
            Actions=new List<KeyValuePair<string,int>>(actions??new KeyValuePair<string,int>[0]).AsReadOnly();
            Mechanics=new List<string>(mechanics??new string[0]).AsReadOnly();
            Blessings=new List<string>(blessings??new string[0]).AsReadOnly();
            PendingChest=pendingChest; FirstClearChoice=firstClearChoice;
        }
    }

    // Presentation only: no combat, rewards, save changes, or parsing of prose logs.
    public sealed class RunRecapPresentation
    {
        public static int[] PositiveRewardIndices(long[] amounts,int limit=int.MaxValue)
        {
            var visible=new List<int>();if(amounts==null)return visible.ToArray();
            for(int i=0;i<Math.Min(amounts.Length,Math.Max(0,limit));i++)if(amounts[i]>0)visible.Add(i);
            return visible.ToArray();
        }
        public readonly RunRecapSnapshot Snapshot;
        public readonly KeyValuePair<string,int>[] Metrics, Rewards;
        public readonly string[] ExtraActions, Mechanics, Blessings, MechanismEvidence;
        public readonly string Tip, FailureLabel;
        public bool HasGenerationFailure { get { return !Snapshot.Won&&Snapshot.FailureReason=="GenerationOrPathFailure"; } }
        public string GenerationFailureDetails { get { return HasGenerationFailure?Snapshot.GenerationFailureDetail:""; } }
        public bool HasFailureBanner { get { return FailureLabel.Length>0&&Snapshot.FailureReason!="PlayerDefeated"&&Snapshot.FailureReason!="Death"; } }
        public bool HasDamage { get { return !Snapshot.Won && !HasFailureBanner && Snapshot.LastDamageAmount>0 && !string.IsNullOrWhiteSpace(Snapshot.LastDamageSource) && Snapshot.LastDamageSource!="未记录"; } }
        public bool HasProgress { get { return Snapshot.RewardDetailsUnavailable || Snapshot.Materials>0 || Snapshot.PendingChest || Snapshot.FirstClearChoice || Snapshot.GoldLost>0 || Rewards.Length>0; } }
        public float ExchangeProgress { get { return Math.Min(1f,Snapshot.Materials/(float)Snapshot.ExchangeCost); } }
        public RunRecapPresentation(RunRecapSnapshot snapshot)
        {
            Snapshot=snapshot??throw new ArgumentNullException(nameof(snapshot));
            FailureLabel=snapshot.Won?"":FailureText(snapshot.FailureReason);
            if(FailureLabel.Length>0&&snapshot.Evidence!=null&&snapshot.Evidence.Room>0)FailureLabel+=" · 第 "+snapshot.Evidence.Room+" 房/阶段";
            var rewards=new List<KeyValuePair<string,int>>();
            if(snapshot.RewardGold>0)rewards.Add(new KeyValuePair<string,int>("金币",snapshot.RewardGold));
            if(snapshot.RewardExperience>0)rewards.Add(new KeyValuePair<string,int>("经验",snapshot.RewardExperience));
            if(snapshot.RewardMaterials>0)rewards.Add(new KeyValuePair<string,int>("碎片",snapshot.RewardMaterials));
            Rewards=rewards.ToArray();
            var actions=new List<KeyValuePair<string,int>>();
            foreach(string key in new[]{"伤害总计","单次最大伤害","最大连击","承受伤害","闪避成功次数"})
            {
                int value=0;foreach(var action in snapshot.Actions)if(action.Key==key)value=Math.Max(0,action.Value);
                actions.Add(new KeyValuePair<string,int>(key,value));
            }
            Metrics=actions.ToArray();ExtraActions=new string[0]; Mechanics=Clean(snapshot.Mechanics); Blessings=Clean(snapshot.Blessings);
            MechanismEvidence=snapshot.EmberCreated+snapshot.FrostCreated==0?new string[0]:new[]{"烬地 · 生成 "+snapshot.EmberCreated+" / 生效 "+snapshot.EmberEffective,"霜环回响 · 生成 "+snapshot.FrostCreated+" / 生效 "+snapshot.FrostEffective};
            Tip=HasGenerationFailure?ChooseTip(snapshot):snapshot.EmberCreated>snapshot.EmberEffective?"烬地有未造成生命损失的实例；下次把落点放在敌人推进路线上。":snapshot.FrostCreated>snapshot.FrostEffective?"霜环回响有未造成生命损失的实例；下次留意回响延迟与敌人位置。":snapshot.Won?"":ChooseTip(snapshot);
        }
        private static int Priority(string action)
        {
            switch(action)
            {
                case "完美闪避":return 0; case "打断":return 1; case "职业能力":return 2;
                case "碎冰连招":case "毒层引爆":case "双契共鸣":case "剑卫反击":return 3;
                case "普攻回能":return 4; case "成功闪避":return 5; default:return 6;
            }
        }
        private static string[] Clean(IEnumerable<string> values)
        {
            var list=new List<string>();
            foreach(string value in values)if(!string.IsNullOrWhiteSpace(value)&&!list.Contains(value))list.Add(value);
            return list.ToArray();
        }
        private static string ChooseTip(RunRecapSnapshot snapshot)
        {
            if(snapshot.FailureReason=="GenerationOrPathFailure")
            {
                string cause=snapshot.GenerationFailureDetail;int line=cause.IndexOf('\n');
                if(line>=0)cause=cause.Substring(0,line);
                return (string.IsNullOrEmpty(cause)?"本次路线或生成异常":cause)+"；可原条件重试或返回营地重新进入。";
            }
            if((snapshot.FailureReason=="Timeout"||snapshot.FailureReason=="TimeExpired")&&snapshot.Evidence!=null&&snapshot.Evidence.Objective.Length>0)return "时限结束时："+snapshot.Evidence.Objective+"；优先推进该目标";
            if(snapshot.FailureReason=="TimeExpired"||snapshot.FailureReason=="Timeout")return snapshot.ModeName.Contains("守望")?"留在中心占领圈，先清理圈边敌人":"减少阶段间空档，优先清理远程敌人";
            if(snapshot.FailureReason=="SpawnBlocked")return "回营重新进入，生成新的来袭位置";
            if(snapshot.FailureReason=="Abandoned")return "";
            if(snapshot.Evidence!=null&&snapshot.Evidence.DamageTaken>0&&snapshot.Evidence.HealingReceived<=0&&snapshot.LastDamageAmount>0)
                return "本局实际受伤 "+snapshot.Evidence.DamageTaken.ToString("0")+"，未记录有效治疗；受伤后留意治疗机会";
            int dodge=0;
            foreach(var action in snapshot.Actions)if(action.Key=="完美闪避")dodge=action.Value;
            if(snapshot.LastDamageSource.Contains("弹幕")||snapshot.LastDamageSource.Contains("魔灵"))return "遇到远程攻击，横向闪避离开弹道";
            if(dodge==0)return "等预警临近命中，再向侧方闪避一次";
            return snapshot.InDungeon?"先选已通关阶数，保留闪避应对连招":"先回营补给，再挑战这类敌人";
        }
        private static string FailureText(string reason)
        {
            switch(reason)
            {
                case "Timeout":case "TimeExpired":return "时限已到";case "GenerationOrPathFailure":return "路线或生成异常";case "SpawnBlocked":return "来袭位置受阻";
                case "Death":case "PlayerDefeated":return "角色倒下";case "Abandoned":return "已结束挑战";
                case "":case "None":return "";default:return reason;
            }
        }
        public static string IconFor(string action)
        { return action.Contains("闪避")?"dodge":action=="普攻回能"||action=="打断"||action=="伤害总计"||action=="单次最大伤害"?"attack":action=="支线"?"confirm":"skills"; }
    }

    public sealed class RunRecapLayout
    {
        public struct Area
        {
            public readonly float X,Y,Width,Height;
            public Area(float x,float y,float width,float height){X=x;Y=y;Width=width;Height=height;}
            public bool Overlaps(Area other){return X<other.X+other.Width&&X+Width>other.X&&Y<other.Y+other.Height&&Y+Height>other.Y;}
        }
        public readonly Area Frame,Header,Viewport,Primary;
        public readonly int Columns;
        public readonly float Gap,MetricHeight,ContentWidth;
        public readonly bool Mobile;
        public RunRecapLayout(float width,float height,bool mobile)
        {
            Mobile=mobile;Gap=mobile?8:12;MetricHeight=mobile?52:88;
            width=Math.Max(280,width);height=Math.Max(220,height);
            float margin=mobile?8:20,frameWidth=Math.Min(mobile?920:960,width-margin*2),frameHeight=Math.Min(mobile?720:656,height-margin*2);
            float x=(width-frameWidth)*.5f,y=(height-frameHeight)*.5f,padding=mobile?16:24;
            float headerHeight=mobile?66:86,footerHeight=mobile?64:74;
            Frame=new Area(x,y,frameWidth,frameHeight);
            Header=new Area(x+padding,y+10,frameWidth-padding*2,headerHeight-14);
            Viewport=new Area(x+padding,y+headerHeight,frameWidth-padding*2,frameHeight-headerHeight-footerHeight);
            Primary=new Area(x+padding,y+frameHeight-footerHeight+10,frameWidth-padding*2,mobile?44:46);
            ContentWidth=Viewport.Width-14;
            Columns=mobile?(ContentWidth>=600?5:ContentWidth>=400?3:2):ContentWidth>=700?4:ContentWidth>=470?3:2;
        }
        public Area Metric(int index,float y)
        {
            float width=(ContentWidth-Gap*(Columns-1))/Columns;
            return new Area(index%Columns*(width+Gap),y+index/Columns*(MetricHeight+Gap),width,MetricHeight);
        }
        public float MetricRowsHeight(int count){return count<=0?0:((count+Columns-1)/Columns)*(MetricHeight+Gap)-Gap;}
    }
    public static class RunRecapChipLayout
    {
        public static RunRecapLayout.Area[] Pack(string[] labels,float availableWidth)
        {
            availableWidth=Math.Max(80,availableWidth);
            var result=new RunRecapLayout.Area[labels.Length];float x=0,y=0;
            for(int i=0;i<labels.Length;i++)
            {
                float textWidth=0;
                foreach(char c in labels[i]??"")textWidth+=c>255?14:8;
                float width=Math.Min(availableWidth,Math.Max(80,textWidth+24));
                if(x>0&&x+width>availableWidth){x=0;y+=38;}
                result[i]=new RunRecapLayout.Area(x,y,width,30);x+=width+8;
            }
            return result;
        }
        public static float Height(RunRecapLayout.Area[] chips)
        { return chips.Length==0?0:chips[chips.Length-1].Y+chips[chips.Length-1].Height; }
    }
}
