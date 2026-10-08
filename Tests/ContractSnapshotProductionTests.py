#!/usr/bin/env python3
"""Production charge/confirmation snapshot test with effects and engine shells."""
from pathlib import Path
import importlib.util,sys,os,tempfile,subprocess
root=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('orders',root/'Tests/CompanionIntentProductionTests.py');orders=importlib.util.module_from_spec(spec);spec.loader.exec_module(orders)
def member(file,signature):
 s=(root/'Assets/Scripts/Combat'/file).read_text();a=s.index(signature);b=s.index('{',a)+1;depth=1
 while depth:depth+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
charge='\n'.join(member('SkillChargeController.cs',sig) for sig in ['public bool Begin(', 'public void Cancel()', 'private void Advance(', 'private bool ValidSnapshotTarget()'])
confirm=member('SkillTargetingController.cs','public bool Confirm()')
shell=orders.shell[:orders.shell.index('class Program{')]
shell=shell.replace('public static float time;','public static float time;public static int frameCount;').replace('public class GameObject {public bool activeInHierarchy=true;}','public class GameObject {public bool activeInHierarchy=true;public void SetActive(bool b){activeInHierarchy=b;}}')
shell=shell.replace('public static class AdvancedSkillVfx {','public class AdvancedSkillVfx {public Transform transform=new Transform();public GameObject gameObject=new GameObject();public static AdvancedSkillVfx Rune(PlayerController o,Vector3 p,float r,Color c,float t,int i,bool b,int identity=0)=>new AdvancedSkillVfx();')
shell=shell.replace('enum HeroClass{Summoner}','enum HeroClass{Summoner,Vanguard,Arcanist}')
shell=shell.replace('public static class CombatSight {','public static class CombatSight {public static bool Direct(Vector3 a,Vector3 b)=>true;')
shell+=r'''
namespace Emberfall {
 public static class CombatReviewEvents {public static bool Enabled=false;public static void Emit(string s,int id,int skill=0){}}
 public static class CombatReviewObjectId {public static int Get(PlayerController p)=>1;}
 public static partial class SnapshotChecks {
  public static int Run(){int n=0;Action<bool,string> check=(ok,why)=>{n++;if(!ok)throw new Exception(why);};
   Time.frameCount=1;var owner=new PlayerController();var game=new GameSession{Player=owner};GameSession.Instance=game;
   var a=new EnemyController();a.transform.position=new Vector3(5,0,0);var b=new EnemyController();b.transform.position=new Vector3(7,0,0);var c=new EnemyController();c.transform.position=new Vector3(6,0,0);game.Enemies.Add(a);game.Enemies.Add(b);game.Enemies.Add(c);
   var targeting=new SkillTargetingController(owner);owner.Targeting=targeting;var charge=new SkillChargeController(owner,game);
   owner.AimTarget=b;owner.AimPoint=b.transform.position;SummonedCompanion.SetFreeFocus(owner,a);
   check(charge.Begin(4)&&charge.TargetEnemy==a&&charge.TargetPoint.x==5,"ordinary spirit snapshots team A instead of incidental B");
   SummonedCompanion.SetFreeFocus(owner,c);check(charge.TargetEnemy==a&&charge.TargetPoint.x==5,"changing team after confirmation cannot retarget pending cast");
   charge.Cancel();check(!charge.IsCharging&&charge.TargetEnemy==null,"cancel clears target snapshot");Time.frameCount++;
   owner.ConfirmCast=(skill,point)=>{owner.AimPoint=point;return charge.Begin(skill);};
   targeting.Select(b.transform.position);check(targeting.Confirm()&&charge.TargetEnemy==b&&charge.TargetPoint.x==7,"explicit desktop confirmation snapshots B despite team C");
   check(!targeting.ConfirmingContract&&SummonedCompanion.ExplicitFocus(owner)==c,"confirmation scope closes without mutating team");
   check(!targeting.Confirm(),"duplicate confirmation cannot cast twice");
   charge.Cancel();Time.frameCount++;owner.AimTarget=null;targeting.Select(new Vector3(3,0,2));
   check(targeting.Confirm()&&charge.TargetEnemy==null&&charge.TargetPoint.x==3&&charge.TargetPoint.z==2,"explicit empty ground remains empty rather than team C");
   charge.Cancel();Time.frameCount++;owner.AimTarget=b;owner.AimPoint=b.transform.position;
   check(charge.Begin(9)&&charge.TargetEnemy==c&&charge.TargetPoint.x==6,"automatic mobile contract uses team C in the same charge path");
   var expected=charge.TargetPoint;c.IsDead=true;bool released=false;
   owner.ExecuteCast=skill=>{released=true;check(charge.TargetEnemy==null&&charge.TargetPoint.x==expected.x,"dead captured target clears while one-shot point stays fixed");return true;};
   charge.Tick(2);check(released&&!charge.IsCharging&&charge.TargetEnemy==null,"release clears pending target");
   Time.frameCount++;owner.AimTarget=b;SummonedCompanion.SetFreeFocus(owner,b);charge.Begin(4);owner.CombatEpoch++;
   released=false;charge.Tick(2);check(!released&&!charge.IsCharging&&charge.TargetEnemy==null,"room change cancels instead of releasing snapshot");
   Time.frameCount++;owner.ConfirmCast=(skill,point)=>{throw new InvalidOperationException("rejected callback");};targeting.Select(b.transform.position);
   try{targeting.Confirm();throw new Exception("expected rejection");}catch(InvalidOperationException){}
   check(!targeting.ConfirmingContract,"exception cannot leak explicit confirmation scope into later casts");
   return n;
  }
 }
 public class SkillTargetingController {
  private PlayerController owner;private int skill=-1,castFrame=-1;public Vector3 TargetPoint;internal bool ConfirmingContract{get;private set;}
  private bool IsTargeting=>skill>=0;private bool Invalid()=>false;public void Cancel(){skill=-1;}
  public SkillTargetingController(PlayerController p){owner=p;}public void Select(Vector3 at){skill=9;TargetPoint=at;}
  CONFIRM
 }
 public class SkillChargeController {
  private PlayerController owner;private GameSession session;private int epoch,cancelledFrame=-1,completedFrame=-1;
  public int SkillIndex=-1;public EnemyController TargetEnemy;public Vector3 TargetPoint,Direction;private float elapsed,duration;
  private AdvancedSkillVfx chargeEffect;public bool IsCharging=>SkillIndex>=0;private bool ConsumedThisFrame=>cancelledFrame==Time.frameCount||completedFrame==Time.frameCount;
  private float Progress=>IsCharging&&duration>0?Mathf.Clamp01(elapsed/duration):0;
  public SkillChargeController(PlayerController p,GameSession g){owner=p;session=g;}
  public void Tick(float dt){Advance(dt);}private static float Duration(HeroClass h,int s)=>1;private void ClearEffect(){chargeEffect=null;}
  CHARGE
 }
}
class Program{static void Main(){Console.WriteLine("PASS: "+Emberfall.SnapshotChecks.Run()+" production contract snapshot assertions");}}
'''.replace('CHARGE',charge).replace('CONFIRM',confirm)
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
with tempfile.TemporaryDirectory(prefix='contract-snapshot-') as folder:
 out=Path(folder);(out/'Replay.cs').write_text(shell);p=cv.write_project(out/'project',[out/'Replay.cs',root/'Assets/Scripts/Combat/CompanionDirective.cs',root/'Assets/Scripts/Combat/CompanionRules.cs',root/'Assets/Scripts/UI/CompanionCommandPresentation.cs'],program='');p.write_text(p.read_text().replace('<OutputType>Library</OutputType>','<OutputType>Exe</OutputType>'))
 config=out/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>');env=os.environ.copy();env.update(DOTNET_CLI_HOME=str(out/'cli'),DOTNET_NOLOGO='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
 dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet';subprocess.run([dotnet,'build',str(p),'--configfile',str(config),'-v:q'],env=env,check=True);subprocess.run([dotnet,str(p.parent/'bin/Debug/net8.0/Validation.dll')],env=env,check=True)
