using System;
using System.Collections.Generic;
using System.Linq;
namespace UnityEngine
{
 public class MonoBehaviour:Component { public int CoroutineStops,InvokeStops;public void StopAllCoroutines(){CoroutineStops++;}public void CancelInvoke(){InvokeStops++;} public static void Destroy(GameObject go){GameObject.Roots.Remove(go);} }
 public class Component { public GameObject gameObject;public Transform transform{get{return gameObject.transform;}} public T GetComponent<T>() where T:class{return gameObject.GetComponent<T>();} }
 public class Transform {public Vector3 position;}public struct Color{public Color(float r,float g,float b){}}
 public class GameObject
 {
  public static string FailDisableName;public static List<GameObject> Roots=new List<GameObject>();readonly List<object> components=new List<object>();public bool activeSelf=true;public string name;public Transform transform=new Transform();public Scene scene=new Scene();
  public GameObject(string n){name=n;Roots.Add(this);}public bool activeInHierarchy=>activeSelf;public void SetActive(bool value){if(activeSelf==value)return;activeSelf=value;if(!value&&name==FailDisableName){FailDisableName=null;throw new Exception("injected root disable failure");}foreach(var c in components.ToArray()){var method=c.GetType().GetMethod(value?"OnEnable":"OnDisable",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);if(method!=null)method.Invoke(c,null);}}
  public T AddComponent<T>() where T:Component,new(){var c=new T{gameObject=this};components.Add(c);return c;}
  public T GetComponent<T>() where T:class{return components.OfType<T>().FirstOrDefault();}public T GetComponentInChildren<T>() where T:class{return GetComponent<T>();}
 }
 public class Scene{public static bool FailSnapshot;public GameObject[] GetRootGameObjects(){if(FailSnapshot)throw new Exception("snapshot failure");return GameObject.Roots.ToArray();}}
 public class Camera:Component{}public class Light:Component{}
 public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}public float sqrMagnitude{get{return x*x+y*y+z*z;}}public static Vector3 up=>new Vector3(0,1,0);public static Vector3 forward{get{return new Vector3(0,0,1);}}public static Vector3 operator *(Vector3 a,float b){return new Vector3(a.x*b,a.y*b,a.z*b);}public static Vector3 operator +(Vector3 a,Vector3 b){return new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);}public static Vector3 operator -(Vector3 a,Vector3 b){return new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);}public static Vector3 ClampMagnitude(Vector3 a,float b){return a;}}
 public static class Random{public struct State{public int value;}public static State state;public static void InitState(int n){state=new State{value=n};}}
 public static class Mathf{public static float Min(float a,float b)=>Math.Min(a,b);public static int CeilToInt(float v)=>(int)Math.Ceiling(v);public static float Max(float a,float b)=>Math.Max(a,b);public static float Sin(float n){return(float)Math.Sin(n);}}
 public static class Time{public static float deltaTime,timeScale,time;}public static class Application{public static event Func<bool> wantsToQuit;}
 public enum KeyCode{H,F}public static class Input{public static bool GetKeyDown(KeyCode code){return false;}}
 public static class Debug{public static void LogException(Exception e){}}
 public static class JsonUtility{public static string ToJson(object o,bool p){return "snapshot";}}
}
namespace Emberfall
{
 using UnityEngine;
 public static class MobileControls{public static bool ConsumePotion()=>false;}
 public enum HeroClass{Vanguard}public enum EnemyKind{Guardian,Wisp}
 public class GameProfile{public HeroClass heroClass;public int level=10;}
 public class ProgressionService
 {public bool HasCommittedWorldLoot(string itemId){throw new InvalidOperationException("practice admission must reject formal loot before receipt lookup");}
 public int Collections;public bool CollectLoot(ItemData item){Collections++;return true;}public int Saves;public bool SaveFails;public string LastError;public event Action Changed;public event Action<int> LeveledUp;public void Save(){Saves++;LastError=SaveFails?"injected save failure":"";}public string PracticeConfigurationSummary(){return "Human summary";}public bool IsPracticeOnly;public GameProfile Profile=new GameProfile();public ProgressionService CreatePracticeCopy(){return new ProgressionService{IsPracticeOnly=true};}public class BuildDraft{public bool valid=true;public ProgressionService CreatePracticeCopy(){return valid?new ProgressionService{IsPracticeOnly=true}:null;}}}
 public enum SoundCue{Loot}public class ItemData{public string name="loot";}public class GroundLootPickup:Component{public bool Retired;public void Retire(){Retired=true;}}
 public static class GameAudio{public static void Play(SoundCue cue){}public static void SetBackgroundPaused(bool p){}}
 public class PracticeChoices{public bool AwaitingChoice;}
 public class GameUI{public void EnterPracticePanel(){}public void LeavePracticePanel(){}}
 public class SkillChargeController:Component{public bool IsCharging;public void Initialize(PlayerController p,GameSession s){}}
 public class SkillTargetingController:Component{public void Initialize(PlayerController p,GameSession s){}}
 public class SkillRuntime{public HeroClass HeroClass;public Action<float> EnergyChanged;public SkillRuntime(HeroClass h){HeroClass=h;}}
 public static class GameBalance{public const int SkillCount=10;public static string SkillName(HeroClass h,int skill)=>"Frozen Hero Skill "+skill;public static string ClassName(HeroClass h){return "Hero";}}
 public class CombatModel:Component{public static CombatModel Hero(Transform t,HeroClass h){if(PlayerController.ThrowOnInitialize)throw new Exception("injected model creation failure");return new GameObject("model").AddComponent<CombatModel>();}}
 public partial class PlayerController:MonoBehaviour{public static bool ThrowOnInitialize;public int CombatEpoch;public float Health=37,Energy=23,Cooldown=8;public float MaxHealth=100;public bool IsDead=>Health<=0;GameSession session;GameUI inputUI;SkillRuntime skillRuntime;public HeroClass HeroClass;CombatModel model;SkillTargetingController targeting;SkillChargeController charge;Vector3 aimPoint;void RefreshStats(bool heal){Health=100;Energy=100;Cooldown=0;}public void Teleport(Vector3 p){transform.position=p;}}

