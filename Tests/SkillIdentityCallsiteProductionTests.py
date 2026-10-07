#!/usr/bin/env python3
"""Execute Healing/Charge with loaded identities and actual hit methods with observable rendering boundaries."""
from CastReceiptFixtureSources import include_cast_receipt_source
from pathlib import Path
import tempfile,subprocess,os,sys
root=Path(__file__).resolve().parents[1]
# Reuse only the clearly bounded Unity/resource doubles from the integration runner.
setup=(root/'Tests/AuthoredSpellIntegrationTests.py').read_text().split('with tempfile.TemporaryDirectory',1)[0]
ns={'__file__':str(root/'Tests/AuthoredSpellIntegrationTests.py')};exec(setup,ns);stubs=ns['s']
stubs=stubs.replace('public bool IsDead;public int CombatEpoch;', 'public bool IsDead;public int CombatEpoch;public float Health=1,MaxHealth=100,Healed,Energy;public void Heal(float n){float delta=Math.Min(n,MaxHealth-Health);Health+=delta;Healed+=delta;}public void RestoreSkillEnergy(float n){Energy+=n;}')
stubs=stubs.replace('public static class CombatFx{','public static class CombatFx{public static void Ring(params object[] args){}').replace('public static Vector3 forward=>','public static Vector3 right=>new Vector3(1,0,0);public static Vector3 forward=>')
source=(root/'Assets/Scripts/Combat/AdvancedSkillSequence.cs').read_text();a=source.index('        private void Healing()');b=source.index('        private void Vanguard()',a);method=source[a:b]
# Release-mode flag is an explicit input; the Healing body and loaded identity are production code.
fixture='''using System;using System.Linq;using System.Reflection;using Emberfall;using UnityEngine;
namespace Emberfall {public enum HeroClass{Vanguard,Arcanist,Ranger,Summoner}public static class SummonedCompanion{public static float Healed;public static void HealAll(PlayerController p,float n){Healed+=n;}}public static class AdvancedSkillVfx{public static int Beams;public static void Beam(params object[] a){Beams++;}public static void Rune(params object[] a){}}
class HealingProbe{private bool restrictedHealing;private PlayerController owner;private int rank=3,step=4,steps=5;private float range=1;private Color color=new Color(1,1,1);private HeroClass heroClass;private static Vector3 Circle(float a,float r)=>new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r);public HealingProbe(PlayerController p,HeroClass h,bool restricted){owner=p;heroClass=h;restrictedHealing=restricted;}public void Run(){Healing();}
METHOD
}}
class Program{static void Main(string[] args){Resources.Root=args[0];foreach(bool restricted in new[]{false,true})foreach(HeroClass h in Enum.GetValues(typeof(HeroClass))){SummonedCompanion.Healed=0;typeof(FilledSkillVfx).GetMethod("ResetAssets",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);var owner=new GameObject("Hero").AddComponent<PlayerController>();GameSession.Instance=new GameSession{Player=owner,HasStarted=true};new HealingProbe(owner,h,restricted).Run();if(Math.Abs(owner.Healed-(restricted?16:11))>0.001||owner.Energy!=8)throw new Exception("real heal/rank3 continuation changed");if(Math.Abs(SummonedCompanion.Healed-(h==HeroClass.Summoner?.11f:0))>.0001f)throw new Exception("companion healing fraction unchanged across modes");var fx=GameObject.All.Last(o=>o.activeInHierarchy&&o.GetComponent<FilledSkillVfx>()!=null);var part=GameObject.All.First(o=>o.transform.parent==fx.transform);if(!part.GetComponent<MeshFilter>().sharedMesh.name.StartsWith("Skill identity / ProtectionCage"))throw new Exception("real heal missing independent identity");if(AdvancedSkillVfx.Beams!=0)throw new Exception("new healing stacks generic old beams");}Console.WriteLine("PASS actual Healing event -> real authored ProtectionCage, all4 hero classes x normal/restricted mode, self pulse11/16 and rank3 energy8, companion11% preserved; managed, not Unity.");}}
'''.replace('METHOD',method)
with tempfile.TemporaryDirectory(prefix='identity-callsite-') as d:
 p=Path(d);(p/'Stubs.cs').write_text(stubs);(p/'Test.cs').write_text(fixture)
 for f in ['Core/FilledVfxRecipes','Core/FilledVfxPlacement','Core/CombatVisualBudget','Combat/CombatVisualLease','Combat/AnchoredImpactMesh','Combat/FilledSkillVfx','Combat/AuthoredActorMeshes','Combat/AuthoredSpellBases']:(p/(Path(f).name+'.cs')).write_text((root/('Assets/Scripts/'+f+'.cs')).read_text())
 (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>');include_cast_receipt_source(p/'Test.csproj');(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 subprocess.run([sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet'),'run','--project',str(p/'Test.csproj'),'--',str(root/'Assets/Resources')],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'))
