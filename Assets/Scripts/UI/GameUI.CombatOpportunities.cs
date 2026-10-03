using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private int opportunityEpoch=-1;
        private PlayerController opportunityOwner;
        private string commandStatus;
        private bool commandRecall;
        private float commandStatusUntil;
        private int commandEpoch;
        public bool CompanionCommandsVisible {get{return session!=null&&session.Player!=null&&session.Player.HeroClass==HeroClass.Summoner&&session.HasStarted&&!session.IsDead;}}
        public void ActivateFreeCommand(bool recall)
        {
            if(!CompanionCommandsVisible||session.InputBlocked||panel!=Panel.None)return;
            var hero=session.Player;var target=hero.AimTarget;
            bool attack=recall&&SummonedCompanion.IsFreeRecalled(hero);
            bool success=recall?(attack?SummonedCompanion.FreeAttack(hero):SummonedCompanion.FreeRecall(hero)):SummonedCompanion.SetFreeFocus(hero,target);
            commandStatus=success?(recall?(attack?"已出击":"已召回"):"已集火"):!recall&&(target==null||target.IsDead)?"无目标":!recall&&CombatFx.Flat(target.transform.position-hero.transform.position).sqrMagnitude>196f?"太远":"不可用";
            commandRecall=recall;commandEpoch=hero.CombatEpoch;commandStatusUntil=Time.unscaledTime+1.1f;
        }
        private void DrawCompanionCommands()
        {
            if(!CompanionCommandsVisible)return;
            float previousOpacity=controlOpacity;if(MobileControls.Active)controlOpacity=EffectPreferences.TouchOpacity;
            for(int i=0;i<2;i++)
            {
                bool recall=i==1;
                Rect r=MobileControls.Active?TouchRect(recall?MobileControls.Layout.RecallCommand:MobileControls.Layout.FocusCommand):new Rect(hotbarBounds.x-76,hotbarBounds.y+27+i*50,66,46);
                blockedRects.Add(r);if(MobileControls.Active)r=MobileVisualRect(r);
                string caption=recall?(SummonedCompanion.IsFreeRecalled(session.Player)?"出击":"召回"):"集火";
                if(!session.InputBlocked&&commandEpoch==session.Player.CombatEpoch&&Time.unscaledTime<commandStatusUntil&&commandRecall==recall)caption=commandStatus;
                Box(r,jade,false);
                Text(new Rect(r.x,r.y+4*(MobileControls.Active?TouchRatio:1),r.width,r.height*.5f),caption,MobileControls.Active?TouchFont(11):12,pale,true,false,TextAnchor.MiddleCenter);
                Text(new Rect(r.x,r.y+r.height*.55f,r.width,r.height*.35f),"免费",MobileControls.Active?TouchFont(9):10,jade,false,false,TextAnchor.MiddleCenter);
                // MobileControls owns all physical pointers; the IMGUI surface is
                // presentation-only on touch devices, preventing duplicate orders.
                if(!MobileControls.Active&&GUI.Button(r,GUIContent.none,invisibleButton))ActivateFreeCommand(recall);
            }
            controlOpacity=previousOpacity;
        }
        // Slot indices belong to keyboard layout; this receives the learned,
        // remapped skill identity so moving a skill never moves its mechanic.
        private string DesktopSkillOpportunityCaption(int skill,bool locked,bool lacksEnergy,float cooldown,out bool actionable)
        {
            actionable=false;var hero=session.Player;
            if(skill<0||locked||hero==null||hero.IsDead||session.InputBlocked||!session.HasStarted)return "";
            string failure=session.ControlFailure("skill"+skill);
            if(!string.IsNullOrEmpty(failure))return failure;
            var pending=hero.GetComponent<SkillChargeController>();
            if(pending!=null&&(pending.IsCharging||pending.ConsumedThisFrame))return pending.IsCharging&&pending.SkillIndex==skill?"蓄力":"";
            if(cooldown>.01f)return ""; // Preserve the existing central cooldown overlay.
            if(lacksEnergy)return "缺能";
            var opportunity=hero.SkillOpportunityWindow(skill);actionable=opportunity.Actionable;
            // The dedicated row owns the live window label and clock. This
            // in-slot channel only explains why the action cannot run now.
            return opportunity.Window?opportunity.BlockReason:"";
        }
        private string DesktopBasicOpportunityCaption()
        {
            var hero=session.Player;
            if(hero==null||hero.IsDead||session.InputBlocked||!session.HasStarted)return "技能快捷栏";
            string failure=session.ControlFailure("attack");
            var combo=hero.BasicOpportunityWindow(true);var counter=hero.BasicOpportunityWindow();
            if(!string.IsNullOrEmpty(failure)&&!combo.Window&&!counter.Window)return "左键普攻 · "+failure;
            if(combo.Window||counter.Window)return "左键普攻 · "+(counter.Window?counter.Caption+(counter.Actionable?"":"·待"):"")+(combo.Window?(counter.Window?" · ":"")+combo.Caption+(combo.Actionable?"":"·待"):"");
            var opportunity=hero.BasicOpportunity();
            return opportunity.Actionable?"左键普攻 · "+opportunity.Caption:"技能快捷栏";
        }
        private readonly CombatResultChannel resultChannel=new CombatResultChannel();
        private string CurrentCombatResult()
        {
            var hero=session.Player;
            bool blocked=hero==null||hero.IsDead||session.InputBlocked||!session.HasStarted;
            // Read the actual receipt even on a blocked frame: it may have arrived
            // after the last draw, immediately before pausing. Observation is read-only.
            var result=hero==null?default(CombatOpportunityState):hero.LatestCombatResult(includeBlocked:true);
            return resultChannel.Observe(result,hero,hero==null?-1:hero.CombatEpoch,hero==null?null:hero.CurrentOpportunityTarget,blocked);
        }
        private string CurrentCombatOpportunity()
        {
            var hero=session.Player;
            if(hero==null||hero.IsDead||session.InputBlocked||!session.HasStarted)return "";
            if(opportunityOwner!=hero||opportunityEpoch!=hero.CombatEpoch)
            {opportunityOwner=hero;opportunityEpoch=hero.CombatEpoch;return "";}

            if(hero.HeroClass==HeroClass.Vanguard)return CombatOpportunityPresentation.Vanguard(hero.CounterOpportunityRemaining);
            if(hero.HeroClass==HeroClass.Summoner)
            {
                int count;float lifetime;SummonedCompanion.DescribeRoster(hero,out count,out lifetime);
                return CombatOpportunityPresentation.Summoner(count,lifetime,SummonedCompanion.CommandOpportunityRemaining(hero));
            }
            var target=hero.CurrentOpportunityTarget;
            var status=target!=null&&session.Enemies.Contains(target)&&target.gameObject.activeInHierarchy?target.StatusEffects:null;
            if(hero.HeroClass==HeroClass.Arcanist)
            {
                var charge=hero.GetComponent<SkillChargeController>();
                bool castBlocked=hero.IsJumping||charge!=null&&(charge.IsCharging||charge.ConsumedThisFrame);
                bool ready=CombatOpportunityPresentation.MeteorReady(session.Progression.Profile.skillRanks[1]>0,
                    hero.SkillCooldownRemaining(1),hero.Energy,GameBalance.SkillEnergyCost(hero.HeroClass,1),castBlocked);
                ready=ready&&hero.SkillOpportunity(1).Actionable;
                bool burnRoute=hero.Specialization==ElementalistSpecialization.Burn;
                return CombatOpportunityPresentation.Arcanist(status!=null&&status.HasFrostMark,status!=null&&status.IsBurning,
                    burnRoute,burnRoute?ready:hero.CanShatterNow(1));
            }
            return status==null?"":CombatOpportunityPresentation.Ranger(status.PoisonStacks,status.IsMarked);
        }
    }
}