 public class EnemyStatusEffects{public bool IsFrozen,KnockedDown,IsAirborne;public float MoveMultiplier=1;}
 public partial class EnemyController:MonoBehaviour{public bool aggro,preparing,activeChargePose,dodgePending;public float windup=2,chargeTime,comboDelay,sidestepTime,attackCooldown=.1f;public int comboRemaining=2,threatMember;public PlayerController dodgePlayer;public GameSession session;public GameObject warning=new GameObject("warning");public object telegraph;public ThreatAdmissionPolicy threatAdmission;public LargeBoss largeBoss;public EnemyStatusEffects StatusEffects;public bool IsStunned,IsDead;public float NavigationRadius=.5f;public EnemyKind Kind;public void ConfigurePracticeTarget(){}public void Provoke(){aggro=true;}public void BeginDeath(){IsDead=true;}}
 public partial class SummonedCompanion{
  public static bool TestConsume(PlayerController owner)=>State(owner).Commands.TryConsume(UnityEngine.Time.time);
  public static bool TestCooperation(PlayerController owner,EnemyController target,int form)=>State(owner).Cooperation.RegisterHit(target,form,UnityEngine.Time.time);
  public static bool TestProtection(PlayerController owner)=>State(owner).Commands.IsProtected(UnityEngine.Time.time);
 }
 public interface IPracticeProducerProbe{void Seed();bool Intact{get;}}
 public class PracticeAuraBoundary{public bool Stopped;public void Stop(){Stopped=true;}}
 public class PracticeArrowBoundary{public bool IsValid=true;public void Retire(){IsValid=false;}}
 public partial class CombatArea:Component,IPracticeProducerProbe{
  CastFirstHitReceipt castReceipt,issued;readonly ScheduledImpactBatch<EnemyController> pendingTickTargets=new ScheduledImpactBatch<EnemyController>();
  public void Seed(){castReceipt=issued=new CastFirstHitRegistry().Issue(1);pendingTickTargets.Begin(new List<EnemyController>{new EnemyController()});}
  public bool Intact=>issued.Active&&pendingTickTargets.Pending;
 }
 public partial class SummonerSpell:Component,IPracticeProducerProbe{
  CastFirstHitReceipt castReceipt,issued;readonly ScheduledImpactBatch<EnemyController> pendingTargets=new ScheduledImpactBatch<EnemyController>();
  public void Seed(){castReceipt=issued=new CastFirstHitRegistry().Issue(1);pendingTargets.Begin(new List<EnemyController>{new EnemyController()});}
  public bool Intact=>issued.Active&&pendingTargets.Pending;
 }
 public partial class AdvancedSkillSequence:Component,IPracticeProducerProbe{
  CastFirstHitReceipt castReceipt,issued;PracticeAuraBoundary healingAura,originalAura;PracticeArrowBoundary arrowBatch;int step,steps=3;
  public void Seed(){castReceipt=issued=new CastFirstHitRegistry().Issue(1);healingAura=originalAura=new PracticeAuraBoundary();arrowBatch=new PracticeArrowBoundary();}
  public bool Intact=>issued.Active&&!originalAura.Stopped&&arrowBatch.IsValid;
 }
 public class CombatProjectile:Component{}public class ThreatAdmissionPolicy{public void Release(int id){}public void Withdraw(int id){}}public class LargeBoss{public LargeBoss State=>this;public bool Interruptible;public void StopEncounter(){}}
 public static class CombatFx{public static void Ring(Vector3 p,float r,Color c,float t){}}
 public static class WorldTraversal{public static Vector3 Move(Vector3 a,Vector3 b,float radius){return new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);}public static bool HasLineOfSight(Vector3 a,Vector3 b){return true;}}
 public sealed partial class GameSession:MonoBehaviour
 { public bool RoomBranchChoiceOpen=>false;
  public bool InDungeon;private float runDamageTaken=91,runHealingReceived=42;private string lastDamageSource="real saved source";private float lastDamageAmount=17;public string LastRunSummary="real summary";private string BuildRunSummary(bool won){throw new Exception("practice must not rebuild real result");}public void SpawnFloatingText(Vector3 p,string s,Color c){}public bool RealTelemetryIntact=>runDamageTaken==91&&runHealingReceived==42&&lastDamageSource=="real saved source"&&lastDamageAmount==17&&LastRunSummary=="real summary";
  public class PendingLoot{public bool Collecting;public ItemData Item=new ItemData();public GroundLootPickup Pickup;}public Dictionary<string,PendingLoot> pendingLoot=new Dictionary<string,PendingLoot>();public HashSet<string> collectedGroundLoot=new HashSet<string>();public void LogSystem(string text){}
  public static GameSession Instance;public bool HasStarted=true,DungeonRewardPending,ModeRewardPending;private bool applicationQuitSavePrepared,applicationQuitSaveAccepted;private readonly ApplicationPauseState pauseState=new ApplicationPauseState();private readonly SaveLifecycleGate lifecycleSave=new SaveLifecycleGate();
  private bool TrySettleSideEventRewards()=>true;private bool TrySettleDungeonReward()=>true;private bool TrySettleArenaReward()=>true;private bool PreserveWorldLoot()=>true;private void SuspendInputs(){}private void OnProgressChanged(){}private void OnLevelUp(int level){}
  public bool TestQuit()=>CanQuitSafely();public void TestQuitEvent()=>OnApplicationQuit();public void TestPause(bool value)=>OnApplicationPause(value);public void TestFocus(bool value)=>OnApplicationFocus(value);public void TestDestroy()=>OnDestroy();public bool IsBackground=>pauseState.BackgroundPaused;
  public ProgressionService Progression=new ProgressionService();public PlayerController Player;public List<EnemyController> Enemies=new List<EnemyController>();
  public bool IsInCamp{get{return !PracticeActive;}}public bool IsDead,Paused,uiBlocking=true;public bool InputBlocked{get{return Paused||uiBlocking;}}public GameObject world;public GameUI ui;public string LastNotice;public void Notify(string s){LastNotice=s;}public bool DungeonSelectionOpen,ModeFinished;public PracticeChoices RunChoices=new PracticeChoices();
  void SpawnEnemy(EnemyKind kind,int level,Vector3 point,bool boss){var go=new GameObject("enemy");go.transform.position=point;var enemy=go.AddComponent<EnemyController>();enemy.Kind=kind;Enemies.Add(enemy);}
  public void DrinkPotion(){throw new Exception("potion outside this session lifecycle fixture");}
  public void TestTick(){TickPractice();}
 }
}
public static class CampPracticeSessionTests
{
 static int n;static void Check(bool value,string text){n++;if(!value)throw new Exception(text);}
 public static string Run()
 {
  UnityEngine.GameObject.Roots.Clear();var root=new UnityEngine.GameObject("session");var s=root.AddComponent<Emberfall.GameSession>();s.world=new UnityEngine.GameObject("world");s.Player=new UnityEngine.GameObject("original").AddComponent<Emberfall.PlayerController>();var original=s.Player;var owner=s.Progression;var enemies=s.Enemies;var draft=new Emberfall.ProgressionService.BuildDraft();UnityEngine.Random.InitState(42);
  foreach(string danger in new[]{"chasing","warning","near"}) {
   var enemy=new UnityEngine.GameObject("real enemy lifecycle").AddComponent<Emberfall.EnemyController>();enemy.session=s;enemy.aggro=danger=="chasing";enemy.preparing=danger=="warning";enemy.transform.position=new UnityEngine.Vector3(danger=="near"?2:40,0,0);s.Enemies.Add(enemy);var warning=enemy.warning;
   Check(!s.BeginPractice(Emberfall.CampPracticeScenario.Stationary,10),"unsafe entry refused before real enemy OnDisable");
   Check(enemy.gameObject.activeSelf&&enemy.warning==warning&&enemy.windup==2&&enemy.comboRemaining==2&&enemy.attackCooldown==.1f&&enemy.CoroutineStops==0,"refused practice preserves attack warning combo and cooldown");
   Check(s.LastNotice.Contains("交战")&&s.Player==original&&s.Progression==owner,"unsafe entry explains combat and keeps original ownership");
   // Positive lifecycle control: prove this engine boundary invokes actual production cancellation.
   enemy.gameObject.SetActive(false);Check(enemy.warning==null&&enemy.windup==0&&enemy.comboRemaining==0&&enemy.attackCooldown==.55f&&enemy.CoroutineStops==1,"real OnDisable cancellation oracle is active");s.Enemies.Remove(enemy);UnityEngine.MonoBehaviour.Destroy(enemy.gameObject);
  }
  var bolt=new UnityEngine.GameObject("in-flight bolt").AddComponent<Emberfall.CombatProjectile>();Check(!s.BeginPractice(Emberfall.CampPracticeScenario.Stationary,10)&&bolt.gameObject.activeSelf,"in-flight projectile cannot be frozen by practice");UnityEngine.MonoBehaviour.Destroy(bolt.gameObject);
  var pickup=new UnityEngine.GameObject("original ground loot").AddComponent<Emberfall.GroundLootPickup>();var loot=new Emberfall.GameSession.PendingLoot{Pickup=pickup};s.pendingLoot.Add("original-loot",loot);
  Emberfall.CampPracticeRecord fixedA=null;
  foreach(var scene in new[]{Emberfall.CampPracticeScenario.Stationary,Emberfall.CampPracticeScenario.Moving,Emberfall.CampPracticeScenario.FrontAndSupplier,Emberfall.CampPracticeScenario.GuardAndWispPressure,Emberfall.CampPracticeScenario.SupplierPressure})
  {
   Check(s.BeginPractice(scene,10,draft),"real BeginPractice accepts legal scene");Check(s.PracticeActive&&s.Player!=original&&s.Progression!=owner&&!original.gameObject.activeSelf,"real ownership handoff");Check(!s.BeginPractice(scene,10,draft),"duplicate start rejected");Check(!s.PinPracticeBaseline(),"live record cannot replace fixed baseline");Check(s.Enemies.Count==(s.PracticeRecord.HasSupplier||s.PracticeRecord.UsesEnemyAI?2:1),"scene target cardinality");
   Check(!s.TryCollectGroundLoot("original-loot")&&s.pendingLoot["original-loot"]==loot&&!pickup.Retired&&owner.Collections==0&&s.Progression.Collections==0,"practice cannot consume original world loot");
   Check(!s.PracticeRecord.Started&&s.InputBlocked&&UnityEngine.Time.timeScale==0,"prepare freezes real session before timer and attacks");UnityEngine.Time.deltaTime=4;s.TestTick();Check(s.PracticeRecord.Elapsed==0,"prepare never advances timer");Check(s.StartPractice()&&!s.StartPractice()&&!s.InputBlocked,"explicit start once unblocks simulation");
   if(s.PracticeRecord.HasSupplier)Check(s.PracticeSupportMultiplier(s.Enemies[0])==.7f&&s.PracticeSupportMultiplier(s.Enemies[1])==1,"actual support routing");
   var priorLive=s.PracticeRecord;s.RecordPracticeCast(1,0);s.RecordPracticeSkillHit(1);s.RecordPracticeEnergy(-15);Check(s.RestartPractice()&&s.PracticeRecord.ActualDamage==0&&s.PracticeRecord.EnergySpent==0,"refresh only temporary actors and new record");Check(!ReferenceEquals(priorLive,s.PreviousPracticeRecord),"baseline A is a separate frozen first result across repeated B");Check(!s.PracticeRecord.Started&&s.StartPractice(),"refresh returns to preparation");
   UnityEngine.Time.deltaTime=10;s.TestTick();Check(!s.PracticeActive&&s.Player==original&&s.Progression==owner&&s.Enemies==enemies,"timed finish restores exact references");Check(original.Health==37&&original.Energy==23&&original.Cooldown==8&&original.gameObject.activeSelf,"no original vitals or cooldown refresh");Check(s.pendingLoot["original-loot"]==loot&&pickup.gameObject.activeSelf&&!pickup.Retired&&s.collectedGroundLoot.Count==0,"exit restores original loot object and registry");Check(UnityEngine.Random.state.value==42,"random state restored");if(fixedA==null)fixedA=s.PreviousPracticeRecord;Check(fixedA!=null&&ReferenceEquals(fixedA,s.PreviousPracticeRecord)&&!ReferenceEquals(fixedA,s.PracticeRecord),"baseline A is a separate frozen first result across repeated B");s.EndPractice("duplicate");
  }
  Check(s.PinPracticeBaseline()&&!ReferenceEquals(fixedA,s.PreviousPracticeRecord)&&s.PreviousPracticeRecord.Scenario==s.PracticeRecord.Scenario,"only explicit pin replaces baseline A");
  for(int producerKind=0;producerKind<3;producerKind++){
   var live=new UnityEngine.GameObject("active sustained producer");Emberfall.IPracticeProducerProbe producer=producerKind==0?(Emberfall.IPracticeProducerProbe)live.AddComponent<Emberfall.CombatArea>():producerKind==1?live.AddComponent<Emberfall.AdvancedSkillSequence>():live.AddComponent<Emberfall.SummonerSpell>();producer.Seed();
   bool entered=s.BeginPractice(Emberfall.CampPracticeScenario.Stationary,10);if(entered)s.EndPractice("unsafe admission reproduction");
   Check(producer.Intact&&live.activeSelf,"practice must not invoke irreversible producer OnDisable on admission: "+producerKind);
   Check(!entered&&s.Player==original&&s.Progression==owner&&original.gameObject.activeSelf,"active sustained producer refuses before original ownership changes: "+producerKind);
   live.SetActive(false);Check(!producer.Intact,"actual producer OnDisable invalidates receipt or pending effect: "+producerKind);live.SetActive(true);Check(!producer.Intact,"reactivation does not reconstruct consumed producer state: "+producerKind);
   live.SetActive(false);Check(s.BeginPractice(Emberfall.CampPracticeScenario.Stationary,10),"already inactive producer does not block safe practice: "+producerKind);s.EndPractice("inactive producer retained");Check(!live.activeSelf,"practice preserves preexisting inactive producer: "+producerKind);UnityEngine.MonoBehaviour.Destroy(live);
  }
  foreach(int trialDuration in new[]{10,60})foreach(bool consumed in new[]{false,true}){
   UnityEngine.Time.time+=100;Emberfall.SummonedCompanion.OnPerfectDodge(original);if(consumed)Check(Emberfall.SummonedCompanion.TestConsume(original),"actual original bond consumes command before practice probe");float remaining=Emberfall.SummonedCompanion.CommandOpportunityRemaining(original);
   bool entered=s.BeginPractice(Emberfall.CampPracticeScenario.Stationary,trialDuration);if(entered){s.StartPractice();UnityEngine.Time.time+=trialDuration;UnityEngine.Time.deltaTime=trialDuration;s.TestTick();}
   Check(Emberfall.SummonedCompanion.CommandOpportunityRemaining(original)==remaining&&Emberfall.SummonedCompanion.TestProtection(original),"practice cannot age original absolute command/protection clocks: "+trialDuration+" consumed="+consumed);
   Check(!entered&&s.Player==original&&s.Progression==owner,"live original bond window refuses before ownership mutation");
   UnityEngine.Time.time+=100;Check(s.BeginPractice(Emberfall.CampPracticeScenario.Stationary,trialDuration),"expired original bond permits ordinary practice without restoration");s.EndPractice("expired bond probe");Check(Emberfall.SummonedCompanion.CommandOpportunityRemaining(original)==0&&!Emberfall.SummonedCompanion.TestProtection(original),"expired or consumed command is never resurrected by practice");
  }
  foreach(bool cooldown in new[]{false,true}){
   UnityEngine.Time.time+=100;var cooperationTarget=new Emberfall.EnemyController();Check(!Emberfall.SummonedCompanion.TestCooperation(original,cooperationTarget,0),"original owner first actual cooperation mark is pending");if(cooldown)Check(Emberfall.SummonedCompanion.TestCooperation(original,cooperationTarget,1),"original owner confirmed cooperation starts real cooldown");
   Check(!s.BeginPractice(Emberfall.CampPracticeScenario.Stationary,10)&&s.Player==original,"original cooperation mark or cooldown refuses practice before aging");
   if(!cooldown)Check(Emberfall.SummonedCompanion.TestCooperation(original,cooperationTarget,1),"entry query did not consume or erase original cooperation mark");
   UnityEngine.Time.time+=4;Check(!Emberfall.SummonedCompanion.HasPracticeTimedState(original)&&s.BeginPractice(Emberfall.CampPracticeScenario.Stationary,10),"expired cooperation permits practice without extending cooldown");s.EndPractice("cooperation probe");UnityEngine.MonoBehaviour.Destroy(cooperationTarget.warning);
  }
  Check(s.BeginPractice(Emberfall.CampPracticeScenario.SupplierPressure,60),"pressure prepare for real telemetry");
  s.RecordIncomingDamage("prepare",20);s.Player.Health=30;s.Player.Heal(20);Check(s.PracticeRecord.DamageTaken==0&&s.PracticeRecord.EffectiveHealing==0,"prepare ignores event telemetry");
  Check(s.StartPractice(),"pressure actual start");Check(s.Enemies.TrueForAll(e=>e.aggro),"pressure start provokes actual enemy ownership");
  s.RecordIncomingDamage("guard",12);s.RecordIncomingDamage("invalid",float.NaN);s.Player.Health=90;s.Player.Heal(500);s.Player.Heal(1);
  Check(s.PracticeRecord.DamageTaken==12&&s.PracticeRecord.EffectiveHealing==10&&s.RealTelemetryIntact,"actual clipped Heal and damage telemetry stay out of original run");
  var front=s.Enemies[0];var supplier=s.Enemies[1];s.OnEnemyInterrupted(front);Check(s.PracticeRecord.Mechanisms["打断"]==1&&s.RealTelemetryIntact,"actual interruption telemetry stays inside practice");UnityEngine.Time.deltaTime=2;s.TestTick();s.TestKill(supplier);s.TestKill(supplier);
  Check(s.PracticeRecord.KillOrder.Count==1&&s.PracticeRecord.SupplyBrokenAt==2&&s.PracticeRecord.Mechanisms["断供"]==1&&s.PracticeSupportMultiplier(front)==1,"actual kill removes supply once and records order");
  s.TestKill(front);Check(s.PracticeActive&&s.PracticeRecord.ObjectiveCompleted&&s.PracticeRecord.KillOrder.Count==2,"last kill finishes record before deferred owner restoration");s.TestTick();Check(!s.PracticeActive&&s.RealTelemetryIntact&&s.Player==original,"objective exit restores owner without original telemetry changes");
  Check(s.BeginPractice(Emberfall.CampPracticeScenario.GuardAndWispPressure,10)&&s.StartPractice(),"pressure before death");s.RecordIncomingDamage("lethal",37);s.TestDeath();Check(s.PracticeActive&&!s.PracticeRecord.Survived&&s.Player!=original,"death callback retains temporary ownership until host tick");s.TestTick();Check(!s.PracticeActive&&!s.PracticeRecord.Survived&&s.Player==original&&s.RealTelemetryIntact,"actual practice death restores original and survival evidence");
  Check(s.BeginPractice(Emberfall.CampPracticeScenario.Stationary,10),"practice before OS close");var temporary=s.Progression;int saves=owner.Saves;Check(s.TestQuit()&&!s.PracticeActive&&s.Player==original&&s.Progression==owner&&owner.Saves==saves+1&&temporary.Saves==0,"actual CanQuitSafely restores owner then saves once");s.TestQuitEvent();Check(owner.Saves==saves+1,"accepted OS close event does not save again");
  Check(s.BeginPractice(Emberfall.CampPracticeScenario.Stationary,10),"practice before failed OS close");owner.SaveFails=true;Check(!s.TestQuit()&&!s.PracticeActive&&s.Progression==owner&&s.LastNotice=="injected save failure","actual OS close retains original save failure protection");owner.SaveFails=false;Check(s.TestQuit(),"later OS close retries restored real owner");
  Check(s.BeginPractice(Emberfall.CampPracticeScenario.Stationary,10),"practice before background");saves=owner.Saves;s.TestPause(true);Check(!s.PracticeActive&&s.IsBackground&&UnityEngine.Time.timeScale==0&&s.Progression==owner&&owner.Saves==saves+1,"actual mobile pause ends practice and saves real owner");s.TestFocus(false);Check(owner.Saves==saves+1,"paired background focus event deduplicates save");s.TestPause(false);s.TestFocus(true);Check(!s.IsBackground,"foreground restores normal pause state");
  Check(s.BeginPractice(Emberfall.CampPracticeScenario.Stationary,10),"practice before teardown");Emberfall.GameSession.Instance=s;s.TestDestroy();Check(!s.PracticeActive&&s.Player==original&&s.Progression==owner&&original.gameObject.activeSelf&&Emberfall.GameSession.Instance==null,"actual OnDestroy restores owner before session release");
  Check(s.BeginPractice(Emberfall.CampPracticeScenario.GuardAndWispPressure,10),"practice before failed exit root snapshot");var abandonedPlayer=s.Player;var abandonedEnemies=s.Enemies.ToArray();UnityEngine.Scene.FailSnapshot=true;bool exitFailed=false;try{s.EndPractice("snapshot failure exit");}catch(Exception){exitFailed=true;}finally{UnityEngine.Scene.FailSnapshot=false;}
  Check(exitFailed&&!s.PracticeActive&&s.Player==original&&s.Progression==owner,"failed exit snapshot restores formal ownership and reports failure");
  Check(!abandonedPlayer.gameObject.activeSelf&&abandonedEnemies.All(e=>!e.gameObject.activeSelf),"failed exit snapshot cannot leave temporary combat actors active beside formal owner");
  int savesBeforeExit=owner.Saves;Check(s.BeginPractice(Emberfall.CampPracticeScenario.Stationary,10),"prepare before failed save-exit snapshot");abandonedPlayer=s.Player;UnityEngine.Scene.FailSnapshot=true;try{Check(!s.TestQuit(),"failed practice cleanup vetoes actual OS close");}finally{UnityEngine.Scene.FailSnapshot=false;}
  Check(owner.Saves==savesBeforeExit&&s.Player==original&&s.Progression==owner&&!abandonedPlayer.gameObject.activeSelf,"cleanup failure cannot save temporary or formal profile before retry");Check(s.TestQuit()&&owner.Saves==savesBeforeExit+1,"clean retry saves restored formal owner exactly once");
  Check(s.BeginPractice(Emberfall.CampPracticeScenario.GuardAndWispPressure,10),"prepare before actor cleanup fault");abandonedPlayer=s.Player;abandonedEnemies=s.Enemies.ToArray();UnityEngine.GameObject.FailDisableName=abandonedPlayer.gameObject.name;exitFailed=false;try{s.EndPractice("actor cleanup fault");}catch(Exception){exitFailed=true;}
  Check(exitFailed&&!s.PracticeActive&&s.Player==original&&original.gameObject.activeSelf&&!abandonedPlayer.gameObject.activeSelf&&abandonedEnemies.All(e=>!e.gameObject.activeSelf),"one root cleanup failure cannot leave later temporary actors active or destroy original");s.EndPractice("duplicate failed exit");Check(s.Player==original&&original.gameObject.activeSelf&&s.RealTelemetryIntact,"duplicate failed exit is harmless to original state");
  Check(s.BeginPractice(Emberfall.CampPracticeScenario.GuardAndWispPressure,10),"prepare before fallback actor fault");abandonedPlayer=s.Player;abandonedEnemies=s.Enemies.ToArray();UnityEngine.Scene.FailSnapshot=true;UnityEngine.GameObject.FailDisableName=abandonedPlayer.gameObject.name;try{s.EndPractice("fallback actor fault");}catch(Exception){}finally{UnityEngine.Scene.FailSnapshot=false;}
  Check(!s.PracticeActive&&s.Player==original&&original.gameObject.activeSelf&&!UnityEngine.GameObject.Roots.Contains(abandonedPlayer.gameObject)&&abandonedEnemies.All(e=>!e.gameObject.activeSelf),"fallback actor exception cannot skip its destroy or remaining actor cleanup");
  int roots=UnityEngine.GameObject.Roots.Count;UnityEngine.Scene.FailSnapshot=true;Check(!s.BeginPractice(Emberfall.CampPracticeScenario.Stationary,10)&&!s.PracticeActive&&s.Player==original&&UnityEngine.GameObject.Roots.Count==roots&&original.gameObject.activeSelf,"snapshot failure cannot retire original roots");UnityEngine.Scene.FailSnapshot=false;
  Emberfall.PlayerController.ThrowOnInitialize=true;Check(!s.BeginPractice(Emberfall.CampPracticeScenario.Moving,10,draft)&&s.Player==original&&s.Progression==owner&&!s.PracticeActive&&original.gameObject.activeSelf,"injected creation failure rolls ownership back");Emberfall.PlayerController.ThrowOnInitialize=false;
  draft.valid=false;Check(!s.BeginPractice(Emberfall.CampPracticeScenario.Moving,10,draft)&&s.Player==original,"stale draft cannot start");
  Check(s.BeginPractice(Emberfall.CampPracticeScenario.Moving,60),"current build sixty second scenario");s.EndPractice("death");Check(s.Player==original&&s.PracticeRecord.EndReason=="death","early exit restores original");
  return "PASS "+n+" actual GameSession.Practice lifecycle assertions (Unity boundary doubles, no engine execution)";
 }
}
