#!/usr/bin/env python3
"""Read-only production wiring contracts, not engine execution."""
from pathlib import Path
root=Path(__file__).resolve().parent.parent
read=lambda n:(root/'Assets/Scripts'/n).read_text()
checks=[]
def check(value,message):
    if not value:raise AssertionError(message)
    checks.append(message)
enemy=read('Combat/EnemyController.cs');status=read('Combat/EnemyStatusEffects.cs');pet=read('Combat/SummonedCompanion.cs')
spell=read('Combat/SummonerSpell.cs');charge=read('Combat/SkillChargeController.cs');player=read('Combat/PlayerController.cs')
allfx=read('Combat/CombatEffects.cs');area=allfx[allfx.index('internal sealed class CombatArea'):]
check('advanceBudget.Advance(dt, distance' in enemy and '!advanceBudget.FallbackActive' in enemy,'boss post-fan range gate has bounded approach and arena-pattern fallback')
prepare=enemy[enemy.index('private bool PrepareAttack'):enemy.index('private float ImpactRadius')]
check('advanceBudget.Reset();' in prepare,'every real new attack resets its own approach budget')
select=enemy[enemy.index('private BossAttackPolicy.Move SelectBossMove'):enemy.index('private bool BeginAttack')]
check('BossAttackPolicy.LegalFallback(selected, distance, advanceBudget.FallbackActive,' in select and 'CanUseBossAttack(BossAttackPolicy.Move.Fan, target)' in select,'expired chase resolves to an actually visible ranged attack, including the anti-repeat charge band')
check('AdventureResultPolicy.AcceptsDamage(session.HasStarted,session.CombatEnded)' in enemy,'late ordinary-dungeon and mode damage are terminal guarded')
statusUpdate=status[status.index('private void Update()'):status.index('private void LateUpdate()')]
check('AdvanceBurnClock(dt)' in statusUpdate and 'burnSchedule.Elapse(delta,true)' in status and 'poisonSchedule.Elapse(dt,true)' in statusUpdate and statusUpdate.count('TryTakeDueTick(true)')==2,'both damage statuses share finite scheduler')
check('game.InputBlocked' in status and 'source.CombatEpoch==epoch' in status,'status clocks obey pause and reject stale caster')
check('burnSchedule.Clear()' in status and 'poisonSchedule.Clear()' in status,'consumption/stale source clears pending schedule')
check(statusUpdate.index('poisonSchedule.Elapse(dt,true)')<statusUpdate.index('burnSchedule.TryTakeDueTick(true)') and status.count('!game.InputBlocked&&!game.CombatEnded')==2,'all status clocks advance before callbacks; pauses retain unclaimed ticks')
check('ScheduledTickWindow.Collect(ref nextTick' in area and '&& ticksDrained' in area,'area lifetime and finisher wait for due ticks to drain')
check('interval, 1)' in area and 'pendingTickTargets.Begin(session.Enemies, true)' in area and 'pendingTickTargets.TryTake(out enemy)' in area and '!pendingTickTargets.Pending && ScheduledTickWindow.Drained' in area,'an interrupted area pulse retains original unvisited targets and claims later ticks incrementally')
check('SummonerDamageRules.MarkInterval, 1)' in spell and 'pendingTargets.Begin(session.Enemies)' in spell and 'pendingTargets.TryTake(out enemy)' in spell,'gravity retains an interrupted pulse/finisher and cannot pre-consume the next pulse')
check('if (session.InputBlocked) return;' in area and 'if (session.InputBlocked) return;' in spell and 'session.InputBlocked) { Destroy' not in area and 'session.InputBlocked) { Destroy' not in spell,'opening a choice during a spell pauses it instead of destroying its finite remaining budget')
for component,batch in [(area,'pendingTickTargets'),(spell,'pendingTargets')]:
 check('session.Player == owner && !owner.IsDead && session.HasStarted && !session.CombatEnded && owner.CombatEpoch == epoch' in component and component.count('if (!IsCurrentCast)')>=3,'pending area targets are guarded at entry, each scheduled event, and each individual impact')
 check('private void Retire() { '+batch+'.Clear(); Destroy(gameObject); }' in component and 'private void OnDisable() { castReceipt?.Release();castReceipt=null;'+batch+'.Clear(); }' in component,'epoch/death/terminal retirement and scene disable clear retained targets immediately')
advanced=read('Combat/AdvancedSkillSequence.cs')
check('if (session.InputBlocked || Time.deltaTime <= 0) return;' in advanced and 'if (session.InputBlocked) return;' in advanced,'advanced multi-event catch-up stops at pause boundaries without consuming the next event')
check('CombatSight.Area(transform.position,enemy.transform.position)' in area,'area tick and pull use shared wall visibility')
check('TargetEnemy = contract && !explicitPoint ? SummonedCompanion.ExplicitFocus(owner) ?? owner.AimTarget : owner.AimTarget;' in charge and charge.index('owner.ExecuteChargedSkill(skill);')<charge.index('TargetEnemy = null;',charge.index('owner.ExecuteChargedSkill(skill);')),'charged target snapshot survives through commit then clears')
check('session.Enemies.Contains(TargetEnemy)' in charge and 'owner.CombatEpoch != epoch' in charge,'charge rejects disposed target/old encounter')
check('SkillDamageBudgets.ChargeSeconds(hero, skill)' in charge,'live charge and combo budget use same production duration')
check('executingChargedSkill ? charge.TargetEnemy : AimTarget, executingChargedSkill' in player,'player passes locked target and point intent to summoner')
check('preserveTargetPoint ? CombatSight.GroundPoint' in spell and 'preserveTargetPoint ? commandTarget' in spell,'charged spirit/tree use captured location and target')
check('living.RefreshContractPower(rank)' in pet and 'RefreshPackLifetime(living.RemainingLifetime,rank)' in pet,'full existing pack refreshes even without spawn slots')
check('PreserveRecastHealth(healthBefore,MaxHealth)' in pet,'recast preserves absolute health rather than full heal')
dodge=pet[pet.index('public static void OnPerfectDodge'):pet.index('public static float CommandOpportunityRemaining')]
check('Commands.Grant(Time.time)' in dodge and 'recallTime =' not in dodge and 'commandedTarget =' not in dodge,'perfect dodge protection/token cannot disrupt current pet command')
check('SummonedCompanion.OnPerfectDodge(this)' in player and 'SummonedCompanion.RecallAll(this)' not in player,'perfect dodge no longer forces recall')
check('owner.RegisterSkillHit(castId);' in area and 'player.RegisterSkillHit(propCast);' in spell and 'owner.RegisterSkillHit(castId);' in spell,'confirmed spell enemy hits pass real cast identity')
check('SummonerDamageRules.MarkTickCoefficient' in spell and 'CompanionRules.AttackCoefficient((int)Form)' in pet and 'CompanionRules.AttackInterval((int)Form)' in pet,'summoner and pet runtime use shared budget coefficients')
print('PASS:',len(checks),'combat timing/contract source contracts')
