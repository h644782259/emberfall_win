"""Actual Enemy Update/Prepare/Cancel with production queue; physics/render are shims."""
from pathlib import Path
import importlib.util,os,subprocess,tempfile,sys
root=Path(__file__).resolve().parents[1];sdk=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet')
spec=importlib.util.spec_from_file_location('blocked',root/'Tests/BlockedCombatUpdateProductionTests.py');m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
source=(root/'Assets/Scripts/Combat/EnemyController.cs').read_text()
methods=['void Update()', 'void ResolveAttack()', 'void DamageTarget(float amount)', 'bool RegroupMobileSupport(float dt,float speed)', 'bool InsideImpact(Vector3 point)', 'void FinishAttack()', 'Vector3 WalkForAnimation(Vector3 displacement)', 'bool BeginAttack()', 'bool PrepareAttack(', 'bool AdmitThreatAttack()', 'void CancelAttack(', 'void OnDisable()']
body='using UnityEngine;namespace Emberfall{public partial class EnemyController{'+''.join(m.method(source,s) for s in methods)+'}}'
fixture=(root/'Tests/BlockedCombatUpdateProductionTests.cs').read_text()
fixture=fixture.replace('bool BeginAttack(){throw new Exception("unexpected begin");}','').replace('public class AdvanceBudget{','public class AdvanceBudget{public void Reset(){}').replace('public static class BossAttackPolicy{','public static class BossAttackPolicy{public static float Windup(Move m,bool b)=>.85f;').replace('public class GameSession{','public class GameSession{public void SpawnMechanismText(Vector3 p,string s,Color c){}public void OnEnemyInterrupted(EnemyController e){}').replace('public class Boss{','public class Boss{public void StopEncounter(){}').replace('throw new Exception("unexpected bolt spawn");','if(onEnded!=null)onEnded();')
fixture=fixture.replace('  enum AttackType{','  bool dodgeRegistered;object dodgePlayer;void StopAllCoroutines(){}void CancelInvoke(){}\n  BossAttackPolicy.Move ToMove(AttackType t)=>BossAttackPolicy.Move.Slam;AttackType ToAttack(BossAttackPolicy.Move m)=>AttackType.Slam;\n  enum AttackType{')
fixture=fixture.replace('public partial class CombatProjectile:MonoBehaviour{public static void Hostile','public partial class CombatProjectile:MonoBehaviour{private void Update(){throw new Exception("projectile Update outside fairness fixture; lifecycle has separate suite");}public static void Hostile')
fixture+='''
namespace Emberfall{public partial class EnemyController{
 public void ConfigureFair(ThreatAdmissionPolicy p,int id){threatAdmission=p;threatMember=id;Kind=id<2?EnemyKind.Wisp:EnemyKind.Guardian;preparing=false;attackCooldown=0;aggro=true;}
 public bool PrepareFair()=>PrepareAttack(Kind==EnemyKind.Wisp?AttackType.Bolt:AttackType.Slam,false,session.Player.transform.position);
 public void CompleteFair(){preparing=false;FinishAttack();attackCooldown=0;}public void DieFair(){IsDead=true;CancelAttack();}public void DisableFair(){OnDisable();}
 public int Starts=>attackNumber;public void MoveAway(){transform.position=new Vector3(0,0,40);} }
}
class FairnessProgram{static int checks;static void C(bool b,string why){checks++;if(!b)throw new System.Exception(why);}static void Main(){
 foreach(float frameDelta in new[]{.25f,.05f,.2f,.5f,-1f})foreach(bool hostFirst in new[]{true,false}){
 var game=new GameSession();var p=new ThreatAdmissionPolicy(7319);var enemies=new EnemyController[4];for(int i=0;i<4;i++){enemies[i]=new EnemyController(game);enemies[i].ConfigureFair(p,i);game.Enemies.Add(enemies[i]);}
 float dt=frameDelta>0?frameDelta:.05f;float clock=0,last=-999;int[] served=new int[4];
 // Constant ready demand executes real PrepareAttack and FinishAttack, in stable Wisp-first order.
 for(int frame=0;frame<1600;frame++){if(frameDelta<0)dt=frame%19==0?.5f:frame%7==0?.2f:.05f;clock+=dt;Time.time=clock;if(hostFirst)p.Advance(dt);for(int id=0;id<4;id++)if(enemies[id].PrepareFair()){C(clock-last>=.35f-.0001f,"no underspaced telegraphs");last=clock;served[id]++;C(p.ActiveCount<=2,"active budget");enemies[id].CompleteFair();}if(!hostFirst)p.Advance(dt);}
 foreach(int count in served)C(count>20,"coarse-frame persistent candidates cannot starve");C(System.Math.Abs(served[0]-served[3])<=1,"FIFO survives stable Update order");
 // Execute the actual Update range rejection and death/disable cleanup, not synthetic Release calls.
 p.Reset();C(enemies[0].PrepareFair(),"start anchor");C(!enemies[2].PrepareFair()&&!enemies[3].PrepareFair(),"queue two guards");
 enemies[2].MoveAway();Time.deltaTime=dt;enemies[2].Tick();C(p.WaitingCount==1,"actual out-of-range Update withdraws waiter");enemies[3].DieFair();C(p.WaitingCount==0,"actual death cancel withdraws waiter");
 p.Reset();enemies[0].CompleteFair();C(enemies[0].PrepareFair(),"next anchor");C(!enemies[1].PrepareFair(),"queue disabled member");enemies[1].DisableFair();C(p.WaitingCount==0,"actual disable unregisters waiter");
 p.Reset();enemies[0].CompleteFair();enemies[0].PrepareFair();enemies[1].PrepareFair();p.Advance(.5f);C(p.WaitingCount==1,"500ms spike preserves request identity");p.Advance(.2f);C(p.WaitingCount==0,"missing complete Update pass reclaims dead requester");
 }
 // Real Update advances windup/recovery and resolves attacks with the same fixed roster.
 foreach(float dt in new[]{.2f,.25f,.5f}){var game=new GameSession();var p=new ThreatAdmissionPolicy(7319);var enemies=new EnemyController[4];for(int i=0;i<4;i++){enemies[i]=new EnemyController(game);enemies[i].ConfigureFair(p,i);game.Enemies.Add(enemies[i]);}
 for(int frame=0;frame<600;frame++){Time.deltaTime=dt;Time.time+=dt;p.Advance(dt);foreach(var e in enemies)e.Tick();C(p.ActiveCount<=2,"real Update cap");}
 foreach(var e in enemies)C(e.Starts>10,"real Update four participants served");}
 System.Console.WriteLine("PASS "+checks+" production Update/Prepare/Finish/Cancel fairness assertions across 50/200/250/500ms frames and both host orders; managed only");}}
'''
math=(root/'Tests/DestructibleTraversalTests.cs').read_text();math='using System;using UnityEngine;'+math[math.index('namespace Emberfall'):]
for d in ['public static class CombatFx','public static class PlayerUpgradeRules','public struct Vector3','public static class Time','public static class Mathf']:math=math.replace(d,d.replace('class ','partial class ').replace('struct ','partial struct '))
with tempfile.TemporaryDirectory(prefix='threat-fairness-') as tmp:
 p=Path(tmp);(p/'Enemy.cs').write_text(body);(p/'Fixture.cs').write_text(fixture);(p/'Math.cs').write_text(math)
 # Compile the same catalog coefficient consumed by the real venom rule; no duplicated balance values.
 catalogSource=(root/'Assets/Scripts/Core/GameTypes.cs').read_text()
 start=catalogSource.index('public static float ConcentratedVenomCoefficient(');end=catalogSource.index('{',start)+1;depth=1
 while depth:
  depth+=(catalogSource[end]=='{')-(catalogSource[end]=='}');end+=1
 catalog=catalogSource[start:end]
 (p/'BuildCatalog.cs').write_text('namespace Emberfall{public static class BuildCatalog{'+catalog+'}}')
 for rel in ['Assets/Scripts/Core/ThreatAdmissionPolicy.cs','Assets/Scripts/Core/DestructiblePropRules.cs','Assets/Scripts/Combat/ConcentratedVenomRules.cs','Assets/Scripts/World/WorldTraversal.cs','Assets/Scripts/World/WorldTraversal.Platforms.cs','Assets/Scripts/Combat/EnemyImpactRegion.cs']:(p/Path(rel).name).write_bytes((root/rel).read_bytes())
 (p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>');proj=p/'Test.csproj';proj.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><NoWarn>0649;0414;0169</NoWarn></PropertyGroup></Project>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'home'),DOTNET_CLI_TELEMETRY_OPTOUT='1',DOTNET_GENERATE_ASPNET_CERTIFICATE='false',DOTNET_NOLOGO='1')
 result=subprocess.run([sdk,'run','--project',str(proj)],env=env,capture_output=True,text=True);print(result.stdout,result.stderr);assert result.returncode==0

 legacy=(root/'Tests/Fixtures/LegacyTimeExpiryThreatAdmission.txt').read_text()
 # API-only bridge: exact old Request/Advance bodies remain unchanged.
 legacy=legacy.replace('public bool Request(int id)', 'public bool Request(int id,float ignored)=>Request(id);\n        public bool Request(int id)')
 (p/'ThreatAdmissionPolicy.cs').write_text(legacy)
 build=subprocess.run([sdk,'build',str(proj),'--no-restore','-v:q'],env=env,capture_output=True,text=True);print(build.stdout,build.stderr);assert build.returncode==0
 old=subprocess.run([sdk,str(p/'bin/Debug/net8.0/Test.dll')],env=env,capture_output=True,text=True);print(old.stdout,old.stderr)
 assert old.returncode!=0 and 'coarse-frame persistent candidates cannot starve' in old.stderr
 print('PASS exact 608e659 time-expiry baseline compiles and fails coarse-frame starvation assertion')
 (p/'ThreatAdmissionPolicy.cs').write_bytes((root/'Assets/Scripts/Core/ThreatAdmissionPolicy.cs').read_bytes())
 (p/'Enemy.cs').write_text(body.replace('threatAdmission.Request(threatMember,Time.time)','threatAdmission.Request(threatMember)'))
 build=subprocess.run([sdk,'build',str(proj),'--no-restore','-v:q'],env=env,capture_output=True,text=True);print(build.stdout,build.stderr);assert build.returncode==0
 old=subprocess.run([sdk,str(p/'bin/Debug/net8.0/Test.dll')],env=env,capture_output=True,text=True);print(old.stdout,old.stderr)
 assert old.returncode!=0 and 'no underspaced telegraphs' in old.stderr
 print('PASS compiled lagging-host-clock mutation rejected by mixed-frame actual spacing')
