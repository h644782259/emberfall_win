using System;
using Emberfall;

public static class ApplicationPauseStateTests
{
    public static string Run()
    {
        int assertions=0;Action<bool,string> check=(condition,message)=>{assertions++;if(!condition)throw new Exception(message);};
        for(int manual=0;manual<2;manual++)for(int modal=0;modal<2;modal++)for(int dead=0;dead<2;dead++)
        {
            var state=new ApplicationPauseState();bool expected=manual==0&&modal==0&&dead==0;
            check(state.CanAdvance(true,manual!=0,modal!=0,dead!=0)==expected,"initial explicit state respected");
            state.SetFocus(false);check(state.BackgroundPaused,"Alt-Tab suspends simulation");
            check(!state.CanAdvance(true,manual!=0,modal!=0,dead!=0),"background cannot advance");
            state.SetFocus(true);check(state.CanAdvance(true,manual!=0,modal!=0,dead!=0)==expected,"refocus preserves menu/modal/death state");
            state.SetSuspended(true);state.SetFocus(false);state.SetFocus(true);check(state.BackgroundPaused,"focus doesn't override app suspension");
            state.SetSuspended(false);check(state.CanAdvance(true,manual!=0,modal!=0,dead!=0)==expected,"mobile resume preserves explicit state");
            check(!state.CanAdvance(false,false,false,false),"title never advances simulation");
        }
        var frames=new ApplicationPauseState();
        check(frames.TargetFrameRate(true,true)==60,"mobile combat retains 60 fps");
        check(frames.TargetFrameRate(true,false)==30,"mobile title/menu uses 30 fps");
        frames.SetFocus(false);
        check(frames.TargetFrameRate(true,true)==15,"focus loss throttles even before OS suspension");
        frames.SetSuspended(true);frames.SetFocus(true);
        check(frames.TargetFrameRate(true,true)==15,"focus cannot override suspension frame cap");
        check(frames.TargetFrameRate(false,false)==60,"desktop policy unchanged while inactive");
        frames.SetSuspended(false);
        check(frames.TargetFrameRate(true,false)==30,"resume retains menu cap");
        check(frames.TargetFrameRate(true,true)==60,"resume combat restores frame cap");
        check(frames.TargetFrameRate(false,false)==60,"desktop menus not mobile throttled");
        return "PASS: "+assertions+" pause-state assertions";
    }
}
