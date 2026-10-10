using System;
using Emberfall;

public static class ExpeditionModeStateTests
{
    private static int checks;
    private static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
    private static ExpeditionPhasePlan Begin(ExpeditionModeState state)
    { ExpeditionPhasePlan plan;Check(state.TryBeginPhase(out plan),"phase can begin once");ExpeditionPhasePlan duplicate;Check(!state.TryBeginPhase(out duplicate)&&duplicate==null,"duplicate begin rejected");return plan; }
    private static void Clear(ExpeditionModeState state,ExpeditionPhasePlan plan)
    {
        for(int i=0;i<plan.EnemyCount;i++)
        {
            Check(state.TryRegisterSpawn(plan,i),"known enemy can register");
            Check(!state.TryRegisterSpawn(plan,i),"duplicate spawn rejected");
            Check(state.RecordDefeat(plan,i),"spawned enemy defeat accepted");
            Check(!state.RecordDefeat(plan,i),"repeat defeat cannot advance");
        }
    }
    private static void Win(ExpeditionModeState state)
    {
        for(int phase=0;phase<ExpeditionModeState.PhaseCount;phase++)
        {
            var plan=Begin(state);
            Check(plan.PhaseIndex==phase && plan.Token==phase+1,"monotonic local phase identity");
            Check(plan.EnemyCount>0&&plan.EnemyCount<=24&&plan.MaxSimultaneous<=10,"bounded phase population");
            Check(!state.RecordDefeat(plan,0),"unspawned enemy cannot count");
            if(state.Mode==ExpeditionModeKind.HoldPoint)
            {
                state.Advance(plan.HoldSeconds,true,true,0);
                Check(state.ObjectiveProgress==1&&state.Status==ExpeditionModeStatus.Active,"hold completion cannot skip enemies");
            }
            Clear(state,plan);
            Check(state.CompletedPhases==phase+1,"one completed phase counted");
            Check(!state.RecordDefeat(plan,0),"finished phase callbacks rejected");
        }
        Check(state.Status==ExpeditionModeStatus.Won&&state.ObjectiveProgress==1,"three phases win");
    }
    public static string Run()
    {
        checks=0;
        foreach(ExpeditionModeKind mode in Enum.GetValues(typeof(ExpeditionModeKind)))
        foreach(int tier in new[]{1,20,50,100})
        foreach(int seed in new[]{-123,0,711})
        {
            var state=new ExpeditionModeState(mode,tier,seed);
            Check(!state.RewardClaimed&&!state.RewardPending,"no reward before victory");
            ExpeditionRewardTicket early;Check(!state.TryReserveReward(out early)&&early==null,"early reward rejected");
            Win(state);
            ExpeditionRewardTicket ticket,again;
            Check(state.TryReserveReward(out ticket)&&ticket.Reward.Gold==state.Reward.Gold,"winner gets exact reward proposal");
            Check(!state.TryReserveReward(out again),"reservation prevents repeated grant attempts");
            Check(state.CompleteReward(ticket,false)&&!state.RewardClaimed,"confirmed save failure permits retry");
            Check(!state.CompleteReward(ticket,true),"stale failed ticket cannot finalize");
            Check(state.TryReserveReward(out again),"new ticket available after failed transaction");
            Check(state.CompleteReward(again,true)&&state.RewardClaimed,"durable transaction acknowledged once");
            Check(!state.CompleteReward(again,true)&&!state.TryReserveReward(out ticket),"committed reward never granted twice");
            Check(!state.Fail(ExpeditionModeFailure.TimeExpired),"terminal winner cannot become failure");
            double before=state.ElapsedCombatSeconds;state.Advance(1000,true,true,0,false);
            Check(state.Status==ExpeditionModeStatus.Won&&state.ElapsedCombatSeconds==before,"terminal state ignores late events");
        }
        PauseAndPressure();DeadlineAndFailure();IdentityAndCapacity();DeterminismAndBounds();
        return "PASS: "+checks+" expedition-mode assertions";
    }
    private static void PauseAndPressure()
    {
        var state=new ExpeditionModeState(ExpeditionModeKind.HoldPoint,1,42);var plan=Begin(state);
        state.Advance(1,true,true,0);
        double elapsed=state.ElapsedCombatSeconds;float progress=state.ObjectiveProgressSeconds;
        // Each host pause source maps to combatActive=false. Neither clock nor point progress changes.
        foreach(string reason in new[]{"manual pause","menu","background","blessing"})
        {state.Advance(60,false,true,0);Check(state.ElapsedCombatSeconds==elapsed&&state.ObjectiveProgressSeconds==progress,reason+" freezes combat time");}
        foreach(float invalid in new[]{0f,-1f,float.NaN,float.PositiveInfinity,float.NegativeInfinity})state.Advance(invalid,true,true,0);
        Check(state.ElapsedCombatSeconds==elapsed&&state.ObjectiveProgressSeconds==progress,"invalid delta ignored");
        state.Advance(2,true,false,0);Check(state.ObjectiveProgressSeconds==progress,"outside point no objective progress");
        state.Advance(2,true,true,1);Check(state.ObjectiveProgressSeconds==progress,"nearby pressure denies objective progress");
        state.Advance(2,true,true,-1);Check(state.ObjectiveProgressSeconds==progress,"invalid negative pressure cannot grant objective progress");
        Clear(state,plan);Check(state.Status==ExpeditionModeStatus.Active,"clear alone cannot skip hold time");
        state.Advance(plan.HoldSeconds,true,true,0);Check(state.Status==ExpeditionModeStatus.AwaitingSpawn,"clear plus uncontested hold advances");
        beforePhaseWait(state);
    }
    private static void beforePhaseWait(ExpeditionModeState state)
    {double elapsed=state.ElapsedCombatSeconds;state.Advance(12,true,true,0);Check(state.ElapsedCombatSeconds==elapsed,"waiting for phase spawn has no clock drift");}
    private static void DeadlineAndFailure()
    {
        foreach(ExpeditionModeKind mode in Enum.GetValues(typeof(ExpeditionModeKind)))
        {
            var state=new ExpeditionModeState(mode,1,4);var plan=Begin(state);
            state.Advance(state.TimeLimitSeconds,true,true,0);
            Check(state.Status==ExpeditionModeStatus.Failed&&state.Failure==ExpeditionModeFailure.TimeExpired,"deadline is explicit failure");
            Check(state.RemainingSeconds==0&&!state.TryRegisterSpawn(plan,0),"failed state does not spawn or underflow timer");
            ExpeditionRewardTicket reward;Check(!state.TryReserveReward(out reward),"timeout never rewarded");
            var huge=new ExpeditionModeState(mode,1,4);Begin(huge);huge.Advance(float.MaxValue,true,true,0);
            Check(huge.Status==ExpeditionModeStatus.Failed&&!double.IsInfinity(huge.ElapsedCombatSeconds),"huge finite delta bounded");
        }
        var dead=new ExpeditionModeState(ExpeditionModeKind.BossGauntlet,1,3);Begin(dead);dead.Advance(0,false,false,0,false);
        Check(dead.Failure==ExpeditionModeFailure.PlayerDefeated,"death recognized independently of timer gating");
        var blocked=new ExpeditionModeState(ExpeditionModeKind.HoldPoint,1,1);
        Check(!blocked.Fail(ExpeditionModeFailure.None)&&blocked.Fail(ExpeditionModeFailure.SpawnBlocked),"valid explicit failure reason required");
        Check(!blocked.Fail(ExpeditionModeFailure.Abandoned),"failure finalized once");
    }
    private static void IdentityAndCapacity()
    {
        var old=new ExpeditionModeState(ExpeditionModeKind.TimedBreakthrough,100,8);var oldPlan=Begin(old);
        var state=old.Retry(8);var plan=Begin(state);
        Check(old.Status==ExpeditionModeStatus.Disposed&&state.ElapsedCombatSeconds==0&&state.CompletedPhases==0,"retry creates fresh state and disposes old run");
        Check(!state.TryRegisterSpawn(oldPlan,0)&&!state.RecordDefeat(oldPlan,0),"same seed stale plan cannot contaminate retry");
        Check(!state.TryRegisterSpawn(plan,-1)&&!state.TryRegisterSpawn(plan,plan.EnemyCount),"out of range spawn rejected");
        for(int i=0;i<plan.MaxSimultaneous;i++)Check(state.TryRegisterSpawn(plan,i),"populate within concurrent cap");
        Check(state.AliveEnemies==plan.MaxSimultaneous&&state.AvailableSpawnSlots==0,"alive cap accounted");
        if(plan.EnemyCount>plan.MaxSimultaneous)Check(!state.TryRegisterSpawn(plan,plan.MaxSimultaneous),"over-cap spawn rejected");
        Check(state.CancelSpawnRegistration(plan,0)&&state.AliveEnemies==plan.MaxSimultaneous-1,"failed spawn registration can roll back");
        Check(!state.RecordDefeat(plan,0)&&state.TryRegisterSpawn(plan,0),"rolled back spawn is not a kill and may retry");
        Check(state.RecordDefeat(plan,0)&&!state.CancelSpawnRegistration(plan,0),"real defeat cannot be recycled as spawn");
        var winner=new ExpeditionModeState(ExpeditionModeKind.BossGauntlet,1,5);Win(winner);
        ExpeditionRewardTicket ticket;Check(winner.TryReserveReward(out ticket),"reserve before abandonment");winner.Dispose();
        Check(!winner.CompleteReward(ticket,true)&&!winner.RewardClaimed&&!winner.RewardPending,"disposed ticket cannot finalize late");
        winner.Dispose();Check(winner.Status==ExpeditionModeStatus.Disposed,"dispose idempotent");
    }
    private static void DeterminismAndBounds()
    {
        foreach(ExpeditionModeKind mode in Enum.GetValues(typeof(ExpeditionModeKind)))
        foreach(int tier in new[]{int.MinValue,1,50,100,int.MaxValue})
        {
            var a=new ExpeditionModeState(mode,tier,-77);var b=new ExpeditionModeState(mode,tier,-77);
            Check(a.Tier>=1&&a.Tier<=100&&a.Reward.Gold>0&&a.Reward.Gold<=5000&&a.Reward.Materials<=7,"tier/reward bounded");
            Check(a.TimeLimitSeconds>=120&&a.TimeLimitSeconds<=240,"finite practical deadline");
            for(int phase=0;phase<3;phase++)
            {
                var pa=Begin(a);var pb=Begin(b);
                Check(pa.Seed==pb.Seed&&pa.EnemyCount==pb.EnemyCount,"seeded reproducible phase");
                for(int i=0;i<pa.EnemyCount;i++)Check(pa.Enemies[i]==pb.Enemies[i],"seeded role order");
                if(mode==ExpeditionModeKind.BossGauntlet)
                { Check(pa.BossCount==1&&pa.Enemies[0]==ExpeditionEnemyRole.Boss&&pa.EnemyCount<=7,"one boss at known index with bounded adds");Check(pa.BossPattern==(ExpeditionBossPattern)(phase+1),"distinct boss pattern per round"); }
                if(mode==ExpeditionModeKind.HoldPoint){a.Advance(pa.HoldSeconds,true,true,0);b.Advance(pb.HoldSeconds,true,true,0);}
                Clear(a,pa);Clear(b,pb);
            }
        }
        bool throws=false;try{new ExpeditionModeState((ExpeditionModeKind)99,1,0);}catch(ArgumentOutOfRangeException){throws=true;}
        Check(throws,"unknown mode rejected instead of falling through");
    }
}
