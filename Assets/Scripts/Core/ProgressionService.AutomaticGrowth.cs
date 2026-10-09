using System;
namespace Emberfall
{
    public sealed class AchievementDefinition
    {
        public readonly string Id,Title;public readonly int Category,Target,Gold,Shards;
        public AchievementDefinition(string id,string title,int category,int target,int gold,int shards)
        {Id=id;Title=title;Category=category;Target=target;Gold=gold;Shards=shards;}
        public int Progress(GameProfile p)
        {
            if(Id.StartsWith("level/"))return p.level;
            if(Id.StartsWith("kills/"))return p.kills;
            if(Id.StartsWith("clears/"))return p.clearedRuns;
            if(Id.StartsWith("dungeon/"))return Math.Min(100,Math.Max(p.highestAdventureTier,p.bestFloor)*10);
            if(Id.StartsWith("gems/"))return p.attachments==null?0:p.attachments.Count;
            if(Id.StartsWith("fashion/"))return p.fashions==null?0:p.fashions.Count;
            if(Id.StartsWith("gear/")){int best=0;if(p.slotUpgradeRanks!=null)foreach(int rank in p.slotUpgradeRanks)best=Math.Max(best,rank);return best;}
            var parts=Id.Split('/');int key;if(parts.Length<2||!int.TryParse(parts[1],out key))return 0;
            if(parts[0]=="trial")
            {
                int mask=0;bool ability=false;
                if((int)p.heroClass==key){mask=p.tutorialMask;ability=p.classTutorialCompleted;}
                else if(p.classStates!=null&&key<p.classStates.Length&&p.classStates[key]!=null){mask=p.classStates[key].tutorialMask;ability=p.classStates[key].classTutorialCompleted;}
                int step=int.Parse(parts[2]);return step==2?(ability?1:0):((mask&(1<<step))!=0?1:0);
            }
            var gem=p.attachments==null?null:p.attachments.Find(a=>(int)a.mechanic==key);if(gem==null)return 0;
            return parts[0]=="core"?1:parts[0]=="upgrade"?gem.upgradeRank:parts[0]=="variant"?(gem.variantUnlocked?1:0):parts[0]=="ascend"?(gem.rarity==Rarity.Legendary?1:0):0;
        }
    }
    public partial class ProgressionService
    {
        private static readonly AchievementDefinition[] BasicAchievements={
            new AchievementDefinition("level/10","初露锋芒 · 达到10级",0,10,100,2),
            new AchievementDefinition("level/30","独当一面 · 达到30级",0,30,300,4),
            new AchievementDefinition("level/50","身经百战 · 达到50级",0,50,600,8),
            new AchievementDefinition("level/100","巅峰之路 · 达到100级",0,100,1500,16),
            new AchievementDefinition("kills/100","初战告捷 · 击败100只怪物",1,100,100,2),
            new AchievementDefinition("kills/500","猎手 · 击败500只怪物",1,500,250,4),
            new AchievementDefinition("kills/2000","破阵者 · 击败2000只怪物",1,2000,600,8),
            new AchievementDefinition("kills/10000","万敌之锋 · 击败10000只怪物",1,10000,1500,16),
            new AchievementDefinition("clears/1","首次凯旋 · 通关1次副本",2,1,100,2),
            new AchievementDefinition("clears/10","常胜之旅 · 通关10次副本",2,10,300,4),
            new AchievementDefinition("clears/50","探索先锋 · 通关50次副本",2,50,800,8),
            new AchievementDefinition("clears/100","百战凯旋 · 通关100次副本",2,100,1500,16),
            new AchievementDefinition("dungeon/10","启程 · 通关Lv10副本",2,10,100,2),
            new AchievementDefinition("dungeon/50","深入遗迹 · 通关Lv50副本",2,50,500,6),
            new AchievementDefinition("dungeon/100","遗迹征服者 · 通关Lv100副本",2,100,1500,16),
            new AchievementDefinition("gems/1","第一颗宝石 · 收集1种宝石",3,1,100,2),
            new AchievementDefinition("gems/3","宝石匠 · 收集3种宝石",3,3,300,4),
            new AchievementDefinition("gems/9","宝石收藏家 · 收集9种宝石",3,9,800,8),
            new AchievementDefinition("fashion/1","焕然一新 · 收集1件时装",3,1,100,2),
            new AchievementDefinition("fashion/3","衣橱初成 · 收集3件时装",3,3,300,4),
            new AchievementDefinition("fashion/6","风格大师 · 收集6件时装",3,6,800,8)
        };
        public static readonly AchievementDefinition[] Achievements=CreateAchievements();
        private static AchievementDefinition[] CreateAchievements()
        {
            var all=new System.Collections.Generic.List<AchievementDefinition>(BasicAchievements);
            for(int hero=0;hero<4;hero++)
            {
                string name=GameBalance.ClassName((HeroClass)hero);
                string[] tasks={"普攻命中并回复能量","成功闪避一次预警攻击",new[]{"完美闪避后反击命中","碎冰命中或刷新灼烧","引爆三层毒素","召唤物命中目标"}[hero],"穿戴一件装备"};
                for(int step=0;step<4;step++)all.Add(new AchievementDefinition("trial/"+hero+"/"+step,name+" · "+tasks[step],1,1,step==2?50:25,1));
                foreach(var gem in BuildCatalog.MechanicsFor((HeroClass)hero))
                {
                    string id=((int)gem).ToString(),title=BuildCatalog.GemName(gem);
                    all.Add(new AchievementDefinition("core/"+id,"获得 "+title,3,1,50,1));
                    all.Add(new AchievementDefinition("upgrade/"+id,title+" · 升至1阶",4,1,50,1));
                    if(BuildCatalog.HasMechanicVariant(gem))all.Add(new AchievementDefinition("variant/"+id,title+" · 解锁机制形态",4,1,50,1));
                    all.Add(new AchievementDefinition("ascend/"+id,title+" · 达到传说品质",4,1,50,1));
                }
            }
            foreach(int rank in new[]{10,30,50})all.Add(new AchievementDefinition("gear/"+rank,"任一部位强化至 +"+rank,4,rank,rank*20,rank/5));
            return all.ToArray();
        }
        private static void MigrateAchievementReceipts(GameProfile p)
        {
            if(p.achievementReceipts==null)p.achievementReceipts=new System.Collections.Generic.List<string>();
            if(p.growthRewardReceipts==null)return;
            foreach(string receipt in p.growthRewardReceipts)
            {
                if(string.IsNullOrEmpty(receipt))continue;var parts=receipt.Split('/');if(parts.Length<3||parts[0]!="growth")continue;
                string id=null;
                if(parts[2]=="practice")id="trial/"+parts[1]+"/2";
                else if(parts.Length==4)
                {
                    if(parts[2]=="tier"){int tier;if(int.TryParse(parts[3],out tier)&&(tier==1||tier==5||tier==10))id="dungeon/"+(tier*10);}
                    else id=parts[2]+"/"+parts[3];
                }
                if(id!=null&&Array.Find(Achievements,a=>a.Id==id)!=null&&!p.achievementReceipts.Contains(id))p.achievementReceipts.Add(id);
            }
        }
        public bool AchievementClaimed(string id)
        {return Profile.achievementReceipts!=null&&Profile.achievementReceipts.Contains(id);}
        public int ClaimableAchievements
        {get{int count=0;foreach(var a in Achievements)if(!AchievementClaimed(a.Id)&&a.Progress(Profile)>=a.Target)count++;return count;}}
        public bool ClaimAchievement(string id)
        {
            if(IsPracticeOnly)return Fail("当前模式不能领取成就奖励。");
            var achievement=Array.Find(Achievements,a=>a.Id==id);
            if(achievement==null||AchievementClaimed(id))return Fail("奖励已领取或成就不存在。");
            if(achievement.Progress(Profile)<achievement.Target)return Fail("成就尚未完成。");
            if(Profile.gold>MaximumGold-achievement.Gold||Profile.mechanicMaterials>999999-achievement.Shards)return Fail("货币已达上限，请消耗后再领取。");
            var candidate=Snapshot();if(candidate.achievementReceipts==null)candidate.achievementReceipts=new System.Collections.Generic.List<string>();
            candidate.achievementReceipts.Add(id);candidate.gold+=achievement.Gold;candidate.mechanicMaterials+=achievement.Shards;
            return CommitCandidate(candidate,true);
        }
        public string LastGrowthReward {get;private set;}
        private static ProgressionGoalState AutomaticGoal(GameProfile p,bool inCamp)
        {
            string prefix="growth/"+(int)p.heroClass+"/";
            var g=new ProgressionGoalState();
            if(!p.growthRewardReceipts.Contains(prefix+"practice"))
            {g.Identity=prefix+"practice";g.Title="实战试炼 · 职业能力";g.Step="右上目标查看实战指引 · 职业能力："+CombatTrialProgress(p)+"/4";g.Done=p.classTutorialCompleted;return g;}
            foreach(var mechanic in BuildCatalog.MechanicsFor(p.heroClass))
            {
                var a=p.attachments.Find(x=>x.mechanic==mechanic);
                string id=prefix+"core/"+(int)mechanic;
                if(!p.growthRewardReceipts.Contains(id))
                {
                    g.Identity=id;g.Title="获得宝石 · "+BuildCatalog.GemName(mechanic);g.Done=a!=null;g.ItemId=a==null?null:a.id;
                    bool first=p.pendingFirstClearReward&&!p.firstClearRewardClaimed;
                    g.Action=first?ProgressionGoalAction.ClaimCore:ProgressionGoalAction.ExchangeCore;g.MaterialCost=first?0:MechanicExchangeCost;
                    g.CanAct=inCamp&&(first||p.mechanicMaterials>=MechanicExchangeCost);g.Step=first?"首通自选可领取":"挑战副本收集碎片，再到营地兑换";return g;
                }
            }
            foreach(int tier in new[]{1,5})
            {
                string id=prefix+"tier/"+tier;if(p.growthRewardReceipts.Contains(id))continue;
                g.Identity=id;g.Title="通关 Lv"+AdventureRewardRules.DungeonLevel(tier)+" 副本";g.RequiredAdventureTier=tier;g.Done=Math.Max(p.highestAdventureTier,p.bestFloor)>=tier;g.Step="点击选择副本 · 逐级通关解锁";return g;
            }
            foreach(var mechanic in BuildCatalog.MechanicsFor(p.heroClass))
            {
                var a=p.attachments.Find(x=>x.mechanic==mechanic);
                string id;
                id=prefix+"upgrade/"+(int)mechanic;
                if(!p.growthRewardReceipts.Contains(id))
                {g.Identity=id;g.ItemId=a==null?null:a.id;g.Title="升级宝石 · "+BuildCatalog.GemName(mechanic);g.Done=a!=null&&a.upgradeRank>0;g.Action=ProgressionGoalAction.UpgradeAttachment;g.MaterialCost=AttachmentUpgradeCost;g.CanAct=inCamp&&a!=null&&p.level>=6&&p.mechanicMaterials>=AttachmentUpgradeCost;g.Step="角色6级 · 消耗6碎片升阶，提升属性和机制强度";return g;}
                id=prefix+"variant/"+(int)mechanic;
                if(BuildCatalog.HasMechanicVariant(mechanic)&&!p.growthRewardReceipts.Contains(id))
                {g.Identity=id;g.ItemId=a==null?null:a.id;g.Title="解锁宝石变体";g.Done=a!=null&&a.variantUnlocked;g.Action=ProgressionGoalAction.UnlockVariant;g.MaterialCost=VariantCost;g.CanAct=inCamp&&a!=null&&p.mechanicMaterials>=VariantCost;g.Step="到营地花4碎片解锁；之后免费切换";return g;}
            }
            foreach(int tier in new[]{10})
            {
                string id=prefix+"tier/"+tier;if(p.growthRewardReceipts.Contains(id))continue;
                g.Identity=id;g.Title="通关 Lv"+AdventureRewardRules.DungeonLevel(tier)+" 副本";g.RequiredAdventureTier=tier;g.Done=Math.Max(p.highestAdventureTier,p.bestFloor)>=tier;g.Step="点击选择副本 · 逐级通关解锁";return g;
            }
            foreach(var a in p.attachments)
            {
                if(BuildCatalog.MechanicClass(a.mechanic)!=p.heroClass)continue;
                string id=prefix+"ascend/"+(int)a.mechanic;if(p.growthRewardReceipts.Contains(id))continue;
                g.Identity=id;g.ItemId=a.id;g.Title="升华宝石 · "+BuildCatalog.GemName(a.mechanic);g.Done=a.rarity==Rarity.Legendary;g.Action=ProgressionGoalAction.Ascend;g.MaterialCost=AscensionCost;g.RequiredAdventureTier=5;g.CanAct=inCamp&&p.highestAdventureTier>=5&&p.mechanicMaterials>=AscensionCost;g.Step="营地消耗24碎片，保留变体与升阶";return g;
            }
            g.Identity=prefix+"complete";g.Title="成长目标已完成";g.Step="自由探索、收藏与尝试更多配装";return g;
        }
        // Legacy callers no longer grant automatic rewards; achievements are claimed explicitly.
        public bool AdvanceAutomaticGrowth(){return true;}
        public bool ResumeAutomaticGrowth()
        {var candidate=Snapshot();candidate.automaticGrowth=true;return CommitCandidate(candidate,true);}
        private bool ExecuteAutomaticGoal(string identity,bool inCamp)
        {
            var goal=AutomaticGoal(Profile,inCamp);if(goal.ActionIdentity!=identity||!goal.CanAct)return Fail("成长目标已变化或条件尚未满足。");
            EquipmentMechanic mechanic=EquipmentMechanic.None;
            foreach(var m in BuildCatalog.MechanicsFor(Profile.heroClass))
                if(goal.Identity.EndsWith("/"+(int)m)&&goal.Identity.Contains("/core/"))mechanic=m;
            if(goal.ItemId!=null){var a=Profile.attachments.Find(x=>x.id==goal.ItemId);if(a!=null)mechanic=a.mechanic;}
            switch(goal.Action)
            {
                case ProgressionGoalAction.ClaimCore:return ClaimFirstClearReward(mechanic);
                case ProgressionGoalAction.ExchangeCore:return ExchangeMechanic(mechanic);
                case ProgressionGoalAction.UpgradeAttachment:return UpgradeAttachment(mechanic,inCamp);
                case ProgressionGoalAction.UnlockVariant:return ToggleAttachmentVariant(mechanic,inCamp);
                case ProgressionGoalAction.Ascend:return AscendAttachment(mechanic,inCamp);
                default:return Fail("点击目标卡定位对应地点。");
            }
        }
    }
}
