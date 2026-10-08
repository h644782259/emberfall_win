using System;using System.Linq;using System.Collections.Generic;using System.Reflection;using UnityEngine;using Emberfall;
namespace Emberfall {
 // This suite exercises the retained procedural fallback; resource readiness has separate coverage.
 public static class AuthoredFixedScenery {public static void Apply(GameObject g,string n,PrimitiveType t){}public static bool Frame(GameObject g,float r,Material m)=>false;public static void Crest(Transform p,string n,Vector3 at,Material m){}}
 public static class BlenderSceneryArt {public static GameObject Create(string n,Transform p,Vector3 at,WorldResources r)=>null;public static GameObject CreatePilotProp(string n,Transform p,Vector3 at)=>null;}

 // Observe production material-category arguments; WorldResources and ApplySurface execute unchanged apart from this trace.
 internal static class SceneryTrace {internal static readonly Dictionary<Material,VisualSurface> surfaces=new Dictionary<Material,VisualSurface>();internal static void Record(Material m,VisualSurface s){surfaces[m]=s;}}
 // Traversal storage is a recording boundary, not a duplicate pathfinder. The real
 // HubSettlementPlan produces every registration; reachability has its existing geometry suite.
 public static class WorldTraversal {
  public static readonly List<(Vector3 p,Vector2 size)> boxes=new List<(Vector3,Vector2)>();public static readonly List<(Vector3 p,float radius)> circles=new List<(Vector3,float)>();
  public static void Reset(ZoneKind zone){boxes.Clear();circles.Clear();}public static void AddBox(Vector3 p,Vector2 s){boxes.Add((p,s));}public static void AddCircle(Vector3 p,float r){circles.Add((p,r));}
 }
 public static class CombatFx{public static Material NewGlow()=>new Material(Shader.Find("test"));}
 public enum WingSilhouette{Mechanical}public static class CostumeMeshLibrary{public static Mesh Get(WingSilhouette s)=>ProceduralVisuals.Shape(PrimitiveType.Cylinder);}
 public class FashionOrbit:MonoBehaviour{}public class WorldMotion:MonoBehaviour{public Vector3 spin;public float bob,speed;}
 public static class BlenderPilotArt{public static GameObject CreateProp(string id,Transform p,Vector3 at)=>null;}
 public static class ChapterRoomGeometry{public static bool IsChapterLayout(int n)=>false;public static object FromLayout(int n,int seed)=>null;}
 public static partial class WorldBuilder {
  // Explicit boundaries: no claim that these unrelated layout builders run here.
  static void BuildClimbableProps(Transform p,WorldResources r){}static void BuildTravelStation(Transform p,WorldResources r,int n){}
  static void BuildWilderness(Transform p,WorldResources r){}static void BuildCampFacilities(Transform p,WorldResources r,int n){}
  static void BuildChapterRoom(Transform p,WorldResources r,object layout){}static void BuildTacticalRoom(Transform p,WorldResources r,int n){}static void BuildLinkedRoom(Transform p,WorldResources r,int n){}static void BuildChallengeArena(Transform p,WorldResources r,int n){}static void BuildDungeon(Transform p,WorldResources r,int n){}static void BuildBreakablePockets(Transform p,int n){}
  public static void TestTree(Transform p,WorldResources r,int seed)=>Tree(p,r,new Vector3(5,0,5),1,seed);
  public static void TestTent(Transform p,WorldResources r)=>Tent(p,r,Vector3.zero);
  public static void TestGround(Transform p,WorldResources r,int hub)=>BuildTownGroundDetail(p,r,hub);
  public static void TestBank(Transform p,WorldResources r)=>BuildWaterBankDetail(p,r,new[]{new Vector3(5,0,-2),new Vector3(10,0,4)});
  public static void TestFocus(Transform p,WorldResources r)=>BuildPortalFocus(p,r,Vector3.zero);
  public static void TestPools(Transform p,int hub)=>BuildHubLightPools(p,hub);
  public static void TestNpcs(Transform p,WorldResources r)=>BuildHubNpcs(p,r);
 }
}
class SceneryPresentationProductionTests {
 static int n;static void Check(bool b,string why){n++;if(!b)throw new Exception(why);}
 static bool Near(Vector3 a,Vector3 b)=>(a-b).magnitude<.0002f;static bool Near(float a,float b)=>Math.Abs(a-b)<.0002f;
 static List<CameraOcclusionSurface> Registry=>(List<CameraOcclusionSurface>)typeof(CameraOcclusionSurface).GetField("surfaces",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
 static object Field(object o,string s)=>o.GetType().GetField(s,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);
 static void SetField(object o,string name,object value)=>o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,value);
 static Renderer[] Visuals(GameObject o)=>o.GetComponentsInChildren<Renderer>(false).Where(r=>!(r is LineRenderer)).ToArray();
 static Transform[] Named(GameObject o,string s)=>o.GetComponentsInChildren<Transform>(false).Where(t=>t.name==s).ToArray();
 static VisualSurface Surface(Renderer r)=>SceneryTrace.surfaces[r.sharedMaterial];
 static GameObject Host(string name)=>new GameObject(name);static WorldResources Resources(GameObject o)=>o.AddComponent<WorldResources>();
 static void Dispose(GameObject o){CameraOcclusionSurface.RestoreAll();o.SetActive(false);UnityEngine.Object.Destroy(o);UnityEngine.Object.Flush();Check(Registry.Count==0,"scene teardown releases actual registry entries");}
 static void Environment(){int start=n;
  foreach(int seed in new[]{-1,0,1,2}){
   var host=Host("tree categories");var r=Resources(host);WorldTraversal.Reset(ZoneKind.Wilderness);WorldBuilder.TestTree(host.transform,r,seed);
   var leaf=Visuals(host).Where(v=>v.transform.name=="Open canopy leaf fan").ToArray();var limbs=Visuals(host).Except(leaf).ToArray();
   Check(leaf.Length==5&&limbs.Length==11,"actual tree contains canopy fans and branches");
   Check(leaf.All(v=>Surface(v)==VisualSurface.Foliage)&&limbs.All(v=>Surface(v)==VisualSurface.Wood),"actual tree assigns Foliage and Wood categories");
   Check(limbs.All(v=>Near(v.sharedMaterial.GetFloat("_Glossiness"),.23f))&&leaf.All(v=>Near(v.sharedMaterial.GetFloat("_Glossiness"),.1f)),"actual tree category reaches shader material settings");
   Check(WorldTraversal.circles.Count==1&&Near(WorldTraversal.circles[0].radius,.22f)&&Registry.Count==1,"actual tree preserves one trunk solid and one hierarchy registry slot");Dispose(host);
  }
  {var host=Host("tent and bridge");var r=Resources(host);WorldBuilder.TestTent(host.transform,r);WorldBuilder.TestBridge(host.transform,r);
   var tents=Named(host,"Camp tent");var planks=Named(host,"Timber crossing plank");Check(tents.Length==2&&tents.All(t=>Surface(t.GetComponent<Renderer>())==VisualSurface.Cloth),"procedural tent fallback uses Cloth category");
   Check(planks.Length==10&&planks.All(t=>Surface(t.GetComponent<Renderer>())==VisualSurface.Wood),"actual timber bridge uses Wood category");
   Check(Surface(Named(host,"Provision coffer base").Single().GetComponent<Renderer>())==VisualSurface.Wood,"provision coffer preserves Wood category");
   var water=r.Material(Color.white,false,VisualSurface.Water);Check(Near(water.GetFloat("_Glossiness"),.82f),"actual Water surface preserves smoothness");
   WorldTraversal.Reset(ZoneKind.Wilderness);WorldBuilder.TestFocus(host.transform,r);WorldBuilder.TestPools(host.transform,1);
   Check(WorldTraversal.boxes.Count==0&&WorldTraversal.circles.Count==0&&Registry.Count==0&&host.GetComponentsInChildren<Collider>(true).Length==0&&host.GetComponentsInChildren<Rigidbody>(true).Length==0,"focus and light pools remain cosmetic without traversal physics or occlusion");Dispose(host);}
  foreach(bool dungeon in new[]{false,true})foreach(int hub in new[]{0,1,2}){
   var root=WorldBuilder.Build(dungeon?ZoneKind.Dungeon:ZoneKind.Wilderness,0,0,hub);var profile=new EnvironmentLightProfile(dungeon,hub);var key=Named(root,"Sun").Single().GetComponent<Light>();var fill=Named(root,"Cool silhouette fill").Single().GetComponent<Light>();
   Check(Near(key.intensity,profile.KeyIntensity)&&Near(fill.intensity,profile.FillIntensity)&&Near(RenderSettings.fogDensity,profile.FogDensity),"factory applies actual environment light profile");
   var points=root.GetComponentsInChildren<Light>(false).Where(l=>l.type==LightType.Point).ToArray();var accents=points.Where(l=>Near(l.range,EnvironmentLightProfile.AccentRange)).ToArray();
   Check(accents.Length==(dungeon?0:2),"factory creates exactly two town accents and none in dungeons");
   Check(points.All(l=>l.shadows==LightShadows.None&&l.renderMode==LightRenderMode.ForceVertex),"actual point lights remain unshadowed vertex accents");
   if(!dungeon){for(int i=0;i<2;i++)Check(accents.Any(l=>Near(l.transform.position,HubSettlementPlan.Npc(i)+new Vector3(0,2.5f,1))),"accent lights follow actual NPC positions");}
   if(!dungeon&&hub>0){var insets=Named(root,"Gate approach inset");Check(insets.Length==6,"town portal invokes six focus insets");Check(insets.All(t=>Near(t.localScale,new Vector3(.24f,.009f,.27f))&&Near(t.localPosition.y,.05f)&&t.GetComponent<CameraOcclusionSurface>()==null&&Surface(t.GetComponent<Renderer>())==VisualSurface.Metal),"portal focus is flush opaque Metal without occlusion registration");}
   Check(root.GetComponentsInChildren<Collider>(true).Length==0&&root.GetComponentsInChildren<Rigidbody>(true).Length==0,"environment construction adds no physics components");Dispose(root);
  }
  Console.WriteLine("PASS: environment partition "+(n-start)+" production material/light/focus checks");
 }
 static void Town(){int start=n;
  foreach(int hub in new[]{1,2}){
   var root=WorldBuilder.Build(ZoneKind.Wilderness,0,0,hub);var groups=root.GetComponentsInChildren<BuildingOcclusionGroup>(false);Check(groups.Length==HubSettlementPlan.BuildingCount,"full town configures every building group");
   for(int index=0;index<groups.Length;index++){
    var group=groups[index];var at=HubSettlementPlan.Building(hub,index);var building=group.gameObject;var parts=Visuals(building);var line=building.GetComponentsInChildren<LineRenderer>(false).Single();
    Check(group.transform.parent==root.transform&&Near(group.transform.localPosition,Vector3.zero)&&Near(group.transform.localScale,Vector3.one)&&Near(group.transform.localRotation*Vector3.forward,Vector3.forward),"building root preserves parent and identity transform");
    var wall=Named(building,hub==1?"Quarry workshop":"Observatory arcade").Single();Check(Near(wall.localPosition,at+Vector3.up*1.9f)&&Near(wall.localScale,new Vector3(HubSettlementPlan.BuildingWidth,3.8f,HubSettlementPlan.BuildingWidth)),"building wall matches authored navigation center and width");
    Check(WorldTraversal.boxes.Count(x=>Near(x.p,at)&&Near(x.size.x,HubSettlementPlan.BuildingWidth)&&Near(x.size.y,HubSettlementPlan.BuildingWidth))==1,"town registers each building solid exactly once");
    var upper=parts.Where(v=>v.bounds.max.y>at.y+.5f).ToArray();var footings=Named(building,"Weathered wall footing");
    Check(footings.Length==4&&footings.All(t=>t.GetComponent<CameraOcclusionSurface>()==null&&Near(t.localScale,new Vector3(.045f,.3f,1.2f))),"four low flush footings remain opaque");
    Check(upper.Length==(hub==1?10:6),"complete upper building parts include roof helper and late ornaments");
    Check(upper.All(v=>v.transform.parent==group.transform&&v.GetComponent<CameraOcclusionSurface>()!=null&&Registry.Contains(v.GetComponent<CameraOcclusionSurface>())&&ReferenceEquals(Field(v.GetComponent<CameraOcclusionSurface>(),"group"),group)),"all upper parts enter real registry and own building group");
    float half=HubSettlementPlan.BuildingWidth*.5f;var expected=new[]{at+new Vector3(-half,.07f,-half),at+new Vector3(-half,.07f,half),at+new Vector3(half,.07f,half),at+new Vector3(half,.07f,-half)};
    Check(line.positionCount==4&&line.loop&&line.useWorldSpace&&!line.enabled&&line.points.Zip(expected,Near).All(x=>x),"group footprint matches actual solid rectangle");
    if(hub==1){foreach(var pair in new[]{("Pitched workshop roof module",2),("Workshop projecting eave",2),("Workshop ridge cap",1),("Kiln chimney",1),("Kiln chimney cap",1)})Check(Named(building,pair.Item1).Length==pair.Item2,"all seven roof helper parts share each workshop root");}
    else Check(Named(building,"Astral dome").Length==1&&Named(building,"Arcade wall pilaster").Length==2&&Named(building,"Observatory roof spire").Length==1,"observatory dome pilasters and spire remain grouped");
    var originals=upper.Select(v=>v.sharedMaterial).ToArray();var lows=footings.Select(t=>t.GetComponent<Renderer>().sharedMaterial).ToArray();
    CameraOcclusionSurface.Advance(at+new Vector3(0,2,-4),at+new Vector3(0,2,4),.1f);
    Check(upper.All(v=>v.sharedMaterial.color.a<1)&&line.enabled,"real camera registry fades whole building including late parts");
    Check(footings.Select((t,i)=>ReferenceEquals(t.GetComponent<Renderer>().sharedMaterial,lows[i])&&lows[i].color.a==1).All(x=>x),"low footing materials stay opaque during group fade");
    CameraOcclusionSurface.RestoreAll();Check(upper.Select((v,i)=>ReferenceEquals(v.sharedMaterial,originals[i])).All(x=>x)&&!line.enabled,"building restore returns every original material and hides outline");
   }
   Check(Registry.Count==(hub==1?63:36)&&Registry.Count<CameraVisibilityRules.MaximumSurfaces,"complete town registry includes all buildings within bounded cap");
   Check(WorldTraversal.boxes.Count==(hub==1?10:8)&&WorldTraversal.circles.Count==(hub==1?2:3),"full town navigation has only building forge or dais and NPC solids");
   var townRenderers=Visuals(root);Check(townRenderers.Where(v=>new[]{"Pitched workshop roof module","Workshop projecting eave","Workshop ridge cap","Kiln chimney","Kiln chimney cap","Astral dome","Arcade wall pilaster","Observatory roof spire","Weathered wall footing","Lit doorway"}.Contains(v.transform.name)).All(v=>v.transform.parent.GetComponent<BuildingOcclusionGroup>()!=null),"no building decoration escapes grouped root");
   root.SetActive(false);Check(Registry.Count==0,"whole town disable clears registry");root.SetActive(true);Check(Registry.Count==(hub==1?63:36),"whole town enable restores full registry");Dispose(root);
  }
  Console.WriteLine("PASS: town partition "+(n-start)+" full production town/group/registry wiring checks");
 }
 static void Hub(){int start=n;
  foreach(int hub in new[]{1,2}){var host=Host("cosmetics");var r=Resources(host);WorldTraversal.Reset(ZoneKind.Wilderness);WorldBuilder.TestGround(host.transform,r,hub);WorldBuilder.TestBank(host.transform,r);
   Check(WorldTraversal.boxes.Count==0&&WorldTraversal.circles.Count==0&&Registry.Count==0,"ground and bank detail do not mutate navigation or occlusion");
   Check(Named(host,"Flush road edge stone").Length==48&&Named(host,"Observatory perimeter mosaic").Length==(hub==2?16:0),"distinct town cosmetic ground recipes stay bounded");
   Check(Named(host,"Brook reed clump").Length==6&&Named(host,"Brook reed clump").All(t=>Surface(t.GetComponent<Renderer>())==VisualSurface.Foliage),"actual bank detail retains Foliage reeds");Dispose(host);}
  var root=Host("actual NPC rigs");var resources=Resources(root);WorldTraversal.Reset(ZoneKind.Wilderness);WorldBuilder.TestNpcs(root.transform,resources);var npcs=root.GetComponentsInChildren<HubNpcIdle>(false);Check(npcs.Length==2,"three actual role NPC rigs constructed");
  for(int i=0;i<2;i++){
   var npc=npcs[i];var p=HubSettlementPlan.Npc(i);var head=Named(npc.gameObject,"NPC head").Single();
   Check(Near(GameSession.HubNpcPosition(i),p)&&Near(head.localPosition,p+Vector3.up*1.66f),"NPC rendering and session interaction share authored positions");
   Check(WorldTraversal.circles.Count(c=>Near(c.p,p)&&Near(c.radius,.43f))==1&&WorldTraversal.boxes.Count(b=>Near(b.p,p+new Vector3(0,0,.65f))&&Near(b.size.x,1.4f)&&Near(b.size.y,.5f))==1,"each NPC registers body and role-prop footprint exactly once");
   Check(Surface(head.GetComponent<Renderer>())==VisualSurface.Skin&&Named(npc.gameObject,"NPC tunic").All(t=>Surface(t.GetComponent<Renderer>())==VisualSurface.Cloth),"actual NPC Skin and Cloth material categories");
   string roleProp=i==0?"Merchant stocked shelf":i==1?"Forged anvil face":"Turning exchange star chart";
   Check(Named(npc.gameObject,roleProp).Length==(i==0?2:1),"role-specific merchant smith exchange props remain present");
   if(i==0)Check(Named(npc.gameObject,roleProp).All(t=>Surface(t.GetComponent<Renderer>())==VisualSurface.Wood),"merchant shelves retain Wood category");
   if(i==1)Check(Surface(Named(npc.gameObject,roleProp).Single().GetComponent<Renderer>())==VisualSurface.Metal,"smith anvil retains Metal category");
   var arm=(Transform)Field(npc,"arm");var left=(Transform)Field(npc,"otherArm");var dial=(Transform)Field(npc,"dial");Check(arm!=null&&left!=null&&(i==2)==(dial!=null),"actual NPC initializer binds articulated arms and exchange dial");
   int objects=UnityEngine.Object.All.Count;Time.deltaTime=.1f;Time.time=100;GameObject.Call(npc,"Update");var rotation=arm.localRotation*Vector3.forward;var leftRotation=left.localRotation*Vector3.forward;var dialRotation=dial==null?Vector3.zero:dial.localRotation*Vector3.forward;float age=(float)Field(npc,"age");
   Time.deltaTime=0;Time.time=10000;for(int tick=0;tick<10;tick++)GameObject.Call(npc,"Update");
   Check(Near(rotation,arm.localRotation*Vector3.forward)&&Near(leftRotation,left.localRotation*Vector3.forward)&&Near(age,(float)Field(npc,"age"))&&(dial==null||Near(dialRotation,dial.localRotation*Vector3.forward)),"actual NPC pose and age freeze while paused despite wall clock");
   Time.deltaTime=-.1f;GameObject.Call(npc,"Update");Check(Near(rotation,arm.localRotation*Vector3.forward)&&Near(age,(float)Field(npc,"age")),"NPC idle rejects negative delta time");
   float savedAngle=(float)Field(npc,"angle");Time.deltaTime=.1f;GameObject.Call(npc,"Update");Check(!Near(rotation,arm.localRotation*Vector3.forward)&&UnityEngine.Object.All.Count==objects&&Near((float)Field(npc,"age"),age+.1f),"NPC idle resumes without object allocations");
   var resumedArm=arm.localRotation*Vector3.forward;var resumedLeft=left.localRotation*Vector3.forward;var resumedDial=dial==null?Vector3.zero:dial.localRotation*Vector3.forward;
   // Repeat the same production state/timestep under a different wall clock.
   SetField(npc,"age",age);SetField(npc,"angle",savedAngle);Time.time=20000;GameObject.Call(npc,"Update");
   Check(Near(resumedArm,arm.localRotation*Vector3.forward)&&Near(resumedLeft,left.localRotation*Vector3.forward)&&(dial==null||Near(resumedDial,dial.localRotation*Vector3.forward)),"same NPC simulation state ignores wall clock during active idle");
  }
  Check(WorldTraversal.circles.Count==2&&WorldTraversal.boxes.Count==2&&Registry.Count==0,"NPC navigation registered once with no scenery occluders");Dispose(root);
  Console.WriteLine("PASS: hub partition "+(n-start)+" production NPC/props/pause/cosmetic-ground checks");
 }
 static void Main(string[] args){string scope=args.Length==0?"all":args[0];if(scope=="all"||scope=="environment")Environment();if(scope=="all"||scope=="town")Town();if(scope=="all"||scope=="hub")Hub();Console.WriteLine("PASS: "+n+" total production scenery assertions; managed Unity APIs, no rendered/GPU/pathfinding acceptance");}
}
