using UnityEngine;
namespace Emberfall
{
    public sealed partial class PlayerController
    {
        // Observation only: no aim/facing change, notification, resource reservation or cast.
        public bool IsSkillAvailable(int skill)
        {
            if (!SkillBudgetReady(skill) || targeting != null && !targeting.CanBeginThisFrame) return false;
            if (MobileControls.Active && ReadMobilePinnedActionReason(skill).Length != 0) return false;
            // Only movement skills need a predicted destination. Companion focus lookup
            // can create/prune command state and must never run during HUD observation.
            bool movement=HeroClass==HeroClass.Vanguard&&skill==5||HeroClass==HeroClass.Ranger&&skill==4;
            if (!movement) return true;
            int rank=session.Progression.Profile.skillRanks[skill];
            Vector3 point=ValidAimTarget(AimTarget)?AimTarget.transform.position:aimPoint;
            if (MobileControls.Active)
            {
                EnemyController unused;ResolveMobileSkillAim(skill,out unused,out point,true);
            }
            return CanUseMovementSkillAt(skill,rank,point);
        }
        private bool SkillBudgetReady(int skill)
        {
            if(session==null||session.Player!=this||skillRuntime==null||IsDead||!session.HasStarted||session.InputBlocked||jumping||skill<0||skill>=GameBalance.SkillCount||GameBalance.IsPassive(skill)||
                charge!=null&&(charge.IsCharging||charge.ConsumedThisFrame))return false;
            int rank=session.Progression.Profile.skillRanks[skill];
            return rank>0&&skillRuntime.Remaining(skill)<=0&&Energy>=GameBalance.SkillEnergyCost(HeroClass,skill)&&SkillHealingHasEffect(skill,rank)&&StockTargetReady(skill,rank)&&
                (HeroClass!=HeroClass.Vanguard||skill!=6||!session.ChallengeRun||!session.InDungeon||session.HealingCharges>0);
        }
        private bool StockTargetReady(int skill,int rank)
        {
            if(HeroClass!=HeroClass.Arcanist||skill!=4)return true;
            float range=GameBalance.SkillRangeMultiplier(rank);
            Vector3 point=ValidAimTarget(AimTarget)?AimTarget.transform.position:aimPoint;
            point=ResolveSkillGroundTarget(point,range);
            foreach(var enemy in session.Enemies)
                if(ValidAimTarget(enemy)&&CombatFx.Flat(enemy.transform.position-point).magnitude<6f*range&&CombatSight.Chain(transform.position,enemy.transform.position))return true;
            return false;
        }
        private bool SkillHealingHasEffect(int skill,int rank)
        { return !(HeroClass==HeroClass.Vanguard&&skill==6&&rank==1&&session.ChallengeRun&&session.InDungeon&&Health>=MaxHealth); }
        private bool CanUseMovementSkillAt(int skill,int rank,Vector3 point)
        {
            // Obstructed travel is shortened during execution; it is not a cast prerequisite.
            return true;
        }
    }
}
