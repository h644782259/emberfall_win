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
            if(Id.StartsWith("refine-max/"))return p.refinementMaxCount;
            if(Id.StartsWith("refine/"))return p.refinementCount;
            if(Id.StartsWith("level/"))return p.level;
            if(Id.StartsWith("kills/"))return p.kills;
            if(Id.StartsWith("clears/"))return p.clearedRuns;
            if(Id.StartsWith("dungeon/"))return ProgressionService.HighestCompletedAdventureTier(p)*10;
            if(Id.StartsWith("gems/"))return p.attachments==null?0:p.attachments.Count;
            if(Id.StartsWith("fashion/"))return p.fashions==null?0:p.fashions.Count;
            if(Id.StartsWith("gear/")){int best=0;if(p.slotUpgradeRanks!=null)foreach(int rank in p.slotUpgradeRanks)best=Math.Max(best,rank);return best;}
            var parts=Id.Split('/');int key;if(parts.Length<2||!int.TryParse(parts[1],out key))return 0;
            if(parts[0]=="chapter-unlock")return p.level;
            if(parts[0]=="chapter-clear")return key>=0&&key<3&&(p.chapterCompletedMask&(1<<key))!=0?1:0;
            if(parts[0]=="chapter-band")return p.chapterBestLevels!=null&&key>=0&&key<p.chapterBestLevels.Length?p.chapterBestLevels[key]:0;
            if(parts[0]=="trial")
            {
                int mask=0;bool ability=false;
                if((int)p.heroClass==key){mask=p.tutorialMask;ability=p.classTutorialCompleted;}
                else if(p.classStates!=null&&key<p.classStates.Length&&p.classStates[key]!=null){mask=p.classStates[key].tutorialMask;ability=p.classStates[key].classTutorialCompleted;}
                int step=int.Parse(parts[2]);return step==2?(ability?1:0):((mask&(1<<step))!=0?1:0);
            }
            var gem=p.attachments==null?null:p.attachments.Find(a=>(int)a.mechanic==key);if(gem==null)return 0;
            return parts[0]=="core"?1:parts[0]=="upgrade"?gem.upgradeRank:parts[0]=="variant"?(gem.variantUnlocked?1:0):parts[0]=="ascend"?Math.Max(0,gem.ascensionRank):0;
        }
    }
    public partial class ProgressionService
    {
        private static readonly AchievementDefinition[] BasicAchievements={
            new AchievementDefinition("level/10","初露锋芒 · 达到10级",0,10,100,2),
            new AchievementDefinition("level/20","渐入佳境 · 达到20级",0,20,200,3),
            new AchievementDefinition("level/30","独当一面 · 达到30级",0,30,300,4),
            new AchievementDefinition("level/40","历练有成 · 达到40级",0,40,450,6),
            new AchievementDefinition("level/50","身经百战 · 达到50级",0,50,600,8),
            new AchievementDefinition("level/60","炉火纯青 · 达到60级",0,60,750,10),
            new AchievementDefinition("level/70","勇往直前 · 达到70级",0,70,900,12),
            new AchievementDefinition("level/80","百炼成钢 · 达到80级",0,80,1100,14),
            new AchievementDefinition("level/90","登峰在望 · 达到90级",0,90,1300,15),
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
            new AchievementDefinition("dungeon/20","遗迹初探 · 通关Lv20副本",2,20,200,3),
            new AchievementDefinition("dungeon/30","遗迹深入 · 通关Lv30副本",2,30,300,4),
            new AchievementDefinition("dungeon/40","遗迹破阵 · 通关Lv40副本",2,40,400,5),
            new AchievementDefinition("dungeon/50","深入遗迹 · 通关Lv50副本",2,50,500,6),
            new AchievementDefinition("dungeon/60","遗迹探路者 · 通关Lv60副本",2,60,650,8),
            new AchievementDefinition("dungeon/70","遗迹破阵者 · 通关Lv70副本",2,70,850,10),
            new AchievementDefinition("dungeon/80","遗迹先行者 · 通关Lv80副本",2,80,1050,12),
            new AchievementDefinition("dungeon/90","遗迹攀登者 · 通关Lv90副本",2,90,1250,14),
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
            foreach(int count in new[]{1,10,50,100})all.Add(new AchievementDefinition("refine/"+count,"精雕细琢 · 洗练"+count+"次",4,count,count*20,2));
            all.Add(new AchievementDefinition("refine-max/1","尽善尽美 · 洗练1件装备至数值上限",4,1,500,5));
            for(int node=0;node<3;node++)
            {
                var chapter=(ChapterNode)node;int unlock=ChapterProgression.UnlockLevel(chapter);string name=ChapterDefinition.Get(chapter).Name;
                all.Add(new AchievementDefinition("chapter-unlock/"+node,"星路开启 · "+name+"（"+unlock+"级）",2,unlock,unlock*10,2));
                all.Add(new AchievementDefinition("chapter-clear/"+node,"星路初捷 · 通关"+name,2,1,500,4));
                for(int level=unlock;level<=100;level+=10)
                    all.Add(new AchievementDefinition("chapter-band/"+node+"/"+level,name+" · 通关Lv"+level,2,level,level*10,4));
            }
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
                    foreach(int rank in new[]{3,6,9})all.Add(new AchievementDefinition("upgrade/"+id+"/"+rank,title+" · 升至"+rank+"阶",4,rank,rank*50,2));
                    foreach(int stage in new[]{2,3})all.Add(new AchievementDefinition("ascend/"+id+"/"+stage,title+" · 完成"+stage+"次升华",4,stage,stage*100,3));
                    if(BuildCatalog.HasMechanicVariant(gem))all.Add(new AchievementDefinition("variant/"+id,title+" · 解锁机制形态",4,1,50,1));
                    all.Add(new AchievementDefinition("ascend/"+id,title+" · 完成首次升华",4,1,50,1));
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
        public bool ClaimAllAchievements()
        {
            if(IsPracticeOnly)return Fail("当前模式不能领取成就奖励。");
            var candidate=Snapshot();
            if(candidate.achievementReceipts==null)candidate.achievementReceipts=new System.Collections.Generic.List<string>();
            int count=0;
            foreach(var a in Achievements)
            {
                if(AchievementClaimed(a.Id)||a.Progress(Profile)<a.Target)continue;
                if(candidate.gold>MaximumGold-a.Gold||candidate.mechanicMaterials>999999-a.Shards)return Fail("货币已达上限，请消耗后再领取。");
                candidate.achievementReceipts.Add(a.Id);candidate.gold+=a.Gold;candidate.mechanicMaterials+=a.Shards;count++;
            }
            if(count==0)return Fail("暂无可领取奖励。");
            return CommitCandidate(candidate,true);
        }
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
        public static int HighestCompletedAdventureTier(GameProfile p)
        {
            int best=Math.Max(p.highestAdventureTier,p.bestFloor);
            if(p.adventureBestTiers!=null)foreach(int tier in p.adventureBestTiers)best=Math.Max(best,tier);
            if(p.chapterBestTiers!=null)foreach(int tier in p.chapterBestTiers)best=Math.Max(best,tier);
            if(p.chapterBestLevels!=null)foreach(int level in p.chapterBestLevels)best=Math.Max(best,level/10);
            return Math.Max(0,Math.Min(10,best));
        }
        private static ProgressionGoalState AutomaticGoal(GameProfile p,bool inCamp)
        {
            for(int i=0;i<3;i++)
            {
                if((p.chapterCompletedMask&(1<<i))!=0)continue;
                var node=(ChapterNode)i;int required=ChapterProgression.UnlockLevel(node);
                if(p.level<required)
                    return new ProgressionGoalState{Identity="main/level/"+required,Title="升至 "+required+" 级 · 解锁"+ChapterDefinition.Get(node).Name,Step="挑战副本或原野敌人积累经验 · 当前 "+p.level+" 级",RequiredAdventureTier=Math.Max(1,Math.Min(10,p.level/10))};
                return new ProgressionGoalState{Identity="main/chapter/"+i,Title="通关章节 · "+ChapterDefinition.Get(node).Name,Step=ChapterDefinition.Get(node).StoryIntro+" · 点击前往章节入口"};
            }
            int best=HighestCompletedAdventureTier(p);
            if(best<10)
                return new ProgressionGoalState{Identity="main/tier/"+(best+1),Title="通关 Lv"+AdventureRewardRules.DungeonLevel(best+1)+" 副本",RequiredAdventureTier=best+1,Step="逐级通关，推进至 Lv100 副本 · 点击前往"};
            return new ProgressionGoalState{Identity="main/complete",Title="主线目标已完成",Step="重访星路章节，挑战更高难度与收藏成就",Done=true};
        }
        // Legacy callers no longer grant automatic rewards; achievements are claimed explicitly.
        public bool AdvanceAutomaticGrowth(){return true;}
        public bool ResumeAutomaticGrowth()
        {var candidate=Snapshot();candidate.automaticGrowth=true;return CommitCandidate(candidate,true);}
    }
}
