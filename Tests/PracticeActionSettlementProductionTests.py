#!/usr/bin/env python3
"""Real BasicAttack/Melee/OnBasicAttackHitTarget, guard TakeDamageFrom/HitArea,
Enemy.TakeDamage and practice session callbacks. Geometry/energy/accounting are
production; engine presentation and unselected class procs are bounded recipients.
"""
from pathlib import Path
import os,sys,tempfile,subprocess
root=Path(__file__).resolve().parents[1]
def member(s,k):
 a=s.index(k);b=s.index('{',a)+1;n=1
 while n:n+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
def replace_member(s,k,v=''):return s.replace(member(s,k),v)
player=(root/'Assets/Scripts/Combat/PlayerController.cs').read_text();enemy=(root/'Assets/Scripts/Combat/EnemyController.cs').read_text()
fixture=(root/'Tests/ReturningCounterFixture.cs').read_text()
fixture=replace_member(fixture,'static void Main(', 'static void Main(){PracticeSettlementTests();}')
fixture=replace_member(fixture,'public struct CombatDamage')
fixture=replace_member(fixture,'public void TakeDamage(float amount')
fixture=replace_member(fixture,'void OnBasicAttackHitTarget(')
fixture=fixture.replace('public bool IsDead,IsBoss;','public bool IsDead=>Health<=0;public bool IsBoss;')
fixture=fixture.replace('public bool IsDead;public int CombatEpoch;','public bool IsDead=>Health<=0;public int CombatEpoch;')
fixture=fixture.replace('Runtime skillRuntime=new Runtime();','SkillRuntime skillRuntime=new SkillRuntime(HeroClass.Vanguard);')
fixture=fixture.replace('new CombatDamage{Amount=p*100}','new CombatDamage(p*100,false)')
fixture=fixture.replace('public class GameSession{','public partial class GameSession{').replace('public bool InputBlocked;','')
fixture=fixture.replace('public void RecordCombatAction(string s){}','').replace('public void SpawnMechanismText(params object[] a){}','public bool ThrowOnMechanism;public void SpawnMechanismText(params object[] a){if(ThrowOnMechanism)throw new System.InvalidOperationException("presentation boundary");}')
fixture=fixture.replace('var e=new EnemyController();','var e=new EnemyController();e.session=session;')
fixture=fixture.replace('private void Proc', 'private void Proc')
fixture=fixture.replace('public static void WeaponSlash(params object[] a){}','public static void WeaponSlash(params object[] a){}public static void Ring(params object[] a){}')
fixture=fixture.replace('public static void StrikeCone','public static void StrikeArea(params object[] a){}public static void StrikeCone')
fixture=replace_member(fixture,'public static class CombatReviewEvents','public static class CombatReviewEvents{public static bool Enabled;public static void Emit(string kind,string actor,string target=null,float amount=0,int skill=-1,string detail=""){} }')
fixture=fixture.replace('public enum SoundCue{Attack}','public enum SoundCue{Attack,Hit,CriticalHit}')
fixture=fixture.replace('public static void Beam(params object[] a){}','public static void Beam(params object[] a){}public static void Rune(PlayerController p,Vector3 at,float r,Color c,float life,int rank,int identity=0){}')
fixture=fixture.replace('public class Charge{','public class Charge{public void Cancel(){}')
fixture=fixture.replace('public class Model{','public class Model{public Transform transform=new Transform();public void SetBlenderPilotOwnerAlive(bool b){}public void Recoil(Vector3 p,float v){}')
fixture=fixture.replace('public class Bonus{','public class Bonus{public float IncomingDamageMultiplier(float f)=>1;')
fixture=fixture.replace('public class Mastery{','public class Mastery{public float WardReduction;public void Reset(){}public float DamageTaken(float f)=>0;')
fixture=fixture.replace('public class Progress{','public class Progress{public PlayerStats GetStats()=>new PlayerStats();')
fixture=fixture.replace('public class Status{','public class Status{public float DamageMultiplier=1;public void Burn(params object[] a){}public void FrostMark(float f){}public void Freeze(float f){}public void Mark(float a,float b){}public void Poison(params object[] a){}')
fixture=fixture.replace('public Vector3 position,forward=', 'public Quaternion localRotation;public Vector3 position,forward=')
# Keep extraction independent of old counter Main helper without compiling its dodge fixture.
fixture=fixture[:fixture.index('\nnamespace Emberfall{public partial class EnemyController{')]+fixture[fixture.index('\nnamespace Emberfall{public static class MobileControls'):]
methods=''.join(member(player,k) for k in ['private void BasicAttack(','private bool Melee(','internal void OnBasicAttackHitTarget(','public void TakeDamageFrom(','internal void HitArea(','private bool ReturningCounterVariant','private bool ReturningCounterReady'])
focus=(root/'Assets/Scripts/Combat/PlayerController.MobileFocus.cs').read_text()
methods+=''.join(member(focus,k) for k in ['public EnemyController MobilePinnedTarget','internal void ClearMobilePinnedTarget(','internal bool PinMobileTarget(','internal bool MobilePinAppliesToSkill(','internal string MobilePinnedActionReason(','internal bool MobilePinnedActionAllowed('])
# Extract unchanged real practice early branches. Other modes are an explicit throwing boundary.
session=''
for path,key in [('GameSession.cs','public void OnEnemyKilled('),('GameSession.cs','public void OnPlayerDied('),('GameSession.Expedition.cs','public void RecordCombatAction('),('GameSession.Expedition.cs','public void RecordIncomingDamage(')]:
 s=member((root/'Assets/Scripts/Core'/path).read_text(),key);branch=member(s,'if(PracticeActive)');session+=s[:s.index('{')+1]+branch+'throw new System.InvalidOperationException("non-practice session callback outside fixture");}'
