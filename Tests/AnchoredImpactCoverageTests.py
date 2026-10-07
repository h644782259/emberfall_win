#!/usr/bin/env python3
"""Actual filled VFX + CombatSight/WorldTraversal; dense independent face oracle, not rendering."""
import os,sys,subprocess,tempfile,time
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
started=time.monotonic()
def member(s,sig):
 a=s.index(sig);b=s.index('{',a)+1;n=1
 while n:n+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
def once(s,a,b):
 assert s.count(a)==1,a
 return s.replace(a,b)
s=(ROOT/'Tests/FilledVfxAllocationTests.cs').read_text();shell='using System;using System.Linq;using System.Collections.Generic;using System.Reflection;using UnityEngine;'+s[s.index('namespace Emberfall'):]
shell=shell.replace(member(shell,'public static class CombatSight'),'').replace('    public enum CombatSightKind { Area }','')
math=(ROOT/'Tests/DestructibleTraversalTests.cs').read_text()
shell=shell.replace(member(shell,'public struct Vector2'),member(math,'public struct Vector2'))
shell=shell.replace('public struct Vector3\n','public partial struct Vector3\n').replace('public static class Mathf\n','public static partial class Mathf\n')
shell=once(shell,'public static class CombatFx{','public static partial class CombatFx{')
files=['Core/FilledVfxRecipes','Core/FilledVfxPlacement','Core/CombatVisualBudget','Core/CombatSightRules','Combat/CombatVisualLease','Combat/FilledSkillVfx','Combat/AnchoredImpactMesh','Combat/CombatSight','World/WorldTraversal','World/WorldTraversal.Platforms']
dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet')
with tempfile.TemporaryDirectory(prefix='anchored-impact-') as temp:
 for mode,expected in [('current',None),('old-origin-disk','legal near-wall impact retains positive-area primary and horizontal contact'),('old-sparse-face','retained anchored triangle crosses actual finite-cover LOS'),('old-anisotropic-motion','animated anchored vertex crosses actual finite-cover LOS')]:
  d=Path(temp)/mode;d.mkdir();(d/'Shell.cs').write_text(shell)
  for f in files:
   code=(ROOT/('Assets/Scripts/'+f+'.cs')).read_text()
   if f=='World/WorldTraversal':
    # Instrument actual traversal calls only in the managed fixture, never the production file.
    code=once(code,'public static partial class WorldTraversal\n    {','public static partial class WorldTraversal\n    {\n        public static int TestSegmentCalls,TestSolidProbes,TestTriangleCalls,TestFanCalls;')
    code=once(code,'            int samples = Mathf.Max(1, Mathf.CeilToInt(CombatFx.Flat(to - from).magnitude / .18f));','            TestSegmentCalls++;\n            int samples = Mathf.Max(1, Mathf.CeilToInt(CombatFx.Flat(to - from).magnitude / .18f));')
    code=once(code,'            Vector2 p = new Vector2(point.x, point.z);\n            if (p.sqrMagnitude','            TestSolidProbes++;\n            Vector2 p = new Vector2(point.x, point.z);\n            if (p.sqrMagnitude')
    code=once(code,'            const double clearance=.04f;','            TestTriangleCalls++;\n            const double clearance=.04f;')
    code=once(code,'            Vector3 center=new Vector3(obstacle.Center.x,0,obstacle.Center.y);','            TestFanCalls++;\n            Vector3 center=new Vector3(obstacle.Center.x,0,obstacle.Center.y);')
   if mode=='old-origin-disk' and f.endswith('AnchoredImpactMesh'):
    code=once(code,'CombatSight.VisualTriangle(origin,a,b,c)','CombatSight.VisualFootprint(origin,(a+b+c)/3,Mathf.Max(CombatFx.Flat(a-(a+b+c)/3).magnitude,Mathf.Max(CombatFx.Flat(b-(a+b+c)/3).magnitude,CombatFx.Flat(c-(a+b+c)/3).magnitude))+.131f)')
   # Animation control restores both former clipping and anisotropic motion;
   # conservative new clipping alone can mask the old motion defect.
   if mode in ('old-sparse-face','old-anisotropic-motion') and f.endswith('AnchoredImpactMesh'):
    code=once(code,'CombatSight.VisualTriangle(origin,a,b,c)','CombatSight.Area(origin,(a+b+c)/3)&&CombatSight.Area(origin,(a+b)*.5f)&&CombatSight.Area(origin,(b+c)*.5f)&&CombatSight.Area(origin,(c+a)*.5f)')
   if mode=='old-anisotropic-motion' and f.endswith('FilledSkillVfx'):
    code=once(code,'float radial=Mathf.Min(1,Mathf.Min(scale.x,scale.z));scale.x=scale.z=radial;','scale.x=Mathf.Min(1,scale.x);scale.z=Mathf.Min(1,scale.z);')
   (d/(Path(f).name+'.cs')).write_text(code)
  (d/'Tests.cs').write_text((ROOT/'Tests/AnchoredImpactCoverageTests.cs').read_text());(d/'Program.cs').write_text('System.Console.WriteLine(AnchoredImpactCoverageTests.'+('RunMotion()' if mode=='old-anisotropic-motion' else 'RunNearWall()' if mode=='old-origin-disk' else 'Run()')+');');(d/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>');p=d/'Test.csproj';p.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>')
  build=subprocess.run([dotnet,'build',str(p),'--configfile',str(d/'NuGet.Config'),'-v:q'],capture_output=True,text=True)
  if build.returncode:print(build.stdout+build.stderr);build.check_returncode()
  result=subprocess.run([dotnet,str(d/'bin/Debug/net8.0/Test.dll')],capture_output=True,text=True)
  if expected:
   assert result.returncode and 'System.Exception: '+expected in result.stdout+result.stderr,result.stdout+result.stderr
   print('PASS: '+mode+' compiled and failed exact assertion: '+expected)
  else:print(result.stdout+result.stderr);result.check_returncode()

print("Managed script elapsed seconds:",round(time.monotonic()-started,1),"(not device performance)")
