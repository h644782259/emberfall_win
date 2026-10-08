using System;
using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameSession
    {
        private string pendingDungeonRewardId;
        private int pendingDungeonRewardTier,pendingDungeonRewardGold,pendingDungeonRewardExperience;
        public bool DungeonRewardPending {get{return !string.IsNullOrEmpty(pendingDungeonRewardId);}}
        public bool DungeonResultsReady {get{return HasStarted&&InDungeon&&DungeonCleared&&!DungeonRewardPending&&Progression!=null&&!Progression.Profile.pendingFashionChest&&!Progression.Profile.pendingChestReveal;}}
        public bool CombatEnded {get{return (PracticeActive&&PracticeRecord!=null&&PracticeRecord.Finished)||ModeFinished||(InDungeon&&DungeonCleared);}}
        // A cleared ordinary dungeon remains explorable until the player leaves.
        // Damage and rewards still use CombatEnded; new cast visuals may finish normally.
        public bool CombatEffectsEnded {get{return CombatEnded&&!(InDungeon&&DungeonCleared&&!ModeFinished&&!PracticeActive);}}
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
            var source=Progression;string slot=source.CurrentSlotId,receipt=pendingDungeonRewardId;
            if(!Progression.TryCompleteDungeonRun(pendingDungeonRewardId,pendingDungeonRewardTier,pendingDungeonRewardGold,pendingDungeonRewardExperience,true))
            {Notify(Progression.LastError);return false;}
            if(Progression!=source||source.CurrentSlotId!=slot||pendingDungeonRewardId!=receipt)return true;
            pendingDungeonRewardId=null;
            ApplyRewardPresentation(receipt);
            LastRunSummary=BuildRunSummary(true);
            LogSystem(RewardPresentationText("遗迹结算已保存"));
            return true;
        }
        private void ClearDungeonSettlement()
        {pendingDungeonRewardId=null;pendingDungeonRewardTier=pendingDungeonRewardGold=pendingDungeonRewardExperience=0;}
    }
}