session+=member((root/'Assets/Scripts/Core/GameSession.DungeonRewards.cs').read_text(),'public bool CombatEnded')
session+=member((root/'Assets/Scripts/Core/GameSession.cs').read_text(),'public bool InputBlocked')
ns={'__file__':str(root/'Tests/ReturningCounterProductionTests.py')}
# Reuse only that runner's existing math construction, never execute its suite.
h=(root/'Tests/ReturningCounterProductionTests.py').read_text();math_block=h[h.index("math=(r/"):h.index('with tempfile.')];exec(math_block,{'r':root},ns);math=ns['math']
math=math.replace('public static float time=0;','public static float time=0,deltaTime;').replace('public static Vector3 zero=>','public static Vector3 operator -(Vector3 a)=>new Vector3(-a.x,-a.y,-a.z);public static Vector3 zero=>').replace('public static float Sin(', 'public static float Lerp(float a,float b,float t)=>a+(b-a)*t;public static float Sin(')
math+='namespace UnityEngine{public struct Quaternion{public static Quaternion Euler(float a,float b,float c)=>default;}}'
with tempfile.TemporaryDirectory(prefix='practice-settlement-') as d:
 p=Path(d)
 for rel in ['Core/GameTypes','Core/CombatBalance','Core/SkillDamageBudgets','Core/SkillRuntime','Core/CampPracticeRecord','Core/CombatImpactBatch','Core/AdventureResultPolicy','Core/GuardArmorRules','Core/CastFirstHitReceipt','Combat/PlayerUpgradeRules','Combat/ReturningCounterRules','World/WorldTraversal','Core/CombatSightRules','Combat/CombatSight','Combat/CombatDamage','Combat/EnemyControlPolicy','Combat/ProjectileVolleyBudget']:
  path=root/'Assets/Scripts'/(rel+'.cs')
  if not path.exists():path=root/'Assets/Scripts/Core'/(Path(rel).name+'.cs')
  (p/path.name).write_text(path.read_text())
 (p/'Math.cs').write_text('using System;'+math)
 (p/'Fixture.cs').write_text(fixture)
 (p/'Player.cs').write_text('using UnityEngine;namespace Emberfall{public partial class PlayerController{'+methods+'}}')
 (p/'Enemy.cs').write_text('using UnityEngine;namespace Emberfall{public partial class EnemyController{'+member(enemy,'public void TakeDamage(')+'}}')
 (p/'Session.cs').write_text('namespace Emberfall{public partial class GameSession{'+session+'}}')
 sequence=member((root/'Assets/Scripts/Combat/AdvancedSkillSequence.cs').read_text(),'private void Update()')
 (p/'Sequence.cs').write_text('using UnityEngine;namespace Emberfall{public partial class SequenceTerminalProbe{'+sequence+'}}')
 companion=member((root/'Assets/Scripts/Combat/SummonedCompanion.cs').read_text(),'private void Update()')
 # Execute the exact lifetime/simulation gate, with a sentinel at the first downstream step.
 companion=companion[:companion.index('            float dt = Time.deltaTime;')]+'Steps++;}finally{CombatImpactBatch.EndAction();}}'
 (p/'Companion.cs').write_text('namespace Emberfall{public partial class CompanionTerminalProbe{'+companion+'}}')
 (p/'Tests.cs').write_text((root/'Tests/PracticeActionSettlementProductionTests.cs').read_text())
 (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NoWarn>0649;0414;0169</NoWarn></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 sdk=sys.argv[1];env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1')
 def run(args):
  q=subprocess.run([sdk]+args,env=env,capture_output=True,text=True);print(q.stdout+q.stderr);return q
 assert run(['run','--project',str(p/'Test.csproj')]).returncode==0

 for name,old,new,oracle in [
  ('Sequence.cs',sequence,sequence.replace('                CombatImpactBatch.BeginAction();','').replace('                try\n                {','').replace('                }\n                finally { CombatImpactBatch.EndAction(); }','').replace('{','{CombatImpactBatch.BeginAction();try{',1)[:-1]+'}finally{CombatImpactBatch.EndAction();}}','one sequence event clears targets without catchup-event receipts'),
  ('Companion.cs','|| !session.HasStarted || Owner.CombatEpoch','|| !session.HasStarted || session.CombatEnded || Owner.CombatEpoch','ordinary finished-mode companion is retained without another attack'),
  ('CampPracticeRecord.cs','ObjectiveCompleted=true;RequestCombatFinish("目标全部击败");','ObjectiveCompleted=true;Finish("目标全部击败");','last basic records actual clipped energy refund'),
  ('Player.cs',member(player,'private void BasicAttack('),member(player,'private void BasicAttack(').replace('CombatImpactBatch.BeginAction();','').replace('finally { CombatImpactBatch.EndAction(); }','finally { }'),'last lethal counter records complete outer action mechanisms'),
  ('Player.cs',member(player,'public void TakeDamageFrom('),member(player,'public void TakeDamageFrom(').replace('CombatImpactBatch.BeginAction();','').replace('finally { CombatImpactBatch.EndAction(); }','finally { }'),'same-stack incoming damage settles after guard last kill'),
  ('CampPracticeRecord.cs','if(!Finished){Survived=false;RequestCombatFinish','if(!Finished&&pendingCombatEnd==null){Survived=false;RequestCombatFinish','same-action death takes priority over target-clear survival'),
  ('Session.cs','(PracticeActive&&PracticeRecord!=null&&PracticeRecord.Finished)||','', 'future hostile action rejected after settlement'),
  ('Session.cs','(PracticeActive&&PracticeRecord!=null&&PracticeRecord.Finished) ||','', 'terminal practice blocks subsequent player input'),
 ]:
  f=p/name;original=f.read_text();assert old in original;f.write_text(original.replace(old,new));assert run(['build',str(p/'Test.csproj'),'--no-restore','-v:q']).returncode==0
  q=run([str(p/'bin/Debug/net8.0/Test.dll')]);assert q.returncode and oracle in q.stdout+q.stderr,(oracle,q.stdout+q.stderr)
  f.write_text(original);print('PASS compiled negative control: '+oracle)
