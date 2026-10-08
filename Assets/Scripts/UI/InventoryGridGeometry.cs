using System;
namespace Emberfall
{
    public sealed class InventoryGridGeometry
    {
        public const float RowHeight=48;
        public readonly int Columns;
        public readonly float CellWidth=44;
        public InventoryGridGeometry(float width)
        {Columns=Math.Max(1,(int)Math.Floor((Math.Max(44,width)+4)/48));}
        public MobilePanelLayout.Area Tile(int index,float top=0)
        {return new MobilePanelLayout.Area((index%Columns)*48,top+(index/Columns)*48,44,44);}
        public int FullyVisible(float height){return Columns*Math.Max(0,(int)Math.Floor((height+4)/48));}
        public static MobilePanelLayout.Area Popup(MobilePanelLayout.Area bounds,MobilePanelLayout.Area anchor,bool comparison)
        {
            float w=Math.Min(250,Math.Max(160,bounds.Width-54)),h=Math.Min(comparison?230:120,bounds.Height);
            float x=anchor.XMax+6;if(x+w>bounds.XMax)x=anchor.X-w-6;
            x=Math.Max(bounds.X,Math.Min(bounds.XMax-w,x));
            float y=Math.Max(bounds.Y,Math.Min(bounds.YMax-h,anchor.Y));
            return new MobilePanelLayout.Area(x,y,w,h);
        }
    }
}
