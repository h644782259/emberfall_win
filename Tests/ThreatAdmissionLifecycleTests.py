"""Execute extracted production attack/lifecycle methods with explicit host shims."""
from CastReceiptFixtureSources import include_cast_receipt_source
from pathlib import Path
import tempfile, subprocess, os
root=Path(__file__).resolve().parents[1]
enemy=(root/'Assets/Scripts/Combat/EnemyController.cs').read_text()
projectile=(root/'Assets/Scripts/Combat/CombatEffects.cs').read_text().split('internal sealed class CombatProjectile',1)[1]
def member(source,key):
 start=source.index(key);brace=source.index('{',start);depth=1;i=brace+1
 while depth:
  if source[i]=='{':depth+=1
  elif source[i]=='}':depth-=1
  i+=1
 return source[start:i]
fixture='''using System;using Emberfall;
static class Time {public static float time;}
struct Vector3 {public static Vector3 up;public static Vector3 operator +(Vector3 a,Vector3 b)=>a;public static Vector3 operator *(Vector3 a,float f)=>a;}
struct Color {public Color(float r,float g,float b){}}
class Transform {public Vector3 position;}
static class Mathf {public static float Max(float a,float b)=>Math.Max(a,b);}
enum EnemyKind {Wisp,Guardian,Slime}
class Budget {public void Reset(){}}
class State {public bool Interruptible;}
class Large {public State State=new State();}
class Session {public void SpawnMechanismText(Vector3 p,string s,Color c){}public void OnEnemyInterrupted(Host h){}}
static class BossAttackPolicy {public enum Move {Slam} public static float Windup(Move m,bool f)=>1;public static float Recovery(bool b)=>1;public const float ComboGap=.2f;}
class Host {
 public ThreatAdmissionPolicy threatAdmission;public int threatMember;
 public bool IsBoss,IsDead,IsEnraged,CanBeSkillInterrupted,preparing,activeChargePose,dodgePending,dodgeRegistered,chargeHit;
 public int attackNumber,repeatedMove,comboRemaining;public float windup,chargeTime,comboDelay,sidestepTime,attackCooldown,totalWindup;
 object dodgePlayer; Vector3 targetPoint;public Transform transform=new Transform();public EnemyKind Kind;
 enum AttackType {Slam,Bolt,Charge,Melee} AttackType attackType;BossAttackPolicy.Move previousMove;
 Budget advanceBudget=new Budget();Large largeBoss;Session session=new Session();public int warnings;
 void CreateWarning(){warnings++;}void ClearWarning(){warnings=0;}void StopAllCoroutines(){}void CancelInvoke(){}
 BossAttackPolicy.Move ToMove(AttackType t)=>BossAttackPolicy.Move.Slam;
 public bool Start()=>PrepareAttack(AttackType.Slam,false,new Vector3());public void Cancel()=>CancelAttack(true);public void Finish()=>FinishAttack();
'''+''.join(member(enemy,k) for k in ['private bool AdmitThreatAttack()','private bool PrepareAttack(','private void CancelAttack(','private void FinishAttack()'])+'''}
class Bolt {CastFirstHitReceipt castReceipt;System.Action hostileEnded;public Bolt(Action callback){hostileEnded=callback;}public void End()=>OnDisable();'''+member(projectile,'private void OnDisable()')+'''}
static class Program {static void C(bool b,string m){if(!b)throw new Exception(m);}static void Main(){
 var p=new ThreatAdmissionPolicy(12);var a=new Host{threatAdmission=p,threatMember=1};var b=new Host{threatAdmission=p,threatMember=2};
 C(a.Start(),"first actual prepare");C(a.preparing&&a.warnings==1&&a.attackNumber==1,"real warning started");
 C(!b.Start()&&b.attackNumber==0&&b.warnings==0&&!b.preparing,"denied prepare changes no attack state");
 p.Advance(.6f);Time.time+=.6f;C(b.Start(),"second actual prepare");var c=new Host{threatAdmission=p,threatMember=3};C(!c.Start(),"third blocked");
 a.Cancel();C(!a.preparing&&a.warnings==0&&p.ActiveCount==1,"actual interruption clears warning and lease");
 p.Advance(.6f);Time.time+=.6f;C(c.Start(),"waiting can start after release");c.Finish();C(p.ActiveCount==1,"actual finish releases melee");
 var end=p.LaunchVolley(2,2);var x=new Bolt(end);var y=new Bolt(end);b.Finish();b.IsDead=true;b.Cancel();C(p.ActiveCount==1,"real finish/death retains flying lease");
 x.End();x.End();C(p.ActiveCount==1,"actual disable exactly once");y.End();C(p.ActiveCount==0,"last actual bolt ends danger");
 Console.WriteLine("Actual PrepareAttack/CancelAttack/FinishAttack/projectile OnDisable: 11 checks PASS");}}
'''
if os.environ.get('THREAT_MUTATE_SKIP_ADMISSION')=='1':
 fixture=fixture.replace('if (!AdmitThreatAttack()) return false;', '')
with tempfile.TemporaryDirectory(prefix='threat-lifecycle-') as tmp:
 p=Path(tmp);(p/'Fixture.cs').write_text(fixture);(p/'ThreatAdmissionPolicy.cs').write_bytes((root/'Assets/Scripts/Core/ThreatAdmissionPolicy.cs').read_bytes())
 (p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><NoWarn>0649;0169;0414</NoWarn></PropertyGroup></Project>');include_cast_receipt_source(p/'Test.csproj')
 env=os.environ.copy();env.update(DOTNET_CLI_HOME=str(p/'home'),DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1',DOTNET_CLI_TELEMETRY_OPTOUT='1',DOTNET_GENERATE_ASPNET_CERTIFICATE='false')
 subprocess.run([os.environ.get('DOTNET','dotnet'),'run','--project',str(p/'Test.csproj')],env=env,check=True)
