// Actual room host/state/traversal; scene, save IO and unrelated systems are explicit doubles.
using System;using System.Collections.Generic;using UnityEngine;using Emberfall;
namespace UnityEngine {
 public static class Debug {public static Exception LastException;public static string LastError;public static void LogException(Exception error){LastException=error;}public static void LogError(object message){LastError=message.ToString();}}
 public class Object {public static void Destroy(Object o){if(o is GameObject g)g.SetActive(false);}}
 public class MonoBehaviour:Object {}
 public class GameObject:Object {public string name;public bool activeInHierarchy=true;public Transform transform;public GameObject(string n=""){name=n;transform=new Transform{gameObject=this};}public void SetActive(bool on){activeInHierarchy=on;}}
 public class Transform {public Vector3 position;public GameObject gameObject;public Transform parent;public void SetParent(Transform t,bool world){parent=t;}}
 public struct Color {public Color(float r,float g,float b){}}
 public class Camera {public static Camera main=new Camera();public T GetComponent<T>()where T:new()=>new T();}
}
namespace Emberfall {
 public enum EnemyKind{Wisp,Guardian,Goblin,Slime}
 public class EnemyController {public bool IsDead,IsBoss;public EnemyKind Kind;public GameObject gameObject=new GameObject();public Transform transform=>gameObject.transform;public bool isActiveAndEnabled=>gameObject.activeInHierarchy;public float NavigationRadius=>Kind==EnemyKind.Guardian?.65f:.5f;public void ConfigureEscapePost(EscapeRole role,Vector3 point){}}
 public class PlayerController {public int CombatEpoch=1;public EnemyController AimTarget;public GameObject gameObject=new GameObject();public Transform transform=>gameObject.transform;public void Teleport(Vector3 p){transform.position=p;}public void RetireCombatForWorldTransition(){CombatEpoch++;}public float MaxHealth=100;public void Heal(float n){}}
 public class SaveDouble {public object Profile=new object();public int HighestAdventureTier=1,Saves;public bool Fail;public string LastError;public void Save(){Saves++;LastError=Fail?"injected write failure":null;}}
 public class ChoiceDouble {public int CompletedWave=1;public bool AwaitingChoice;public void PrepareRoomChoice(int wave,object p,bool mobile,int seed){CompletedWave=wave;AwaitingChoice=true;}}
 public static class MobileControls {public static bool Active;}
 public class AdventureCamera {public void Snap(){}}
 public static class TacticalCaptureVisual {public static List<GameObject> Rings=new List<GameObject>();public static void Attach(GameObject o,GameSession s){}public static void AttachRoomSeal(GameObject o,GameSession s,int index){Rings.Add(o);}}
 public static class TacticalEnemyVisual {public static void Attach(EnemyController e,GameSession s){}}
 public static class LargeExpeditionBoss {public static void Configure(EnemyController e,int tier,int seed){}}
 public static class WorldBuilder {
  public static GameObject MakeLootBeacon(Vector3 p,Color c)=>new GameObject();
  public static GameObject MakeRoomObjective(Vector3 p,bool indexed=false){var o=new GameObject();o.transform.position=p;return o;}
  public static GameObject Build(ZoneKind kind,int layout,int tier){WorldTraversal.Reset(kind);if(layout>=20&&layout<=25){for(int side=-1;side<=1;side+=2){WorldTraversal.AddBox(new Vector3(side*16,0,0),new Vector2(1.1f,29));WorldTraversal.AddBox(new Vector3(side*3.4f,0,15),new Vector2(1.4f,1.4f));}TacticalRoomGeometry.Register(layout);for(int side=-1;side<=1;side+=2){var rubble=new Vector3(side*13.3f,0,2.2f);if(WorldTraversal.IsWalkable(rubble,.7f))WorldTraversal.AddDynamicCircle(rubble,.7f);}}return new GameObject("world");}
 }
 public class ChapterDouble {public bool DoorUnlocked;public RoomObjective Objective;public int Seals;public float Progress;}
 public sealed partial class GameSession:MonoBehaviour {
  public bool PracticeActive=>false;public void EndPractice(string reason)=>throw new System.InvalidOperationException("Room seal fixture must not enter practice exit");public float PracticeSupportMultiplier(EnemyController e)=>throw new System.InvalidOperationException("Room seal fixture must not enter practice support");
  public RoomChainState RoomChainRun;public PlayerController Player=new PlayerController();public List<EnemyController> Enemies=new List<EnemyController>();
  public SaveDouble Progression=new SaveDouble();public ChoiceDouble RunChoices=new ChoiceDouble();
  public bool Paused,BackgroundPaused,IsDead,HasStarted=true,InDungeon=true;public bool InputBlocked=>Paused||BackgroundPaused||IsDead;
  public bool ChapterActive=false,ChapterFinished=false;public ChapterDouble ChapterRun=new ChapterDouble();GameObject chapterObjective;
  public bool IsChapterContesting(EnemyController e)=>false;
  private GameObject world=new GameObject(),roomExitMarker;private int runSeed,wavePopulation;public int DungeonTier=1,DungeonWave,DungeonLayout,DungeonEntryLevel=1;
  private bool objectiveHealedThisWave,changingZone;private List<GameObject> transientObjects=new List<GameObject>();
  private class RoomEnemyReceipt {public RoomChainPlan Plan;public int Index;}
  private Dictionary<EnemyController,RoomEnemyReceipt> roomEnemies=new Dictionary<EnemyController,RoomEnemyReceipt>();
  public bool DungeonRewardPending=false,ModeRewardPending=false;bool TrySettleSideEventRewards()=>true;bool TrySettleDungeonReward()=>true;bool TrySettleArenaReward()=>true;bool PreserveWorldLoot()=>true;
  public string LastNotice;void Notify(string s){LastNotice=s;}void LogSystem(string s){}void UpdateTimeScale(){}void SuspendInputs(){}void AbandonSideEvent(){}void RetireWorldLootReceipts(int e){}void BuildSideEvent(){}void FinalizeRoomChain(){}
  bool TrySafeSpawn(Vector3 desired,float radius,float safe,out Vector3 p){p=desired;return true;}
  void SpawnEnemy(EnemyKind kind,int level,Vector3 p,bool boss){var e=new EnemyController{Kind=kind,IsBoss=boss};e.transform.position=p;Enemies.Add(e);}
  public GameObject[] Markers=>roomSealMarkers;public bool WorldAlive=>world.activeInHierarchy;
  public GameSession(int seed){runSeed=seed;RoomChainRun=new RoomChainState(seed);world=WorldBuilder.Build(ZoneKind.Dungeon,RoomChainRun.Room.Layout,1);Player.Teleport(TacticalRoomGeometry.Entrance);BeginRoomChainScene();}
  public void Tick(){Time.frameCount++;TickRoomTactics();}
  public void Kill(EnemyController e){e.IsDead=true;RecordRoomDefeat(e);}
 }
}
public static class RoomFreeSealHostTests {
 static int checks;static void Check(bool b,string message){checks++;if(!b)throw new Exception(message);}
 static bool Same(Vector3 a,Vector3 b)=>(a-b).sqrMagnitude<.00001f;
 static void ClearPressure(GameSession s){foreach(var enemy in s.Enemies)enemy.transform.position=new Vector3(0,0,-10);}
 static void Tick(GameSession s,int count){for(int i=0;i<count;i++)s.Tick();}
 public static string Run(){
  foreach(int seed in new[]{0,3,6,9,12,15})foreach(int first in new[]{1,0}){
   var s=new GameSession(seed);var run=s.RoomChainRun;var old=run.Room;int mirror=RoomTactics.Mirror(seed);var a=new Vector3(-mirror*8,0,-6);var b=new Vector3(mirror*8,0,9);
   Check(!run.Finished&&s.Enemies.Count==6,"real room host registers exactly six seeded enemies");
   Check(s.Markers[0]!=null&&s.Markers[1]!=null&&Same(s.Markers[0].transform.position,a)&&Same(s.Markers[1].transform.position,b),"both physical markers preserve exact original coordinates and stable identities");
   foreach(float radius in new[]{.45f,.65f,1.3f})Check(WorldTraversal.CanReach(a,b,radius)&&WorldTraversal.CanReach(b,a,radius),"all six layouts support either seal order with actual traversal");
   ClearPressure(s);var firstPoint=first==0?a:b;var other=first==0?b:a;
   s.Player.Teleport(firstPoint);Tick(s,4);Check(run.SealProgress(first)==1&&run.SealProgress(1-first)==0,"live host permits B-first and A-first independently");
   s.Player.Teleport(other);Tick(s,2);Check(run.CaptureFraction==.25f,"aggregate includes partial progress at both physical seals");
   s.Player.Teleport(firstPoint);s.Enemies[0].transform.position=other;Tick(s,1);Check(run.SealProgress(first)==1.25f&&!s.RoomCaptureContested,"other seal pressure cannot pause occupied seal");
   s.Enemies[0].transform.position=firstPoint;Tick(s,1);Check(run.SealProgress(first)==1.25f&&s.RoomSealView(first).Contested,"local live enemy pauses its own seal");
   s.Enemies[0].transform.position=firstPoint+new Vector3(2,0,0);var wall=WorldTraversal.AddDynamicBox(firstPoint+new Vector3(1,0,0),new Vector2(.4f,2));Tick(s,1);Check(run.SealProgress(first)==1.5f&&!s.RoomSealView(first).Contested,"actual LOS blocker severs local contest");WorldTraversal.RemoveDynamicObstacle(wall);
   ClearPressure(s);s.Paused=true;Tick(s,2);s.Paused=false;s.BackgroundPaused=true;Tick(s,2);s.BackgroundPaused=false;s.Player.Teleport(new Vector3(0,0,-12));Tick(s,2);Check(run.SealProgress(first)==1.5f&&run.SealProgress(1-first)==.5f,"pause background and leaving preserve both partial values");
   s.Player.Teleport(firstPoint);Tick(s,6);Check(run.SealComplete(first)&&!run.DoorUnlocked&&run.Seals==1,"one complete physical seal never opens gate");Check(s.RoomSealView(first).Complete&&Same(s.RoomNextObjectivePoint,other),"completed identity remains stable and arrow targets remaining ring");
   Tick(s,3);Check(run.Seals==1&&run.SealProgress(1-first)==.5f,"staying at completed seal cannot advance other seal");
   s.Player.Teleport(other);Tick(s,10);Check(run.Seals==2&&run.DoorUnlocked&&run.CaptureFraction==1,"both independent seals open door with six enemies alive");
   Check(s.Markers[0].activeInHierarchy&&s.Markers[1].activeInHierarchy&&s.RoomSealView(0).Complete&&s.RoomSealView(1).Complete,"open door retains both completed physical records");
   s.Player.Teleport(new Vector3(0,0,14));int epoch=s.Player.CombatEpoch;s.Progression.Fail=true;Check(!s.EnterNextRoom()&&ReferenceEquals(old,run.Room)&&s.Player.CombatEpoch==epoch&&s.WorldAlive&&run.SealComplete(0)&&run.SealComplete(1),"actual SaveBeforeLeaving failure preserves room epoch and both completed seals");
   s.Progression.Fail=false;Check(s.EnterNextRoom()&&s.Player.CombatEpoch==epoch+1&&!ReferenceEquals(old,run.Room)&&run.SealProgress(0)==0&&run.SealProgress(1)==0,"actual room transition resets both progress and retires epoch");Check(s.RoomSealView(0)==null&&!run.Defeat(old,0),"new nonpurify room rejects seal snapshot and old enemy callback");
  }
  var failed=new GameSession(0);ClearPressure(failed);failed.Player.Teleport(failed.Markers[1].transform.position);Tick(failed,2);failed.RoomChainRun.Fail(RoomFailureReason.Death);Tick(failed,5);failed.RoomChainRun.AdvanceSeal(1,1,true,true,false);Check(failed.RoomChainRun.SealProgress(1)==.5f&&failed.RoomSealView(1)==null&&!failed.RoomChainRun.DoorUnlocked,"terminal run refuses capture and removes live snapshots");
  return "PASS: "+checks+" actual free-seal host/state/navigation/save-boundary assertions; managed scene/save doubles, not Unity gameplay";
 }
}
