using System;using System.Collections.Generic;using System.IO;using System.Linq;using Emberfall;using UnityEngine;
namespace UnityEngine
{
 public static class Random{public struct State{public uint Value;}static State current=new State{Value=1};public static int Draws;public static State state{get=>current;set=>current=value;}public static float value{get{Draws++;current.Value=unchecked(current.Value*1664525u+1013904223u);return current.Value/(float)uint.MaxValue;}}}
 public static class Time{public static float time;public static int frameCount;}
 public struct Quaternion{public static Quaternion identity=>new Quaternion();}
 public struct Vector3{public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}public float sqrMagnitude=>x*x+y*y+z*z;public static Vector3 forward=>new Vector3(0,0,1);public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);public static Vector3 operator *(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);}
 public static class Mathf{public static float Max(float a,float b)=>Math.Max(a,b);public static float Min(float a,float b)=>Math.Min(a,b);public static float Clamp(float x,float a,float b)=>Math.Max(a,Math.Min(b,x));public static float Clamp01(float x)=>Clamp(x,0,1);}
 public sealed class Transform{public Vector3 position;public Quaternion rotation;public Vector3 forward=>Vector3.forward;public GameObject gameObject;}
 public class Component{public GameObject gameObject;public Transform transform=>gameObject.transform;public T GetComponent<T>() where T:class=>gameObject.GetComponent<T>();}
 public class MonoBehaviour:Component{protected static void Destroy(GameObject o){if(o!=null){o.destroyed=true;o.activeSelf=false;}}}
 public struct Scene{public GameObject[] GetRootGameObjects()=>GameObject.All.Where(x=>!x.destroyed).ToArray();}
 public sealed class GameObject
 {
  public static List<GameObject> All=new List<GameObject>();public readonly Dictionary<Type,object> Components=new Dictionary<Type,object>();public string name;public bool activeSelf=true,destroyed;public bool activeInHierarchy=>activeSelf&&!destroyed;public Transform transform=new Transform();public Scene scene=>new Scene();
  public GameObject(string name=""){this.name=name;transform.gameObject=this;All.Add(this);}public void SetActive(bool active){activeSelf=active;}
  public T AddComponent<T>() where T:Component,new(){var t=new T{gameObject=this};Components[typeof(T)]=t;return t;}
  public T GetComponent<T>() where T:class{object c;return Components.TryGetValue(typeof(T),out c)?c as T:null;}
  public T GetComponentInChildren<T>() where T:class=>GetComponent<T>();
 }
}
namespace Emberfall
{
 public sealed class CombatProjectile:Component{}public sealed class CombatArea:Component{}public sealed class AdvancedSkillSequence:Component{}public sealed class SummonerSpell:Component{}
 public sealed class EnemyController:Component{public bool IsDead,IsAggro,IsPreparingAttack;}
 public sealed class SkillTargetingController:Component{public bool IsTargeting;public void Initialize(PlayerController p,GameSession s){}public void Cancel(){IsTargeting=false;}}
 public sealed class SkillChargeController:Component{public bool IsCharging;public void Initialize(PlayerController p,GameSession s){}public void Cancel(){IsCharging=false;}}
 public sealed class CombatModel:Component
 {
  public static bool FailNext;public static Action Preparing;
  public static CombatModel Hero(Transform t,HeroClass h){float consumed=UnityEngine.Random.value;if(FailNext){FailNext=false;throw new Exception("fixture visual preparation failure");}var callback=Preparing;Preparing=null;callback?.Invoke();return new GameObject("model").AddComponent<CombatModel>();}
  public void ApplyFashion(FashionData a,FashionData b){}public void ApplyEquipment(ItemData a,ItemData b,ItemData c){}public void SetBlenderPilotOwnerAlive(bool value){}
 }
 public static class SummonedCompanion
 {
  public static HashSet<PlayerController> Timed=new HashSet<PlayerController>(),Partners=new HashSet<PlayerController>();public static bool HasPracticeTimedState(PlayerController p)=>Timed.Contains(p);
  public static void RetireOwner(PlayerController p){Partners.Remove(p);}public static void RefreshBuild(PlayerController p){}
 }
 public static class MobileControls{public static int Resets;public static void ResetInput(){Resets++;}}
 public sealed class GameUI:Component{public bool ClassSwitchHasPendingEdit;public int Resets;public void OnClassSwitched(){Resets++;}}
 public sealed partial class GameSession:MonoBehaviour
 {
  public ProgressionService Progression;public PlayerController Player;public GameUI ui;public bool HasStarted=true,IsInCamp=true,InDungeon,PracticeActive,Paused,BackgroundPaused;public bool IsDead=>Player==null||Player.IsDead;public List<EnemyController> Enemies=new List<EnemyController>();public void Notify(string s){}public void RecordPracticeEnergy(float delta){}
  public void Observe(){Progression.Changed+=()=>{if(Player.HeroClass!=Progression.Profile.heroClass)throw new Exception("mixed owner publication");Publications++;Player.RefreshStats(false);};}
  public int Publications;public void AssertArchivesClear(){if(classRuntimeArchives.Any(x=>x!=null))throw new Exception("foreign character runtime leaked");}
 }
 public sealed partial class PlayerController:MonoBehaviour
 {
  public HeroClass HeroClass{get;private set;}public float Health{get;private set;}public float MaxHealth{get;private set;}public float Energy=>skillRuntime.Energy;public bool IsDead=>Health<=0;internal int CombatEpoch{get;private set;}
  GameSession session;GameUI inputUI;StatBlock stats;CombatModel model;SkillTargetingController targeting;SkillChargeController charge;SkillRuntime skillRuntime;
  readonly MasteryCoreRuntime masteryCore=new MasteryCoreRuntime();readonly CombatProcCooldown returningBladeProc=new CombatProcCooldown(),venomSpreadProc=new CombatProcCooldown(),openingFrostProc=new CombatProcCooldown();
  bool jumping,executingChargedSkill,suppressBasicUntilReleased;float starterRetry,attackAnimation,hurtTimer,invulnerability,guardTime,healingProtectionTime,mobilityTime,slowTime,movementSkillLock,passiveTime,perfectDodgeCounterTime,blinkBufferTime,perfectDodgeWindow,counterTime,dodgeShockTime,chargedWardTime,pursuitTime,classDodgeTime,burnStrideTime,coreWardTime,focusTime,dodgeCooldown,attackCooldown,passiveCooldown;
  SkillBasicRecoveryClock skillBasicRecovery;Vector3 aimPoint;int nextCastId;EnemyController AimTarget,focusedEnemy;readonly Dictionary<int,int> aimGeometry=new Dictionary<int,int>();
  void ClearMobilePinnedTarget(){}void CancelCombatPose(){}void CancelTransientInput(){suppressBasicUntilReleased=true;}
  public void SetVitals(float fraction){Health=MaxHealth*fraction;}public void Consume(){skillRuntime.TryConsume(0,1);dodgeCooldown=4;passiveCooldown=7;returningBladeProc.TryTrigger(9);}
  public float StarterRetry=>starterRetry;public MasteryResourceProc CoreSpent(float energy)=>masteryCore.SkillSpent(energy);
  public float Remaining(int i)=>skillRuntime.Remaining(i);public float Dodge=>dodgeCooldown;public float Proc=>returningBladeProc.Remaining;public int NewReceipt()=>IssueCastId();public bool HasReceipt(int id)=>CaptureCastReceipt(id)!=null;
  public void Advance(float dt){starterRetry=Math.Max(0,starterRetry-dt);skillRuntime.Advance(dt);masteryCore.Advance(dt);dodgeCooldown=Math.Max(0,dodgeCooldown-dt);passiveCooldown=Math.Max(0,passiveCooldown-dt);returningBladeProc.Advance(dt);}
  public void Transient(string field,float value){GetType().GetField(field,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(this,value);}
 }
}
public static class ClassSwitchPersistenceFaults{public static bool Armed;public static int Attempts;}
public static class ClassSwitchRuntimeTest
{
 static int n;static void Check(bool ok,string s){n++;if(!ok)throw new Exception(s);}
 static GameSession New(string root,HeroClass hero)
 {
  GameObject.All.Clear();SummonedCompanion.Partners.Clear();SummonedCompanion.Timed.Clear();Time.frameCount++;Time.time=0;
  var p=new ProgressionService(Path.Combine(root,Guid.NewGuid().ToString("N")));p.NewGame(hero);p.Profile.level=45;p.Profile.skillRanks[0]=3;p.Save();
  var go=new GameObject("session");var s=go.AddComponent<GameSession>();s.Progression=p;s.ui=go.AddComponent<GameUI>();var h=new GameObject("hero").AddComponent<PlayerController>();s.Player=h;h.Initialize(s,hero);h.SetVitals(.37f);h.Consume();s.Observe();return s;
 }
 static string OpenChest(ProgressionService p)
 {var direct=typeof(ProgressionService).GetMethod("OpenDungeonChest",Type.EmptyTypes);return direct!=null?(string)direct.Invoke(p,null):p.OpenDungeonChest(0);}
 static void Main(string[] args)
 {
  Directory.CreateDirectory(args[0]);
  foreach(HeroClass from in Enum.GetValues(typeof(HeroClass)))foreach(HeroClass to in Enum.GetValues(typeof(HeroClass)))
  {
   if(from==to)continue;var s=New(args[0],from);var old=s.Player;float energy=old.Energy,cd=old.Remaining(0),fraction=old.Health/old.MaxHealth;int receipt=old.NewReceipt();SummonedCompanion.Partners.Add(old);
   var randomBefore=UnityEngine.Random.state;int draws=UnityEngine.Random.Draws;
   Check(s.TrySwitchClass(to),"actual session accepts safe class switch");var current=s.Player;
   Check(UnityEngine.Random.Draws>draws&&!UnityEngine.Random.state.Equals(randomBefore),"successful staged model keeps random consumption");
   Check(current!=old&&current.HeroClass==to&&s.Progression.Profile.heroClass==to&&s.Publications==1,"publication installs consistent owner exactly once");
   Check(Math.Abs(current.Health/current.MaxHealth-fraction)<.00001f&&current.Energy==energy&&current.Remaining(0)>=cd&&current.Dodge==4&&current.Proc==9,"actual player install preserves health ratio energy and cooldowns");
   Check(!old.gameObject.activeInHierarchy&&!SummonedCompanion.Partners.Contains(old)&&!old.HasReceipt(receipt),"old owner partner and cast receipt retired");
   Check(current.gameObject.activeInHierarchy&&s.ui.Resets==1,"prepared owner and UI activated after commit");
   Check(!s.TrySwitchClass(from)&&s.Player==current,"same-frame double invocation rejected");
   Time.frameCount++;Time.time+=1;current.Advance(1);float now=current.Energy;Check(s.TrySwitchClass(from),"return to archived class");
   Check(s.Player.Energy==now&&s.Player.Remaining(0)>=Math.Max(0,cd-1)&&s.Player.Proc>=8,"away cooldowns advance without reset and current energy wins");
  }
  {
   var s=New(args[0],HeroClass.Summoner);var old=s.Player;var profile=s.Progression.Profile;SummonedCompanion.Partners.Add(old);int receipt=old.NewReceipt();
   string json=JsonUtility.ToJson(profile,true),disk=File.ReadAllText(s.Progression.SaveFilePath);float hp=old.Health,energy=old.Energy,cd=old.Remaining(0);
   var rng=UnityEngine.Random.state;int draws=UnityEngine.Random.Draws;
   Directory.CreateDirectory(s.Progression.SaveFilePath+".tmp");Check(!s.TrySwitchClass(HeroClass.Ranger),"actual session rejects failed storage");Directory.Delete(s.Progression.SaveFilePath+".tmp");
   Check(UnityEngine.Random.Draws>draws&&UnityEngine.Random.state.Equals(rng),"failed staged model restores consumed combat RNG");
   Check(s.Player==old&&ReferenceEquals(profile,s.Progression.Profile)&&old.gameObject.activeInHierarchy&&SummonedCompanion.Partners.Contains(old)&&old.HasReceipt(receipt),"storage failure preserves original owner partners and receipts");
   Check(old.Health==hp&&old.Energy==energy&&old.Remaining(0)==cd&&s.Publications==0&&s.ui.Resets==0&&JsonUtility.ToJson(profile,true)==json&&File.ReadAllText(s.Progression.SaveFilePath)==disk,"storage failure has no runtime publication or data mutation");
   rng=UnityEngine.Random.state;draws=UnityEngine.Random.Draws;Time.frameCount++;CombatModel.FailNext=true;Check(!s.TrySwitchClass(HeroClass.Ranger)&&s.Player==old&&s.Publications==0,"visual preparation failure leaves original owner intact");
   Check(UnityEngine.Random.Draws>draws&&UnityEngine.Random.state.Equals(rng),"throwing staged model restores consumed combat RNG");
   Time.frameCount++;Check(s.TrySwitchClass(HeroClass.Ranger),"retry succeeds once after failure");
  }
  {
   var s=New(args[0],HeroClass.Summoner);var p=s.Progression;var old=s.Player;SummonedCompanion.Partners.Add(old);int receipt=old.NewReceipt();
   Check(p.PrepareDungeonChest(),"second-write fixture has real pending qualification");Directory.CreateDirectory(p.SaveFilePath+".tmp");Check(OpenChest(p)==null,"second-write fixture freezes actual failed draw");Directory.Delete(p.SaveFilePath+".tmp");
   var field=typeof(ProgressionService).GetField("pendingChestRoll",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);var frozen=(ChestReward)field.GetValue(p);Check(frozen!=null,"actual draw exists before target-write failure");
   p.Profile.gold+=7;var profile=p.Profile;string json=JsonUtility.ToJson(profile,true),disk=File.ReadAllText(p.SaveFilePath);float hp=old.Health,energy=old.Energy,cd=old.Remaining(0);var rng=UnityEngine.Random.state;int draws=UnityEngine.Random.Draws;
   int publications=s.Publications,uiResets=s.ui.Resets;ClassSwitchPersistenceFaults.Attempts=0;ClassSwitchPersistenceFaults.Armed=true;bool switched=s.TrySwitchClass(HeroClass.Ranger);ClassSwitchPersistenceFaults.Armed=false;
   Check(!switched&&ClassSwitchPersistenceFaults.Attempts==2,"first current-profile save succeeds before injected second target save failure");
   Check(s.Player==old&&ReferenceEquals(p.Profile,profile)&&old.gameObject.activeInHierarchy&&SummonedCompanion.Partners.Contains(old)&&old.HasReceipt(receipt),"target-write failure retains profile owner partner and cast receipts");
   Check(old.Health==hp&&old.Energy==energy&&old.Remaining(0)==cd&&s.Publications==publications&&s.ui.Resets==uiResets&&JsonUtility.ToJson(p.Profile,true)==json,"target-write failure publishes no runtime or profile changes");
   Check(ReferenceEquals(frozen,field.GetValue(p))&&p.Profile.pendingFashionChest&&!p.Profile.pendingChestReveal,"target-write failure retains complete frozen draw identity");
   Check(UnityEngine.Random.Draws>draws&&UnityEngine.Random.state.Equals(rng),"target-write failure restores consumed combat RNG");
   var reload=new ProgressionService(p.SaveDirectory);Check(reload.Load()&&reload.Profile.heroClass==HeroClass.Summoner&&reload.Profile.gold==profile.gold&&reload.Profile.pendingFashionChest&&File.ReadAllText(p.SaveFilePath)!=disk,"successful first write durably retains current configuration");
   Check(OpenChest(p)!=null&&p.LastChestReward.id==frozen.id,"frozen draw still commits exact original identity after second-write failure");
  }
  {
   var s=New(args[0],HeroClass.Summoner);s.Player.Transient("starterRetry",12);
   Check(s.TrySwitchClass(HeroClass.Ranger),"leave summoner with partner respawn timer");Time.frameCount++;Time.time+=3;s.Player.Advance(3);
   Check(s.TrySwitchClass(HeroClass.Summoner)&&s.Player.StarterRetry==9,"summoner respawn timer advances while away and is never refreshed");
  }
  {
   var s=New(args[0],HeroClass.Vanguard);s.Progression.Profile.masteryRanks[3]=10;s.Progression.Profile.masteryCore=3;s.Progression.Save();s.Player.RefreshStats(false);
   Check(s.Player.CoreSpent(40).Energy==0,"partial technique charge established");
   Check(s.TrySwitchClass(HeroClass.Ranger)&&s.Player.CoreSpent(20).Energy==0,"first target does not duplicate technique charge");Time.frameCount++;Time.time+=1;s.Player.Advance(1);
   Check(s.TrySwitchClass(HeroClass.Vanguard)&&s.Player.CoreSpent(20).Energy==8,"returning class retains its own technique charge");Time.frameCount++;Time.time+=1;s.Player.Advance(1);
   Check(s.TrySwitchClass(HeroClass.Ranger)&&s.Player.CoreSpent(60).Energy==0,"same-core cooldown floor prevents switching for an immediate proc");
  }
  foreach(string guard in new[]{"camp","dungeon","practice","paused","background","draft","dead","enemy","aggro","telegraph","partner","projectile","area","sequence","spell","changed-during-prepare"})
  {
   var s=New(args[0],HeroClass.Vanguard);var old=s.Player;var profile=s.Progression.Profile;string disk=File.ReadAllText(s.Progression.SaveFilePath);
   if(guard=="camp")s.IsInCamp=false;if(guard=="dungeon")s.InDungeon=true;if(guard=="practice")s.PracticeActive=true;if(guard=="paused")s.Paused=true;if(guard=="background")s.BackgroundPaused=true;if(guard=="draft")s.ui.ClassSwitchHasPendingEdit=true;if(guard=="dead")old.SetVitals(0);
   if(guard=="enemy"||guard=="aggro"||guard=="telegraph"||guard=="changed-during-prepare")
   {var e=new GameObject("enemy").AddComponent<EnemyController>();e.transform.position=new Vector3(guard=="enemy"?1:100,0,0);e.IsAggro=guard=="aggro";e.IsPreparingAttack=guard=="telegraph";s.Enemies.Add(e);if(guard=="changed-during-prepare")CombatModel.Preparing=()=>e.IsAggro=true;}
   if(guard=="partner")SummonedCompanion.Timed.Add(old);
   if(guard=="projectile")new GameObject().AddComponent<CombatProjectile>();if(guard=="area")new GameObject().AddComponent<CombatArea>();if(guard=="sequence")new GameObject().AddComponent<AdvancedSkillSequence>();if(guard=="spell")new GameObject().AddComponent<SummonerSpell>();
   Check(!s.TrySwitchClass(HeroClass.Arcanist)&&s.Player==old&&ReferenceEquals(profile,s.Progression.Profile)&&File.ReadAllText(s.Progression.SaveFilePath)==disk&&s.Publications==0,"actual session guard: "+guard);
  }
  foreach(string field in new[]{"guardTime","healingProtectionTime","mobilityTime","slowTime","movementSkillLock","passiveTime","perfectDodgeCounterTime","counterTime","classDodgeTime","coreWardTime","focusTime","attackAnimation","invulnerability"})
  {var s=New(args[0],HeroClass.Vanguard);s.Player.Transient(field,1);Check(!s.TrySwitchClass(HeroClass.Arcanist)&&s.Publications==0,"actual transient guard: "+field);}
  Console.WriteLine("PASS "+n+" actual session/player class switch assertions; Unity scene/model boundaries are managed substitutes");
 }
}
