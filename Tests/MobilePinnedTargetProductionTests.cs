using System;using System.Collections.Generic;using System.Reflection;using Emberfall;using UnityEngine;
namespace UnityEngine
{
 public class Object{public static void Destroy(Object o){}}
 public class MonoBehaviour:Object{public GameObject gameObject=new GameObject();public Transform transform=>gameObject.transform;}
 public class GameObject:Object{public bool activeInHierarchy=true;public Transform transform=new Transform();public void SetActive(bool v){activeInHierarchy=v;}}
 public class Transform{public Vector3 position,forward=new Vector3(0,0,1),localScale=Vector3.one;public Quaternion rotation;}
 public struct Quaternion{public static Quaternion Euler(float x,float y,float z)=>new Quaternion();public static Vector3 operator*(Quaternion q,Vector3 v)=>v;public static Quaternion LookRotation(Vector3 v)=>new Quaternion();}
 public struct Color{public Color(float r,float g,float b,float a=1){}}
 public struct Bounds{public Vector3 min,max;public Bounds(Vector3 p){min=p-new Vector3(.5f,0,.5f);max=p+new Vector3(.5f,2,.5f);}}
 public struct Vector2{public float x,y;public Vector2(float a,float b){x=a;y=b;}public static Vector2 zero=>new Vector2();public float magnitude=>(float)Math.Sqrt(x*x+y*y);public static float Distance(Vector2 a,Vector2 b)=>(a-b).magnitude;public static Vector2 operator-(Vector2 a,Vector2 b)=>new Vector2(a.x-b.x,a.y-b.y);public static Vector2 operator/(Vector2 a,float b)=>new Vector2(a.x/b,a.y/b);public static Vector2 operator*(Vector2 a,float b)=>new Vector2(a.x*b,a.y*b);}
 public struct Vector3{public void Normalize(){this=normalized;}public static Vector3 operator-(Vector3 a)=>new Vector3(-a.x,-a.y,-a.z);public static Vector3 forward=>new Vector3(0,0,1);public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}public static Vector3 zero=>new Vector3();public static Vector3 up=>new Vector3(0,1,0);public static Vector3 one=>new Vector3(1,1,1);public float sqrMagnitude=>x*x+y*y+z*z;public float magnitude=>(float)Math.Sqrt(sqrMagnitude);public Vector3 normalized=>magnitude>0?this/magnitude:zero;public static float Angle(Vector3 a,Vector3 b){return (float)(Math.Acos(Math.Clamp((a.x*b.x+a.y*b.y+a.z*b.z)/Math.Max(.0001f,a.magnitude*b.magnitude),-1,1))*180/Math.PI);}public static Vector3 ClampMagnitude(Vector3 v,float m)=>v.magnitude>m?v.normalized*m:v;public static Vector3 operator+(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator-(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);public static Vector3 operator*(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);public static Vector3 operator/(Vector3 a,float b)=>new Vector3(a.x/b,a.y/b,a.z/b);public static implicit operator Vector2(Vector3 a)=>new Vector2(a.x,a.y);}
 public struct Rect{public float x,y,width,height;public Rect(float a,float b,float c,float d){x=a;y=b;width=c;height=d;}public float xMin=>x;public float xMax=>x+width;public float yMin=>y;public float yMax=>y+height;public static Rect MinMaxRect(float a,float b,float c,float d)=>new Rect(a,b,c-a,d-b);public bool Contains(Vector2 p)=>p.x>=x&&p.x<xMax&&p.y>=y&&p.y<yMax;public bool Overlaps(Rect r)=>x<r.xMax&&xMax>r.x&&y<r.yMax&&yMax>r.y;public static bool operator==(Rect a,Rect b)=>a.x==b.x&&a.y==b.y&&a.width==b.width&&a.height==b.height;public static bool operator!=(Rect a,Rect b)=>!(a==b);public override bool Equals(object o)=>o is Rect r&&this==r;public override int GetHashCode()=>0;}
 public static class Mathf{public static float Clamp(float v,float a,float b)=>Math.Clamp(v,a,b);public static float Clamp01(float v)=>Clamp(v,0,1);public static float Max(float a,float b)=>Math.Max(a,b);public static float Min(float a,float b)=>Math.Min(a,b);public static float Lerp(float a,float b,float t)=>a+(b-a)*t;}
 public enum TouchPhase{Began,Moved,Stationary,Ended,Canceled}public struct Touch{public int fingerId;public TouchPhase phase;public Vector2 position;}
 public static class Input{public static bool mousePresent;public static List<Touch> touches=new List<Touch>();public static int touchCount=>touches.Count;public static Touch GetTouch(int i)=>touches[i];public static bool GetMouseButton(int i)=>false;public static bool GetMouseButtonDown(int i)=>false;public static bool GetMouseButtonUp(int i)=>false;public static Vector3 mousePosition;}
 public static class Screen{public static float width=568,height=320;}public static class Time{public static int frameCount;public static float deltaTime;}
 public struct Ray{public Vector3 GetPoint(float v)=>new Vector3();}public struct Plane{public Plane(Vector3 a,Vector3 b){}public bool Raycast(Ray r,out float t){t=1;return true;}}
 public static class Random{public static float value=>.5f;}
 public class Camera{public static Camera main=new Camera();public Rect pixelRect=new Rect(0,0,568,320);public float nearClipPlane=.1f;public Vector3 WorldToScreenPoint(Vector3 p)=>new Vector3(280+p.x*10,160+p.z*10+p.y*3,10+p.z);public Ray ScreenPointToRay(Vector2 p)=>new Ray();public T GetComponent<T>()where T:new()=>new T();}
}
namespace Emberfall
{
 public class EnemyController{public GameObject gameObject=new GameObject();public Transform transform=>gameObject.transform;public bool IsDead,IsBoss;public void TrySkillInterrupt(PlayerController p,int skill,int cast){}public float HitFootprintBonus;public EnemyController(float z){transform.position=new Vector3(0,0,z);}}
 // Optional authored visuals are emission boundaries in this targeting suite.
 // Actual geometry, admission and lifecycle execute in BlenderSkillVfxProductionTests.
 public static class BlenderSkillVfx{public static bool TryPlay(PlayerController hero,float range,bool shock)=>false;}
 public static class WeaponSlashRibbon{public static void Spawn(params object[] args){}}
 public static class CombatFx{public static void Ring(params object[] a){}public static Vector3 Flat(Vector3 v)=>new Vector3(v.x,0,v.z);public static void WeaponSlash(params object[] p){}}
 public static class CombatSight{public static float BlockedZ=float.NaN;public static bool Visible=true;public static bool Direct(Vector3 a,Vector3 b)=>Visible&&b.z!=BlockedZ;public static bool Melee(Vector3 a,Vector3 b)=>Direct(a,b);public static Vector3 GroundPoint(Vector3 a,Vector3 b)=>Visible?b:a;}
 public enum RunBlessing{ChargedWard}public enum SkillVisualRecipe{Neutral,Steel,Ice,Fire,Poison,Lightning,Arcane,Spirit,ArrowRain}
 public struct MasteryResourceProc{public float Energy,CooldownReduction;}public class Mastery{public MasteryResourceProc SkillSpent(float cost)=>default;}
 public static class WorldTraversal{public static bool LeapAllowed=true;public static bool CanLeap(Vector3 from,Vector3 to,float radius)=>LeapAllowed;}
 public class ProjectileVolleyBudget<T>{public ProjectileVolleyBudget(float attack,float cap){}}
 public static class CombatArea{public static void Spawn(PlayerController player,GameSession game,Vector3 at,float size,CombatDamage amount,float disable,float startup,float activeTime,float tickInterval,Color tint,bool followPlayer=false,bool fallingMeteor=false,float pulling=0,CombatDamage finisher=default,int statusSkill=-1,int statusRank=1,int castId=0,SkillVisualRecipe visual=SkillVisualRecipe.Neutral,int trackedMechanic=-1){player.RecordEmission(at);}}
 public static class SummonerSpell{public static void Cast(PlayerController player,GameSession game,int skill,int rank,Vector3 point,float damage,EnemyController target=null,bool preserve=false,int castId=0){player.RecordEmission(point,target);}}
 public static class AdvancedSkillSequence{public static void Spawn(params object[] a){}}
 public class FakeProgression{public GameProfile Profile=new GameProfile{skillRanks=new int[10]};public float MechanicRangeMultiplier(EquipmentMechanic m)=>1;public float MechanicPowerMultiplier(EquipmentMechanic m)=>1;}
 // Historical targeting/healing fixtures represent ordinary play, never the isolated practice session.
 public class GameSession{public bool PracticeActive=>false;public void RecordPracticeCast(int castId,int skill){if(PracticeActive)throw new InvalidOperationException("unexpected practice session in ordinary targeting fixture");}public void Notify(string s){}public bool TrySpendHealingCharge()=>true;public bool HasBlessing(RunBlessing b)=>false;public PlayerController Player;public bool HasStarted=true,InputBlocked;public bool ChallengeRun,InDungeon;public int HealingCharges=3;public float ArenaRadius=25;public List<EnemyController> Enemies=new List<EnemyController>();public FakeProgression Progression=new FakeProgression();public string Failure;public void ReportControlFailure(string key,string s){Failure=key+":"+s;}public void RecordClassTutorial(HeroClass h){}public void RecordCombatAction(string s){}public void SpawnMechanismText(Vector3 p,string s,Color c){}}
 public class GameUI{public bool LifecycleTouchBlocked,CompanionCommandsVisible,Overlay;public void RefreshTouchViewport(){}public bool TryBeginTouchSkill(int f,Vector2 p)=>p.x>500&&p.y>250;public void UpdateTouchSkill(int f,Vector2 p,bool e,bool c){}public bool IsScreenPointOverUI(Vector2 p)=>Overlay||p.x<45;public bool IsScreenPointOverHUD(Vector2 p)=>Overlay||p.x<45;public void ActivateFreeCommand(bool b){}public void ActivateMobileInteraction(int f){}public void CancelMobileCast(){}}
 public class AdventureCamera{public static float Pitch;public void ApplyMobilePitch(float d){Pitch+=d;}}
 public class Recovery{public bool Blocked;public void Begin(HeroClass hero,int skill,bool charged){}}public class Model{public void ReleaseCharge(int skill){}public void PlayAction(int s,bool a,float c=0){}}
 public class Bonus{public float AttackSpeedMultiplier=1,CooldownMultiplier=1;}
 public struct CombatDamage{public static implicit operator CombatDamage(float n)=>new CombatDamage();}public enum SoundCue{Attack,Cast}public static class GameAudio{public static void Play(SoundCue c){}}
 public static class PlayerUpgradeRules{public static float CounterAfterAttack(float a,float b,bool hit)=>b;}
 public static class CombatProjectile{public static bool CanLaunchFromMuzzle(PlayerController p,Vector3 at,CombatDamage d,int cast)=>true;public static void Friendly(PlayerController player,GameSession game,Vector3 at,Vector3 forward,CombatDamage amount,Color tint,bool piercing=false,bool arrow=false,bool basic=false,float size=1,float velocity=0,EnemyController tracking=null,CombatDamage blastDamage=default,float blastRadius=0,int skillIndex=-1,int castId=0,object companionSource=null,ProjectileVolleyBudget<EnemyController> volley=null,EnemyController markTarget=null,float markStrength=0){}public static int Shots;public static EnemyController Last;public static void BasicShot(PlayerController p,GameSession s,Vector3 a,Vector3 b,CombatDamage d,Color c,bool r,EnemyController e){Shots++;Last=e;}}
 public static class CombatReviewEvents{public static bool Enabled=false;public static void Emit(string name,int id,int skill=0){}}public static class CombatReviewObjectId{public static int Get(object o)=>1;}
 public static class SummonedCompanion{public static bool HealingTarget;public static bool HasHealingTarget(PlayerController p)=>HealingTarget;public static EnemyController Team;public static int Commands,FocusReads;public static EnemyController ExplicitFocus(PlayerController p){FocusReads++;return Team;}}
 // Protection remains an explicit presentation-only boundary in this targeting/cost fixture.
 public class AdvancedSkillVfx:MonoBehaviour{public static void Beam(params object[] a){}public static AdvancedSkillVfx Protection(PlayerController owner,Vector3 at,float radius,Color color,float lifetime,int detail,Func<bool> active,bool passive=false)=>new AdvancedSkillVfx();public static AdvancedSkillVfx Rune(params object[] a)=>new AdvancedSkillVfx();}
 public sealed partial class PlayerController:MonoBehaviour
 {
  public CombatOpportunityState[] OpportunityWindows=new CombatOpportunityState[10];public CombatOpportunityState CounterWindow,ComboWindow;public CombatOpportunityState SkillOpportunityWindow(int skill)=>session.InputBlocked?default:OpportunityWindows[skill];public CombatOpportunityState BasicOpportunityWindow(bool mastery=false)=>session.InputBlocked?default:mastery?ComboWindow:CounterWindow;
  // Mechanism-free equipment in this fixture: the full production B branch remains compiled.
  private bool ConcentratedVenom=>false;
  private void CastConcentratedVenom(int rank,float range,Color color,int castId){throw new InvalidOperationException("unexpected venom B dispatch with mechanism-free equipment");}
  public float Health=100,MaxHealth=100;public GameSession session;public HeroClass HeroClass=HeroClass.Arcanist;public bool IsDead,TraversalStartedThisFrame;public int CombatEpoch=1;public EnemyController AimTarget,FocusTarget;public Vector3 aimPoint;public Vector3 AimPoint=>aimPoint;
  public SkillChargeController charge;public SkillTargetingController targeting;public int Casts;public Vector3 LastCast;public float Energy=>skillRuntime.Energy;public SkillRuntime skillRuntime=new SkillRuntime(HeroClass.Arcanist);public EnemyController LastCastEnemy;
  readonly List<MobileSkillPolicy.Candidate> mobileAimCandidates=new List<MobileSkillPolicy.Candidate>();Recovery skillBasicRecovery=new Recovery();Model model=new Model();Bonus ActiveRunBonuses;float attackCooldown,mobilityTime,attackAnimation,counterTime,perfectDodgeCounterTime;int mobilityRank;bool lastMeleeDamagedEnemy,executingChargedSkill;
  public PlayerController(GameSession s){session=s;s.Player=this;charge=new SkillChargeController();charge.Initialize(this,s);targeting=new SkillTargetingController(this,s);for(int i=0;i<10;i++)s.Progression.Profile.skillRanks[i]=1;}
  public T GetComponent<T>()where T:class=>typeof(T)==typeof(SkillChargeController)?charge as T:targeting as T;
  Bounds EnemyAimBounds(EnemyController e)=>new Bounds(e.transform.position);Vector3 EnemyBodyPoint(EnemyController e)=>e.transform.position+Vector3.up;void PruneAimGeometry(){}
  public void CancelCombatPose(){}
  public bool jumping;float skillFeedbackCooldown,castDamageRoll,chargedWardTime,movementSkillLock,guardTime,guardReduction,guardPower,guardRadius,guardPulseTimer,burnStrideTime,invulnerability;int nextCastId,guardRank,guardCastId;public ElementalistSpecialization Specialization;
  public class Stats{public float Damage=10;}Stats stats=new Stats();float CombatAttack=>stats.Damage;int MechanicVariant(EquipmentMechanic m)=>0;Mastery masteryCore=new Mastery();void TraversalFailure(){}
  public void RecordEmission(Vector3 target,EnemyController confirmedTarget=null){Casts++;LastCast=target;LastCastEnemy=confirmedTarget;}

