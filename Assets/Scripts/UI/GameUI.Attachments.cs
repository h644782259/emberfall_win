using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private void DrawAttachmentWorkshop()
        {merchantMode=1;DrawMerchantService();}
        private void NavigateProgressionGoal(ProgressionGoalState goal)
        {
            if(!session.IsInCamp)
            {
                ClosePanel();session.ReturnToCamp();
                if(!session.IsInCamp)return;
            }
            if(panel!=Panel.Camp)TogglePanel(Panel.Camp);
            if(goal.Action==ProgressionGoalAction.OpenPresets)
            {progressionGoalsOpen=false;OpenBuildPlans();buildPlanDetails=1;}
            else if(goal.RequiredAdventureTier>0&&goal.MaterialCost==0||goal.MaterialCost>session.Progression.Profile.mechanicMaterials)
            {
                ClosePanel();session.Player.Teleport(WorldTraversal.NearestWalkable(new Vector3(0,0,11),.45f));
                session.EnterDungeon();session.SelectedDungeonTier=Mathf.Min(Mathf.Max(1,goal.RequiredAdventureTier),session.MaximumDungeonTier);
            }
            else if(goal.Identity.Contains("practice")||session.Progression.Profile.progressionGoal==ProgressionGoalKind.ClassTutorial||session.Progression.Profile.progressionGoal==ProgressionGoalKind.CombatTrial)OpenProgressionGoals();
            else NavigateMerchantExchange();
            BlockUITransition();
        }
    }
}
