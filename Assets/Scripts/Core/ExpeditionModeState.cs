using System;
using System.Collections.ObjectModel;

namespace Emberfall
{
    public enum HoldPointState { Inactive, Outside, Contested, Capturing, Secured }
    public enum ExpeditionModeKind { HoldPoint, TimedBreakthrough, BossGauntlet }
    public enum ExpeditionModeStatus { AwaitingSpawn, Active, Won, Failed, Disposed }
    public enum ExpeditionModeFailure { None, PlayerDefeated, TimeExpired, SpawnBlocked, Abandoned }
    public enum ExpeditionEnemyRole { Melee, Ranged, Guardian, Boss }
    public enum ExpeditionBossPattern { None, HeavyStrike, RangedVolley, RelentlessCharge }

    public sealed class ExpeditionModeReward
    {
        public readonly int Gold, Experience, Materials;
        internal ExpeditionModeReward(int gold,int experience,int materials)
        { Gold=gold;Experience=experience;Materials=materials; }
    }

    public sealed class ExpeditionPhasePlan
    {
        public readonly int Token, PhaseIndex, Seed, EnemyCount, BossCount, MaxSimultaneous, RangedCount, EliteCount;
        public readonly float HoldSeconds;
        public readonly ExpeditionBossPattern BossPattern;
        public readonly ReadOnlyCollection<ExpeditionEnemyRole> Enemies;
        internal ExpeditionPhasePlan(int phase,int seed,ExpeditionEnemyRole[] roles,int maximum,float hold,ExpeditionBossPattern pattern)
        {
            Token=phase+1;PhaseIndex=phase;Seed=seed;EnemyCount=roles.Length;MaxSimultaneous=maximum;
            HoldSeconds=hold;BossPattern=pattern;
            Enemies=Array.AsReadOnly((ExpeditionEnemyRole[])roles.Clone());
            foreach(var role in roles)
            {
                if(role==ExpeditionEnemyRole.Boss)BossCount++;
                else if(role==ExpeditionEnemyRole.Ranged)RangedCount++;
                else if(role==ExpeditionEnemyRole.Guardian)EliteCount++;
            }
        }
    }

    public sealed class ExpeditionRewardTicket
    {
        public readonly ExpeditionModeReward Reward;
        internal ExpeditionRewardTicket(ExpeditionModeReward reward){Reward=reward;}
    }

    /// <summary>
    /// Managed-only arena rules. The host owns spawning, combat, pause eligibility,
    /// durable reward transactions, and ordinary-dungeon progression. No Unity state.
    /// </summary>
    public sealed class ExpeditionModeState : IDisposable
    {
        public const int PhaseCount=3;
        public const float HoldPointRadius=3.2f;
        private bool holdInside;
        private int holdPressure;
        // Combat-active time after all required kills while capture remains incomplete.
        // Per-run telemetry only; never serialized into character saves.
        public double HoldIdleWaitSeconds {get;private set;}
        public HoldPointState HoldState
        {get{
            if(Mode!=ExpeditionModeKind.HoldPoint)return HoldPointState.Inactive;
            if(Status==ExpeditionModeStatus.Won)return HoldPointState.Secured;
            if(Status!=ExpeditionModeStatus.Active||CurrentPhase==null)return HoldPointState.Inactive;
            if(ObjectiveProgressSeconds>=CurrentPhase.HoldSeconds)return HoldPointState.Secured;
            if(!holdInside)return HoldPointState.Outside;
            return holdPressure>0?HoldPointState.Contested:HoldPointState.Capturing;
        }}
        public string HoldStateLabel
        {get{switch(HoldState){case HoldPointState.Outside:return "进入圈内";case HoldPointState.Contested:return "争夺中 · 进度保留";
            case HoldPointState.Capturing:return "占领中";case HoldPointState.Secured:return "占领完成 · 清理敌人";default:return "等待下一阶段";}}}
        public static bool InsideHoldPoint(float distanceSquared)
        {return !float.IsNaN(distanceSquared)&&!float.IsInfinity(distanceSquared)&&distanceSquared>=0&&distanceSquared<=HoldPointRadius*HoldPointRadius;}
        public static bool ContestsHoldPoint(float distanceSquared,float enemyFootprint)
        {return !float.IsNaN(distanceSquared)&&!float.IsInfinity(distanceSquared)&&!float.IsNaN(enemyFootprint)&&!float.IsInfinity(enemyFootprint)&&
            distanceSquared>=0&&enemyFootprint>=0&&distanceSquared<=(HoldPointRadius+enemyFootprint)*(HoldPointRadius+enemyFootprint);}

