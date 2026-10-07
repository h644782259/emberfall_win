#!/usr/bin/env python3
"""Actual chapter room generator/atmosphere/ribbon and traversal; primitive rendering is recorded."""
import os,sys,tempfile,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
def member(s,sig):
 a=s.index(sig);b=s.index('{',a)+1;n=1
 while n:n+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
w=(ROOT/'Assets/Scripts/World/WorldBuilder.cs').read_text();chapter=(ROOT/'Assets/Scripts/World/WorldBuilder.ChapterRooms.cs').read_text();water=(ROOT/'Assets/Scripts/World/WorldBuilder.WaterSurface.cs').read_text()
s=(ROOT/'Tests/FilledVfxAllocationTests.cs').read_text();shell='using System;using System.Linq;using System.Collections.Generic;using System.Reflection;using UnityEngine;'+s[s.index('namespace Emberfall'):]
math=(ROOT/'Tests/DestructibleTraversalTests.cs').read_text();shell=shell.replace(member(shell,'public struct Vector2'),member(math,'public struct Vector2'));shell=shell.replace('public struct Vector3\n','public partial struct Vector3\n').replace('public static class Mathf\n','public static partial class Mathf\n').replace('public static class CombatFx{','public static partial class CombatFx{')
shell=shell.replace('public enum PrimitiveType{Sphere}','public enum PrimitiveType{Sphere,Cube,Cylinder}')
shell=shell.replace('public void RecalculateNormals(){}','public void SetVertices(List<Vector3> v){vertices=v.ToArray();}public void SetTriangles(List<int> t,int sub){triangles=t.ToArray();}public void RecalculateNormals(){}')
shell=shell.replace('public Material(Shader shader){}','public Material(Shader shader){}public bool HasProperty(string n)=>true;public void SetColor(string n,Color v){}')
shell=shell.replace('public struct Color{','public struct Color{public static Color operator*(Color c,float s)=>new Color(c.r*s,c.g*s,c.b*s,c.a*s);')
shell=shell.replace('public void SetParent(Transform value,bool worldPositionStays){parent=value;}','public void SetParent(Transform value,bool worldPositionStays){parent=value;}public T[] GetComponentsInChildren<T>()where T:Component=>GameObject.All.Where(o=>o.transform.parent==this).Select(o=>o.GetComponent<T>()).Where(c=>c!=null).ToArray();')
base='using UnityEngine;using System.Collections.Generic;namespace Emberfall{public static partial class WorldBuilder{'+member(w,'private static void Ribbon(')+member(w,'private static GameObject Geometry(')+member(water,'private static void RibbonSection(')+'}'+member(w,'public sealed class WorldResources')+'}'
dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet')
with tempfile.TemporaryDirectory(prefix='chapter-world-presentation-') as tmp:
 for old in (False,True):
  d=Path(tmp)/str(old);d.mkdir();(d/'Shell.cs').write_text(shell);(d/'Base.cs').write_text(base)
  generator=member(chapter,'private static void BuildChapterRoom(')+member(chapter,'private static void ApplyChapterAtmosphere(')
  if old:generator=generator.replace('            ApplyChapterAtmosphere(parent,plan);','')
  (d/'Chapter.cs').write_text('using UnityEngine;namespace Emberfall{public static partial class WorldBuilder{'+generator+'}}')
  for f in ['Assets/Scripts/World/WorldTraversal.cs','Assets/Scripts/World/WorldTraversal.Platforms.cs','Assets/Scripts/World/ChapterRoomGeometry.cs','Tests/ChapterNodeWorldProductionTests.cs']:(d/Path(f).name).write_text((ROOT/f).read_text())
  (d/'Program.cs').write_text('System.Console.WriteLine(ChapterNodeWorldProductionTests.Run());');(d/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>');project=d/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>')
  build=subprocess.run([dotnet,'build',str(project),'--configfile',str(d/'NuGet.Config'),'-v:q'],capture_output=True,text=True)
  if build.returncode:print(build.stdout+build.stderr);build.check_returncode()
  result=subprocess.run([dotnet,str(d/'bin/Debug/net8.0/Test.dll')],capture_output=True,text=True)
  if old:
   assert result.returncode and 'System.Exception: chapter generator selects readable node fog' in result.stdout+result.stderr,result.stdout+result.stderr
   print('PASS: compiled old generic atmosphere rejected at actual generator entry')
  else:print(result.stdout+result.stderr);result.check_returncode()
