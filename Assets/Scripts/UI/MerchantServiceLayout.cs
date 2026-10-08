using System;
namespace Emberfall
{
    public sealed class MerchantServiceLayout
    {
        public readonly MobilePanelLayout.Area Header,Balance,Close,Body,Info,Action;
        private readonly float x,y,w,h,tileWidth;
        public static bool StablePanelEvent(bool blocked,bool active,bool paint) {return !blocked||active&&paint;}
        public MerchantServiceLayout(float width,float height)
        {
            width=Math.Max(568,width);height=Math.Max(320,height);w=Math.Min(780,width-24);h=Math.Min(600,height-16);x=(width-w)/2;y=(height-h)/2;
            Header=new MobilePanelLayout.Area(x+12,y+10,w-320,36);Balance=new MobilePanelLayout.Area(x+w-292,y+12,224,28);Close=new MobilePanelLayout.Area(x+w-52,y+10,40,36);
            Body=new MobilePanelLayout.Area(x+12,y+110,w-24,h-178);
            tileWidth=Math.Min(132,(Body.Width-16)/3);
            Info=new MobilePanelLayout.Area(x+12,y+h-58,w-170,42);Action=new MobilePanelLayout.Area(x+w-148,y+h-56,136,40);
        }
        public MobilePanelLayout.Area Tab(int index)
        {if(index<0||index>2)throw new ArgumentOutOfRangeException(nameof(index));return new MobilePanelLayout.Area(x+12+index*108,y+58,100,40);}
        public MobilePanelLayout.Area Tile(int index)
        {if(index<0)throw new ArgumentOutOfRangeException(nameof(index));return new MobilePanelLayout.Area(index%3*(tileWidth+8),index/3*130,tileWidth,122);}
        public float GridHeight(int count){return Math.Max(0,(count+2)/3*130);}
    }
}
