#!/usr/bin/env python3
"""Execute actual IMGUI draw methods with rectangle/text recorders; no Unity/font rendering.
--runtime-root supports the independently owned room-state worktree before integration.
"""
import argparse,os,subprocess,tempfile
from pathlib import Path
root=Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser();p.add_argument('dotnet',nargs='?',default=os.environ.get('DOTNET','dotnet'));p.add_argument('--runtime-root',type=Path,default=root);args=p.parse_args()
def member(s,k):
 a=s.index(k);b=s.index('{',a)+1;n=1
 while n:n+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
modesSource=(root/'Assets/Scripts/UI/GameUI.Modes.cs').read_text()
modes=member(modesSource,'private void DrawMobileModeStatus(')+member(modesSource,'private void DrawMobileSealText(')+member((root/'Assets/Scripts/UI/GameUI.Mobile.cs').read_text(),'private void DrawMobileObjectiveText(')
desktop=(root/'Assets/Scripts/UI/GameUI.cs').read_text();a=desktop.index('            string objectiveText =');b=desktop.index('            DrawMinimap();',a);desktop=desktop[a:b]
with tempfile.TemporaryDirectory(prefix='room-seal-hud-') as temporary:
 t=Path(temporary)
 for name in ['Core/RoomTactics','Core/RoomTacticalRegion','UI/RoomObjectivePresentation','UI/ChapterSealPresentation','UI/ObjectiveCardLayout','UI/MobileControlLayout']:(t/(Path(name).name+'.cs')).write_text((root/('Assets/Scripts/'+name+'.cs')).read_text())
 (t/'RoomChainState.cs').write_text((args.runtime_root/'Assets/Scripts/Core/RoomChainState.cs').read_text())
 (t/'Rows.cs').write_text((root/'Assets/Scripts/UI/GameUI.ChapterSeals.cs').read_text())
 (t/'Draw.cs').write_text('using UnityEngine;namespace Emberfall{public partial class GameUI{'+modes+'public void Desktop(){var p=session.Profile;'+desktop+'}}}')
 (t/'Fixture.cs').write_text((root/'Tests/RoomSealHudProductionTests.cs').read_text());(t/'PresentationTests.cs').write_text((root/'Tests/RoomObjectivePresentationTests.cs').read_text())
 (t/'Program.cs').write_text('Emberfall.GameUI.Run();System.Console.WriteLine(RoomObjectivePresentationTests.Run());')
 project=t/'Tests.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NoWarn>0649;0414</NoWarn></PropertyGroup></Project>');(t/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(t/'cli'),DOTNET_NOLOGO='1')
 def build():
  q=subprocess.run([args.dotnet,'build',str(project),'--configfile',str(t/'NuGet.Config'),'-v:q'],env=env,capture_output=True,text=True);assert q.returncode==0,q.stdout+q.stderr
 build();q=subprocess.run([args.dotnet,str(t/'bin/Debug/net8.0/Tests.dll')],env=env,capture_output=True,text=True);print(q.stdout);assert q.returncode==0,q.stderr
 source=(t/'Draw.cs').read_text();changed=source.replace('y+=h+2*TouchRatio;','y+=h+20*TouchRatio;');assert changed!=source;(t/'Draw.cs').write_text(changed);build();q=subprocess.run([args.dotnet,str(t/'bin/Debug/net8.0/Tests.dll')],env=env,capture_output=True,text=True);assert q.returncode and 'compact actual draw stays inside existing mode card' in q.stderr,q.stdout+q.stderr
 print('PASS: compiled expanded text spacing rejected by actual mobile draw rectangles')
