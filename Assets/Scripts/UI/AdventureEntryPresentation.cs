using System;
namespace Emberfall
{
    // Guaranteed clear rewards are separate from probabilistic enemy equipment.
    public static class AdventureEntryPresentation
    {
        public static int Materials(int mode,int tier)
        {
            if(mode < -1 || mode > 3)throw new ArgumentOutOfRangeException(nameof(mode));
            return AdventureRewardRules.MaterialsMinimum(mode,tier);
        }
        public static string RewardLine(int mode,int tier)
        {return AdventureRewardRules.EquipmentSummary(mode,tier)+" · "+Materials(mode,tier)+"～"+AdventureRewardRules.MaterialsMaximum(mode,tier)+"碎片 · "+(mode==-1?"外观宝箱":"无外观宝箱");}
        public static string GoalFit(GameProfile profile,ProgressionGoalState goal,int mode,int tier)
        {
            if(profile==null||goal==null||profile.progressionGoal==ProgressionGoalKind.None&&!profile.automaticGrowth)return mode==-1?"外观收集可选遗迹；碎片用于营地整备":"碎片整备；本模式不产外观宝箱";
            if(goal.Done)return goal.Step;
            if(goal.MaterialCost>0)return "保底"+Materials(mode,tier)+"碎片 · "+(string.IsNullOrEmpty(goal.Requirements)?goal.ResourceRequirements(profile,0):goal.Requirements);
            if(goal.GoldCost>0)return string.IsNullOrEmpty(goal.Requirements)?goal.ResourceRequirements(profile,0):goal.Requirements;
            if(profile.progressionGoal==ProgressionGoalKind.Tier)return "目标第"+profile.progressionGoalTier+"阶 · "+(tier>=profile.progressionGoalTier?"本次通关可达成":"本次用于逐阶推进");
            if(profile.progressionGoal==ProgressionGoalKind.SecondPreset)return "收集配装后回营地保存两套方案";
            if(profile.progressionGoal==ProgressionGoalKind.ClassTutorial)return "实战练习职业循环；通关不自动完成";
            return goal.Step;
        }
        public static string EncounterLine(int mode)
        {
            switch(mode)
            {
                case -1:return "三波 / 终局首领 · 敌人随机装备";
                case 0:return "守点清敌 · 碎片整备 / 随机装备";
                case 1:return "限时窄桥 · 星烬强化 / 随机装备";
                case 2:return "三首领连战 · 首领物资 / 随机装备";
                case 3:return "五房远征 · 机制探索 / 随机装备";
                default:throw new ArgumentOutOfRangeException(nameof(mode));
            }
        }
    }
}
