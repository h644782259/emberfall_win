#!/usr/bin/env python3
"""Frozen, explicit repair after three old fixture incompatibilities; retain original aggregate."""
from pathlib import Path
import importlib.util,json,hashlib,os,sys,tempfile
from datetime import datetime,timezone
base=Path(__file__).resolve().parent;root=base.parents[2];spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
dotnet=sys.argv[1];platform=sys.argv[2];assert platform in ['windows','ios'];output=base/'Evidence'/platform/'repair';output.mkdir(parents=True,exist_ok=True)
original=base/'Evidence'/platform/'full/report.json';full=json.loads(original.read_text());initial=cv.source_hashes()
expected={'Tests/RewardViewingRulesTests.cs','Tests/ChestPauseBackProductionTests.py','Tests/PracticeHotbarNavigationProductionTests.py'}
changes={p:{'full':full['sourceSha256'].get(p),'repaired':initial.get(p)} for p in full['sourceSha256'].keys()|initial.keys() if full['sourceSha256'].get(p)!=initial.get(p)}
assert set(changes)==expected,changes
report={'startedUtc':datetime.now(timezone.utc).isoformat(),'environment':{'DOTNET_TieredCompilation':'0'},'checks':[],'scope':'Targeted frozen fixture repair: no Unity execution or native platform build. Production inputs unchanged from original complete run.','baselineSha256':hashlib.sha256(original.read_bytes()).hexdigest(),'runnerSha256':hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),'inputComparison':changes,'sourceSha256':initial}
with tempfile.TemporaryDirectory(prefix='reward-polish-repair-') as tmp:
 p=Path(tmp);config=p/'NuGet.Config';config.write_text('<configuration><packageSources><clear/></packageSources></configuration>');env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1',DOTNET_CLI_TELEMETRY_OPTOUT='1',DOTNET_TieredCompilation='0')
 names=['SkillRuntime','GameTypes','ProgressionService','CombatBalance','SkillDamageBudgets','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState','ChapterProgression','ProgressionService.Chapter','ProgressionService.Reforge','ReforgeQuote','RoomTactics','CombatImpactBatch','SafeSaveFlow']
 sources=[root/('Assets/Scripts/Core/'+n+'.cs') for n in names]+[root/'Tests/ProgressionTests.cs',root/'Tests/RewardViewingRulesTests.cs']+[root/('Assets/Scripts/UI/'+n+'.cs') for n in ['ChestRevealPresentation','ChestCompositeRules','CollectionViewingState','CollectionPreviewComposition']]
 proj=cv.write_project(p/'viewing',sources,'using System;class Program{static void Main(){Console.WriteLine(RewardViewingRulesTests.Run());}}')
 cv.run_check('reward-viewing-rules',[[dotnet,'run','--project',str(proj)]],env,output,report)
 for name,script in [('chest-pause-back-production','ChestPauseBackProductionTests.py'),('practice-hotbar-navigation','PracticeHotbarNavigationProductionTests.py'),('reward-polish-service','RewardPolishServiceTests.py'),('reward-polish-ui','RewardPolishUIProductionTests.py')]:cv.run_check(name,[[sys.executable,str(root/'Tests'/script),dotnet]],env,output,report)
 refs=list(cv.unity_references(False).glob('*.dll'))
 for name,defines in [('runtime-compile','UNITY_STANDALONE;UNITY_STANDALONE_WIN'),('ios-runtime-compile','UNITY_IOS')]:
  proj=cv.write_project(p/name,sorted((root/'Assets/Scripts').rglob('*.cs')),references=refs,defines=defines)
  cv.run_check(name,[[dotnet,'restore',str(proj),'--configfile',str(config),'--verbosity','quiet'],[dotnet,'build',str(proj),'--no-restore','--configuration','Release','--verbosity','minimal']],env,output,report)
final=cv.source_hashes();report['sourceChangedDuringRun']=[p for p in initial.keys()|final.keys() if initial.get(p)!=final.get(p)];report['passed']=not report['sourceChangedDuringRun'] and all(c['passed'] for c in report['checks'])
report['completedUtc']=datetime.now(timezone.utc).isoformat()
(output/'report.json').write_text(json.dumps(report,indent=2)+'\n');print('Frozen fixture repair:',report['passed']);sys.exit(0 if report['passed'] else 1)
