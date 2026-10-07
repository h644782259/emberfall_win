using System;
namespace Emberfall
{
    // The combat clock has no animation input or callback. It advances once per
    // gameplay update and never queues missed attacks after a long frame.
    public struct SkillBasicRecoveryClock
    {
        public float Remaining { get; private set; }
        public bool Blocked { get { return Remaining > 0; } }
        public void Begin(HeroClass hero,int skill,bool charged)
        { Remaining=SkillDamageBudgets.SkillBasicRecovery(hero,skill,charged); }
        public void Advance(float dt)
        { if(!float.IsNaN(dt)&&!float.IsInfinity(dt)&&dt>0)Remaining=Math.Max(0,Remaining-dt); }
        public void Clear() { Remaining=0; }
    }
    public readonly struct PeriodicSkillBudget
    {
        public readonly float Startup,Duration,Interval,TickCoefficient,FinisherCoefficient;
        public int Ticks {get{return SkillDamageBudgets.TickCount(Duration,Interval);}}
        public float TotalCoefficient {get{return Ticks*TickCoefficient+FinisherCoefficient;}}
        public PeriodicSkillBudget(float startup,float duration,float interval,float total,float finisher)
        {Startup=startup;Duration=duration;Interval=interval;FinisherCoefficient=finisher;TickCoefficient=Math.Max(0,total-finisher)/SkillDamageBudgets.TickCount(duration,interval);}
    }
    /// <summary>Per-skill non-critical attack coefficients, shared by live casts and deterministic schedules.</summary>
    public static class SkillDamageBudgets
    {
        private static int Rank(int rank){return Math.Max(1,Math.Min(3,rank));}
        public static int TickCount(float duration,float interval)
        {
            if(float.IsNaN(duration)||float.IsInfinity(duration)||duration<0||float.IsNaN(interval)||float.IsInfinity(interval)||interval<=0)return 0;
            return (int)Math.Min(128,Math.Floor(duration/interval+.0001f)+1);
        }
        public static float AdvancedScale(HeroClass hero,int skill)
        {
            // Each value budgets its own event count, coverage, control and cost.
            // Ultimates retain the established upper envelope; early fields were
            // the outliers and are not used as a reason to inflate boss deletion.
            if(hero==HeroClass.Vanguard){if(skill==5)return .30f;if(skill==7)return .20f;if(skill==9)return .24f;}
            if(hero==HeroClass.Arcanist){if(skill==4)return .42f;if(skill==7)return .30f;if(skill==9)return .24f;}
            if(hero==HeroClass.Ranger){if(skill==4)return .32f;if(skill==5)return .30f;if(skill==7)return .24f;if(skill==9)return .24f;}
            return 0;
        }
        public static PeriodicSkillBudget EarlyField(HeroClass hero,int rank)
        {
            rank=Rank(rank);
            if(hero==HeroClass.Vanguard)return new PeriodicSkillBudget(0,2.4f+(rank-1)*.6f,.45f,rank==1?3.4f:rank==2?4.8f:6.4f,rank==3?1.12f:0);
            if(hero==HeroClass.Arcanist)return new PeriodicSkillBudget(.2f,4.5f,rank>=2?.5f:.6f,rank==1?3.8f:rank==2?5.2f:6.8f,rank==3?1.28f:0);
            if(hero==HeroClass.Ranger)return new PeriodicSkillBudget(.3f,4f+(rank-1),.4f,rank==1?3.8f:rank==2?5.4f:7.2f,rank==3?1.6f:0);
            return new PeriodicSkillBudget(0,0,1,0,0);
        }
        // Same single-target ceiling as the former 3/4/5 arrow retreat volley.
        public static float RangerVault(int rank)
        {rank=Rank(rank);return .32f*(1f+(rank-1)*.3f)*(rank+2)*1.65f;}
        public const float FlameRideTick=.18f;
        public const float FlameRideInterval=.5f;
        public const float BasicEnergyOnHit=8f;
        // Nominal release/recovery timing is gameplay data, not the current model's
        // animation age. Visual settling may extend beyond these values safely.
        public static float SkillPoseDuration(HeroClass hero,int skill,bool charged)
        { return charged && hero==HeroClass.Vanguard && skill==9 ? AdvancedFirstEvent(hero,skill)/(.52f-.30f) : skill==9?1.12f:skill>=4?.84f:.68f; }
        public static float SkillPoseStart(HeroClass hero,int skill,bool charged)
        { return charged && hero==HeroClass.Vanguard && skill==9 ? .30f : .52f; }
        public static float SkillBasicRecovery(HeroClass hero,int skill,bool charged)
        { return SkillPoseDuration(hero,skill,charged)*(.65f-SkillPoseStart(hero,skill,charged)); }
        public static float BasicCoefficient(HeroClass hero){return hero==HeroClass.Vanguard?1f:hero==HeroClass.Ranger?.78f:1.15f;}
        public static float BasicInterval(HeroClass hero){return hero==HeroClass.Vanguard?.46f:hero==HeroClass.Ranger?.34f:.52f;}
        // Auxiliary values multiply the same rank-scaled advanced cast snapshot.
        public static float AdvancedAuxiliary(HeroClass hero,int skill,int rank)
        {
            rank=Rank(rank);
            if(hero==HeroClass.Vanguard&&skill==5&&rank>=2)return 1.2f;
            if(hero==HeroClass.Vanguard&&skill==7&&rank==3)return 4f;
            if(hero==HeroClass.Arcanist&&skill==4&&rank==3)return .65f;
            return 0;
        }
        public static PeriodicSkillBudget AdvancedTail(HeroClass hero,int skill,int rank)
        {
            if(Rank(rank)<3)return new PeriodicSkillBudget(0,0,1,0,0);
            if(hero==HeroClass.Vanguard&&skill==5)return new PeriodicSkillBudget(.4f,0,1,1.4f,0);
            if(hero==HeroClass.Vanguard&&skill==9)return new PeriodicSkillBudget(.5f,2f,.5f,5.5f,0);
            if(hero==HeroClass.Arcanist&&skill==9)return new PeriodicSkillBudget(.6f,2.4f,.6f,5.5f,0);
            if(hero==HeroClass.Ranger&&skill==4)return new PeriodicSkillBudget(.5f,0,1,2f,0);
            return new PeriodicSkillBudget(0,0,1,0,0);
        }
        public static PeriodicSkillBudget ToxicField(int rank)
        {
            rank=Rank(rank);float duration=3.6f+(rank-1)*.8f,finish=rank==3?2.5f:0;
            return new PeriodicSkillBudget(.25f,duration,.6f,TickCount(duration,.6f)*.7f+finish,finish);
        }
        public static int RadialArrowCount(HeroClass hero,int skill,int rank){return hero==HeroClass.Ranger&&skill==9&&Rank(rank)==3?12:0;}
        public static float RadialArrowCoefficient {get{return 1.2f;}}
        public static float OpeningImpact(HeroClass hero,int skill,int rank,int part=0)
        {
            float power=CombatBalance.RankPower(rank),basis=0;
            if(hero==HeroClass.Vanguard){if(skill==0)basis=part==0?1.9f:part==1?.4f:.35f;if(skill==1)basis=part==0?2.5f:part==1?.6f:.4f;}
            if(hero==HeroClass.Arcanist){if(skill==0)basis=part==0?1.5f:part==1?.75f:.6f;if(skill==1)basis=part==0?2.6f:.8f;}
            if(hero==HeroClass.Ranger){if(skill==0)basis=part==0?1.15f:.4f;if(skill==1)basis=part==0?1.7f:.75f;}
            return basis*power;
        }
        public static PeriodicSkillBudget MeteorAftermath(int rank)
        {return new PeriodicSkillBudget(1.3f,2f,.5f,Rank(rank)==3?1.6f:0,0);}
        public static float MeteorTrailTick(int rank,bool concentrated)
        {return OpeningImpact(HeroClass.Arcanist,1,rank)*BuildCatalog.CinderTrailTickMultiplier(concentrated);}
        public static float AdvancedImpact(HeroClass hero,int skill,int rank,int step)
        {
            bool final=step>=AdvancedSteps(hero,skill,rank)-1;
            if(hero==HeroClass.Vanguard){if(skill==5)return 5.2f;if(skill==7)return 2.8f;if(skill==9)return step==0?8f:2.2f;}
            if(hero==HeroClass.Arcanist){if(skill==4)return 2.9f;if(skill==7)return final?(Rank(rank)==3?6f:4f):.85f;if(skill==9)return final?9f:2.2f;}
            if(hero==HeroClass.Ranger){if(skill==4)return 1.65f;if(skill==5)return .7f;if(skill==7)return 1.05f;if(skill==9)return final?8.5f:1.35f;}
            return 0;
        }
        public static float FanTargetCap(int rank){return Rank(rank)==1?3.2f:Rank(rank)==2?4.8f:6.4f;}
        public static float RepeatedVolleyMultiplier(int previousHits)
        {return previousHits<=0?1f:previousHits==1?.5f:previousHits==2?.25f:.1f;}
        public static int AdvancedSteps(HeroClass hero,int skill,int rank)
        {
            rank=Rank(rank);if(skill==6)return 5;
            if(hero==HeroClass.Vanguard&&(skill==7||skill==9))return 6+rank-1;
            if(hero==HeroClass.Arcanist){if(skill==7)return 9+(rank-1)*2;if(skill==9)return 6+rank-1;}
            if(hero==HeroClass.Ranger){if(skill==7)return 14+(rank-1)*3;if(skill==9)return 9+rank-1;}
            return 1;
        }
        public static float AdvancedInterval(HeroClass hero,int skill)
        {if(skill==6)return 1;if(hero==HeroClass.Vanguard)return skill==7?.18f:skill==9?.4f:.2f;if(hero==HeroClass.Arcanist)return skill==7||skill==9?.45f:.2f;return skill==7?.12f:skill==9?.25f:.2f;}
        public static float AdvancedFirstEvent(HeroClass hero,int skill)
        {return skill==6?1:hero==HeroClass.Vanguard&&skill==9?.15f:skill==9?.6f:hero==HeroClass.Arcanist&&skill==7?.3f:0;}
        public static float ChargeSeconds(HeroClass hero,int skill)
        {if(skill==9)return hero==HeroClass.Vanguard?.9f:hero==HeroClass.Ranger?.75f:1.1f;if(hero==HeroClass.Arcanist&&skill==1)return .55f;if(hero==HeroClass.Vanguard&&skill==7)return .5f;if(hero==HeroClass.Summoner&&skill==4)return .4f;return 0;}
    }
}
