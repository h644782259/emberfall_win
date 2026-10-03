// Production components run intact; Unity rendering/objects and the final player damage recipient are managed substitutes.
using System;using System.Collections.Generic;using System.Reflection;using Emberfall;using UnityEngine;
namespace UnityEngine
{
    public class Object {public bool Destroyed;public static void Destroy(Object value){if(value==null)return;value.Destroyed=true;if(value is GameObject go)go.SetActive(false);}}
    public class MonoBehaviour:Object
    {public GameObject gameObject=new GameObject();public Transform transform=>gameObject.transform;public bool enabled=true;public T GetComponent<T>() where T:class=>gameObject.GetComponent<T>();}
    public class GameObject:Object
    {
        public string name;public bool activeInHierarchy=true;public Transform transform=new Transform();private readonly Dictionary<Type,object> components=new Dictionary<Type,object>();
        public GameObject(string name="object"){this.name=name;}
        public void SetActive(bool value){activeInHierarchy=value;}
        public T AddComponent<T>() where T:new(){var value=new T();if(value is MonoBehaviour mb)mb.gameObject=this;components[typeof(T)]=value;return value;}
        public T GetComponent<T>() where T:class {return components.TryGetValue(typeof(T),out var value)?value as T:null;}
    }
    public class Transform {public Vector3 forward=>Vector3.forward;public Vector3 position,localPosition,localScale;public Quaternion localRotation;public GameObject gameObject;public Transform(){ }public void SetParent(Transform parent,bool world){} }
    public struct Color{public float r,g,b,a;public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}}
    public enum PrimitiveType{Cylinder,Cube,Capsule}
    public class Shader:Object{public static Shader Find(string n)=>new Shader();}
    public class Mesh:Object{public string name;public Vector3[] vertices;public int[] triangles;public void RecalculateNormals(){}public void RecalculateBounds(){}}
    public class MeshFilter:MonoBehaviour{public Mesh sharedMesh;}public class MeshRenderer:MonoBehaviour{public Material sharedMaterial;}
    public class Material:Object{public Color color;public int renderQueue;public Material(){}public Material(Shader shader){}}
    public static class Resources{public static T Load<T>(string path) where T:class{return null;}}
    public class LineRenderer:MonoBehaviour{public Material sharedMaterial;public bool useWorldSpace,loop;public int positionCount,numCapVertices,sortingOrder;public float widthMultiplier;public Color startColor,endColor;public Rendering.ShadowCastingMode shadowCastingMode;public readonly Dictionary<int,Vector3> points=new Dictionary<int,Vector3>();public void SetPosition(int i,Vector3 p){points[i]=p;}public void SetPositions(Vector3[] values){for(int i=0;i<values.Length;i++)points[i]=values[i];}}
    public struct Quaternion{float yaw;public static Quaternion Euler(float x,float y,float z){return new Quaternion{yaw=y*Mathf.Deg2Rad};}public static Vector3 operator *(Quaternion q,Vector3 v){return new Vector3(v.x*Mathf.Cos(q.yaw)+v.z*Mathf.Sin(q.yaw),v.y,-v.x*Mathf.Sin(q.yaw)+v.z*Mathf.Cos(q.yaw));}}
    public static partial class Time{public static float deltaTime=.05f;}
    public partial struct Vector2{public static Vector2 zero=>new Vector2();}
    public partial struct Vector3{public static Vector3 up=>new Vector3(0,1,0);public static Vector3 forward=>new Vector3(0,0,1);public static Vector3 operator -(Vector3 a)=>a*-1;public static Vector3 Cross(Vector3 a,Vector3 b)=>new Vector3(a.y*b.z-a.z*b.y,a.z*b.x-a.x*b.z,a.x*b.y-a.y*b.x);}
    public static partial class Mathf{public const float Deg2Rad=(float)(Math.PI/180),Rad2Deg=1/Deg2Rad;public static float Clamp01(float f)=>Clamp(f,0,1);public static float Atan2(float x,float y)=>(float)Math.Atan2(x,y);}
}
namespace UnityEngine.Rendering{public enum ShadowCastingMode{Off}}
namespace Emberfall
{
    public partial class GameSession
    {public static GameSession Instance;public bool HasStarted=true,ChapterActive=true,ChapterFinished,InputBlocked,IsDead,ModeFinished;public PlayerController Player=new PlayerController();public int ChapterRoomIndex,ChapterSeed,MasteryAnchorEvents,MasteryInterruptEvents;public void RecordChapterInterrupt(EnemyController e){MasteryInterruptEvents++;}public void RecordChapterAnchorExposure(EnemyController e){MasteryAnchorEvents++;}public ChapterNode ActiveChapterNode;public ProgressionService Progression=new ProgressionService();public List<string> Logs=new List<string>();public void LogSystem(string s){Logs.Add(s);}public void SpawnMechanismText(Vector3 p,string s,Color c){} }
    public class PlayerController:MonoBehaviour {
        // Explicit cast birth in this scheduler host; contact lookup never mints a receipt.
        readonly CastFirstHitRegistry casts=new CastFirstHitRegistry();CastFirstHitReceipt liveCast;
        public void PrimeSkillCast(int id){casts.SetEpoch(CombatEpoch);liveCast=casts.Issue(id);}
        public CastFirstHitReceipt CaptureCastReceipt(int id){casts.SetEpoch(CombatEpoch);return casts.Find(id);}
public HeroClass HeroClass=HeroClass.Vanguard;public bool IsDead;public int CombatEpoch=1,DamageCalls;public float MaxHealth=100,Health=100;public void TakeDamageFrom(float value,string reason){Health-=value;DamageCalls++;}}
    public partial class EnemyController:MonoBehaviour{public bool IsBoss=true,IsDead,IsStunned;public bool CanBeSkillInterrupted=true;public float Health=70,MaxHealth=100,AttackDamage=20;public int BeginCalls;public void ConfigureLargeExpedition(LargeExpeditionBoss boss){largeBoss=boss;session=GameSession.Instance;}public void BeginLargeBossMechanic(){BeginCalls++;attackNumber++;}}
    public partial class ProgressionService{public Profile Profile=new Profile();}public class Profile{public int level=1;}
    public enum VisualSurface{Metal,Crystal}public enum DestructibleKind{Crate}public enum PropRecovery{None}
    public class WorldResources:MonoBehaviour{public Material Material(Color c,bool glow,VisualSurface surface)=>new Material();}
    public static class LargeBossRig
    {public static Transform Joint(Transform parent,string name,Vector3 p){var go=new GameObject(name);go.transform.gameObject=go;go.transform.position=p;return go.transform;}public static Transform Part(Transform t,string n,PrimitiveType type,Vector3 p,Vector3 size,Material m)=>Joint(t,n,p);}
    public class DestructibleProp:MonoBehaviour{public static bool AllowPlacement=true;public bool Broken,PlayerBroken;public bool WasBrokenBy(PlayerController owner,int epoch)=>Broken&&PlayerBroken&&owner!=null&&owner.CombatEpoch==epoch;public static bool CanPlace(Vector3 p,float radius)=>AllowPlacement&&WorldTraversal.IsWalkable(p,radius);public void Initialize(DestructibleKind k,int level,float radius,bool loot,PropRecovery r,Transform model,Material m){} }
    public static class EffectPreferences{public static bool ReducedEffects;}
    public static partial class CombatFx{public static Material NewGlow()=>new Material();public static void Ring(Vector3 p,float r,Color c,float t,float w){} }
}
public static class ChapterCombatProductionTests
{
    static int checks;
    static void Check(bool okay,string why){checks++;if(!okay)throw new Exception(why);}
    static T Field<T>(object o,string field)=>(T)o.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(o);
    static void Update(object o)=>o.GetType().GetMethod("Update",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(o,null);
    static GameSession Game(ChapterNode node=ChapterNode.StarPlatform){WorldTraversal.Reset(ZoneKind.Dungeon);DestructibleProp.AllowPlacement=true;return GameSession.Instance=new GameSession{ActiveChapterNode=node};}
    static void Steps(LargeExpeditionBoss boss,int steps){for(int i=0;i<steps;i++)boss.Tick(.05f);}
    public static string Run()
    {
        checks=0;
        // Exact component configuration is exercised, including legacy non-chapter opt-out.
        var game=Game();var enemy=new EnemyController();game.Player.transform.position=new Vector3(0,0,5);
        var normal=LargeExpeditionBoss.Configure(enemy,1,0);normal.Tick(.05f);Steps(normal,390);
        Check(enemy.BeginCalls==1&&!normal.State.IsFollowup,"legacy default must not add followup");
        Check(normal.State.CurrentBeamLength==9f&&normal.State.CurrentBeamDegreesPerSecond==24f,"legacy five room length and speed unchanged");
        foreach(var difficulty in new[]{ChapterDifficulty.Normal,ChapterDifficulty.Hard,ChapterDifficulty.Heroic})
        {
            game=Game();game.Player.transform.position=new Vector3(0,0,5);enemy=new EnemyController();
            var boss=LargeExpeditionBoss.ConfigureChapter(enemy,1,0,difficulty);boss.Tick(.05f);
            if(difficulty==ChapterDifficulty.Normal)Check(boss.State.CurrentBeamLength==9f&&boss.State.CurrentBeamDegreesPerSecond==24f,"normal chapter length and speed unchanged");
            Check(boss.State.Phase==LargeBossPhase.Windup&&boss.State.PhaseNumber==1,"chapter phase starts at original 70 percent threshold");
            Check(boss.State.Remaining>2.1f,"chapter starts with full warning");
            float angle=boss.BeamWorldAngle;game.InputBlocked=true;Steps(boss,100);
            Check(boss.State.Remaining>2.1f&&boss.BeamWorldAngle==angle,"blocked component freezes phase clock");game.InputBlocked=false;
            Steps(boss,360); // 18 seconds: original beam complete and recovery almost done.
            while(boss.State.Phase==LargeBossPhase.Recovery)boss.Tick(.05f);
            if(difficulty==ChapterDifficulty.Normal)Check(enemy.BeginCalls==1&&!boss.State.IsFollowup,"chapter Normal keeps original single sequence");
            else
            {
                Check(enemy.BeginCalls==2&&boss.State.IsFollowup&&boss.State.Phase==LargeBossPhase.Windup,"hard component must create exactly one full warned followup");
                Check(boss.State.Remaining==2.2f&&boss.State.LiveAnchorMask==0&&!boss.State.DamagePulse,"followup never creates new anchors or immediate damage");
                Steps(boss,40);Check(boss.State.Phase==LargeBossPhase.Windup,"followup retains readable 2.2 second warning");
                float locked=boss.BeamWorldAngle;Check(Math.Abs(locked+48f)<.01f,"followup warning begins bearing minus 48 degrees");game.Player.transform.position=new Vector3(5,0,0);
                Steps(boss,4);Check(boss.State.Phase==LargeBossPhase.Beam&&Math.Abs(boss.BeamWorldAngle-locked)<.01f,"followup releases at its locked warning bearing");
                Steps(boss,79);Check(boss.State.Phase==LargeBossPhase.Beam&&Math.Abs(boss.BeamWorldAngle-(locked+94.8f))<.02f,"followup sweeps bounded 96 degrees over four seconds");
                Steps(boss,230);Check(enemy.BeginCalls==2&&!boss.State.OwnsAttacks,"followup never chains indefinitely");
            }
        }
        game=Game();enemy=new EnemyController();var clock=LargeExpeditionBoss.ConfigureChapter(enemy,1,0,ChapterDifficulty.Hard);int ticks=0;
        do{clock.Tick(.05f);ticks++;}while(clock.State.OwnsAttacks&&ticks<1000);
        Check(Math.Abs(ticks*.05f-26.4f)<.06f,"uncountered chapter threshold retains 26.4 seconds complete clock");
        foreach(var difficulty in new[]{ChapterDifficulty.Hard,ChapterDifficulty.Heroic}) {
            game=Game();enemy=new EnemyController();var interrupted=LargeExpeditionBoss.ConfigureChapter(enemy,1,0,difficulty);
            do{interrupted.Tick(.05f);}while(!interrupted.State.IsFollowup);
            interrupted.InterruptWindup();Check(game.MasteryInterruptEvents==1,"F completed real interrupt records exactly once");Check(interrupted.State.Phase==LargeBossPhase.Recovery&&interrupted.State.Remaining==2f&&interrupted.State.IncomingMultiplier==1,"followup interrupt is recovery not exposure");
        }
        for(int mode=0;mode<4;mode++)
        {
            game=Game();enemy=new EnemyController();DestructibleProp.AllowPlacement=mode!=3;
            var mastery=LargeExpeditionBoss.ConfigureChapter(enemy,1,0,ChapterDifficulty.Hard);mastery.Tick(.05f);
            var props=Field<DestructibleProp[]>(mastery,"anchors");
            foreach(var prop in props)if(prop!=null){prop.Broken=true;prop.PlayerBroken=mode==0;}
            if(mode==2&&props[0]!=null)props[0].gameObject.SetActive(false);
            mastery.Tick(.05f);
            Check(game.MasteryAnchorEvents==(mode==0?1:0),"F only player-proven anchor exposure grants evidence, never disappearance or failed spawn");
            mastery.Tick(.05f);Check(game.MasteryAnchorEvents==(mode==0?1:0),"F exposure evidence bounded once per actual phase");
        }
        game=Game();game.Player.transform.position=new Vector3(0,0,5);enemy=new EnemyController();var complete=LargeExpeditionBoss.ConfigureChapter(enemy,1,0,ChapterDifficulty.Hard);Steps(complete,900);
        Check(enemy.BeginCalls==2&&complete.State.Phase==LargeBossPhase.Combat,"unbroken hard sequence ends after one followup");
        enemy.Health=35;Steps(complete,900);
        Check(enemy.BeginCalls==4&&complete.State.PhaseNumber==2&&complete.State.Phase==LargeBossPhase.Combat,"35 percent gets one bounded followup and no third health phase");
        game=Game();game.Player.transform.position=new Vector3(0,0,5);enemy=new EnemyController();var reverse=LargeExpeditionBoss.ConfigureChapter(enemy,1,0,ChapterDifficulty.Heroic);reverse.Tick(.05f);
        float initial=reverse.BeamWorldAngle;
        var arrows=Field<LineRenderer[]>(reverse,"beamLines");Vector3 direction=Quaternion.Euler(0,initial,0)*Vector3.forward;
        Vector3 arrowHead=(arrows[3].points[0]+arrows[3].points[2])*.5f;
        Check(Vector3.Dot(arrows[3].points[1]-arrowHead,Vector3.Cross(Vector3.up,direction))<0,"heroic warning arrow matches actual reverse beam sign");
        Steps(reverse,50);
        Check(reverse.BeamWorldAngle<initial&&game.Logs.Exists(s=>s.Contains("逆时针")),"heroic actual beam follows the announced reverse direction");
        game=Game();game.Player.transform.position=new Vector3(0,0,5);enemy=new EnemyController();var hard=LargeExpeditionBoss.ConfigureChapter(enemy,1,0,ChapterDifficulty.Hard);hard.Tick(.05f);hard.InterruptWindup();Check(hard.State.Phase==LargeBossPhase.Recovery&&hard.State.Remaining==2f&&hard.State.IncomingMultiplier==1,"chapter interrupt gives two seconds recovery without vulnerability");Steps(hard,600);
        Check(enemy.BeginCalls==1&&hard.State.Phase==LargeBossPhase.Combat,"interrupt cancels pending followup without new attacks");
        foreach(var counterDifficulty in new[]{ChapterDifficulty.Hard,ChapterDifficulty.Heroic}) foreach(bool bossFirst in new[]{true,false})
        {
            game=Game();enemy=new EnemyController();var atomic=LargeExpeditionBoss.ConfigureChapter(enemy,1,0,counterDifficulty);atomic.Tick(.05f);
            var actualAnchors=(DestructibleProp[])Field<DestructibleProp[]>(atomic,"anchors").Clone();
            actualAnchors[0].Broken=true;actualAnchors[1].Broken=true;atomic.Tick(.05f);
            Check(atomic.State.LiveAnchorMask==4,"two prior broken anchors leave exactly the last anchor");
            CombatImpactBatch.Begin();try {
                game.Player.PrimeSkillCast(91);
                if(bossFirst){Check(enemy.TrySkillInterrupt(game.Player,1,91),"real hard heroic attack qualifies before anchor target");enemy.TakeDamage(10,Vector3.forward,impact:false);}
                for(int i=0;i<3;i++)actualAnchors[i].Broken=true;
                if(!bossFirst){Check(enemy.TrySkillInterrupt(game.Player,1,91),"real hard heroic attack qualifies after anchor target");enemy.TakeDamage(10,Vector3.forward,impact:false);}
            } finally { CombatImpactBatch.End(); }
            Check(atomic.State.Phase==LargeBossPhase.Exposed&&atomic.State.Remaining==6f&&atomic.State.IncomingMultiplier==1.35f,"same attack last anchor break wins over interrupt in both target orders");
            Check(game.MasteryInterruptEvents==0,"F anchor-winning batch does not pretend a separate actual interrupt");
            Steps(atomic,700);Check(enemy.BeginCalls==1,"anchor counter cancels the threshold followup");
        }
        game=Game();enemy=new EnemyController();DestructibleProp.AllowPlacement=false;hard=LargeExpeditionBoss.ConfigureChapter(enemy,1,0,ChapterDifficulty.Hard);hard.Tick(.05f);
        Check(hard.State.Phase==LargeBossPhase.Exposed,"no safe anchors degrades to exposure");
        game.InputBlocked=true;game.Player.CombatEpoch++;hard.Tick(.05f);
        Check(hard.State.Phase==LargeBossPhase.Finished,"chapter old owner retires before blocked simulation");
        for(int seed=0;seed<4;seed++)for(int phase=1;phase<=2;phase++)
        {Check(ChapterBossPattern.StartOffset(false,seed,phase)==35&&ChapterBossPattern.SweepSign(false,seed,phase)==1,"legacy angle preserved");Check(ChapterBossPattern.SweepSign(true,seed,phase)==-ChapterBossPattern.SweepSign(true,seed,3-phase),"heroic phase direction changes reproducibly");}
        foreach(var node in new[]{ChapterNode.ForestCourt,ChapterNode.Redrock})
        {
            game=Game(node);var plan=ChapterRoomGeometry.Plan(node,0,0);
            foreach(var obstacle in plan.Obstacles){if(obstacle.Radius>0)WorldTraversal.AddCircle(obstacle.Center,obstacle.Radius);else WorldTraversal.AddBox(obstacle.Center,obstacle.Size);}
            Check(ChapterHazards.Configure(game,node,ChapterDifficulty.Hard,0,0,plan)==null,"non heroic rooms have no new floor hazard");
            var hazard=ChapterHazards.Configure(game,node,ChapterDifficulty.Heroic,0,0,plan);Check(hazard!=null,"heroic room gets one optional hazard");
            var center=Field<Vector3>(hazard,"center");game.Player.transform.position=center;
            var bodies=Field<Transform[]>(hazard,"bodies");int retained=0;
            foreach(var body in bodies)if(body!=null)
            {
                retained++;Check(body.localScale.y==.025f,"heroic body starts dormant rather than faking active damage");
                foreach(var vertex in Field<Mesh>(hazard,"bodyMesh").vertices)
                {
                    var point=body.position+new Vector3(vertex.x*.24f,0,vertex.z*.24f);
                    bool inside=node==ChapterNode.ForestCourt?ArenaPulseRules.Contains(CombatFx.Flat(point-center).sqrMagnitude,CombatSight.Area(center,point)):CombatFx.SegmentDistance(point,Field<Vector3>(hazard,"start"),Field<Vector3>(hazard,"end"))<=ChapterHazardGeometry.HeatHalfWidth&&CombatSight.Area(body.position,point);
                    Check(inside,"heroic body footprint stays inside actual clipped hazard region");
                }
            }
            Check(retained>0&&retained<=(node==ChapterNode.ForestCourt?8:6),"heroic physical feedback is nonempty and bounded");

            game.InputBlocked=true;for(int i=0;i<200;i++)Update(hazard);
            Check(Field<float>(hazard,"age")==0&&game.Player.DamageCalls==0,"blocked chapter hazard must preserve clock and health");
            game.InputBlocked=false;for(int i=0;i<120;i++)Update(hazard);
            Check(game.Player.DamageCalls==0&&Field<LineRenderer>(hazard,"boundary").enabled,"visible warning precedes hazard damage");
            Check(bodies[0].localScale.y>.025f&&bodies[0].localScale.y<1.05f,"physical hazard grows during warning before active damage");
            for(int i=0;i<8;i++)Update(hazard);
            Check(game.Player.DamageCalls==1&&game.Player.Health==93.5f,"single active window damages at most once");
            Check(bodies[0].localScale.y==1.05f,"physical thorn or heat body reaches active height on actual damage window");
            if(node==ChapterNode.Redrock)Check(Vector3.Distance(Field<Vector3>(hazard,"end"),ChapterHazardGeometry.ClipLine(plan.HazardStart,plan.HazardEnd))<.0001f&&Vector3.Distance(plan.HazardStart,Field<Vector3>(hazard,"end"))<Vector3.Distance(plan.HazardStart,plan.HazardEnd),"actual rock clips single heat line and damage endpoint");
            game.Player.transform.position=new Vector3(12,0,12);for(int i=0;i<140;i++)Update(hazard);
            Check(game.Player.DamageCalls==1,"outside hazard remains safe on later cycle");
            Check(bodies[0].localScale.y<.15f,"physical hazard retracts through cooldown without new damage");
            game.InputBlocked=true;game.ChapterFinished=true;Time.deltaTime=0;Update(hazard);
            Check(!hazard.gameObject.activeInHierarchy&&Field<bool>(hazard,"retired"),"finished chapter retires hazard at zero delta while blocked");Time.deltaTime=.05f;
            hazard.GetType().GetMethod("OnDestroy",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(hazard,null);
            Check(Field<Mesh>(hazard,"bodyMesh").Destroyed&&Field<Material>(hazard,"bodyMaterial").Destroyed,"hazard retirement releases owned static body mesh and material");
        }
        foreach(var node in new[]{ChapterNode.ForestCourt,ChapterNode.Redrock})foreach(float offset in new[]{-.001f,0,.001f})
        {
            game=Game(node);var plan=ChapterRoomGeometry.Plan(node,0,0);
            foreach(var obstacle in plan.Obstacles){if(obstacle.Radius>0)WorldTraversal.AddCircle(obstacle.Center,obstacle.Radius);else WorldTraversal.AddBox(obstacle.Center,obstacle.Size);}
            var hazard=ChapterHazards.Configure(game,node,ChapterDifficulty.Heroic,0,0,plan);
            Vector3 center=Field<Vector3>(hazard,"center");
            game.Player.transform.position=center+(node==ChapterNode.ForestCourt?new Vector3(ArenaPulseRules.Radius+offset,0,0):new Vector3(0,0,ChapterHazardGeometry.HeatHalfWidth+offset));
            for(int i=0;i<128;i++)Update(hazard);
            Check(game.Player.DamageCalls==(offset<=0?1:0),"actual hazard footprint matches circle or capsule boundary at plus/minus .001");
        }
        game=Game(ChapterNode.ForestCourt);var old=ChapterHazards.Configure(game,ChapterNode.ForestCourt,ChapterDifficulty.Heroic,0,0,ChapterRoomGeometry.Plan(ChapterNode.ForestCourt,0,0));game.InputBlocked=true;game.Player.CombatEpoch++;Update(old);
        Check(!old.gameObject.activeInHierarchy,"old epoch hazard retires before pause gate");
        foreach(int mode in new[]{0,1,2,3}) {
            var combat=Game();var victim=new EnemyController();var encounter=mode==0?LargeExpeditionBoss.Configure(victim,1,0):LargeExpeditionBoss.ConfigureChapter(victim,1,0,(ChapterDifficulty)(mode-1));encounter.Tick(.05f);
            float before=victim.Health;
            CombatImpactBatch.Begin();try {
                combat.Player.PrimeSkillCast(101);
                Check(victim.TrySkillInterrupt(combat.Player,1,101),"actual qualifying controller skill interrupts windup");
                victim.TakeDamage(10,Vector3.forward,impact:false);
                Check(Math.Abs(before-victim.Health-(mode<2?13.5f:10f))<.001f,"legacy and normal triggering hit retain immediate 1.35 multiplier; hard heroic defer without vulnerability");
            } finally { CombatImpactBatch.End(); }
            Check(encounter.State.Phase==(mode<2?LargeBossPhase.Exposed:LargeBossPhase.Recovery),"attack resolution retains mode-specific counterplay");
        }
        // Whole production component: configuration, phase advancement, drawn footprint and damage recipient.
        foreach(var difficulty in new[]{ChapterDifficulty.Hard,ChapterDifficulty.Heroic})
        foreach(var hero in new[]{HeroClass.Vanguard,HeroClass.Arcanist,HeroClass.Ranger,HeroClass.Summoner})
        {
            var trialGame=Game();trialGame.Player.HeroClass=hero;trialGame.Player.transform.position=new Vector3(0,0,5);
            var trialEnemy=new EnemyController();var trial=LargeExpeditionBoss.ConfigureChapter(trialEnemy,1,0,difficulty);trial.Tick(.05f);
            var lines=Field<LineRenderer[]>(trial,"beamLines");
            // Boundary endcap crown (j=0) includes the unchanged .55 + .4 player-center compensation.
            Check(Math.Abs(CombatFx.Flat(lines[1].points[0]).magnitude-14.95f)<.002f,"star main warning and damage endpoint share trial length");
            while(trial.State.Phase==LargeBossPhase.Windup)trial.Tick(.05f);
            Check(trial.State.Remaining==14f,"star main retains fourteen second duration");
            float bearing=trial.BeamWorldAngle;trial.Tick(.05f);
            Check(Math.Abs(Math.Abs(trial.BeamWorldAngle-bearing)-.9f)<.002f,"star main uses independent 18 degree speed");
            foreach(float distance in new[]{2f,10f,12f,14.94f,14.96f,16f})
            {
                int damage=trialGame.Player.DamageCalls;
                do {
                    float sign=difficulty==ChapterDifficulty.Heroic?-1:1;
                    trialGame.Player.transform.position=Quaternion.Euler(0,trial.BeamWorldAngle+sign*.9f,0)*Vector3.forward*distance;
                    trial.Tick(.05f);
                } while(!trial.State.DamagePulse);
                Check(trialGame.Player.DamageCalls==damage+(distance<=14.95f?1:0),"actual star damage includes body endcap and rejects beyond compensated endpoint");
            }
            // A pillar on the current ray clips both warning and live beam before its solid surface.
            var ray=Quaternion.Euler(0,trial.BeamWorldAngle,0)*Vector3.forward;
            WorldTraversal.AddCircle(ray*7f,1f);trialGame.Player.transform.position=ray*10;
            trial.Tick(.00001f);
            Check(CombatFx.Flat(lines[1].points[0]).magnitude<6.01f,"pillar clips compensated warning and damage capsule");
            int sheltered=trialGame.Player.DamageCalls;
            for(int i=0;i<14;i++)trial.Tick(.05f);
            Check(trialGame.Player.DamageCalls==sheltered,"pillar shelter prevents actual pulse damage");
            WorldTraversal.Reset(ZoneKind.Dungeon);
            while(!trial.State.IsFollowup)trial.Tick(.05f);
            Check(trial.State.CurrentBeamLength==9f&&trial.State.CurrentBeamDegreesPerSecond==24f,"followup preserves nine metre length and 24 degree speed");
            trialEnemy.IsDead=true;trialGame.InputBlocked=true;trial.Tick(0);
            Check(trial.State.Phase==LargeBossPhase.Finished&&Field<GameObject>(trial,"beamRoot")==null,"death retires beam before blocked zero-delta tick");
        }
        foreach(var pillar in ChapterRoomGeometry.Plan(ChapterNode.StarPlatform,0,0).Obstacles)
        {
            var mapGame=Game();ChapterRoomGeometry.Register(ChapterRoomGeometry.Plan(ChapterNode.StarPlatform,0,0));
            var mapEnemy=new EnemyController();mapEnemy.transform.position=new Vector3(0,0,1);
            Vector3 ray=(pillar.Center-mapEnemy.transform.position).normalized;
            mapGame.Player.transform.position=mapEnemy.transform.position+(Quaternion.Euler(0,-35,0)*ray)*5;
            var mapBoss=LargeExpeditionBoss.ConfigureChapter(mapEnemy,1,0,ChapterDifficulty.Hard);mapBoss.Tick(.05f);
            Check(mapBoss.State.Phase==LargeBossPhase.Windup,"real star map retains safe anchor placements");
            var endcap=Field<LineRenderer[]>(mapBoss,"beamLines")[1].points[0];
            Check(CombatFx.Flat(endcap-mapEnemy.transform.position).magnitude<(pillar.Center-mapEnemy.transform.position).magnitude-pillar.Radius+.03f,"real star pillars clip warning before solid surface from actual boss spawn");
        }
        foreach(var node in new[]{ChapterNode.ForestCourt,ChapterNode.Redrock})
        {
            Game(node);var unchanged=LargeExpeditionBoss.ConfigureChapter(new EnemyController(),1,0,ChapterDifficulty.Hard);
            Check(unchanged.State.CurrentBeamLength==9f&&unchanged.State.CurrentBeamDegreesPerSecond==24f,"trial never changes other chapter nodes");
        }
        return "PASS: "+checks+" chapter component/pattern checks (managed substitutes; not Unity play)";
    }
}
