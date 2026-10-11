#!/usr/bin/env python3
"""Exercise production minimap classification and cache identity with numeric Unity shims."""
from pathlib import Path
import subprocess, tempfile, sys
root=Path(__file__).resolve().parents[1]
with tempfile.TemporaryDirectory(prefix='world-terrain-check-') as work:
 p=Path(work)
 fixture=(root/'Tests/DestructibleTraversalTests.cs').read_text()
 fixture='\n'.join(line for line in fixture.splitlines() if 'public static class PlayerUpgradeRules' not in line)
 rules=(root/'Assets/Scripts/Combat/PlayerUpgradeRules.cs').read_text()
 first=rules.index('        public static float FindSafeBlinkDistance');last=rules.index('        public static float CounterAfterAttack',first)
 (p/'LandingRules.cs').write_text('using System;namespace Emberfall{public static class PlayerUpgradeRules{'+rules[first:last]+'}}')
 (p/'Fixture.cs').write_text('using System;using UnityEngine;'+fixture[fixture.index('namespace Emberfall'):])
 for name,path in [('World.cs','Assets/Scripts/World/WorldTraversal.cs'),('Sight.cs','Assets/Scripts/Combat/CombatSight.cs'),('Rules.cs','Assets/Scripts/Core/CombatSightRules.cs')]:
  (p/name).write_text((root/path).read_text())
 platforms=root/'Assets/Scripts/World/WorldTraversal.Platforms.cs'
 if platforms.exists():(p/'Platforms.cs').write_text(platforms.read_text())
 (p/'Program.cs').write_text('''using System;using UnityEngine;using Emberfall;
class Program{
static int n;static void Check(bool v,string label){n++;if(!v)throw new Exception(label);}
static void Layout(){WorldTraversal.Reset(ZoneKind.Wilderness);WorldTraversal.SetRiver(new[]{new Vector3(-8,0,0),new Vector3(8,0,0)},2,new Rect(-1,-2,2,4));WorldTraversal.AddBox(new Vector3(4,0,4),new Vector2(2,2));}
static void Main(){
 Layout();string original=WorldTraversal.NavigationMapKey;int revision=WorldTraversal.Revision;
 Check(object.ReferenceEquals(original,WorldTraversal.NavigationMapKey),"revision key reused without allocation");
 for(int z=-24;z<=24;z++)for(int x=-24;x<=24;x++){
 var p=new Vector3(x,0,z);int expected=WorldTraversal.IsWalkable(p,.16f)?1:WorldTraversal.IsOpenWater(p,.16f)?2:0;
 Check(WorldTraversal.MapSurface(p)==expected,"single-pass map matches gameplay geometry");}
 Layout();Check(WorldTraversal.Revision!=revision&&original==WorldTraversal.NavigationMapKey,"rebuilt camp has stable map identity");
 var obstacle=WorldTraversal.AddDynamicCircle(new Vector3(-3,0,0),1);string blocked=WorldTraversal.NavigationMapKey;
 Check(blocked!=original&&WorldTraversal.MapSurface(new Vector3(-3,0,0))==0,"dynamic blocker invalidates cached map");
 WorldTraversal.RemoveDynamicObstacle(obstacle);Check(original==WorldTraversal.NavigationMapKey&&WorldTraversal.MapSurface(new Vector3(-3,0,0))==2,"removing blocker restores original water identity");
 Check(WorldTraversal.MapSurface(Vector3.zero)==1,"bridge remains land");
 WorldTraversal.SetRiver(new[]{new Vector3(-8,0,1),new Vector3(8,0,1)},2,new Rect(-1,-2,2,4));Check(original!=WorldTraversal.NavigationMapKey,"river geometry invalidates map");
 WorldTraversal.Reset(ZoneKind.Dungeon);Check(original!=WorldTraversal.NavigationMapKey&&WorldTraversal.MapSurface(new Vector3(20,0,0))==0,"arena radius and river reset invalidate map");
 Console.WriteLine(n+" production minimap geometry and cache identity assertions passed");}}
''')
 (p/'Check.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><NuGetAudit>false</NuGetAudit></PropertyGroup></Project>')
 (p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 subprocess.run([sys.argv[1] if len(sys.argv)>1 else 'dotnet','run','--project',str(p/'Check.csproj')],check=True)
