using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private Vector2 buildPlanScroll;
        private void ResetBuildPlanSurface(){CancelAllocationDraft();practiceChoicesOpen=false;}
        private void ReconcileBuildPlanSurface()
        {if(allocationDraft!=null&&(panel!=Panel.Camp&&panel!=Panel.Skills))ResetBuildPlanSurface();}
        private bool CloseBuildPlanSurface()
        {if(allocationDraft==null)return false;CancelAllocationDraft();CancelMobileScroll();BlockUITransition();return true;}
        private bool DrawBuildPlanSurface()
        {ReconcileBuildPlanSurface();return allocationDraft!=null&&DrawAllocationDraftSurface();}
        private static Rect BuildPlanRect(MobilePanelLayout.Area area,float unit)
        {return new Rect(area.X*unit,area.Y*unit,area.Width*unit,area.Height*unit);}
        private void BuildPlanParagraph(ref float y,float width,float unit,string value,Color color,bool draw,bool bold=false)
        {
            if(string.IsNullOrEmpty(value))return;
            int size=Mathf.RoundToInt(14*unit);
            float h=Mathf.Ceil(Style(size,bold,true).CalcHeight(new GUIContent(value),(width-16)*unit)/unit)+2;
            if(draw)Text(new Rect(8*unit,y*unit,(width-16)*unit,h*unit),value,size,color,bold,true);
            y+=h+10;
        }
    }
}
