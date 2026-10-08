#!/usr/bin/env python3
"""Actual companion order/contract/hit paths; Unity effects and physics are shells."""
from pathlib import Path
import importlib.util,os,subprocess,sys,tempfile
root=Path(__file__).resolve().parents[1]
def member(signature):
 s=(root/'Assets/Scripts/Combat/SummonedCompanion.cs').read_text();start=s.index(signature);end=s.index('{',start)+1;depth=1
 while depth:depth+=(s[end]=='{')-(s[end]=='}');end+=1
 return s[start:end]
methods='\n'.join(member(x) for x in ['private sealed class BondState','private static BondState State(', 'public static SummonedCompanion[] Snapshot(', 'public static void DescribeRoster(', 'public static bool SetFreeFocus(', 'public static bool FreeRecall(', 'public static EnemyController ExplicitFocus(', 'public static SummonedCompanion CastContract(', 'private bool ValidTarget(', 'private void Command(', 'private EnemyController AcquireTarget()', 'private void AdvanceCommand(', 'private bool LegalPackTarget(', 'private bool PackCanReach(', 'private struct PackPathProbe', 'private EnemyController AcquirePackTarget()', 'public static bool IsFreeRecalled(', 'public static bool FreeAttack(', 'public void OnConfirmedHit(', 'public bool EmpoweredAttackActive', 'public void RecordEmpoweredHit(', 'public static bool EmpoweredHitFeedback(', 'private float AttackPreparation('])
if '--legacy-clear' in sys.argv:methods=methods.replace('BondState state = State(owner);','BondState state = State(owner); state.Directive.Clear();')
shell=r'''
using System;using System.Collections.Generic;using UnityEngine;
namespace UnityEngine {
 public static class Time {public static float time;}
 public struct Vector3 {public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}public static Vector3 ClampMagnitude(Vector3 v,float max)=>v.magnitude>max?v.normalized*max:v;public static Vector3 one=>new Vector3(1,1,1);public static Vector3 zero=>new Vector3();public static Vector3 up=>new Vector3(0,1,0);public float sqrMagnitude=>x*x+y*y+z*z;public float magnitude=>(float)Math.Sqrt(sqrMagnitude);public Vector3 normalized=>magnitude>0?this*(1/magnitude):zero;public static Vector3 operator+(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator-(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);public static Vector3 operator*(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);}
 public struct Color {public Color(float r,float g,float b){}}
 public class GameObject {public bool activeInHierarchy=true;}
 public class Transform {public Vector3 position,localScale,forward=new Vector3(0,0,1), right=new Vector3(1,0,0);}
 public static class Mathf {public static float Clamp01(float v)=>Math.Max(0,Math.Min(1,v));public static float Lerp(float a,float b,float t)=>a+(b-a)*t;public static float Max(float a,float b)=>Math.Max(a,b);public static float Min(float a,float b)=>Math.Min(a,b);}
}
namespace Emberfall {
 public enum SummonerRoute{Bonded,Pack} public enum HeroClass{Summoner}public enum EquipmentMechanic{TwinSummonResonance}
 public static class GameBalance {public static float SkillRangeMultiplier(int rank)=>1;public static Color ClassColor(HeroClass h)=>new Color();}
 public class PlayerController {public GameObject gameObject=new GameObject();public float RunAttackMultiplier=1;public bool IsDead,Twin;public HeroClass HeroClass;public int CombatEpoch;public EnemyController FocusTarget;public object Targeting;public Vector3 AimPoint;public EnemyController AimTarget;public bool Ready=true;public Func<int,Vector3,bool> ConfirmCast;public Func<int,bool> ExecuteCast;public T GetComponent<T>() where T:class=>Targeting as T;public bool CanBeginSkillTargeting(int skill)=>Ready;public bool ConfirmTargetedSkill(int skill,Vector3 point)=>ConfirmCast(skill,point);public bool ExecuteChargedSkill(int skill)=>ExecuteCast(skill);public void CancelCombatPose(){}public Transform transform=new Transform();public bool HasMechanic(EquipmentMechanic m)=>Twin;}
 public class EnemyController {public float Health=100;public bool IsDead,IsAggro=true;public GameObject gameObject=new GameObject();public Transform transform=new Transform();public int DamageCalls;public void TakeDamage(float d,Vector3 v,float a=0,float b=0,bool impact=true,int practiceCastId=0){DamageCalls++;Health-=d;}}
 public class GameSession {public static GameSession Instance;public class ProfileStub{public SummonerRoute summonerRoute;public int[] skillRanks={1,1,1,1,1,1,1,1,1,1};}public class ProgressionStub{public ProfileStub Profile=new ProfileStub();}public ProgressionStub Progression=new ProgressionStub();public float ArenaRadius=30;public PlayerController Player;public bool HasStarted=true,InputBlocked,CombatEnded,InDungeon=true;public List<EnemyController> Enemies=new List<EnemyController>();public int Procs;public void RecordCombatAction(string s){if(s=="双契共鸣")Procs++;}public void RecordClassTutorial(HeroClass h){}public void SpawnMechanismText(Vector3 p,string s,Color c){}}
 public static class CombatFx {public static Vector3 Flat(Vector3 v)=>new Vector3(v.x,0,v.z);}
 public static class CombatSight {public static Vector3 GroundPoint(Vector3 a,Vector3 b)=>b;}
 public static class WorldTraversal {public static int Revision;public static bool IsWalkable(Vector3 p,float r)=>true;public static bool CanReach(Vector3 a,Vector3 b,float r)=>Blocked==null||Blocked.transform.position.x!=b.x;public static EnemyController Blocked;public static bool HasGroundPath(Vector3 a,Vector3 b,float n)=>Blocked==null||Blocked.transform.position.x!=b.x;public static Vector3 NearestWalkable(Vector3 v,float n)=>v;public static Vector3 Move(Vector3 a,Vector3 b,float n)=>a+b;}
 public static class AdvancedSkillVfx {public static void Beam(PlayerController p,Vector3 a,Vector3 b,Color c,float t,float w){}}
 public sealed class SummonedCompanion {
  public enum Kind{Wolf,Spirit,Treant}
  private static List<SummonedCompanion> active=new List<SummonedCompanion>();private static Dictionary<PlayerController,BondState> bonds=new Dictionary<PlayerController,BondState>();private static List<PlayerController> staleOwners=new List<PlayerController>();
  public PlayerController Owner;public Kind Form;public bool IsStarter,IsPermanent=true,IsAlive=true;public float RemainingLifetime=100;
  private GameSession session;private Transform transform=new Transform();private int rank=1;private readonly Dictionary<EnemyController,PackPathProbe> packPaths=new Dictionary<EnemyController,PackPathProbe>();private float packTargetHoldUntil;private float cooldown=2,attackPose,recallTime,commandTime,commandMultiplier=1,damage=10;private bool commandEmpowered,hasCommandPoint,commandHadTarget,recallVisualPending;private EnemyController commandedTarget,target;private Vector3 commandedPoint;
  private float NavigationRadius=>.3f;private float AttackMultiplier=>commandMultiplier;
  private bool pathClear;private bool CanReachTarget(Vector3 p)=>pathClear;
  private void RefreshContractPower(int r){rank=r;}
  private static int Count(PlayerController p)=>active.Count;
  private static SummonedCompanion SummonStarter(PlayerController p,GameSession g,float strength)=>Summon(p,g,Kind.Wolf,1,p.transform.position,strength,true);
  private static SummonedCompanion Summon(PlayerController p,GameSession g,Kind k,int r,Vector3 at,float strength,bool permanent=false){var pet=new SummonedCompanion{Owner=p,session=g,Form=k,rank=r,IsStarter=k==Kind.Wolf,IsPermanent=permanent};active.Add(pet);return pet;}
  METHODS
  public static int Verify(){int n=0;Action<bool,string> check=(ok,why)=>{n++;if(!ok)throw new Exception(why);};
   active.Clear();bonds.Clear();Time.time=0;var owner=new PlayerController{Twin=true};var game=new GameSession{Player=owner};GameSession.Instance=game;
   var a=new EnemyController();a.transform.position=new Vector3(5,0,0);var b=new EnemyController();b.transform.position=new Vector3(7,0,0);game.Enemies.Add(a);game.Enemies.Add(b);
   var wolf=Summon(owner,game,Kind.Wolf,1,Vector3.zero,10,true);var spirit=Summon(owner,game,Kind.Spirit,1,Vector3.zero,10,true);
   State(owner).Commands.Grant(Time.time);
   check(SetFreeFocus(owner,a),"free focus accepted");
   float wolfLife=wolf.RemainingLifetime,spiritLife=spirit.RemainingLifetime;
   for(int i=0;i<1000;i++)SetFreeFocus(owner,a);
   check(a.DamageCalls==0&&wolf.cooldown==2&&spirit.cooldown==2&&wolf.attackPose==0&&spirit.attackPose==0&&wolf.target==null&&spirit.target==null&&wolf.RemainingLifetime==wolfLife&&spirit.RemainingLifetime==spiritLife,"repeated free focus neither hits nor resets attack recovery");
   check(wolf.AcquireTarget()==a&&spirit.AcquireTarget()==a&&State(owner).Commands.Remaining(Time.time)==16,"both follow explicit team A without consuming dodge-command opportunity");
   CastContract(owner,game,Kind.Spirit,1,b.transform.position,10,false,b,false);
   check(ExplicitFocus(owner)==a,"ordinary contract must not clear shared directive");
   check(spirit.commandedTarget==a&&spirit.AcquireTarget()==a&&wolf.AcquireTarget()==a,"ordinary contract defaults to team A despite incidental B");
   CastContract(owner,game,Kind.Spirit,1,b.transform.position,10,false,b,true);
   check(ExplicitFocus(owner)==a&&spirit.AcquireTarget()==b&&wolf.AcquireTarget()==a,"captured B temporarily overrides only its partner");
   float recovery=spirit.cooldown;SetFreeFocus(owner,a);
   check(spirit.AcquireTarget()==b&&spirit.cooldown==recovery,"repeated team orders cannot overwrite temporary intent or recovery");
   var c=new EnemyController();c.transform.position=new Vector3(6,0,0);game.Enemies.Add(c);SetFreeFocus(owner,c);
   check(spirit.AcquireTarget()==b&&wolf.AcquireTarget()==c,"new team C does not erase temporary B");
   spirit.AdvanceCommand(100);check(spirit.AcquireTarget()==c&&spirit.commandedTarget==null&&!spirit.hasCommandPoint,"expiry resolves latest team C and clears override");SetFreeFocus(owner,a);
   CastContract(owner,game,Kind.Spirit,1,b.transform.position,10,false,b,true);b.IsDead=true;spirit.AdvanceCommand(0);
   check(spirit.AcquireTarget()==a&&!spirit.hasCommandPoint,"dead enemy override immediately returns to team");b.IsDead=false;
   CastContract(owner,game,Kind.Spirit,1,b.transform.position,10,false,b,true);b.transform.position=new Vector3(15,0,0);spirit.AdvanceCommand(0);
   check(spirit.AcquireTarget()==a&&!spirit.hasCommandPoint,"out-of-range enemy override immediately returns to team");b.transform.position=new Vector3(7,0,0);
   CastContract(owner,game,Kind.Spirit,1,b.transform.position,10,false,null,true);spirit.AdvanceCommand(.1f);
   check(spirit.AcquireTarget()==null&&spirit.hasCommandPoint,"explicit empty-ground command persists until expiry");spirit.AdvanceCommand(100);
   wolf.OnConfirmedHit(a);check(game.Procs==0,"one confirmed species cannot cooperate alone");
   Time.time=1.5f;spirit.OnConfirmedHit(a);check(game.Procs==1,"explicit free focus qualifies only after different real hits within1.5s");
   Time.time=2;wolf.OnConfirmedHit(a);spirit.OnConfirmedHit(a);check(game.Procs==1,"three-second proc cooldown remains");
   Time.time=5;wolf.OnConfirmedHit(a);Time.time=6.6f;spirit.OnConfirmedHit(a);check(game.Procs==1,"expired1.5s pairing rejected");
   Time.time=7;wolf.OnConfirmedHit(a);check(game.Procs==2,"new distinct real pair after cooldown triggers");
   owner.Twin=false;Time.time=11;wolf.OnConfirmedHit(a);spirit.OnConfirmedHit(a);check(game.Procs==2,"equipment remains mandatory");owner.Twin=true;
   Time.time=15;wolf.OnConfirmedHit(a);wolf.OnConfirmedHit(a);check(game.Procs==2,"same species cannot pair with itself");
   SetFreeFocus(owner,b);spirit.OnConfirmedHit(b);check(game.Procs==2,"different target does not complete A's marks");
   b.IsDead=true;check(ExplicitFocus(owner)==null,"dead team target cleared");b.IsDead=false;SetFreeFocus(owner,b);b.transform.position=new Vector3(15,0,0);check(ExplicitFocus(owner)==null,"out of range team target cleared");
   b.transform.position=new Vector3(7,0,0);SetFreeFocus(owner,b);game.Enemies.Remove(b);check(ExplicitFocus(owner)==null&&!SetFreeFocus(owner,b),"removed encounter target cannot persist or be ordered");game.Enemies.Add(b);
   SetFreeFocus(owner,a);owner.CombatEpoch++;check(ExplicitFocus(owner)==null,"new room epoch clears team and cooperation state");
   SetFreeFocus(owner,a);CastContract(owner,game,Kind.Spirit,1,b.transform.position,10,false,b,true);float cd=spirit.cooldown;
   check(FreeRecall(owner)&&spirit.recallVisualPending&&wolf.recallVisualPending&&spirit.AcquireTarget()==null&&wolf.AcquireTarget()==null,"free recall cancels local navigation without casting");
   int damageCalls=a.DamageCalls+b.DamageCalls+c.DamageCalls;float remaining=spirit.RemainingLifetime,commandRemaining=spirit.commandTime;
   for(int i=0;i<1000;i++)FreeRecall(owner);
   check(spirit.cooldown==cd&&spirit.RemainingLifetime==remaining&&spirit.commandTime==commandRemaining&&damageCalls==a.DamageCalls+b.DamageCalls+c.DamageCalls,"1000 free recalls do not deal damage or reset cooldown/lifetime/command timer");
   SetFreeFocus(owner,a);check(spirit.AcquireTarget()==a&&!spirit.recallVisualPending,"new explicit focus exits recall");
   FreeRecall(owner);float resumeCommand=spirit.commandTime,resumeMultiplier=spirit.commandMultiplier,resumeRecovery=spirit.cooldown,resumeLife=spirit.RemainingLifetime,resumeOpportunity=State(owner).Commands.Remaining(Time.time);
   for(int i=0;i<1000;i++)check(FreeAttack(owner),"free attack accepted");
   check(!IsFreeRecalled(owner)&&spirit.commandTime==resumeCommand&&spirit.commandMultiplier==resumeMultiplier&&spirit.cooldown==resumeRecovery&&spirit.RemainingLifetime==resumeLife&&State(owner).Commands.Remaining(Time.time)==resumeOpportunity,"free attack never refreshes timers buffs recovery lifetime or opportunity");
   owner.FocusTarget=null;State(owner).Directive.Clear();wolf.commandTime=spirit.commandTime=0;wolf.target=a;c.transform.position=new Vector3(9,0,0);
   game.Progression.Profile.summonerRoute=SummonerRoute.Pack;
   var extra=Summon(owner,game,Kind.Wolf,1,Vector3.zero,10,false);extra.IsStarter=false;
   var extra2=Summon(owner,game,Kind.Wolf,1,Vector3.zero,10,false);extra2.IsStarter=false;
   extra.target=extra.AcquireTarget();extra2.target=extra2.AcquireTarget();
   check(extra.target==b&&extra2.target==c,"reinforcements split including permanent base wolf occupancy");
   // Actual AcquireTarget/AdvanceCommand/Directive methods: sustained basic focus
   // must not continually steal the two reinforcement wolves from their guards.
   owner.FocusTarget=a;
   for(int tick=0;tick<100;tick++) {
    Time.time+=.1f;wolf.target=wolf.AcquireTarget();extra.AdvanceCommand(.1f);extra2.AdvanceCommand(.1f);
    extra.target=extra.AcquireTarget();extra2.target=extra2.AcquireTarget();
    check(wolf.target==a&&extra.target==b&&extra2.target==c,"ten second basic focus leaves reinforcements on two guards");
   }
   check(spirit.AcquireTarget()==a,"spirit still follows basic focus on pack route");
   extra.IsPermanent=true;check(extra.AcquireTarget()==a,"permanent nonstarter wolf still follows basic focus");extra.IsPermanent=false;
   extra.IsStarter=true;check(extra.AcquireTarget()==a,"foundation marker excludes autonomous pack priority");extra.IsStarter=false;
   game.Progression.Profile.summonerRoute=SummonerRoute.Bonded;check(extra.AcquireTarget()==a,"bonded route still follows basic focus");game.Progression.Profile.summonerRoute=SummonerRoute.Pack;
   SetFreeFocus(owner,c);check(extra.AcquireTarget()==c&&extra2.AcquireTarget()==c,"explicit focus overrides basic focus and autonomous hunt");
   FreeRecall(owner);check(extra.AcquireTarget()==null&&extra2.AcquireTarget()==null,"recall overrides basic focus and autonomous hunt");
   FreeAttack(owner);wolf.target=wolf.AcquireTarget();extra.target=extra.AcquireTarget();extra2.target=extra2.AcquireTarget();
   check(wolf.target==a&&extra.target==b&&extra2.target==c,"focus recall attack resumes split with continuous basic focus");
   extra.Command(a,false,a.transform.position,true);check(extra.AcquireTarget()==a,"paid submitted wolf contract overrides autonomous hunt");
   extra.AdvanceCommand(4);extra.target=null;check(extra.AcquireTarget()==b,"paid wolf command expiry resumes autonomous hunt despite basic focus");
   extra.Command(b,false,b.transform.position,true);b.IsDead=true;extra.AdvanceCommand(0);extra.target=extra.AcquireTarget();
   check(extra.target!=b&&!extra.hasCommandPoint,"dead paid wolf target cannot hold hunt");b.IsDead=false;
   extra.target=b;extra.packTargetHoldUntil=Time.time+1;game.Enemies.Remove(b);check(extra.AcquireTarget()!=b,"removed pack target bypasses hold with active basic focus");game.Enemies.Add(b);
   b.gameObject.activeInHierarchy=false;check(extra.AcquireTarget()!=b,"inactive pack target bypasses hold with active basic focus");b.gameObject.activeInHierarchy=true;
   owner.FocusTarget=c;game.InDungeon=false;a.IsAggro=b.IsAggro=c.IsAggro=false;extra.target=null;
   check(extra.AcquireTarget()==null,"basic focus cannot bypass wilderness engagement rule for pack");game.InDungeon=true;a.IsAggro=b.IsAggro=c.IsAggro=true;
   owner.FocusTarget=null;extra.target=b;extra.packTargetHoldUntil=Time.time+1;
   extra2.target=null;b.transform.position=new Vector3(8,0,0);c.transform.position=new Vector3(4,0,0);Time.time+=.5f;
   check(extra.AcquireTarget()==b,"legal target held for one second despite nearer choice");
   b.IsDead=true;extra.target=extra.AcquireTarget();check(extra.target!=b,"death bypasses hold");b.IsDead=false;
   extra.target=b;extra.packTargetHoldUntil=Time.time+1;WorldTraversal.Blocked=b;check(extra.AcquireTarget()!=b,"unreachable bypasses hold");WorldTraversal.Blocked=null;
   b.transform.position=new Vector3(15,0,0);check(extra.AcquireTarget()!=b,"owner range bypasses hold");b.transform.position=new Vector3(7,0,0);
   game.InDungeon=false;b.IsAggro=false;c.IsAggro=false;extra.target=null;check(extra.AcquireTarget()==a,"wilderness never initiates unengaged enemies");game.InDungeon=true;b.IsAggro=c.IsAggro=true;
   SetFreeFocus(owner,c);check(extra.AcquireTarget()==c&&extra2.AcquireTarget()==c,"explicit focus takes precedence over splitting");
   extra.commandedTarget=b;extra.commandTime=2;extra.commandHadTarget=true;SetFreeFocus(owner,a);check(extra.AcquireTarget()==b,"paid override wins latest team order");extra.AdvanceCommand(3);check(extra.AcquireTarget()==a,"expired override restores latest team intent");
   State(owner).Directive.Clear();b.IsDead=c.IsDead=true;extra.target=null;extra2.target=null;check(extra.AcquireTarget()==a&&extra2.AcquireTarget()==a,"single boss converges despite occupancy");b.IsDead=c.IsDead=false;
   game.Progression.Profile.summonerRoute=SummonerRoute.Bonded;extra.target=null;check(extra.AcquireTarget()==wolf.AcquireTarget(),"other routes keep existing nearest behavior");
   wolf.target=a;wolf.pathClear=true;wolf.recallTime=0;wolf.attackPose=0;a.transform.position=new Vector3(.5f,0,0);wolf.cooldown=.11f;
   float originalCooldown=wolf.cooldown;int originalDamage=a.DamageCalls;
   check(wolf.AttackPreparation(1.4f)>.4f&&wolf.cooldown==originalCooldown&&a.DamageCalls==originalDamage,"preparation is a read-only final cooldown slice");
   wolf.pathClear=false;check(wolf.AttackPreparation(1.4f)==0,"blocked attack cannot advertise preparation");wolf.pathClear=true;
   wolf.cooldown=.3f;check(wolf.AttackPreparation(1.4f)==0,"outside existing cooldown tail no preparation");wolf.cooldown=.11f;
   a.IsDead=true;check(wolf.AttackPreparation(1.4f)==0,"dead target cancels preparation");a.IsDead=false;
   a.transform.position=new Vector3(8,0,0);check(wolf.AttackPreparation(1.4f)==0,"distant target cancels preparation");a.transform.position=new Vector3(.5f,0,0);
   wolf.recallTime=1;check(wolf.AttackPreparation(1.4f)==0,"recall cancels preparation");wolf.recallTime=0;
   int sequence,hits;float age;check(!EmpoweredHitFeedback(owner,out sequence,out hits,out age),"no forecast is a real hit");
   wolf.Command(a,true,a.transform.position,false);check(EmpoweredHitFeedback(owner,out sequence,out hits,out age)&&hits==1&&age==0,"actual empowered wolf HP loss produces feedback");int firstSequence=sequence;
   check(wolf.cooldown==CompanionRules.WolfCommandRecovery&&wolf.attackPose==1,"actual wolf release preserves recovery and contact pose");
   SetFreeFocus(owner,a);FreeRecall(owner);check(EmpoweredHitFeedback(owner,out sequence,out hits,out age)&&sequence==firstSequence,"pin and recall never mint hit feedback");
   wolf.RecordEmpoweredHit(a,0,true);wolf.RecordEmpoweredHit(a,1,false);check(EmpoweredHitFeedback(owner,out sequence,out hits,out age)&&sequence==firstSequence,"blocked damage and unempowered damage cannot emit");
   wolf.commandTime=0;wolf.RecordEmpoweredHit(a,1,true);check(EmpoweredHitFeedback(owner,out sequence,out hits,out age)&&sequence==firstSequence+1,"inflight empowered release snapshot survives command expiry");
   Time.time+=2.1f;check(!EmpoweredHitFeedback(owner,out sequence,out hits,out age),"feedback expires without renewing opportunity");
   wolf.RecordEmpoweredHit(a,1,true);owner.CombatEpoch++;check(!EmpoweredHitFeedback(owner,out sequence,out hits,out age),"epoch cancels old feedback");
   active.Clear();bonds.Clear();var live=new PlayerController();var actualGame=new GameSession{Player=live};GameSession.Instance=actualGame;var aim=new EnemyController();aim.transform.position=new Vector3(5,0,0);actualGame.Enemies.Add(aim);live.AimTarget=aim;var hud=new GameUI(actualGame);
   check(!hud.Observe().FocusEnabled&&!hud.Observe().RecallEnabled,"HUD disables both free orders without a live roster");
   Summon(live,actualGame,Kind.Wolf,1,Vector3.zero,10,true);check(hud.Observe().FocusEnabled&&hud.Observe().RecallEnabled&&!hud.Observe().FocusActive,"new roster advertises legal free orders without invented cooldown");
   for(int i=0;i<1000;i++)check(SetFreeFocus(live,aim)&&hud.Observe().FocusActive&&!hud.Observe().RecallActive,"repeated focus keeps actual live order active");
   aim.IsDead=true;check(!hud.Observe().FocusEnabled&&!hud.Observe().FocusActive,"dead aim and resolved team target clear focus HUD");aim.IsDead=false;
   check(FreeRecall(live)&&hud.Observe().RecallActive&&hud.Observe().RecallName=="出击","real recall advertises active protection intent and resume action");
   check(FreeAttack(live)&&!hud.Observe().RecallActive,"resume immediately removes actual recall marker");live.AimTarget=null;check(!hud.Observe().FocusEnabled&&hud.Observe().FocusReason=="无目标"&&hud.Observe().RecallEnabled,"missing aim only disables focus");live.AimTarget=aim;aim.transform.position=new Vector3(15,0,0);check(!hud.Observe().FocusEnabled&&hud.Observe().FocusReason=="目标太远","range reason matches real14 metre command gate");aim.transform.position=new Vector3(5,0,0);
   SetFreeFocus(live,aim);actualGame.Enemies.Remove(aim);check(!hud.Observe().FocusEnabled&&!hud.Observe().FocusActive,"removed encounter target cannot leave focus active");actualGame.Enemies.Add(aim);FreeRecall(live);active.Clear();check(!hud.Observe().RecallEnabled&&!hud.Observe().RecallActive,"lost roster removes active marker despite retained buffered order");
   Summon(live,actualGame,Kind.Wolf,1,Vector3.zero,10,true);live.CombatEpoch++;check(!hud.Observe().RecallActive&&!hud.Observe().FocusActive,"new world epoch clears old directive state");
   actualGame.InputBlocked=true;check(!hud.Observe().FocusEnabled&&!hud.Observe().RecallEnabled,"blocked combat never advertises actionable commands");actualGame.InputBlocked=false;live.IsDead=true;check(!hud.Observe().FocusEnabled&&!hud.Observe().RecallEnabled,"owner death disables actual command adapter");
   return n;
  }
 }
}
class Program{static void Main(){Console.WriteLine("PASS: "+Emberfall.SummonedCompanion.Verify()+" production companion intent assertions");}}
'''.replace('METHODS',methods)
ui_source=(root/'Assets/Scripts/UI/GameUI.CombatOpportunities.cs').read_text();start=ui_source.index('private CompanionCommandPresentation CompanionCommandState()');end=ui_source.index('{',start)+1;depth=1
while depth:depth+=(ui_source[end]=='{')-(ui_source[end]=='}');end+=1
adapter='namespace Emberfall {public sealed class GameUI{GameSession session;enum Panel{None,Menu}Panel panel;public GameUI(GameSession owner){session=owner;}public CompanionCommandPresentation Observe()=>CompanionCommandState();'+ui_source[start:end]+'}}'
shell=shell.replace('class Program{',adapter+'class Program{',1)
shell=shell.replace('public class ProgressionStub{','public class ProgressionStub{public float MechanicPowerMultiplier(EquipmentMechanic m)=>1;')
shell=shell.replace('public static class GameBalance {','public static class GameBalance {public const float ArcanistFinaleRadius=9.5f,ArcanistPulseRadius=8f;')

