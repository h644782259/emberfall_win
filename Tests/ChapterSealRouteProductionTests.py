#!/usr/bin/env python3
import os,sys,tempfile,subprocess,argparse
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser();p.add_argument('dotnet',nargs='?',default=os.environ.get('DOTNET','dotnet'));p.add_argument('--definition',default=str(ROOT/'Assets/Scripts/Core/ChapterProgression.cs'));args=p.parse_args()
files=['Core/ChapterCombatRun','UI/ChapterSealPresentation','Core/RoomTactics','Core/ArenaPulseRules','World/WorldTraversal','World/WorldTraversal.Platforms','World/ChapterRoomGeometry','World/ChapterHazardGeometry','World/TacticalRoomGeometry']
with tempfile.TemporaryDirectory(prefix='chapter-seal-route-') as temp:
 for mode,expected in [('current',None),('old-empty-seal-hud','HUD keeps half-complete A and B independent'),('old-sequential-seals','HUD keeps half-complete A and B independent'),('old-room-one-boss','star room-zero boss finalizes without a phantom second room'),('old-fixed-heat-route','heat hazard follows mirrored hunt-side short approach')]:
  d=Path(temp)/mode;d.mkdir()
  for f in files:
   s=(ROOT/('Assets/Scripts/'+f+'.cs')).read_text()
   if mode=='old-empty-seal-hud' and f.endswith('ChapterSealPresentation'):s=s.replace('Seconds=seconds;', 'Seconds=0;')
   if mode=='old-sequential-seals' and f.endswith('ChapterCombatRun'):s=s.replace('if(Objective!=RoomObjective.Purify||index<0','if(index!=Seals||Objective!=RoomObjective.Purify||index<0')
   if mode=='old-room-one-boss' and f.endswith('ChapterCombatRun'):s=s.replace('if(Finished||Objective!=RoomObjective.Boss','if(Finished||RoomIndex!=1||Objective!=RoomObjective.Boss')
   if mode=='old-fixed-heat-route' and f.endswith('ChapterRoomGeometry'):s=s.replace('new Vector3(mirror*2.7f,0,-4);HazardEnd=new Vector3(mirror*5.8f,0,-4)','new Vector3(-4,0,1);HazardEnd=new Vector3(4,0,1)')
   (d/(Path(f).name+'.cs')).write_text(s)
  (d/'ChapterProgression.cs').write_text(Path(args.definition).read_text())
  for f in ['DestructibleTraversalTests','ChapterGeometryFixture','ChapterRoomGeometryTests','ChapterFormationGeometryTests','ChapterSealRouteProductionTests']:(d/(f+'.cs')).write_text((ROOT/('Tests/'+f+'.cs')).read_text())
  (d/'Program.cs').write_text('System.Console.WriteLine(ChapterSealRouteProductionTests.Run());'+('System.Console.WriteLine(ChapterRoomGeometryTests.Run());System.Console.WriteLine(ChapterFormationGeometryTests.Run());' if mode=='current' else ''))
  project=d/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>');(d/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>');build=subprocess.run([args.dotnet,'build',str(project),'--configfile',str(d/'NuGet.Config'),'-v:q'],capture_output=True,text=True)
  if build.returncode:print(build.stdout+build.stderr);build.check_returncode()
  run=subprocess.run([args.dotnet,str(d/'bin/Debug/net8.0/Test.dll')],capture_output=True,text=True)
  if expected:
   assert run.returncode and 'System.Exception: '+expected in run.stdout+run.stderr,run.stdout+run.stderr
   print('PASS compiled exact negative:',mode)
  else:print(run.stdout+run.stderr);run.check_returncode()

# Source wiring, not a claim about pixels or mobile legibility.
host=(ROOT/'Assets/Scripts/Core/GameSession.ChapterSeals.cs').read_text()
assert 'TryGetChapterSeal(index' in host and 'chapterPlan.Objectives[index]' in host and 'ChapterRun.SealProgress(index)' in host
for f in ['UI/GameUI.cs','UI/GameUI.Modes.cs']:
 assert 'DrawChapterSeals(' in (ROOT/('Assets/Scripts/'+f)).read_text()
visual=(ROOT/'Assets/Scripts/Combat/TacticalCaptureVisual.cs').read_text()
assert '"Seal A":"Seal B"' in visual and 'if(identity!=null)identity.enabled=false' in visual
print('PASS: A/B HUD and physical-ring identity wiring (source-only, not rendered UI)')
