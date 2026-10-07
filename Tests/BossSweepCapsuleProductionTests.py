#!/usr/bin/env python3
"""Production boss clipping/containment/contour and exact WorldTraversal solids; no Unity render claim."""
import os,sys,subprocess,tempfile
from pathlib import Path
r=Path(__file__).resolve().parents[1];dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet')
def member(s,key):
 a=s.index(key);b=s.index('{',a)+1;n=1
 while n:n+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
s=(r/'Assets/Scripts/Combat/LargeExpeditionBoss.cs').read_text()
assert 'BeamContains(game.Player.transform.position,from,to)' in s
assert 'SetPosition(j,BeamBoundary(from,to,direction,side,j))' in s
methods='\n'.join(member(s,k) for k in ['private static Vector3 ClipBeam(', 'private static bool BeamContains(', 'private static Vector3 BeamBoundary(']).replace('private static','public static')
fixture=(r/'Tests/DestructibleTraversalTests.cs').read_text();fixture='using System;using UnityEngine;'+fixture[fixture.index('namespace Emberfall'):]
test='''using System;using Emberfall;using UnityEngine;
class Test{static int n;static void C(bool b,string s){n++;if(!b)throw new Exception(s);}static void Main(){
var a=new Vector3(0,0,0);var b=new Vector3(0,0,9);var dir=new Vector3(0,0,1);
WorldTraversal.Reset(ZoneKind.Dungeon);C(Boss.ClipBeam(a,b).z==9,"clear beam retains length");
C(Boss.BeamContains(new Vector3(.94f,0,4),a,b),"body allowance is actual danger");C(!Boss.BeamContains(new Vector3(.96f,0,4),a,b),"outside side stays safe");
C(Boss.BeamContains(new Vector3(0,0,9.94f),a,b)&&!Boss.BeamContains(new Vector3(0,0,9.96f),a,b),"round endcap matches body allowance");
foreach(float x in new[]{0f,.7f,1.1499f}){WorldTraversal.Reset(ZoneKind.Dungeon);WorldTraversal.AddDynamicCircle(new Vector3(x,0,4.09f),.2f);var end=Boss.ClipBeam(a,b);C(end.z<4.1f,"full width pillar tangent clips before cover");C(WorldTraversal.HasClearSweepCapsule(a,end,.95f),"clipped full capsule is clear");C(!Boss.BeamContains(new Vector3(0,0,7),a,end),"behind clipped pillar remains safe");
for(int side=-1;side<=1;side+=2)for(int j=0;j<18;j++){var edge=Boss.BeamBoundary(a,end,dir,side,j);C(Math.Abs(CombatFx.SegmentDistance(edge,a,end)-.95f)<.0001f,"warning includes compensated body and endcaps");C(Vector3.Distance(edge,new Vector3(x,0,4.09f))>=.199f,"warning cannot translate through pillar");}}
WorldTraversal.Reset(ZoneKind.Dungeon);WorldTraversal.AddDynamicBox(new Vector3(.94f,0,4),new Vector2(.02f,.02f));C(Boss.ClipBeam(a,b).z<4,"thin box grazes clip full width");
WorldTraversal.Reset(ZoneKind.Dungeon);WorldTraversal.AddDynamicCircle(new Vector3(1.151f,0,4),.2f);C(Boss.ClipBeam(a,b).z==9,"truly clear tangent passes");
Console.WriteLine("PASS: "+n+" actual sweep clip/containment/contour/traversal geometry checks (managed)");}}
'''
with tempfile.TemporaryDirectory(prefix='boss-capsule-') as tmp:
 p=Path(tmp);(p/'World.cs').write_text((r/'Assets/Scripts/World/WorldTraversal.cs').read_text());(p/'Fixture.cs').write_text(fixture);(p/'Test.cs').write_text(test)
 (p/'Platforms.cs').write_text((r/'Assets/Scripts/World/WorldTraversal.Platforms.cs').read_text())
 (p/'Phase.cs').write_text((r/'Assets/Scripts/Core/LargeBossPhaseState.cs').read_text())
 project=p/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 for old in [False,True]:
  body=methods
  if old:body=body.replace('HasClearSweepCapsule(from,to,BeamDangerRadius)','HasGroundPath(from,to,.12f)').replace('HasClearSweepCapsule(from,Vector3.Lerp(from,to,mid),BeamDangerRadius)','HasGroundPath(from,Vector3.Lerp(from,to,mid),.12f)')
  (p/'Boss.cs').write_text('using UnityEngine;namespace Emberfall{static class Boss{const float BeamDangerRadius=LargeBossPhaseState.BeamHalfWidth+.4f;'+body+'}}')
  subprocess.run([dotnet,'build',str(project),'--configfile',str(p/'NuGet.Config'),'-v:q'],check=True,stdout=subprocess.DEVNULL)
  q=subprocess.run([dotnet,str(p/'bin/Debug/net8.0/Test.dll')],capture_output=True,text=True)
  if old:assert q.returncode and ('clipped full capsule is clear' in q.stderr or 'full width pillar tangent clips before cover' in q.stderr),q.stdout+q.stderr;print('PASS: compiled old center-ray control fails exact capsule safety assertion')
  else:print(q.stdout,end='');assert q.returncode==0,q.stderr
