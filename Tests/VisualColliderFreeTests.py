"""Run actual shared visual quad creation with absent collider types; source-audit remaining paths."""
from pathlib import Path
import tempfile,subprocess,os,sys,re
root=Path(__file__).resolve().parents[1]
def member(s,key):
 a=s.index(key);b=s.index('{',a)+1;n=1
 while n:n+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
for p in (root/'Assets/Scripts').rglob('*.cs'):
 s=re.sub(r'//[^\n]*','',p.read_text());assert 'GameObject.CreatePrimitive(' not in s,str(p)
for rel in ['Combat/EnemyController.cs','Combat/SummonedCompanion.cs','Combat/EnemyDeathDissolve.cs','World/GroundLootPickup.cs']:
 assert 'ProceduralVisuals.Create(' in (root/'Assets/Scripts'/rel).read_text()
s=(root/'Assets/Scripts/Combat/ProceduralVisuals.cs').read_text()
fixture=r"""
using System;using System.Collections.Generic;using UnityEngine;
namespace UnityEngine{
 public enum PrimitiveType{Cube,Cylinder,Capsule,Sphere,Quad}
 public struct Vector2{public float x,y;public Vector2(float a,float b){x=a;y=b;}}
 public struct Vector3{public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}public static Vector3 back=>new Vector3(0,0,-1);}
 public class Mesh{public string name;public Vector3[] vertices,normals;public Vector2[] uv;public int[] triangles;public void RecalculateBounds(){}}
 public class Material{}
 public class MeshFilter{public Mesh sharedMesh;}
 public class MeshRenderer{public Material sharedMaterial;public Rendering.ShadowCastingMode shadowCastingMode;public bool receiveShadows;}
 namespace Rendering{public enum ShadowCastingMode{On}}
 public class GameObject{public Dictionary<Type,object> components=new Dictionary<Type,object>();public GameObject(string n){}public T AddComponent<T>() where T:new(){var v=new T();components[typeof(T)]=v;return v;}public T Get<T>()=>(T)components[typeof(T)];}
}
namespace Emberfall{
 public class VisualMeshData{public Vector3[] Vertices,Normals;public Vector2[] Uv;public int[] Triangles;}
 public static class VisualMeshRecipes{public static VisualMeshData BevelBox()=>new VisualMeshData();public static VisualMeshData Cylinder(int n)=>new VisualMeshData();public static VisualMeshData RoundBody(bool b,int n,int m)=>new VisualMeshData();}
 public static class ProceduralVisuals{static Dictionary<PrimitiveType,Mesh> shapes=new Dictionary<PrimitiveType,Mesh>();METHODS}
 class Test{static void Check(bool b,string s){if(!b)throw new Exception(s);}static void Main(){var material=new Material();var first=ProceduralVisuals.Create("health",PrimitiveType.Quad,material);var mesh=first.Get<MeshFilter>().sharedMesh;
 Check(first.components.Count==2&&first.Get<MeshRenderer>().sharedMaterial==material,"only mesh filter/renderer are added; collider type is absent");Check(mesh.vertices.Length==4&&mesh.triangles.Length==6&&mesh.uv.Length==4,"explicit quad data");
 for(int i=0;i<6;i+=3){var a=mesh.vertices[mesh.triangles[i]];var b=mesh.vertices[mesh.triangles[i+1]];var c=mesh.vertices[mesh.triangles[i+2]];Check((b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x)<0,"quad winding faces negative Z like Unity Quad");}
 for(int i=0;i<100;i++)Check(Object.ReferenceEquals(mesh,ProceduralVisuals.Create("health",PrimitiveType.Quad,material).Get<MeshFilter>().sharedMesh),"health bars reuse cached mesh");
 Console.WriteLine("PASS actual visual quad/create: collider-free components, topology, winding and 100 cache reuses; all runtime primitive calls removed (managed boundary)");}}
}
"""
fixture=fixture.replace('METHODS',''.join(member(s,k) for k in ['public static Mesh Shape(','public static Mesh Build(','public static GameObject Create(']))
with tempfile.TemporaryDirectory(prefix='visual-no-collider-') as t:
 p=Path(t);(p/'Program.cs').write_text(fixture);(p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><NuGetAudit>false</NuGetAudit></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 subprocess.run([sys.argv[1] if len(sys.argv)>1 else 'dotnet','run','--project',str(p/'Test.csproj')],env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'),check=True)
