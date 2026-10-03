#!/usr/bin/env python3
"""Actual state anchors, pooled authored cage, MPB and healing sequence; managed Unity boundary, not rendering."""
from CastReceiptFixtureSources import include_cast_receipt_source
from pathlib import Path
import os,sys,tempfile,subprocess
root=Path(__file__).resolve().parents[1]
ns={'__file__':str(root/'Tests/DefenseIdentityProductionTests.py')};exec((root/'Tests/DefenseIdentityProductionTests.py').read_text().split('evidence_path=',1)[0],ns)
shell=ns['stubs'];player=ns['fixture'];member=ns['member'];body=(root/'Assets/Scripts/Combat/AdvancedSkillSequence.cs').read_text()
shell=shell.replace('public bool HasStarted,ModeFinished,InputBlocked;', 'public bool HasStarted,ModeFinished,InputBlocked,CombatEnded;public float ActualHealed;public void RecordActualHealing(float n){ActualHealed+=n;}public void SpawnFloatingText(params object[] a){}')
shell=shell.replace('public static class CombatFx{','public static class CombatFx{public static void Ring(params object[] a){}')
shell=shell.replace('public static float Min(float a,float b)', 'public static int CeilToInt(float n)=>(int)Math.Ceiling(n);public static float Min(float a,float b)')
shell=shell.replace('public float Opacity,TintAlpha;public void SetPropertyBlock(MaterialPropertyBlock block){Opacity=block.Opacity;TintAlpha=block.TintAlpha;}', 'public float Opacity,TintAlpha,EnvelopeMode,EnvelopeAge;public void SetPropertyBlock(MaterialPropertyBlock block){Opacity=block.Opacity;TintAlpha=block.TintAlpha;EnvelopeMode=block.EnvelopeMode;EnvelopeAge=block.EnvelopeAge;}')
shell=shell.replace('public float Opacity,TintAlpha=1;', 'public float Opacity,EnvelopeMode,EnvelopeAge,TintAlpha=1;').replace('if(name=="_Opacity")Opacity=value;', 'if(name=="_Opacity")Opacity=value;if(name=="_EnvelopeMode")EnvelopeMode=value;if(name=="_EnvelopeAge")EnvelopeAge=value;')
ptext=(root/'Assets/Scripts/Combat/PlayerController.cs').read_text()
player=player.replace('internal void TriggerPassive()',member(ptext,'public void Heal(')+'\ninternal void HealingProtection(int rank){}internal void RestoreSkillEnergy(float n){skillRuntime.RestoreEnergy(n);}\ninternal void TriggerPassive()')
budget=(root/'Assets/Scripts/Core/SkillDamageBudgets.cs').read_text();actualbudgets=''.join(member(budget,k) for k in ['public static int AdvancedSteps(','public static float AdvancedInterval(','public static float AdvancedFirstEvent('])
player=player.replace('public const float BasicEnergyOnHit=1;', 'public const float BasicEnergyOnHit=1;private static int Rank(int n)=>Math.Max(1,Math.Min(3,n));'+actualbudgets)
fields=body[body.index('        private PlayerController owner;'):body.index('        public static void Spawn(')]
sequence='''using System;using UnityEngine;namespace Emberfall{
public class EnemyController{}public struct CombatDamage{}public static class SummonedCompanion{public static float Healed;public static void HealAll(PlayerController h,float n){Healed+=n;}}
internal sealed class HealingProbe:MonoBehaviour {
FIELDS
public void Setup(PlayerController h,int r,bool restricted){owner=h;session=GameSession.Instance;heroClass=h.HeroClass;skill=6;rank=r;epoch=h.CombatEpoch;range=1;color=new Color(.4f,1,.6f);restrictedHealing=restricted;Configure();}
public void Tick(float delta){Time.deltaTime=delta;Update();}
public void Pulse(){Healing();}public void SetFinal(){step=steps-1;}
private EnemyController Nearest(Vector3 p,float r)=>throw new Exception("nonhealing route");private void Vanguard()=>throw new Exception("nonhealing route");private void Arcanist()=>throw new Exception("nonhealing route");private void Ranger()=>throw new Exception("nonhealing route");private void Pull(Vector3 a,float b,float c)=>throw new Exception("nonhealing route");
METHODS
}}
'''.replace('FIELDS',fields).replace('METHODS',''.join(member(body,k) for k in ['private void Configure(','private void Update(','private void Healing(','private void OnDisable(']))
pet=(root/'Assets/Scripts/Combat/SummonedCompanion.cs').read_text()
petbody='public class SummonedCompanion:MonoBehaviour{public static float Healed;public static System.Collections.Generic.List<SummonedCompanion> active=new System.Collections.Generic.List<SummonedCompanion>();public PlayerController Owner;public GameSession session;public int epoch;public float Health,MaxHealth=100;'+member(pet,'public bool IsAlive')+member(pet,'public static void HealAll(').replace('            foreach (var pet in active)','            Healed+=fraction;foreach (var pet in active)')+'}'
sequence=sequence.replace('public static class SummonedCompanion{public static float Healed;public static void HealAll(PlayerController h,float n){Healed+=n;}}',petbody)
with tempfile.TemporaryDirectory(prefix='protection-presentation-') as tmp:
 p=Path(tmp);(p/'Stubs.cs').write_text(shell);(p/'Player.cs').write_text(player);(p/'Sequence.cs').write_text(sequence);(p/'Test.cs').write_text((root/'Tests/ProtectionPresentationProductionTests.cs').read_text())
 for f in ['Core/CombatImpactBatch','Core/FilledVfxRecipes','Core/FilledVfxPlacement','Core/CombatVisualBudget','Combat/CombatVisualLease','Combat/AnchoredImpactMesh','Combat/FilledSkillVfx','Combat/AuthoredActorMeshes','Combat/AuthoredSpellBases','Combat/AdvancedSkillVfx']:(p/(Path(f).name+'.cs')).write_text((root/('Assets/Scripts/'+f+'.cs')).read_text())
 (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>');include_cast_receipt_source(p/'Test.csproj');(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 sdk=sys.argv[1] if len(sys.argv)>1 else 'dotnet';cmd=[sdk,'run','--project',str(p/'Test.csproj'),'--',str(root/'Assets/Resources')];env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1')
 subprocess.run(cmd,env=env,check=True)
 for file,old,new,oracle in [('Sequence.cs','if(owner.Health>healthBefore)','if(true)','full HP produces no healing-success pulse'),('Sequence.cs','if(pet.Health>before)','if(true)','full caster wounded companion heals only real recipient')]:
  f=p/file;original=f.read_text();assert old in original;f.write_text(original.replace(old,new));r=subprocess.run(cmd,env=env,capture_output=True,text=True);f.write_text(original);assert r.returncode!=0 and oracle in r.stdout+r.stderr,r.stdout+r.stderr;print('PASS compiled negative control',oracle)
 for file,old,new,oracle in [('AdvancedSkillVfx.cs','passive?1:0','0','passive identity channel distinct'),('Sequence.cs','if(healingAura!=null){healingAura.Stop();healingAura=null;}','','sequence disable immediately releases healing state'),('FilledSkillVfx.cs','block.SetFloat("_EnvelopeMode",0);','', 'rental reset removes envelope mode')]:
  f=p/file;original=f.read_text();assert old in original;f.write_text(original.replace(old,new));r=subprocess.run(cmd,env=env,capture_output=True,text=True);f.write_text(original);assert r.returncode!=0 and oracle in r.stdout+r.stderr,r.stdout+r.stderr;print('PASS compiled negative control',oracle)
shader=(root/'Assets/Resources/FilledSpell.shader').read_text();assert 'if(_EnvelopeMode>.5)' in shader and '_EnvelopeMode>2.5 && _EnvelopeMode<3.5' in shader
print('PASS shader envelope-only branch source contract; shader pixels require Unity acceptance')
