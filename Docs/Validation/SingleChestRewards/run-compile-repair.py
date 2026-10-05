#!/usr/bin/env python3
from pathlib import Path
import argparse,hashlib,importlib.util,json,os,subprocess,tempfile
root=Path(__file__).resolve().parents[3];p=argparse.ArgumentParser();p.add_argument('--dotnet',required=True);args=p.parse_args()
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
out=Path(__file__).resolve().parent/'Evidence/ios-compile-repair';out.mkdir(parents=True,exist_ok=True)
baseline=root/'Tests/TestResults/Cloud-Latest/report.json';full=json.loads(baseline.read_text());assert not full['passed'] and len(full['checks'])==283 and {c['name'] for c in full['checks'] if not c['passed']}=={'reference-compile-setup'} and not full['sourceChangedDuringRun']
before=cv.source_hashes();assert before==full['sourceSha256'];report={'scope':'Only compile setup repair; original complete aggregate remains failed and is retained separately','originalAggregateSha256':hashlib.sha256(baseline.read_bytes()).hexdigest(),'sourceSha256':before,'checks':[]}
with tempfile.TemporaryDirectory(prefix='reward-compile-repair-') as directory:
 p=Path(directory);(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>');env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1');refs=list(cv.unity_references(False).glob('*.dll'))
 for name,defines in [('windows-compile','UNITY_STANDALONE;UNITY_STANDALONE_WIN'),('ios-compile','UNITY_IOS')]:
  project=cv.write_project(p/name,sorted((root/'Assets/Scripts').rglob('*.cs')),references=refs,framework='netstandard2.1',defines=defines)
  cv.run_check(name,[[args.dotnet,'build',str(project),'--configuration','Release','--verbosity','minimal']],env,out,report)
final=cv.source_hashes();report['sourceChangedDuringRun']=sorted(p for p in before.keys()|final.keys() if before.get(p)!=final.get(p));report['passed']=all(c['passed'] for c in report['checks']) and not report['sourceChangedDuringRun'];(out/'report.json').write_text(json.dumps(report,indent=2)+'\n');print('PASS' if report['passed'] else 'FAIL','compile repair only');raise SystemExit(0 if report['passed'] else 1)
