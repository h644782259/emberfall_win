#!/usr/bin/env python3
from pathlib import Path
import hashlib,json
root=Path(__file__).resolve().parents[3];e=Path(__file__).resolve().parent/'Evidence'
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
for path,digest in json.loads((e/'manifest.json').read_text()).items():assert sha(e/path)==digest,path
matched=[]
for platform in ['windows','ios']:
 report=json.loads((e/platform/'report.json').read_text());assert report['passed'] and len(report['checks'])==282 and not report['sourceChangedDuringRun']
 assert all(check['passed'] and (e/platform/check['log']).is_file() for check in report['checks'])
 revised=json.loads((e/'review-fix'/platform/'report.json').read_text())
 assert revised['passed'] and len(revised['checks'])==5 and not revised['sourceChangedDuringRun']
 assert all(check['passed'] and (e/'review-fix'/platform/check['log']).is_file() for check in revised['checks'])
 assert revised['baselineSha256']==sha(e/platform/'report.json')
 assert revised['runnerSha256']==sha(Path(__file__).resolve().parent/'run-review-fix.py')
 changes={p:{'fullBaseline':report['sourceSha256'].get(p),'reviewFix':revised['sourceSha256'].get(p)} for p in report['sourceSha256'].keys()|revised['sourceSha256'].keys() if report['sourceSha256'].get(p)!=revised['sourceSha256'].get(p)}
 assert changes==revised['inputComparison'] and set(changes)=={'Assets/Scripts/Core/GameSession.ClassSwitch.cs','Tests/ClassSwitchRuntimeBoundary.cs','Tests/ClassSwitchRuntimeTests.py'}
 matched.append(all((root/path).is_file() and sha(root/path)==digest for path,digest in revised['sourceSha256'].items()))
assert any(matched),'Current source differs from both frozen incremental snapshots'
print('PASS: original 282-check baselines intact; three changed inputs covered by frozen 5-check incremental runs; current source matches')