        public readonly ExpeditionModeKind Mode;
        public readonly int Tier, Seed;
        public readonly float TimeLimitSeconds;
        public readonly ExpeditionModeReward Reward;
        public ExpeditionModeStatus Status { get; private set; }
        public ExpeditionModeFailure Failure { get; private set; }
        public int PhaseIndex { get; private set; }
        public int CompletedPhases { get; private set; }
        public int AliveEnemies { get; private set; }
        public int DefeatedEnemies { get; private set; }
        public double ElapsedCombatSeconds { get; private set; }
        public float ObjectiveProgressSeconds { get; private set; }
        public ExpeditionPhasePlan CurrentPhase { get; private set; }
        public bool RewardClaimed { get; private set; }
        public bool RewardPending { get { return Status==ExpeditionModeStatus.Won&&!RewardClaimed; } }
        public bool RewardReserved { get { return rewardTicket!=null; } }
        public bool IsTerminal { get { return Status==ExpeditionModeStatus.Won||Status==ExpeditionModeStatus.Failed||Status==ExpeditionModeStatus.Disposed; } }
        public float RemainingSeconds { get { return (float)Math.Max(0,TimeLimitSeconds-ElapsedCombatSeconds); } }
        public int AvailableSpawnSlots { get { return Status==ExpeditionModeStatus.Active?Math.Max(0,CurrentPhase.MaxSimultaneous-AliveEnemies):0; } }
        public int PendingSpawnCount { get { return Status==ExpeditionModeStatus.Active?CurrentPhase.EnemyCount-spawnedCount:0; } }
        public float ObjectiveProgress
        {
            get
            {
                if(Status==ExpeditionModeStatus.Won)return 1;
                if(CurrentPhase==null)return 0;
                if(Mode==ExpeditionModeKind.HoldPoint)return Math.Min(1,ObjectiveProgressSeconds/CurrentPhase.HoldSeconds);
                return DefeatedEnemies/(float)CurrentPhase.EnemyCount;
            }
        }
        private bool[] spawned, defeated;
        private int spawnedCount;
        private ExpeditionRewardTicket rewardTicket;

        public ExpeditionModeState(ExpeditionModeKind mode,int tier,int seed)
        {
            if(!Enum.IsDefined(typeof(ExpeditionModeKind),mode))throw new ArgumentOutOfRangeException(nameof(mode));
            Mode=mode;Tier=Math.Max(1,Math.Min(100,tier));Seed=seed;
            Status=ExpeditionModeStatus.AwaitingSpawn;
            float tierAllowance=(Tier-1)*30f/99f;
            if(mode==ExpeditionModeKind.HoldPoint)
            {
                TimeLimitSeconds=210f+tierAllowance;
                Reward=new ExpeditionModeReward(110+Tier*20,90+Tier*20,TierRewardBand.Materials(1,Tier));
            }
            else if(mode==ExpeditionModeKind.TimedBreakthrough)
            {
                TimeLimitSeconds=120f+tierAllowance;
                Reward=new ExpeditionModeReward(100+Tier*25,100+Tier*20,TierRewardBand.Materials(2,Tier));
            }
            else
            {
                TimeLimitSeconds=210f;
                Reward=new ExpeditionModeReward(180+Tier*40,140+Tier*30,TierRewardBand.Materials(3,Tier));
            }
        }