  CombatDamage Damage(float n)=>new CombatDamage();bool HasMechanic(EquipmentMechanic m)=>false;bool ReturningCounterVariant=>false;bool ReturningCounterReady=>false;bool Melee(float r,float a,CombatDamage d,float k,float s,float knockdown=0,bool basic=false,int skillIndex=-1,int castId=0,bool counterThrust=false)=>false;
  public Vector3 Aim(Vector3 movement)=>ResolveMobileAim(movement);public void Attack()=>BasicAttack();public void AdvanceCharge(float dt){typeof(SkillChargeController).GetMethod("Advance",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(charge,new object[]{dt});}
 }
 public partial class SkillTargetingController
 {
  PlayerController owner;GameSession session;int skill=-1,epoch,cancelledFrame=-1,castFrame=-1;bool previewDirty;GameObject visuals;public bool IsTargeting=>skill>=0;public Preview CurrentPreview;public Vector3 TargetPoint;public bool ConfirmingContract;public SkillTargetingController(PlayerController p,GameSession s){owner=p;session=s;}void EnsureVisuals(){visuals=new GameObject();}public bool CanBeginThisFrame=>castFrame!=Time.frameCount;public void SetTarget(Vector3 p){TargetPoint=p;}public bool Confirm()=>true;
 }
 public sealed partial class MobileControls
 {
  enum Role{Move,Attack,Skill,Aim,Camera,Consumed}readonly Dictionary<int,Role> fingers=new Dictionary<int,Role>();readonly MobileCameraGesture cameraGesture=new MobileCameraGesture();readonly List<int> staleFingers=new List<int>();static MobileControls instance;GameSession session;GameUI ui;int moveFinger=-1000;Vector2 joystickOrigin;bool hasJoystickOrigin;PlayerController worldPointerOwner;EnemyController worldPointerTarget;int worldPointerEpoch;Rect lastSafe;
  public static bool Active=true,SimulationEnabled;public static Vector2 Move;public static bool AttackHeld;static bool dodge,potion,jump;public static MobileControlLayout Layout=new MobileControlLayout(568,320,160);public static Rect SafeArea=new Rect(0,0,568,320);float Scale=>Layout.Scale;Vector2 ToUI(Vector2 p)=>(new Vector2(p.x,Screen.height-p.y)-new Vector2(SafeArea.x,Screen.height-SafeArea.yMax))/Scale;static Rect Area(MobileControlLayout.Area a)=>new Rect(a.X,a.Y,a.Width,a.Height);Rect Attack=>Area(Layout.Attack);Rect Dodge=>Area(Layout.Dodge);Rect Potion=>Area(Layout.Potion);Rect Jump=>Area(Layout.Jump);Rect Cancel=>Area(Layout.Cancel);bool CanCancel=>session.Player.charge.IsCharging;
  public MobileControls(GameSession s,GameUI u){session=s;ui=u;instance=this;lastSafe=SafeArea;}void CheckDodgeFeedback(){}void CheckPotionFeedback(){}
  public void Tick()=>Update();public Vector2 Control(MobileControlLayout.Area a)=>new Vector2(SafeArea.x+(a.X+a.Width/2)*Scale,SafeArea.yMax-(a.Y+a.Height/2)*Scale);
 }
}
public static class MobilePinnedTargetProductionTests
{
 static int n;static void Check(bool b,string m){n++;if(!b)throw new Exception(m);}static Vector2 ScreenEnemy(EnemyController e)=>Camera.main.WorldToScreenPoint(e.transform.position+Vector3.up);
 static void Tap(MobileControls c,int f,Vector2 p){c.ProcessPointer(f,TouchPhase.Began,p);c.ProcessPointer(f,TouchPhase.Ended,p);}
 public static string Run()
 {
  n=0;Time.frameCount=1;var s=new GameSession();var hero=new PlayerController(s);var ui=new GameUI();var controls=new MobileControls(s,ui);var a=new EnemyController(8);var b=new EnemyController(2);s.Enemies.Add(a);s.Enemies.Add(b);var p=ScreenEnemy(a);
  controls.ProcessPointer(11,TouchPhase.Began,controls.Control(MobileControls.Layout.Joystick));Tap(controls,12,p);Check(hero.MobilePinnedTarget==a,"joystick and separate tap select actual projected enemy");hero.Aim(new Vector3(0,0,-1));Check(hero.AimTarget==a,"movement and closer enemy cannot replace pin");hero.Attack();Check(CombatProjectile.Last==a,"actual basic attack uses pinned enemy");hero.PrepareMobileSkillAim(1);Check(hero.AimTarget==a&&hero.AimPoint.z==8,"actual skill preparation shares pinned intent");
  hero.PinMobileTarget(null);controls.ProcessPointer(12,TouchPhase.Began,p);controls.ProcessPointer(12,TouchPhase.Moved,new Vector2(p.x+20,p.y));controls.ProcessPointer(12,TouchPhase.Moved,p);controls.ProcessPointer(12,TouchPhase.Ended,p);Check(hero.MobilePinnedTarget==null,"horizontal out-and-back never taps");
  foreach(Vector2 move in new[]{new Vector2(0,20),new Vector2(8,8)}){controls.ProcessPointer(12,TouchPhase.Began,p);controls.ProcessPointer(12,TouchPhase.Moved,new Vector2(p.x+move.x,p.y+move.y));controls.ProcessPointer(12,TouchPhase.Ended,p);Check(hero.MobilePinnedTarget==null,"vertical and diagonal drag never tap");}
  controls.ProcessPointer(12,TouchPhase.Began,p);controls.ProcessPointer(13,TouchPhase.Ended,p);Check(hero.MobilePinnedTarget==null,"nonowner release cannot submit tap");controls.ProcessPointer(12,TouchPhase.Canceled,p);Check(hero.MobilePinnedTarget==null,"canceled pointer never pins");
  controls.ProcessPointer(12,TouchPhase.Began,p);controls.ProcessPointer(13,TouchPhase.Began,controls.Control(MobileControls.Layout.Attack));controls.ProcessPointer(12,TouchPhase.Ended,p);Check(hero.MobilePinnedTarget==null,"action finger preempts world candidate");MobileControls.ResetInput();
  controls.ProcessPointer(12,TouchPhase.Began,new Vector2(10,100));controls.ProcessPointer(12,TouchPhase.Ended,p);Check(hero.MobilePinnedTarget==null,"UI-started pointer never becomes world tap");controls.ProcessPointer(12,TouchPhase.Began,p);controls.ProcessPointer(12,TouchPhase.Ended,new Vector2(10,100));Check(hero.MobilePinnedTarget==null,"release over UI never commits");
  controls.ProcessPointer(12,TouchPhase.Began,p);hero.CombatEpoch++;controls.ProcessPointer(12,TouchPhase.Ended,p);Check(hero.MobilePinnedTarget==null,"room epoch cancels pending world pointer");controls.ProcessPointer(12,TouchPhase.Began,p);controls.Tick();controls.ProcessPointer(12,TouchPhase.Ended,p);Check(hero.MobilePinnedTarget==null,"missing touch is cancelled through real Update");
  Tap(controls,12,p);Check(hero.MobilePinnedTarget==a,"fresh pointer can pin after cancellation");Tap(controls,12,new Vector2(220,180));Check(hero.MobilePinnedTarget==null,"empty world tap clears pin");
  controls.ProcessPointer(12,TouchPhase.Began,p);controls.ProcessPointer(13,TouchPhase.Began,new Vector2(10,100));controls.ProcessPointer(12,TouchPhase.Ended,p);Check(hero.MobilePinnedTarget==null,"another UI pointer cancels candidate");
  controls.ProcessPointer(12,TouchPhase.Began,p);ui.LifecycleTouchBlocked=true;controls.ProcessPointer(12,TouchPhase.Ended,p);ui.LifecycleTouchBlocked=false;Check(hero.MobilePinnedTarget==null,"viewport/background lifecycle block discards candidate");
  controls.ProcessPointer(12,TouchPhase.Began,p);s.InputBlocked=true;controls.Tick();s.InputBlocked=false;controls.ProcessPointer(12,TouchPhase.Ended,p);Check(hero.MobilePinnedTarget==null,"pause interrupt requires fresh world down");
  controls.ProcessPointer(12,TouchPhase.Began,p);controls.ProcessPointer(12,TouchPhase.Ended,ScreenEnemy(b));Check(hero.MobilePinnedTarget==null,"start and release must resolve same projected identity");
  for(int density=1;density<=3;density++){var gesture=new MobileCameraGesture();gesture.Begin(8,100,100);gesture.Move(8,100+9*density,100,density,false,false);gesture.Move(8,100,100,density,true,false);Check(!gesture.TapCompleted,"horizontal threshold scales by actual logical density");}
  var overlap=new EnemyController(8);s.Enemies.Add(overlap);Check(hero.PickMobileTarget(p)==a,"exact projected overlap preserves stable session order");s.Enemies.Remove(overlap);
  hero.PinMobileTarget(a);CombatSight.Visible=false;int shots=CombatProjectile.Shots;hero.Attack();Check(CombatProjectile.Shots==shots&&s.Failure=="attack:目标被遮挡"&&hero.MobilePinnedTarget==a,"occlusion refuses basic attack without replacing intent");float energy=hero.Energy;Check(!hero.targeting.Begin(1)&&!hero.charge.IsCharging&&hero.Energy==energy&&hero.Casts==0&&hero.MobilePinnedTarget==a,"blocked pin rejects actual targeted skill without charge or resource spending");CombatSight.Visible=true;
  a.transform.position=new Vector3(0,0,12);Check(!hero.targeting.Begin(1)&&s.Failure=="skill1:距离不足"&&hero.MobilePinnedTarget==a,"skill range failure retains valid general-scope pin");
  a.transform.position=new Vector3(0,0,8);Time.frameCount++;Check(hero.targeting.Begin(1)&&hero.charge.IsCharging,"actual targeting begins charge from pinned point");var captured=hero.charge.TargetPoint;hero.PinMobileTarget(b);Check(hero.charge.TargetPoint.z==captured.z,"new pin never changes captured charge point");hero.AdvanceCharge(20);Check(hero.Casts==1&&hero.LastCast.z==captured.z&&hero.MobilePinnedTarget==b,"actual charge release ignores later pin and uses snapshot");
  Time.frameCount++;hero.PinMobileTarget(a);a.IsDead=true;hero.Aim(Vector3.one);Check(hero.MobilePinnedTarget==null&&hero.AimTarget==b,"dead pin clears and automatic targeting recovers");a.IsDead=false;hero.PinMobileTarget(a);s.Enemies.Remove(a);Check(hero.MobilePinnedTarget==null,"removed target clears scope");s.Enemies.Add(a);hero.PinMobileTarget(a);a.transform.position=new Vector3(0,0,15);Check(hero.MobilePinnedTarget==null,"same existing14m scope expires pin");a.transform.position=new Vector3(0,0,8);hero.PinMobileTarget(a);hero.CombatEpoch++;Check(hero.MobilePinnedTarget==null,"new combat epoch clears pin");
  hero.HeroClass=HeroClass.Summoner;hero.PinMobileTarget(a);SummonedCompanion.Team=b;hero.PrepareMobileSkillAim(9);Check(hero.AimTarget==b&&hero.MobilePinnedTarget==a&&SummonedCompanion.Commands==0,"ordinary contract preview follows explicit team without pin commanding pets");Time.frameCount++;Check(hero.targeting.Begin(9)&&hero.charge.TargetEnemy==b,"ordinary contract charge owns same explicit team intention");
  // New scenes exercise the actual readiness, SkillRuntime and full CastSkillCore.
  // Only the emitted CombatArea and optional mechanics/visuals are boundary recorders.
  foreach(bool obstructed in new[]{true,false})
  {
   Time.frameCount++;CombatSight.Visible=true;CombatSight.BlockedZ=float.NaN;
   var game=new GameSession();var player=new PlayerController(game);var capturedEnemy=new EnemyController(8);var later=new EnemyController(obstructed?2:12);game.Enemies.Add(capturedEnemy);game.Enemies.Add(later);player.PinMobileTarget(capturedEnemy);
   float before=player.Energy;float cost=GameBalance.SkillEnergyCost(HeroClass.Arcanist,1);
   Check(player.SkillTargetingReady(1)&&player.targeting.Begin(1)&&player.charge.IsCharging&&player.Energy==before,"real readiness begins a charge without reserving or spending its budget");
   Vector3 fixedPoint=player.charge.TargetPoint;var fixedEnemy=player.charge.TargetEnemy;player.PinMobileTarget(later);if(obstructed)CombatSight.BlockedZ=later.transform.position.z;
   Check(player.MobilePinnedActionReason(1)==(obstructed?"目标被遮挡":"距离不足")&&!player.SkillTargetingReady(1),"later pin rejects new action while actual pending charge locks readiness");
   player.AdvanceCharge(20);
   Check(player.Casts==1&&player.LastCast.z==fixedPoint.z&&player.LastCastEnemy==fixedEnemy&&player.MobilePinnedTarget==later,obstructed?"actual readiness and full cast preserve confirmed point after later pin becomes blocked":"actual readiness and full cast preserve confirmed point after later pin exceeds skill range");
   Check(Math.Abs(player.Energy-(before-cost))<.001f&&Math.Abs(player.skillRuntime.Remaining(1)-GameBalance.EffectiveCooldown(HeroClass.Arcanist,1,1))<.001f,"actual SkillRuntime charges exactly one learned skill budget at release");
   Check(!player.ExecuteChargedSkill(1)&&player.Casts==1&&Math.Abs(player.Energy-(before-cost))<.001f,"same-frame repeated release cannot pay or emit twice through real readiness");
   Time.frameCount++;Check(!player.ExecuteChargedSkill(1)&&player.Casts==1,"real cooldown still rejects duplicate release on following frame");
  }
  CombatSight.BlockedZ=float.NaN;Time.frameCount++;
  {
   var game=new GameSession();var player=new PlayerController(game);player.HeroClass=HeroClass.Summoner;player.skillRuntime=new SkillRuntime(HeroClass.Summoner);
   var teamA=new EnemyController(7);var laterPin=new EnemyController(12);var newTeam=new EnemyController(4);game.Enemies.Add(teamA);game.Enemies.Add(laterPin);game.Enemies.Add(newTeam);SummonedCompanion.Team=teamA;player.PinMobileTarget(teamA);
   Check(player.targeting.Begin(9)&&player.charge.TargetEnemy==teamA,"actual contract confirmation retains a nonnull captured team enemy");
   var fixedPoint=player.charge.TargetPoint;float before=player.Energy;player.PinMobileTarget(laterPin);SummonedCompanion.Team=newTeam;CombatSight.BlockedZ=laterPin.transform.position.z;
   player.AdvanceCharge(20);
   Check(player.Casts==1&&player.LastCast.z==fixedPoint.z&&player.LastCastEnemy==teamA&&player.MobilePinnedTarget==laterPin,"full contract cast sends captured enemy and point despite later blocked pin and changed team");
   Check(Math.Abs(player.Energy-(before-GameBalance.SkillEnergyCost(HeroClass.Summoner,9)))<.001f&&player.skillRuntime.Remaining(9)>0&&SummonedCompanion.Commands==0,"contract release spends real skill budget once and pin never sends a free command");
  }
  CombatSight.BlockedZ=float.NaN;
  {
   var game=new GameSession();var warrior=new PlayerController(game);warrior.HeroClass=HeroClass.Vanguard;
   var distant=new EnemyController(4);game.Enemies.Add(distant);warrior.PinMobileTarget(distant);
   Check(warrior.MobilePinnedActionReason(-1)=="距离不足"&&warrior.MobilePinnedActionReason(1)=="","warrior basic rejection does not reject longer-range skill");
  }
  foreach(bool unlearned in new[]{false,true})
  {
   Time.frameCount++;var game=new GameSession();var player=new PlayerController(game);var target=new EnemyController(8);game.Enemies.Add(target);player.PinMobileTarget(target);Check(player.targeting.Begin(1),"budget revalidation case starts a real confirmed charge");
   if(unlearned)game.Progression.Profile.skillRanks[1]=0;
   else {float cost=GameBalance.SkillEnergyCost(HeroClass.Arcanist,1);while(player.Energy>=cost){player.skillRuntime.ResetCooldowns();player.skillRuntime.TryConsume(1,1);}player.skillRuntime.ResetCooldowns();}
   float before=player.Energy;player.AdvanceCharge(20);Check(player.Casts==0&&player.Energy==before&&!player.charge.IsCharging,unlearned?"unlearned at release is rejected by actual readiness without emission":"insufficient real runtime energy at release is rejected without emission");
  }
  // Execute the same observation used by both HUDs against real budget/cast code.
  foreach(HeroClass kind in Enum.GetValues(typeof(HeroClass)))
  {
   Time.frameCount++;var game=new GameSession();var player=new PlayerController(game);player.HeroClass=kind;player.skillRuntime=new SkillRuntime(kind);
   CombatSight.Visible=true;SummonedCompanion.FocusReads=0;
   float observedEnergy=player.Energy;var aim=player.AimPoint;var facing=player.transform.forward;
   for(int repeat=0;repeat<3;repeat++)for(int skill=0;skill<10;skill++)
    Check(player.IsSkillAvailable(skill)==!GameBalance.IsPassive(skill),"learned active skills allow empty ground while passives never advertise a cast");
   Check(SummonedCompanion.FocusReads==0&&player.Energy==observedEnergy&&player.Casts==0&&player.AimPoint.sqrMagnitude==aim.sqrMagnitude&&player.transform.forward.z==facing.z&&game.Failure==null,"readiness observation never mutates companion focus, resources, facing or feedback");
   game.Progression.Profile.skillRanks[0]=0;Check(!player.IsSkillAvailable(0),"unlearned skill unavailable");game.Progression.Profile.skillRanks[0]=1;
   player.IsDead=true;Check(!player.IsSkillAvailable(0),"dead hero unavailable");player.IsDead=false;
   player.jumping=true;Check(!player.IsSkillAvailable(0),"jump blocks cast readiness");player.jumping=false;
   game.InputBlocked=true;Check(!player.IsSkillAvailable(0),"blocked input unavailable");game.InputBlocked=false;
   Check(player.skillRuntime.TryConsume(0,1)&&!player.IsSkillAvailable(0),"real cooldown blocks HUD readiness");player.skillRuntime.ResetCooldowns();
   float cost=GameBalance.SkillEnergyCost(kind,0);while(player.Energy>=cost){player.skillRuntime.TryConsume(0,1);player.skillRuntime.ResetCooldowns();}
   Check(!player.IsSkillAvailable(0),"real energy budget blocks HUD readiness");
  }
  {
   Time.frameCount++;var game=new GameSession();var player=new PlayerController(game);var pin=new EnemyController(8);game.Enemies.Add(pin);player.PinMobileTarget(pin);
   CombatSight.Visible=false;Check(!player.IsSkillAvailable(1)&&game.Failure==null,"blocked pin is observed without failure notification");CombatSight.Visible=true;
   player.CombatEpoch++;var field=typeof(PlayerController).GetField("mobilePinnedEnemy",BindingFlags.NonPublic|BindingFlags.Instance);
   Check(player.IsSkillAvailable(1)&&ReferenceEquals(field.GetValue(player),pin),"stale pin observation does not clear stored identity");
   player.ClearMobilePinnedTarget();Check(player.targeting.Begin(1)&&!player.IsSkillAvailable(1),"pending charge blocks new readiness");player.charge.Cancel();
  }
  foreach(HeroClass kind in new[]{HeroClass.Vanguard,HeroClass.Ranger})
  {
   Time.frameCount++;var game=new GameSession();var player=new PlayerController(game);player.HeroClass=kind;player.skillRuntime=new SkillRuntime(kind);int skill=kind==HeroClass.Vanguard?5:4;
   WorldTraversal.LeapAllowed=false;Check(!player.IsSkillAvailable(skill),"illegal movement destination blocks readiness");WorldTraversal.LeapAllowed=true;Check(player.IsSkillAvailable(skill),"legal empty-ground movement remains available");
  }
  {
   Time.frameCount++;var game=new GameSession();var player=new PlayerController(game);game.ChallengeRun=true;game.InDungeon=true;
   Check(!player.IsSkillAvailable(6),"limited healing at full health has no effect");player.Health=50;Check(player.IsSkillAvailable(6),"limited healing with health deficit available");game.HealingCharges=0;Check(!player.IsSkillAvailable(6),"zero limited healing charges unavailable");
   game.HealingCharges=1;player.Health=100;player.HeroClass=HeroClass.Summoner;player.skillRuntime=new SkillRuntime(HeroClass.Summoner);SummonedCompanion.HealingTarget=true;
   Check(player.IsSkillAvailable(6),"injured companion allows full-health summoner healing");SummonedCompanion.HealingTarget=false;
  }
  return "PASS: "+n+" actual pointer/aim/basic/targeting/charge assertions (managed scene doubles, not touch-device delivery)";
 }
}

namespace Emberfall{internal static class ReturningCounterRules{internal static string Predict(UnityEngine.Vector3 a,UnityEngine.Vector3 b,bool boss,float footprint,out UnityEngine.Vector3 landing){throw new System.Exception("Counter disabled in pin-only suite");}internal static UnityEngine.Vector3 Advance(UnityEngine.Vector3 a,UnityEngine.Vector3 b,UnityEngine.Vector3 c){throw new System.Exception("ReturningBlade is disabled in aim fixture; actual adapter has its own suite");}}}
