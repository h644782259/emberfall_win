using UnityEngine;
namespace Emberfall
{
    public sealed partial class PlayerController
    {
        // Same 14m scope as ResolveMobileAim and charge snapshot validation; not a new skill range.
        private EnemyController mobilePinnedEnemy;
        private int mobilePinnedEpoch;
        public EnemyController MobilePinnedTarget
        {
            get
            {
                if(mobilePinnedEnemy!=null && (session==null||session.Player!=this||IsDead||CombatEpoch!=mobilePinnedEpoch||
                    !ValidAimTarget(mobilePinnedEnemy)||!session.Enemies.Contains(mobilePinnedEnemy)||
                    CombatFx.Flat(mobilePinnedEnemy.transform.position-transform.position).sqrMagnitude>14f*14f))ClearMobilePinnedTarget();
                return mobilePinnedEnemy;
            }
        }
        private EnemyController ReadMobilePinnedTarget()
        {
            return mobilePinnedEnemy!=null && session!=null && session.Player==this && !IsDead && CombatEpoch==mobilePinnedEpoch &&
                ValidAimTarget(mobilePinnedEnemy) && session.Enemies.Contains(mobilePinnedEnemy) &&
                CombatFx.Flat(mobilePinnedEnemy.transform.position-transform.position).sqrMagnitude<=14f*14f ? mobilePinnedEnemy : null;
        }
        internal string ReadMobilePinnedActionReason(int skill)
        { return MobilePinnedActionReasonFor(skill,ReadMobilePinnedTarget()); }
        internal void ClearMobilePinnedTarget(){mobilePinnedEnemy=null;mobilePinnedEpoch=CombatEpoch;}
        internal bool PinMobileTarget(EnemyController enemy)
        {
            if(!MobileControls.Active||session==null||session.InputBlocked||session.Player!=this||IsDead)return false;
            if(enemy!=null&&(!ValidAimTarget(enemy)||!session.Enemies.Contains(enemy)||CombatFx.Flat(enemy.transform.position-transform.position).sqrMagnitude>14f*14f))return false;
            mobilePinnedEnemy=enemy;mobilePinnedEpoch=CombatEpoch;
            // Only the next action changes. In-flight charge retains its own point, enemy and facing.
            if(charge==null||!charge.IsCharging){AimTarget=enemy;if(enemy!=null)aimPoint=CombatFx.Flat(enemy.transform.position);}
            return true;
        }
        internal bool MobilePinAppliesToSkill(int skill)
        {
            if(skill<0)return true;
            // Ordinary contracts still inherit the explicit team order. A tap never commands pets.
            if(HeroClass==HeroClass.Summoner&&(skill==2||skill==4||skill==9))return false;
            return SkillTargetingController.Describe(HeroClass,skill,session.Progression.Profile.skillRanks[skill]).shape!=SkillTargetingController.Shape.Self;
        }
        internal string MobilePinnedActionReason(int skill)
        { return MobilePinnedActionReasonFor(skill,MobilePinnedTarget); }
        private string MobilePinnedActionReasonFor(int skill,EnemyController enemy)
        {
            if(!MobileControls.Active||!MobilePinAppliesToSkill(skill))return "";
            if(enemy==null)return "";
            if(skill<0&&ReturningCounterReady)
            {
                Vector3 landing;
                return ReturningCounterRules.Predict(transform.position,enemy.transform.position,enemy.IsBoss,enemy.HitFootprintBonus,out landing);
            }
            float distance=CombatFx.Flat(enemy.transform.position-transform.position).magnitude;
            float range=skill<0?(HeroClass==HeroClass.Vanguard?2.8f+(enemy.IsBoss?.85f:.4f)+enemy.HitFootprintBonus:14f):
                SkillTargetingController.Describe(HeroClass,skill,session.Progression.Profile.skillRanks[skill]).distance;
            if(range>0&&distance>range)return "距离不足";
            return CombatSight.Direct(transform.position,enemy.transform.position)?"":"目标被遮挡";
        }
        internal bool MobilePinnedActionAllowed(int skill,bool feedback)
        {
            string reason=MobilePinnedActionReason(skill);if(reason.Length==0)return true;
            if(feedback)session.ReportControlFailure(skill<0?"attack":"skill"+skill,reason);
            return false;
        }
        // Uses real projected model bounds; exact overlaps break ties by stable session order.
        internal EnemyController PickMobileTarget(Vector2 screen)
        {
            Camera camera=Camera.main;if(camera==null||!camera.pixelRect.Contains(screen))return null;
            EnemyController best=null;float bestScore=float.PositiveInfinity;
            foreach(var enemy in session.Enemies)
            {
                if(!ValidAimTarget(enemy)||CombatFx.Flat(enemy.transform.position-transform.position).sqrMagnitude>14f*14f)continue;
                Rect projected;if(!ProjectedBounds(camera,EnemyAimBounds(enemy),out projected)||!projected.Contains(screen))continue;
                Vector3 body=camera.WorldToScreenPoint(EnemyBodyPoint(enemy));
                float score=Vector2.Distance(screen,new Vector2(body.x,body.y))+body.z*.02f;
                if(score<bestScore){bestScore=score;best=enemy;}
            }
            PruneAimGeometry();return best;
        }
    }
}
