#!/usr/bin/env python3
"""Execute real status clocks, selection query and counter grant bodies; managed boundaries only."""
from pathlib import Path
import os,sys,tempfile,subprocess,re
root=Path(__file__).resolve().parents[1];dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
def member(source,signature):
 a=source.index(signature);b=source.index('{',a)+1;depth=1
 while depth:depth+=(source[b]=='{')-(source[b]=='}');b+=1
 return source[a:b]
fixture=(root/'Tests/BurnFinaleProductionTests.cs').read_text().split('public static class BurnFinaleProductionTests')[0]
fixture=fixture.replace('public enum HeroClass{Arcanist}','public enum HeroClass{Arcanist,Vanguard,Ranger,Summoner}public enum EquipmentMechanic{ReturningBlade}')
fixture=fixture.replace('public static Vector3 zero=>','public Vector3 normalized=>this;public static Vector3 up=>new Vector3(0,1,0);public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator *(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);public static Vector3 zero=>')
fixture=fixture.replace('public float ApplyControl(float duration)=>duration;','public float ApplyControl(float duration)=>IsBoss?0:duration;')
fixture=fixture.replace('public void RecordClassTutorial(HeroClass hero){}','public Progression Progression=new Progression();public void RecordCombatAction(string text){}public void SpawnMechanismText(Vector3 at,string text,Color color){}public void RecordClassTutorial(HeroClass hero){}')
rules=(root/'Assets/Scripts/Combat/PlayerUpgradeRules.cs').read_text()
constants=''.join(re.findall(r'public const float (?:CounterWindow|PerfectDodgeEnergy) = [^;]+;',rules))
fixture=fixture.replace('public const float PoisonDetonationTicks=3;','public const float PoisonDetonationTicks=3;'+constants)
fixture+='''namespace Emberfall{
 public class Progression{public Profile Profile=new Profile();}public class Profile{public int[] skillRanks=new int[10];}
 public class SkillTargetingController{public bool IsTargeting;public int TargetedSkillIndex;public Vector3 TargetPoint;}
 public static class MobileControls{public static bool Active;}
 public static class GameBalance{public static float SkillRangeMultiplier(int rank)=>1;public static Color ClassColor(HeroClass hero)=>new Color();}
 public static class AdvancedSkillVfx{public static void Beam(PlayerController player,Vector3 a,Vector3 b,Color c,float d,float w){}}
 public static class SummonedCompanion{public static void OnPerfectDodge(PlayerController p){}}
 public class Runtime{public void RestoreEnergy(float amount){}}
 public class Mastery{public float PerfectDodge()=>0;}
 public sealed partial class PlayerController{
 public HeroClass HeroClass=HeroClass.Vanguard;public Vector3 aimPoint;public SkillTargetingController targeting;public bool ReturningCounterVariant,OwnReturningBlade=true;
 public EnemyController BounceTarget;public float CombatAttack=1,Energy=100;Runtime skillRuntime=new Runtime();Mastery masteryCore=new Mastery();
 float counterTime,perfectDodgeCounterTime,perfectDodgeWindow,coreWardTime,classDodgeTime;bool perfectDodgeAwarded;private float counterWindowDuration;
 CombatProcCooldown returningBladeProc=new CombatProcCooldown();
 private bool HasMechanic(EquipmentMechanic mechanic)=>OwnReturningBlade;
 private EnemyController NearestOtherEnemy(Vector3 at,EnemyController excluded,float radius)=>BounceTarget;
 private Vector3 ResolveSkillGroundTarget(Vector3 point,float range)=>point;
 private void ResolveMobileSkillAim(int skill,out EnemyController enemy,out Vector3 point){enemy=null;point=aimPoint;}
 public void GrantPerfect(bool variant){ReturningCounterVariant=variant;perfectDodgeAwarded=false;perfectDodgeWindow=.1f;NotifyPerfectDodge();}
 public void ResetBounceProc(){returningBladeProc=new CombatProcCooldown();}
 }
}'''
player=(root/'Assets/Scripts/Combat/PlayerController.cs').read_text()
query=(root/'Assets/Scripts/Combat/PlayerController.Opportunities.cs').read_text()
proc=member(player,'if (HeroClass == HeroClass.Vanguard && HasMechanic(EquipmentMechanic.ReturningBlade) && !ReturningCounterVariant)')
timers=''.join(line for line in player.splitlines(True) if line.strip().startswith(('counterTime = Mathf.Max(0, counterTime - dt);','perfectDodgeCounterTime = Mathf.Max(0, perfectDodgeCounterTime - dt);')))
methods='using UnityEngine;namespace Emberfall{public sealed partial class PlayerController{'+member(query,'internal CombatOpportunityState ElementalOpportunityWindow(')+member(player,'public void NotifyPerfectDodge(')+member(player,'internal float CounterOpportunityDuration')+member(player,'internal float CounterOpportunityRemaining')+'public void AdvanceCounter(float dt){'+timers+'}public void Bounce(EnemyController enemy){Vector3 position=enemy.transform.position;'+proc+'}}}'
with tempfile.TemporaryDirectory(prefix='opportunity-duration-') as tmp:
 p=Path(tmp)
 for file in ['Combat/CombatDamage','Combat/EnemyStatusEffects','Core/ScheduledTickWindow','Core/BurnFinaleReceipts','Core/CombatOpportunityState']:(p/(Path(file).name+'.cs')).write_text((root/('Assets/Scripts/'+file+'.cs')).read_text())
 (p/'Fixture.cs').write_text(fixture);(p/'Player.cs').write_text(methods)
 (p/'Rules.cs').write_text('using System;using System.Collections.Generic;namespace Emberfall{'+member(rules,'public sealed class RecentCastGate')+member(rules,'public sealed class CombatProcCooldown')+'}')
 (p/'Tests.cs').write_text((root/'Tests/OpportunityDurationProductionTests.cs').read_text())
 (p/'Program.cs').write_text('System.Console.WriteLine(OpportunityDurationProductionTests.Run());')
 (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>')
 (p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1');build=[dotnet,'build',str(p/'Test.csproj'),'--configfile',str(p/'NuGet.Config'),'-v:q'];run=[dotnet,str(p/'bin/Debug/net8.0/Test.dll')]
 def execute():
  q=subprocess.run(build,env=env,capture_output=True,text=True);print(q.stdout+q.stderr,flush=True);q.check_returncode()
  return subprocess.run(run,env=env,capture_output=True,text=True)
 q=execute();print(q.stdout+q.stderr,flush=True);q.check_returncode()
 for file,old,new,oracle in [
 ('EnemyStatusEffects.cs','if(duration>=frostMarkTime)frostWindowDuration=duration;','frostWindowDuration=duration;','short frost refresh retains denominator'),
 ('EnemyStatusEffects.cs','if(duration>=poisonSchedule.Remaining)poisonWindowDuration=duration;','poisonWindowDuration=duration;','short poison refresh retains denominator'),
 ('Player.cs','duration=kind==CombatOpportunityKind.Shatter?status.FrostWindowDuration:status.BurnWindowDuration;','duration=expiry;','multi target pairs duration with longest remaining'),
 ('Player.cs','counterWindowDuration = counterTime = perfectDodgeCounterTime =','counterTime = perfectDodgeCounterTime =','normal perfect counter grant duration')]:
  path=p/file;original=path.read_text();assert old in original;path.write_text(original.replace(old,new));q=execute();print('NEGATIVE CONTROL',file,old,flush=True);print(q.stdout+q.stderr,flush=True)
  assert q.returncode and oracle in q.stdout+q.stderr,'negative must fail designated oracle'
  path.write_text(original)
 print('PASS: four compiled duration regressions rejected; no Unity execution claim')
