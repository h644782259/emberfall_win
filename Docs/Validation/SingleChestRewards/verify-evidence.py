#!/usr/bin/env python3
from pathlib import Path
import hashlib,json
base=Path(__file__).resolve().parent;root=base.parents[2];e=base/'Evidence'
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
for path,digest in json.loads((e/'manifest.json').read_text()).items():assert sha(base/path)==digest,path
matched=[]
for platform in ['windows','ios']:
 full_path=e/platform/'report.json';full=json.loads(full_path.read_text());assert not full['sourceChangedDuringRun']
 if platform=='windows':
  assert full['passed'] and len(full['checks'])==284 and all(c['passed'] for c in full['checks']);baseline_path=full_path
 else:
  assert not full['passed'] and len(full['checks'])==283
  assert {c['name'] for c in full['checks'] if not c['passed']}=={'reference-compile-setup'}
  baseline_path=e/'ios-compile-repair/report.json';repair=json.loads(baseline_path.read_text())
  assert repair['passed'] and len(repair['checks'])==2 and not repair['sourceChangedDuringRun']
  assert repair['sourceSha256']==full['sourceSha256'] and repair['originalAggregateSha256']==sha(full_path)
  assert all(c['passed'] and (e/'ios-compile-repair'/c['log']).is_file() for c in repair['checks'])
 for c in full['checks']:
  if c['passed']:assert (e/platform/c['log']).is_file()
 revised=json.loads((e/'review-fix'/platform/'report.json').read_text())
 assert revised['passed'] and len(revised['checks'])==7 and not revised['sourceChangedDuringRun']
 assert all(c['passed'] and (e/'review-fix'/platform/c['log']).is_file() for c in revised['checks'])
 assert revised['baselineSha256']==sha(baseline_path)
 assert revised['runnerSha256']==sha(root/'Docs/Validation/CampClassSwitch/run-review-fix.py')
 changes={p:{'fullBaseline':full['sourceSha256'].get(p),'reviewFix':revised['sourceSha256'].get(p)} for p in full['sourceSha256'].keys()|revised['sourceSha256'].keys() if full['sourceSha256'].get(p)!=revised['sourceSha256'].get(p)}
 assert changes==revised['inputComparison'] and set(changes)=={'Assets/Scripts/Core/GameSession.ClassSwitch.cs','Tests/ClassSwitchRuntimeBoundary.cs','Tests/ClassSwitchRuntimeTests.py'}
 matched.append(all((root/path).is_file() and sha(root/path)==digest for path,digest in revised['sourceSha256'].items()))
assert any(matched),'Current source differs from both frozen final incremental snapshots'
print('PASS: Windows full284; original iOS compile-setup failure retained, managed282 + exact-input compile repair; final three-input RNG change covered by both frozen 7-check runs; current hashes match')
