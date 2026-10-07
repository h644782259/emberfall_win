// Production presentation + marker lifecycle with deterministic Unity API doubles.
// Native font metrics, camera projection, glyph atlas and actual rendering are not exercised.
using System;using System.Collections.Generic;using System.Reflection;using Emberfall;
namespace UnityEngine {
 public class Object {public bool destroyed;public static void Destroy(Object o){o.destroyed=true;}}
 public class Component:Object {public GameObject gameObject;public Transform transform=>gameObject.transform;public T GetComponent<T>() where T:Component=>gameObject.GetComponent<T>();public T GetComponentInChildren<T>(bool inactive) where T:Component=>gameObject.GetComponentInChildren<T>(inactive);}
 public class MonoBehaviour:Component {public bool isActiveAndEnabled=>gameObject.activeSelf;}
 public class Transform:Component {public Transform parent;public Vector3 position,localPosition,localScale=new Vector3(1,1,1);public Vector3 lossyScale=>localScale;public Quaternion rotation;public Vector3 up=>new Vector3(0,1,0);public Vector3 right=>new Vector3(1,0,0);public void SetParent(Transform t,bool world){parent=t;if(t!=null)t.gameObject.children.Add(gameObject);}public Vector3 TransformPoint(Vector3 p)=>position+new Vector3(p.x*localScale.x,p.y*localScale.y,p.z*localScale.z);}
 public class GameObject:Object {public bool activeSelf=true;public Transform transform;public List<Component> components=new List<Component>();public List<GameObject> children=new List<GameObject>();public GameObject(string n){transform=new Transform{gameObject=this};}public T AddComponent<T>() where T:Component,new(){var c=new T{gameObject=this};components.Add(c);if(c is TextMesh)AddComponent<Renderer>();Call(c,"OnEnable");return c;}public T GetComponent<T>() where T:Component{return (T)components.Find(c=>c is T);}public T GetComponentInChildren<T>(bool inactive) where T:Component{var c=GetComponent<T>();if(c!=null)return c;foreach(var child in children){c=child.GetComponentInChildren<T>(inactive);if(c!=null&&!c.destroyed)return c;}return null;}public void SetActive(bool b){activeSelf=b;foreach(var c in components)Call(c,b?"OnEnable":"OnDisable");}public static void Call(object o,string name){o.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic)?.Invoke(o,null);}}
 public struct Quaternion{}
 public struct Vector3 {public float x,y,z;public float sqrMagnitude=>x*x+y*y+z*z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);public static Vector3 operator *(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);}
 public struct Bounds {public Vector3 size,center;}
 public class Renderer:Component {public bool enabled=true;public Bounds localBounds=new Bounds{size=new Vector3(2,1,0)};}
 public struct Rect {public float x,y,width,height;public Rect(float a,float b,float c,float d){x=a;y=b;width=c;height=d;}public bool Overlaps(Rect r)=>x<r.x+r.width&&x+width>r.x&&y<r.y+r.height&&y+height>r.y;}
 public class Camera:Component {public static Camera main;public Vector3 WorldToScreenPoint(Vector3 p)=>new Vector3(640+p.x*10,360+p.y*10,p.z);}
 public static class Screen {public static Rect safeArea=new Rect(0,0,1280,720);}
 public static class Time {public static int frameCount;}
 public static class Mathf {public static float Abs(float f)=>Math.Abs(f);}
 public struct Color {public Color(float a,float b,float c){}}
 public enum TextAnchor {MiddleCenter}public enum TextAlignment {Center}
 public class Font {}
 public class TextMesh:Component {public Font font;public string text="标签";public int fontSize;public float characterSize;public TextAnchor anchor;public TextAlignment alignment;public Color color;}
 public enum RuntimeInitializeLoadType {SubsystemRegistration}
 public class RuntimeInitializeOnLoadMethodAttribute:Attribute {public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t){}}
}
namespace Emberfall {
 public static class GameFont {public static int calls;public static void Apply(UnityEngine.TextMesh text){calls++;text.font=new UnityEngine.Font();}}
 public class EnemyController:UnityEngine.MonoBehaviour{}
 public class GameSession:UnityEngine.MonoBehaviour {public HashSet<EnemyController> enemies=new HashSet<EnemyController>();public bool IsSideEventEnemy(EnemyController e)=>enemies.Contains(e);public int SideEventEnemiesRemaining=>enemies.Count;}
}
public static class WorldLabelProductionTests {
 static int checks;static void Check(bool b,string m){checks++;if(!b)throw new Exception(m);}
 static UnityEngine.GameObject Label(int priority,float x=0,float z=10){var go=new UnityEngine.GameObject("label");go.transform.position=new UnityEngine.Vector3(x,0,z);go.AddComponent<WorldLabelPresentation>().Initialize(go.AddComponent<UnityEngine.TextMesh>(),priority);return go;}
 static void Tick(UnityEngine.GameObject go){UnityEngine.Time.frameCount++;UnityEngine.GameObject.Call(go.GetComponent<WorldLabelPresentation>(),"LateUpdate");}
 public static void Main(){
 var existing=new UnityEngine.GameObject("font-test");var configured=existing.AddComponent<UnityEngine.TextMesh>();var font=new UnityEngine.Font();configured.font=font;int before=GameFont.calls;existing.AddComponent<WorldLabelPresentation>().Initialize(configured);Check(object.ReferenceEquals(configured.font,font)&&GameFont.calls==before,"preserves authored world font");configured.font=null;existing.GetComponent<WorldLabelPresentation>().Initialize(configured);Check(configured.font!=null&&GameFont.calls==before+1,"missing font receives fallback");existing.SetActive(false);
 UnityEngine.Camera.main=new UnityEngine.GameObject("camera").AddComponent<UnityEngine.Camera>();var low=Label(0);var high=Label(2);Tick(low);Check(!low.GetComponent<UnityEngine.Renderer>().enabled&&high.GetComponent<UnityEngine.Renderer>().enabled,"objective priority suppresses overlapping scenery label");
 high.transform.position=new UnityEngine.Vector3(0,0,-10);Tick(low);Check(!high.GetComponent<UnityEngine.Renderer>().enabled&&low.GetComponent<UnityEngine.Renderer>().enabled,"behind-camera hidden and lower priority restored");
 high.transform.position=new UnityEngine.Vector3(0,0,40);Tick(low);Check(!high.GetComponent<UnityEngine.Renderer>().enabled,"distant label hidden rather than enlarged");
 high.SetActive(false);Tick(low);Check(low.GetComponent<UnityEngine.Renderer>().enabled,"disabled winner unregisters");
 Check(HudLogicalScale.For(1280,720)==1&&HudLogicalScale.For(2560,1440)==2,"shared HUD logical density");Check(WorldLabelReadability.Scale(10,1,2)==2.4f&&WorldLabelReadability.Scale(10,1,.3f)==1.2f,"logical scaling preserves physical legibility floor");
 var game=new UnityEngine.GameObject("session").AddComponent<GameSession>();var a=new UnityEngine.GameObject("enemy A").AddComponent<EnemyController>();var b=new UnityEngine.GameObject("enemy B").AddComponent<EnemyController>();game.enemies.Add(a);game.enemies.Add(b);SideEventEnemyMarker.Attach(a,game);SideEventEnemyMarker.Attach(b,game);var marker=a.GetComponentInChildren<SideEventEnemyMarker>(true);var text=marker.GetComponent<UnityEngine.TextMesh>();Check(text.text=="晶核守卫 2/2","real registered enemies create remaining-two display");SideEventEnemyMarker.Attach(a,game);Check(a.gameObject.children.Count==1,"attachment is idempotent");int calls=GameFont.calls;UnityEngine.GameObject.Call(marker,"LateUpdate");Check(GameFont.calls==calls,"unchanged remaining count does not churn font requests");game.enemies.Remove(b);UnityEngine.GameObject.Call(marker,"LateUpdate");Check(text.text=="晶核守卫 1/2","survivor updates remaining-one");game.enemies.Clear();UnityEngine.GameObject.Call(marker,"LateUpdate");Check(!marker.gameObject.activeSelf&&marker.gameObject.destroyed&&!text.GetComponent<UnityEngine.Renderer>().enabled,"invalid run membership retires label immediately");Check(!a.destroyed&&!b.destroyed,"marker never destroys actors");
 low.SetActive(false);for(int i=0;i<WorldLabelReadability.MaximumLabels;i++)Label(0,i);var overflow=Label(0);Check(!overflow.GetComponent<UnityEngine.Renderer>().enabled,"registration overflow hides rather than leaks labels");
 Console.WriteLine("PASS "+checks+" actual label/marker production lifecycle and overlap cases (Unity API doubles)");
 }
}
