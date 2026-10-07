using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private void DrawCollectionItemCard(Rect row,Texture2D icon,Color accent,string title,string detail,string state,bool selected=false)
        {
            Fill(row,selected?new Color(.1f,.2f,.23f):card);Fill(new Rect(row.x,row.y,3,row.height),accent);
            if(selected)Border(row,jade);
            DrawIcon(new Rect(row.x+12,row.y+10,38,38),icon,accent);
            Text(new Rect(row.x+60,row.y+6,row.width-72,22),title,15,accent,true);
            Text(new Rect(row.x+60,row.y+30,row.width-72,18),detail,11,pale);
            Text(new Rect(row.x+12,row.y+52,row.width-24,18),state,11,muted);
        }
    }
}
