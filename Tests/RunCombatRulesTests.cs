using System;
using Emberfall;

public static class RunCombatRulesTests
{
    private static int assertions;
    private static void Check(bool condition, string message) { assertions++; if (!condition) throw new Exception(message); }
    private static bool Near(float a, float b) { return Math.Abs(a-b)<.0001f; }
    private static RunChoices Select(RunBlessing first, RunBlessing? second = null)
    {
        for (int seed=0;seed<2000;seed++)
        {
            var result=new RunChoices();var skills=new int[10];for(int i=0;i<skills.Length;i++)skills[i]=3;
            result.Prepare(1,HeroClass.Ranger,skills,seed);
            int index=Array.IndexOf(result.Offer,first);if(index<0)continue;
            result.Choose(index);
            if(!second.HasValue)return result;
            for(int next=0;next<2000;next++)
            {
                var pair=new RunChoices();pair.Prepare(1,HeroClass.Ranger,skills,seed);pair.Choose(index);
                pair.Prepare(2,HeroClass.Ranger,skills,next);int other=Array.IndexOf(pair.Offer,second.Value);
                if(other>=0){pair.Choose(other);return pair;}
            }
        }
        throw new Exception("Requested blessing not reachable through legitimate seeded choices");
    }
    public static string Run()
    {
        assertions=0;
        var empty=new RunChoices();
        Check(Near(empty.AttackMultiplier,1)&&Near(empty.CritChance(.9f),.9f)&&Near(empty.CooldownMultiplier,1),"no run choice changes baseline stats");
        var keen=Select(RunBlessing.KeenSight);Check(Near(keen.CritChance(.1f),.3f)&&Near(keen.CritChance(.75f),.8f)&&Near(keen.CritChance(.9f),.9f),"crit adds twenty points, caps added bonus and never nerfs higher baseline");
        Check(Near(keen.CritChance(float.NaN),.2f)&&keen.CritChance(float.PositiveInfinity)<=1,"invalid crit input remains finite and bounded");
        var deadly=Select(RunBlessing.DeadlyEdge);
        CombatDamage hit=CombatDamage.Roll(100,1,0,deadly.CriticalMultiplier);
        Check(hit.IsCritical&&Near(hit.Amount,215)&&Near(hit.WithoutCritical().Amount,100),"critical-damage bonus travels with hit and strips correctly for DoT");
        Check(Near((hit*2).WithoutCritical().Amount,200),"scaled critical hit preserves its actual multiplier");
        Check(Near(CombatDamage.Roll(100,1,0,999).Amount,225)&&Near(CombatDamage.Roll(100,1,0,float.NaN).Amount,165),"critical multiplier cannot runaway or propagate NaN");
        var attack=Select(RunBlessing.BattleFervor);Check(Near(attack.AttackMultiplier,1.10f),"attack blessing is exactly ten percent");
        var fast=Select(RunBlessing.SwiftHands);Check(Near(fast.AttackSpeedMultiplier,1.18f),"attack-speed blessing is exactly eighteen percent");
        var energy=Select(RunBlessing.FlowingEssence);Check(Near(energy.ExtraEnergyPerSecond,2),"energy blessing adds two per second");
        var survival=Select(RunBlessing.IronSkin,RunBlessing.LastStand);
        Check(Near(survival.IncomingDamageMultiplier(.36f),.9f)&&Near(survival.IncomingDamageMultiplier(.35f),.75f)&&Near(survival.IncomingDamageMultiplier(.01f),.75f),"survival boundary and additive25percent cap exact");
        Check(survival.IncomingDamageMultiplier(float.NaN)>=.75f&&survival.IncomingDamageMultiplier(-1)>=.75f,"invalid health fraction cannot produce excessive reduction");
        var quick=Select(RunBlessing.QuickRecovery);
        for(int hero=0;hero<4;hero++)for(int skill=0;skill<10;skill++)for(int rank=1;rank<=3;rank++)
        {
            if(GameBalance.IsPassive(skill))continue;
            var runtime=new SkillRuntime((HeroClass)hero);
            Check(runtime.TryConsume(skill,rank,quick.CooldownMultiplier),"fresh active skill accepts run cooldown modifier");
            float expected=Math.Max(1,GameBalance.EffectiveCooldown((HeroClass)hero,skill,rank)*.85f);
            bool stored=skill==SkillRuntime.StockSkill((HeroClass)hero);
            Check(Near(stored?runtime.RechargeRemaining(skill):runtime.Remaining(skill),expected),"new timer receives modifier exactly once");
            Check(!runtime.TryConsume(skill,rank,.7f)&&Near(stored?runtime.RechargeRemaining(skill):runtime.Remaining(skill),expected),"repeated casts cannot compound/reduce running timer");
            if(stored){runtime.Advance(.7f);Check(runtime.TryConsume(skill,rank,.7f)&&Near(runtime.RechargeRemaining(skill),expected-.7f),"second stock preserves existing recovery modifier and progress");}
            float spent=runtime.Energy;runtime.ResetCooldowns();
            if(stored)Check(runtime.Charges(skill)==2&&runtime.RechargeRemaining(skill)==0,"entry reset restores bank without hidden recovery");
            Check(Near(runtime.Remaining(skill),0)&&Near(runtime.Energy,spent),"actual-entry reset clears timer without refunding energy");
            runtime.ResetCooldowns();Check(Near(runtime.Energy,spent),"duplicate reset is idempotent and not resource generation");
        }
        foreach(float invalid in new[]{0f,-2f,float.NaN,float.PositiveInfinity,float.NegativeInfinity})
            Check(SkillRuntime.ModifiedCooldown(10,invalid)>=7&&SkillRuntime.ModifiedCooldown(.1f,invalid)>=1,"cooldown inputs cannot remove hard floors");
        foreach(RunChoices state in new[]{keen,deadly,attack,fast,energy,survival,quick})
        {
            state.Reset();Check(Near(state.AttackMultiplier,1)&&Near(state.CriticalMultiplier,1.65f)&&Near(state.CooldownMultiplier,1)&&Near(state.AttackSpeedMultiplier,1)&&Near(state.ExtraEnergyPerSecond,0)&&Near(state.IncomingDamageMultiplier(.1f),1),"exit reset clears every run-only modifier");
        }
        foreach(var field in typeof(GameProfile).GetFields())Check(field.FieldType!=typeof(RunChoices)&&field.FieldType!=typeof(RunBlessing),"permanent profile has no blessing state");
        ControlPolicies();
        ArenaPreferences();
        return "PASS: "+assertions+" run-combat modifier, cooldown-reset and control-budget assertions";
    }
    private static void ControlPolicies()
    {
        float previousImpulse=float.MaxValue;
        foreach(EnemyControlTier tier in Enum.GetValues(typeof(EnemyControlTier)))
        {
            var state=new EnemyControlPolicy(tier);
            float impulse=state.ApplyKnockback(3);
            Check(impulse>0&&impulse<previousImpulse&&impulse<=state.MaximumImpulse,"normal/elite/boss have strictly differentiated capped knockback");
            previousImpulse=impulse;
            Check(state.ApplyKnockback(3)==0,"repeated impulse does not accumulate during recovery");
            state.Advance(10);Check(state.ApplyKnockback(999)<=state.MaximumImpulse,"extreme knockback remains bounded");
            float stagger;
            Check(state.TryInterrupt(1,1,true,true,out stagger)&&stagger>0,"real qualifying cast interrupts a live windup");
            Check(!state.TryInterrupt(1,1,true,true,out stagger)&&!state.TryInterrupt(2,2,true,true,out stagger),"cast target duplicate and recovery deny interrupt loops");
            state.Advance(20);
            Check(!state.TryInterrupt(2,3,true,true,out stagger),"a cast consumed during immunity cannot become a delayed free interrupt");
            Check(!state.TryInterrupt(3,3,false,true,out stagger),"first hit outside windup is consumed");
            Check(!state.TryInterrupt(3,4,true,true,out stagger),"old lingering field cannot cancel a later windup");
            Check(state.TryInterrupt(4,4,true,true,out stagger),"fresh cast after recovery can interrupt again");
            state.Advance(20);Check(!state.TryInterrupt(5,4,true,true,out stagger),"one windup cannot report twice");
            state.ResetCastOwner();Check(state.TryInterrupt(1,5,true,true,out stagger),"new combat owner can begin its own cast sequence");
            Check(!state.TryInterrupt(2,6,true,false,out stagger),"ordinary damage or unapproved skill cannot hard-interrupt boss");
            state.Advance(20);
            Check(state.PullDistance(999,1)<=state.MaximumPullSpeed*.1f&&state.PullDistance(float.NaN,.016f)==0,
                "pull resistance is frame-budgeted and rejects invalid displacement");
            float duration=state.ApplyStun(100);
            Check(tier==EnemyControlTier.Boss?duration==0:duration>0&&duration<=1.5f,"raw stun is capped and boss uses explicit skill interruption only");
            Check(state.ApplyStun(100)==0,"raw hard control cannot extend itself during recovery");
            for(int i=0;i<1000;i++){state.Advance(.01f);Check(state.ApplyKnockback(float.NaN)==0&&state.ApplyStun(float.PositiveInfinity)==0,"non-finite control rejected");}
        }
        Check(EnemyControlPolicy.IsInterruptSkill(HeroClass.Vanguard,1)&&EnemyControlPolicy.IsInterruptSkill(HeroClass.Arcanist,0)&&EnemyControlPolicy.IsInterruptSkill(HeroClass.Arcanist,1)&&EnemyControlPolicy.IsInterruptSkill(HeroClass.Ranger,1)&&EnemyControlPolicy.IsInterruptSkill(HeroClass.Summoner,0),"each class has an explicit available interruption skill");
        for(int hero=0;hero<4;hero++)Check(!EnemyControlPolicy.IsInterruptSkill((HeroClass)hero,-1)&&!EnemyControlPolicy.IsInterruptSkill((HeroClass)hero,6),"basic hits and healing never qualify for boss interrupt");
        var boss=new EnemyControlPolicy(EnemyControlTier.Boss);float block;
        Check(boss.TryInterrupt(1,1,true,true,out block)&&Near(boss.InterruptRecovery,5)&&Near(block,.45f),"boss stagger .45s and interrupt recovery5s explicit");
        boss.Advance(4.99f);Check(!boss.CanInterruptWindup,"boss red recovery remains before5sec");boss.Advance(.02f);Check(boss.CanInterruptWindup,"boss cyan window returns after recovery");
    }
    private static void ArenaPreferences()
    {
        for(int distance=0;distance<=20;distance++)foreach(BossAttackPolicy.Move prior in Enum.GetValues(typeof(BossAttackPolicy.Move)))for(int repeats=0;repeats<3;repeats++)
        {
            var baseline=BossAttackPolicy.Select(distance,prior,repeats);
            Check(ArenaBossPatternPolicy.Preferred(0,distance,prior,repeats)==baseline,"ordinary pattern0 remains exact baseline");
            Check(ArenaBossPatternPolicy.Preferred(99,distance,prior,repeats)==baseline,"unknown pattern safely falls back");
            for(int pattern=1;pattern<=3;pattern++)
            {
                var move=ArenaBossPatternPolicy.Preferred(pattern,distance,prior,repeats);
                if(move==BossAttackPolicy.Move.Slam&&move!=baseline)Check(distance<=BossAttackPolicy.SlamRadius,"preferred slam remains reachable");
                if(move==BossAttackPolicy.Move.Charge&&move!=baseline)Check(distance>2&&distance<=BossAttackPolicy.ChargeRange+1,"preferred charge stays inside corridor range");
                if(repeats>0&&move==prior)Check(move==baseline,"preference never bypasses repeat fallback");
            }
        }
        Check(ArenaBossPatternPolicy.Preferred(2,3,BossAttackPolicy.Move.Slam,1)==BossAttackPolicy.Move.Fan,"fan-biased boss has distinct close-range choice");
        Check(ArenaBossPatternPolicy.Preferred(3,3,BossAttackPolicy.Move.Fan,1)==BossAttackPolicy.Move.Charge,"charge-biased boss has distinct mid-range choice");
        Check(ArenaBossPatternPolicy.Preferred(1,4,BossAttackPolicy.Move.Charge,1)==BossAttackPolicy.Move.Slam,"slam bias uses reachable outer impact edge");
    }

}
