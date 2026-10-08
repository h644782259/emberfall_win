using System;using Emberfall;
public static class InventoryGridGeometryTests
{
 public static string Run(){int checks=0;Action<bool,string> check=(a,b)=>{checks++;if(!a)throw new Exception(b);};
 foreach(float width in new[]{568f,667,720,800,1024,1366})foreach(float height in new[]{320f,375,400,768}){
  var panel=new MobilePanelLayout(width,height);float left=Math.Max(156,Math.Min(232,panel.Body.Width*.28f));float bag=panel.Body.Width-left-12;var grid=new InventoryGridGeometry(bag-18);float available=panel.Height-panel.Body.Y-12-48;
  check(174<=bag,"tabs and one category filter fit without sort entry");
  check(grid.FullyVisible(available)>=24,"smallest landscape shows at least 24 complete icon slots");
  for(int i=0;i<40;i++){var tile=grid.Tile(i);
   check(tile.Width==44&&tile.Height==44,"icon-only cells retain independent 44-unit hit area");
   check(tile.X>=0&&tile.XMax<=bag-18+.01,"grid stays inside viewport");
   for(int j=0;j<i;j++)check(!tile.Overlaps(grid.Tile(j)),"independent icon hits never overlap");
   foreach(bool comparison in new[]{false,true}){
    var bounds=new MobilePanelLayout.Area(220,68,bag,available);var anchor=new MobilePanelLayout.Area(bounds.X+tile.X,bounds.Y+tile.Y,44,44);var popup=InventoryGridGeometry.Popup(bounds,anchor,comparison);
    check(popup.X>=bounds.X&&popup.Y>=bounds.Y&&popup.XMax<=bounds.XMax+.01&&popup.YMax<=bounds.YMax+.01,"anchored popup remains safe at every edge");
    check(popup.Width<=bounds.Width-54&&popup.Height<=bounds.Height,"popup leaves at least one icon column visible");
   }
  }
 }
 return "PASS "+checks+" adaptive icon grid and bounded popup geometry assertions";
 }
}
