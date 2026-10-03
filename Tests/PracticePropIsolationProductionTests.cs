using System;
using UnityEngine;
using Emberfall;
namespace Emberfall
{
 public static class WorldTraversal{public class ObstacleHandle{public bool Released;}public static int Obstacles;public static ObstacleHandle AddDynamicCircle(Vector3 p,float r){Obstacles++;return new ObstacleHandle();}public static void RemoveDynamicObstacle(ObstacleHandle h){h.Released=true;Obstacles--;}public static bool HasLineOfSightIgnoringObstacle(Vector3 a,Vector3 b,ObstacleHandle h)=>true;public static bool IsWalkable(Vector3 p,float r)=>true;}
 public static class CombatFx{public static Vector3 Flat(Vector3 v)=>new Vector3(v.x,0,v.z);public static float SegmentDistance(Vector3 p,Vector3 a,Vector3 b)=>0;}
 public struct CombatDamage{public float Amount;public bool IsCritical;}
 public enum CombatVisualPriority{RealContact}public static class HitFeedback{public static void Spawn(Vector3 a,Vector3 b,float c,bool crit,CombatVisualPriority priority){}}
 public static class EffectPreferences{public static bool ReducedEffects;}
 public static class DestructibleDebrisBurst{public static void Spawn(Transform t,Material m,int n){}}
 public enum SoundCue{Hit}public static class GameAudio{public static void Play(SoundCue c){}}
 public class PlayerController:MonoBehaviour{public bool IsDead;public int CombatEpoch=4;public float Health=30,MaxHealth=100,Energy=20;public int NewCastId()=>1;public void Heal(float v){Health+=v;}public void RestoreSkillEnergy(float v){Energy+=v;}}
 public class GameSession{public static GameSession Instance;public PlayerController Player;public bool HasStarted=true,InputBlocked,ChallengeRun,PracticeActive;public void LogSystem(string s){}}
}
class Program
{
 static int n;static void Check(bool v,string s){n++;if(!v)throw new Exception(s);}
 static void Main()
 {
  foreach(string path in new[]{"direct","area","cone","line","projectile"})
  {
   var real=new GameObject("original hero").AddComponent<PlayerController>();var temporary=new GameObject("practice hero").AddComponent<PlayerController>();var g=new GameSession{Player=real};GameSession.Instance=g;
   var prop=new GameObject("original world pot").AddComponent<DestructibleProp>();var visual=new GameObject("original pot visual");visual.transform.SetParent(prop.transform,false);prop.transform.position=new Vector3(0,0,1);prop.Initialize(DestructibleKind.Pot,1,.5f,true,PropRecovery.Health,visual.transform,null);
   int before=WorldTraversal.Obstacles;g.Player=temporary;g.PracticeActive=true;var damage=new CombatDamage{Amount=99999};
   switch(path){case "direct":prop.Impact(temporary,1,damage);break;case "area":DestructibleProp.StrikeArea(temporary,Vector3.zero,10,damage,1);break;case "cone":DestructibleProp.StrikeCone(temporary,Vector3.zero,Vector3.forward,10,90,damage,1);break;case "line":DestructibleProp.StrikeLine(temporary,Vector3.zero,Vector3.forward*3,1,damage,1);break;default:DestructibleProp target;float fraction;Check(!DestructibleProp.FindProjectileHit(temporary,Vector3.zero,Vector3.forward*3,1,out target,out fraction)&&target==null,"practice projectile cannot intercept original pot");break;}
   Check(!prop.Broken&&visual.activeSelf&&WorldTraversal.Obstacles==before,"practice cannot break or release original obstacle");Check(temporary.Health==30&&temporary.Energy==20,"practice cannot consume real scenery recovery");
   g.Player=real;g.PracticeActive=false;Check(!prop.Broken&&visual.activeSelf&&WorldTraversal.Obstacles==before,"exit preserves original Broken visual and collision registry");
   DestructibleProp hit;float t;Check(DestructibleProp.FindProjectileHit(real,Vector3.zero,Vector3.forward*3,1,out hit,out t)&&hit==prop,"ordinary projectile still intercepts original pot");prop.Impact(real,1,damage);Check(prop.Broken&&!visual.activeSelf&&WorldTraversal.Obstacles==before-1&&real.Health==33,"ordinary impact still breaks and heals once");
   UnityEngine.Object.Destroy(prop.gameObject);UnityEngine.Object.Flush();
  }
  Console.WriteLine("PASS "+n+" actual scenery damage/interception/exit-scope assertions; managed boundary, not Unity collider proof");
 }
}
