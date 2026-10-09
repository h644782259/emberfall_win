using System;using System.Linq;using UnityEngine;using Emberfall;
namespace Emberfall
{
 public enum ZoneKind{Dungeon,Wilderness}
 public static class PlayerUpgradeRules{public static float FindSafeBlinkDistance(float d,Func<float,bool>a,Func<float,bool>b){throw new NotSupportedException();}}
 public static partial class CombatFx{public static float SegmentDistance(Vector3 p,Vector3 a,Vector3 b){p.y=a.y=b.y=0;var d=b-a;return Vector3.Distance(p,a+d*(d.sqrMagnitude<.0001f?0:Mathf.Clamp01(Vector3.Dot(p-a,d)/d.sqrMagnitude)));}}
}
namespace UnityEngine
{
 public struct Rect{public float xMin,xMax,yMin,yMax;public Rect(float x,float y,float w,float h){xMin=x;xMax=x+w;yMin=y;yMax=y+h;}}
 public partial struct Vector3{public void Normalize(){this=normalized;}public static float Dot(Vector3 a,Vector3 b)=>a.x*b.x+a.y*b.y+a.z*b.z;public static float Distance(Vector3 a,Vector3 b)=>(a-b).magnitude;public static Vector3 ClampMagnitude(Vector3 a,float m)=>a.magnitude>m?a.normalized*m:a;}
 public static partial class Mathf{public static int Min(int a,int b)=>Math.Min(a,b);public static int Clamp(int a,int b,int c)=>Math.Min(c,Math.Max(a,b));public static int CeilToInt(float v)=>(int)Math.Ceiling(v);public static int RoundToInt(float v)=>(int)Math.Round(v);}
}
public static class AnchoredImpactCoverageTests
{
 static int checks,faces,vertices,maxSegments,maxProbes,maxTriangles;static void Check(bool v,string why){checks++;if(!v)throw new Exception(why);}
 static PlayerController Reset(){foreach(var o in GameObject.All.ToArray())UnityEngine.Object.Destroy(o);GameObject.All.Clear();Check(CombatVisualLease.Active==0,"mesh cleanup returns leases");WorldTraversal.Reset(ZoneKind.Dungeon);Application.isMobilePlatform=true;EffectPreferences.ReducedEffects=false;var hero=new GameObject("hero").AddComponent<PlayerController>();GameSession.Instance=new GameSession{HasStarted=true,Player=hero};return hero;}
 static void Verify(GameObject part,Vector3 origin,bool dense)
 {
  var mesh=part.GetComponent<MeshFilter>().sharedMesh;Check(mesh.vertices.Length<=12288,"clipping vertex budget is bounded");var points=mesh.vertices.Select(v=>part.transform.TransformPoint(v)).ToArray();
  if(dense)for(int i=0;i<mesh.triangles.Length;i+=3)
  {
   faces++;Vector3 a=points[mesh.triangles[i]],b=points[mesh.triangles[i+1]],c=points[mesh.triangles[i+2]];
   // Independent 1/17 barycentric grid, deliberately not production centroid/edge-midpoint samples.
   for(int u=0;u<=17;u++)for(int v=0;v<=17-u;v++)Check(CombatSight.Area(origin,a*(u/17f)+b*(v/17f)+c*((17-u-v)/17f)),"retained anchored triangle crosses actual finite-cover LOS");
  }
  foreach(var v in points){vertices++;Check(CombatSight.Area(origin,v),"animated anchored vertex crosses actual finite-cover LOS");}
 }
 static double AreaSum(GameObject part,bool horizontal)
 {
  var m=part.GetComponent<MeshFilter>().sharedMesh;double sum=0;
  for(int i=0;i<m.triangles.Length;i+=3){Vector3 a=part.transform.TransformPoint(m.vertices[m.triangles[i]]),b=part.transform.TransformPoint(m.vertices[m.triangles[i+1]]),c=part.transform.TransformPoint(m.vertices[m.triangles[i+2]]);var u=b-a;var v=c-a;double x=(double)u.y*v.z-(double)u.z*v.y,y=(double)u.z*v.x-(double)u.x*v.z,z=(double)u.x*v.y-(double)u.y*v.x;sum+=horizontal?Math.Abs(y)*.5:Math.Sqrt(x*x+y*y+z*z)*.5;}return sum;
 }
 public static string RunNearWall()
 {
  int partsChecked=0,maxQueries=0,maxFans=0;double minimumPrimary=double.MaxValue,minimumContact=double.MaxValue;
  foreach(float gap in new[]{.040001f,.0403f,.05f,.08f,.1f,.13f,.131f,-1f})
  foreach(var type in new[]{FilledVfxKind.Ice,FilledVfxKind.Fire,FilledVfxKind.Summon,FilledVfxKind.Sword,FilledVfxKind.Lightning,FilledVfxKind.Arcane})
  {
   var hero=Reset();Vector3 center=Vector3.zero;
   if(gap<0){WorldTraversal.AddBox(Vector3.zero,new Vector2(.2f,6));center=CombatSight.GroundPoint(new Vector3(-3,0,0),new Vector3(3,0,0));Check(Math.Abs(center.x+.14025879f)<.000001f,"exact production GroundPoint reproduction");}
   else WorldTraversal.AddBox(new Vector3(gap+.5f,0,0),new Vector2(1,6));
   Check(CombatSight.Direct(new Vector3(-3,0,0),center)&&CombatSight.Area(center,new Vector3(-2,0,0)),"near-wall center has actual cast and damage LOS");
   WorldTraversal.TestTriangleCalls=WorldTraversal.TestFanCalls=0;
   FilledSkillVfx.Impact(hero,center,4,type,new Color(1,1,1));maxQueries=Math.Max(maxQueries,WorldTraversal.TestTriangleCalls);maxFans=Math.Max(maxFans,WorldTraversal.TestFanCalls);
   var effect=GameObject.All.Single(o=>o.GetComponent<FilledSkillVfx>()!=null);Check((effect.transform.position-center).sqrMagnitude==0,"near-wall impact center cannot relocate");
   var parts=GameObject.All.Where(o=>o.name.Contains("Primary ")||o.name.Contains("Landing base")||o.name.Contains("Contact flash")).ToArray();Check(parts.Length==3,"all three real anchor parts exist");
   foreach(var part in parts){partsChecked++;bool primary=part.name.Contains("Primary ");double area=AreaSum(part,!primary);Check(area>.001,"legal near-wall impact retains positive-area primary and horizontal contact");if(primary)minimumPrimary=Math.Min(minimumPrimary,area);else minimumContact=Math.Min(minimumContact,area);Verify(part,center,true);}
   if(gap<0)Console.WriteLine("GroundPoint "+type+": "+string.Join(", ",parts.Select(o=>o.name+"="+o.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3+" faces area="+AreaSum(o,!o.name.Contains("Primary ")))));
   for(int frame=1;frame<=7;frame++){Time.deltaTime=.1f;effect.Call("Update");foreach(var part in parts)Verify(part,center,frame==2);}
   var owned=parts.Select(o=>o.GetComponent<MeshFilter>().sharedMesh).ToArray();UnityEngine.Object.Destroy(effect);Check(owned.All(m=>m.Destroyed),"near-wall owned meshes disposed");
  }
  Reset();return "PASS: "+partsChecked+" positive-area near-wall anchored meshes; min primary 3D area="+minimumPrimary+", min contact horizontal area="+minimumContact+"; max creation certificate calls="+maxQueries+", obstacle fan checks="+maxFans;
 }
 public static string RunMotion()
 {
  int witnessed=0;
  foreach(float radius in new[]{.12f,.28f,.48f})foreach(float distance in new[]{.35f,.65f,1f,1.5f})for(int angle=0;angle<24;angle++)
  {
   if(distance<=radius+.05f)continue;
   var hero=Reset();float a=angle*Mathf.PI/12+.13f;WorldTraversal.AddCircle(new Vector3(Mathf.Cos(a)*distance,0,Mathf.Sin(a)*distance),radius);
   FilledSkillVfx.Impact(hero,Vector3.zero,4,FilledVfxKind.Lightning,new Color(1,1,1));var effect=GameObject.All.Single(o=>o.GetComponent<FilledSkillVfx>()!=null);var part=GameObject.All.Single(o=>o.name.Contains("Primary "));var mesh=part.GetComponent<MeshFilter>().sharedMesh;
   if(mesh.vertices.Length==0||!mesh.vertices.All(v=>CombatSight.Area(Vector3.zero,part.transform.TransformPoint(v))))continue;
   witnessed++;Time.deltaTime=.2f;effect.Call("Update");
   foreach(var v in mesh.vertices)Check(CombatSight.Area(Vector3.zero,part.transform.TransformPoint(v)),"animated anchored vertex crosses actual finite-cover LOS");
  }
  Check(witnessed>0,"animation oracle observes initially visible lightning");Reset();return "PASS: "+witnessed+" initially visible finite-cover lightning animations";
 }
 public static string RunFastPath()
 {
  var hero=Reset();var source=new Mesh{vertices=new[]{new Vector3(-1,0,-1),new Vector3(1,0,-1),new Vector3(1,0,1),new Vector3(-1,0,1)},uv=new[]{new Vector2(),new Vector2(),new Vector2(),new Vector2()},triangles=Enumerable.Range(0,1000).SelectMany(i=>i%2==0?new[]{0,1,2}:new[]{0,2,3}).ToArray()};
  var original=source.vertices.ToArray();WorldTraversal.TestTriangleCalls=WorldTraversal.TestSegmentCalls=0;
  var result=AnchoredImpactMesh.Create(source,hero.transform,Vector3.zero,Vector3.one,Quaternion.identity);
  Check(result.triangles.Length==3000,"open ground retains all 1000 source triangles");
  Check(WorldTraversal.TestTriangleCalls==2&&WorldTraversal.TestSegmentCalls==0,"open ground requires exactly two face certificates and zero vertex rays");
  Check(original.SequenceEqual(source.vertices),"fast path cannot mutate shared source vertices");
  UnityEngine.Object.Destroy(result);UnityEngine.Object.Destroy(source);Reset();
  return "PASS: 1000-face open-ground mesh: two certificates, zero vertex rays, unchanged shared source";
 }
 public static string Run()
 {
  Console.WriteLine(RunFastPath());
  Console.WriteLine(RunNearWall());
  Console.WriteLine(RunMotion());
  foreach(var type in new[]{FilledVfxKind.Arcane,FilledVfxKind.Lightning})
  {
   var hero=Reset();FilledSkillVfx.Impact(hero,Vector3.zero,4,type,new Color(1,1,1));
   var primary=GameObject.All.Single(o=>o.name.Contains("Primary "));var original=(Mesh)typeof(FilledSkillVfx).GetField(type==FilledVfxKind.Arcane?"arcane":"lightning",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).GetValue(null);
   Check(primary.GetComponent<MeshFilter>().sharedMesh.triangles.Length==original.triangles.Length,"unobstructed primary retains every original identity face");
   hero=Reset();WorldTraversal.AddBox(new Vector3(-.65f,0,0),new Vector2(.2f,6));WorldTraversal.AddBox(new Vector3(.65f,0,0),new Vector2(.2f,6));FilledSkillVfx.Impact(hero,Vector3.zero,4,type,new Color(1,1,1));primary=GameObject.All.Single(o=>o.name.Contains("Primary "));
   Check(primary.GetComponent<MeshFilter>().sharedMesh.triangles.Length>0,"narrow corridor retains actual primary silhouette faces");Verify(primary,Vector3.zero,true);
  }
  foreach(var type in new[]{FilledVfxKind.Arcane,FilledVfxKind.Lightning})
  foreach(float distance in new[]{.65f,1f,1.5f,2f})for(int angle=0;angle<12;angle++)
  {
   var hero=Reset();float a=angle*Mathf.PI/6+.13f;WorldTraversal.AddCircle(new Vector3(Mathf.Cos(a)*distance,0,Mathf.Sin(a)*distance),.28f);
   WorldTraversal.TestSegmentCalls=WorldTraversal.TestSolidProbes=0;
   FilledSkillVfx.Impact(hero,Vector3.zero,4,type,new Color(1,1,1));maxSegments=Math.Max(maxSegments,WorldTraversal.TestSegmentCalls);maxProbes=Math.Max(maxProbes,WorldTraversal.TestSolidProbes);var effect=GameObject.All.Single(o=>o.GetComponent<FilledSkillVfx>()!=null);var parts=GameObject.All.Where(o=>o.name.Contains("Primary ")||o.name.Contains("Landing base")||o.name.Contains("Contact flash")).ToArray();
   maxTriangles=Math.Max(maxTriangles,parts.Sum(o=>o.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3));
   Check(parts.Length==3&&parts.All(o=>o.GetComponent<MeshFilter>().sharedMesh.triangles.Length>0),"finite cover preserves reachable anchored feedback");Check(effect.transform.position.sqrMagnitude==0,"real impact center cannot relocate");
   foreach(var part in parts)Verify(part,Vector3.zero,true);
   for(int frame=1;frame<=9;frame++){Time.deltaTime=.1f;effect.Call("Update");foreach(var part in parts)Verify(part,Vector3.zero,frame==2);}
   var owned=parts.Select(o=>o.GetComponent<MeshFilter>().sharedMesh).ToArray();UnityEngine.Object.Destroy(effect);Check(owned.All(m=>m.Destroyed),"all subdivided mesh resources released with effect");
  }
  Reset();Console.WriteLine("Creation maxima in tested cases: "+maxTriangles+" anchored triangles summed across 3 parts; "+maxSegments+" real traversal segment calls; "+maxProbes+" solid probes per Impact (includes ornaments, excludes test oracle)");return "PASS: "+checks+" anchored real LOS checks; "+faces+" faces, "+vertices+" animated vertices (managed, not Unity rendering)";
 }
}
