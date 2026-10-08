using UnityEngine;
namespace Emberfall {
 public sealed partial class GameUI {
  private string adventureRewardHint;
  private void AdventureRewardIcon(Rect tile,Texture2D icon,Rarity rarity,string count,string hint,float u) {
   Color c=GameBalance.RarityColor(rarity);Fill(tile,card);Border(tile,c);
   DrawIcon(new Rect(tile.x+8*u,tile.y+8*u,tile.width-16*u,tile.height-22*u),icon,c);
   for(int i=0;i<=(int)rarity;i++)Fill(new Rect(tile.x+4*u+i*5*u,tile.y+3*u,3*u,3*u),pale);
   Text(new Rect(tile.x,tile.yMax-17*u,tile.width-5*u,16*u),count,Mathf.RoundToInt(10*u),pale,false,false,TextAnchor.MiddleRight);
   if(tile.Contains(Mouse))tooltip=hint;
   if(GUI.Button(tile,GUIContent.none,invisibleButton))adventureRewardHint=hint;
  }
  private void DrawAdventureRewards(int mode,int tier,float w,float u,bool showEncounter=true)
  { DrawEntryRewardPreviews(w,u,mode,tier,!showEncounter,true); }
 }
}
