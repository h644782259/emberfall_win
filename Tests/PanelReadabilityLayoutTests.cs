using System;
using Emberfall;
public static class PanelReadabilityLayoutTests
{
    public static string Run()
    {
        int checks=0;
        Action<bool,string> check=(ok,message)=>{checks++;if(!ok)throw new Exception(message);};
        foreach(var size in new[]{new[]{568f,320f},new[]{667f,375f},new[]{1024f,768f},new[]{1280f,720f},new[]{1920f,1080f}})
        {
            var layout=new AdventureSelectionLayout(size[0],size[1]);
            for(int i=0;i<5;i++)
            {
                var a=layout.Entry(i);
                check(a.Width>=48&&a.Height>=48,"every adventure remains a touch-size target");
                check(a.X>=0&&a.Y>=0&&a.XMax<=size[0]&&a.YMax<=layout.OptionsY,"all five entrances fit before fixed actions");
                var title=layout.EntryTitle(i);var reward=layout.EntryReward(i);var encounter=layout.EntryEncounter(i);
                check(reward.Y-title.YMax>=2 && encounter.Y-reward.YMax>=2,"three text rows have visible vertical gaps");
                check(title.Y-a.Y>=4 && a.YMax-encounter.YMax>=3.9f,"card text has top and bottom padding");
                for(int j=0;j<i;j++)check(!a.Overlaps(layout.Entry(j)),"entry targets never overlap");
            }
            check(layout.Frame.Y>=0&&layout.Frame.YMax<=size[1]&&layout.Frame.X>=0&&layout.Frame.XMax<=size[0],"dialog frame stays inside viewport");
            check(layout.OptionsY+48<=layout.FooterY&&layout.FooterY+48<=size[1],"tier/challenge/confirm stay visible and separate");
        }
        foreach(float height in new[]{720f,900f,1080f})
        foreach(bool expanded in new[]{false,true})
        for(int messages=0;messages<40;messages++)
        {
            float workshopY=AdventureSelectionLayout.WorkshopY(height,messages,expanded);
            float logTop=height-16-AdventureSelectionLayout.LogHeight(messages,expanded);
            check(workshopY>=0&&workshopY+36+8<=logTop,"workshop keeps eight pixels clear of bounded system history");
        }
        return "PASS: "+checks+" panel readability geometry checks (no Unity rendering)";
    }
}
