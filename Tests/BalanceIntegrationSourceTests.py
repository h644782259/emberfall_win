from pathlib import Path
r=Path(__file__).resolve().parents[1]
def read(name):return(r/'Assets/Scripts'/name).read_text()
p=read('Combat/PlayerController.cs');a=read('Combat/AdvancedSkillSequence.cs');fx=read('Combat/CombatEffects.cs');s=read('Core/GameSession.cs');d=read('Core/GameSession.DungeonRewards.cs');preview=read('Combat/SkillTargetingController.cs')
checks=[]
def check(ok,label):
 if not ok:raise AssertionError(label)
 checks.append(label)
check('AdvancedSequenceMultiplier' not in read('Core/CombatBalance.cs') and 'SkillDamageBudgets.AdvancedScale(HeroClass,slot)' in p,'live advanced casts select per-skill budget')
check(p.count('SkillDamageBudgets.EarlyField(HeroClass,rank)')==3,'all three early fields share production full-event allocation')
check('SkillDamageBudgets.MeteorAftermath(rank)' in p and 'volley:shards' in p,'meteor tail and frost overlap have explicit budgets')
check('SkillDamageBudgets.AdvancedSteps' in a and 'SkillDamageBudgets.AdvancedImpact' in a and 'SkillDamageBudgets.AdvancedTail' in a,'advanced event count, primary and tail use same audit catalog')
check('new ProjectileVolleyBudget<EnemyController>(CombatAttack,SkillDamageBudgets.FanTargetCap(rank))' in p and 'volley:volley' in p,'fan allocates one shared budget per cast')
check('volley.Apply(enemy,damage,false)' in fx and 'volley.Apply(enemy,damage,true)' in p and 'castId,volley' in fx,'direct arrows and all explosions share per-target cap')
check('guardCastId=castId' in p and 'castId:guardCastId' in p and 'guardPower, .5f, .2f,guardCastId' in p,'guard retaliation and pulses retain original cast identity')
check('color,castId);' in p and 'castId==0?hero.NewCastId():castId' in a,'advanced sequence retains parent cast identity')
check('CombatSight.Area(at,enemy.transform.position)' in p and 'CombatSight.Chain(previous,enemy.transform.position)' in a,'area and each chain hop reject cover')
check('CombatSight.GroundPoint(transform.position,transform.position+transform.forward*3f*range)' in p,'distant auxiliary shock cannot originate behind a wall')
check(p.count('CombatProjectile.CanLaunchFromMuzzle')==2 and 'CanLaunchFromMuzzle(player,muzzle,amount,attackId)' in fx,'short launch segments are checked without suppressing remote spell centers')
check('CombatSight.Melee(transform.position, enemy.transform.position)' in p and 'CombatSight.GroundPoint' in preview,'melee and placement use shared distinct reach policy')
check('BoundaryPoint' in preview and 'WorldTraversal.Revision' in preview and 'renderedTarget-TargetPoint' in preview,'cover-aware preview caches only unchanged geometry')
check('=>' not in read('Combat/CombatSight.cs'),'runtime boundary clipping allocates no per-ray closures')
check('EnemyController selected=AimTarget;' in p and 'HeroClass==HeroClass.Summoner&&skill==9&&ValidAimTarget(selected)' in p,'summon ultimate selection survives ground confirmation into charge snapshot')
check('SettleMasteryCombo(enemy);' in p and 'masteryCore.BasicHit()' in read('Combat/PlayerController.MasteryCombo.cs') and 'masteryCore.SkillSpent(' in p and 'masteryCore.DamageTaken(' in p and 'masteryCore.PerfectDodge()' in p,'four cores have distinct actual combat triggers')
check('masteryCore.Reset();coreWardTime=0' in p and 'oldCore!=masteryCore.Core||oldTier!=masteryCore.Tier' in p,'runtime core and ward state reset on epoch or configuration change')
check('!session.CombatEnded' in p and 'session.CombatEnded' in fx,'terminal ordinary/mode callbacks cannot rearm core or deal late projectile damage')
check('QueueDungeonCompletion();' in s and 'TrySettleDungeonReward();' in s and 'Progression.Profile.clearedRuns++' not in s,'ordinary clear uses one pending atomic settlement')
zone=s[s.index('private bool ChangeZone('):s.index('private void SpawnWildernessEnemy(')]
check(s.count('if(DungeonRewardPending&&!TrySettleDungeonReward())return false;')==1 and
      'if (!loadingSaveSnapshot && !enteringChapter && !SaveBeforeLeaving()) return false;' in zone and
      zone.index('SaveBeforeLeaving()')<zone.index('Enemies.Clear();'),
      'save/leave settles pending clear; scene preflights the same checked path before teardown')
chapter=read('Core/GameSession.Chapter.cs')
entry=chapter[chapter.index('public bool ConfirmChapterEnter()'):chapter.index('private void ResetChapterRun()')]
check(entry.index('if(!SaveBeforeLeaving())return false;')<entry.index('Progression.TryBeginChapterNode(')<entry.index('enteringChapter=true;') and
      'finally {enteringChapter=false;' in entry,'chapter bypass follows successful preflight and cannot leak outside scoped entry')
check('ClearDungeonSettlement();' in read('Core/GameSession.Expedition.cs') and 'pendingDungeonRewardId=null;' in d,'explicit discard/new run clears old pending receipt')
check('TryCompleteDungeonRun(pendingDungeonRewardId' in d and 'TotalEarnedExperience(Progression.Profile)-xp' in d,'ordinary recap uses committed actual deltas')
for file in ['Core/GameSession.cs','Core/GameSession.Expedition.cs','Core/GameSession.Modes.cs','Core/GameSession.RoomChain.cs']:
 check('DungeonEntryLevel' in read(file) and 'Progression.Profile.level + DungeonTier' not in read(file),'fixed entry-level single scaling in '+file)
check('boss, InDungeon ? DungeonTier : 0)' in s,'dungeon loot receives the actual tier and wilderness retains its zero-tier roll')
check('TierRewardBand.Materials(4,DungeonTier)' in read('Core/GameSession.RoomChain.cs') and read('Core/ExpeditionModeState.cs').count('TierRewardBand.Materials')==3,'all new modes use the same bounded tier schedule')
check('AdventureEntryPresentation.Materials(session.SelectedArenaMode,session.SelectedDungeonTier)' in read('UI/GameUI.Modes.cs') and 'TierRewardBand.Materials(mode==-1?3:mode==3?4:mode+1,tier)' in read('UI/AdventureEntryPresentation.cs'),'entry reward preview uses actual tier schedule')
mobile=read('UI/GameUI.Mobile.cs')
check('if(Button(interact' not in mobile and 'finally{BlockUITransitionForFinger(triggeringFinger);}' in mobile,'context has one dispatch owner and consumes initiating pointer through transition')
check('ui.ActivateMobileInteraction(finger)' in read('UI/MobileControls.cs') and 'uiTransition.IsBlocked' in read('UI/GameUI.Exit.cs'),'touch and simulated mouse share the pointer-specific release latch')
print('PASS:',len(checks),'third-review integration source contracts (not engine execution)')
