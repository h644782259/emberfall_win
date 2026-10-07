#!/usr/bin/env python3
"""Real status/consume/basic-hit/aura lifecycle chain with managed engine recorders; no Unity render claim."""
import sys,os,tempfile,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
def member(s,signature):
 a=s.index(signature);i=s.index('{',a)+1;depth=1
 while depth:depth+=(s[i]=='{')-(s[i]=='}');i+=1
 return s[a:i]
fixture=(ROOT/'Tests/BurnFinaleProductionTests.cs').read_text().split('public static class BurnFinaleProductionTests')[0]
fixture=fixture.replace('public class MonoBehaviour{public Emberfall.EnemyController OwnerEnemy;public Transform transform=new Transform();public T GetComponent<T>()where T:class=>OwnerEnemy as T;public T GetComponentInChildren<T>()where T:class=>null;}', '''public class MonoBehaviour{public Emberfall.EnemyController OwnerEnemy;public Transform transform=new Transform();public GameObject gameObject;public MonoBehaviour(){gameObject=new GameObject(this);}public T GetComponent<T>()where T:class=>gameObject.GetComponent<T>()??OwnerEnemy as T;public T GetComponentInChildren<T>()where T:class=>null;}
 public class GameObject {public bool activeInHierarchy=true;public bool activeSelf=>activeInHierarchy;public Transform transform=new Transform();readonly Dictionary<Type,object> components=new Dictionary<Type,object>();public GameObject(object owner=null){if(owner!=null)components[owner.GetType()]=owner;}public void SetActive(bool b){activeInHierarchy=b;}public T GetComponent<T>()where T:class=>components.TryGetValue(typeof(T),out var o)?o as T:null;public T AddComponent<T>()where T:new(){var t=new T();components[typeof(T)]=t;return t;}}
 public enum ParticleSystemStopBehavior{StopEmittingAndClear}public class ParticleSystem:MonoBehaviour{public bool isEmitting,isPlaying;public int Clears;public void Play(){isEmitting=isPlaying=true;}public void Stop(bool children,ParticleSystemStopBehavior b){isEmitting=isPlaying=false;Clears++;}}''')
