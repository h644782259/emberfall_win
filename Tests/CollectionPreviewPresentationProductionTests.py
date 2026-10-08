#!/usr/bin/env python3
"""Actual preview host lifecycle and clock; native/render/model stand-ins explicitly scoped."""
import os,sys,subprocess,tempfile
from pathlib import Path
root=Path(__file__).resolve().parents[1];dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet')
with tempfile.TemporaryDirectory(prefix='preview-presentation-') as directory:
 p=Path(directory)
 (p/'RendererGroupCache.cs').write_text((root/'Assets/Scripts/Core/RendererGroupCache.cs').read_text())
 for name in ['CollectionModelPreview','CollectionPreviewState','CollectionPreviewComposition']:(p/(name+'.cs')).write_text((root/'Assets/Scripts/UI'/(name+'.cs')).read_text())
 for name in ['CollectionRenderLifecycleTests','CollectionPreviewCompositionTests']:(p/(name+'.cs')).write_text((root/'Tests'/(name+'.cs')).read_text())
 (p/'Program.cs').write_text('System.Console.WriteLine(CollectionRenderLifecycleTests.Run());System.Console.WriteLine(CollectionPreviewCompositionTests.Run());');project=p/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1');cmd=[dotnet,'run','--project',str(project),'--no-restore'];subprocess.run([dotnet,'restore',str(project),'--configfile',str(p/'NuGet.Config'),'-v:q'],env=env,check=True);subprocess.run(cmd,env=env,check=True)
 source=p/'CollectionModelPreview.cs';original=source.read_text()
 for before,after,expected in [('if(centerOnAvatar)','if(false)','centered imported weapon preserves asymmetric envelope without clipping'),('||t.name=="Vanguard_Sword"','', 'weapon composition frames actual imported sword renderer bounds'),('||t.name=="Vanguard_Back"','', 'back composition includes actual imported back renderer bounds'),('model.SamplePreview(motion.Time,motion.Action,motion.Progress,motion.OrbitYaw);','model.SamplePreview(0,CollectionPreviewAction.Idle,1);','actual preview host samples selected action on its local clock'),('if(framingDirty){FrameModel();framingDirty=false;}','if(true){FrameModel();framingDirty=false;}','local pose motion reuses model texture and cached framing'),('camera.fieldOfView=48f;','camera.fieldOfView=60f;','combat viewing uses real default perspective FOV'),('if(!equipmentFraming||equipmentHighlightSlot<0)return;','return;','only selected subtree tinted during render and exact properties restored'),('model.SamplePreview(motion.Time,motion.Action,motion.Progress,motion.OrbitYaw);','if(equipmentFraming)model.SamplePreview(0,CollectionPreviewAction.Idle,1);else model.SamplePreview(motion.Time,motion.Action,motion.Progress,motion.OrbitYaw);','equipment move sample preserves fixed camera')]:
  assert original.count(before)==1;source.write_text(original.replace(before,after));subprocess.run([dotnet,'build',str(project),'--no-restore','-v:q'],env=env,check=True,stdout=subprocess.DEVNULL)
  result=subprocess.run(cmd+['--no-build'],env=env,capture_output=True,text=True);print('NEGATIVE CONTROL:',expected,flush=True);print(result.stdout+result.stderr,flush=True);assert result.returncode and 'System.Exception: '+expected in result.stdout+result.stderr,result.stdout+result.stderr
 print('PASS: eight compiled imported-group/presentation-action/cached-envelope/equipment-view negative controls fail exact host assertions')

 # Missing hierarchy notifications must invalidate the actual host/cache regression.
 source.write_text(original)
 cache=p/'RendererGroupCache.cs';cache.write_text(cache.read_text().replace('foreach(var owner in owners.ToArray())owner.Invalidate();',''))
 subprocess.run([dotnet,'build',str(project),'--no-restore','-v:q'],env=env,check=True,stdout=subprocess.DEVNULL)
 result=subprocess.run(cmd+['--no-build'],env=env,capture_output=True,text=True)
 assert result.returncode and 'hierarchy addition invalidates actual preview membership and texture' in result.stdout+result.stderr,result.stdout+result.stderr
 print('PASS: missing hierarchy cache notification rejected by actual preview host')
