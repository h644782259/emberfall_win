from pathlib import Path
import hashlib,json,subprocess,sys,importlib.util,os,tempfile,base64,zipfile
from datetime import datetime,timezone
base=Path(__file__).resolve().parent;root=base.parents[2]
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def inputs():
 paths=[]
 for folder in ['Assets','Packages','ProjectSettings','Tests']:
  paths.extend(p for p in (root/folder).rglob('*') if p.is_file() and 'TestResults' not in p.parts)
 paths.extend(p for p in (root/'Tools').glob('*') if p.is_file());paths.extend(base/p for p in ['run.py','verify.py','README.md','applicability.json'])
 return {str(p.relative_to(root)):sha(p) for p in sorted(paths)}
initial=inputs();report={'startedUtc':datetime.now(timezone.utc).isoformat(),'baseline':json.loads((base/'applicability.json').read_text())['baseline'],'sourceSha256':initial,'checks':[],'scope':'Managed production regression and pinned UnityEngine 2021.3.33 API compilation only; no Unity 6000.6.3f1 engine, native serializer, full Editor build, IL2CPP/Gradle or device execution'}
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
archive=root/'Tools/ReferenceAssemblies/unityengine.modules.2021.3.33.nupkg';assert base64.b64encode(hashlib.sha512(archive.read_bytes()).digest()).decode()==cv.PACKAGE_SHA512
with zipfile.ZipFile(archive) as z:
 for p in cv.unity_references(False).glob('*.dll'):assert p.read_bytes()==z.read('lib/netstandard2.0/'+p.name)
with tempfile.TemporaryDirectory(prefix='shared-fixes-check-') as d:
 p=Path(d);(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>');env=dict(os.environ,DOTNET=sys.argv[1],DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
 for test in ['SharedChestNormalizationTests','SharedNativeBoundaryTests','FilledVfxPoolProductionTests','BlenderSkillVfxProductionTests','BlenderSceneryProductionTests','WorldLabelProductionTests','SingleChestProductionTests','ChestSnapshotRetryTests','ChestCompositeProductionTests','ChestTrialProductionTests','SingleChestUIProductionTests','MobileInventoryBackProductionTests','EditorAssemblyBoundaryTests']:
  result=subprocess.run([sys.executable,str(root/'Tests'/(test+'.py')),sys.argv[1]],cwd=root,env=env,text=True,stdout=subprocess.PIPE,stderr=subprocess.STDOUT);(base/(test+'.log')).write_text(result.stdout);report['checks'].append({'name':test,'exitCode':result.returncode,'passed':result.returncode==0});print(test,result.returncode,flush=True)
 for platform in ['UNITY_STANDALONE_WIN','UNITY_ANDROID','UNITY_IOS']:
  proj=cv.write_project(p/platform,sorted((root/'Assets/Scripts').rglob('*.cs')),references=list(cv.unity_references(False).glob('*.dll')),defines=platform)
  result=subprocess.run([sys.argv[1],'build',str(proj),'-c','Release','-v','minimal'],env=env,text=True,stdout=subprocess.PIPE,stderr=subprocess.STDOUT);(base/(platform+'.log')).write_text(result.stdout);report['checks'].append({'name':platform,'exitCode':result.returncode,'passed':result.returncode==0});print(platform,result.returncode,flush=True)
final=inputs();report['sourceChangedDuringRun']=[n for n in initial.keys()|final.keys() if initial.get(n)!=final.get(n)];report['passed']=all(c['passed'] for c in report['checks']) and not report['sourceChangedDuringRun'];report['completedUtc']=datetime.now(timezone.utc).isoformat();(base/'report.json').write_text(json.dumps(report,indent=2)+'\n');sys.exit(0 if report['passed'] else 1)
