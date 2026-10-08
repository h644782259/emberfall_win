#!/usr/bin/env python3
"""Actual retained sequence fields/Configure/Update/arrow event/OnDisable against actual pooled VFX."""
from CastReceiptFixtureSources import include_cast_receipt_source
from pathlib import Path
import tempfile,subprocess,sys,os
root=Path(__file__).resolve().parents[1]
setup=(root/'Tests/FilledVfxPoolProductionTests.py').read_text().split('with tempfile.TemporaryDirectory',1)[0];ns={'__file__':str(root/'Tests/FilledVfxPoolProductionTests.py')};exec(setup,ns);shell=ns['s'].replace('public static class CombatFx{','public static class CombatFx{public static void Ring(params object[] a){}')+'namespace Emberfall{public static class GameBalance{public const float ArcanistPulseRadius=8f;}}'
shell=shell.replace('public bool IsDead;public int CombatEpoch;', 'public bool IsDead;public int CombatEpoch;public int Hits;public void HitArea(Vector3 at,float r,CombatDamage d,float k=0,float s=0,int castId=0){Hits++;}public void HealingProtection(int rank){}')
shell=shell.replace('public bool HasStarted,ModeFinished,InputBlocked;', 'public bool HasStarted,ModeFinished,InputBlocked,CombatEnded;public bool CombatEffectsEnded=>CombatEnded;')
shell=shell.replace('public static Vector3 forward=>','public static Vector3 right=>new Vector3(1,0,0);public static Vector3 forward=>')
source=(root/'Assets/Scripts/Combat/AdvancedSkillSequence.cs').read_text()
def member(marker):
 a=source.index(marker);b=source.index('{',a)+1;depth=1
 while depth:depth+=(source[b]=='{')-(source[b]=='}');b+=1
 return source[a:b]
fields=source[source.index('        private PlayerController owner;'):source.index('        public static void Spawn(')]
ranger=source[source.index('        private void Ranger()'):];a=ranger.index('                case 9:')+len('                case 9:');b=ranger.index('                    break;',a);event=ranger[a:b]
fixture='''using System;using UnityEngine;namespace Emberfall{
public enum HeroClass{Vanguard,Arcanist,Ranger,Summoner}public class EnemyController{}
public struct CombatDamage{public float Amount;public static CombatDamage operator*(CombatDamage d,float n){d.Amount*=n;return d;}}
public static class SkillDamageBudgets{public static int AdvancedSteps(HeroClass h,int s,int r)=>3;public static float AdvancedInterval(HeroClass h,int s)=>.1f;public static float AdvancedFirstEvent(HeroClass h,int s)=>0;public static float AdvancedImpact(HeroClass h,int s,int r,int step)=>1;public static int RadialArrowCount(HeroClass h,int s,int r)=>12;public const float RadialArrowCoefficient=1;}
public static class CombatProjectile{public static void Friendly(PlayerController h,GameSession s,Vector3 a,Vector3 b,CombatDamage d,Color c,bool p,bool arrow,bool basic,float size,float speed,int castId){}}
public class AdvancedSkillVfx{public static AdvancedSkillVfx Healing(PlayerController h,float r,Color c,float life,int detail,System.Func<bool> active)=>new AdvancedSkillVfx();public void Stop(){}public static void Rune(params object[] values){} }
internal sealed class SequenceProbe:MonoBehaviour {
FIELDS
public FilledSkillVfx.ArrowBatchHandle Handle=>arrowBatch;
public void Setup(PlayerController h,int id){owner=h;session=GameSession.Instance;heroClass=HeroClass.Ranger;skill=9;rank=1;epoch=h.CombatEpoch;castId=id;range=1;target=new Vector3(1,0,2);color=new Color(1,1,1);damage=new CombatDamage{Amount=10};Configure();}
private EnemyController Nearest(Vector3 p,float r)=>null;private void Healing(){}private void Vanguard(){}private void Arcanist(){}private void Pull(Vector3 p,float r,float s){}
private void Ranger(){EVENT}
CONFIGURE
UPDATE
DISABLE
private static Vector3 Circle(float a,float r)=>new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r);
}}'''.replace('FIELDS',fields).replace('EVENT',event).replace('CONFIGURE',member('        private void Configure()')).replace('UPDATE',member('        private void Update()')).replace('DISABLE',member('        private void OnDisable()'))
# Optional named identity argument requires a typed stub on the unused Rune branch.
fixture=fixture.replace('public static void Rune(params object[] values){}','public static void Rune(PlayerController h,Vector3 at,float size,Color c,float duration,int detail,bool follow=false,int identity=0){}')
with tempfile.TemporaryDirectory(prefix='arrow-batch-generation-') as d:
 p=Path(d);(p/'Stubs.cs').write_text(shell);(p/'SequenceProbe.cs').write_text(fixture)
 for f in ['Assets/Scripts/Core/CombatImpactBatch.cs','Assets/Scripts/Core/FilledVfxRecipes.cs','Assets/Scripts/Core/FilledVfxPlacement.cs','Assets/Scripts/Core/CombatVisualBudget.cs','Assets/Scripts/Combat/CombatVisualLease.cs','Assets/Scripts/Combat/AnchoredImpactMesh.cs','Assets/Scripts/Combat/FilledSkillVfx.cs','Assets/Scripts/Combat/AuthoredActorMeshes.cs','Assets/Scripts/Combat/AuthoredSpellBases.cs','Tests/ArrowBatchGenerationProductionTests.cs']:(p/Path(f).name).write_text((root/f).read_text())
 (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>');include_cast_receipt_source(p/'Test.csproj');(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1');command=[sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet'),'run','--project',str(p/'Test.csproj'),'--',str(root/'Assets/Resources')]
 subprocess.run(command,env=env,check=True)
 target=p/'FilledSkillVfx.cs';original=target.read_text();guard='effect.rentGeneration==generation &&';assert guard in original;target.write_text(original.replace(guard,''));result=subprocess.run(command,env=env,capture_output=True,text=True)
 assert result.returncode!=0 and 'stale sequence Update cannot mutate new rental snapshot' in result.stdout+result.stderr,(result.stdout,result.stderr)
 print('PASS removing only rental-generation guard fails actual stale sequence Update/full snapshot despite same owner and same castId.')

 target.write_text(original.replace('if(fx.rentGeneration==generation)fx.Retire();','fx.Retire();'));result=subprocess.run(command,env=env,capture_output=True,text=True)
 assert result.returncode!=0 and 'stale lease callback cannot mutate new rental snapshot' in result.stdout+result.stderr,(result.stdout,result.stderr)
 print('PASS removing lease callback generation guard fails full protected new-rental snapshot.')
