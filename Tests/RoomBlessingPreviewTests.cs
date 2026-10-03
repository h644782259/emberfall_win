using System;
using Emberfall;
public static class RoomBlessingPreviewTests
{
    static int n;
    static void Check(bool ok,string why){n++;if(!ok)throw new Exception(why);}
    static void Open(RoomChainState run)
    {
        var p=run.Room;
        if(p.Interlude){run.ChooseInterlude();return;}
        for(int i=0;i<p.EnemyCount;i++){run.Register(p,i);run.Defeat(p,i);}
        for(int i=0;i<32;i++)run.Advance(.25f,true,true,false);
    }
    static void Match(RoomChainPlan preview,RoomChainPlan actual)
    {Check(preview.Seed==actual.Seed&&preview.Index==actual.Index&&preview.Layout==actual.Layout&&preview.Objective==actual.Objective&&preview.EnemyCount==actual.EnemyCount&&preview.Boss==actual.Boss,"preview exactly matches production next plan");}
    public static string Run()
    {
        n=0;RoomChainPlan preview;
        Check(!RoomBlessingPreview.TryNext(null,1,out preview)&&preview==null,"ordinary mode has no invented room");
        Check(RoomBlessingPreview.Subtitle(null,1,"ordinary waves")=="ordinary waves","ordinary fallback unchanged");
        foreach(int seed in new[]{int.MinValue,-1,0,1,2,8,17,53,999999,int.MaxValue})TestSeed(seed);
        for(int seed=0;seed<540;seed++)TestSeed(seed);
        var failed=new RoomChainState(2);failed.Fail();Check(!RoomBlessingPreview.TryNext(failed,1,out preview),"failed run no forecast");
        failed.Dispose();Check(!RoomBlessingPreview.TryNext(failed,2,out preview),"disposed run no forecast");
        return "PASS: "+n+" authoritative room preview and fallback checks";
    }
    static void TestSeed(int seed)
    {
        foreach(var branch in new[]{RoomBranch.Seal,RoomBranch.Supply})TestSeed(seed,branch);
    }
    static void TestSeed(int seed,RoomBranch branch)
    {
        var run=new RoomChainState(seed);RoomChainPlan p;
        Check(!RoomBlessingPreview.TryNext(run,1,out p),"locked first room cannot forecast a blessing");
        Open(run);var current=run.Room;float progress=run.Progress;
        Check(!RoomBlessingPreview.TryNext(run,2,out p),"wrong first choice stage rejected");
        Check(RoomBlessingPreview.TryNext(run,1,out p),"first choice forecasts next room");
        string line=RoomBlessingPreview.Subtitle(run,1,"fallback");Check(line.Contains(RoomTactics.Name(p.Objective))&&line!="fallback"&&line.Length<=32,"forecast names actual objective");
        for(int i=0;i<5;i++){RoomChainPlan again;Check(RoomBlessingPreview.TryNext(run,1,out again)&&again.Layout==p.Layout&&ReferenceEquals(current,run.Room)&&run.Progress==progress&&run.DoorUnlocked,"repeated GUI reads do not change run or plan");}
        Check(run.Next(true,false),"production next succeeds");Match(p,run.Room);
        Check(!RoomBlessingPreview.TryNext(run,1,out p),"no blessing forecast in second room");
        Open(run);Check(!run.Next(true,false),"second room waits for an explicit branch");
        Check(run.OpenBranchChoice()&&run.SelectBranch(branch)&&run.Next(true,false),"selected third room entered through production choice");
        Check(run.Room.Branch==branch,"only selected branch becomes the current plan");
        Open(run);Check(run.Next(true,false),"both selected third rooms converge at rest");
        Check(run.Room.Interlude&&!run.DoorUnlocked,"real interlude reached");
        Check(!RoomBlessingPreview.TryNext(run,1,out p),"wrong rest choice stage rejected");
        Check(RoomBlessingPreview.TryNext(run,2,out p)&&p.Boss&&p.EnemyCount==3,"rest previews actual three-enemy boss plan");
        Check(RoomBlessingPreview.Subtitle(run,2,"fallback").Contains("2名护卫全灭"),"boss preview includes required guards");
        Check(run.ChooseInterlude(),"choose real interlude");
        Check(!RoomBlessingPreview.TryNext(run,2,out var ignored),"consumed rest choice no forecast");
        Check(run.Next(true,false),"production boss transition");Match(p,run.Room);
        Open(run);Check(run.Finished&&!RoomBlessingPreview.TryNext(run,2,out p),"terminal run no next room");
    }
}
