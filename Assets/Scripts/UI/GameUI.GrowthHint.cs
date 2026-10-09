namespace Emberfall
{
    public sealed partial class GameUI
    {
        private bool TryGrowthHudHint(out string title,out string detail)
        {
            if (!MobileControls.Active && !session.InDungeon && !session.SpecialAdventure && !session.IsDead && !session.PracticeActive)
            {
                var goal = session.Progression.SelectedProgressionGoal(session.IsInCamp);
                title = goal.Title;
                detail = goal.Step + " · 点击查看与定位";
                return true;
            }
            return ProgressionHudHint.TryGet(session.Progression,session.IsInCamp?Attention:null,
                session.InDungeon||session.SpecialAdventure||session.IsDead,session.IsInCamp,ProgressionHudHint.TutorialUsable(session.Progression.Profile,MobileControls.Active,session.ClassTutorialVisible),out title,out detail);
        }
    }
}
