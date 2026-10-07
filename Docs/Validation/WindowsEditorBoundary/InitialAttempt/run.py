from pathlib import Path
import hashlib,json,subprocess,sys,importlib.util,base64,zipfile
from datetime import datetime,timezone
base=Path(__file__).resolve().parent;root=base.parents[2];baseline='e1397a26049e624d98daa0b20159526e045f6076'
def digest(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def inputs():
 paths=[]
 for folder in ['Assets','Packages','ProjectSettings','Tests']:
  paths.extend(p for p in (root/folder).rglob('*') if p.is_file())
 paths.extend(p for p in (root/'Tools').glob('*') if p.is_file())
 paths.extend([base/'run.py',base/'verify.py',base/'README.md'])
 return {str(p.relative_to(root)):digest(p) for p in sorted(paths)}
initial=inputs();report={'baseline':baseline,'startedUtc':datetime.now(timezone.utc).isoformat(),'sourceSha256':initial,'checks':[],'scope':'Windows managed compilation and regression checks; pinned UnityEngine 2021.3.33, small UnityEditor substitutes; not Unity 6000.6.3f1 import/build or device validation'}
# No runtime/package/font edits, and the only modified pre-existing tracked file is the validator.
changed=subprocess.check_output(['git','diff',baseline,'--name-only'],cwd=root,text=True).splitlines();assert changed==['Assets/Editor/GroundLootValidation.cs'],changed
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
arc=root/'Tools/ReferenceAssemblies/unityengine.modules.2021.3.33.nupkg';assert base64.b64encode(hashlib.sha512(arc.read_bytes()).digest()).decode()==cv.PACKAGE_SHA512
with zipfile.ZipFile(arc) as z:
 for p in cv.unity_references(False).glob('*.dll'):assert p.read_bytes()==z.read('lib/netstandard2.0/'+p.name)
for name in ['EditorAssemblyBoundaryTests','GroundLootCallbackRecoveryTests','WorldLootReceiptSourceTests','RoomFailureEvidenceTests']:
 result=subprocess.run([sys.executable,str(root/'Tests'/(name+'.py')),sys.argv[1]],cwd=root,text=True,stdout=subprocess.PIPE,stderr=subprocess.STDOUT)
 (base/(name+'.log')).write_text(result.stdout);report['checks'].append({'name':name,'exitCode':result.returncode,'passed':result.returncode==0});print(name,result.returncode,flush=True)
final=inputs();report['sourceChangedDuringRun']=[n for n in initial.keys()|final.keys() if initial.get(n)!=final.get(n)];report['passed']=all(c['passed'] for c in report['checks']) and not report['sourceChangedDuringRun'];report['completedUtc']=datetime.now(timezone.utc).isoformat();(base/'report.json').write_text(json.dumps(report,indent=2)+'\n');sys.exit(0 if report['passed'] else 1)
