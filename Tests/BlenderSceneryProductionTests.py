#!/usr/bin/env python3
"""Compile real scenery resource adapter and real pilot atlas gate against managed Unity boundaries."""
from pathlib import Path
import os,subprocess,tempfile,sys
root=Path(__file__).resolve().parents[1];dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet')
fixture=(root/'Tests/BlenderPilotAdapterProductionFixture.cs').read_text().split('namespace Emberfall{')[0]
fixture=fixture.replace('g.transform.localPosition=transform.localPosition;', 'g.transform.localPosition=transform.localPosition;g.transform.localScale=transform.localScale;')
fixture=fixture.replace('if(Bound)LayerFixture.ApplyClip(g,name,t/length);','')
fixture=fixture.replace('public bool activeInHierarchy=>','public T GetComponent<T>()where T:Component=>Components.OfType<T>().FirstOrDefault();public bool activeInHierarchy=>')
fixture=fixture.replace('var v=g.AddComponent<Renderer>();v.enabled=r.enabled;v.sharedMaterial=r.sharedMaterial;','var v=c is MeshRenderer?g.AddComponent<MeshRenderer>():g.AddComponent<Renderer>();v.enabled=r.enabled;v.sharedMaterial=r.sharedMaterial;v.sharedMaterials=(Material[])r.sharedMaterials.Clone();')
fixture=fixture.replace('public Material sharedMaterial;','public Material sharedMaterial;public Material[] sharedMaterials=new Material[0];')
fixture=fixture.replace('public class Mesh:Object{}', """public enum MeshTopology{Triangles,Lines}
 public struct SubMeshDescriptor{public int firstVertex,vertexCount;}
 public class Mesh:Object{
 public Vector3[] vertices=new Vector3[0];public int[][] indices=new int[0][];public MeshTopology topology=MeshTopology.Triangles;
 public int vertexCount=>vertices.Length;public int subMeshCount=>indices.Length;
 public uint GetIndexCount(int s)=>(uint)indices[s].Length;public MeshTopology GetTopology(int s)=>topology;
 public SubMeshDescriptor GetSubMesh(int s){var a=indices[s];return new SubMeshDescriptor{firstVertex=a.Length==0?0:a.Min(),vertexCount=a.Length==0?0:a.Max()-a.Min()+1};}
 }""")
