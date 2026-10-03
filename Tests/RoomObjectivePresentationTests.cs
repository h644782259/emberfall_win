using System;
using Emberfall;
public static class RoomObjectivePresentationTests
{
    static int n;
    static void Check(bool value,string why){n++;if(!value)throw new Exception(why);}
    static RoomObjectivePresentation View(RoomChainState run,bool inside=false,bool contested=false,bool paused=false,bool support=true)
    {
        var view=RoomObjectivePresentation.Create(run,inside,contested?2:0,paused,new RoomSupportSnapshot(support,support?2:0,support?RoomTargetSupport.Supported:RoomTargetSupport.Severed));
        float width=new MobileControlLayout(568,320,163).AdventureStatus.Width-12;
        // Conservative one-em-per-character bound, not real Unity font rendering.
        Check(view.Title.Length*13<=width&&view.ProgressText.Length*11<=width&&view.Hint.Length*10<=width&&view.SupportHint.Length*10<=width,"all compact lines fit one-em budget on smallest phone");
        Check(view.Fraction>=0&&view.Fraction<=1,"objective bar bounded");return view;
    }
    static void Register(RoomChainState run){for(int i=0;i<run.Room.EnemyCount;i++)run.Register(run.Room,i);}
    public static string Run()
    {
        n=0;var purify=new RoomChainState(0);Register(purify);
        foreach(RoomTargetSupport target in Enum.GetValues(typeof(RoomTargetSupport)))
        foreach(int count in new[]{0,1,5})
        {
            var compact=RoomObjectivePresentation.Create(purify,true,6,false,new RoomSupportSnapshot(true,count,target));
            float budget=new MobileControlLayout(568,320,163).AdventureStatus.Width-12;
            Check(compact.SupportHint.Length*10<=budget&&compact.Hint.Length*10<=budget,"every target support state and max room count fits compact text budget");
        }
        var halves=new RoomChainState(0);Register(halves);
        for(int i=0;i<6;i++){halves.AdvanceSeal(0,.25f,true,true,false);halves.AdvanceSeal(1,.25f,true,true,false);}
        var shared=View(halves);Check(shared.Fraction==.5f&&shared.ProgressText.Contains("3.0/6秒"),"aggregate uses both incomplete seal halves");
        var v=View(purify);Check(v.Title.Contains("双印净化")&&v.ProgressText.Contains("0/2")&&v.ProgressText.Contains("0.0/6秒")&&v.Hint.Contains("任选A/B"),"initial mobile card explains seal count and time");
        for(int i=0;i<6;i++)purify.Advance(.25f,true,true,false);
        v=View(purify,true);Check(v.ProgressText.Contains("1.5/6秒")&&v.Fraction==.25f&&v.Hint.Contains("正在累积"),"bar tracks capture rather than room index");
        Check(View(purify,true,true).Hint.Contains("争夺2敌"),"contest visible");
        Check(View(purify,true,true,true).Hint.Contains("已暂停"),"pause takes precedence");
        Check(View(purify).SupportHint.Contains("援2")&&View(purify).SupportHint.Contains("目标受援"),"support counterplay remains visible during capture");
        Check(View(purify,support:false).SupportHint.Contains("护援已断"),"supplier death changes persistent hint");
        for(int i=0;i<6;i++)purify.Advance(.25f,true,true,false);
        v=View(purify);Check(v.ProgressText.Contains("1/2")&&v.Fraction==.5f,"first seal advances overall bar and resets current timer");
        for(int i=0;i<12;i++)purify.Advance(.25f,true,true,false);
        v=View(purify);Check(v.ProgressText=="北门已开"&&v.Fraction==1&&v.Hint.Contains("留下清场"),"open gate explains choice");
        purify.Fail();v=View(purify);Check(v.ProgressText=="远征失败"&&v.Fraction==0&&v.SupportHint=="","death overrides open gate hint");
        var escape=new RoomChainState(2);Register(escape);
        v=View(escape);Check(v.Title.Contains("北门突围")&&v.ProgressText.Contains("0.0/4秒")&&v.Hint.Contains("北门金环"),"escape has actual four-second requirement");
        for(int i=0;i<8;i++)escape.Advance(.25f,true,true,false);
        v=View(escape,true);Check(v.ProgressText.Contains("2.0/4秒")&&v.Fraction==.5f,"escape mid-capture");
        var hunt=new RoomChainState(1);Register(hunt);
        Check(View(hunt).ProgressText=="击败金环魔灵","hunt identifies actual priority target");
        hunt.Defeat(hunt.Room,0);Check(View(hunt,support:false).ProgressText=="北门已开","hunt updates on actual supplier death");
        foreach(var branch in new[]{RoomBranch.Seal,RoomBranch.Supply})
        {
        var complete=new RoomChainState(0);
        while(!complete.Finished)
        {
            if(complete.Room.Index==2)Check(complete.Room.Branch==branch&&complete.Room.Objective==(branch==RoomBranch.Seal?RoomObjective.Purify:RoomObjective.Hunt),"selected third-room objective drives presentation");
            Register(complete);
            if(complete.Room.Interlude)Check(View(complete).ProgressText=="选择一项祝福","rest instruction");
            if(complete.Room.Boss)Check(View(complete).ProgressText=="击败首领与护卫"&&View(complete).SupportHint=="","boss does not show obsolete supplier hint");
            if(complete.Room.Interlude)complete.ChooseInterlude();
            else
            {
                for(int i=0;i<complete.Room.EnemyCount;i++)
                {
                    Check(complete.Defeat(complete.Room,i),"registered enemy defeat accepted");
                    if(complete.Room.Boss && i<complete.Room.EnemyCount-1)
                    {
                        Check(!complete.Finished,"boss killed first cannot finish while registered guards remain");
                        Check(View(complete).ProgressText.Contains("护卫"),"remaining guards stay explicit after boss and first guard die");
                    }
                }
                for(int i=0;i<24;i++)complete.Advance(.25f,true,true,false);
            }
            View(complete);
            if(!complete.Finished)
            {
                if(complete.Room.Index==1)
                {
                    Check(!complete.Next(true,false),"unselected branch cannot silently advance third room");
                    Check(complete.OpenBranchChoice()&&complete.SelectBranch(branch),"second-room completion explicitly selects third-room branch");
                }
                Check(complete.Next(true,false),"completed room advances exactly once after any required branch selection");
            }
        }
        Check(View(complete).ProgressText=="远征完成","final victory wording");
        }
        return "PASS: "+n+" mobile room-objective presentation assertions (text/state/layout, not rendered UI)";
    }
}
