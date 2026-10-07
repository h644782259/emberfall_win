using System;
using System.Collections.Generic;
using Emberfall;
using UnityEngine;
public static class ShatterAvailabilityTests
{
 static int checks;static void Check(bool value,string why){checks++;if(!value)throw new Exception(why);}
 static PlayerController Fresh(float distance){var p=new PlayerController();p.session.Progression.Profile.skillRanks[1]=1;p.aimPoint=new Vector3(0,0,distance);p.session.Enemies.Add(new EnemyController(distance,true));p.session.Player=p;return p;}
 static string View(PlayerController player){var ui=new GameUI(player.session);ui.Read();return ui.Read();}
 public static string Run()
 {
  MobileControls.Active=true;CombatSight.Wall=float.PositiveInfinity;
  foreach(float distance in new[]{8f,10f,13f})
  {var p=Fresh(distance);Check(p.CanShatterNow()==(distance<13),"real release footprint distinguishes 8/10/13m; cooldown alone is not availability");Check(p.AimTarget==null&&p.Energy==100&&p.aimPoint.z==distance,"query never changes aim or spends energy");}
  var mixed=Fresh(10);mixed.session.Enemies.Insert(0,new EnemyController(2,false));
  Check(!mixed.CanShatterNow(),"near unmarked target owns mobile release; query must not select distant frost target");
  mixed.FocusTarget=mixed.session.Enemies[1];Check(!mixed.CanShatterNow(),"out-of-range frost focus cannot override actual near target selection");
  mixed.session.Enemies[1].transform.position=new Vector3(0,0,8);Check(mixed.CanShatterNow(),"in-range focus and actual release resolver agree");
  var wall=Fresh(8);CombatSight.Wall=5;Check(!wall.CanShatterNow(),"wall blocks actual area LOS even when frost exists");CombatSight.Wall=float.PositiveInfinity;
  var gate=Fresh(8);gate.Energy=0;Check(!gate.CanShatterNow(),"energy gate");gate.Energy=100;gate.skillRuntime.Cooldown=1;Check(!gate.CanShatterNow(),"cooldown gate");gate.skillRuntime.Cooldown=0;gate.jumping=true;Check(!gate.CanShatterNow(),"jump gate");gate.jumping=false;gate.charge.IsCharging=true;Check(!gate.CanShatterNow(),"charge never promises future shatter");gate.charge.IsCharging=false;gate.session.InputBlocked=true;Check(!gate.CanShatterNow(),"input blocked gate");gate.session.InputBlocked=false;gate.Specialization=ElementalistSpecialization.Burn;Check(!gate.CanShatterNow(),"burn route cannot shatter");gate.Specialization=ElementalistSpecialization.Shatter;gate.session.Progression.Profile.skillRanks[1]=0;Check(!gate.CanShatterNow(),"unlearned gate");
  MobileControls.Active=false;var desktop=Fresh(13);Check(!desktop.CanShatterNow(),"desktop release clamp rejects beyond footprint");desktop.session.Enemies[0].HitFootprintBonus=1;Check(desktop.CanShatterNow(),"actual enemy footprint bonus is included");
  desktop.session.Enemies[0].HitFootprintBonus=0;desktop.session.Progression.Profile.skillRanks[1]=2;Check(desktop.CanShatterNow(),"actual rank range expands release and impact");
  desktop.aimPoint=new Vector3(0,0,1);Check(!desktop.CanShatterNow(),"desktop chosen ground point controls impact not nearest frost enemy");
  var point=Fresh(8);point.targeting.skill=1;point.targeting.TargetPoint=new Vector3(0,0,1);
  Check(!point.CanShatterNow(),"desktop active target preview overrides stale aimPoint");point.targeting.TargetPoint=new Vector3(0,0,8);Check(point.CanShatterNow(),"same-skill preview uses actual confirmed release point");point.targeting.skill=2;Check(!point.CanShatterNow(),"another skill targeting does not advertise meteor action");
  var uiFar=Fresh(13);uiFar.AimTarget=uiFar.session.Enemies[0];Check(View(uiFar)=="目标霜痕","actual HUD must not call out unreachable marked target as available");
  var uiArea=Fresh(10);uiArea.aimPoint=new Vector3(0,0,8);var plain=new EnemyController(8,false);uiArea.session.Enemies.Insert(0,plain);uiArea.AimTarget=plain;
  Check(View(uiArea)=="当前落点可碎冰","actual HUD can show AoE frost opportunity when display target has no frost");
  uiArea.AimTarget=null;Check(View(uiArea)=="当前落点可碎冰","actual HUD retains ground opportunity without display target");
  uiArea.AimTarget=uiArea.session.Enemies[1];uiArea.charge.IsCharging=true;Check(View(uiArea)=="目标霜痕","charging HUD keeps status without future-action promise");
  MobileControls.Active=true;var typed=Fresh(8);typed.AimTarget=typed.session.Enemies[0];
  Check(typed.SkillOpportunity(1).Kind==CombatOpportunityKind.Shatter&&typed.SkillOpportunity(1).Remaining==4,"typed meteor uses actual frost expiry and production footprint");
  typed.session.Enemies[0].StatusEffects.FrostTime=.2f;Check(typed.SkillOpportunity(1).Remaining==.2f,"typed frost expiry is not a synthetic timer");
  typed.PinAllowed=false;Check(!typed.CanShatterNow()&&!typed.SkillOpportunity(1).Actionable,"pin range or obstruction cannot light a rejected skill");typed.PinAllowed=true;
  typed.Energy=0;Check(!typed.SkillOpportunity(1).Actionable,"typed no-energy skill remains unavailable despite frost");typed.Energy=100;
  typed.session.Enemies[0].StatusEffects.HasFrostMark=false;Check(!typed.SkillOpportunity(1).Actionable,"expired frost cannot light shatter");
  typed.Specialization=ElementalistSpecialization.Burn;typed.session.Enemies[0].StatusEffects.IsBurning=true;typed.session.Progression.Profile.skillRanks[9]=1;
  Check(typed.SkillOpportunity(1).Kind==CombatOpportunityKind.Reignite&&typed.SkillOpportunity(9).Kind==CombatOpportunityKind.BurnFinale,"burn refresh and terminal cash are distinct typed skill identities");
  typed.session.Enemies[0].StatusEffects.OwnedBurn=false;Check(typed.SkillOpportunity(1).Actionable&&!typed.SkillOpportunity(9).Actionable,"foreign burn may be refreshed but cannot promise own-burn cash");
  typed.skillRuntime.Cooldown=2;Check(!typed.SkillOpportunity(1).Actionable,"typed CD gate");typed.skillRuntime.Cooldown=0;typed.charge.IsCharging=true;Check(!typed.SkillOpportunity(1).Actionable,"typed locked charge gate");typed.charge.IsCharging=false;
  typed.HeroClass=HeroClass.Summoner;SummonedCompanion.Opportunity=7.5f;typed.session.Progression.Profile.skillRanks[2]=1;
  Check(typed.SkillOpportunity(2).Kind==CombatOpportunityKind.EmpoweredContract&&typed.SkillOpportunity(2).Remaining==7.5f&&!typed.SkillOpportunity(0).Actionable,"real contract window only marks contract slots");
  SummonedCompanion.Opportunity=0;Check(!typed.SkillOpportunity(2).Actionable,"expired command window clears action caption");
  typed.HeroClass=HeroClass.Vanguard;typed.CounterOpportunityRemaining=.8f;Check(typed.BasicOpportunity().Actionable,"counter marks basic attack");typed.attackCooldown=.1f;Check(!typed.BasicOpportunity().Actionable,"counter does not override basic cooldown");typed.attackCooldown=0;
  typed.session.InputBlocked=true;Check(!typed.BasicOpportunity().Actionable,"blocked UI suppresses opportunity");typed.session.InputBlocked=false;typed.session.Player=new PlayerController();Check(!typed.BasicOpportunity().Actionable,"retired owner cannot publish current opportunity");
  return "PASS: "+checks+" production shatter resolver/availability/geometry assertions (managed substitutes)";
 }
}
namespace Emberfall
{
 public static class MobileControls {public static bool Active;}
 public class SkillChargeController {public bool IsCharging,ConsumedThisFrame;}
 public class FakeRuntime {public float Cooldown;public float Remaining(int skill)=>Cooldown;}
 public class FakeProgression {public GameProfile Profile=new GameProfile();}
 public class GameSession {public PlayerController Player;public bool ChallengeRun,InDungeon;public int HealingCharges=3;public bool HasStarted=true,InputBlocked;public float ArenaRadius=25;public List<EnemyController> Enemies=new List<EnemyController>();public FakeProgression Progression=new FakeProgression();}
 public static class CompanionRules {public const float CommandOpportunityDuration=16f;}
 public class Status {public float FrostWindowDuration=4,BurnWindowDuration=3,PoisonWindowDuration=2;public float FrostTime=4,BurnTime=3;public float FrostRemaining=>HasFrostMark?FrostTime:0;public float BurnRemaining=>IsBurning?BurnTime:0;public bool OwnedBurn=true;public float OwnBurnRemaining(PlayerController source)=>OwnedBurn?BurnRemaining:0;public float OwnPoisonOpportunityRemaining(PlayerController source)=>PoisonStacks>=3?2:0;public bool HasFrostMark,IsBurning,IsMarked;public int PoisonStacks;}
 public class EnemyController {public bool IsDead,IsBoss;public float HitFootprintBonus;public Transform transform=new Transform();public GameObject gameObject=new GameObject();public Status StatusEffects=new Status();public EnemyController(float distance,bool frost){transform.position=new Vector3(0,0,distance);StatusEffects.HasFrostMark=frost;}}
 public static class CombatFx {public static Vector3 Flat(Vector3 p)=>new Vector3(p.x,0,p.z);}
 public static class CombatSight
 {public static float Wall=float.PositiveInfinity;public static bool Direct(Vector3 a,Vector3 b)=>a.z<=Wall&&b.z<=Wall;public static bool Area(Vector3 a,Vector3 b)=>Direct(a,b);public static Vector3 GroundPoint(Vector3 a,Vector3 b)=>new Vector3(b.x,b.y,Math.Min(b.z,Wall));}
 public static class WorldTraversal {public static bool CanLeap(Vector3 a,Vector3 b,float radius)=>true;}
 public static class SummonedCompanion {public static bool HasHealingTarget(PlayerController p)=>false;public static float Opportunity;public static bool EmpoweredHitFeedback(PlayerController p,out int sequence,out int hits,out float age){sequence=hits=0;age=0;return false;}public static EnemyController ExplicitFocus(PlayerController p)=>null;public static void DescribeRoster(PlayerController p,out int count,out float life){count=0;life=0;}public static float CommandOpportunityRemaining(PlayerController p)=>Opportunity;}
 public partial class GameUI {private GameSession session;private PlayerController opportunityOwner;private int opportunityEpoch;public GameUI(GameSession value){session=value;}public string Read()=>CurrentCombatOpportunity();}
 public sealed partial class PlayerController
 { private int burnFeedbackCast;
  internal bool BurnCashFeedback(out int targets,out float remaining,bool includeBlocked=false){targets=0;remaining=0;return false;}
  public bool PinAllowed=true;internal bool MobilePinnedActionAllowed(int skill,bool feedback)=>PinAllowed;
  public float attackCooldown;public class Recovery{public bool Blocked;}public Recovery skillBasicRecovery=new Recovery();
  public SkillTargetingController targeting=new SkillTargetingController();
  public float Health=100,MaxHealth=100;private EnemyController ReadMobilePinnedTarget()=>null;internal string ReadMobilePinnedActionReason(int skill)=>PinAllowed?"":"blocked";public EnemyController MobilePinnedTarget=>null;internal bool MobilePinAppliesToSkill(int skill)=>false;
  public int CombatEpoch=1;public float CounterOpportunityRemaining;public float CounterOpportunityDuration=2;public bool IsJumping=>jumping;
  public T GetComponent<T>() where T:class {return charge as T;}
  public float SkillCooldownRemaining(int skill)=>skillRuntime.Remaining(skill);
  public HeroClass HeroClass=HeroClass.Arcanist;public ElementalistSpecialization Specialization=ElementalistSpecialization.Shatter;
  public bool IsDead,jumping;public float Energy=100;public Transform transform=new Transform();public GameSession session=new GameSession();public SkillChargeController charge=new SkillChargeController();public FakeRuntime skillRuntime=new FakeRuntime();
  public EnemyController AimTarget,FocusTarget;public Vector3 aimPoint;private readonly List<MobileSkillPolicy.Candidate> mobileAimCandidates=new List<MobileSkillPolicy.Candidate>();
 }
}
namespace UnityEngine
{
 public struct Color {public Color(float r,float g,float b,float a=1){}}
 public class GameObject {public bool activeInHierarchy=true;}
 public class Transform {public Vector3 position;public Vector3 forward=new Vector3(0,0,1);}
 public static class Mathf {public static float Min(float a,float b)=>Math.Min(a,b);public static float Max(float a,float b)=>Math.Max(a,b);}
 public struct Vector3
 {
  public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}public float sqrMagnitude=>x*x+y*y+z*z;public float magnitude=>(float)Math.Sqrt(sqrMagnitude);
  public void Normalize(){float m=magnitude;if(m>0){x/=m;y/=m;z/=m;}}
  public static Vector3 ClampMagnitude(Vector3 v,float m){return v.magnitude<=m?v:v*(m/v.magnitude);}
  public static Vector3 operator+(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator-(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);public static Vector3 operator-(Vector3 a)=>new Vector3(-a.x,-a.y,-a.z);public static Vector3 operator*(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);
 }
}
