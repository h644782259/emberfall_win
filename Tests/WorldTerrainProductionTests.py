#!/usr/bin/env python3
"""Exercise real terrain/traversal/contact code with the existing numeric Unity shim."""
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
static void Main(){
 WorldTraversal.Reset(ZoneKind.Wilderness);WorldTerrain.Configure(ZoneKind.Wilderness,0);
 foreach(var at in new[]{new Vector3(0,0,-10),new Vector3(0,0,-1),new Vector3(0,0,11),new Vector3(-18,0,4),new Vector3(14,0,-5)})Check(WorldTerrain.Height(at)==0,"camp, crossing, portal and river stay flat");
 foreach(var at in new[]{new Vector3(-13,0,-11),new Vector3(16,0,1),new Vector3(-17,0,13)})Check(WorldTerrain.Height(at)>.8f,"visible hill relief");
 float fall=0;var player=WorldTraversal.NearestWalkable(new Vector3(16,0,-4));var monster=player;
 for(int i=0;i<180;i++){
  var before=player;player=WorldTraversal.MovePlayer(player,new Vector3(0,0,.04f),1f/60,ref fall);
  monster=WorldTraversal.Move(monster,new Vector3(0,0,.04f),.65f);
  Check(Math.Abs(player.y-WorldTerrain.Height(player))<.001,"ascending/descending player ground contact");
  Check(Math.Abs(monster.y-WorldTerrain.Height(monster))<.001,"ascending/descending creature ground contact");
  Check(Math.Abs(player.y-before.y)<.02,"no sudden vertical step");
 }
 var start=WorldTraversal.NearestWalkable(new Vector3(15,0,-1));var target=WorldTraversal.ResolveSkillLanding(start,new Vector3(0,0,1),2,.45f,22);
 Check(Math.Abs(target.y-WorldTerrain.Height(target))<.001,"movement skill ground landing");
 Check(WorldTraversal.TryResolvePlatformJump(start,new Vector3(0,0,1),2,.45f,out target)&&Math.Abs(target.y-WorldTerrain.Height(target))<.001,"jump lands on natural slope");
 var boundary=new Vector3[64];CombatSight.FillAreaBoundary(boundary,start,3);
 foreach(var point in boundary)Check(Math.Abs(point.y-WorldTraversal.SurfaceHeight(point)-.12f)<.001,"skill boundary follows slope");
 WorldTraversal.AddBox(new Vector3(15,0,2),new Vector2(2,1));
 Check(!WorldTraversal.HasGroundPath(start,new Vector3(15,0,4)),"slope preserves cover");
 WorldTraversal.SetArenaRadius(32);Check(WorldTraversal.CanReach(new Vector3(0,0,-10),new Vector3(28,0,12)),"expanded camp outer route reachable");
 Check(WorldTerrain.Height(new Vector3(-24,0,-18))>3,"outer camp ridge has stronger relief");
 WorldTraversal.Reset(ZoneKind.Dungeon);WorldTraversal.SetArenaRadius(28);WorldTerrain.Configure(ZoneKind.Dungeon,0,28);
 Check(WorldTerrain.Height(new Vector3(0,0,-16))==0&&WorldTerrain.Height(new Vector3(9,0,0))==0,"dungeon return and crossing datum remain flat");
 Check(WorldTerrain.Height(new Vector3(-14,0,-11))>2,"expanded dungeon archive ridge rises");
 player=WorldTraversal.NearestWalkable(new Vector3(-14,0,-14));monster=player;fall=0;
 for(int i=0;i<180;i++){player=WorldTraversal.MovePlayer(player,new Vector3(0,0,.045f),1f/60,ref fall);monster=WorldTraversal.Move(monster,new Vector3(0,0,.045f),.65f);Check(Math.Abs(player.y-WorldTerrain.Height(player))<.001,"dungeon player ground contact");Check(Math.Abs(monster.y-WorldTerrain.Height(monster))<.001,"dungeon monster ground contact");}
 CombatSight.FillAreaBoundary(boundary,player,3);foreach(var point in boundary)Check(Math.Abs(point.y-WorldTraversal.SurfaceHeight(point)-.12f)<.001,"dungeon skill boundary conforms to relief");
 WorldTraversal.Reset(ZoneKind.Dungeon);
 WorldTraversal.AddPlatform(Vector3.zero,new Vector2(2.2f,1.8f),.65f);
 Check(WorldTraversal.TryResolvePlatformJump(new Vector3(0,0,-2.0f),new Vector3(0,0,1),2.5f,.45f,out target)&&target.y==.65f,"original raised platform remains reachable");
 Console.WriteLine(n+" production terrain, movement and skill contact assertions passed");}}
''')
 (p/'Check.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><NuGetAudit>false</NuGetAudit></PropertyGroup></Project>')
 (p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 subprocess.run([sys.argv[1] if len(sys.argv)>1 else 'dotnet','run','--project',str(p/'Check.csproj')],check=True)
