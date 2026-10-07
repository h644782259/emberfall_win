using System;
namespace Emberfall
{
    public sealed class InventoryGridGeometry
    {
        public const float RowHeight=98;
        public readonly int Columns;
        public readonly float CellWidth;
        public InventoryGridGeometry(float width)
        {
            width=Math.Max(220,width);
            Columns=Math.Max(2,Math.Min(8,(int)Math.Floor((width+6)/110)));
            CellWidth=(width-(Columns-1)*6)/Columns;
        }
        public MobilePanelLayout.Area Tile(int index,float top=0)
        {return new MobilePanelLayout.Area((index%Columns)*(CellWidth+6),top+(index/Columns)*RowHeight,CellWidth,RowHeight-6);}
        public MobilePanelLayout.Area Lock(int index,float top=0)
        {var t=Tile(index,top);return new MobilePanelLayout.Area(t.XMax-44,t.Y,44,44);}
        public MobilePanelLayout.Area Action(int index,bool compare,float top=0)
        {var t=Tile(index,top);return new MobilePanelLayout.Area(compare?t.X+CellWidth*.5f+2:t.X+4,t.Y+44,(CellWidth-12)*.5f,44);}
        public int FullyVisible(float height){return Columns*Math.Max(0,(int)Math.Floor(height/RowHeight));}
    }
}
