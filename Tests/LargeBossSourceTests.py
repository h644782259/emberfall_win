#!/usr/bin/env python3
"""Static integration contracts; not a PlayMode or rendering test."""
from pathlib import Path
root=Path(__file__).resolve().parent.parent
read=lambda name:(root/'Assets/Scripts'/name).read_text()
def check(value,why):
    if not value: raise AssertionError(why)
e=read('Combat/EnemyController.cs');m=read('Combat/LargeExpeditionBoss.cs');rig=read('Combat/LargeBossRig.cs');model=read('Combat/CombatModel.cs')
check('largeBoss.Tick(dt)' in e and e.index('largeBoss.Tick(dt)')<e.index('if (chargeTime <= 0) transform.position'),'mechanic owns update before movement/attacks')
check('controlPolicy.TryInterrupt(source.CaptureCastReceipt(castId), attackNumber, IsPreparingAttack' in e and 'if (specialWindup) largeBoss.InterruptWindup();' in e,'existing cast/control policy authorizes special interruption')
check('CancelAttack(); attackNumber++;' in e,'mechanic gets unique windup identity and cancels earlier scheduled attacks')
check('largeBoss.State.IncomingMultiplier' in e,'core exposure modifies actual incoming damage')
check('largeBoss != null ? 1.25f' in e and 'largeBoss != null ? 1.3f' in e,'opt-in navigation and projectile footprints')
check('CombatModel.LargeExpedition(transform)' in e and 'largeBossRig.Animate(speed, attack)' in model,'distinct rig receives actual attack animation')
check('MakeRing()' in rig and 'new Transform[4]' in rig and 'Opening core armour' in rig,'original gyroscopic four-limb core silhouette')
check('PropRecovery.None' in m and 'DestructibleProp.CanPlace(point,.55f)' in m and 'State.CommitAnchors(mask)' in m,'anchors use shared bounded destructibles with no persistent rewards')
check('anchorRoot.SetActive(false);Destroy(anchorRoot)' in m,'old anchors release active registry cap immediately')
check('State.DamagePulse && !game.InputBlocked && !boss.IsDead' in m and 'WorldTraversal.HasClearSweepCapsule(from,to,BeamDangerRadius)' in m,'real damage respects timing, terminal state and cover')
check('game.ModeFinished' in m and 'private void OnDisable(){StopEncounter();}' in m,'completion/background disposal cannot leave delayed hazards')
check('ConfigureArenaBoss(int pattern)' in e and 'LargeExpeditionBoss.Configure' not in read('Core/GameSession.Modes.cs'),'small trial and ordinary bosses are not globally converted')
print('PASS: 12 large-expedition boss source contracts (not engine execution)')
