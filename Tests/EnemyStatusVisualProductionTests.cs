using System;using System.Collections.Generic;using System.Linq;using System.Reflection;using Emberfall;using UnityEngine;
namespace UnityEngine{
public class Object{public static void Destroy(Object value){}}
public class Component:Object{public GameObject gameObject;public Transform transform=>gameObject.transform;public T GetComponent<T>()where T:Component=>gameObject.GetComponent<T>();}
public class MonoBehaviour:Component{public T GetComponentInChildren<T>()where T:Component=>null;}
public class GameObject:Object{public string name;public bool activeSelf=true;public Transform transform=new Transform();public bool activeInHierarchy=>activeSelf;readonly Dictionary<Type,Component> parts=new Dictionary<Type,Component>();public static List<GameObject> All=new List<GameObject>();public GameObject(string name=""){this.name=name;All.Add(this);}public T AddComponent<T>()where T:Component,new(){var value=new T{gameObject=this};parts[typeof(T)]=value;return value;}public T GetComponent<T>()where T:Component=>parts.TryGetValue(typeof(T),out var value)?(T)value:null;public void SetActive(bool value){activeSelf=value;}}
public class Transform{public Vector3 position;public Vector3 TransformPoint(Vector3 point)=>position+point;public Transform parent;public Vector3 localPosition,localScale;public Quaternion localRotation;public void SetParent(Transform value,bool stay){parent=value;}}
public static class Mathf{public static float Max(float a,float b)=>Math.Max(a,b);}
public struct Vector3{public static Vector3 right=>new Vector3(1,0,0);public static Vector3 up=>new Vector3(0,1,0);public static Vector3 forward=>new Vector3(0,0,1);public float magnitude=>(float)Math.Sqrt(x*x+y*y+z*z);public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}public static Vector3 operator *(Vector3 p,float s)=>new Vector3(p.x*s,p.y*s,p.z*s);}
public struct Color{public Color(float r,float g,float b){}}
public struct Quaternion{public static Quaternion Euler(float x,float y,float z)=>new Quaternion();}
public class Shader{public static Shader Find(string name)=>new Shader();}public class Material:Object{public Color color;public Material(Shader s){}}
public class MeshRenderer:Component{public UnityEngine.Rendering.ShadowCastingMode shadowCastingMode;}public enum PrimitiveType{Cube}
}
namespace UnityEngine.Rendering{public enum ShadowCastingMode{Off,On}}
namespace Emberfall{
public class CombatModel:MonoBehaviour{public bool TryStatusAttachment(out Transform anchor,out Vector3 frost,out Vector3 vulnerability){throw new Exception("legacy view test has no model; real models covered by status anchor suite");}}
public class EnemyStatusEffects{public bool IsFrozen,HasFrostMark,IsMarked;internal event Action VisualStateChanged;public void Refresh(){VisualStateChanged?.Invoke();}}
public class EnemyController:MonoBehaviour{public enum ThreatTier{Normal,Elite}public bool IsDead,IsBoss;public ThreatTier Tier;public EnemyStatusEffects StatusEffects=new EnemyStatusEffects();}
public class PlayerController{public EnemyController AimTarget;}public class GameSession{public static GameSession Instance=new GameSession();public PlayerController Player=new PlayerController();}
public static class EffectPreferences{public static bool ReducedEffects;}
public static class ProceduralVisuals{public static GameObject Create(string name,PrimitiveType type,Material material){var obj=new GameObject(name);obj.AddComponent<MeshRenderer>();return obj;}}
}
class Program{
static int n;static void C(bool b,string msg){n++;if(!b)throw new Exception(msg);}static int Active(EnemyController e,string name)=>GameObject.All.Count(x=>x.name==name&&x.activeSelf&&x.transform.parent==e.transform);
static void Sync(EnemyController e)=>typeof(EnemyStatusVisual).GetMethod("LateUpdate",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(e.GetComponent<EnemyStatusVisual>(),null);
static EnemyController Enemy(){var e=new GameObject().AddComponent<EnemyController>();EnemyStatusVisual.Attach(e);return e;}
static void Main(){var first=Enemy();var second=Enemy();GameSession.Instance.Player.AimTarget=first;first.StatusEffects.IsFrozen=first.StatusEffects.HasFrostMark=true;first.StatusEffects.Refresh();C(Active(first,"Frozen ankle crystal")==3&&Active(first,"Frost mark diamond")==1,"selected real freeze and frost have separate channels");C(Active(second,"Frozen ankle crystal")==0,"enemy ownership isolated");
first.StatusEffects.IsFrozen=first.StatusEffects.HasFrostMark=false;first.StatusEffects.Refresh();C(Active(first,"Frozen ankle crystal")==0&&Active(first,"Frost mark diamond")==0,"consume synchronously clears reusable channels");int created=GameObject.All.Count;first.StatusEffects.IsFrozen=first.StatusEffects.HasFrostMark=true;first.StatusEffects.Refresh();C(Active(first,"Frozen ankle crystal")==3&&GameObject.All.Count==created,"same frame reapply reuses shapes without queued destroy");
GameSession.Instance.Player.AimTarget=second;Sync(first);C(Active(first,"Frozen ankle crystal")==1&&Active(second,"Frozen ankle crystal")==0,"retarget updates emphasis not state ownership");first.IsBoss=true;Sync(first);C(Active(first,"Frozen ankle crystal")==0&&Active(first,"Frost mark diamond")==1,"boss mark never implies hard freeze");first.StatusEffects.IsMarked=true;first.StatusEffects.Refresh();C(Active(first,"Vulnerability slash")==2&&!GameObject.All.Any(x=>x.name.Contains("reticle")),"vulnerability slashes distinct from target reticle");
first.IsBoss=false;first.Tier=EnemyController.ThreatTier.Elite;Sync(first);C(Active(first,"Frozen ankle crystal")==2,"elite intermediate detail");EffectPreferences.ReducedEffects=true;GameSession.Instance.Player.AimTarget=first;Sync(first);C(Active(first,"Frozen ankle crystal")==1,"reduced detail bounded");first.IsDead=true;Sync(first);C(Active(first,"Frozen ankle crystal")==0&&Active(first,"Frost mark diamond")==0&&Active(first,"Vulnerability slash")==0,"death clears all channels");
Console.WriteLine("PASS "+n+" actual full status visual component lifecycle checks; Unity rendering doubled");}}
