// Execute the real friendly contact branch, including collision/LOS/prop and pierce guards.
// Scene queries, damage receiver and feedback are recording boundaries; no native physics claim.
using System;using System.Collections.Generic;using System.Linq;using UnityEngine;
namespace UnityEngine {
 public struct Color{public Color(float r,float g,float b,float a=1){}}
 public struct Vector3 {public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}}
 internal class Transform {public Vector3 position;}
 internal class GameObject {public bool Destroyed,activeInHierarchy=true;}
 public static class Mathf {public static float Max(float a,float b)=>Math.Max(a,b);public static int Min(int a,int b)=>Math.Min(a,b);}
}
namespace Emberfall {
 public static class Trace {public static List<string> Events=new List<string>();}
 internal class Status {public void Mark(float seconds,float strength){Trace.Events.Add("mark");}}
 internal class EnemyController {public GameObject gameObject=new GameObject();public float Health=10,ProjectileHitRadius=.5f;public bool IsDead=>Health<=0;public Transform transform=new Transform();public Status StatusEffects=new Status();public bool Visible=true;public void TakeDamage(float amount,Vector3 d,float stagger,bool critical=false,int practiceCastId=0){Trace.Events.Add("damage");Health=Math.Max(0,Health-amount);}}
 internal class PlayerController {public Vector3 EnemyBodyPoint(EnemyController e)=>e.transform.position;public void RegisterSkillHit(int id){Trace.Events.Add("skillhit");}public float ResolveSkillImpact(EnemyController e,int skill,int id,float damage,bool critical,float multiplier)=>damage;public void OnBasicAttackHitTarget(Vector3 p,EnemyController e,bool hit){}public void HitArea(Vector3 p,float r,CombatDamage d,float a,float b,int cast,ProjectileVolleyBudget<EnemyController> volley){}}
 internal class SummonedCompanion {public float Loss;public void OnConfirmedHit(EnemyController e){Trace.Events.Add("companion");}public void RecordEmpoweredHit(EnemyController e,float loss,bool empowered){Loss=loss;Trace.Events.Add("feedback");}}
 internal class GameSession {public List<EnemyController> Enemies=new List<EnemyController>();}
 public static class CombatSight {public static HashSet<float> Blocked=new HashSet<float>();public static bool Direct(Vector3 a,Vector3 b)=>!Blocked.Contains(b.x);}
 public static class CombatFx {public static float SegmentDistance(Vector3 p,Vector3 a,Vector3 b){float dx=b.x-a.x,dz=b.z-a.z;float length=dx*dx+dz*dz;float t=length==0?0:Math.Max(0,Math.Min(1,((p.x-a.x)*dx+(p.z-a.z)*dz)/length));return (float)Math.Sqrt(Math.Pow(p.x-a.x-t*dx,2)+Math.Pow(p.z-a.z-t*dz,2));}public static void Ring(Vector3 p,float r,Color c,float a,float width=0){}}
 internal class DestructibleProp {public Transform transform=new Transform();public static DestructibleProp Next;public static float Fraction;public static void FindProjectileHit(PlayerController p,Vector3 a,Vector3 b,float r,out DestructibleProp prop,out float fraction){prop=Next;fraction=Fraction;}public void Impact(PlayerController p,int cast,CombatDamage d){Trace.Events.Add("prop");}}
 public static class CombatReviewEvents {public static bool Enabled=false;public static void Emit(string name,int owner,int target,float loss,int skill,string detail){}}
 public static class CombatReviewObjectId {public static int Get(object o)=>1;}
 internal partial class CombatProjectile {
  public bool concentrated;public GameSession session=new GameSession();public PlayerController owner=new PlayerController();public SummonedCompanion companionSource;public bool empoweredCompanionShot=true,pierce=true,basicAttack,energyAwarded;public EnemyController impactMarkTarget;public float impactMarkStrength=.2f,radius=.1f,explosionRadius;public CombatDamage damage=4,explosionDamage=0;public int skillIndex=7,castId=1;public ProjectileVolleyBudget<EnemyController> volley;
  public Transform transform=new Transform{position=new Vector3(10,0,0)};public GameObject gameObject=new GameObject();public Vector3 direction;public Color color;public HashSet<EnemyController> hitTargets=new HashSet<EnemyController>();void Destroy(GameObject o){o.Destroyed=true;}
 }
 public static class CombatReviewImpactProductionTests {
  static int n;static void C(bool b,string why){n++;if(!b)throw new Exception(why);}
  static CombatProjectile Setup(out EnemyController enemy){Trace.Events.Clear();CombatSight.Blocked.Clear();DestructibleProp.Next=null;enemy=new EnemyController();enemy.transform.position=new Vector3(5,0,0);var p=new CombatProjectile();p.session.Enemies.Add(enemy);p.impactMarkTarget=enemy;return p;}
  public static void Run(){EnemyController e;var p=Setup(out e);p.Contacts(new Vector3());C(Trace.Events.SequenceEqual(new[]{"mark","skillhit","damage"}),"accepted locked hit marks before damage");C(e.Health==6,"actual accepted contact invokes one damage receiver");p.Contacts(new Vector3());C(Trace.Events.Count==3,"already hit target cannot repeat mark or damage");
   p=Setup(out e);p.companionSource=new SummonedCompanion();e.Health=2;p.Contacts(new Vector3());C(Trace.Events.SequenceEqual(new[]{"mark","damage","companion","feedback"}),"companion callback survives expanded block and follows damage");C(p.companionSource.Loss==2,"feedback receives actual clamped lethal HP loss");
   p=Setup(out e);p.impactMarkTarget=new EnemyController();p.Contacts(new Vector3());C(!Trace.Events.Contains("mark")&&e.Health==6,"bystander collision cannot inherit locked mark");
   p=Setup(out e);e.Health=0;p.Contacts(new Vector3());C(Trace.Events.Count==0,"dead candidate rejected before hit registration");
   p=Setup(out e);CombatSight.Blocked.Add(e.transform.position.x);p.Contacts(new Vector3());C(Trace.Events.Count==0&&p.hitTargets.Count==0,"occluded candidate cannot mark or damage");
   p=Setup(out e);e.transform.position=new Vector3(5,0,3);p.Contacts(new Vector3());C(Trace.Events.Count==0,"off-segment candidate cannot mark or damage");
   p=Setup(out e);p.damage=0;p.Contacts(new Vector3());C(Trace.Events.Count==0,"zero accepted damage cannot mark or invoke damage");
   p=Setup(out e);p.volley=new ProjectileVolleyBudget<EnemyController>(0,1);p.Contacts(new Vector3());C(Trace.Events.Count==0,"real exhausted volley budget cannot mark or invoke damage");
   p=Setup(out e);p.pierce=false;var behind=new EnemyController();behind.transform.position=new Vector3(7,0,0);p.session.Enemies.Insert(0,behind);p.Contacts(new Vector3());C(p.gameObject.Destroyed&&e.Health==6&&behind.Health==10,"nonpiercing contact stops later candidates");
   p=Setup(out e);DestructibleProp.Next=new DestructibleProp();DestructibleProp.Fraction=.2f;p.Contacts(new Vector3());C(e.Health==10&&Trace.Events.SequenceEqual(new[]{"prop"})&&p.gameObject.Destroyed,"nearer prop excludes enemy behind it and retires projectile");
   Console.WriteLine("PASS: "+n+" actual friendly-projectile branch qualification/order/pierce/feedback assertions; managed boundaries, not Unity physics");
  }
 }
}
class Program {static void Main(){Emberfall.CombatReviewImpactProductionTests.Run();}}

namespace Emberfall{internal static class VenomSkillVfx{internal static void Contact(PlayerController p,Vector3 point,bool consumed){}}}
