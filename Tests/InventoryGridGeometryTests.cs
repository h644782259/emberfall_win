using System;using Emberfall;
public static class InventoryGridGeometryTests
{
 public static string Run(){int checks=0;Action<bool,string> check=(a,b)=>{checks++;if(!a)throw new Exception(b);};
 foreach(float width in new[]{568f,667,720,800,1024,1366})foreach(float height in new[]{320f,375,400,768}){
  var panel=new MobilePanelLayout(width,height);float left=Math.Max(156,Math.Min(232,panel.Body.Width*.28f));float bag=panel.Body.Width-left-12;var grid=new InventoryGridGeometry(bag-18);float available=panel.Height-panel.Body.Y-12-48;
  check(194+44<=bag,"compact filter and sort fit beside tabs");
  check(3*94+88<=panel.Tabs.Width&&284+64<=panel.Footer.Width,"content-width workshop tabs/navigation fit phone and tablet");
  check(grid.FullyVisible(available)>=6,"smallest landscape displays at least six complete equipment tiles before comparison is opened");
  for(int i=0;i<40;i++){var tile=grid.Tile(i);var wear=grid.Action(i,false);var compare=grid.Action(i,true);var padlock=grid.Lock(i);
   check(wear.Width>=44&&compare.Width>=44&&wear.Height==44&&padlock.Width==44,"action and small-lock hit targets remain accessible");
   check(!wear.Overlaps(compare)&&!wear.Overlaps(padlock)&&!compare.Overlaps(padlock),"wear compare and lock cannot share a touch target");
   check(tile.X>=0&&tile.XMax<=bag-18+.01,"grid remains inside its viewport");
  }
 }
 return "PASS "+checks+" adaptive bag density and nonoverlapping action geometry assertions";
 }
}
