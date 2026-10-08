using System;
namespace Emberfall
{
    /// <summary>Shared landscape panel geometry, in the same touch units as battle controls.</summary>
    public sealed class MobilePanelLayout
    {
        public readonly struct Area
        {
            public readonly float X,Y,Width,Height;
            public float XMax {get{return X+Width;}} public float YMax {get{return Y+Height;}}
            public Area(float x,float y,float width,float height){X=x;Y=y;Width=width;Height=height;}
            public bool Overlaps(Area other){return X<other.XMax&&XMax>other.X&&Y<other.YMax&&YMax>other.Y;}
        }
        public readonly float Width,Height;
        public readonly Area Header,Close,Body,Tabs,TabbedBody,Left,Right,BodyLeft,BodyRight,Footer;
        public MobilePanelLayout(float width,float height)
        {
            Width=Finite(width)?Math.Max(568,width):568;Height=Finite(height)?Math.Max(320,height):320;
            Header=new Area(16,8,Width-88,48);Close=new Area(Width-64,8,48,48);
            Body=new Area(16,68,Width-32,Height-132);
            Tabs=new Area(16,66,Width-32,44);TabbedBody=new Area(16,120,Width-32,Height-184);
            Footer=new Area(16,Height-56,Width-32,48);
            float left=Math.Max(216,Math.Min(320,Body.Width*.4f));
            Left=new Area(TabbedBody.X,TabbedBody.Y,left,TabbedBody.Height);
            Right=new Area(Left.XMax+12,Left.Y,TabbedBody.Width-left-12,Left.Height);
            BodyLeft=new Area(Body.X,Body.Y,left,Body.Height);
            BodyRight=new Area(BodyLeft.XMax+12,Body.Y,Body.Width-left-12,Body.Height);
        }
        public Area FooterButton(int index,int count){return Divide(Footer,index,count,4);}
        public Area Tab(int index,int count){return Divide(Tabs,index,count,6);}
        private static Area Divide(Area row,int index,int count,int maximum)
        {
            if(count<1||count>maximum||index<0||index>=count)throw new ArgumentOutOfRangeException(nameof(index));
            float w=(row.Width-(count-1)*8)/count;return new Area(row.X+index*(w+8),row.Y,w,row.Height);
        }
        private static bool Finite(float v){return !float.IsNaN(v)&&!float.IsInfinity(v);}
    }

    /// <summary>Centered touch dialog with measured, scrolling body and fixed actions.</summary>
    public sealed class MobileDialogLayout
    {
        public readonly MobilePanelLayout.Area Frame,Header,Body,Footer;
        public MobileDialogLayout(float width,float height)
        {
            width=float.IsNaN(width)||float.IsInfinity(width)?568:Math.Max(568,width);
            height=float.IsNaN(height)||float.IsInfinity(height)?320:Math.Max(320,height);
            float w=Math.Min(620,width-32),h=Math.Min(440,height-16),x=(width-w)*.5f,y=(height-h)*.5f;
            Frame=new MobilePanelLayout.Area(x,y,w,h);
            Header=new MobilePanelLayout.Area(x+16,y+10,w-32,34);
            Body=new MobilePanelLayout.Area(x+16,y+54,w-32,h-126);
            Footer=new MobilePanelLayout.Area(x+16,y+h-64,w-32,48);
        }
        public MobilePanelLayout.Area FooterButton(int index,int count)
        {
            if(count<1||count>3||index<0||index>=count)throw new ArgumentOutOfRangeException(nameof(index));
            float width=(Footer.Width-(count-1)*8)/count;
            return new MobilePanelLayout.Area(Footer.X+index*(width+8),Footer.Y,width,Footer.Height);
        }
    }
}

namespace Emberfall
{
    public sealed class SkillDevelopmentLayout
    {
        public readonly float Width,ClassWidth,PlanWidth,ResetWidth;
        public float ContentHeight {get{return 520;}}
        public SkillDevelopmentLayout(float width)
        {Width=width;ClassWidth=Math.Min(128,width*.24f);ResetWidth=Math.Min(96,width*.17f);PlanWidth=(width-ClassWidth-ResetWidth-24)/2;}
        public MobilePanelLayout.Area MasteryNode(int index)
        {if(index<0||index>=4)throw new ArgumentOutOfRangeException(nameof(index));float w=(Width-24)/4;return new MobilePanelLayout.Area(index*(w+8),64,w,116);}
    }
}

namespace Emberfall
{
    public static class SkillTreePopupLayout
    {
        public static MobilePanelLayout.Area Place(MobilePanelLayout.Area bounds,MobilePanelLayout.Area anchor)
        {
            float w=Math.Min(360,bounds.Width),h=Math.Min(240,bounds.Height),x=anchor.XMax+8;
            if(x+w>bounds.XMax)x=anchor.X-w-8;
            x=Math.Max(bounds.X,Math.Min(bounds.XMax-w,x));float y=Math.Max(bounds.Y,Math.Min(bounds.YMax-h,anchor.Y));
            return new MobilePanelLayout.Area(x,y,w,h);
        }
    }
}
