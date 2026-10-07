using UnityEngine;
namespace Emberfall
{
    public sealed partial class PlayerController
    {
        internal CombatOpportunityState SkillOpportunity(int skill)
        {
            if(session==null||session.Player!=this||IsDead||!session.HasStarted||session.InputBlocked||!SkillTargetingReady(skill)||!MobilePinnedActionAllowed(skill,false))return default;
            if(HeroClass==HeroClass.Summoner&&(skill==2||skill==4||skill==9))
                return new CombatOpportunityState(CombatOpportunityKind.EmpoweredContract,SummonedCompanion.CommandOpportunityRemaining(this),duration:CompanionRules.CommandOpportunityDuration);
            if(HeroClass==HeroClass.Arcanist&&(skill==1||skill==9))
            {
                bool burn=Specialization==ElementalistSpecialization.Burn;
                if(skill==9&&!burn)return default;
                var kind=burn?(skill==9?CombatOpportunityKind.BurnFinale:CombatOpportunityKind.Reignite):CombatOpportunityKind.Shatter;
                return ElementalOpportunityWindow(skill,kind);
            }
            if(HeroClass==HeroClass.Ranger&&skill==0)
            {
                var enemy=MobileControls.Active?MobilePinnedTarget:CurrentOpportunityTarget;
                Vector3 point=aimPoint;
                if(MobileControls.Active)ResolveMobileSkillAim(skill,out enemy,out point);
                if(!ValidAimTarget(enemy)||enemy.StatusEffects==null||!CombatSight.Direct(transform.position,enemy.transform.position))return default;
                // Status/availability only: the fan remains ballistic. No target or
                // hit guarantee is added, and no foreign owner's poison is claimed.
                float range=GameBalance.SkillRangeMultiplier(session.Progression.Profile.skillRanks[skill]);
                if(CombatFx.Flat(enemy.transform.position-transform.position).magnitude>20f*range)return default;
                return new CombatOpportunityState(CombatOpportunityKind.PoisonDetonation,enemy.StatusEffects.OwnPoisonOpportunityRemaining(this),duration:enemy.StatusEffects.PoisonWindowDuration);
            }
            return default;
        }
        internal float ElementalOpportunityRemaining(int skill,CombatOpportunityKind kind)
        {return ElementalOpportunityWindow(skill,kind).Remaining;}
        internal CombatOpportunityState ElementalOpportunityWindow(int skill,CombatOpportunityKind kind)
        {
            Vector3 point=aimPoint;EnemyController selected;
            if(targeting!=null&&targeting.IsTargeting)
            {if(targeting.TargetedSkillIndex!=skill)return default;point=targeting.TargetPoint;}
            else if(MobileControls.Active)ResolveMobileSkillAim(skill,out selected,out point);
            float range=GameBalance.SkillRangeMultiplier(session.Progression.Profile.skillRanks[skill]);
            Vector3 center=ResolveSkillGroundTarget(point,range);float radius=(skill==9?GameBalance.ArcanistFinaleRadius:3f)*range,remaining=0,duration=0;
            foreach(var enemy in session.Enemies)
            {
                if(!ValidAimTarget(enemy)||enemy.StatusEffects==null||CombatFx.Flat(enemy.transform.position-center).magnitude>radius+(enemy.IsBoss?.85f:.4f)+enemy.HitFootprintBonus||!CombatSight.Area(center,enemy.transform.position))continue;
                var status=enemy.StatusEffects;
                float expiry=kind==CombatOpportunityKind.Shatter?status.FrostRemaining:kind==CombatOpportunityKind.BurnFinale?status.OwnBurnRemaining(this):status.BurnRemaining;
                if(expiry>remaining){remaining=expiry;duration=kind==CombatOpportunityKind.Shatter?status.FrostWindowDuration:status.BurnWindowDuration;}
            }
            return new CombatOpportunityState(kind,remaining,duration:duration);
        }
        internal CombatOpportunityState BasicOpportunity()
        {
            if(session==null||session.Player!=this||IsDead||!session.HasStarted||session.InputBlocked||HeroClass!=HeroClass.Vanguard||jumping||attackCooldown>0||skillBasicRecovery.Blocked||charge!=null&&(charge.IsCharging||charge.ConsumedThisFrame)||!MobilePinnedActionAllowed(-1,false))return default;
            return new CombatOpportunityState(CombatOpportunityKind.Counter,CounterOpportunityRemaining,duration:CounterOpportunityDuration);
        }
        internal CombatOpportunityState LatestCombatResult(bool includeBlocked=false)
        {
            if(session==null||session.Player!=this||IsDead||!session.HasStarted||!includeBlocked&&session.InputBlocked)return default;
            int count;float remaining;
            if(BurnCashFeedback(out count,out remaining,includeBlocked))return new CombatOpportunityState(CombatOpportunityKind.BurnCash,remaining,count,receipt:burnFeedbackCast);
            int sequence;float age;
            if(HeroClass==HeroClass.Summoner&&SummonedCompanion.EmpoweredHitFeedback(this,out sequence,out count,out age))
                return new CombatOpportunityState(CombatOpportunityKind.EmpoweredHit,2f-age,count,receipt:sequence);
            return default;
        }
    }
}
