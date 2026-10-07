using System;
namespace Emberfall
{
    // Logical units shared by desktop and safe-area touch canvases. Five entries are always visible.
    public sealed class AdventureSelectionLayout
    {
        public readonly float X,Y,OptionsY,FooterY,ContentHeight,EntryHeight,TextPadding,TitleHeight,BodyHeight,TextGap;
        public readonly MobilePanelLayout.Area Frame;
        private readonly float headerHeight,rowGap;
        public float Spaciousness {get; private set;}
        public AdventureSelectionLayout(float width,float height)
        {
            ContentHeight=Math.Min(438,height-16);
            float room=Math.Max(0,Math.Min(1,(ContentHeight-304)/134));
            headerHeight=28+12*room;rowGap=4+6*room;
            float optionsGap=4+8*room,footerGap=8+4*room;
            EntryHeight=(ContentHeight-headerHeight-2*rowGap-optionsGap-footerGap-96)/3;
            Spaciousness=room;
            TextPadding=4+6*room;TitleHeight=16+4*room;BodyHeight=12+3*room;
            TextGap=(EntryHeight-2*TextPadding-TitleHeight-2*BodyHeight)/2;
            X=(width-520)*.5f;Y=(height-ContentHeight)*.5f;
            OptionsY=Y+headerHeight+3*EntryHeight+2*rowGap+optionsGap;
            FooterY=OptionsY+48+footerGap;
            Frame=new MobilePanelLayout.Area(X-20,Y-8,560,ContentHeight+16);
        }
        public MobilePanelLayout.Area Entry(int index)
        {
            if(index<0||index>=6)throw new ArgumentOutOfRangeException(nameof(index));
            return new MobilePanelLayout.Area(X+(index%2)*266,Y+headerHeight+(index/2)*(EntryHeight+rowGap),254,EntryHeight);
        }
        public MobilePanelLayout.Area EntryTitle(int index) {return TextRow(index,0,TitleHeight);}
        public MobilePanelLayout.Area EntryReward(int index) {return TextRow(index,TitleHeight+TextGap,BodyHeight);}
        public MobilePanelLayout.Area EntryEncounter(int index) {return TextRow(index,TitleHeight+BodyHeight+2*TextGap,BodyHeight);}
        private MobilePanelLayout.Area TextRow(int index,float offset,float height)
        {
            var entry=Entry(index);
            return new MobilePanelLayout.Area(entry.X+12,entry.Y+TextPadding+offset,entry.Width-24,height);
        }
        public static float LogHeight(int messages,bool expanded)
        {return messages<=0?0:Math.Min(expanded?8:3,messages)*39+34;}
        public static float WorkshopY(float height,int messages,bool expanded)
        {return Math.Min(height-223,height-16-LogHeight(messages,expanded)-44);}
    }
}
