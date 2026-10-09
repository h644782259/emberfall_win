#!/usr/bin/env python3
"""Execute production traversal using numeric Unity shims, without the Unity renderer."""
import os, sys, subprocess, tempfile
from pathlib import Path
root=Path(__file__).resolve().parents[1]
dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
with tempfile.TemporaryDirectory(prefix='movement-landings-') as work:
 p=Path(work)
 for name in ['WorldTraversal.cs','WorldTraversal.Platforms.cs']:
  if (root/'Assets/Scripts/World'/name).exists(): (p/name).write_text((root/'Assets/Scripts/World'/name).read_text())
 fixture=(root/'Tests/DestructibleTraversalTests.cs').read_text()
 fixture='\n'.join(line for line in fixture.splitlines() if 'public static class PlayerUpgradeRules' not in line)
 (p/'Shims.cs').write_text('using System;using Emberfall;using UnityEngine;\n'+fixture[fixture.index('namespace Emberfall'):])
 rules=(root/'Assets/Scripts/Combat/PlayerUpgradeRules.cs').read_text()
 start=rules.index('        public static float FindSafeBlinkDistance')
 end=rules.index('        public static float CounterAfterAttack',start)
 (p/'Rules.cs').write_text('using System;namespace Emberfall{public static class PlayerUpgradeRules{'+rules[start:end]+'}}')
 (p/'Program.cs').write_text("""
using System;using Emberfall;using UnityEngine;
class Program{
 static int n;
 static void Check(bool v,string m){n++;if(!v)throw new Exception(m);}
 static void Main(){
  var right=new Vector3(1,0,0);
  WorldTraversal.Reset(ZoneKind.Wilderness);
  var free=WorldTraversal.ResolveSkillLanding(Vector3.zero,right,7,.45f,21.35f);
  Check(Math.Abs(free.x-7)<.001,"full movement on open ground");
  WorldTraversal.AddBox(new Vector3(3,0,0),new Vector2(1,5));
  var stopped=WorldTraversal.ResolveSkillLanding(Vector3.zero,right,7,.45f,21.35f);
  Check(stopped.x>2&&stopped.x<=2.05f,"stop before wall rather than reject whole cast");
  var near=new Vector3(2.04f,0,0);
  var contact=WorldTraversal.ResolveSkillLanding(near,right,7,.45f,21.35f);
  Check((contact-near).magnitude<.001,"wall contact safely casts in place");
  Check(WorldTraversal.IsWalkable(stopped,.45f),"clipped landing has body clearance");
  WorldTraversal.Reset(ZoneKind.Wilderness);
  var edge=WorldTraversal.ResolveSkillLanding(new Vector3(21,0,0),right,7,.45f,21.35f);
  Check(edge.x<=21.351f&&edge.x>=21,"arena edge never overshoots");
  WorldTraversal.SetRiver(new[]{new Vector3(-8,0,0),new Vector3(8,0,0)},2,new Rect(-1,-2,2,4));
  var bank=new Vector3(4,0,-2);
  var water=WorldTraversal.ResolveSkillLanding(bank,new Vector3(0,0,1),2,.45f,21.35f);
  Check(WorldTraversal.IsWalkable(water,.45f)&&water.z< -1.44f,"water target falls back to reachable bank");
  foreach(float radius in new[]{.325f,.37f,.45f}){
   WorldTraversal.Reset(ZoneKind.Wilderness);WorldTraversal.AddJumpPlatform(Vector3.zero,radius,.7f);
   Check(!WorldTraversal.IsWalkable(Vector3.zero),"platform stays solid on the ground");
   Vector3 top;Check(WorldTraversal.TryResolvePlatformJump(new Vector3(-2,0,0),right,2.2f,.45f,out top),"jump onto circular support");
   Check(Math.Abs(top.y-.7f)<.001&&WorldTraversal.CanStand(top),"land and remain above surface");
   Check(Math.Abs(WorldTraversal.Move(top,Vector3.zero).y-.7f)<.001,"idle does not flatten platform height");
   Vector3 down;Check(WorldTraversal.TryResolvePlatformJump(top,right,3,.45f,out down)&&down.y==0,"jump off platform onto ground");
   WorldTraversal.AddBox(new Vector3(-1,0,0),new Vector2(.2f,4));
   Check(!WorldTraversal.TryResolvePlatformJump(new Vector3(-2,0,0),right,2.2f,.45f,out top),"solid wall still blocks jump arc");
  }
  WorldTraversal.Reset(ZoneKind.Wilderness);WorldTraversal.AddPlatform(Vector3.zero,new Vector2(2.4f,1.8f),.65f);
  Vector3 box;Check(WorldTraversal.TryResolvePlatformJump(new Vector3(-3,0,0),right,3,.45f,out box)&&box.y==.65f,"existing box platforms preserved");
  foreach(int fps in new[]{30,60,120}){
   WorldTraversal.Reset(ZoneKind.Wilderness);WorldTraversal.AddPlatform(Vector3.zero,new Vector2(2.4f,1.8f),.65f);
   var actor=new Vector3(0,.65f,0);float velocity=0;bool fell=false;
   for(int frame=0;frame<fps;frame++){
    var previous=actor;
    actor=WorldTraversal.MovePlayer(actor,right*(3f/fps),1f/fps,ref velocity);
    if(actor.y<previous.y&&actor.y>0)fell=true;
    Check(actor.y>=0&&actor.y<=.651f,"gravity never rises or sinks below ground");
   }
   Check(actor.x>2.9f&&actor.y==0&&fell,"walk off naturally at "+fps+" FPS");
   Check(WorldTraversal.IsWalkable(actor),"walk-off ends outside platform sides");
  }
  WorldTraversal.Reset(ZoneKind.Wilderness);WorldTraversal.AddPlatform(Vector3.zero,new Vector2(2.4f,1.8f),.95f);
  var stoppedFall=new Vector3(0,.95f,0);float fallSpeed=0;
  for(int i=0;i<36;i++)stoppedFall=WorldTraversal.MovePlayer(stoppedFall,right*.05f,1f/60,ref fallSpeed);
  Check(stoppedFall.y>0&&stoppedFall.y<.95f,"edge departure starts a gradual fall");
  float stoppedX=stoppedFall.x;
  for(int i=0;i<60;i++)stoppedFall=WorldTraversal.MovePlayer(stoppedFall,Vector3.zero,1f/60,ref fallSpeed);
  Check(stoppedFall.y==0&&Math.Abs(stoppedFall.x-stoppedX)<.001f,"releasing movement still falls without horizontal snap");
  var grounded=WorldTraversal.MovePlayer(new Vector3(-3,0,0),right*3,1,ref fallSpeed);
  Check(grounded.x<-1.6f&&grounded.y==0,"walking cannot climb a vertical box face");
  WorldTraversal.Reset(ZoneKind.Wilderness);
  WorldTraversal.SetRiver(new[]{new Vector3(-11,0,2),new Vector3(-5,0,.5f),new Vector3(0,0,-1)},2.4f,new Rect(-1.9f,-3.9f,3.8f,5.8f));
  for(int i=0;i<2;i++)Check(WorldTraversal.IsWalkable(new Vector3(-5.5f-i*3.4f,0,-3.3f),1.7f),"camp river block footprint is on dry bank");
  Console.WriteLine(n+" production landing checks passed");
 }
}
""")
 (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>')
 (p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1')
 subprocess.run([dotnet,'run','--project',str(p/'Test.csproj')],env=env,check=True)
# Guard the production integration that previously re-flattened every jump frame.
controller=(root/'Assets/Scripts/Combat/PlayerController.cs').read_text()
arc=controller.split('private void AdvanceJump')[1].split('private void TraversalFailure')[0]
assert 'WorldTraversal.Move(' not in arc
assert 'WorldTraversal.TryResolvePlatformJump(origin,travel' in controller
assert 'WorldTraversal.ResolveSkillLanding(previous,flat' in controller
assert 'WorldTraversal.ResolveSkillLanding(jumpOrigin,transform.forward' in controller
print('PASS player cast and jump integration')
