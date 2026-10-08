// Executes the production FilledSkillVfx component with managed scene/transform substitutes.
// Component counts, mesh vertices and placement are real production outputs, not GPU evidence.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Emberfall;
using UnityEngine;
public static class FilledVfxAllocationTests
{
    static int checks;
    static void Check(bool value,string label){checks++;if(!value)throw new Exception(label);}
    static List<GameObject> Spawn(FilledVfxKind kind,bool mobile,bool reduced,float wall=float.PositiveInfinity)
    {
        typeof(FilledSkillVfx).GetMethod("ResetAssets",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        foreach(var old in GameObject.All.ToArray())UnityEngine.Object.Destroy(old);
        GameObject.All.Clear();Application.isMobilePlatform=mobile;EffectPreferences.ReducedEffects=reduced;CombatSight.Wall=wall;
        var hero=new GameObject("Hero").AddComponent<PlayerController>();GameSession.Instance=new GameSession{Player=hero,HasStarted=true};
        FilledSkillVfx.Impact(hero,Vector3.zero,4,kind,new Color(1,1,1,1));
        return GameObject.All.Where(x=>x.GetComponent<MeshFilter>()!=null).ToList();
    }
    public static string Run()
    {
        foreach(var kind in new[]{FilledVfxKind.Ice,FilledVfxKind.Fire,FilledVfxKind.Summon,FilledVfxKind.Sword,FilledVfxKind.Lightning,FilledVfxKind.Arcane})
        foreach(bool reduced in new[]{false,true})
        {
            var parts=Spawn(kind,true,reduced);
            Check(parts.Count==(reduced?7:kind==FilledVfxKind.Summon?9:10),"real mobile retained count");
            Check(parts[0].name.EndsWith("Landing base"),"landing base must survive actual allocation before decorations");
            Check(parts[1].name.EndsWith("Primary "+kind),"primary identity must survive actual allocation");
            Check(parts[2].name.EndsWith("Contact flash"),"contact flash must survive actual allocation");
            if(kind==FilledVfxKind.Sword)Check(parts[1].GetComponent<MeshFilter>().sharedMesh.name.Contains("sword"),"sword is not ice or thrust");
            if(kind==FilledVfxKind.Lightning)Check(parts[1].GetComponent<MeshFilter>().sharedMesh.name.Contains("branched"),"lightning primary has branches");
            if(kind==FilledVfxKind.Arcane)Check(parts[1].GetComponent<MeshFilter>().sharedMesh.name.Contains("lattice"),"arcane is not summon crescents");
        }
        foreach(var kind in new[]{FilledVfxKind.Ice,FilledVfxKind.Fire,FilledVfxKind.Sword,FilledVfxKind.Lightning,FilledVfxKind.Arcane})
        {
            var parts=Spawn(kind,true,false,.25f);
            Check(parts.Any(x=>x.name.EndsWith("Landing base")),"wall-adjacent landing retains visible base");
            Check(parts.Any(x=>x.name.EndsWith("Primary "+kind)),"wall-adjacent primary placed in valid side");
            var root=GameObject.All.First(x=>x.GetComponent<FilledSkillVfx>()!=null);
            foreach(float age in new[]{0f,.12f,.3f,.65f,.94f})
            {
                var effect=root.GetComponent<FilledSkillVfx>();
                typeof(FilledSkillVfx).GetField("age",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(effect,age);
                Time.deltaTime=.001f;root.Call("Update");
                foreach(var part in parts)
                {
                    if(!part.activeSelf)continue;
                    Check(part.GetComponent<MeshRenderer>().enabled,"whole renderer does not blink off against wall");
                    foreach(var vertex in part.GetComponent<MeshFilter>().sharedMesh.vertices)
                    {
                        Vector3 actual=part.transform.TransformPoint(vertex);
                        Check(actual.x<=.2502f,"actual animated mesh remains on visible wall side: "+kind+" "+part.name+" x="+actual.x);
                        Check(actual.x*actual.x+actual.z*actual.z<=16.01f,"mesh stays within its area footprint");
                    }
                }
            }
        }
        {
            var parts=Spawn(FilledVfxKind.Arcane,true,false);var primary=parts[1];var initial=primary.transform.localScale.x;
            var root=GameObject.All.First(o=>o.GetComponent<FilledSkillVfx>()!=null);var effect=root.GetComponent<FilledSkillVfx>();
            Check(primary.activeSelf&&parts.Skip(3).All(o=>!o.activeSelf),"contact has cage before delayed disassembly");
            typeof(FilledSkillVfx).GetField("age",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(effect,.17f);root.Call("Update");
            Check(primary.transform.localScale.x<initial*.7f,"arcane compresses during settle phase");
            typeof(FilledSkillVfx).GetField("age",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(effect,.44f);root.Call("Update");
            Check(primary.transform.localScale.x>initial&&parts.Skip(3).Any(o=>o.activeSelf&&o.GetComponent<MeshFilter>().sharedMesh.name=="Broken arcane strut"),"release expands lattice and exposes separate struts");
            typeof(FilledSkillVfx).GetField("age",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(effect,.94f);root.Call("Update");
            Check(primary.GetComponent<MeshRenderer>().Opacity<.5f,"residual phase visibly fades opacity in production property block");
        }
        {
            CombatSight.Wall=.25f;Time.deltaTime=.016f;
            var obj=new GameObject("Covered particles");var particles=obj.AddComponent<ParticleSystem>();particles.shape=new ParticleSystem.ShapeModule{radius=2};
            particles.Values=new[]{new ParticleSystem.Particle{position=new Vector3(1,0,0),velocity=new Vector3(1,1,0),startLifetime=1,remainingLifetime=1,startSize=.2f},
                new ParticleSystem.Particle{position=new Vector3(1,0,0),startLifetime=1,remainingLifetime=.5f,startSize=.2f}};
            obj.AddComponent<CoveredAreaParticles>();obj.Call("Awake");obj.Call("LateUpdate");
            Check(particles.Values[0].remainingLifetime>0&&particles.Values[0].position.x+particles.Values[0].startSize*.5f<=.25f,"fresh covered particle is placed on a clear side before render");
            Check(particles.Values[0].velocity.x==0&&particles.Values[1].remainingLifetime==0,"fresh drift constrained; old covered particle retired rather than teleported");
            obj.Call("LateUpdate");Check(particles.Values[0].remainingLifetime>0,"redirected particle survives subsequent clipping frame");
        }
        float x,z,fit;
        Check(FilledVfxPlacement.TryPlace(1,0,3,.5f,(px,pz,r)=>px+r<.1f,out x,out z,out fit)&&fit==1&&x<0,"clear directions preferred before shrinking");
        Check(!FilledVfxPlacement.TryPlace(0,0,3,.5f,(px,pz,r)=>false,out x,out z,out fit),"fully covered area rejects rather than drawing through cover");
        foreach(float invalid in new[]{float.NaN,float.PositiveInfinity,-1,0})Check(!FilledVfxPlacement.TryPlace(0,0,invalid,.5f,(px,pz,r)=>true,out x,out z,out fit),"invalid area rejected");
        return "PASS: "+checks+" production filled allocation/animated-vertex/coverage checks (managed, not GPU)";
    }
}
namespace Emberfall
{
    // Resource boundary for legacy procedural allocation tests; authored success is tested separately.
    internal static class AuthoredSpellBases { internal static UnityEngine.Mesh Load(string name){return null;} internal static UnityEngine.Mesh Identity(string name){return null;} }
    public sealed class PlayerController:MonoBehaviour{public bool IsDead;public int CombatEpoch;}
    public sealed class GameSession{public static GameSession Instance;public PlayerController Player;public bool HasStarted,ModeFinished,InputBlocked;}
    public static class EffectPreferences{public static bool ReducedEffects;public static float EffectsScale=1;}
    public enum CombatSightKind { Area }
    public static class CombatFx{public static Vector3 Flat(Vector3 value){value.y=0;return value;}public static Material NewGlow()=>new Material(new Shader());}
    public static class WorldTraversal {public static int Revision;}
    public static class CombatSight
    {
        public static float Wall=float.PositiveInfinity;public static int FootprintCalls;
        public static Vector3 BoundaryPoint(CombatSightKind kind,Vector3 from,Vector3 to){float t=to.x<=Wall?1:(Wall-from.x)/Mathf.Max(.000001f,to.x-from.x);var v=Vector3.Lerp(from,to,Mathf.Clamp01(t));v.y=0;return v;}
        public static bool Direct(Vector3 from,Vector3 to)=>Area(from,to);
        public static bool Area(Vector3 from,Vector3 to)=>from.x<=Wall&&to.x<=Wall;
        public static bool VisualTriangle(Vector3 origin,Vector3 a,Vector3 b,Vector3 c){FootprintCalls++;return origin.x+.04f<=Wall&&a.x+.04f<=Wall&&b.x+.04f<=Wall&&c.x+.04f<=Wall;}
        public static bool VisualFootprint(Vector3 from,Vector3 to,float radius){FootprintCalls++;return from.x+radius<=Wall&&to.x+radius<=Wall;}
    }
}
namespace UnityEngine
{
    public enum RuntimeInitializeLoadType{SubsystemRegistration}
    [AttributeUsage(AttributeTargets.Method)]public sealed class RuntimeInitializeOnLoadMethodAttribute:Attribute{public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType value){}}
    public static class Application{public static bool isMobilePlatform;}
    public static class Time{public static float deltaTime,time,unscaledDeltaTime;public static int frameCount;}
    public class Object{public string name;public bool Destroyed;public static void Destroy(Object value){if(value==null||value.Destroyed)return;value.Destroyed=true;if(value is GameObject go){go.SetActive(false);go.Call("OnDestroy");foreach(var child in GameObject.All.Where(x=>x.transform.parent==go.transform).ToArray())Destroy(child);}}}
    public class Component:Object{public GameObject gameObject;public Transform transform=>gameObject.transform;public T GetComponent<T>()where T:Component=>gameObject.GetComponent<T>();}
    public class MonoBehaviour:Component{}
    public class GameObject:Object
    {
        public static readonly List<GameObject> All=new List<GameObject>();public readonly Transform transform;public bool activeSelf=true;
        readonly List<Component> components=new List<Component>();
        public GameObject(string name=""){this.name=name;transform=new Transform{gameObject=this};All.Add(this);}
        public T AddComponent<T>()where T:Component,new(){var c=new T{gameObject=this};components.Add(c);return c;}
        public T GetComponent<T>()where T:Component=>components.OfType<T>().FirstOrDefault();
        public bool activeInHierarchy=>activeSelf&&(transform.parent==null||transform.parent.gameObject.activeInHierarchy);
        public void SetActive(bool value){if(activeSelf==value)return;activeSelf=value;Call(value?"OnEnable":"OnDisable");}
        public void Call(string name){foreach(var c in components)c.GetType().GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance)?.Invoke(c,null);}
    }
    public sealed class Transform
    {
        public GameObject gameObject;public Transform parent;public Vector3 localPosition,localScale=Vector3.one;public Quaternion localRotation=Quaternion.identity;
        public Vector3 position{get=>parent==null?localPosition:parent.TransformPoint(localPosition);set=>localPosition=parent==null?value:parent.InverseTransformPoint(value);}
        public Vector3 lossyScale=>parent==null?localScale:new Vector3(parent.lossyScale.x*localScale.x,parent.lossyScale.y*localScale.y,parent.lossyScale.z*localScale.z);
        public Vector3 right=>localRotation.Rotate(new Vector3(1,0,0));
        public Vector3 InverseTransformPoint(Vector3 value){if(parent!=null)value=parent.InverseTransformPoint(value);value=localRotation.InverseRotate(value-localPosition);return new Vector3(value.x/localScale.x,value.y/localScale.y,value.z/localScale.z);}
        public Quaternion rotation{get=>localRotation;set=>localRotation=value;}
        public void SetParent(Transform value,bool worldPositionStays){parent=value;}
        public Vector3 TransformPoint(Vector3 value){var point=localRotation.Rotate(new Vector3(value.x*localScale.x,value.y*localScale.y,value.z*localScale.z))+localPosition;return parent==null?point:parent.TransformPoint(point);}
    }
    public sealed class Mesh:Object{public Vector3[] vertices;public Color[] colors;public Vector2[] uv;public int[] triangles;public void RecalculateNormals(){}public void RecalculateBounds(){}}
    public enum PrimitiveType{Sphere}
    public struct Keyframe{public Keyframe(float time,float value){}}
    public sealed class AnimationCurve{public AnimationCurve(params Keyframe[] keys){}}
    public sealed class LineRenderer:Renderer{public bool useWorldSpace;public int positionCount;public float widthMultiplier;public AnimationCurve widthCurve;public Color startColor,endColor;public readonly Vector3[] Positions=new Vector3[4];public void SetPosition(int index,Vector3 value){Positions[index]=value;}}
    public sealed class TrailRenderer:Component{public bool emitting;public int ClearCount;public void Clear(){ClearCount++;}}
    public sealed class MeshFilter:Component{public Mesh sharedMesh;}
    public class Renderer:Component{public bool enabled=true,receiveShadows;public int sortingOrder;public Rendering.ShadowCastingMode shadowCastingMode;public Material sharedMaterial;public float Opacity;public void SetPropertyBlock(MaterialPropertyBlock block){Opacity=block.Opacity;}}
    public sealed class MeshRenderer:Renderer{}
    public sealed class Material:Object{public int renderQueue;public Color color;public Material(Shader shader){}}
    public sealed class Shader:Object{public static Shader Find(string name)=>new Shader();}
    public static class Resources{public static T Load<T>(string name)where T:new()=>new T();}
    public sealed class MaterialPropertyBlock{public float Opacity;public void SetColor(string name,Color value){}public void SetFloat(string name,float value){if(name=="_Opacity")Opacity=value;}}
    public sealed class ParticleSystem:Component{public struct Particle{public Vector3 position,velocity;public float startLifetime,remainingLifetime,startSize;}public struct ShapeModule{public float radius;}public ShapeModule shape;public Particle[] Values;public int GetParticles(Particle[] buffer){Array.Copy(Values,buffer,Values.Length);return Values.Length;}public void SetParticles(Particle[] buffer,int count){Values=new Particle[count];Array.Copy(buffer,Values,count);}}
    public struct Color{public float r,g,b,a;public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}}
    public struct Vector2{public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}}
    public struct Vector3
    {
        public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
        public static Vector3 zero=>new Vector3();public static Vector3 one=>new Vector3(1,1,1);public static Vector3 up=>new Vector3(0,1,0);public static Vector3 forward=>new Vector3(0,0,1);
        public float sqrMagnitude=>x*x+y*y+z*z;public float magnitude=>(float)Math.Sqrt(sqrMagnitude);public Vector3 normalized=>this*(1/magnitude);
        public static Vector3 Lerp(Vector3 a,Vector3 b,float t)=>a+(b-a)*Mathf.Clamp01(t);
        public static Vector3 Scale(Vector3 a,Vector3 b)=>new Vector3(a.x*b.x,a.y*b.y,a.z*b.z);
        public static Vector3 operator/(Vector3 a,float b)=>a*(1/b);
        public static Vector3 Cross(Vector3 a,Vector3 b)=>new Vector3(a.y*b.z-a.z*b.y,a.z*b.x-a.x*b.z,a.x*b.y-a.y*b.x);
        public static Vector3 operator+(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3 operator-(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
        public static Vector3 operator*(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);
    }
    public struct Quaternion
    {
        System.Numerics.Quaternion value;
        public static Quaternion identity=>new Quaternion{value=System.Numerics.Quaternion.Identity};
        public static Quaternion Euler(float x,float y,float z)=>new Quaternion{value=System.Numerics.Quaternion.CreateFromYawPitchRoll(y*Mathf.PI/180,x*Mathf.PI/180,z*Mathf.PI/180)};
        public static Quaternion LookRotation(Vector3 forward)=>identity;public static Quaternion FromToRotation(Vector3 from,Vector3 to)=>identity;
        public static Vector3 operator*(Quaternion q,Vector3 v)=>q.Rotate(v);
        public static Quaternion operator*(Quaternion a,Quaternion b)=>new Quaternion{value=a.value*b.value};
        public Vector3 InverseRotate(Vector3 vector){var r=System.Numerics.Vector3.Transform(new System.Numerics.Vector3(vector.x,vector.y,vector.z),System.Numerics.Quaternion.Inverse(value));return new Vector3(r.X,r.Y,r.Z);}
        public Vector3 Rotate(Vector3 vector){var r=System.Numerics.Vector3.Transform(new System.Numerics.Vector3(vector.x,vector.y,vector.z),value);return new Vector3(r.X,r.Y,r.Z);}
    }
    public static class Mathf
    {
        public static float Pow(float value,float power)=>(float)Math.Pow(value,power);
        public const float PI=(float)Math.PI,Rad2Deg=180/PI;
        public static float Min(float a,float b)=>Math.Min(a,b);public static float Max(float a,float b)=>Math.Max(a,b);public static int Max(int a,int b)=>Math.Max(a,b);
        public static float Clamp01(float value)=>Math.Max(0,Math.Min(1,value));
        public static float Clamp(float value,float low,float high)=>Math.Max(low,Math.Min(high,value));public static float Lerp(float a,float b,float t)=>a+(b-a)*Clamp(t,0,1);
        public static float Abs(float value)=>Math.Abs(value);public static float Repeat(float value,float length)=>value-(float)Math.Floor(value/length)*length;
        public static float Cos(float value)=>(float)Math.Cos(value);public static float Sin(float value)=>(float)Math.Sin(value);
    }
}
namespace UnityEngine.Rendering{public enum ShadowCastingMode{Off}}
