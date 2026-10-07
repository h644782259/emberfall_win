#!/usr/bin/env python3
"""Actual room host, state and navigation with explicit managed engine/save boundaries."""
import os,sys,tempfile,subprocess
from pathlib import Path
root=Path(__file__).resolve().parents[1];dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet')
def member(path,signature):
 source=(root/path).read_text();a=source.index(signature);b=source.index('{',a)+1;depth=1
 while depth:depth+=(source[b]=='{')-(source[b]=='}');b+=1
 return source[a:b]
methods='\n'.join(member('Assets/Scripts/Core/GameSession.RoomChain.cs',s) for s in ['public bool NearRoomExit','private void BeginRoomChainScene()','private void RecordRoomDefeat(','public bool EnterNextRoom()','private bool EnterNextRoomAfterSave()'])+'\n'+member('Assets/Scripts/Core/GameSession.cs','public bool SaveBeforeLeaving()')
with tempfile.TemporaryDirectory(prefix='room-free-seals-') as temporary:
 p=Path(temporary)
 files=['Core/RoomChainState','Core/RoomTactics','Core/RoomTacticalRegion','Core/DeferredRoomChoice','Core/EscapePostPolicy','Core/GameSession.RoomDiagnostics','Core/GameSession.RoomTactics','World/WorldTraversal','World/TacticalRoomGeometry','World/EscapeRoomFormation','World/RoomBranchGeometry','UI/ChapterSealPresentation','UI/RoomObjectivePresentation']
 for f in files:(p/(Path(f).name+'.cs')).write_text((root/('Assets/Scripts/'+f+'.cs')).read_text())
 math=(root/'Tests/DestructibleTraversalTests.cs').read_text();math='using System;using UnityEngine;'+math[math.index('namespace Emberfall'):];math=math.replace('public static float time=0;','public static float time=0,deltaTime=.25f;public static int frameCount;')
 (p/'Math.cs').write_text(math)
 (p/'Methods.cs').write_text('using UnityEngine;using System.Collections.Generic;namespace Emberfall {public sealed partial class GameSession {'+methods+'}}')
 for f in ['RoomFreeSealHostTests','RoomChainStateTests']:(p/(f+'.cs')).write_text((root/('Tests/'+f+'.cs')).read_text())
 (p/'Program.cs').write_text('System.Console.WriteLine(RoomFreeSealHostTests.Run());System.Console.WriteLine(RoomChainStateTests.Run());')
 project=p/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NoWarn>0649;0414</NoWarn><NuGetAudit>false</NuGetAudit></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 subprocess.run([dotnet,'restore',str(project),'--configfile',str(p/'NuGet.Config'),'-v:q'],check=True)
 command=[dotnet,'run','--project',str(project),'--no-restore'];subprocess.run(command,check=True)
 state=p/'RoomChainState.cs';original=state.read_text();changed=original.replace('if(Room==null||Room.Objective!=RoomObjective.Purify||index<0','if(index!=Seals||Room==null||Room.Objective!=RoomObjective.Purify||index<0');assert changed!=original;state.write_text(changed)
 subprocess.run([dotnet,'build',str(project),'--no-restore','-v:q'],check=True,stdout=subprocess.DEVNULL)
 result=subprocess.run(command+['--no-build'],capture_output=True,text=True);assert result.returncode and 'System.Exception: live host permits B-first and A-first independently' in result.stdout+result.stderr,result.stdout+result.stderr
 print('PASS: compiled forced-order production mutation fails actual B-first host assertion')
