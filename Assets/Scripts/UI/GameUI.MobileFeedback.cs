using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private readonly MobileOpportunityMeter[] mobileOpportunityMeters=new MobileOpportunityMeter[10];
        private string MobileSkillState(int skill)
        {
            var p=session.Progression.Profile;var hero=session.Player;
            var charge=hero==null?null:hero.GetComponent<SkillChargeController>();
            return MobileCombatPresentation.Skill(p.skillRanks[skill]>0,GameBalance.IsPassive(skill),
                hero==null?0:hero.SkillCooldownRemaining(skill),hero==null?0:hero.Energy,
                GameBalance.SkillEnergyCost(p.heroClass,skill),skill==6&&session.ChallengeRun&&session.InDungeon,session.HealingCharges,charge!=null&&charge.IsCharging&&charge.SkillIndex==skill);
        }
        private void DrawMobileSkillAvailability(Rect r,int skill)
        {
            string state=MobileSkillState(skill);
            var window=session.Player==null?default(CombatOpportunityState):session.Player.SkillOpportunityWindow(skill);
            var meter=mobileOpportunityMeters[skill]??(mobileOpportunityMeters[skill]=new MobileOpportunityMeter());
            // Pause hides observation without treating a still-live timer as expired.
            if(!session.InputBlocked)meter.Draw(TouchRect(MobileControls.Layout.SkillOpportunities[skill]),window,session.Player,
                session.Player==null?-1:session.Player.CombatEpoch,TouchRatio,EffectPreferences.TouchOpacity);

            string targetReason=state.Length==0&&session.Player!=null?session.Player.MobilePinnedActionReason(skill):"";
            Rect caption=new Rect(r.x,r.yMax-15*TouchRatio,r.width,15*TouchRatio);
            string rejected=session.ControlFailure("skill"+skill);
            string reason=!string.IsNullOrEmpty(rejected)?rejected:targetReason.Length>0?targetReason:
                state=="缺能"||state=="限疗空"||state=="蓄力"||state=="冷却"?state:
                state.Length==0&&window.Window&&!window.Actionable?window.BlockReason:"";
            if(reason.Length>0)
            {
                Fill(caption,new Color(.035f,.06f,.12f,.9f));
                if(reason=="冷却"){float seconds=session.Player.SkillCooldownRemaining(skill);reason=seconds.ToString(seconds>=10?"0":"0.0");}
                Text(caption,MobileCombatPresentation.SkillRejectionCaption(reason),TouchFont(11),gold,true,false,TextAnchor.MiddleCenter);
            }
            if(state=="蓄力")
            {
                var charge=session.Player.GetComponent<SkillChargeController>();
                Bar(new Rect(r.x+3*TouchRatio,r.yMax-6*TouchRatio,r.width-6*TouchRatio,3*TouchRatio),charge.Progress,gold);
            }
        }
    }
}
