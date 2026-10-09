// Actual boss channels and shutdown use hierarchy/API doubles. No native mesh,
// shader, rendering or physics execution; reward ordering is checked separately as source.
using System;using System.Collections.Generic;using System.Reflection;using Emberfall;using UnityEngine;
namespace UnityEngine {
 public class Object {public bool destroyed;public static void Destroy(Object o){o.destroyed=true;}}
 public class Component:Object {public GameObject gameObject;public Transform transform=>gameObject.transform;public T GetComponent<T>() where T:Component=>gameObject.Get<T>();}
 public class MonoBehaviour:Component {}
 public class GameObject:Object {public bool activeSelf=true;public bool activeInHierarchy=>activeSelf&&(transform.parent==null||transform.parent.gameObject.activeInHierarchy);public Transform transform;public List<Component> components=new List<Component>();public GameObject(string name){transform=new Transform{gameObject=this};}public T AddComponent<T>() where T:Component,new(){var c=new T{gameObject=this};components.Add(c);return c;}public T Get<T>()where T:Component=>(T)components.Find(c=>c is T);public void SetActive(bool value){activeSelf=value;foreach(var c in components)Call(c,value?"OnEnable":"OnDisable");}public static void Call(object c,string m,params object[] args){c.GetType().GetMethod(m,BindingFlags.Instance|BindingFlags.NonPublic)?.Invoke(c,args);}}
 public class Transform:Component {public Transform parent;public Vector3 localScale=Vector3.one,localPosition,position;public Quaternion localRotation,rotation;public void SetParent(Transform t,bool world){parent=t;}}
 public struct Vector3 {public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}public static Vector3 one=>new Vector3(1,1,1);public static Vector3 zero=>new Vector3();public static Vector3 down=>new Vector3(0,-1,0);public static Vector3 forward=>new Vector3(0,0,1);public static Vector3 up=>new Vector3(0,1,0);public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator *(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);}
 public struct Quaternion {public static Quaternion Euler(float a,float b,float c)=>new Quaternion();public static Vector3 operator *(Quaternion a,Vector3 b)=>b;public static Quaternion operator *(Quaternion a,Quaternion b)=>a;}
 public class Material {}
 public struct Color {public Color(float a,float b,float c){}}
 public enum PrimitiveType {Cube,Sphere,Capsule}
 public static class Mathf {public const float PI=(float)Math.PI;public static int Clamp(int a,int b,int c)=>Math.Max(b,Math.Min(c,a));public static float Clamp01(float a)=>Math.Max(0,Math.Min(1,a));public static float SmoothStep(float a,float b,float t){t=Clamp01(t);return a+(b-a)*t*t*(3-2*t);}public static float Max(float a,float b)=>Math.Max(a,b);public static int Max(int a,int b)=>Math.Max(a,b);public static float Sin(float a)=>(float)Math.Sin(a);}
 public static class Time {public static float deltaTime=.1f,unscaledDeltaTime=.1f;}
 public enum RuntimeInitializeLoadType {SubsystemRegistration}public class RuntimeInitializeOnLoadMethodAttribute:Attribute {public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType value){}}
}
namespace Emberfall {
 internal static class FilledSkillVfx{internal static void SkipFinales(GameSession game){}}
 public enum VisualSurface {Metal,Crystal,Wood}
 public class PlayerController:MonoBehaviour {public int CombatEpoch;public bool IsDead;}
 public class GameSession:MonoBehaviour {public static GameSession Instance;public bool Paused,BackgroundPaused;public bool FinalBossEncounter=true;public PlayerController Player;public bool HasStarted=true,InputBlocked;}
 public sealed partial class CombatModel:MonoBehaviour {private LargeBossRig largeBossRig;internal void Rig(LargeBossRig r){largeBossRig=r;}
  public readonly List<Transform> Parts=new List<Transform>();public bool BeganDeath;public float Opacity=1;
  private Transform Joint(string name,Vector3 at){var t=new GameObject(name).transform;t.SetParent(transform,false);return t;}
  private Transform Part(string name,PrimitiveType shape,Vector3 at,Vector3 size,Color color,Transform parent,VisualSurface surface){var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=at;t.localScale=size;Parts.Add(t);return t;}
  public void BeginDeath(){BeganDeath=true;}public void SetDeathOpacity(float a){Opacity=a;}
 }
 public sealed partial class SummonedCompanion:MonoBehaviour {public enum Kind {Wolf,Spirit,Treant}internal static readonly List<SummonedCompanion> active=new List<SummonedCompanion>();public CombatModel model;public PlayerController Owner;public GameSession session;}
}
namespace Emberfall {
internal class LargeExpeditionBoss {internal LargeBossPhaseState State=new LargeBossPhaseState();}
internal sealed partial class LargeBossRig:MonoBehaviour {
 Transform body,core,firstRing,secondRing,beamEmitter;Transform[] legs=new Transform[4],petals=new Transform[4];LargeExpeditionBoss encounter;
 internal void Initialize(){body=Joint(transform,"body",Vector3.zero);core=Joint(body,"core",Vector3.zero);firstRing=Joint(body,"ring",Vector3.zero);secondRing=Joint(body,"ring",Vector3.zero);beamEmitter=Joint(body,"beam",Vector3.zero);for(int i=0;i<4;i++){legs[i]=Joint(transform,"leg",Vector3.zero);petals[i]=Joint(body,"petal",Vector3.zero);}BuildPowerChannels((c,s)=>new Material());}
 internal static Transform Joint(Transform parent,string name,Vector3 p){var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=p;return t;}
 internal static Transform Part(Transform parent,string name,PrimitiveType shape,Vector3 p,Vector3 scale,Material m){var t=Joint(parent,name,p);t.localScale=scale;return t;}
 internal void BindMask(int mask){encounter=new LargeExpeditionBoss();encounter.State.TryBegin(.5f,true);encounter.State.CommitAnchors(mask);LateUpdate();}
 internal bool Channel(int i)=>powerChannels[i].activeSelf;internal float CoreSize=>core.localScale.x;internal float BodyY=>body.localPosition.y;internal float PetalZ=>petals[0].localPosition.z;
}
}
public static class ShutdownTests {
static int n;static void Check(bool b,string m){n++;if(!b)throw new Exception(m);}
static CombatModel Build(out LargeBossRig rig){var m=new GameObject("model").AddComponent<CombatModel>();rig=m.gameObject.AddComponent<LargeBossRig>();rig.Initialize();m.Rig(rig);return m;}
public static void Main(){
LargeBossRig rig;var model=Build(out rig);for(int mask=0;mask<8;mask++){rig.BindMask(mask);for(int i=0;i<3;i++)Check(rig.Channel(i)==((mask&(1<<i))!=0),"actual channel object matches each live anchor bit");}
var owner=new GameObject("owner").AddComponent<PlayerController>();var game=new GameObject("session").AddComponent<GameSession>();game.Player=owner;game.InputBlocked=true;GameSession.Instance=game;Check(model.TryBeginLargeBossShutdown(),"large rig selects independent shutdown");for(int i=0;i<3;i++)Check(!rig.Channel(i),"all channels off at shutdown");Check(model.transform.parent==null&&model.BeganDeath,"pure model transfers with original owned palette");var fx=model.gameObject.Get<LargeBossShutdownVisual>();Time.deltaTime=0;for(int i=0;i<7;i++)GameObject.Call(fx,"Update");Check(rig.BodyY<0&&rig.CoreSize<1&&rig.PetalZ>0,"real chassis/core/petals dismantle separately");for(int i=0;i<37;i++)GameObject.Call(fx,"Update");Check(model.gameObject.destroyed&&!model.gameObject.activeSelf,"settlement input block and timescale zero still finish and release");
var a=Build(out rig);a.TryBeginLargeBossShutdown();var b=Build(out rig);b.TryBeginLargeBossShutdown();var c=Build(out rig);Check(c.TryBeginLargeBossShutdown()&&c.gameObject.Get<LargeBossShutdownVisual>()==null,"exhausted visual budget still selects immediate host teardown");
owner.CombatEpoch++;GameObject.Call(a.gameObject.Get<LargeBossShutdownVisual>(),"Update");Check(a.gameObject.destroyed,"epoch invalidation retires detached model");GameObject.Call(b.gameObject.Get<LargeBossShutdownVisual>(),"OnApplicationPause",true);Check(b.gameObject.destroyed,"background callback clears pure shutdown visual");
var d=Build(out rig);d.TryBeginLargeBossShutdown();game.Paused=true;GameObject.Call(d.gameObject.Get<LargeBossShutdownVisual>(),"Update");Check(!d.gameObject.destroyed,"manual pause preserves final boss presentation");GameObject.Call(d.gameObject.Get<LargeBossShutdownVisual>(),"OnApplicationPause",true);
game.Paused=false;Check(!LargeBossShutdownVisual.IsPresenting(game),"retired shutdown no longer blocks result");
var e=Build(out rig);e.TryBeginLargeBossShutdown();Check(LargeBossShutdownVisual.IsPresenting(game),"actual active shutdown queried for same owner epoch");LargeBossShutdownVisual.Skip(game);Check(!LargeBossShutdownVisual.IsPresenting(game)&&e.gameObject.destroyed,"continue retires actual presentation only");
var pending=Build(out rig);pending.TryBeginLargeBossShutdown();Check(pending.gameObject.Get<LargeBossShutdownVisual>()==null,"same-frame continue suppresses later pending detach");owner.CombatEpoch++;var next=Build(out rig);next.TryBeginLargeBossShutdown();Check(LargeBossShutdownVisual.IsPresenting(game),"next epoch is not suppressed by previous skip");GameObject.Call(next.gameObject.Get<LargeBossShutdownVisual>(),"OnApplicationPause",true);Check(!LargeBossShutdownVisual.IsPresenting(game),"background retirement clears actual query");
owner.CombatEpoch++;game.FinalBossEncounter=false;var middle=Build(out rig);middle.TryBeginLargeBossShutdown();var middleFx=middle.gameObject.Get<LargeBossShutdownVisual>();Check(!LargeBossShutdownVisual.IsPresenting(game),"middle gauntlet boss does not gate the next wave");for(int i=0;i<14;i++)GameObject.Call(middleFx,"Update");Check(middle.gameObject.destroyed,"middle boss retains short teardown");game.FinalBossEncounter=true;var last=Build(out rig);last.TryBeginLargeBossShutdown();var lastFx=last.gameObject.Get<LargeBossShutdownVisual>();for(int i=0;i<14;i++)GameObject.Call(lastFx,"Update");Check(!last.gameObject.destroyed&&LargeBossShutdownVisual.IsPresenting(game),"last boss still presents before chest settlement");for(int i=0;i<30;i++)GameObject.Call(lastFx,"Update");Check(last.gameObject.destroyed&&!LargeBossShutdownVisual.IsPresenting(game),"last boss finishes and releases settlement gate");
Console.WriteLine("PASS "+n+" actual boss channel-mask and detached shutdown lifecycle cases (Unity API doubles)");}}
