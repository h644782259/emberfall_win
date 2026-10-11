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
skills=read('UI/GameUI.SkillIntegration.cs')
growth=read('Core/ProgressionService.AutomaticGrowth.cs')
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
check('candidate.classTutorialCompleted=true' in service and 'CommitCandidate(candidate)' in service,'class tutorial evidence remains persisted independently of retired trial UI')
check('DrawAchievements(' in selector and 'ProgressionService.Achievements' in selector and 'p.AchievementClaimed(a.Id)' in selector and 'a.Progress(p.Profile)' in selector,'achievement surface reads authoritative progress and receipt state')
check('snapshot.RewardMaterials' in read('UI/GameUI.RunRecap.cs') and 'snapshot.RewardExperience' in read('UI/GameUI.RunRecap.cs'),'results render actual persisted run rewards')
check('p.ClaimAchievement(a.Id)' in selector and 'session.Progression.ClaimAllAchievements()' in selector and 'BeginTouchScroll' in selector,'single and bulk achievement claims use service transactions in a bounded scroll UI')
check('progressionGoalCharacter!=session.Progression.CurrentSlotId' in selector,'goal modal retires on character change')
check('OpenProgressionGoals()' in read('UI/GameUI.Mobile.cs') and 'OpenProgressionGoals()' in read('UI/GameUI.cs'),'mobile and desktop both open the shared goals surface')
check('DrawProgressionGoalSurface()' in read('UI/GameUI.Expedition.cs') and 'DrawBuildPlanSurface()' in read('UI/GameUI.Expedition.cs'),'camp dispatches goal and saved-build surfaces')
check('AchievementClaimed(id)' in growth and 'candidate.achievementReceipts.Add(id)' in growth and 'CommitCandidate(candidate,true)' in growth,'claimed achievements are recorded durably to prevent duplicate rewards')
check('DrawSkillDevelopmentContent' in read('UI/GameUI.MobileWorkshop.cs') and 'DrawSkillDevelopmentContent' in skills and 'p.SetSpecialization((ElementalistSpecialization)i,session.IsInCamp)' in skills and 'p.SetSummonerRoute((SummonerRoute)i,session.IsInCamp)' in skills,'mobile workshop and skill development retain class-route actions')
check('case CampRouteAction.TrackCore:' in read('UI/GameUI.RouteNextStep.cs') and 'NavigateMerchantExchange();break;' in read('UI/GameUI.RouteNextStep.cs'),'class core action navigates to merchant exchange')
print(f'PASS: {checks} adventure progression UI/lifecycle contracts (not Unity execution)')
