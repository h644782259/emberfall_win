"""Production basic/melee/perfect-dodge + ground geometry; managed engine recipients."""
from pathlib import Path
import sys,subprocess,tempfile,os
r=Path(__file__).resolve().parents[1];dotnet=sys.argv[1]
def member(s,key):
 a=s.index(key);b=s.index('{',a)+1;d=1
 while d:d+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
s=(r/'Assets/Scripts/Combat/PlayerController.cs').read_text()
timers=''.join(line for line in s.splitlines(True) if line.strip().startswith(('counterTime = Mathf.Max(0, counterTime - dt);','perfectDodgeCounterTime = Mathf.Max(0, perfectDodgeCounterTime - dt);')))
methods=''.join(member(s,k) for k in ['private void BasicAttack(','private bool Melee(','public void NotifyPerfectDodge(','private bool ReturningCounterVariant','private bool ReturningCounterReady'])
focus=(r/'Assets/Scripts/Combat/PlayerController.MobileFocus.cs').read_text()
methods+=''.join(member(focus,k) for k in ['public EnemyController MobilePinnedTarget','internal void ClearMobilePinnedTarget(','internal bool PinMobileTarget(','internal bool MobilePinAppliesToSkill(','internal string MobilePinnedActionReason(','private string MobilePinnedActionReasonFor(','internal bool MobilePinnedActionAllowed('])
# Execute the unchanged complete proc branch separately; surrounding on-hit features are independent.
branch=member(s,'if (HeroClass == HeroClass.Vanguard && HasMechanic(EquipmentMechanic.ReturningBlade) && !ReturningCounterVariant)')
math=(r/'Tests/DestructibleTraversalTests.cs').read_text();math=math[math.index('namespace UnityEngine'):]
math=math.replace('public static Vector3 zero=>new Vector3();','public static Vector3 zero=>new Vector3();public static Vector3 up=>new Vector3(0,1,0);public static Vector3 Cross(Vector3 a,Vector3 b)=>new Vector3(a.y*b.z-a.z*b.y,a.z*b.x-a.x*b.z,a.x*b.y-a.y*b.x);public static float Angle(Vector3 a,Vector3 b)=>(float)(Math.Acos(Math.Max(-1,Math.Min(1,Dot(a.normalized,b.normalized))))*180/Math.PI);')
with tempfile.TemporaryDirectory(prefix='return-counter-') as t:
 p=Path(t)
 for f in ['Core/CombatImpactBatch','Core/GameTypes','Core/CombatBalance','Core/SkillDamageBudgets','Combat/PlayerUpgradeRules','Combat/ReturningCounterRules','World/WorldTraversal','Core/CombatSightRules','Combat/CombatSight','Combat/EnemyImpactRegion','Combat/BossAttackPolicy']:(p/(Path(f).name+'.cs')).write_text((r/('Assets/Scripts/'+f+'.cs')).read_text())
 (p/'Math.cs').write_text('using System;'+math)
 (p/'Fixture.cs').write_text((r/'Tests/ReturningCounterFixture.cs').read_text())
 enemy=(r/'Assets/Scripts/Combat/EnemyController.cs').read_text()
 (p/'Enemy.cs').write_text('using UnityEngine;namespace Emberfall{public partial class EnemyController{'+''.join(member(enemy,k) for k in ['public bool TryRegisterPerfectDodge(','private static bool FinitePoint(','private bool PendingDodgeIsValid(','private void ConfirmImpactDodge(','private bool InsideImpact('])+'}}')
 (p/'Player.cs').write_text('using UnityEngine;namespace Emberfall{public partial class PlayerController{'+methods+'void AdvanceWindow(float dt){'+timers+'}void Proc(EnemyController enemy){Vector3 position=enemy.transform.position;'+branch+'}}}')
 (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NoWarn>0649;0414</NoWarn></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1');build=[dotnet,'build',str(p/'Test.csproj'),'--configfile',str(p/'NuGet.Config'),'-v:q'];run=[dotnet,str(p/'bin/Debug/net8.0/Test.dll')]
 subprocess.run(build,env=env,check=True);subprocess.run(run,env=env,check=True)
 for old,new,expected in [('if(skill<0&&ReturningCounterReady)','if(false)','real locked target3.7 admission'),('if(skill<0&&ReturningCounterReady)','if(skill<0)','empty dodge no extended admission'),('&& !ReturningCounterVariant)','&& true)','B no bounce or return grant'),('ReturningCounterVariant ? 3f','ReturningCounterVariant ? 2f','blocked admission spends no cooldown or counter'),('<= .35f+enemy.HitFootprintBonus','<= 3.5f+enemy.HitFootprintBonus','B narrow side rejection')]:
  f=p/'Player.cs';original=f.read_text();assert old in original;f.write_text(original.replace(old,new));subprocess.run(build,env=env,check=True);result=subprocess.run(run,env=env,capture_output=True,text=True);assert result.returncode and expected in result.stdout+result.stderr,result.stdout+result.stderr;f.write_text(original);print('PASS compiled negative control: '+expected)

 rules=p/'ReturningCounterRules.cs';original=rules.read_text();rules.write_text(original.replace('return "目标被遮挡";','return "";'))
 subprocess.run(build,env=env,check=True);result=subprocess.run(run,env=env,capture_output=True,text=True)
 assert result.returncode and 'blocked counter button reason' in result.stdout+result.stderr,result.stdout+result.stderr
 print('PASS compiled negative control: shared predictor cannot allow a blocked landing')

# The actual blink entry only registers candidates; only impact resolution grants reward.
blink=member((r/'Assets/Scripts/Combat/PlayerController.cs').read_text(),'private bool TryBlinkCore(')
assert 'NotifyPerfectDodge(' not in blink and 'TryRegisterPerfectDodge(origin, destination)' in blink
print('PASS actual blink registration source contract: empty dodge grants no counter')
