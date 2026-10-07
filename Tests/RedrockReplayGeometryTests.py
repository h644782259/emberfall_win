#!/usr/bin/env python3
"""Run actual chapter/navigation geometry and a compiled continuous-wall negative control."""
import importlib.util, os, subprocess, sys, tempfile
from pathlib import Path
root=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py')
cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet')
files=[root/p for p in ['Assets/Scripts/World/WorldTraversal.cs','Assets/Scripts/World/WorldTraversal.Platforms.cs','Assets/Scripts/World/ChapterRoomGeometry.cs','Assets/Scripts/Core/ChapterProgression.cs','Assets/Scripts/Core/RoomTactics.cs','Assets/Scripts/Core/ArenaPulseRules.cs','Assets/Scripts/World/ChapterHazardGeometry.cs','Assets/Scripts/World/TacticalRoomGeometry.cs','Tests/DestructibleTraversalTests.cs','Tests/ChapterGeometryFixture.cs','Tests/ChapterRoomGeometryTests.cs','Tests/ChapterFormationGeometryTests.cs']]
# Execute the actual host placement method against real WorldTraversal, including fallback.
host=(root/'Assets/Scripts/Core/GameSession.Chapter.cs').read_text()
start=host.index('private bool TryChapterSpawn(');opening=host.index('{',start);end=opening+1;depth=1
while depth:
 depth+=(host[end]=='{')-(host[end]=='}');end+=1
program=r'''
using System;using System.Collections.Generic;using Emberfall;using UnityEngine;
class ReplaySpawnProbe {
 ChapterRoomPlan chapterPlan;bool ForestMobileLineup=>false;int ChapterSeed=>chapterPlan.Seed;int ChapterRoomIndex=>chapterPlan.Room;
 METHOD
 static Vector3[] Resolve(int seed,float radius){
  var plan=ChapterRoomGeometry.Plan(ChapterNode.Redrock,0,seed);WorldTraversal.Reset(ZoneKind.Dungeon);ChapterRoomGeometry.Register(plan);
  var probe=new ReplaySpawnProbe{chapterPlan=plan};var occupied=new List<Vector3>();
  for(int i=0;i<6;i++){Vector3 at;if(!probe.TryChapterSpawn(i,true,occupied,radius,out at))throw new Exception("room zero placement failed");occupied.Add(at);}
  return occupied.ToArray();
 }
 static void Main(){
  int checks=0;
  foreach(int raw in new[]{0,1,2,3,4,255,256,999999})foreach(bool split in new[]{false,true}){
   int encoded=ChapterRoomGeometry.RedrockReplaySeed(raw,split);
   if((encoded&0xfffff)!=raw||(encoded&255)!=(raw&255)||ChapterRoomGeometry.RedrockReplaySeed(encoded,split)!=encoded||ChapterRoomGeometry.RedrockSplitRoute(encoded)!=split)throw new Exception("ROUTE_LOW_BYTE preserve generated raw bits and chosen flag");checks++;
  }
  var seeds=new List<int>();for(int seed=0;seed<64;seed++)seeds.Add(seed);seeds.AddRange(new[]{255,256,999999});
  foreach(int raw in seeds)foreach(float radius in new[]{.45f,.65f,.9f,1.3f}){
   var baseline=Resolve(raw,radius);
   foreach(bool split in new[]{false,true}){
    var actual=Resolve(ChapterRoomGeometry.RedrockReplaySeed(raw,split),radius);
    for(int i=0;i<6;i++){if(Vector3.Distance(baseline[i],actual[i])>.00001f)throw new Exception("ROOM_ZERO_ISOLATION route flag changes actual host fallback placement");checks++;}
   }
  }
  Console.WriteLine("PASS: "+checks+" actual room-zero host spawn isolation and full jitter-byte assertions");
  Console.WriteLine(ChapterRoomGeometryTests.Run());Console.WriteLine(ChapterFormationGeometryTests.Run());
 }
}
'''.replace('METHOD',host[start:end])
with tempfile.TemporaryDirectory(prefix='redrock-route-') as folder:
 out=Path(folder);config=out/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(out/'cli'),DOTNET_NOLOGO='1')
 for mutation in ["positive","wall","low-bit"]:
  mutant=mutation!="positive"
  source=root/'Assets/Scripts/World/ChapterRoomGeometry.cs';inputs=files
  if mutant:
   changed=out/'ContinuousWall.cs';text=source.read_text();before='if(room==1&&RedrockSplitRoute(seed))';assert before in text
   # Retain four obstacles/seed encoding but close the gap: identity-only tests cannot catch this.
   if mutation=='wall':text=text.replace('new Vector3(0,0,-5),new Vector2(4,4)','new Vector3(0,0,-4),new Vector2(4,6)').replace('new Vector3(0,0,3),new Vector2(4,4)','new Vector3(0,0,2),new Vector2(4,6)')
   else:text=text.replace('private const int RedrockRouteBit=1<<20;','private const int RedrockRouteBit=1<<2;')
   changed.write_text(text);inputs=[changed if path==source else path for path in files]
  project=cv.write_project(out/mutation,inputs,program=program)
  subprocess.run([dotnet,'build',str(project),'--configfile',str(config),'-v:q'],env=env,check=True)
  run=subprocess.run([dotnet,str(project.parent/'bin/Debug/net8.0/Validation.dll')],env=env,capture_output=True,text=True)
  if mutant:
   expected='SPLIT_CROSSING' if mutation=='wall' else 'ROUTE_LOW_BYTE'
   assert run.returncode and 'System.Exception: '+expected in run.stdout+run.stderr,run.stdout+run.stderr
   print('PASS: compiled '+mutation+' control fails exact '+expected+' oracle')
  else:
   print(run.stdout,end='');assert not run.returncode,run.stderr
