#!/usr/bin/env python3
"""Frozen incremental validation for the reviewed class RNG fix; no Unity execution."""
from pathlib import Path
import argparse,hashlib,importlib.util,json,os,subprocess,tempfile
root=Path(__file__).resolve().parents[3]
a=argparse.ArgumentParser();a.add_argument('--dotnet',required=True);a.add_argument('--platform',choices=['windows','ios'],required=True);a.add_argument('--baseline',type=Path);a.add_argument('--output',type=Path);a.add_argument('--reward',action='store_true');args=a.parse_args()
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
baseline=args.baseline or Path(__file__).resolve().parent/'Evidence'/args.platform/'report.json';output=args.output or Path(__file__).resolve().parent/'Evidence/review-fix'/args.platform;output.mkdir(parents=True,exist_ok=True)
before=cv.source_hashes();prior=json.loads(baseline.read_text());assert prior['passed'] and not prior['sourceChangedDuringRun']
changes={p:{'fullBaseline':prior['sourceSha256'].get(p),'reviewFix':before.get(p)} for p in prior['sourceSha256'].keys()|before.keys() if prior['sourceSha256'].get(p)!=before.get(p)}
assert set(changes)=={'Assets/Scripts/Core/GameSession.ClassSwitch.cs','Tests/ClassSwitchRuntimeBoundary.cs','Tests/ClassSwitchRuntimeTests.py'},changes
report={'passed':False,'scope':'Incremental production regressions and pinned Unity API compilation; not Unity execution','baselineSha256':hashlib.sha256(baseline.read_bytes()).hexdigest(),'runnerSha256':hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),'sourceSha256':before,'inputComparison':changes,'checks':[]}
with tempfile.TemporaryDirectory(prefix='class-review-fix-') as directory:
 p=Path(directory);(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>');env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1')
 cases=[('class-service','ClassSwitchProductionTests.py'),('class-runtime','ClassSwitchRuntimeTests.py'),('class-ui','ClassSwitchUIProductionTests.py')]
 if args.reward:cases += [('single-chest-service','SingleChestProductionTests.py'),('single-chest-ui','SingleChestUIProductionTests.py')]
 for name,test in cases:cv.run_check(name,[[os.sys.executable,str(root/'Tests'/test),args.dotnet]],env,output,report)
 refs=list(cv.unity_references(False).glob('*.dll'))
 for name,defines in [('windows-compile','UNITY_STANDALONE;UNITY_STANDALONE_WIN'),('ios-compile','UNITY_IOS')]:
  project=cv.write_project(p/name,sorted((root/'Assets/Scripts').rglob('*.cs')),references=refs,framework='netstandard2.1',defines=defines)
  cv.run_check(name,[[args.dotnet,'build',str(project),'--configuration','Release','--verbosity','minimal']],env,output,report)
final=cv.source_hashes();report['sourceChangedDuringRun']=sorted(p for p in before.keys()|final.keys() if before.get(p)!=final.get(p));report['passed']=all(c['passed'] for c in report['checks']) and not report['sourceChangedDuringRun']
(output/'report.json').write_text(json.dumps(report,indent=2)+'\n');print('PASS' if report['passed'] else 'FAIL','frozen review-fix checks:',len(report['checks']));raise SystemExit(0 if report['passed'] else 1)
