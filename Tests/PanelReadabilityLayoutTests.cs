using System;
using Emberfall;
public static class PanelReadabilityLayoutTests
{
 public static string Run()
 {
  int n=0;Action<bool,string> C=(b,s)=>{n++;if(!b)throw new Exception(s);};
  foreach(var size in new[]{new[]{568f,320f},new[]{667f,375f},new[]{681f,323f},new[]{813f,421f},new[]{1024f,768f},new[]{1280f,720f},new[]{1920f,1080f}})
  {
   var l=new AdventureSelectionLayout(size[0],size[1]);
   foreach(float textScale in new[]{1f,1.1f,1.2f}){C(5*14*textScale<=l.Entry(0).Width-16&&14*textScale*1.2f<=24,"five-character entry names fit supported enlarged text");C(5*13*textScale<=80&&4*15*textScale<=116&&4*15*textScale<=l.Frame.Width-356,"tier 100, healing and enter labels fit at largest font");}
   C(l.Frame.X>=0&&l.Frame.Y>=0&&l.Frame.XMax<=size[0]&&l.Frame.YMax<=size[1],"frame stays within safe logical canvas");
   C(Math.Abs(l.List.Width/(l.List.Width+l.Details.Width)-.3f)<.001f,"list/detail ratio is 30/70");
   C(!l.List.Overlaps(l.Details)&&!l.List.Overlaps(l.Footer)&&!l.Details.Overlaps(l.Footer),"scroll bodies exclude fixed footer");
   C(l.Footer.Height>=48&&l.Footer.YMax<=size[1]&&l.Frame.Width-356>=108,"enter touch target stays visible at shortest supported viewport");
   for(int i=0;i<6;i++){var a=l.Entry(i);C(a.Width>=100&&a.Height>=48,"all six scroll targets touch-sized");C(a.X>=0&&a.XMax<=l.List.Width-18,"list reserves its scrollbar");C(l.EntryTitle(i).YMax+2<=l.EntryReward(i).Y&&l.EntryReward(i).YMax<=a.YMax-4,"two concise rows never overlap");for(int j=0;j<i;j++)C(!a.Overlaps(l.Entry(j)),"entry targets distinct");}
  }
  foreach(float h in new[]{720f,900f,1080f})foreach(bool expanded in new[]{false,true})for(int m=0;m<40;m++)C(AdventureSelectionLayout.WorkshopY(h,m,expanded)+44<=h-16-AdventureSelectionLayout.LogHeight(m,expanded),"workshop remains clear of system history");
  return "PASS "+n+" split adventure layout and fixed-footer geometry assertions; no rendering";
 }
}
