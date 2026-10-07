#!/usr/bin/env python3
"""Actual three water-entry slices + complete water/ribbon/resource/traversal production generators."""
import os,sys,subprocess,tempfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
def member(s,sig):
 a=s.index(sig);b=s.index('{',a)+1;n=1
 while n:n+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
def once(s,a,b):
 assert s.count(a)==1,a
 return s.replace(a,b)
w=(ROOT/'Assets/Scripts/World/WorldBuilder.cs').read_text();linked=(ROOT/'Assets/Scripts/World/WorldBuilder.LinkedRooms.cs').read_text();tactical=(ROOT/'Assets/Scripts/World/WorldBuilder.TacticalRooms.cs').read_text()
brook=w[w.index('            Vector3[] stream = {'):w.index('            BuildWaterBankDetail(lowland,r,stream);')]
court=linked[linked.index('    Vector3[] stream={'):linked.index('    Primitive(parent,"Sunken courtyard bridge"')]
tact=member(tactical,'if(TacticalRoomGeometry.Flooded(layout))');tact=tact[tact.index('{')+1:tact.rfind('}')]
entries=[]
for name,code in [('Brook',brook),('Courtyard',court),('Tactical',tact)]:
 entries.append('public static void '+name+'(Transform parent,WorldResources r,int layout=22,bool baseline=false){var lowland=parent;'+ ('TacticalRoomGeometry.Register(layout);' if name=='Tactical' else '')+code.replace('BuildWaterSurface(', 'WaterEntry(baseline,')+'}')
water=(ROOT/'Assets/Scripts/World/WorldBuilder.WaterSurface.cs').read_text()
base=(ROOT/'Tests/FilledVfxAllocationTests.cs').read_text();shell='using System;using System.Linq;using System.Collections.Generic;using System.Reflection;using UnityEngine;'+base[base.index('namespace Emberfall'):]
math=(ROOT/'Tests/DestructibleTraversalTests.cs').read_text();shell=shell.replace(member(shell,'public struct Vector2'),member(math,'public struct Vector2'));shell=shell.replace('public struct Vector3\n','public partial struct Vector3\n').replace('public static class Mathf\n','public static partial class Mathf\n').replace('public static class CombatFx{','public static partial class CombatFx{')
shell=shell.replace('public enum PrimitiveType{Sphere}','public enum PrimitiveType{Sphere,Cube}')
shell=shell.replace('public void RecalculateNormals(){}','public void SetVertices(List<Vector3> v){vertices=v.ToArray();}public void SetTriangles(List<int> t,int sub){triangles=t.ToArray();}public Vector3[] normals;public void RecalculateNormals(){normals=new Vector3[vertices.Length];for(int i=0;i<triangles.Length;i+=3){int a=triangles[i],b=triangles[i+1],c=triangles[i+2];var n=Vector3.Cross(vertices[b]-vertices[a],vertices[c]-vertices[a]);normals[a]+=n;normals[b]+=n;normals[c]+=n;}for(int i=0;i<normals.Length;i++)normals[i]=normals[i].sqrMagnitude>0?normals[i].normalized:Vector3.zero;}')
shell=shell.replace('public Material(Shader shader){}','public Material(Shader shader){}public bool HasProperty(string n)=>true;public void SetColor(string n,Color v){}')
shell=shell.replace('public struct Color{','public struct Color{public static Color operator*(Color c,float s)=>new Color(c.r*s,c.g*s,c.b*s,c.a*s);')
helper='using UnityEngine;using System.Collections.Generic;namespace Emberfall{public static partial class WorldBuilder{'+member(w,'private static void Ribbon(')+member(w,'private static GameObject Geometry(')+''.join(entries)+'}'+member(w,'public sealed class WorldResources')+'}'
dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet')
with tempfile.TemporaryDirectory(prefix='shore-contact-') as tmp:
 for variant in ("current","missing-hook","shared-backface-vertices"):
  old=variant=="missing-hook"
  d=Path(tmp)/variant;d.mkdir();(d/'Shell.cs').write_text(shell);(d/'WorldExtract.cs').write_text(helper)
  code=once(water,'            BuildShoreWaterContact(parent,r,name,path,profile.Width,height);','') if old else water
  if variant=="shared-backface-vertices":
   code=once(code,'int[] face={a,b,a+1,a+1,b,b+1};','int[] face={a,b,a+1,a+1,b,b+1,a+1,b,a,b+1,b,a+1};')
   begin=code.index('            int frontVertices=line.Count,frontIndices=lineTriangles.Count;')
   end=code.index('            Geometry(parent,r,name+" shore damp seam"',begin)
   code=code[:begin]+code[end:]
  (d/'Water.cs').write_text(code)
  for f in ['Assets/Scripts/Core/WaterPresentation.cs','Assets/Scripts/World/WorldTraversal.cs','Assets/Scripts/World/WorldTraversal.Platforms.cs','Assets/Scripts/World/TacticalRoomGeometry.cs','Tests/ShoreContactProductionTests.cs']:(d/Path(f).name).write_text((ROOT/f).read_text())
  (d/'Program.cs').write_text('System.Console.WriteLine(ShoreContactProductionTests.Run());');(d/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>');p=d/'Test.csproj';p.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>')
  build=subprocess.run([dotnet,'build',str(p),'--configfile',str(d/'NuGet.Config'),'-v:q'],capture_output=True,text=True)
  if build.returncode:print(build.stdout+build.stderr);build.check_returncode()
  result=subprocess.run([dotnet,str(d/'bin/Debug/net8.0/Test.dll')],capture_output=True,text=True)
  if old:
   assert result.returncode and 'System.Exception: actual water entry must build both static shoreline layers' in result.stdout+result.stderr,result.stdout+result.stderr
   print('PASS: old missing shoreline call compiled and failed exact actual-entry assertion')
  elif variant=="shared-backface-vertices":
   assert result.returncode and 'System.Exception: WATERLINE_NORMAL_ZERO' in result.stdout+result.stderr,result.stdout+result.stderr
   print('PASS: old shared-backface topology compiled and failed exact WATERLINE_NORMAL_ZERO assertion')
  else:print(result.stdout+result.stderr);result.check_returncode()