if __name__ == '__main__':
 spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
 with tempfile.TemporaryDirectory(prefix='companion-intent-') as folder:
  out=Path(folder);(out/'Replay.cs').write_text(shell);p=cv.write_project(out/'project',[out/'Replay.cs',root/'Assets/Scripts/Combat/CompanionDirective.cs',root/'Assets/Scripts/Combat/CompanionRules.cs',root/'Assets/Scripts/UI/CompanionCommandPresentation.cs'],program='')
  p.write_text(p.read_text().replace('<OutputType>Library</OutputType>','<OutputType>Exe</OutputType>'))
  config=out/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>');env=os.environ.copy();env.update(DOTNET_CLI_HOME=str(out/'cli'),DOTNET_NOLOGO='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
  dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet';subprocess.run([dotnet,'build',str(p),'--configfile',str(config),'-v:q'],env=env,check=True);subprocess.run([dotnet,str(p.parent/'bin/Debug/net8.0/Validation.dll')],env=env,check=True)

  for old,new,expected in [
   ('if (Form == Kind.Wolf && !IsPermanent && !IsStarter && session.Progression.Profile.summonerRoute == SummonerRoute.Pack)', 'if (Owner.FocusTarget != null) return Owner.FocusTarget;\n            if (Form == Kind.Wolf && !IsPermanent && !IsStarter && session.Progression.Profile.summonerRoute == SummonerRoute.Pack)', 'ten second basic focus leaves reinforcements on two guards'),
   ('EnemyController chosen = unclaimed ?? fallback;', 'EnemyController chosen = fallback;', 'reinforcements split including permanent base wolf occupancy'),
   ('Time.time < packTargetHoldUntil && LegalPackTarget(target)', 'false && LegalPackTarget(target)', 'legal target held for one second despite nearer choice'),
   ('State(owner).Directive.Clear();\n            foreach (var pet in Snapshot(owner)) { pet.target = null; pet.recallVisualPending = false; }', 'State(owner).Directive.Clear();\n            foreach (var pet in Snapshot(owner)) { pet.cooldown = 0; pet.target = null; pet.recallVisualPending = false; }', 'free attack never refreshes timers buffs recovery lifetime or opportunity'),
   ('||!CanReachTarget(target.transform.position))return 0;',')return 0;','blocked attack cannot advertise preparation'),
   ('||actualHealthLoss<=0','', 'blocked damage and unempowered damage cannot emit')]:
   assert old in shell
   (out/'Replay.cs').write_text(shell.replace(old,new))
   subprocess.run([dotnet,'build',str(p),'--configfile',str(config),'-v:q'],env=env,check=True)
   failed=subprocess.run([dotnet,str(p.parent/'bin/Debug/net8.0/Validation.dll')],env=env,capture_output=True,text=True)
   assert failed.returncode!=0 and expected in failed.stdout+failed.stderr,failed.stdout+failed.stderr
   print('PASS: compiled invalid companion behavior fails exact assertion: '+expected)
  update=member('private void Update()')
  assert update.index('cooldown -= dt;')<update.index('cooldown <= 0')<update.index('model.SetCompanionAttackPreparation(')<update.index('model.Animate(')
  assert 'cooldown = CompanionRules.AttackInterval((int)Form);' in update
  assert 'companionSource: this' in update and 'RecordEmpoweredHit(target,before-target.Health,empoweredHit)' in update
  print('PASS: actual Update keeps cooldown release order and uses read-only preparation after movement/contact')
