using System;
namespace Emberfall
{
    public sealed class AdventureSelectionLayout
    {
        public readonly float X,Y,OptionsY,FooterY,ContentHeight,EntryHeight=56,TextPadding=8,TitleHeight=24,BodyHeight=18,TextGap=2;
        public float Spaciousness {get{return 1;}}
        public readonly MobilePanelLayout.Area Frame,List,Details,Footer;
        public AdventureSelectionLayout(float width,float height,float padding=0)
        {
            float w=Math.Min(1000,width-24),h=Math.Min(620,height-16);X=(width-w)*.5f;Y=(height-h)*.5f;ContentHeight=h;
            Frame=new MobilePanelLayout.Area(X,Y,w,h);
            float inner=w-2*padding,list=(inner-12)*.3f;
            List=new MobilePanelLayout.Area(X+padding,Y+44+padding,list,h-108-2*padding);
            Details=new MobilePanelLayout.Area(List.XMax+12,List.Y,inner-list-12,List.Height);
            FooterY=Y+h-52-padding;OptionsY=FooterY;Footer=new MobilePanelLayout.Area(X+padding,FooterY,inner,48);
        }
        public MobilePanelLayout.Area Entry(int index)
        {if(index<0||index>=6)throw new ArgumentOutOfRangeException(nameof(index));return new MobilePanelLayout.Area(0,index*60,List.Width-18,56);}
        public MobilePanelLayout.Area EntryTitle(int index){var a=Entry(index);return new MobilePanelLayout.Area(a.X+8,a.Y+6,a.Width-16,25);}
        public MobilePanelLayout.Area EntryReward(int index){var a=Entry(index);return new MobilePanelLayout.Area(a.X+8,a.Y+33,a.Width-16,18);}
        public MobilePanelLayout.Area EntryEncounter(int index){return EntryReward(index);}
        public static float LogHeight(int messages,bool expanded){return messages<=0?0:Math.Min(expanded?8:3,messages)*39+34;}
        public static float WorkshopY(float height,int messages,bool expanded){return Math.Min(height-223,height-54-LogHeight(messages,expanded)-44);}
    }
}
