using UnityEngine;
namespace Emberfall
{
    public sealed partial class MobileControls
    {
        // Pure presentation hit test: consuming a visible clock never casts its
        // action, changes aim, or enlarges the action's actual hit rectangle.
        private bool IsOpportunityPoint(Vector2 point)
        {
            var hero=session==null?null:session.Player;
            if(!Active||hero==null||session.InputBlocked)return false;
            var layout=Layout;
            for(int skill=0;skill<layout.SkillOpportunities.Length;skill++)
                if(ui!=null&&ui.MobileSkillVisible(skill)&&layout.SkillOpportunities[skill].Contains(point.x,point.y)&&hero.SkillOpportunityWindow(skill).Window)return true;
            return layout.CounterOpportunity.Contains(point.x,point.y)&&hero.BasicOpportunityWindow().Window||
                layout.ComboOpportunity.Contains(point.x,point.y)&&hero.BasicOpportunityWindow(true).Window;
        }
    }
}
