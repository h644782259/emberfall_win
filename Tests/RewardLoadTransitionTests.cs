using System;
using System.IO;
using System.Reflection;
using Emberfall;
public static class RewardLoadTransitionTests
{
    static int checks;
    static void Check(bool value,string why){checks++;if(!value)throw new Exception(why);}
    sealed class FixedRoll:Random
    {readonly int gold,rarity;public FixedRoll(int g,int r){gold=g;rarity=r;}public override int Next(int max){return max==41?gold:max==100?rarity:0;}}
    static void Rng(ProgressionService p,int gold,int rarity){typeof(ProgressionService).GetField("random",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(p,new FixedRoll(gold,rarity));}
    static ChestReward Pending(ProgressionService p){return (ChestReward)typeof(ProgressionService).GetField("pendingChestRoll",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(p);}
    static ProgressionService Stage(ProgressionService p,string slot)
    {ProgressionService candidate;string error;Check(SaveSlotTransition.TryStage(p,slot,out candidate,out error)&&candidate!=null,"real SaveSlotTransition stages requested slot");return candidate;}
    static ChestReward FailDraw(ProgressionService p,int choice)
    {
        string before=File.ReadAllText(p.SaveFilePath);int gold=p.Profile.gold,threads=p.Profile.fashionThreads,fashions=p.Profile.fashions.Count;
        Directory.CreateDirectory(p.SaveFilePath+".tmp");Check(p.OpenDungeonChest()==null,"actual disk failure freezes uncommitted draw");Directory.Delete(p.SaveFilePath+".tmp");
        Check(File.ReadAllText(p.SaveFilePath)==before&&p.Profile.gold==gold&&p.Profile.fashionThreads==threads&&p.Profile.fashions.Count==fashions,"failed draw changes no durable currency collection or profile");return Pending(p);
    }
    public static string Run(string directory)
    {
        var p=new ProgressionService(Path.Combine(directory,"real-load-roll-"+Guid.NewGuid().ToString("N")));
        Check(p.CreateNewSlot(HeroClass.Ranger),"create A");string a=p.CurrentSlotId;
        p.Profile.pendingFashionChest=true;p.Save();Rng(p,1,18);var original=FailDraw(p,0);
        int gold=p.Profile.gold,threads=p.Profile.fashionThreads,events=0;var live=p.Profile;p.Changed+=()=>events++;
        string disk=File.ReadAllText(p.SaveFilePath),backup=File.ReadAllText(p.SaveFilePath+".bak");
        var staged=Stage(p,a);Rng(staged,40,0);
        Check(staged.OpenDungeonChest(1)==null&&staged.LastError.Contains("上次开箱"),"same-slot real staged replacement retains failed choice");
        Check(ReferenceEquals(live,p.Profile)&&events==0&&File.ReadAllText(p.SaveFilePath)==disk&&File.ReadAllText(p.SaveFilePath+".bak")==backup,"preflight and rejected alternate preserve live role and both documents");
        var retried=FailDraw(staged,0);
        Check(retried.id==original.id&&retried.gold==original.gold&&retried.rarityIndex==original.rarityIndex,"staged retry retains exact id amount rarity despite new RNG");
        var stagedAgain=Stage(staged,a);Rng(stagedAgain,40,0);
        Check(stagedAgain.OpenDungeonChest()!=null&&stagedAgain.LastChestReward.id==original.id&&stagedAgain.LastChestReward.gold==original.gold&&stagedAgain.LastChestReward.rarityIndex==original.rarityIndex,"second real stage commits original draw once");
        Check(stagedAgain.Profile.gold==gold+original.gold&&stagedAgain.Profile.fashionThreads==threads+1,"staged success grants one currency delta");
        var committed=Stage(stagedAgain,a);Check(committed.OpenDungeonChest(0)==null&&committed.LastChestReward.id==original.id&&committed.Profile.gold==gold+original.gold,"durable receipt defeats same-slot subsequent reload replay");
        // A second slot must preserve its own draw and carry A back without leaking either.
        Check(committed.AcknowledgeChestReward()&&committed.CreateNewSlot(HeroClass.Vanguard),"create B");string b=committed.CurrentSlotId;
        committed.Profile.pendingFashionChest=true;committed.Save();Rng(committed,5,39);var drawB=FailDraw(committed,1);
        var backA=Stage(committed,a);backA.Profile.pendingFashionChest=true;backA.Profile.clearedRuns++;backA.Save();Rng(backA,2,50);var drawA=FailDraw(backA,2);
        var backB=Stage(backA,b);Rng(backB,40,0);
        Check(backB.OpenDungeonChest(0)==null&&backB.OpenDungeonChest()!=null&&backB.LastChestReward.id==drawB.id&&backB.LastChestReward.Slot==FashionSlot.Wings,"A to B restores only B draw and wing slot");
        var backAgainA=Stage(backB,a);Rng(backAgainA,40,0);
        Check(backAgainA.OpenDungeonChest(0)==null&&backAgainA.OpenDungeonChest()!=null&&backAgainA.LastChestReward.id==drawA.id&&!backAgainA.LastChestReward.Rarity.HasValue,"B to A retains A supply draw without B contamination");
        // Failed stage / discarded candidate cannot consume live pending roll.
        backAgainA.AcknowledgeChestReward();backAgainA.Profile.pendingFashionChest=true;backAgainA.Profile.clearedRuns++;backAgainA.Save();Rng(backAgainA,3,18);var retained=FailDraw(backAgainA,0);
        ProgressionService invalid;string error;Check(!SaveSlotTransition.TryStage(backAgainA,Guid.NewGuid().ToString("N"),out invalid,out error)&&invalid==null,"missing target rejects stage");
        var abandoned=Stage(backAgainA,a);abandoned.NewGame(HeroClass.Vanguard); // In-memory context copy must not erase original's pending draw.
        // Restore the original saved character fixture after deliberate new-game cancellation.
        backAgainA.Save();var fromOriginal=Stage(backAgainA,a);
        Check(fromOriginal.OpenDungeonChest(1)==null&&fromOriginal.OpenDungeonChest()!=null&&fromOriginal.LastChestReward.id==retained.id,"abandoned candidate map clearing does not mutate source draw");
        // Actual committed new game is an explicit fresh role, even at the same path.
        fromOriginal.AcknowledgeChestReward();fromOriginal.Profile.pendingFashionChest=true;fromOriginal.Save();FailDraw(fromOriginal,0);
        fromOriginal.NewGame(HeroClass.Ranger);fromOriginal.Profile.pendingFashionChest=true;fromOriginal.Save();var fresh=Stage(fromOriginal,a);
        Check(fresh.OpenDungeonChest()!=null,"new game clears retained draw at same path");
        // A genuinely new service without a transition models a process restart.
        fresh.AcknowledgeChestReward();fresh.Profile.pendingFashionChest=true;fresh.Profile.clearedRuns++;fresh.Save();FailDraw(fresh,0);
        var restart=new ProgressionService(fresh.SaveDirectory);Check(restart.LoadSlot(a)&&restart.OpenDungeonChest()!=null,"unwritten draw is intentionally not promised across process restart");
        return "PASS: "+checks+" actual SaveSlotTransition failed-chest identity/isolation/receipt/restart-boundary checks";
    }
}
