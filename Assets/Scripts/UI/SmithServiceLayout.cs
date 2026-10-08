using System;
namespace Emberfall
{
    public sealed class SmithServiceLayout
    {
        public readonly MobilePanelLayout.Area Header,Balance,Close,Detail,Primary;
        private readonly float x,y,w,h,left,right,bodyHeight;
        public SmithServiceLayout(float width,float height)
        {
            width=Math.Max(568,width);height=Math.Max(320,height);w=Math.Min(960,width-24);h=Math.Min(620,height-16);x=(width-w)*.5f;y=(height-h)*.5f;
            left=78;right=Math.Max(112,Math.Min(180,w*.22f));bodyHeight=h-122;
            Header=new MobilePanelLayout.Area(x+14,y+10,w-320,36);Balance=new MobilePanelLayout.Area(x+w-296,y+12,224,28);Close=new MobilePanelLayout.Area(x+w-56,y+10,42,36);
            Detail=new MobilePanelLayout.Area(x+left+26,y+60,w-left-right-54,bodyHeight);
            Primary=new MobilePanelLayout.Area(Detail.X,y+h-56,w-left-40,42);
        }
        public MobilePanelLayout.Area Category(int index)
        {if(index<0||index>2)throw new ArgumentOutOfRangeException(nameof(index));return new MobilePanelLayout.Area(x+12,y+60+index*52,left,44);}
        public MobilePanelLayout.Area Equipment(int index)
        {if(index<0||index>2)throw new ArgumentOutOfRangeException(nameof(index));float row=(bodyHeight-16)/3;return new MobilePanelLayout.Area(x+w-right-12,y+60+index*(row+8),right,row);}
    }
}
