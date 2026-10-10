using System;using Emberfall;
public static class InventoryGridGeometryTests
{
 public static string Run(){int checks=0;Action<bool,string> check=(a,b)=>{checks++;if(!a)throw new Exception(b);};
 foreach(float width in new[]{568f,667,720,800,1024,1366})foreach(float height in new[]{320f,375,400,768}){
  var panel=new MobilePanelLayout(width,height);float left=Math.Max(200,Math.Min(260,panel.Body.Width*.30f));float bag=panel.Body.Width-left-12;var grid=new InventoryGridGeometry(bag-InventoryGridGeometry.FilterRailWidth-18,InventoryGridGeometry.MobileCellSize);float available=panel.Height-panel.Body.Y-12-48;
  var viewport=new MobilePanelLayout.Area(220,68,bag,available);
  for(int filter=0;filter<4;filter++){var button=InventoryGridGeometry.FilterButton(viewport,filter);check(button.Width==52&&button.Height>=32&&button.Height<=44,"compact direct filters preserve independent touch area");check(button.X>=viewport.XMax-60&&button.XMax<=viewport.XMax&&button.Y>=viewport.Y&&button.YMax<=viewport.YMax+.01,"filters stay in a fixed 60-unit rail to the right of the scrollbar");for(int previous=0;previous<filter;previous++)check(!button.Overlaps(InventoryGridGeometry.FilterButton(viewport,previous)),"constant visible filter hits never overlap");}
  check(174<=bag,"tabs and one category filter fit without sort entry");
  check(grid.FullyVisible(available)>=6,"enlarged cells retain at least 6 complete slots on smallest landscape");
  for(int i=0;i<40;i++){var tile=grid.Tile(i);
   check(tile.Width==InventoryGridGeometry.MobileCellSize&&tile.Height==InventoryGridGeometry.MobileCellSize,"icon-only cells retain enlarged independent hit area");
   check(tile.X>=0&&tile.XMax<=bag-InventoryGridGeometry.FilterRailWidth-18+.01,"grid stays inside viewport");
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
