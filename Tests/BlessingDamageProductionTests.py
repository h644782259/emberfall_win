"""Live RunChoices/CombatDamage and companion Command/OnConfirmedHit paths.
Fixed-roll arithmetic and engine shells are not real fight DPS or Unity validation.
"""
from pathlib import Path
import importlib.util,tempfile,os,subprocess,sys
root=Path(__file__).resolve().parents[1];dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
def load(name,path):
 s=importlib.util.spec_from_file_location(name,path);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
cv=load('cv',root/'Tools/cloud-validation.py');comp=load('comp',root/'Tests/CompanionIntentProductionTests.py')
shell=comp.shell.replace('private float AttackMultiplier=>commandMultiplier;',comp.member('private float AttackMultiplier'))
shell=shell.replace('private bool pathClear;private bool CanReachTarget(Vector3 p)=>pathClear;','private bool pathClear=true;private bool CanReachTarget(Vector3 p)=>pathClear;').replace('public int DamageCalls;','public float DamageTotal;public int DamageCalls;').replace('{DamageCalls++;Health-=d;}','{DamageCalls++;Health-=d;DamageTotal+=d;}')
extra=r'''
 public static int VerifyBlessing(){int n=0;Action<bool,string> check=(ok,why)=>{n++;if(!ok)throw new Exception(why);};
  foreach(bool twin in new[]{false,true}){
   float[] normal=new float[2],command=new float[2],proc=new float[2];
   for(int boosted=0;boosted<2;boosted++){
    active.Clear();bonds.Clear();Time.time=0;var owner=new PlayerController{Twin=twin,RunAttackMultiplier=boosted==1?1.1f:1};var game=new GameSession{Player=owner};GameSession.Instance=game;
    var enemy=new EnemyController();enemy.transform.position=new Vector3(1,0,0);game.Enemies.Add(enemy);
    var wolf=Summon(owner,game,Kind.Wolf,1,Vector3.zero,10,true);var spirit=Summon(owner,game,Kind.Spirit,1,Vector3.zero,10,true);
    normal[boosted]=wolf.damage*wolf.AttackMultiplier;
    wolf.Command(enemy,false,enemy.transform.position,false);command[boosted]=enemy.DamageTotal;
    enemy.DamageTotal=0;SetFreeFocus(owner,enemy);Time.time=4;wolf.OnConfirmedHit(enemy);spirit.OnConfirmedHit(enemy);proc[boosted]=enemy.DamageTotal;
    float value=wolf.damage;for(int i=0;i<1000;i++)SetFreeFocus(owner,enemy);check(wolf.damage==value,"free focus never bakes run multiplier into stored partner damage");
   }
   check(Math.Abs(normal[1]/normal[0]-1.1f)<.0001f,"partner ordinary attack includes Fervor exactly once");
   check(command[0]>0&&Math.Abs(command[1]/command[0]-1.1f)<.0001f,"actual wolf command includes Fervor exactly once");
   check(twin?proc[0]>0&&Math.Abs(proc[1]/proc[0]-1.1f)<.0001f:proc[0]==0&&proc[1]==0,"actual cooperation includes Fervor exactly once and still needs equipment");
  }
  return n;
 }
'''
shell=shell.replace('  public static int Verify(){',extra+'  public static int Verify(){').replace('Emberfall.SummonedCompanion.Verify()','Emberfall.SummonedCompanion.VerifyBlessing()')
def member(file,signature):
 source=(root/'Assets/Scripts'/file).read_text();start=source.index(signature);end=source.index('{',start)+1;depth=1
 while depth:depth+=(source[end]=='{')-(source[end]=='}');end+=1
 return source[start:end]
