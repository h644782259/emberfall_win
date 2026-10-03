#!/usr/bin/env python3
"""HUD/free-order wiring contracts; not Unity touch or rendering validation."""
from pathlib import Path
root=Path(__file__).resolve().parent.parent
ui=(root/'Assets/Scripts/UI/GameUI.CombatOpportunities.cs').read_text()
controls=(root/'Assets/Scripts/UI/MobileControls.cs').read_text()
marks=(root/'Assets/Scripts/Combat/CombatTargetFeedback.cs').read_text()
pet=(root/'Assets/Scripts/Combat/SummonedCompanion.cs').read_text()
checks=0
def check(value,message):
    global checks
    assert value,message
    checks+=1
check('SummonedCompanion.FreeRecall(hero)' in ui and 'SummonedCompanion.SetFreeFocus(hero,target)' in ui,'Both actions dispatch the free-order API')
check('CastSkill(' not in ui and '.Begin(' not in ui and 'TryConsume' not in ui,'Free order surface does not dispatch or spend a skill')
check('!MobileControls.Active&&GUI.Button' in ui,'Touch presentation cannot submit an IMGUI duplicate')
check('ui.ActivateFreeCommand(Area(Layout.RecallCommand).Contains(point));role=Role.Consumed;' in controls,'Touch dispatch consumes its owned pointer')
check('if(fingers.ContainsKey(finger))return true;' in controls,'Repeated begin cannot submit the same pointer twice')
check('session.InputBlocked||panel!=Panel.None' in ui,'Commands respect UI and pause gating')
check('commandEpoch==session.Player.CombatEpoch' in ui and 'Time.unscaledTime<commandStatusUntil' in ui,'Local command result expires and cannot cross scenes')
check('"无目标"' in ui and '"太远"' in ui,'Targeting failures have local readable reasons')
check('opportunityEpoch!=hero.CombatEpoch' in ui and 'hero.IsDead||session.InputBlocked' in ui,'Opportunities reset across lifecycle boundaries')
check('hero.CurrentOpportunityTarget' in ui and 'session.Enemies.Contains(target)' in ui,'Opportunity uses the current living scene target')
check('CounterOpportunityRemaining' in ui and 'status.HasFrostMark' in ui and 'status.PoisonStacks' in ui,'Class statuses originate in runtime state')
check('DescribeRoster(hero,out count,out lifetime)' in ui and 'Snapshot(' not in ui,'HUD roster query does not allocate snapshot arrays')
roster=pet[pet.index('public static void DescribeRoster'):pet.index('public static SummonedCompanion[] Snapshot')]
check('pet.IsAlive&&pet.Owner==owner' in roster and '!pet.IsPermanent' in roster and 'new ' not in roster,'Roster reads living owned contracts without allocation')
check('SummonedCompanion.ExplicitFocus(hero)' in marks and 'charge.TargetPoint' in marks,'World marks display authoritative free focus and locked charge point')
check('hero.IsJumping||charge!=null&&(charge.IsCharging||charge.ConsumedThisFrame)' in ui, 'Any ongoing or frame-consumed charge and airborne state suppress meteor opportunity')
check('hero.SkillCooldownRemaining(1),hero.Energy,GameBalance.SkillEnergyCost(hero.HeroClass,1),castBlocked' in ui and 'bool burnRoute=hero.Specialization==ElementalistSpecialization.Burn' in ui and 'burnRoute,burnRoute?ready:hero.CanShatterNow(1)' in ui, 'Meteor opportunity reads actual cooldown/energy and explicit specialization')
typed=(root/'Assets/Scripts/Combat/PlayerController.Opportunities.cs').read_text()
slots=(root/'Assets/Scripts/UI/GameUI.MobileFeedback.cs').read_text()
actions=(root/'Assets/Scripts/UI/MobileControls.Feedback.cs').read_text()
check('session.Player.SkillOpportunityWindow(skill)' in slots and 'meter.Draw(' in slots and 'window.Caption' not in slots,'Actual skill slot uses typed observation only when ordinary availability is ready')
check('hero.BasicOpportunityWindow()' in actions and 'counterMeter.Draw(' in actions and 'hero.BasicOpportunity();' not in actions,'Actual attack control has one typed counter window with authoritative actionability')
check('hero.LatestCombatResult(includeBlocked:true)' in ui and 'SummonedCompanion.EmpoweredHitFeedback(this,out sequence,out count,out age)' in typed,'One HUD result reads actual companion event rather than free order')
check('status.FrostRemaining' in typed and 'status.OwnBurnRemaining(this)' in typed and 'OwnPoisonOpportunityRemaining(this)' in typed,'Opportunity expiry and ownership originate in authoritative status')
check('!SkillTargetingReady(skill)||!MobilePinnedActionAllowed(skill,false)' in typed and '(skill==2||skill==4||skill==9)' in typed,'Typed actions respect readiness/pin intent and only real contract identities')
check('6.5f:3f' in typed and 'ResolveSkillGroundTarget(point,range)' in typed and 'CombatSight.Area(center,enemy.transform.position)' in typed,'Elemental status query uses actual release center, final footprint and area sight')
print('PASS:',checks,'combat opportunity/free-command source contracts')
