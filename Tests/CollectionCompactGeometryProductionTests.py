#!/usr/bin/env python3
"""Actual production preview UI rectangles and compact geometry with overlay/framing negatives."""
import os,sys,tempfile,subprocess
from pathlib import Path
root=Path(__file__).resolve().parents[1];dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet')
def method(s,signature):
 a=s.index(signature);b=s.index('{',a)+1;n=1
 while n:n+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
source=(root/'Assets/Scripts/UI/GameUI.CollectionPreview.cs').read_text()
# The inventory now uses the persistent wear model and owned-only icon grid.
wear=(root/'Assets/Scripts/UI/GameUI.WearMap.cs').read_text()
assert 'DrawBagFashion(' in wear and 'Profile.fashions' in wear and 'collectionTrial' not in wear
body='using UnityEngine;namespace Emberfall{public sealed partial class GameUI{'+''.join(method(source,s) for s in ['private void DrawCollectionModel(','private static Rect PreviewControlRect(','private void DrawCollectionControls('])+'}}'
with tempfile.TemporaryDirectory(prefix='compact-preview-') as directory:
 p=Path(directory)
 for name in ['CollectionPreviewLayout','MobilePanelLayout','CollectionViewingState','CollectionPreviewComposition']:(p/(name+'.cs')).write_text((root/'Assets/Scripts/UI'/(name+'.cs')).read_text())
 (p/'Fixture.cs').write_text((root/'Tests/CollectionCompactGeometryProductionTests.cs').read_text());production=p/'UI.cs';production.write_text(body);(p/'Program.cs').write_text('System.Console.WriteLine(CollectionCompactGeometryProductionTests.Run());')
 project=p/'Tests.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NoWarn>0649</NoWarn></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>');env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1');cmd=[dotnet,'run','--project',str(project),'--no-restore']
 subprocess.run([dotnet,'restore',str(project),'--configfile',str(p/'NuGet.Config'),'-v:q'],env=env,check=True);subprocess.run(cmd,env=env,check=True)
 for before,after,expected in [('if(controls!=null)DrawCollectionControls(controls,Vector2.zero,1);','if(rotate&&MobileControls.Active)DrawCollectionControls(CollectionPreviewLayout.Desktop(new MobilePanelLayout.Area(area.x,area.y,area.width,area.height)),Vector2.zero,1);if(controls!=null)DrawCollectionControls(controls,Vector2.zero,1);','compact image has no overlay control draws'),('SetViewport(viewport.width*Mathf.Abs(GUI.matrix.m00),viewport.height*Mathf.Abs(GUI.matrix.m11),','SetViewport(area.width*Mathf.Abs(GUI.matrix.m00),area.height*Mathf.Abs(GUI.matrix.m11),','native viewport matches actual unobscured image area')]:
  assert body.count(before)==1;production.write_text(body.replace(before,after));subprocess.run([dotnet,'build',str(project),'--no-restore','-v:q'],env=env,check=True,stdout=subprocess.DEVNULL);result=subprocess.run(cmd+['--no-build'],env=env,capture_output=True,text=True);assert result.returncode and 'System.Exception: '+expected in result.stdout+result.stderr,result.stdout+result.stderr
 print('PASS: overlay control and wrong native framing compiled mutations fail exact production UI assertions')
