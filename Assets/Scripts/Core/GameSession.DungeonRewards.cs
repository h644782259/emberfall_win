using System;
using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameSession
    {
        private string pendingDungeonRewardId;
        private int pendingDungeonRewardTier,pendingDungeonRewardGold,pendingDungeonRewardExperience;
        public bool DungeonRewardPending {get{return !string.IsNullOrEmpty(pendingDungeonRewardId);}}
        public bool CombatEnded {get{return (PracticeActive&&PracticeRecord!=null&&PracticeRecord.Finished)||ModeFinished||(InDungeon&&DungeonCleared);}}
        private void QueueDungeonCompletion()
        {
            if(DungeonRewardPending)return;
            pendingDungeonRewardId=Guid.NewGuid().ToString("N");
            pendingDungeonRewardTier=DungeonTier;
            pendingDungeonRewardGold=120+DungeonTier*30;
            if(HasBlessing(RunBlessing.RiskContract))pendingDungeonRewardGold=Mathf.RoundToInt(pendingDungeonRewardGold*1.3f);
            pendingDungeonRewardExperience=100+DungeonTier*20;
        }
        public bool TrySettleDungeonReward()
        {
            if(!DungeonRewardPending)return true;
            if(!HasStarted||Progression==null)return false;
            int gold=Progression.Profile.gold,materials=Progression.Profile.mechanicMaterials;
            long xp=TotalEarnedExperience(Progression.Profile);
            if(!Progression.TryCompleteDungeonRun(pendingDungeonRewardId,pendingDungeonRewardTier,pendingDungeonRewardGold,pendingDungeonRewardExperience))
            {Notify(Progression.LastError);return false;}
            pendingDungeonRewardId=null;
            modeGoldReward=Mathf.Max(0,Progression.Profile.gold-gold);
            modeXpReward=(int)Math.Max(0,TotalEarnedExperience(Progression.Profile)-xp);
            modeMaterialReward=Mathf.Max(0,Progression.Profile.mechanicMaterials-materials);
            LastRunSummary=BuildRunSummary(true);
            LogSystem("遗迹结算已保存 · +"+modeGoldReward+"金币 · +"+modeXpReward+"经验 · +"+modeMaterialReward+"碎片");
            return true;
        }
        private void ClearDungeonSettlement()
        {pendingDungeonRewardId=null;pendingDungeonRewardTier=pendingDungeonRewardGold=pendingDungeonRewardExperience=0;}
    }
}
