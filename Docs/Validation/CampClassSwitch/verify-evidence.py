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
 matched.append(all((root/path).is_file() and sha(root/path)==digest for path,digest in report['sourceSha256'].items()))
assert any(matched),'Current source differs from both validated platform snapshots'
print('PASS: both 282-check reports and all raw logs intact; current source matches a frozen complete run')
