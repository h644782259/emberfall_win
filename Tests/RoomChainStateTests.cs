using System;
using System.Collections.Generic;
using Emberfall;
public static class RoomChainStateTests
{
    static int n;
    static void Check(bool b,string m){n++;if(!b)throw new Exception(m);}
    public static string Run()
    {
        n=0;var signatures=new HashSet<string>();
        for(int seed=0;seed<540;seed++)
        {
            var run=new RoomChainState(seed);var first=run.Room;
            var goals=new HashSet<RoomObjective>();var terrain=new HashSet<int>();
            Check(!run.Next(true,false),"locked door");
            for(int room=0;room<5;room++)
            {
                var plan=run.Room;var repeat=new RoomChainPlan(room,seed,run.SelectedBranch);
                Check(plan.Index==room && plan.Layout==repeat.Layout && plan.Objective==repeat.Objective,"reproducible identity");
                Check(plan.EnemyCount<=6,"finite six-enemy budget");
                if(room==3)Check(!plan.Interlude&&plan.Objective==RoomObjective.Hunt&&plan.EnemyCount==6&&!run.DoorUnlocked,"fourth room has a guarded target and locked exit");
                if(room<3){goals.Add(plan.Objective);terrain.Add((plan.Layout-20)/2);}
                if(plan.Interlude)Check(run.ChooseInterlude()&&!run.ChooseInterlude(),"choice once");
                else
                {
                    for(int enemy=0;enemy<plan.EnemyCount;enemy++)
                    {
                        Check(!run.Defeat(plan,enemy),"unspawned death rejected");
                        Check(run.Register(plan,enemy)&&!run.Register(plan,enemy),"registration once");
                    }
                    run.Advance(float.NaN,true,true,false);run.Advance(float.PositiveInfinity,true,true,false);run.Advance(-1,true,true,false);
                    run.Advance(1,false,true,false);run.Advance(1,true,false,false);run.Advance(1,true,true,true);
                    Check(run.Progress==0&&!run.DoorUnlocked,"invalid, pause, leave and contest do not capture");
                    if(plan.Objective==RoomObjective.Hunt)
                    {
                        Check(run.Defeat(plan,0)&&run.DoorUnlocked,"only supplier kill needed; escorts optional");
                        Check(!run.Defeat(plan,0),"duplicate supplier rejected");
                    }
                    else
                    {
                        for(int enemy=0;enemy<plan.EnemyCount;enemy++)Check(run.Defeat(plan,enemy)&&!run.Defeat(plan,enemy),"unique deaths");
                        if(!plan.Boss)
                        {
                            if(plan.Index<2&&plan.Branch==RoomBranch.None)
                            {Check(run.DoorUnlocked,"ordinary opening rooms unlock on complete roster clear without repeating capture");}
                            else
                            {
                                Check(!run.DoorUnlocked,"clearing enemies alone does not solve spatial objective");
                                run.Advance(1000,true,true,false);Check(run.Progress==.25f,"long frames cannot instantly capture");
                                for(int tick=0;tick<23;tick++)run.Advance(.25f,true,true,false);
                                Check(run.DoorUnlocked,"spatial objective can finish safely after clear");
                            }
                        }
                    }
                }
                if(room<4)
                {
                    Check(run.DoorUnlocked&&!run.Next(false,false)&&!run.Next(true,true),"gate proximity and block guards");
                    if(room==1){Check(!run.Next(true,false)&&run.OpenBranchChoice(),"third room requires explicit branch");Check(run.SelectBranch(seed%2==0?RoomBranch.Seal:RoomBranch.Supply),"select branch once");}
                    Check(run.Next(true,false)&&!run.Defeat(plan,0),"transition rejects stale callbacks");
                }
            }
            Check(goals.Count>=2&&terrain.Count>=2,"opening rotation retained; selected third objective and terrain may repeat");
            Check(run.Finished&&!run.Failed&&!run.Next(true,false),"boss terminates");
            Check(!run.ClaimReward(false)&&run.ClaimReward(true)&&!run.ClaimReward(true),"durable reward once");
            run.Dispose();Check(!run.Register(first,0)&&!run.ChooseInterlude(),"disposed inert");
            signatures.Add(first.Objective+":"+first.Layout+":"+RoomTactics.EventRoom(seed));
        }
        Check(signatures.Count==54,"54 goal/terrain/mirror/event combinations across seeds");
        int previous=-1,before=-1;
        for(int i=0;i<1000;i++)
        {
            int next=RoomTactics.NextSeed(i%2==0?7:int.MaxValue,previous,before);
            if(previous>=0)Check(RoomTactics.Opening(next)!=RoomTactics.Opening(previous)&&RoomTactics.Terrain(next,0)!=RoomTactics.Terrain(previous,0),"adjacent runs differ even with repeated RNG");
            if(before>=0)Check(RoomTactics.Opening(next)!=RoomTactics.Opening(before)&&RoomTactics.Terrain(next,0)!=RoomTactics.Terrain(before,0),"three-run opening rotation");
            before=previous;previous=next;
        }
        var capture=new RoomChainState(0);
        for(int i=0;i<capture.Room.EnemyCount;i++)capture.Register(capture.Room,i);
        for(int i=0;i<4;i++)capture.Advance(.25f,true,true,false);
        capture.Advance(1,true,true,true);capture.Advance(1,false,true,false);capture.Advance(1,true,false,false);
        Check(capture.Progress==1,"contest/pause/leave preserves partial progress");
        for(int i=0;i<8;i++)capture.Advance(.25f,true,true,false);
        Check(capture.Seals==1&&!capture.DoorUnlocked,"first seal cannot unlock door");
        for(int i=0;i<12;i++)capture.Advance(.25f,true,true,false);
        Check(capture.DoorUnlocked,"capture can bypass living guards");
        Check(capture.Next(true,false)&&capture.Progress==0&&capture.Seals==0,"room transition clears capture state");
        Check(!capture.Next(true,false),"duplicate entry cannot skip the newly locked room");
        var hunt=new RoomChainState(1);
        for(int i=0;i<hunt.Room.EnemyCount;i++)hunt.Register(hunt.Room,i);
        for(int i=1;i<hunt.Room.EnemyCount;i++)hunt.Defeat(hunt.Room,i);
        Check(!hunt.DoorUnlocked,"escort kills cannot replace supplier objective");
        hunt.Defeat(hunt.Room,0);Check(hunt.DoorUnlocked,"supplier after escorts still opens gate");
        var escape=new RoomChainState(2);
        for(int i=0;i<5;i++)escape.Register(escape.Room,i);
        for(int i=0;i<100;i++)escape.Advance(.25f,true,true,false);
        Check(!escape.DoorUnlocked&&escape.Progress==0,"incomplete spawn cannot unlock");
        escape.Register(escape.Room,5);
        for(int i=0;i<16;i++)escape.Advance(.25f,true,true,false);
        Check(escape.DoorUnlocked,"escape opens with all six guards alive");
        escape.Fail();escape.Advance(.25f,true,true,false);
        Check(!escape.DoorUnlocked&&!escape.Next(true,false)&&!escape.ClaimReward(true),"death after open gate cancels escape and reward");
        var abandoned=new RoomChainState(1);var old=abandoned.Room;
        for(int i=0;i<old.EnemyCount;i++)abandoned.Register(old,i);
        abandoned.Dispose();abandoned.Dispose();
        Check(!abandoned.Defeat(old,0)&&!abandoned.Next(true,false)&&!abandoned.ClaimReward(true),"repeated abandonment rejects late deaths and payout");
        Check(RoomTactics.NextSeed(int.MinValue,-1)>=0,"negative seed safe");
        var failed=new RoomChainState();failed.Fail();failed.Advance(1,true,true,false);
        Check(failed.Failed&&failed.Finished&&!failed.Next(true,false)&&!failed.ClaimReward(true),"failed cannot pay/advance");
        return "PASS: "+n+" room objectives, 540 seeds, 54 combinations and 1000 consecutive-run assertions (not playtesting)";
    }
}
