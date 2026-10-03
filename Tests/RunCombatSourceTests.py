#!/usr/bin/env python3
"""Read-only wiring checks; not a Unity play/render test."""
from pathlib import Path
root=Path(__file__).resolve().parent.parent
read=lambda p:(root/p).read_text(encoding='utf-8')
checks=[]
def check(condition,message):
    if not condition: raise AssertionError(message)
    checks.append(message)
p=read('Assets/Scripts/Combat/PlayerController.cs')
check('session != null && session.InDungeon ? session.RunChoices : null' in p,'run modifiers require active dungeon context')
check('ActiveRunBonuses.AttackMultiplier' in p and 'bonus.CriticalMultiplier' in p and 'ActiveRunBonuses.AttackSpeedMultiplier' in p,'attack crit and speed are wired into player combat')
check('ActiveRunBonuses.CooldownMultiplier' in p and 'ActiveRunBonuses.ExtraEnergyPerSecond' in p,'cooldown and energy bonuses have live hooks')
check('ActiveRunBonuses.IncomingDamageMultiplier' in p,'survival bonus affects actual incoming damage')
check('skillRuntime.ResetCooldowns();' in p and 'public void ResetCooldownsForDungeonEntry()' in p,'successful-entry reset API reaches actual skill timers')
check('skillIndex:slot,castId:castId' in p and 'statusSkill:1,statusRank:rank,castId:castId' in p,'melee and trap pass real cast identity')
e=read('Assets/Scripts/Combat/EnemyController.cs')
check('controlPolicy.TryInterrupt(source.CaptureCastReceipt(castId), attackNumber, IsPreparingAttack' in e,'enemy requires current windup and per-cast budget')
check('source != session.Player' in e and 'WorldTraversal.HasLineOfSight' in e,'interrupt rejects stale owner and occluded target')
check('windup = chargeTime = comboDelay = sidestepTime = 0;' in e and 'StopAllCoroutines();' in e and 'CancelInvoke();' in e,'interrupt cancels every scheduled attack stage')
check('if (!preparing || IsDead' in e,'stale attack resolution is guarded')
check('Vector3.ClampMagnitude(knockVelocity' in e and 'WorldTraversal.Move' in e,'knockback is bounded and respects terrain')
status=read('Assets/Scripts/Combat/EnemyStatusEffects.cs')
check('enemy.ApplyControl(duration)' in status and 'enemy.ControlStunRemaining' in status,'status visuals follow granted control instead of extending immunity')
fx=read('Assets/Scripts/Combat/CombatEffects.cs')
check('skillIndex, castId, impact.Amount, impact.IsCritical, impact.CriticalMultiplier' in fx and 'statusSkill, castId, damage.Amount, damage.IsCritical, damage.CriticalMultiplier' in fx,'projectile budget and area preserve their actual critical multiplier')
t=read('Assets/Scripts/Combat/EnemyAttackTelegraph.cs')
check('ThreatVisualStyle.Material()' in t and 'Resources.Load<Shader>("ThreatBoundary")' in read('Assets/Scripts/Combat/ThreatVisualStyle.cs') and 'sortingOrder = 120' in t and 'SetInterruptible' in t,'warning keeps visible boundary and distinct interrupt cue')
shader=read('Assets/Resources/ThreatBoundary.shader')
check('ZTest Always' in shader and 'ZWrite Off' in shader,'thin threat outlines remain readable above floor effects')
for source in ['CombatEffects','SummonerSpell','AdvancedSkillSequence']:
    text=read('Assets/Scripts/Combat/'+source+'.cs')
    check('enemy.ApplyPull(' in text and 'enemy.transform.position = WorldTraversal.Move' not in text,'every '+source+' pull uses the shared per-frame target budget')
check('SummonedCompanion.TransferPermanentPartners(this);' in p[p.index('public void ResetCooldownsForDungeonEntry()'):p.index('public void TakeDamage(')],'entry reset preserves permanent companions across its epoch change')
print('PASS:',len(checks),'run-combat source wiring contracts (not engine execution)')
