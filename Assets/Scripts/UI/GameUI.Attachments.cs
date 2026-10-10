using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private void DrawAttachmentWorkshop()
        {merchantMode=1;DrawMerchantService();}
        private void NavigateProgressionGoal(ProgressionGoalState goal)
        {
            if(!CanSwitchFunction)return;
            PrepareFunctionSwitch();
            if(!session.IsInCamp)
            {
                session.ReturnToCamp();
                if(!session.IsInCamp)return;
            }
            if(goal.Identity.StartsWith("main/chapter/")||goal.Done)
            {
                if(goal.Identity.StartsWith("main/chapter/"))session.SelectedChapterNode=(ChapterNode)int.Parse(goal.Identity.Substring("main/chapter/".Length));
                OpenChapterSelection();
            }
            else if(goal.RequiredAdventureTier>0)
            {
                session.Player.Teleport(WorldTraversal.NearestWalkable(new Vector3(0,0,11),.45f));
                session.SelectedArenaMode=-1;adventureChapterSelected=false;session.EnterDungeon();
                session.SelectedDungeonTier=Mathf.Min(Mathf.Max(1,goal.RequiredAdventureTier),session.MaximumDungeonTier);
            }
            BlockUITransition();
        }
    }
}
