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
        {return "宝箱随机获得装备"+(AdventureRewardRules.ChestFashion(mode,false)?"、时装":"")+"或一组材料"+(AdventureRewardRules.ChestAffixReforge(mode,false)?" · 可掉词条重铸石":"")+" · 装备史诗起步 · 传说装备4%";}
        public static string GoalFit(GameProfile profile,ProgressionGoalState goal,int mode,int tier)
        {
            if(profile==null||goal==null)return "收集装备、时装与整备材料";
            if(goal.RequiredAdventureTier>0)return "主线推进 · "+(tier>=goal.RequiredAdventureTier?"挑战当前目标副本":"通关后继续向更高等级副本推进");
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
