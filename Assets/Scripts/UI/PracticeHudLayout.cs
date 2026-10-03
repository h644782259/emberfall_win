using System;
namespace Emberfall
{
    // Same touch-unit top band reserved by MobileControlLayout. No combat-centre card.
    public sealed class PracticeHudLayout
    {
        public readonly MobilePanelLayout.Area Header,Sidebar,Primary,Leave;
        public PracticeHudLayout(float width,float pixelsPerUnit=1)
        {
            width=float.IsNaN(width)||float.IsInfinity(width)?568:Math.Max(568,width);
            pixelsPerUnit=float.IsNaN(pixelsPerUnit)||float.IsInfinity(pixelsPerUnit)||pixelsPerUnit<=0?1:pixelsPerUnit;
            float buttonWidth=Math.Max(80,48/pixelsPerUnit),buttonHeight=Math.Max(48,48/pixelsPerUnit);
            Sidebar=new MobilePanelLayout.Area(12,8,172,62);
            Header=new MobilePanelLayout.Area(190,8,width-206-2*buttonWidth,48);
            Primary=new MobilePanelLayout.Area(width-16-2*buttonWidth,8,buttonWidth,buttonHeight);
            Leave=new MobilePanelLayout.Area(width-8-buttonWidth,8,buttonWidth,buttonHeight);
        }
    }
}