fixture+='\nnamespace UnityEngine {public class Collider:Component{}public class MeshRenderer:Renderer{}}\nnamespace Emberfall {public enum VisualSurface{Stone,Wood,Foliage}public class WorldResources{public Material Material(Color c,bool e,VisualSurface s)=>new Material{name=s.ToString()};}}\n'
pilot=(root/'Assets/Scripts/Combat/BlenderPilotVisual.cs').read_text().split('    // Sole writer of the imported skeleton.')[0]+'}\n'
checks='''
class Test {
 static int count;static void C(bool b,string s){count++;if(!b)throw new Exception(s);}
 static GameObject Source(string slot="Clay"){var g=new GameObject("resource");g.AddComponent<MeshFilter>().sharedMesh=new Mesh{vertices=new[]{new Vector3(0,0,0),new Vector3(1,0,0),new Vector3(0,1,0)},indices=new[]{new[]{0,1,2}}};var r=g.AddComponent<MeshRenderer>();r.sharedMaterials=new[]{new Material{name=slot}};return g;}
 static void Main(){var owner=new GameObject("owner");var r=new WorldResources();
 C(BlenderSceneryArt.Create("missing",owner.transform,Vector3.zero,r)==null,"missing falls back");
 var source=Source();source.transform.localScale=new Vector3(100,100,100);Resources.Items["BlenderScenery/pot"]=source;
 BlenderSceneryArt.Enabled=false;int loads=Resources.Loads;C(BlenderSceneryArt.Create("pot",owner.transform,Vector3.zero,r)==null&&Resources.Loads==loads,"opt out avoids load");BlenderSceneryArt.Enabled=true;
 var made=BlenderSceneryArt.Create("pot",owner.transform,new Vector3(1,2,3),r);C(made!=null&&made.transform.parent==owner.transform&&made.transform.localPosition==new Vector3(1,2,3),"preserves parent and authored placement");C(made.transform.localScale==Vector3.one&&source.transform.localScale==new Vector3(100,100,100),"instance root scale normalized without mutating source");C(made.GetComponentsInChildren<MeshRenderer>(true)[0].sharedMaterials[0].name=="Stone","binds shared world surface");C(source.GetComponentsInChildren<MeshRenderer>(true)[0].sharedMaterials[0].name=="Clay","never mutates resource material slots");
 source.AddComponent<Collider>();C(BlenderSceneryArt.Create("pot",owner.transform,Vector3.zero,r)==null,"collider resource rejected");
 Resources.Items["BlenderScenery/pot"]=Source("unknown");int before=GameObject.All.Count;C(BlenderSceneryArt.Create("pot",owner.transform,Vector3.zero,r)==null,"unknown imported material rejected");C(GameObject.All.Skip(before).All(g=>!g.activeSelf&&g.destroyed),"rejected partial instance hidden and destroyed");
 foreach(string slot in new[]{"Clay","Ochre","Stone","Bark","Leaf"}){Resources.Items["BlenderScenery/pot"]=Source(slot);C(BlenderSceneryArt.Create("pot",owner.transform,Vector3.zero,r)!=null,"accept palette "+slot);}
 // Invalid resource must not create an instance or suppress the procedural caller fallback.
 foreach(string kind in new[]{"empty","no-indices","no-submesh","bad-index","degenerate-indices","lines","no-slots","null-slot","uncovered-submesh","disabled","inactive","inactive-parent","renderer-only","filter-only"}){
  var invalid=Source();var mesh=invalid.GetComponent<MeshFilter>().sharedMesh;var renderer=invalid.GetComponent<MeshRenderer>();
  if(kind=="empty")mesh.vertices=new Vector3[0];
  if(kind=="no-indices")mesh.indices=new[]{new int[0]};
  if(kind=="no-submesh")mesh.indices=new int[0][];
  if(kind=="bad-index")mesh.indices=new[]{new[]{0,1,99}};
  if(kind=="degenerate-indices")mesh.indices=new[]{new[]{0,0,0}};
  if(kind=="lines")mesh.topology=MeshTopology.Lines;
  if(kind=="no-slots")renderer.sharedMaterials=new Material[0];
  if(kind=="null-slot")renderer.sharedMaterials=new Material[]{null};
  if(kind=="uncovered-submesh")mesh.indices=new[]{new[]{0,1,2},new[]{0,2,1}};
  if(kind=="disabled")renderer.enabled=false;
  if(kind=="inactive")invalid.SetActive(false);
  if(kind=="inactive-parent"){var holder=new GameObject("inactive holder");invalid.transform.SetParent(holder.transform);holder.SetActive(false);invalid=holder;}
  if(kind=="renderer-only")invalid.Components.Remove(invalid.GetComponent<MeshFilter>());
  if(kind=="filter-only")invalid.Components.Remove(renderer);
  Resources.Items["BlenderScenery/invalid"]=invalid;int objects=GameObject.All.Count;
  C(BlenderSceneryArt.Create("invalid",owner.transform,Vector3.zero,r)==null,"invalid resource fallback: "+kind);
  C(GameObject.All.Count==objects,"invalid resource rejected before instance: "+kind);
  Resources.Items["BlenderPilot/invalid"]=invalid;
  C(BlenderSceneryArt.CreatePilotProp("invalid",owner.transform,Vector3.zero)==null,"pilot also rejects invalid geometry: "+kind);
 }
 var tree=Source("Bark");tree.GetComponent<MeshFilter>().sharedMesh.indices=new[]{new[]{0,1,2},new[]{0,2,1}};tree.GetComponent<MeshRenderer>().sharedMaterials=new[]{new Material{name="Bark"},new Material{name="Leaf"}};
 Resources.Items["BlenderScenery/tree"]=tree;var builtTree=BlenderSceneryArt.Create("tree",owner.transform,Vector3.zero,r);
 C(builtTree!=null&&builtTree.GetComponent<MeshRenderer>().sharedMaterials.Select(x=>x.name).SequenceEqual(new[]{"Wood","Foliage"}),"two drawable submeshes receive both world materials");
 var nested=new GameObject("nested root");var activePart=Source();activePart.transform.SetParent(nested.transform);var optional=Source("unknown");optional.transform.SetParent(nested.transform);optional.SetActive(false);var disabledPart=Source("unknown");disabledPart.transform.SetParent(nested.transform);disabledPart.GetComponent<MeshRenderer>().enabled=false;
 Resources.Items["BlenderScenery/nested"]=nested;C(BlenderSceneryArt.Create("nested",owner.transform,Vector3.zero,r)!=null,"valid nested renderer permits inactive and disabled optional children");
 Resources.Items["BlenderPilot/SupplyCrate"]=Source();BlenderPilotArt.Enabled=false;C(BlenderSceneryArt.CreatePilotProp("SupplyCrate",owner.transform,Vector3.zero)==null,"missing atlas falls back");Resources.Items["BlenderPilot/Pilot_Atlas_Standard"]=new Material();C(BlenderSceneryArt.CreatePilotProp("SupplyCrate",owner.transform,Vector3.zero)!=null&&!BlenderPilotArt.Enabled,"scenery independent of hero pilot opt in");
 Console.WriteLine("PASS: "+count+" real scenery adapter checks; managed boundaries, no Unity import/render claim");
 }
}
'''
with tempfile.TemporaryDirectory(prefix='scenery-loader-') as tmp:
 p=Path(tmp);(p/'Fixture.cs').write_text(fixture+checks);(p/'Pilot.cs').write_text(pilot);(p/'Scenery.cs').write_text((root/'Assets/Scripts/World/BlenderSceneryArt.cs').read_text());(p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1')
 subprocess.run([dotnet,'restore',str(p/'Test.csproj'),'--configfile',str(p/'NuGet.Config'),'-v:q'],check=True,env=env)
 subprocess.run([dotnet,'run','--project',str(p/'Test.csproj'),'--no-restore'],check=True,env=env)
 source=p/'Scenery.cs';original=source.read_text()
 for before,after,oracle in [
  ('mesh.vertexCount<3','mesh.vertexCount<0','invalid resource fallback: empty'),
  ('if(!renderer.enabled||!LocallyActive(renderer.transform,source.transform))continue;','if(!LocallyActive(renderer.transform,source.transform))continue;','invalid resource fallback: disabled'),
  ('slots.Length<mesh.subMeshCount','slots.Length<0','invalid resource fallback: no-slots')]:
  assert before in original;source.write_text(original.replace(before,after))
  result=subprocess.run([dotnet,'run','--project',str(p/'Test.csproj'),'--no-restore'],env=env,capture_output=True,text=True)
  # Some other geometry guards may also reject the same invalid mesh; mutations must remove the relevant whole boundary.
  if before=='mesh.vertexCount<3':
   source.write_text(original.replace('mesh.vertexCount<3','mesh.vertexCount<0').replace('(long)descriptor.firstVertex+descriptor.vertexCount>mesh.vertexCount','false'))
   result=subprocess.run([dotnet,'run','--project',str(p/'Test.csproj'),'--no-restore'],env=env,capture_output=True,text=True)
  assert result.returncode and 'System.Exception: '+oracle in result.stdout+result.stderr,result.stdout+result.stderr
  print('PASS compiled drawable negative:',oracle)
 source.write_text(original)

