using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private readonly MobileOpportunityMeter[] mobileOpportunityMeters=new MobileOpportunityMeter[10];
        private void DrawSkillStock(Rect r,int skill,float u)
        {
            var hero=session.Player;var profile=session.Progression.Profile;
            if(hero==null||skill<0||skill!=SkillStockRules.Skill(profile.heroClass)||profile.skillRanks[skill]<1)return;
            float period=hero.SkillRechargePeriod(skill),remaining=hero.SkillRechargeRemaining(skill);
            if(remaining>0&&period>0)
            {
                float progress=1-Mathf.Clamp01(remaining/period),radius=Mathf.Min(r.width,r.height)*.47f;
                for(int i=0;i<32;i++)
                {float angle=(-90+i*360f/32)*Mathf.Deg2Rad;Fill(new Rect(r.center.x+Mathf.Cos(angle)*radius-u,r.center.y+Mathf.Sin(angle)*radius-u,2*u,2*u),i<progress*32?jade:new Color(.25f,.3f,.35f,.55f));}
            }
            Rect badge=new Rect(r.xMax-25*u,r.y+2*u,23*u,12*u);
            Fill(badge,new Color(.015f,.025f,.04f,.85f));
            Text(badge,hero.SkillCharges(skill)+"/2",Mathf.RoundToInt(9*u),hero.SkillCharges(skill)>0?pale:muted,true,false,TextAnchor.MiddleCenter);
        }
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
            DrawSkillStock(r,skill,TouchRatio);
            string state=MobileSkillState(skill);
            var window=session.Player==null?default(CombatOpportunityState):session.Player.SkillOpportunityWindow(skill);
            var meter=mobileOpportunityMeters[skill]??(mobileOpportunityMeters[skill]=new MobileOpportunityMeter());
            // Pause hides observation without treating a still-live timer as expired.
            if(!session.InputBlocked)meter.Draw(TouchRect(MobileOpportunityArea(skill)),window,session.Player,
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
