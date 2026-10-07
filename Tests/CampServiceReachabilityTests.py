"""Use authored wilderness coordinates and production traversal for NPC arrival/exit.
Unity vector values are doubled; rendering and engine collision delivery are not tested.
"""
from pathlib import Path
import re,sys,tempfile,subprocess,os,importlib.util
r=Path(__file__).resolve().parents[1];source=(r/'Assets/Scripts/World/WorldBuilder.cs').read_text();world=source[source.index('private static void BuildWilderness('):source.index('private static void BuildCampFacilities(')]
arrays='\n'.join(re.search(r'Vector3\[\] '+name+r' = .*?;',world).group() for name in ('mainRoad','stream','trees'))
branch=re.search(r'"Woodland branch path", (new\[\] \{.*?\}), 1.9f',world).group(1)
river=re.search(r'WorldTraversal.SetRiver\(stream,.*?;',world).group()
rocks=re.findall(r'Rock\(parent, r, (new Vector3\([^)]*\)), ([0-9.]+f), [0-9]+\);',world)
assert len(rocks)==3
assert 'WorldTraversal.AddCircle(p, .22f)' in source and 'WorldTraversal.AddCircle(p, scale * .82f)' in source
assert 'WorldTraversal.AddCircle(p,.48f)' in source
assert 'new Vector3(i % 2 == 0 ? -4.4f : 4.4f, 0, i < 2 ? -8 : -12)' in source
body=arrays+'\nVector3[] branch='+branch+';\n'+river+'\nforeach(var p in trees)WorldTraversal.AddCircle(p,.22f);\n'+'\n'.join('WorldTraversal.AddCircle('+v+','+size+'*.82f);' for v,size in rocks)
body+='\nWorldTraversal.AddBox(new Vector3(10.5f,0,13),new Vector2(5,.85f));\nfor(int i=0;i<4;i++)WorldTraversal.AddCircle(new Vector3(i%2==0?-4.4f:4.4f,0,i<2?-8:-12),.48f);'
program='''using System;using Emberfall;using UnityEngine;
class Program{static int checks;static void Check(bool b,string s){checks++;if(!b)throw new Exception(s);}static void Main(){
foreach(float radius in new[]{.35f,.45f,.65f,.9f}){
WorldTraversal.Reset(ZoneKind.Wilderness);SETUP
for(int i=0;i<3;i++)HubSettlementPlan.RegisterNpcNavigation(i);
var entry=new Vector3(0,0,-16);Check(WorldTraversal.CanReach(entry,new Vector3(0,0,11),radius),"main road and bridge remain traversable");
for(int npc=0;npc<3;npc++){
var at=HubSettlementPlan.Npc(npc);var approach=at+new Vector3(0,0,-1.6f);var exit=at+new Vector3(0,0,-3.8f);
Check(WorldTraversal.IsWalkable(approach,radius)&&WorldTraversal.CanReach(entry,approach,radius),"authored camp approach reachable");
Check(Vector3.Distance(approach,at)<2.65f,"approach activates real NPC interaction radius");
Check(WorldTraversal.IsWalkable(exit,radius)&&WorldTraversal.CanReach(approach,exit,radius)&&Vector3.Distance(exit,at)>2.65f,"exit leaves interaction radius on a reachable path");
foreach(var road in new[]{mainRoad,branch})for(int segment=1;segment<road.Length;segment++){
float half=road==mainRoad?1.75f:.95f;
Check(CombatFx.SegmentDistance(at,road[segment-1],road[segment])>half+.43f+radius,"NPC body clears branch and main road");
Check(CombatFx.SegmentDistance(at+new Vector3(0,0,.65f),road[segment-1],road[segment])>half+.744f+radius,"NPC furniture clears road traversal envelope");
}
}
}
Console.WriteLine("PASS "+checks+" authored camp approach/exit, river crossing and road clearance assertions; no Unity scene execution");}}
'''.replace('SETUP',body)
spec=importlib.util.spec_from_file_location('cv',r/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
with tempfile.TemporaryDirectory(prefix='EmberfallCampReach-') as d:
 p=Path(d);plan=p/'HubSettlementPlan.cs';original=(r/'Assets/Scripts/World/HubSettlementPlan.cs').read_text();plan.write_text(original)
 project=cv.write_project(p/'project',[r/'Assets/Scripts/World/WorldTraversal.cs',plan,r/'Tests/DestructibleTraversalTests.cs'],program=program)
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1');dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet');subprocess.run([dotnet,'run','--project',str(project)],env=env,check=True)
 plan.write_text(original.replace('new Vector3(-6,0,-4)','new Vector3(-6,0,-5.5f)'))
 bad=subprocess.run([dotnet,'run','--project',str(project)],env=env,capture_output=True,text=True)
 assert bad.returncode and 'NPC body clears branch and main road' in bad.stdout+bad.stderr,bad.stdout+bad.stderr
 print('PASS compiled prior NPC-position control rejected at branch road clearance')
