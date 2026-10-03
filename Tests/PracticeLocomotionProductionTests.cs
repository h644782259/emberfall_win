using System;
using System.Collections.Generic;
using UnityEngine;
using Emberfall;
namespace UnityEngine
{
 public class Transform {public Vector3 position;public Vector3 InverseTransformDirection(Vector3 v)=>v;}
 public struct Vector3
 {
  public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}public static Vector3 zero=>new Vector3();public float sqrMagnitude=>x*x+y*y+z*z;public float magnitude=>(float)Math.Sqrt(sqrMagnitude);
  public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);public static Vector3 operator *(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);
  public static Vector3 ClampMagnitude(Vector3 a,float n)=>a.magnitude>n?a*(n/a.magnitude):a;public static Vector3 Lerp(Vector3 a,Vector3 b,float t)=>a+(b-a)*Math.Min(1,Math.Max(0,t));
 }
 public static class Mathf{public static float Max(float a,float b)=>Math.Max(a,b);public static float Min(float a,float b)=>Math.Min(a,b);public static float Sin(float v)=>(float)Math.Sin(v);public static float Clamp01(float v)=>Math.Min(1,Math.Max(0,v));}
 public static class Time{public static float deltaTime=.1f;}
}
namespace Emberfall
{
 public static class CombatFx{public static Vector3 Flat(Vector3 v)=>new Vector3(v.x,0,v.z);}
 // Traversal boundary models accepted motion separately from requested motion.
 public static class WorldTraversal{public static float Acceptance=1;public static Vector3 Move(Vector3 a,Vector3 b,float radius)=>a+b*Acceptance;}
 public class EnemyStatusEffects{public bool IsFrozen,KnockedDown,IsAirborne;public float MoveMultiplier=1;}
 public class ControlClock{public void Advance(float dt){}}
 public partial class CombatModel
 {
  public readonly LocomotionPoseState locomotion=new LocomotionPoseState();public Vector3 LastWalking;public float Hint;private bool pilotAirborne;
  private void RecordPilotWalking(Vector3 v,float d,float r,bool w,bool a){LastWalking=v;}
  public void SetEnemyAttackPose(EnemyPosePhase state,float p){}public void Animate(float speed,float attack,bool hurt){Hint=speed;}
 }
 public partial class EnemyController
 {
  public Transform transform=new Transform();public GameSession session;public Vector3 walkingDisplacement,knockVelocity;public float NavigationRadius=.5f,stunTime,hurtTime,speed=2.5f,windup,totalWindup=1,attackAnimation;public bool preparing,activeChargePose;public bool IsStunned=>stunTime>0;
  public EnemyStatusEffects StatusEffects=new EnemyStatusEffects();public CombatModel model=new CombatModel();public ControlClock controlPolicy=new ControlClock();
 }
 public partial class GameSession{public bool PracticeActive=true;public CampPracticeRecord PracticeRecord;public List<EnemyController> Enemies=new List<EnemyController>();}
}
class Program
{
 static int count;static void Check(bool v,string s){count++;if(!v)throw new Exception(s);}static bool Near(float a,float b)=>Math.Abs(a-b)<.0001;
 static EnemyController New(CampPracticeScenario scene=CampPracticeScenario.Moving){var s=new GameSession{PracticeRecord=new CampPracticeRecord(scene,60,"")};s.PracticeRecord.Advance(.3f);var e=new EnemyController{session=s};e.transform.position=new Vector3(-2,0,-5);s.Enemies.Add(e);return e;}
 static void Main()
 {
  var e=New();e.Tick();Check(e.model.locomotion.Phase>0,"actual accepted walking advances phase");Check(Near(e.model.LastWalking.x,.25f)&&Near(e.model.Hint,1),"normal accepted displacement and hint");
  foreach(string control in new[]{"frozen","stunned","down","airborne"})
  {
   e=New();e.Tick();float phase=e.model.locomotion.Phase;e.StatusEffects.IsFrozen=control=="frozen";e.stunTime=control=="stunned"?1:0;e.StatusEffects.KnockedDown=control=="down";e.StatusEffects.IsAirborne=control=="airborne";e.Tick();
   Check(Near(e.model.locomotion.Phase,phase)&&e.model.LastWalking.sqrMagnitude==0&&e.model.Hint==0,control+" active gait stops");
   e.StatusEffects=new EnemyStatusEffects();e.stunTime=0;e.Tick();Check(e.model.locomotion.Phase>phase,"control release resumes accepted walking");
  }
  e=New(CampPracticeScenario.Stationary);e.knockVelocity=new Vector3(5,0,0);float start=e.transform.position.x;e.Tick();Check(e.transform.position.x>start&&e.model.locomotion.Phase==0&&e.model.LastWalking.sqrMagnitude==0,"knockback excluded from active gait");
  e=New();e.StatusEffects.MoveMultiplier=.4f;e.Tick();Check(Near(e.model.LastWalking.x,.1f)&&Near(e.model.Hint,.4f),"slow uses actual smaller displacement");e.StatusEffects.MoveMultiplier=1;e.Tick();Check(Near(e.model.LastWalking.x,.25f),"slow release resumes normal displacement");
  e=New();WorldTraversal.Acceptance=.2f;e.Tick();Check(Near(e.model.LastWalking.x,.05f),"obstacle records accepted not requested displacement");WorldTraversal.Acceptance=0;float old=e.model.locomotion.Phase;e.Tick();Check(Near(e.model.locomotion.Phase,old)&&e.model.Hint==0,"fully blocked target cannot step");WorldTraversal.Acceptance=1;
  e=New();e.Tick();e.session.PracticeRecord.Advance(3);e.Tick();Check(e.model.LastWalking.x<0,"moving target reversal updates direction");
  foreach(var scene in new[]{CampPracticeScenario.Stationary,CampPracticeScenario.FrontAndSupplier}){e=New(scene);e.walkingDisplacement=new Vector3(3,0,0);e.Tick();Check(e.model.LastWalking.sqrMagnitude==0&&e.model.locomotion.Phase==0,"stationary frame clears stale walking");}
  Console.WriteLine("PASS "+count+" real practice traversal/animation adapter/locomotion assertions; managed boundaries, not Unity");
 }
}
