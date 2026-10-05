#!/usr/bin/env python3
from pathlib import Path
import hashlib,json,importlib.util
base=Path(__file__).resolve().parent;root=base.parents[2];e=base/'Evidence'
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
manifest=json.loads((e/'manifest.json').read_text())
for path,digest in manifest.items():assert sha(base/path)==digest,path
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
current=cv.source_hashes();matched=[];fixed={'reward-viewing-rules','chest-pause-back-production','practice-hotbar-navigation'}
for platform in ['windows','ios']:
 folder=e/platform/'full';full=json.loads((folder/'report.json').read_text());freeze=json.loads((folder/'freeze.json').read_text())
 assert (folder/'exit-code.txt').read_text().strip()=='1',platform
 assert not full['passed'] and len(full['checks'])==286 and {c['name'] for c in full['checks'] if not c['passed']}==fixed,platform
 assert not full['sourceChangedDuringRun'] and full['sourceSha256']==freeze['sourceSha256'],platform
 assert all((folder/c['log']).is_file() for c in full['checks']),platform
 repairFolder=e/platform/'repair';repair=json.loads((repairFolder/'report.json').read_text())
 assert (repairFolder/'exit-code.txt').read_text().strip()=='0' and repair['passed'] and len(repair['checks'])==7 and all(c['passed'] for c in repair['checks']),platform
 assert not repair['sourceChangedDuringRun'] and all((repairFolder/c['log']).is_file() for c in repair['checks']),platform
 assert repair['baselineSha256']==sha(folder/'report.json') and repair['runnerSha256']==sha(base/'run-fixture-repair.py'),platform
 diff={p:{'full':full['sourceSha256'].get(p),'repaired':repair['sourceSha256'].get(p)} for p in full['sourceSha256'].keys()|repair['sourceSha256'].keys() if full['sourceSha256'].get(p)!=repair['sourceSha256'].get(p)}
 assert diff==repair['inputComparison'] and set(diff)=={'Tests/RewardViewingRulesTests.cs','Tests/ChestPauseBackProductionTests.py','Tests/PracticeHotbarNavigationProductionTests.py'},platform
 assert fixed|{'reward-polish-service','reward-polish-ui','runtime-compile','ios-runtime-compile'}==set(c['name'] for c in repair['checks']),platform
 matched.append(current==repair['sourceSha256'])
assert any(matched),'current inputs do not match either final frozen repair run'
print('PASS: '+str(len(manifest))+' evidence artifacts; both original complete 286-check reports retain three old fixture failures; only three test inputs changed, both frozen 7-check repairs pass, production inputs unchanged and current source verified')
