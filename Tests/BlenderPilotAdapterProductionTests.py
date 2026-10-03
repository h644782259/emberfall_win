#!/usr/bin/env python3
"""Actual pilot adapters, actual PlayAction/CommitActionPose and pre-procedural AnimateHero branch.
Set EMBERFALL_PILOT_SOURCE to the integration worktree; Unity object/import/sampling boundaries are doubles.
"""
import os,sys,tempfile,subprocess
from pathlib import Path
r=Path(__file__).resolve().parents[1];source=Path(os.environ.get('EMBERFALL_PILOT_SOURCE',str(r)));dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet')
def member(s,k):
 a=s.index(k);b=s.index('{',a)+1;n=1
 while n:n+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
model=(source/'Assets/Scripts/Combat/CombatModel.cs').read_text()
animate=member(model,'private void AnimateHero(');animate=animate[:animate.index('            float stride =')]+ '}';assert 'if (SampleBlenderPilot(acting,t,hurt)) return;' in animate
methods='\n'.join(member(model,k) for k in ['public void PlayAction(', 'private void CommitActionPose(', 'private void OnDestroy(', 'public void ApplyEquipment(', 'private static string EquipmentKey(', 'private static void SetVisible(', 'private void SetBaseWeaponVisible(', 'public void CancelAction('])+animate
with tempfile.TemporaryDirectory(prefix='blender-adapter-') as tmp:
 p=Path(tmp)
 (p/'BuildCatalogDamage.cs').write_text('namespace Emberfall{public static class BuildCatalog{'+member((source/'Assets/Scripts/Core/GameTypes.cs').read_text(),'public static float CinderTrailTickMultiplier(')+'}}')
 for f in ['Combat/BlenderPilotVisual','Combat/CombatModel.BlenderPilot','Combat/CombatModel.WeaponRig','Core/RendererGroupCache','Core/BlenderPilotPosePolicy','Core/BasicActionTimeline','Core/SkillDamageBudgets','Core/CombatImpactBatch','Core/CombatBalance','Core/WeaponStructure']:(p/(Path(f).name+'.cs')).write_text((source/('Assets/Scripts/'+f+'.cs')).read_text())
 (p/'Layers.cs').write_text((r/'Tests/BlenderPilotLayerProductionFixture.cs').read_text())
 (p/'Readiness.cs').write_text((r/'Tests/BlenderPilotReadinessTests.cs').read_text())
 (p/'Methods.cs').write_text('using UnityEngine;namespace Emberfall{'+''.join(member((source/'Assets/Scripts/Core/GameTypes.cs').read_text(),signature) for signature in ['public enum EnemyKind','public enum FashionSlot','public enum ItemSlot','public enum Rarity','public enum EquipmentMechanic','public class ItemData'])+'public sealed partial class CombatModel{'+methods+'}}');(p/'Fixture.cs').write_text((r/'Tests/BlenderPilotAdapterProductionFixture.cs').read_text())
 player=(source/'Assets/Scripts/Combat/PlayerController.cs').read_text();(p/'Player.cs').write_text('using System;using UnityEngine;namespace Emberfall{public partial class PlayerController{'+member(player,'public void TakeDamageFrom(')+member(player,'internal void CancelCombatPose(')+member(player,'public void RefreshStats(')+'}}')
 (p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>');proj=p/'Test.csproj';proj.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>')
 for mutation in ['current','automatic-driver','hidden-anchor','contact-zero','restore-all-visible','stale-opt-in','preview-wall-clock','pilot-alternates','early-visible','live-death','supported-refresh','moving-fullbody','respawn-owner','missing-material','unbound-animation','frozen-lower','no-recovery','speed-threshold','ambiguous-bones','bad-parent','partial-visible','lost-root','frozen-action-clock','idle-wall-clock','renderer-invalidation','renderer-release','renderer-detach','preview-short-move']:
  names={'respawn-owner': 'Player.cs', 'live-death': 'Player.cs', 'supported-refresh': 'Methods.cs', 'moving-fullbody': 'CombatModel.BlenderPilot.cs', 'early-visible': 'BlenderPilotVisual.cs', 'automatic-driver': 'BlenderPilotVisual.cs', 'hidden-anchor': 'CombatModel.WeaponRig.cs', 'contact-zero': 'Methods.cs', 'restore-all-visible': 'CombatModel.BlenderPilot.cs', 'stale-opt-in': 'BlenderPilotVisual.cs', 'preview-wall-clock': 'CombatModel.BlenderPilot.cs', 'pilot-alternates': 'CombatModel.WeaponRig.cs', 'missing-material': 'BlenderPilotVisual.cs', 'unbound-animation': 'BlenderPilotVisual.cs'}
  edits={'respawn-owner': ('if(model!=null)model.SetBlenderPilotOwnerAlive(!IsDead);', '', 'actual healing stat refresh allows reused respawn model to sample again'), 'live-death': ('model.SetBlenderPilotOwnerAlive(false);', '', 'actual player lethal damage restores procedural death presentation immediately'), 'supported-refresh': ('SetBlenderPilotVisible(false); // Restore owned renderer states before equipment builders edit them.', '// old supported refresh does not restore before builders', 'fallback restores refreshed null-equipment base visibility'), 'moving-fullbody': ('(!acting || actionBasic || locomotion.Speed <= .05f) &&', '', 'actual moving skill commit falls back instead of fullbody foot sliding'), 'early-visible': ('root.SetActive(false); // first visible frame must already have a sampled pose', '// regression: visible before sampling', 'imported root stays hidden until first sample'), 'pilot-alternates': ('pilotVisible ? -1 :', 'false ? -1 :', 'authored external sword keeps fixed sampled swing side'), 'stale-opt-in': ('private static void ResetForPlayerStartup() { Enabled=false; }', 'private static void ResetForPlayerStartup() {}', 'subsystem startup resets stale opt-in'), 'preview-wall-clock': ('isolatedPreview?1:Time.time-pilotHurtStarted', 'Time.time-pilotHurtStarted', 'isolated preview does not sample wall-clock hit reaction'), 'automatic-driver': ('animator.enabled = false', 'animator.enabled = true', 'manual sampler disables all automatic drivers'), 'hidden-anchor': ('if(pilotVisible && blenderPilot != null', 'if(false && blenderPilot != null', 'ribbon anchor reads the sampled external tip'), 'contact-zero': ('BasicActionTimeline.Contact(heroClass == HeroClass.Ranger)', '0f', 'actual PlayAction commit samples contact .52'), 'restore-all-visible': ('pilotHiddenRenderers[i].enabled=pilotRendererStates[i]', 'pilotHiddenRenderers[i].enabled=true', 'fallback restores each original renderer'), 'missing-material': ('if(material==null||material.shader==null', 'if(material==null)return true;\n            if(material.shader==null', 'invalid pilot readiness keeps original hero: missing material'), 'unbound-animation': ('valid &= changed;', 'valid &= true;', 'invalid pilot readiness keeps original hero: unbound animation')}
  names.update({'frozen-lower':'BlenderPilotVisual.cs','no-recovery':'BlenderPilotVisual.cs','speed-threshold':'BlenderPilotPosePolicy.cs','ambiguous-bones':'BlenderPilotVisual.cs','bad-parent':'BlenderPilotVisual.cs','partial-visible':'CombatModel.BlenderPilot.cs','lost-root':'BlenderPilotVisual.cs','frozen-action-clock':'Methods.cs','idle-wall-clock':'CombatModel.BlenderPilot.cs'})
  names['renderer-detach']='RendererGroupCache.cs'
  edits['renderer-detach']=('                if(observer!=null)observer.Remove(this);\n                observers.RemoveAt(i);','                observers.RemoveAt(i);', 'detached subtree immediately drops old cache but keeps its own live cache')
  names['renderer-release']='Methods.cs'
  edits['renderer-release']=('ReleasePilotRendererGroup();','', 'model destruction restores detached hidden renderer states without another sample')
  names['renderer-invalidation']='RendererGroupCache.cs'
  edits['renderer-invalidation']=('foreach(var owner in owners.ToArray())owner.Invalidate();','','added nested renderer group is hidden on next production sample')
  edits.update({
   'idle-wall-clock':('isolatedPreview?previewTime:Time.time','Time.time','isolated idle final pose uses preview clock'),
   'lost-root':('for(int i=0;i<10;i++)basePose[i].Apply(bones[i]);','for(int i=1;i<10;i++)basePose[i].Apply(bones[i]);','composed local TRS ownership: Root'),
   'frozen-action-clock':('actionAge + dt','actionAge','actual AnimateHero advances authoritative action age by dt'),
   'frozen-lower':('for(int i=0;i<10;i++)basePose[i].Apply(bones[i]);','basePose[0].Apply(bones[0]); // lost lower restoration','composed local TRS ownership: Pelvis'),
   'no-recovery':('float weight=layer.UpperWeight;','float weight=1;','composed local TRS ownership: Spine'),
   'speed-threshold':('SpeedWeight=Clamp(speed)','SpeedWeight=speed>.05f?1:0','composed local TRS ownership: Pelvis'),
   'ambiguous-bones':('if(bones[i]!=null)return false;','if(bones[i]!=null)continue;','layer binding rejected: duplicate Root'),
   'bad-parent':('if(BoneParents[i]>=0 && bones[i].parent!=bones[BoneParents[i]])return false;','// parent unchecked','layer binding rejected: wrong parent Pelvis'),
   'partial-visible':('SetBlenderPilotVisible(false);\n                return false;','return false;','runtime sample failure hides partially composed rig')})
  names['preview-short-move']='Methods.cs'
  edits['preview-short-move']=(' && !(isolatedPreview && actionSkill == -3)','', 'isolated short move restores visible procedural pose instead of frozen pilot idle')
  old=None
  if mutation!='current':
   f=p/names[mutation];old=f.read_text();a,b,expected=edits[mutation];assert a in old;f.write_text(old.replace(a,b))
  q=subprocess.run([dotnet,'build',str(proj),'--configfile',str(p/'NuGet.Config'),'-v:q'],capture_output=True,text=True);assert q.returncode==0,q.stdout+q.stderr
  q=subprocess.run([dotnet,str(p/'bin/Debug/net8.0/Test.dll')],capture_output=True,text=True)
  if old is not None:f.write_text(old);assert q.returncode and expected in q.stderr,q.stdout+q.stderr;print('PASS: compiled '+mutation+' control fails exact adapter assertion')
  else:print(q.stdout,end='');assert q.returncode==0,q.stderr