summon=(root/'Assets/Scripts/Combat/SummonerSpell.cs').read_text()
assert summon.index('enemy.TakeDamage(impact.Amount')<summon.index('if(enemy.Health<contactHealth)FilledSkillVfx.IdentityContact')
assert source.index('nearest.TakeDamage(damage.Amount*')<source.index('if(nearest.Health<endpointHealth)')
assert 'ElementalCombatVfx.Lightning(previous + Vector3.up * 1.15f, position + Vector3.up * 1.15f);' in source
charge=(root/'Assets/Scripts/Combat/SkillChargeController.cs').read_text();assert 'owner.ExecuteChargedSkill(skill);' in charge and 'ClearEffect();' in charge
print('PASS source hook contracts: actual health reduction gates lightning/contract endpoint feedback; dynamic lightning line retained; release/cancel owner retained.')

def run_probe(name,files,body,shell,mutations=()):
 with tempfile.TemporaryDirectory(prefix='identity-'+name+'-') as d:
  p=Path(d);(p/'Stubs.cs').write_text(shell);(p/'Test.cs').write_text(body)
  for dest,text in files.items():(p/dest).write_text(text)
  (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>');include_cast_receipt_source(p/'Test.csproj');(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
  command=[sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet'),'run','--project',str(p/'Test.csproj'),'--',str(root/'Assets/Resources')];env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1')
  subprocess.run(command,env=env,check=True)
  for file,old,new,reason in mutations:
   target=p/file;before=target.read_text();assert old in before;target.write_text(before.replace(old,new));r=subprocess.run(command,env=env,capture_output=True,text=True);target.write_text(before)
   assert r.returncode!=0 and reason in r.stdout+r.stderr,(reason,r.stdout,r.stderr)
   print('PASS negative control:',reason)

charge_shell=ns['s'].replace('public bool IsDead;public int CombatEpoch;', '''public bool IsDead;public int CombatEpoch;public HeroClass HeroClass;public EnemyController AimTarget;public Vector3 AimPoint;public int Releases,Cancelled;public bool CanBeginSkillTargeting(int i)=>true;public void CancelCombatPose(){Cancelled++;}public void ExecuteChargedSkill(int i){Releases++;}''')
charge_shell=charge_shell.replace('public bool HasStarted,ModeFinished,InputBlocked;', 'public bool HasStarted,ModeFinished,InputBlocked;public float ArenaRadius=30;public Progression Progression=new Progression();public List<EnemyController> Enemies=new List<EnemyController>();')
charge_shell=charge_shell.replace('public static float Wall=', 'public static Vector3 GroundPoint(Vector3 a,Vector3 b)=>b;public static float Wall=')
charge_shell=charge_shell.replace('public Vector3 right=>localRotation.Rotate', 'public Vector3 forward=>localRotation.Rotate(Vector3.forward);public Vector3 right=>localRotation.Rotate')
charge_shell=charge_shell.replace('public static Vector3 Lerp(', 'public static Vector3 ClampMagnitude(Vector3 v,float m)=>v.magnitude>m?v.normalized*m:v;public static Vector3 Lerp(')
charge_support=r'''
using System;using System.Linq;using System.Reflection;using Emberfall;using UnityEngine;
namespace Emberfall {
 public enum HeroClass{Vanguard,Arcanist,Ranger,Summoner}public class EnemyController:MonoBehaviour{public bool IsDead;}
 public class Progression{public Profile Profile=new Profile();}public class Profile{public int[] skillRanks=Enumerable.Repeat(1,10).ToArray();}
 public class SkillTargetingController:MonoBehaviour{public bool ConfirmingContract;}
 public static class SummonedCompanion{public static EnemyController ExplicitFocus(PlayerController h)=>null;}
 public static class SkillDamageBudgets{public static float ChargeSeconds(HeroClass h,int i)=>.3f;}
 public static class GameBalance{public const float ArcanistFinaleRadius=9.5f;public static float SkillRangeMultiplier(int r)=>1;public static Color ClassColor(HeroClass h)=>new Color(1,1,1);}
 public static class CombatReviewEvents{public static bool Enabled;public static void Emit(string n,int id,int skill){}}public static class CombatReviewObjectId{public static int Get(PlayerController p)=>1;}
}
class Program{
 static void Check(bool yes,string label){if(!yes)throw new Exception(label);}
 static void Reset(){typeof(FilledSkillVfx).GetMethod("ResetAssets",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);foreach(var o in GameObject.All.ToArray())UnityEngine.Object.Destroy(o);GameObject.All.Clear();typeof(AdvancedSkillVfx).GetMethod("ResetCount",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);Time.frameCount++;}
 static int Active()=>GameObject.All.Count(o=>o.activeInHierarchy&&o.GetComponent<FilledSkillVfx>()!=null);
 static void Advance(SkillChargeController c,float dt)=>typeof(SkillChargeController).GetMethod("Advance",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c,new object[]{dt});
 static void Main(string[] args){Resources.Root=args[0];var heroes=new[]{HeroClass.Vanguard,HeroClass.Arcanist,HeroClass.Summoner,HeroClass.Ranger};var slots=new[]{7,4,9,6};var names=new[]{"BladeSlices","ForkPulse","ContractSigil","ProtectionCage"};
 for(int i=0;i<4;i++)foreach(string exit in new[]{"cancel","release","epoch","disable"}){Reset();var h=new GameObject("Hero").AddComponent<PlayerController>();h.HeroClass=heroes[i];var game=new GameSession{Player=h,HasStarted=true};GameSession.Instance=game;var c=h.gameObject.AddComponent<SkillChargeController>();c.Initialize(h,game);Check(c.Begin(slots[i]),"actual Begin accepted");Check(Active()==1,"actual Begin owns typed effect");var fx=GameObject.All.First(o=>o.activeInHierarchy&&o.GetComponent<FilledSkillVfx>()!=null);var part=GameObject.All.First(o=>o.transform.parent==fx.transform);Check(part.GetComponent<MeshFilter>().sharedMesh.name.StartsWith("Skill identity / "+names[i]),"actual Begin routes correct typed resource");game.InputBlocked=true;Advance(c,1);Check(c.IsCharging&&h.Releases==0&&Active()==1,"pause preserves pending typed effect without release");game.InputBlocked=false;
 if(exit=="cancel")c.Cancel();else if(exit=="release")Advance(c,.31f);else if(exit=="epoch"){h.CombatEpoch++;Advance(c,.31f);}else h.gameObject.SetActive(false);
 Check(!c.IsCharging&&Active()==0&&CombatVisualLease.Active==0,"typed charge leaves no effect or lease after "+exit);Check(h.Releases==(exit=="release"?1:0),"actual release count");if(exit=="release")Advance(c,.31f);Check(h.Releases==(exit=="release"?1:0),"no duplicate release");}
 Reset();Console.WriteLine("PASS actual SkillChargeController Begin/pause/cancel/release/epoch/disable -> actual AdvancedSkillVfx -> loaded typed mesh:4 families, no leftover effect/lease, release once. Managed, not Unity.");}}
'''
charge_files={Path(f).name+'.cs':(root/('Assets/Scripts/'+f+'.cs')).read_text() for f in ['Core/FilledVfxRecipes','Core/FilledVfxPlacement','Core/CombatVisualBudget','Combat/CombatVisualLease','Combat/AnchoredImpactMesh','Combat/FilledSkillVfx','Combat/AuthoredActorMeshes','Combat/AuthoredSpellBases','Combat/AdvancedSkillVfx','Combat/SkillChargeController']}
run_probe('charge',charge_files,charge_support,charge_shell,[('SkillChargeController.cs','            ClearEffect();','            // mutation: leak pending typed child','typed charge leaves no effect or lease after cancel')])

def balanced_member(text,marker,body_only=False):
 a=text.index(marker);start=text.index('{',a);end=start+1;depth=1
 while depth:depth+=(text[end]=='{')-(text[end]=='}');end+=1
 return text[start+1:end-1] if body_only else text[a:end]
chain=balanced_member(source,'        private void ChainLightning(int maximumTargets)')
impulse=balanced_member(summon,'            else if (skill == 0)',True)
route_shell=ns['s'].replace('public bool IsDead;public int CombatEpoch;', '''public bool IsDead;public int CombatEpoch;public CombatDamage RollDirectDamage(float n)=>new CombatDamage{Amount=n};public void RegisterSkillHit(int i){}public void ApplySpellDodgeBoon(EnemyController e){}public void HitArea(Vector3 p,float r,CombatDamage d,float k,float s,int castId){}''')
route_shell=route_shell.replace('public bool HasStarted,ModeFinished,InputBlocked;', 'public bool HasStarted,ModeFinished,InputBlocked;public List<EnemyController> Enemies=new List<EnemyController>();')
route_shell=route_shell.replace('public static class CombatFx{','public static class CombatFx{public static void Slash(params object[] args){}')
route_shell=route_shell.replace('public static float Wall=', 'public static bool Chain(Vector3 a,Vector3 b)=>Area(a,b);public static bool Melee(Vector3 a,Vector3 b)=>Area(a,b);public static float Wall=')
route_shell=route_shell.replace('public Vector3 right=>localRotation.Rotate','public Vector3 forward=>localRotation.Rotate(Vector3.forward);public Vector3 right=>localRotation.Rotate')
route_shell=route_shell.replace('public static Vector3 Lerp(', 'public static float Angle(Vector3 a,Vector3 b)=>(float)(Math.Acos(Math.Max(-1,Math.Min(1,(a.x*b.x+a.y*b.y+a.z*b.z)/(a.magnitude*b.magnitude))))*180/Math.PI);public static Vector3 Lerp(')
route_body=r'''
using System;using System.Linq;using System.Collections.Generic;using Emberfall;using UnityEngine;
namespace Emberfall{
 public enum HeroClass{Vanguard,Arcanist,Ranger,Summoner}public enum CombatVisualPriority{ActionBody,RealContact}
 public struct CombatDamage{public float Amount;public bool IsCritical;public static CombatDamage operator*(CombatDamage d,float n){d.Amount*=n;return d;}}
 public class EnemyController:MonoBehaviour{public bool IsDead,Invulnerable;public float Health=100;public int Attempts;public void TakeDamage(float n,Vector3 f,float knockback=0,float stun=0,bool critical=false,int practiceCastId=0){Attempts++;if(!Invulnerable&&!IsDead){Health-=n;if(Health<=0)IsDead=true;}}}
 public static class SkillDamageBudgets{public static float AdvancedImpact(HeroClass h,int s,int r,int step)=>1;public static float AdvancedAuxiliary(HeroClass h,int s,int r)=>1;}
 public static class SummonerDamageRules{public const float ImpulseCoefficient=1;}
 public static class DestructibleProp{public static void StrikeCone(params object[] a){}}
 public static class FilledSkillVfx{public static int Release,Contact;public static bool IdentityContact(PlayerController h,Vector3 at,Vector3 f,float r,Color c,int identity,CombatVisualPriority priority=CombatVisualPriority.ActionBody){if(priority==CombatVisualPriority.RealContact)Contact++;else Release++;return true;}}
 public static class AdvancedSkillVfx{public static int Beams;public static void Beam(params object[] a){Beams++;}}
 public static class ElementalCombatVfx{public static int Lines;public static void Lightning(params object[] a){Lines++;}}
 class RouteProbe{
  private PlayerController owner;private GameSession session;private Vector3 target=new Vector3(0,0,2),forward=Vector3.forward;private CombatDamage damage=new CombatDamage{Amount=10};private int rank=1,skill=4,step=0,castId=5;private float range=1;private HeroClass heroClass=HeroClass.Arcanist;
  private float Strength;public RouteProbe(PlayerController p,GameSession g,float strength){owner=p;session=g;Strength=strength;damage=new CombatDamage{Amount=strength};}
  public void RunChain(){ChainLightning(2);}
CHAIN
  public void RunImpulse(){var player=owner;var game=session;int propCast=9;float damage=Strength;Color color=new Color(1,1,1);
IMPULSE
  }
 }
}
class Program{
 static void Check(bool yes,string label){if(!yes)throw new Exception(label);}
 static void Main(){foreach(string route in new[]{"chain","impulse"})foreach(string scenario in new[]{"damage","immune","zero","corpse","kill","empty"}){
  FilledSkillVfx.Contact=FilledSkillVfx.Release=AdvancedSkillVfx.Beams=ElementalCombatVfx.Lines=0;var h=new GameObject("Hero").AddComponent<PlayerController>();var game=new GameSession{Player=h,HasStarted=true};var e=new GameObject("Enemy").AddComponent<EnemyController>();e.transform.position=new Vector3(0,0,2);e.Invulnerable=scenario=="immune";e.IsDead=scenario=="corpse";if(scenario=="kill")e.Health=5;if(scenario!="empty")game.Enemies.Add(e);var probe=new RouteProbe(h,game,scenario=="zero"?0:10);if(route=="chain")probe.RunChain();else probe.RunImpulse();bool damage=scenario=="damage"||scenario=="kill";
  Check(FilledSkillVfx.Contact==(damage?1:0),route+" only actual health decrease routes contact");Check(FilledSkillVfx.Release==(route=="impulse"?1:0),"release is not mislabelled as confirmed contact");Check(e.Attempts==(scenario=="empty"||scenario=="corpse"?0:1),"existing live-target damage dispatch");if(route=="chain")Check(ElementalCombatVfx.Lines==1,"dynamic chain line retained even without confirmed contact");if(scenario=="kill")Check(e.IsDead&&FilledSkillVfx.Contact==1,"killing hit keeps real contact before corpse exclusion");if(scenario=="immune"||scenario=="zero")Check(e.Health==100,"no-damage target unchanged");
 }Console.WriteLine("PASS actual ChainLightning and actual Summoner slot0 production body:damage/immune/zero/corpse/kill/empty; observed IdentityContact priorities/counts; dynamic chain line retained without fake hit. Observable rendering boundary, not Unity.");}}
'''.replace('CHAIN',chain).replace('IMPULSE',impulse)
run_probe('hit-routes',{},route_body,route_shell,[('Test.cs','if(nearest.Health<endpointHealth)','if(true)','chain only actual health decrease routes contact'),('Test.cs','if(enemy.Health<contactHealth)','if(true)','impulse only actual health decrease routes contact')])
