"""Compile the old primitive/contact failure against a collider-less engine boundary."""
from pathlib import Path
import os,sys,subprocess,tempfile
root=Path(__file__).resolve().parents[1];sdk=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
source=(root/'Assets/Scripts/UI/CollectionModelPreview.cs').read_text()
assert 'GameObject.CreatePrimitive(' not in source and 'AddComponent<Collider>' not in source
for f in list((root/'Assets/Scripts/Combat').glob('CombatModel*.cs'))+[root/'Assets/Scripts/Combat/ProceduralVisuals.cs']:
 assert 'GameObject.CreatePrimitive(' not in f.read_text(),str(f)
for name,instance in [('GameUI.WearMap.cs','wearModel'),('GameUI.CollectionPreview.cs','collectionModel'),('GameUI.RewardMoments.cs','rewardMomentModel')]:
 text=(root/'Assets/Scripts/UI'/name).read_text();assert instance+'.RenderSafe(' in text and instance+'.Render(' not in text
print('PASS collider-free preview/model source paths and non-throwing inventory/fashion entry points')
a=source.index('            // A visual-only quad:');b=source.index('            var ring=new GameObject',a)
old='''            var shadow=GameObject.CreatePrimitive(PrimitiveType.Quad);shadow.name="Soft preview contact";shadow.layer=PreviewLayer;shadow.transform.SetParent(stage.transform,false);
            shadow.GetComponent<Collider>().enabled=false;shadow.GetComponent<Renderer>().sharedMaterial=shadowMaterial;
'''
with tempfile.TemporaryDirectory(prefix='preview-collider-negative-') as tmp:
 p=Path(tmp)
 for name in ['CollectionModelPreview','CollectionPreviewState','CollectionPreviewComposition']:(p/(name+'.cs')).write_text(source[:a]+old+source[b:] if name=='CollectionModelPreview' else (root/'Assets/Scripts/UI'/(name+'.cs')).read_text())
 for src in ['Assets/Scripts/Core/RendererGroupCache.cs','Tests/CollectionRenderLifecycleTests.cs']:(p/Path(src).name).write_text((root/src).read_text())
 (p/'Program.cs').write_text('System.Console.WriteLine(CollectionRenderLifecycleTests.Run());');(p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1')
 subprocess.run([sdk,'build',str(p/'Test.csproj'),'--configfile',str(p/'NuGet.Config'),'-v:q'],check=True,env=env)
 r=subprocess.run([sdk,str(p/'bin/Debug/net8.0/Test.dll')],capture_output=True,text=True,env=env)
 assert r.returncode and 'NullReferenceException' in r.stdout+r.stderr and 'CollectionModelPreview.CreateContact' in r.stdout+r.stderr,r.stdout+r.stderr
 print('PASS compiled old contact code reproduces CreateContact NullReferenceException when primitive collider is absent')
