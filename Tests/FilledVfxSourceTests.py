#!/usr/bin/env python3
"""Static wiring checks only. These do not compile shaders or render Unity scenes."""
from pathlib import Path
root=Path(__file__).resolve().parent.parent
read=lambda p:(root/p).read_text()
checks=[]
def check(value,label):
    if not value:raise AssertionError(label)
    checks.append(label)
fx=read('Assets/Scripts/Combat/FilledSkillVfx.cs');recipes=read('Assets/Scripts/Core/FilledVfxRecipes.cs')
old=read('Assets/Scripts/Combat/AdvancedSkillVfx.cs');combat=read('Assets/Scripts/Combat/CombatEffects.cs')
seq=read('Assets/Scripts/Combat/AdvancedSkillSequence.cs');summon=read('Assets/Scripts/Combat/SummonerSpell.cs')
shader=read('Assets/Resources/FilledSpell.shader');threat=read('Assets/Resources/ThreatBoundary.shader')
check('LineRenderer' not in fx and 'LineRenderer' not in old,'primary filled effects do not construct lines')
check('MeshFilter' in fx and 'MeshRenderer' in fx and 'RecalculateNormals' in fx,'real world-space mesh surfaces with normals')
check('private static Mesh crescent, crystal, flame, sword, lightning, arcane, rupture, arcaneShard, arrow, vine;' in fx and 'private static Material sharedMaterial;' in fx,'distinct reusable identity meshes and one shared material')
check('MaximumParts=14, ReducedParts=7, DesktopEffects=20, MobileEffects=12' in recipes,'bounded mobile/reduced resource policy')
check('CombatVisualLease.Attach(root,priority,()=>{if(fx.rentGeneration==generation)fx.Retire();})' in fx and 'if(count>=cap)return' in fx and 'private void OnDisable(){finales.Remove(this);Release();}' in fx,'runtime caps and immediate release')
check('owner.CombatEpoch!=epoch' in fx and 'game.ModeFinished' in fx and 'game.InputBlocked||Time.deltaTime<=0' in fx,'no stale character/room or paused advancement')
check('FilledSkillVfx.Crescent(' in combat,'ordinary slash uses filled volume')
area=combat[combat.index('internal sealed class CombatArea'):]
update=area[area.index('private void Update()'):area.index('private void Retire()')]
claim=update.index('if (ScheduledTickWindow.Collect(ref nextTick, age, delay + duration, interval, 1) == 0) break;')
claim_scope=update.index('if (!pendingTickTargets.Pending)')
visual=update.index('if (!solidImpactSpawned)')
delivery=update.index('while (pendingTickTargets.Pending)')
check(claim_scope<claim<update.index('pendingTickTargets.Begin(session.Enemies, true)')<visual<delivery,
      'filled impact begins only after claiming a due damage pulse, inside its new-pulse boundary')
check('FilledSkillVfx.Impact' not in update[:claim] and 'ElementalCombatVfx.Area' not in update[:claim] and
      update.index('if (session.InputBlocked) return;')<claim,
      'startup and paused frames cannot emit the damage impact before an eligible scheduled pulse')
check('solidImpactSpawned = true;' in update[visual:delivery] and 'if (!solidImpactSpawned)' not in update[delivery:],
      'resuming unfinished pulse targets cannot replay the one-time filled impact')
check('FilledSkillVfx.Charge(obj.transform' in combat,'startup envelope owned by cancellable area')
check('ElementalCombatVfx.Area(transform, radius' in combat and 'ElementalCombatVfx.Area(obj.transform' not in combat,'field emission waits for startup to finish')
check('FilledSkillVfx.Impact(owner,target' in seq and 'owner.ElementalAdvancedArea(target' in seq,'advanced elemental impact hooks share actual sequence events')
check('if (partner != null) FilledSkillVfx.Impact' in summon,'summon emergence requires actual contract partner')
check('ZTest LEqual' in shader and 'ZWrite Off' in shader and 'Cull Off' in shader,'filled surfaces respect scene depth without writing opaque floor coverage')
check('renderQueue=3070' in fx and 'ZTest Always' in threat,'enemy threat outlines remain higher priority')
check('TakeDamage' not in fx and 'Physics.' not in fx,'cosmetic volumes cannot add damage or colliders')
check('Resources.Load<Shader>("FilledSpell")' in fx and 'Shader.Find("Sprites/Default")' in fx,'resource-loaded production shader has a conservative material fallback')
print('PASS:',len(checks),'filled VFX source contracts (not shader/runtime verification)')
