using UnityEngine;
namespace Emberfall
{
    public sealed partial class PlayerController
    {
        private bool OpportunityOwnerValid {get{return session!=null&&session.Player==this&&session.HasStarted&&!session.InputBlocked&&!session.CombatEnded&&!IsDead;}}
        // Observation survives temporary resource/aim rejection. It never grants a cast.
        internal CombatOpportunityState SkillOpportunityWindow(int skill)
        {
            if(!OpportunityOwnerValid||skill<0||skill>=GameBalance.SkillCount||GameBalance.IsPassive(skill)||session.Progression.Profile.skillRanks[skill]<=0)return default;
            var ready=SkillOpportunity(skill);
            if(ready.Window)return HeroClass==HeroClass.Summoner&&!ValidAimTarget(OpportunityWindowTarget(skill))?ready.Blocked("无目标"):ready;
            CombatOpportunityKind kind=CombatOpportunityKind.None;float remaining=0,duration=0;
            if(HeroClass==HeroClass.Summoner&&(skill==2||skill==4||skill==9))
            {kind=CombatOpportunityKind.EmpoweredContract;remaining=SummonedCompanion.CommandOpportunityRemaining(this);duration=CompanionRules.CommandOpportunityDuration;}
            else if(HeroClass==HeroClass.Arcanist&&(skill==1||skill==9))
            {
                bool burn=Specialization==ElementalistSpecialization.Burn;if(skill==9&&!burn)return default;
                kind=burn?(skill==9?CombatOpportunityKind.BurnFinale:CombatOpportunityKind.Reignite):CombatOpportunityKind.Shatter;
                var elemental=ElementalOpportunityWindow(skill,kind);remaining=elemental.Remaining;duration=elemental.Duration;
                // A marked intended target retains its true clock when the release
                // footprint is currently blocked/out of reach, without claiming a hit.
                var target=OpportunityWindowTarget(skill);
                if(ValidAimTarget(target)&&session.Enemies.Contains(target)&&target.gameObject.activeInHierarchy&&target.StatusEffects!=null)
                {
                    var status=target.StatusEffects;
                    float candidate=kind==CombatOpportunityKind.Shatter?status.FrostRemaining:kind==CombatOpportunityKind.BurnFinale?status.OwnBurnRemaining(this):status.BurnRemaining;
                    if(candidate>remaining){remaining=candidate;duration=kind==CombatOpportunityKind.Shatter?status.FrostWindowDuration:status.BurnWindowDuration;}
                }
            }
            else if(HeroClass==HeroClass.Ranger&&skill==0)
            {
                var target=OpportunityWindowTarget(skill);
                if(ValidAimTarget(target)&&session.Enemies.Contains(target)&&target.gameObject.activeInHierarchy&&target.StatusEffects!=null)
                {kind=CombatOpportunityKind.PoisonDetonation;remaining=target.StatusEffects.OwnPoisonOpportunityRemaining(this);duration=target.StatusEffects.PoisonWindowDuration;}
            }
            return new CombatOpportunityState(kind,remaining,blockReason:OpportunityBlockReason(skill),duration:duration);
        }
        private EnemyController OpportunityWindowTarget(int skill)
        {
            var target=MobileControls.Active?MobilePinnedTarget:CurrentOpportunityTarget;
            if(target==null&&MobileControls.Active&&skill>=0){Vector3 point;ResolveMobileSkillAim(skill,out target,out point);}
            return target;
        }
        private string OpportunityBlockReason(int skill)
        {
            if(jumping)return "空中";
            if(charge!=null&&(charge.IsCharging||charge.ConsumedThisFrame))return "施法中";
            if(skill>=0)
            {
                if(SkillCooldownRemaining(skill)>0)return "冷却";
                if(Energy<GameBalance.SkillEnergyCost(HeroClass,skill))return "缺能";
            }
            else if(attackCooldown>0||skillBasicRecovery.Blocked)return "恢复中";
            string pinned=MobilePinnedActionReason(skill);if(pinned.Length>0)return pinned;
            var target=OpportunityWindowTarget(skill);
            if(!ValidAimTarget(target))return "无目标";
            if(!CombatSight.Direct(transform.position,target.transform.position))return "被遮挡";
            if(skill<0&&!BasicWindowInRange(target))return "距离不足";
            return "无有效命中";
        }
        private bool BasicWindowInRange(EnemyController target)
        {
            if(!ValidAimTarget(target))return false;
            float reach=HeroClass==HeroClass.Vanguard?2.8f+(target.IsBoss?.85f:.4f)+target.HitFootprintBonus:14f;
            return CombatFx.Flat(target.transform.position-transform.position).magnitude<=reach;
        }
        internal CombatOpportunityState BasicOpportunityWindow(bool mastery=false)
        {
            if(!OpportunityOwnerValid)return default;
            var kind=mastery?CombatOpportunityKind.MasteryCombo:CombatOpportunityKind.Counter;
            float remaining=mastery?masteryCore.ComboRemaining:HeroClass==HeroClass.Vanguard?CounterOpportunityRemaining:0;
            if(remaining<=0)return default;
            string reason="";var target=OpportunityWindowTarget(-1);
            if(jumping||attackCooldown>0||skillBasicRecovery.Blocked||charge!=null&&(charge.IsCharging||charge.ConsumedThisFrame)||!BasicWindowInRange(target)||!CombatSight.Direct(transform.position,target.transform.position)||!MobilePinnedActionAllowed(-1,false))reason=OpportunityBlockReason(-1);
            return new CombatOpportunityState(kind,remaining,blockReason:reason,duration:mastery?MasteryCoreRuntime.ComboDuration:CounterOpportunityDuration);
        }
    }
}
