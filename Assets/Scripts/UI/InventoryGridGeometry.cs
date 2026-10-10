using System;
namespace Emberfall
{
    public sealed class InventoryGridGeometry
    {
        public const float RowHeight=48;
        public const float MobileCellSize=66,DesktopCellSize=50;
        public const float ItemPopupWidth=250,ItemPopupHeight=120;
        public const float FilterRailWidth=60;
        public static MobilePanelLayout.Area FilterButton(MobilePanelLayout.Area viewport,int index)
        {
            float row=Math.Min(44,viewport.Height/4);
            return new MobilePanelLayout.Area(viewport.XMax-FilterRailWidth+4,viewport.Y+index*row,FilterRailWidth-8,row);
        }
        public readonly int Columns;
        public readonly float CellWidth;
        public readonly float Stride;
        public InventoryGridGeometry(float width,float cellSize=44)
        {CellWidth=cellSize;Stride=cellSize+4;Columns=Math.Max(1,(int)Math.Floor((Math.Max(cellSize,width)+4)/Stride));}
        public MobilePanelLayout.Area Tile(int index,float top=0)
        {return new MobilePanelLayout.Area((index%Columns)*Stride,top+(index/Columns)*Stride,CellWidth,CellWidth);}
        public int FullyVisible(float height){return Columns*Math.Max(0,(int)Math.Floor((height+4)/Stride));}
        public static MobilePanelLayout.Area Popup(MobilePanelLayout.Area bounds,MobilePanelLayout.Area anchor,bool comparison)
        {
            float w=Math.Min(ItemPopupWidth,Math.Max(160,bounds.Width-54)),h=Math.Min(comparison?230:ItemPopupHeight,bounds.Height);
            float x=anchor.XMax+6;if(x+w>bounds.XMax)x=anchor.X-w-6;
            x=Math.Max(bounds.X,Math.Min(bounds.XMax-w,x));
            float y=Math.Max(bounds.Y,Math.Min(bounds.YMax-h,anchor.Y));
            return new MobilePanelLayout.Area(x,y,w,h);
        }
    }
}