        public bool TryBeginPhase(out ExpeditionPhasePlan plan)
        {
            plan=null;
            if(Status!=ExpeditionModeStatus.AwaitingSpawn||PhaseIndex>=PhaseCount)return false;
            CurrentPhase=CreatePlan(PhaseIndex);plan=CurrentPhase;
            spawned=new bool[plan.EnemyCount];defeated=new bool[plan.EnemyCount];
            spawnedCount=0;AliveEnemies=0;DefeatedEnemies=0;ObjectiveProgressSeconds=0;
            holdInside=false;holdPressure=0;
            Status=ExpeditionModeStatus.Active;
            return true;
        }

        public bool TryRegisterSpawn(ExpeditionPhasePlan plan,int enemyIndex)
        {
            if(!ValidIndex(plan,enemyIndex)||spawned[enemyIndex]||AvailableSpawnSlots<=0)return false;
            spawned[enemyIndex]=true;spawnedCount++;AliveEnemies++;return true;
        }

        // For a failed host spawn only, before an enemy became active. A death callback
        // must use RecordDefeat instead; this rollback does not count as an objective kill.
        public bool CancelSpawnRegistration(ExpeditionPhasePlan plan,int enemyIndex)
        {
            if(!ValidIndex(plan,enemyIndex)||!spawned[enemyIndex]||defeated[enemyIndex])return false;
            spawned[enemyIndex]=false;spawnedCount--;AliveEnemies--;return true;
        }

        public bool RecordDefeat(ExpeditionPhasePlan plan,int enemyIndex)
        {
            if(!ValidIndex(plan,enemyIndex)||!spawned[enemyIndex]||defeated[enemyIndex])return false;
            defeated[enemyIndex]=true;AliveEnemies--;DefeatedEnemies++;
            EvaluatePhase();return true;
        }

        private bool ValidIndex(ExpeditionPhasePlan plan,int index)
        { return Status==ExpeditionModeStatus.Active&&object.ReferenceEquals(plan,CurrentPhase)&&index>=0&&index<CurrentPhase.EnemyCount; }

        public void Advance(float deltaSeconds,bool combatActive,bool playerInsidePoint,int pressureEnemies,bool playerAlive=true)
        {
            if(IsTerminal)return;
            if(!playerAlive){Fail(ExpeditionModeFailure.PlayerDefeated);return;}
            if(Status!=ExpeditionModeStatus.Active||!combatActive||deltaSeconds<=0||float.IsNaN(deltaSeconds)||float.IsInfinity(deltaSeconds))return;
            holdInside=playerInsidePoint;holdPressure=pressureEnemies<0?1:pressureEnemies;
            // Use a double accumulator, then stop at the deadline; even a huge finite
            // delta cannot overflow, skip a phase, or accrue progress after time expires.
            double step=Math.Min((double)deltaSeconds,Math.Max(0,TimeLimitSeconds-ElapsedCombatSeconds));
            ElapsedCombatSeconds=Math.Min(TimeLimitSeconds,ElapsedCombatSeconds+step);
            if(ElapsedCombatSeconds>=TimeLimitSeconds)
            { Fail(ExpeditionModeFailure.TimeExpired);return; }
            if(Mode==ExpeditionModeKind.HoldPoint)
            {
                if(DefeatedEnemies==CurrentPhase.EnemyCount&&ObjectiveProgressSeconds<CurrentPhase.HoldSeconds)
                    HoldIdleWaitSeconds+=holdInside&&holdPressure==0?Math.Min(step,CurrentPhase.HoldSeconds-ObjectiveProgressSeconds):step;
                if(holdInside&&holdPressure==0)
                    ObjectiveProgressSeconds=Math.Min(CurrentPhase.HoldSeconds,ObjectiveProgressSeconds+(float)step);
            }
            EvaluatePhase();
        }

