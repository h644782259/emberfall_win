"""Real ranger impact branch plus full EnemyStatusEffects, with rendering/session boundaries."""
from pathlib import Path
import sys,tempfile,subprocess,os
r=Path(__file__).resolve().parents[1]
def member(src,key):
 a=src.index(key);b=src.index('{',a)+1;depth=1
 while depth:depth+=(src[b]=='{')-(src[b]=='}');b+=1
 return src[a:b]
fixture=(r/'Tests/BurnFinaleProductionTests.cs').read_text().split('public static class BurnFinaleProductionTests')[0]
fixture=fixture.replace('public static class Mathf{','public static class Mathf{public static float Pow(float x,float y)=>(float)System.Math.Pow(x,y);')
fixture=fixture.replace('public class GameSession{','public class GameSession{public NeutralProgression Progression=new NeutralProgression();')
fixture+='namespace Emberfall{public class NeutralProgression{public float MechanicRangeMultiplier(EquipmentMechanic m)=>1;public float MechanicPowerMultiplier(EquipmentMechanic m)=>1;}}'
fixture=fixture.replace('public enum HeroClass{Arcanist}','public enum HeroClass{Arcanist,Ranger}public enum EquipmentMechanic{VenomSpread}')
fixture=fixture.replace('public static Vector3 zero=>','public static Vector3 up=>new Vector3(0,1,0);public float sqrMagnitude=>x*x+y*y+z*z;public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator *(Vector3 a,float v)=>new Vector3(a.x*v,a.y*v,a.z*v);public static Vector3 zero=>')
fixture=fixture.replace('public static bool Area','public static bool Direct(Vector3 a,Vector3 b)=>true;public static bool Area')
fixture=fixture.replace('public void RecordClassTutorial(HeroClass hero){}','public List<string> Events=new List<string>();public void RecordCombatAction(string s){Events.Add(s);}public void SpawnMechanismText(Vector3 v,string s,Color c){}public void RecordClassTutorial(HeroClass hero){}')
fixture=fixture.replace('public int CombatEpoch=1,ProcHits,Boons;','public HeroClass HeroClass=HeroClass.Ranger;public bool ConcentratedVenom=true,Equipped=true;public float CombatAttack=100;public bool HasMechanic(EquipmentMechanic m)=>Equipped;public class Gate{public bool TryTrigger(float t)=>true;}Gate venomSpreadProc=new Gate();public int CombatEpoch=1,ProcHits,Boons;')
fixture=fixture.replace('public int CombatEpoch=1,ProcHits,Boons;', 'internal Vector3 EnemyBodyPoint(EnemyController e)=>e.transform.position;public int CombatEpoch=1,ProcHits,Boons;')
fixture+='namespace Emberfall{public static class VenomSkillVfx{public static int Consumed,Physical;public static void Contact(PlayerController p,Vector3 point,bool consumed){if(consumed)Consumed++;else Physical++;}}}'
player=(r/'Assets/Scripts/Combat/PlayerController.cs').read_text();branch=member(player,'if (HeroClass == HeroClass.Ranger && skill == 0)')
fixture+='\nnamespace Emberfall{public partial class PlayerController{public float Impact(EnemyController enemy,int castId,float baseDamage){int skill=0;var status=enemy.StatusEffects;'+branch+'return baseDamage;}}}'
program='''using System;using System.Reflection;using Emberfall;class Program{
static int n;static void C(bool b,string m){n++;if(!b)throw new Exception(m);}
static EnemyController Enemy(){var e=new EnemyController();e.StatusEffects=new EnemyStatusEffects{OwnerEnemy=e};typeof(EnemyStatusEffects).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(e.StatusEffects,null);GameSession.Instance.Enemies.Add(e);return e;}
static void Poison(EnemyController e,PlayerController p){for(int i=0;i<3;i++)e.StatusEffects.Poison(p,4,14);}
static void Main(){var p=new PlayerController();GameSession.Instance=new GameSession{Player=p};var e=Enemy();var near=Enemy();C(p.Impact(e,99,480)==480&&VenomSkillVfx.Consumed==0,"no stacks no successful consumption visual");Poison(e,p);C(p.Impact(e,1,480)==606,"B independent ordinary 126 poison bonus");C(e.StatusEffects.PoisonStacks==0&&near.StatusEffects.PoisonStacks==0,"B consumes own poison no spread");C(GameSession.Instance.Events.FindAll(x=>x=="收束毒爆").Count==1&&!GameSession.Instance.Events.Contains("毒层引爆"),"one actual B feedback");C(p.Impact(e,1,480)==480,"same contact no second explosion");C(VenomSkillVfx.Consumed==1,"only actual three-stack consumption emits success");Poison(e,p);C(p.Impact(e,1,480)==480&&e.StatusEffects.PoisonStacks==3,"same cast replenished stack cannot consume again");C(p.Impact(e,2,960)==1086,"critical direct does not multiply poison bonus");Poison(e,p);p.ConcentratedVenom=false;C(Math.Abs(p.Impact(e,3,480)-580.8f)<.01&&near.StatusEffects.PoisonStacks==1,"A retains 80 percent bonus and spread");Poison(e,p);p.CombatEpoch++;C(p.Impact(e,4,480)==480,"retired owner epoch cannot consume");Console.WriteLine("PASS "+n+" actual poison impact/status checks; managed recipients");}}
'''
with tempfile.TemporaryDirectory(prefix='venom-poison-') as t:
 p=Path(t);(p/'Fixture.cs').write_text(fixture);(p/'Program.cs').write_text(program)
 for f in ['Core/CombatImpactBatch','Core/ScheduledTickWindow','Core/BurnFinaleReceipts','Combat/EnemyStatusEffects','Combat/PlayerController.BurnFeedback','Combat/CombatDamage']:(p/(Path(f).name+'.cs')).write_text((r/'Assets/Scripts'/(f+'.cs')).read_text())
 (p/'Gate.cs').write_text('using System.Collections.Generic;namespace Emberfall{'+member((r/'Assets/Scripts/Combat/PlayerUpgradeRules.cs').read_text(),'public sealed class RecentCastGate')+'}')
 (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>');env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1')
 def run(expected=None):
  subprocess.run([sys.argv[1],'build',str(p/'Test.csproj'),'--configfile',str(p/'NuGet.Config'),'-v:q'],env=env,check=True);out=subprocess.run([sys.argv[1],str(p/'bin/Debug/net8.0/Test.dll')],env=env,capture_output=True,text=True);print(out.stdout+out.stderr)
  if expected:assert out.returncode and expected in out.stdout+out.stderr
  else:out.check_returncode()
 run();(p/'Fixture.cs').write_text(fixture.replace(' && !ConcentratedVenom',''));run('B independent ordinary 126 poison bonus');print('PASS old spread branch rejected by real impact/status test')
