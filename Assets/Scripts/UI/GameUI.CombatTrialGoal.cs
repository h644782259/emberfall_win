using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private void DrawCombatTrialGoal(ref float y,float width,float unit,bool draw)
        {
            var p=session.Progression;
            GoalNode(ref y,width,unit,"实战试炼 · 目标的一种","进度沿用已有存档；达成职业练习的成长奖励只发放一次。",draw);
            string[] actions={"普攻命中，回复能量","躲过一次即将命中的预警攻击",p.ClassTutorialText,"在行囊换上一件装备"};
            for(int i=0;i<actions.Length;i++)
            {
                bool done=i==2?p.Profile.classTutorialCompleted:(p.Profile.tutorialMask&(1<<i))!=0;
                GoalParagraph(ref y,width,unit,(done?"✓ ":"○ ")+actions[i],14,done?jade:pale,false,draw);
            }
        }
        private void NavigateMerchantExchange()
        {
            if(!session.IsInCamp){ClosePanel();session.ReturnToCamp();if(!session.IsInCamp)return;}
            progressionGoalsOpen=false;merchantExchangeOpen=false;panel=Panel.None;session.SetUIBlocking(false);
            session.Player.Teleport(WorldTraversal.NearestWalkable(GameSession.HubNpcPosition(0),.45f));
            session.Notify("已定位商人，请对话进入宝石兑换。");
            BlockUITransition();
        }
        private void NavigateSmithAttachment(ProgressionGoalState goal)
        {
            if(!session.IsInCamp){ClosePanel();session.ReturnToCamp();if(!session.IsInCamp)return;}
            var p=session.Progression;
            var attachment=p.Profile.attachments.Find(a=>a.id==goal.ItemId);
            var gear=p.Profile.inventory.Find(item=>item.id==goal.ItemId);
            if(attachment!=null)smithSelectedSlot=(int)BuildCatalog.MechanicSlot(attachment.mechanic);
            else if(gear!=null)smithSelectedSlot=(int)gear.slot;
            smithCategory=1;progressionGoalsOpen=false;panel=Panel.None;session.SetUIBlocking(false);
            session.Player.Teleport(WorldTraversal.NearestWalkable(GameSession.HubNpcPosition(1),.45f));
            session.Notify("已定位铁匠，请对话进入镶嵌与宝石成长。");BlockUITransition();
        }
        private void PerformGoalAction(ProgressionGoalState goal)
        {
            if(goal.Action==ProgressionGoalAction.ClaimCore||goal.Action==ProgressionGoalAction.ExchangeCore)
            {NavigateMerchantExchange();return;}
            if(goal.Action==ProgressionGoalAction.UnlockVariant||goal.Action==ProgressionGoalAction.Ascend||goal.Action==ProgressionGoalAction.UpgradeAttachment)
            {NavigateSmithAttachment(goal);return;}
            Feedback(session.Progression.ExecuteProgressionGoal(goal.ActionIdentity,session.IsInCamp),"目标操作已保存");
        }
    }
}
