"""Execute actual flame producer and actual player vault methods against explicit engine doubles.
No Unity rendering, physics or device execution is claimed.
"""
from pathlib import Path
import os,sys,tempfile,subprocess
root=Path(__file__).resolve().parents[1]
def method(source, marker):
 start=source.index(marker);body=source.index('{',start);depth=1;i=body+1
 while depth:
  depth+=(source[i]=='{')-(source[i]=='}');i+=1
 return source[start:i]
player=(root/'Assets/Scripts/Combat/PlayerController.cs').read_text()
fixture=r'''
using System;using System.Collections.Generic;using System.Reflection;using UnityEngine;
namespace UnityEngine{
 public class Object{public bool destroyed;public static void Destroy(Object o){if(o!=null)o.destroyed=true;}}
 public class MonoBehaviour:Object{public GameObject gameObject;public Transform transform=>gameObject.transform;}
 public class GameObject:Object{public Transform transform=new Transform();public static List<MonoBehaviour> parts=new List<MonoBehaviour>();public GameObject(string s){} public T AddComponent<T>()where T:MonoBehaviour,new(){var t=new T{gameObject=this};parts.Add(t);return t;}}
 public class Transform{public Vector3 position,localPosition,localScale,forward=new Vector3(0,0,1);public Quaternion rotation;public void SetParent(Transform t,bool b){}public void SetPositionAndRotation(Vector3 p,Quaternion q){position=p;rotation=q;}}
 public struct Quaternion{}
 public struct Vector3{public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}public static Vector3 zero=>new Vector3();public static Vector3 up=>new Vector3(0,1,0);public float sqrMagnitude=>x*x+y*y+z*z;public float magnitude=>(float)Math.Sqrt(sqrMagnitude);public Vector3 normalized=>magnitude>0?this*(1/magnitude):new Vector3();public static Vector3 operator+(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator-(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);public static Vector3 operator*(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);public static Vector3 Lerp(Vector3 a,Vector3 b,float t)=>a+(b-a)*t;public static Vector3 ClampMagnitude(Vector3 a,float m)=>a.magnitude>m?a.normalized*m:a;}
 public struct Color{public Color(float a,float b,float c,float d=1){}}
 public class Material:Object{public Color color;}
 public enum PrimitiveType{Sphere}public static class Time{public static float deltaTime=.1f;public static int frameCount;}
 public static class Mathf{public const float PI=(float)Math.PI;public static float Sin(float v)=>(float)Math.Sin(v);public static float Max(float a,float b)=>Math.Max(a,b);public static int Min(int a,int b)=>Math.Min(a,b);public static float Clamp01(float v)=>Math.Clamp(v,0,1);}
}
namespace Emberfall{
 public static class BuildCatalog{public static float CinderTrailTickMultiplier(bool b)=>1;}
 public enum HeroClass{Vanguard,Arcanist,Ranger,Summoner}public static class CombatBalance{public static float RankPower(int rank)=>1+(rank-1)*.3f;}
 public class CastFirstHitReceipt{public void Release(){}}
 public struct CombatDamage{public float Amount;public static implicit operator CombatDamage(float f)=>new CombatDamage{Amount=f};}
 public class GameSession{public PlayerController Player;public bool HasStarted=true,CombatEnded,InputBlocked;public float ArenaRadius=30;public List<EnemyController> Enemies=new List<EnemyController>();}
 public class EnemyController{public bool IsDead,IsBoss;public float HitFootprintBonus,Damage;public Transform transform=new Transform();public void TakeDamage(float damage,Vector3 d,float k,float s,int practiceCastId=0){Damage+=damage;}}
 public static class CombatFx{public static Material NewGlow()=>new Material();public static Vector3 Flat(Vector3 v)=>new Vector3(v.x,0,v.z);public static void Ring(Vector3 v,float r,Color c,float d,float w){}}
 public static class CombatSight{public static bool Visible=true;public static bool Area(Vector3 a,Vector3 b)=>Visible;}
 public static class ProceduralVisuals{public static GameObject Create(string s,PrimitiveType p,Material m)=>new GameObject(s);}
 public static class ElementalCombatVfx{public enum Element{Fire}public static void OnEnemy(EnemyController e,Element v,float t){}}
 public enum SkillVisualRecipe{Neutral}public enum CombatVisualPriority{ActionBody}public static class SkillVisualRecipes{public static int Filled(SkillVisualRecipe r)=>0;}
 public static class FilledSkillVfx{public static void Impact(PlayerController p,Vector3 v,float r,int recipe,Color c,CombatVisualPriority pr){}}
 public static class WorldTraversal{public static float Wall=100;public static float SurfaceHeight(Vector3 v,float r)=>0;public static bool IsWalkable(Vector3 p,float radius)=>p.x<=Wall;public static bool HasGroundPath(Vector3 a,Vector3 b,float radius)=>a.x<=Wall&&b.x<=Wall;public static Vector3 Move(Vector3 from,Vector3 delta,float radius){var p=from+delta;p.x=Math.Min(p.x,Wall);return p;}}
 public enum SoundCue{Dodge}public static class GameAudio{public static void Play(SoundCue c){}}
 public static class PlayerUpgradeRules{DISTANCE}
 public class Stats{public float MoveSpeed=4;}public class Charge{public bool IsCharging;}
 public static class CombatArea{public static void Spawn(PlayerController p,GameSession g,Vector3 v,float r,CombatDamage d,float s,float start,float duration,float interval,Color c,int castId=0){}}
 public class PlayerController{
 public Transform transform=new Transform();public bool IsDead;public int CombatEpoch;private GameSession session;private Vector3 jumpOrigin,jumpDestination;private float jumpAge,invulnerability,rangerVaultRange;private bool jumping,rangerVault;private int traversalFrame,rangerVaultRank,rangerVaultCast;private CombatDamage rangerVaultDamage;
 private Stats stats=new Stats();private Charge charge;private bool TraversalStartedThisFrame=>false;private float movementSkillLock,passiveTime,passiveSpeed,mobilityTime,mobilityRank,pursuitTime,burnStrideTime;private float MovementMultiplier=>1;
 public int Hits,Buffs;public float Total;public PlayerController(GameSession g){session=g;g.Player=this;}public CastFirstHitReceipt RetainCastReceipt(int id)=>new CastFirstHitReceipt();public void RegisterSkillHit(int id){}private CombatDamage Damage(float v)=>v;
 private void MobilityBuff(int rank){Buffs++;}private void HitArea(Vector3 at,float r,CombatDamage d,float k,float s,int cast){Hits++;Total+=d.Amount;}
 METHODS
 public void Begin(int rank){BeginRangerVault(rank,1,SkillDamageBudgets.RangerVault(rank),1);}public void Tick(float dt){AdvanceJump(dt);}public bool Flying=>jumping;
 }
 public class Program{
 static void Check(bool v,string s){if(!v)throw new Exception(s);}
 static FlameRide Ride(GameSession g,PlayerController p,float duration){FlameRide.Spawn(p,g,duration,100,1);return (FlameRide)GameObject.parts[GameObject.parts.Count-1];}
 static void Tick(FlameRide f){typeof(FlameRide).GetMethod("Update",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(f,null);}
 public static void Main(){
 for(int rank=1;rank<=3;rank++){var g=new GameSession();var p=new PlayerController(g);p.Begin(rank);p.Tick(.275f);Check(p.Flying&&p.Hits==0&&p.transform.position.y>1.6f,"vault rises before damage");p.Tick(.275f);Check(!p.Flying&&p.Hits==1&&p.Buffs==1&&Math.Abs(p.transform.position.z-5)<.001,"vault lands and strikes once");Check(Math.Abs(p.Total-SkillDamageBudgets.RangerVault(rank))<.001,"vault budget applied once");p.Tick(1);Check(p.Hits==1,"no duplicate landing");}
 var game=new GameSession();var hero=new PlayerController(game);var e=new EnemyController();game.Enemies.Add(e);var ride=Ride(game,hero,6);
 Time.deltaTime=.5f;for(int i=0;i<12;i++)Tick(ride);Check(Math.Abs(e.Damage-216)<.01,"12 ticks, overlapping stationary footprints cannot triple damage");Tick(ride);Check(ride.gameObject.destroyed&&Math.Abs(e.Damage-216)<.01,"expiry denies extra endpoint tick");
 game=new GameSession();hero=new PlayerController(game);e=new EnemyController();game.Enemies.Add(e);ride=Ride(game,hero,6);game.InputBlocked=true;for(int i=0;i<20;i++)Tick(ride);Check(e.Damage==0,"paused ride does not age or deal damage");game.InputBlocked=false;CombatSight.Visible=false;Tick(ride);Check(e.Damage==0,"walls reject damage");CombatSight.Visible=true;hero.CombatEpoch++;Tick(ride);Check(ride.gameObject.destroyed&&e.Damage==0,"room epoch invalidates producer");
 game=new GameSession();hero=new PlayerController(game);e=new EnemyController();game.Enemies.Add(e);ride=Ride(game,hero,6);Tick(ride);hero.transform.position=new Vector3(0,0,2);Tick(ride);Check(e.Damage==36,"old footprint continues burning along travelled path");hero.IsDead=true;Tick(ride);Check(ride.gameObject.destroyed&&e.Damage==36,"death stops further damage");
 foreach(var direction in new[]{Vector3.zero,new Vector3(1,0,0),new Vector3(-1,0,0),new Vector3(0,0,1),new Vector3(0,0,-1),new Vector3(1,0,1)}){var jumpGame=new GameSession();var jumper=new PlayerController(jumpGame);Check(jumper.TryJump(direction),"normal jump starts");Check(!jumper.TryJump(direction),"airborne repeat rejected");jumper.Tick(.55f);var expected=Vector3.ClampMagnitude(direction,1)*2.2f;Check((jumper.transform.position-expected).sqrMagnitude<.002f,"moving jump follows supplied world direction, stationary remains in place");}
 WorldTraversal.Wall=1;var wallGame=new GameSession();var wallHero=new PlayerController(wallGame);Check(wallHero.TryJump(new Vector3(1,0,0)),"jump near wall starts");wallHero.Tick(.55f);Check(wallHero.transform.position.x<=1,"jump destination respects collision path");WorldTraversal.Wall=100;wallHero=new PlayerController(wallGame);wallHero.TryJump(new Vector3(1,0,0));wallHero.Tick(.2f);WorldTraversal.Wall=.9f;wallHero.Tick(.35f);Check(wallHero.transform.position.x<=.9f,"new obstruction cannot be bypassed at landing");WorldTraversal.Wall=100;
 Console.WriteLine("PASS normal directional/stationary jump and collision-boundary checks; actual vault landing + flame tick, overlap, wall, pause, path, expiry, death and epoch cases (engine doubles)");}
 }
}
'''
fixture=fixture.replace('METHODS',method(player,'        private void BeginRangerVault(')+method(player,'        private void AdvanceJump(')+method(player,'        internal bool TryJump('))
fixture=fixture.replace('DISTANCE',method((root/'Assets/Scripts/Combat/PlayerUpgradeRules.cs').read_text(),'        public static float FindSafeBlinkDistance('))
with tempfile.TemporaryDirectory(prefix='EmberfallMovementRound-') as d:
 p=Path(d);(p/'Program.cs').write_text(fixture)
 sources=[root/'Assets/Scripts/Combat/FlameRide.cs',root/'Assets/Scripts/Core/SkillDamageBudgets.cs']
 (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework></PropertyGroup><ItemGroup>'+''.join('<Compile Include="'+str(s)+'" />' for s in sources)+'</ItemGroup></Project>')
 (p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 os.environ.setdefault('DOTNET_CLI_HOME',str(p/'dotnet-home'))
 subprocess.run([sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet'),'run','--project',str(p/'Test.csproj'),'--configuration','Release'],check=True)
