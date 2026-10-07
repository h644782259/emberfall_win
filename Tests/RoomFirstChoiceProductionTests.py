#!/usr/bin/env python3
"""Real first-room B-first -> deferred choice -> saved transition; managed scene/save doubles."""
from pathlib import Path
import os, subprocess, sys, tempfile
root=Path(__file__).resolve().parents[1]
dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet')
def source(path):
 return (root/path).read_text()
def replace_required(text,before,after):
 assert text.count(before)==1, 'fixture/production seam changed: '+before
 return text.replace(before,after)
def member(s,sig):
 a=s.index(sig);b=s.index('{',a)+1;d=1
 while d:d+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
room=source('Assets/Scripts/Core/GameSession.RoomChain.cs');session=source('Assets/Scripts/Core/GameSession.cs')
methods='\n'.join(member(room,s) for s in ['public bool NearRoomExit','private void BeginRoomChainScene()','private void RecordRoomDefeat(','public bool EnterNextRoom()','private bool EnterNextRoomAfterSave()','private bool ConfirmRoomInterlude('])+'\n'+'\n'.join(member(session,s) for s in ['public bool SaveBeforeLeaving()','public bool InputBlocked','private void UpdateTimeScale()'])
methods+='\n'+member(source('Assets/Scripts/Core/GameSession.RoomBranch.cs'),'public bool RoomBranchChoiceOpen')
# Reuse the room host's explicit scene/enemy/persistence doubles, but replace its
# choice and pause shortcuts with the real production implementations.
fixture=source('Tests/RoomFreeSealHostTests.cs').split('public static class RoomFreeSealHostTests')[0]
fixture=replace_required(fixture,' public enum EnemyKind{Wisp,Guardian,Goblin,Slime}\n','')
fixture=replace_required(fixture,'public Color(float r,float g,float b)','public Color(float r,float g,float b,float a=1)')
start=fixture.index(' public class ChoiceDouble');end=fixture.index(' public static class MobileControls',start);fixture=fixture[:start]+fixture[end:]
fixture=replace_required(fixture,'public object Profile=new object();','public GameProfile Profile=new GameProfile{heroClass=HeroClass.Vanguard,skillRanks=new[]{1,1,1,0,1,1,1,1,0,1},equippedSkills=new[]{0,1,2,4,5}};')
fixture=replace_required(fixture,'public ChoiceDouble RunChoices=new ChoiceDouble();','public RunChoices RunChoices=new RunChoices();')
fixture=replace_required(fixture,'public bool InputBlocked=>Paused||BackgroundPaused||IsDead;','private ApplicationPauseState pauseState=new ApplicationPauseState();private bool uiBlocking,DungeonSelectionOpen;private bool ModeFinished=>RoomChainRun!=null&&RoomChainRun.Finished;')
fixture=replace_required(fixture,'void UpdateTimeScale(){}','')
math=source('Tests/DestructibleTraversalTests.cs');math='using System;using UnityEngine;'+math[math.index('namespace Emberfall'):]
math=replace_required(math,'    public enum ZoneKind{Wilderness,Dungeon}\n','');math=replace_required(math,'public static float time=0;','public static float time=0,deltaTime=.25f,timeScale=1;public static int frameCount;')
fixture=fixture.replace('public bool PracticeActive=>false;', 'public bool PracticeActive=>false;public CampPracticeRecord PracticeRecord=>throw new System.InvalidOperationException("ordinary room fixture cannot access practice record");')
files=['Core/CombatImpactBatch','Core/CampPracticeRecord','Core/RoomChainState','Core/RoomTactics','Core/RoomTacticalRegion','Core/DeferredRoomChoice','Core/EscapePostPolicy','Core/GameSession.RoomDiagnostics','Core/GameSession.RoomTactics','World/WorldTraversal','World/TacticalRoomGeometry','World/EscapeRoomFormation','World/RoomBranchGeometry','UI/ChapterSealPresentation','UI/RoomObjectivePresentation','Core/GameTypes','Core/CombatBalance','Core/RunChoices','Core/RunChoices.Rooms','Core/ApplicationPauseState']
# All inputs come from this checkout; no pinned Git revision or external probe files.
originals={Path(f).name+'.cs':source('Assets/Scripts/'+f+'.cs') for f in files}
probe=source('Tests/RoomFirstChoiceProductionTests.cs')
with tempfile.TemporaryDirectory(prefix='room-first-choice-') as temporary:
 base=Path(temporary)
 env=dict(os.environ,DOTNET_CLI_HOME=str(base/'cli'),DOTNET_NOLOGO='1')
 for mode,expected in [('current',None),('remove-pending-block','pending blessing blocks actual EnterNextRoom before save or transition'),('remove-deferred-claim','next frame claims deferred blessing through real offer generation')]:
  p=base/mode;p.mkdir(exist_ok=True)
  for name,s in originals.items():
   if mode=='remove-deferred-claim' and name=='GameSession.RoomTactics.cs':s=replace_required(s,'if(TryOpenPendingRoomChoice())return;','')
   (p/name).write_text(s)
  body=replace_required(methods,'||pendingRoomChoice.Pending','') if mode=='remove-pending-block' else methods
  (p/'Methods.cs').write_text('using UnityEngine;using System.Collections.Generic;namespace Emberfall{public sealed partial class GameSession{'+body+'}}')
  (p/'Fixture.cs').write_text(fixture);(p/'Math.cs').write_text(math);(p/'Probe.cs').write_text(probe)
  project=p/'Probe.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NuGetAudit>false</NuGetAudit><NoWarn>0649;0414</NoWarn></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
  build=subprocess.run([dotnet,'build',str(project),'--configfile',str(p/'NuGet.Config'),'-v:q'],env=env,capture_output=True,text=True);(p/'build.log').write_text(build.stdout+build.stderr)
  if build.returncode:print(build.stdout+build.stderr);build.check_returncode()
  run=subprocess.run([dotnet,str(p/'bin/Debug/net8.0/Probe.dll')],env=env,capture_output=True,text=True);(p/'run.log').write_text(run.stdout+run.stderr)
  if expected:assert run.returncode and 'System.Exception: '+expected in run.stdout+run.stderr,run.stdout+run.stderr;print('PASS compiled negative:',mode)
  else:print(run.stdout+run.stderr);run.check_returncode()