player_methods='\n'.join(member('Combat/PlayerController.cs',x) for x in ['private RunChoices ActiveRunBonuses','internal float RunAttackMultiplier','private float CombatAttack','internal CombatDamage RollDirectDamage(','private CombatDamage Damage('])
player_program=r'''using System;using UnityEngine;
namespace UnityEngine{public static class Random{public static float value=.5f;}}
namespace Emberfall{
public class GameSession{public bool InDungeon=true;public RunChoices RunChoices;}
public class PlayerController{
GameSession session;StatBlock stats=new StatBlock{Damage=100,CritChance=.1f};float castDamageRoll=.05f;
METHODS
public static string Verify(){int n=0;Action<bool,string> check=(ok,why)=>{n++;if(!ok)throw new Exception(why);};
var hero=new PlayerController{session=new GameSession{RunChoices=BlessingDamageBudgetTests.Reach(HeroClass.Summoner,RunBlessing.BattleFervor,RunBlessing.DeadlyEdge)}};
check(Math.Abs(hero.Damage(2).Amount-473)<.001f,"actual player Damage applies 1.10 attack and 2.15 crit once");
check(Math.Abs(hero.RollDirectDamage(hero.CombatAttack*2).Amount-473)<.001f,"actual skill direct roll never reapplies attack multiplier");
hero.castDamageRoll=.9f;check(Math.Abs(hero.Damage(2).Amount-220)<.001f&&!hero.Damage(2).IsCritical,"noncrit still has exactly one attack multiplier");
hero.session.InDungeon=false;hero.castDamageRoll=.05f;check(Math.Abs(hero.Damage(2).Amount-330)<.001f&&hero.RunAttackMultiplier==1,"leaving dungeon gates all owner and companion run multipliers");
return "PASS: "+n+" actual Player Damage/RollDirectDamage run-scope checks";
}}}
class Program{static void Main(){Console.WriteLine(Emberfall.PlayerController.Verify());}}
'''.replace('METHODS',player_methods).replace('Random.value','UnityEngine.Random.value')
# Structural audit guards at the distinct snapshot/tick boundary, not a claim that
# this script simulates the live scheduled DOT engine (covered by its own tests).
status=(root/'Assets/Scripts/Combat/EnemyStatusEffects.cs').read_text()
assert 'enemy.TakeDamage(burnDamage*StatusTickRates.Burn,Vector3.zero,impact:false)' in status
assert 'enemy.TakeDamage(poisonDamage*poisonStacks,Vector3.zero,impact:false)' in status
assert 'RollDirectDamage' not in status and 'RunAttackMultiplier' not in status,'DOT ticks must not reroll crit or reapply attack'
player=(root/'Assets/Scripts/Combat/PlayerController.cs').read_text()
assert '(slot == 2 || slot == 4 || slot == 9 ? stats.Damage : CombatAttack)' in player,'partner contracts must keep unboosted base snapshot'
assert 'damage = stats.Damage * CompanionRules.RankPower(rank)' in (root/'Assets/Scripts/Combat/SummonedCompanion.cs').read_text(),'partner refresh cannot bake in run multiplier'
with tempfile.TemporaryDirectory(prefix='blessing-damage-') as folder:
 out=Path(folder);config=out/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>');env=dict(os.environ,DOTNET_CLI_HOME=str(out/'cli'),DOTNET_NOLOGO='1')
 def run(name,sources,program,negative=None):
  p=cv.write_project(out/name,sources,program=program);subprocess.run([dotnet,'build',str(p),'--configfile',str(config),'-v:q'],env=env,check=True)
  command=[dotnet,str(p.parent/'bin/Debug/net8.0/Validation.dll')]
  if negative is None:subprocess.run(command,env=env,check=True)
  else:
   r=subprocess.run(command,env=env,capture_output=True,text=True);error=r.stdout+r.stderr;expected='Unhandled exception. System.Exception: '+negative
   if not r.returncode or not error.splitlines() or error.splitlines()[0]!=expected:raise RuntimeError('wrong mutation result: '+error)
   print('PASS: compiled negative control rejected at '+negative)
 core=[root/'Assets/Scripts'/f for f in ['Core/GameTypes.cs','Core/CombatBalance.cs','Core/SkillRuntime.cs','Core/CampRouteCards.cs','Core/RunChoices.cs','Core/RunChoices.Rooms.cs','Combat/CombatDamage.cs']]+[root/'Tests/SkillRuntimeTests.cs',root/'Tests/BlessingDamageBudgetTests.cs',root/'Tests/RunChoicesTests.cs',root/'Tests/RoomBlessingRouteTests.cs']
 run('budget',core,'using System;class Program{static void Main(){Console.WriteLine(BlessingDamageBudgetTests.Run());Console.WriteLine(RunChoicesTests.Run());Console.WriteLine(RoomBlessingRouteTests.Run());}}')
 run('player',core,player_program)
 pet=[root/'Assets/Scripts/Combat/CompanionDirective.cs',root/'Assets/Scripts/Combat/CompanionRules.cs',root/'Assets/Scripts/UI/CompanionCommandPresentation.cs']
 run('partner',pet,shell)
 run('partner-negative',pet,shell.replace(' * Owner.RunAttackMultiplier',''),'partner ordinary attack includes Fervor exactly once')