fixture=fixture.replace('public Vector3 position;','public Vector3 position,localPosition;')
fixture=fixture.replace('public static Vector3 zero=>','public Vector3 normalized=>this;public static Vector3 up=>new Vector3(0,1,0);public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator *(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);public static Vector3 zero=>')
fixture=fixture.replace('public struct Color{public Color(float r,float g,float b){}}','public struct Color{public float r,g,b;public Color(float r,float g,float b){this.r=r;this.g=g;this.b=b;}}')
fixture=fixture.replace('public enum HeroClass{Arcanist}','public enum HeroClass{Arcanist,Vanguard,Ranger,Summoner}public enum EnemyKind{Slime,Goblin}public enum EquipmentMechanic{ReturningBlade}')
fixture=fixture.replace('public bool IsDead,IsBoss,IsStunned;','public EnemyKind Kind;public bool IsDead,IsBoss,IsStunned;')
fixture=fixture.replace('public void RecordClassTutorial(HeroClass hero){}','public Progression Progression=new Progression();public string Text;public Color TextColor;public void SpawnMechanismText(Vector3 p,string text,Color c){Text=text;TextColor=c;}public void RecordCombatAction(string s){}public void RecordClassTutorial(HeroClass hero){}')
start=fixture.index('    public static class ElementalCombatVfx');end=fixture.index('\n    public static class CombatFx',start);fixture=fixture[:start]+fixture[end:]
fixture=fixture.replace('public static void Ring(Vector3 p,float r,Color c,float duration,float width){}','public static Color RingColor;public static void Ring(Vector3 p,float r,Color c,float duration,float width){RingColor=c;}')
fixture=fixture.replace('public static class PlayerUpgradeRules{public const float PoisonDetonationTicks=3;}','public static class PlayerUpgradeRules{public const float PoisonDetonationTicks=3,BasicFrostProcCooldown=1.2f,BasicFrostMarkDuration=3,BasicPoisonDuration=4,BasicPoisonCoefficient=.14f,FocusDuration=1.8f;}')
fixture+='''namespace Emberfall{
 public class Progression{public float MechanicRangeMultiplier(EquipmentMechanic m)=>1;public float MechanicPowerMultiplier(EquipmentMechanic m)=>1;public Profile Profile=new Profile();}public class Profile{public int[] masteryRanks=new int[10],skillRanks=new int[10];}public class Runtime{public float Energy;public void RestoreEnergy(float n){Energy+=n;}}public class Mastery{public float BasicHit()=>0;}
 public static class SkillDamageBudgets{public const float BasicEnergyOnHit=1;} public enum CombatVisualPriority{SustainedBackground}
 public class ElementalFieldVisual:MonoBehaviour{public static ElementalFieldVisual Spawn(Transform p,ElementalCombatVfx.Element e,float radius,bool attached,CombatVisualPriority priority)=>new ElementalFieldVisual();}
 public static class AdvancedSkillVfx{public static void Beam(PlayerController p,Vector3 a,Vector3 b,Color c,float d,float w){}}public static class GameBalance{public static Color ClassColor(HeroClass h)=>new Color();}
 public sealed partial class PlayerController{
  private float counterWindowDuration;public HeroClass HeroClass=HeroClass.Arcanist;public float CombatAttack=100;public Runtime skillRuntime=new Runtime();private Mastery masteryCore=new Mastery();private void SettleMasteryCombo(EnemyController enemy){}private EnemyController openingFrostTarget,focusedEnemy;private int openingFrostIdentity;private OpeningFrostCounter openingFrost=new OpeningFrostCounter();private CombatProcCooldown openingFrostProc=new CombatProcCooldown(),returningBladeProc=new CombatProcCooldown();private float classDodgeTime,focusTime,dodgeShockTime,counterTime;public bool ReturningCounterVariant,OwnReturningBlade;public EnemyController BounceTarget;private bool HasMechanic(EquipmentMechanic m)=>OwnReturningBlade;private EnemyController NearestOtherEnemy(Vector3 p,EnemyController e,float r)=>BounceTarget;private void HitArea(Vector3 p,float radius,float damage,float stun,float knock){} }
}'''
visual=(ROOT/'Assets/Scripts/Combat/ElementalCombatVfx.cs').read_text()
aura='using UnityEngine;namespace Emberfall { public static class ElementalCombatVfx {public enum Element {Fire,Poison,Lightning}'+''.join(member(visual,x) for x in ['public static void OnEnemy(', 'internal static void ClearFire(', 'internal static void ClearPoison('])+'internal static ParticleSystem Create(Transform t,string n,Element e,float rate,float radius)=>new ParticleSystem();}'+member(visual,'internal sealed class ElementalEnemyAura')+'}'
player=(ROOT/'Assets/Scripts/Combat/PlayerController.cs').read_text()
rules=(ROOT/'Assets/Scripts/Combat/PlayerUpgradeRules.cs').read_text()
with tempfile.TemporaryDirectory(prefix='status-feedback-') as d:
 p=Path(d)
 for path in ['Assets/Scripts/Combat/EnemyStatusEffects.cs','Assets/Scripts/Core/ScheduledTickWindow.cs','Assets/Scripts/Core/BurnFinaleReceipts.cs','Assets/Scripts/Combat/CombatDamage.cs']:(p/Path(path).name).write_text((ROOT/path).read_text())
 (p/'Fixture.cs').write_text(fixture);(p/'Aura.cs').write_text(aura)
 (p/'Player.cs').write_text('using UnityEngine;namespace Emberfall{public sealed partial class PlayerController{'+member(player,'internal void OnBasicAttackHitTarget(')+'}}')
 (p/'Rules.cs').write_text('using System;using System.Collections.Generic;namespace Emberfall{'+''.join(member(rules,x) for x in ['public sealed class RecentCastGate','public sealed class OpeningFrostCounter','public sealed class CombatProcCooldown'])+'}')
 # Variant ownership and target search are explicit inputs; actual basic-hit branch executes.
 tests=(ROOT/'Tests/StatusFeedbackProductionTests.cs').read_text()
 seam='  Console.WriteLine("PASS actual poison'
 assert seam in tests
 tests=tests.replace(seam,'''  foreach(bool counterVariant in new[]{false,true}){
   enemy=Create(out owner);owner.HeroClass=HeroClass.Vanguard;owner.OwnReturningBlade=true;owner.ReturningCounterVariant=counterVariant;owner.BounceTarget=new EnemyController();
   owner.OnBasicAttackHitTarget(Vector3.zero,enemy,true);
   Check(owner.BounceTarget.Hits.Count==(counterVariant?0:1),"counter variant excludes legacy returning-blade bounce while normal retains it");
   Check(Math.Abs(Field<float>(owner,"counterTime")-(counterVariant?0:1.8f))<.0001f,"counter variant excludes legacy heavy-counter grant");
   if(!counterVariant)Check(Math.Abs(owner.BounceTarget.Hits[0].Amount-110)<.001f,"normal returning-blade damage dispatch unchanged");
  }
'''+seam)
 (p/'Test.cs').write_text(tests)
 (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet')
 for name,mutation in [('current',None),('poison-clear-missing',('EnemyStatusEffects.cs','ElementalCombatVfx.ClearPoison(enemy);','')),('false-frost-feedback',('Player.cs','burning ? "灼触" : "霜触"','"霜触"')),('lost-counter-variant-exclusion',('Player.cs',' && !ReturningCounterVariant',''))]:
  original=None
  if mutation:
   path=p/mutation[0];original=path.read_text();assert mutation[1] in original;path.write_text(original.replace(mutation[1],mutation[2]))
  result=subprocess.run([dotnet,'run','--project',str(p/'Test.csproj')],env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'),capture_output=True,text=True)
  if name=='current':print(result.stdout+result.stderr);result.check_returncode()
  else:
   expected={'poison-clear-missing':'poison consumption retires sustained visuals immediately','false-frost-feedback':'burn proc reports actual fire type','lost-counter-variant-exclusion':'counter variant excludes legacy returning-blade bounce while normal retains it'}[name]
   assert result.returncode!=0 and 'System.Exception: '+expected in result.stdout+result.stderr,result.stdout+result.stderr
   print('PASS compiled negative control:',name);path.write_text(original)
