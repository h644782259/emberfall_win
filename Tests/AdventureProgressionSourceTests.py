"""Current production wiring contracts; these checks do not execute Unity."""
from pathlib import Path
root=Path(__file__).resolve().parents[1]
def read(p):return (root/'Assets/Scripts'/p).read_text()
def compact(s):return ''.join(s.split())
service=read('Core/ProgressionService.cs')
exp=read('Core/GameSession.Expedition.cs')
mode=read('Core/GameSession.Modes.cs')
room=read('Core/GameSession.RoomChain.cs')
selector=read('UI/GameUI.ProgressionGoal.cs')
trial=read('UI/GameUI.CombatTrialGoal.cs')
skills=read('UI/GameUI.SkillIntegration.cs')
checks=0
def check(ok,why):
 global checks
 checks+=1
 assert ok,why
check('Progression.UnlockedAdventureTier(SelectedArenaMode)' in exp and 'candidate.adventureBestTiers[adventureMode+1]' in service and 'candidate.adventureBestTiers[0]' in service,'each mode uses its own saved unlock progress')
check('ticket.Reward.Materials,rewardRun.Tier,(int)rewardRun.Mode)' in compact(mode) and 'TierRewardBand.Materials(4,DungeonTier),DungeonTier,3)' in compact(room),'settlement persists the actual reserved tier and mode, including room-chain mode 3')
check('!ModeRun.RewardPending' in mode and 'RoomChainRun.Failed||RoomChainRun.RewardClaimed' in room,'failed/incomplete runs cannot admit progression settlement')
check('ApplyRewardPresentation(receipt)' in mode and 'ApplyRewardPresentation(receipt)' in room and 'candidate.lastModeRewardDetails=CaptureRewardPresentation(receipt,Profile,candidate)' in compact(service),'arena and room results restore committed reward details')
check('Profile.classTutorialCompleted' in exp and 'RecordClassTutorialEvidence(hero)' in exp,'real class callback commits persistent evidence')
check('key=="职业能力"?4' not in exp and 'Progression.RecordTutorialEvidence(tutorialBit)' in exp,'generic casts never complete class mechanic tutorial; real basics use transactional persistence')
check('SummonedCompanion.Count(Player)' in exp and 'Progression.ClassTutorialUsable' in exp,'class lesson eligibility retains companion and learned-skill availability')
check('p.ClassTutorialText' in trial and 'p.Profile.classTutorialCompleted' in trial and 'DrawCombatTrialGoal(refy,w,u,draw)' in compact(selector),'the shared goals surface renders actual class evidence independently of the legacy mask')
check('session.Progression.ProgressionGoalStatus(runMaterials,session.IsInCamp)' in selector and 'p.SelectedProgressionGoal(session.IsInCamp)' in selector,'goal status and actions use authoritative selected-goal state')
check('CurrentProgressionGoalStatus(data.Snapshot.RewardMaterials)' in read('UI/GameUI.RunRecap.cs'),'results include actual settled run gains in the shared goal status')
check('SelectProgressionGoal(kind,id,tier,kind==ProgressionGoalKind.Reforge?session.Progression.Profile.level:0)' in selector and 'BeginTouchScroll' in selector,'goal replacement requires explicit selection in bounded scroll UI')
check('progressionGoalCharacter!=session.Progression.CurrentSlotId' in selector,'goal modal retires on character change')
check('OpenProgressionGoals()' in read('UI/GameUI.Mobile.cs') and 'OpenProgressionGoals()' in read('UI/GameUI.cs'),'mobile and desktop both open the shared goals surface')
check('DrawProgressionGoalSurface()' in read('UI/GameUI.Expedition.cs') and 'DrawBuildPlanSurface()' in read('UI/GameUI.Expedition.cs'),'camp dispatches goal and saved-build surfaces')
check('ProgressionGoalAction.OpenPresets' in trial and 'panel=Panel.Skills;skillSection=1;OpenBuildPlans()' in compact(trial),'saved-build goal navigates to the integrated skill development surface')
check('DrawSkillDevelopmentContent' in read('UI/GameUI.MobileWorkshop.cs') and 'DrawSkillDevelopmentContent' in skills and 'p.SetSpecialization((ElementalistSpecialization)i,session.IsInCamp)' in skills and 'p.SetSummonerRoute((SummonerRoute)i,session.IsInCamp)' in skills,'mobile workshop and skill development retain class-route actions')
check('p.SelectCoreGoal(route.Mechanic)' in read('UI/GameUI.RouteNextStep.cs') and 'OpenProgressionGoals()' in read('UI/GameUI.RouteNextStep.cs'),'tracking a class core opens the selected goal')
print(f'PASS: {checks} adventure progression UI/lifecycle contracts (not Unity execution)')