        private void EvaluatePhase()
        {
            if(Status!=ExpeditionModeStatus.Active||DefeatedEnemies!=CurrentPhase.EnemyCount)return;
            if(Mode==ExpeditionModeKind.HoldPoint&&ObjectiveProgressSeconds<CurrentPhase.HoldSeconds)return;
            CompletedPhases++;
            if(CompletedPhases==PhaseCount){Status=ExpeditionModeStatus.Won;return;}
            PhaseIndex++;Status=ExpeditionModeStatus.AwaitingSpawn;
            CurrentPhase=null;spawned=defeated=null;spawnedCount=0;AliveEnemies=0;DefeatedEnemies=0;ObjectiveProgressSeconds=0;
        }

        public bool Fail(ExpeditionModeFailure reason)
        {
            if(IsTerminal||reason==ExpeditionModeFailure.None||!Enum.IsDefined(typeof(ExpeditionModeFailure),reason))return false;
            Failure=reason;Status=ExpeditionModeStatus.Failed;rewardTicket=null;return true;
        }

        public bool TryReserveReward(out ExpeditionRewardTicket ticket)
        {
            ticket=null;
            if(!RewardPending||rewardTicket!=null)return false;
            ticket=rewardTicket=new ExpeditionRewardTicket(Reward);return true;
        }

        // committed=false is only for a confirmed failed transaction. An uncertain
        // persistence result must remain reserved until the host reconciles its receipt.
        public bool CompleteReward(ExpeditionRewardTicket ticket,bool committed)
        {
            if(Status!=ExpeditionModeStatus.Won||ticket==null||!object.ReferenceEquals(ticket,rewardTicket)||RewardClaimed)return false;
            if(committed)RewardClaimed=true;
            rewardTicket=null;return true;
        }

        public ExpeditionModeState Retry(int seed)
        {
            Dispose();
            return new ExpeditionModeState(Mode,Tier,seed);
        }

        public void Dispose()
        {
            Status=ExpeditionModeStatus.Disposed;rewardTicket=null;CurrentPhase=null;
            spawned=defeated=null;AliveEnemies=0;spawnedCount=0;
        }

        private ExpeditionPhasePlan CreatePlan(int phase)
        {
            int phaseSeed=unchecked((int)Mix((uint)Seed^(uint)(phase+1)*0x9e3779b9u^(uint)Tier*0x85ebca6bu));
            int pressure=Math.Min(3,(Tier-1)/30);
            if(Mode==ExpeditionModeKind.BossGauntlet)
            {
                int adds=phase+(Tier>=40?1:0);
                var roles=new ExpeditionEnemyRole[1+adds];roles[0]=ExpeditionEnemyRole.Boss;
                for(int i=1;i<roles.Length;i++)roles[i]=i%2==1?ExpeditionEnemyRole.Ranged:ExpeditionEnemyRole.Guardian;
                return new ExpeditionPhasePlan(phase,phaseSeed,roles,Math.Min(4,roles.Length),0,(ExpeditionBossPattern)(phase+1));
            }
            int count=Math.Min(14,(Mode==ExpeditionModeKind.HoldPoint?6:7)+phase*2+pressure);
            var enemies=new ExpeditionEnemyRole[count];
            int ranged=Math.Max(1,count/3),elites=phase+(Tier>=30?1:0);
            for(int i=0;i<count;i++)enemies[i]=i<elites?ExpeditionEnemyRole.Guardian:i<elites+ranged?ExpeditionEnemyRole.Ranged:ExpeditionEnemyRole.Melee;
            uint random=unchecked((uint)phaseSeed);
            for(int i=count-1;i>0;i--)
            {
                random=Mix(unchecked(random+0x9e3779b9u));int swap=(int)(random%(uint)(i+1));
                var old=enemies[i];enemies[i]=enemies[swap];enemies[swap]=old;
            }
            return new ExpeditionPhasePlan(phase,phaseSeed,enemies,Math.Min(8,count),Mode==ExpeditionModeKind.HoldPoint?5+phase:0,ExpeditionBossPattern.None);
        }
        private static uint Mix(uint value)
        {
            unchecked { value^=value>>16;value*=0x7feb352du;value^=value>>15;value*=0x846ca68bu;return value^(value>>16); }
        }
    }
}
