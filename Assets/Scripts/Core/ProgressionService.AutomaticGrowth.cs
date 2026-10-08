using System;
namespace Emberfall
{
    public partial class ProgressionService
    {
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
                    g.Identity=id;g.Title="获得挂件 · "+BuildCatalog.MechanicName(mechanic);g.Done=a!=null;g.ItemId=a==null?null:a.id;
                    bool first=p.pendingFirstClearReward&&!p.firstClearRewardClaimed;
                    g.Action=first?ProgressionGoalAction.ClaimCore:ProgressionGoalAction.ExchangeCore;g.MaterialCost=first?0:MechanicExchangeCost;
                    g.CanAct=inCamp&&(first||p.mechanicMaterials>=MechanicExchangeCost);g.Step=first?"首通自选可领取":"挑战副本收集碎片，再到营地兑换";return g;
                }
            }
            foreach(int tier in new[]{1,5})
            {
                string id=prefix+"tier/"+tier;if(p.growthRewardReceipts.Contains(id))continue;
                g.Identity=id;g.Title="通关第 "+tier+" 阶";g.RequiredAdventureTier=tier;g.Done=Math.Max(p.highestAdventureTier,p.bestFloor)>=tier;g.Step="点击定位传送门 · 任一冒险通关均可";return g;
            }
            foreach(var mechanic in BuildCatalog.MechanicsFor(p.heroClass))
            {
                var a=p.attachments.Find(x=>x.mechanic==mechanic);
                string id;
                id=prefix+"upgrade/"+(int)mechanic;
                if(!p.growthRewardReceipts.Contains(id))
                {g.Identity=id;g.ItemId=a==null?null:a.id;g.Title="升级挂件 · "+BuildCatalog.MechanicName(mechanic);g.Done=a!=null&&a.upgradeRank>0;g.Action=ProgressionGoalAction.UpgradeAttachment;g.MaterialCost=AttachmentUpgradeCost;g.CanAct=inCamp&&a!=null&&p.level>=6&&p.mechanicMaterials>=AttachmentUpgradeCost;g.Step="角色6级 · 消耗6碎片升阶，提升属性和机制强度";return g;}
                id=prefix+"variant/"+(int)mechanic;
                if(BuildCatalog.HasMechanicVariant(mechanic)&&!p.growthRewardReceipts.Contains(id))
                {g.Identity=id;g.ItemId=a==null?null:a.id;g.Title="解锁挂件变体";g.Done=a!=null&&a.variantUnlocked;g.Action=ProgressionGoalAction.UnlockVariant;g.MaterialCost=VariantCost;g.CanAct=inCamp&&a!=null&&p.mechanicMaterials>=VariantCost;g.Step="到营地花4碎片解锁；之后免费切换";return g;}
            }
            foreach(int tier in new[]{10,20})
            {
                string id=prefix+"tier/"+tier;if(p.growthRewardReceipts.Contains(id))continue;
                g.Identity=id;g.Title="通关第 "+tier+" 阶";g.RequiredAdventureTier=tier;g.Done=Math.Max(p.highestAdventureTier,p.bestFloor)>=tier;g.Step="点击定位传送门 · 任一冒险通关均可";return g;
            }
            if(!p.growthRewardReceipts.Contains(prefix+"plans"))
            {
                g.Identity=prefix+"plans";g.Title="保存第二套配装";g.Action=ProgressionGoalAction.OpenPresets;g.CanAct=inCamp;
                g.Done=p.buildPresets!=null&&p.buildPresets.Length>1&&p.buildPresets[0].populated&&p.buildPresets[1].populated;
                g.Step=g.Done?"方案 A / B 已保存，点击检查方案 B":"到营地保存方案 A / B，记录挂件与变体";return g;
            }
            foreach(var a in p.attachments)
            {
                if(BuildCatalog.MechanicClass(a.mechanic)!=p.heroClass)continue;
                string id=prefix+"ascend/"+(int)a.mechanic;if(p.growthRewardReceipts.Contains(id))continue;
                g.Identity=id;g.ItemId=a.id;g.Title="升华挂件 · "+BuildCatalog.MechanicName(a.mechanic);g.Done=a.rarity==Rarity.Legendary;g.Action=ProgressionGoalAction.Ascend;g.MaterialCost=AscensionCost;g.RequiredAdventureTier=5;g.CanAct=inCamp&&p.highestAdventureTier>=5&&p.mechanicMaterials>=AscensionCost;g.Step="营地消耗24碎片，保留变体与升阶";return g;
            }
            foreach(int tier in new[]{40,60,80,100})
            {
                string id=prefix+"tier/"+tier;if(p.growthRewardReceipts.Contains(id))continue;
                g.Identity=id;g.Title="通关第 "+tier+" 阶";g.RequiredAdventureTier=tier;g.Done=Math.Max(p.highestAdventureTier,p.bestFloor)>=tier;g.Step="点击定位传送门 · 任一冒险通关均可";return g;
            }
            g.Identity=prefix+"complete";g.Title="成长目标已完成";g.Step="自由探索、收藏与尝试更多配装";return g;
        }
        public bool AdvanceAutomaticGrowth()
        {
            if(IsPracticeOnly)return true;
            var candidate=Snapshot();int rewarded=0;
            string practice="growth/"+(int)candidate.heroClass+"/practice";
            if(candidate.classTutorialCompleted&&!candidate.growthRewardReceipts.Contains(practice))
            {candidate.growthRewardReceipts.Add(practice);candidate.gold=Math.Min(MaximumGold,candidate.gold+50);candidate.mechanicMaterials=Math.Min(999999,candidate.mechanicMaterials+1);rewarded++;}
            for(int i=0;candidate.automaticGrowth&&i<32;i++)
            {
                var goal=AutomaticGoal(candidate,false);if(!goal.Done)break;
                if(candidate.growthRewardReceipts.Contains(goal.Identity))break;
                candidate.growthRewardReceipts.Add(goal.Identity);candidate.gold=Math.Min(MaximumGold,candidate.gold+50);candidate.mechanicMaterials=Math.Min(999999,candidate.mechanicMaterials+1);rewarded++;
            }
            if(rewarded==0)return true;
            if(!CommitCandidate(candidate,true))return false;
            LastGrowthReward="完成 "+rewarded+" 个成长目标 · +"+(rewarded*50)+" 金币 / +"+rewarded+" 碎片";return true;
        }
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
