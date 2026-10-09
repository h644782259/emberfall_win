using System;
namespace Emberfall
{
    public sealed class MerchantServiceLayout
    {
        public readonly MobilePanelLayout.Area Frame,Header,Balance,Close,Body,Info,Action;
        private readonly float x,y,w,h,tileWidth;
        public readonly int Columns;
        public static bool StablePanelEvent(bool blocked,bool active,bool paint) {return !blocked||active&&paint;}
        public MerchantServiceLayout(float width,float height,bool ipad=false)
        {
            width=Math.Max(568,width);height=Math.Max(320,height);w=Math.Min(ipad?1040:780,width-24);h=Math.Min(ipad?780:600,height-16);x=(width-w)/2;y=(height-h)/2;
            Frame=new MobilePanelLayout.Area(x,y,w,h);
            Header=new MobilePanelLayout.Area(x+12,y+10,w-320,36);Balance=new MobilePanelLayout.Area(x+w-292,y+12,224,28);Close=new MobilePanelLayout.Area(x+w-52,y+10,40,36);
            Body=new MobilePanelLayout.Area(x+12,y+110,w-24,h-178);
            Columns=Math.Max(3,(int)((Body.Width-16+8)/140));
            tileWidth=(Body.Width-16-(Columns-1)*8)/Columns;
            Info=new MobilePanelLayout.Area(x+12,y+h-58,w-170,42);Action=new MobilePanelLayout.Area(x+w-148,y+h-56,136,40);
        }
        public MobilePanelLayout.Area Tab(int index)
        {if(index<0||index>2)throw new ArgumentOutOfRangeException(nameof(index));return new MobilePanelLayout.Area(x+12+index*108,y+58,100,40);}
        public MobilePanelLayout.Area Tile(int index,bool inlineSale=false)
        {if(index<0)throw new ArgumentOutOfRangeException(nameof(index));return new MobilePanelLayout.Area(index%Columns*(tileWidth+8),index/Columns*(inlineSale?158:130),tileWidth,inlineSale?150:122);}
        public float GridHeight(int count,bool inlineSale=false){return Math.Max(0,(count+Columns-1)/Columns*(inlineSale?158:130));}
    }
}
