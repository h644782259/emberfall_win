#!/usr/bin/env python3
"""Verify tracked raw evidence, initial failures/repair closure, and frozen final inputs."""
from pathlib import Path
import hashlib,json,subprocess,sys
root=Path(__file__).resolve().parents[3]
base=Path(__file__).resolve().parent
evidence=base/'Evidence'
def sha(data):return hashlib.sha256(data).hexdigest()
def read(path):return json.loads(path.read_text())
inventory=read(evidence/'manifest.json')
for name,digest in inventory.items():
 assert sha((evidence/name).read_bytes())==digest,name
expected={'Tests/CampBuildDraftProductionTests.py','Tests/CampBuildDraftUIBoundary.cs','Tests/PresetReplacementProductionTests.py','Tests/RewardRevisionTests.py'}
final_maps=[]
current_matches=[]
completed=[]
for platform,failures in [('windows',{'preset-replacement','camp-build-draft-production'}),('ios',{'preset-replacement','camp-build-draft-production','reward-revision'})]:
 historical=read(base/'results.json')['platforms']['win' if platform=='windows' else 'ios']
 assert sha((evidence/'initial'/platform/'report.json').read_bytes())==historical['fullReportSha256']
 for check,log in [('camp-build-draft-production','draft'),('preset-replacement','preset'),('reward-revision','reward')]:
  assert sha((evidence/'reruns'/platform/(log+'.log')).read_bytes())==historical['targetedRepairReruns'][check]['logSha256']
 initial=read(evidence/'initial'/platform/'report.json')
 baseline=read(evidence/'initial'/platform/'repaired-baseline-sources.json')
 assert initial['passed'] is False
 assert {x['name'] for x in initial['checks'] if not x['passed']}==failures
 changes={k for k,v in baseline['sources'].items() if initial['sourceSha256'].get(k)!=v}
 assert changes==expected==set(baseline['changedSinceInitial'])
 assert set(initial['sourceChangedDuringRun'])==expected-{'Tests/RewardRevisionTests.py'}
 draft=(evidence/'reruns'/platform/'draft.log').read_text()
 assert '461' in draft and '136' in draft,'combined draft + preset coverage missing'
 for check in initial['checks']:assert (evidence/'initial'/platform/check['log']).is_file(),check
 final_path=evidence/'final'/platform/'report.json'
 frozen=read(evidence/'final'/platform/'frozen-sources.json')
 if final_path.exists():
  final=read(final_path)
  assert final['passed'] and not final['sourceChangedDuringRun']
  assert len(final['checks'])==279 and all(x['passed'] for x in final['checks'])
  assert 'build-plan-page-geometry' in {x['name'] for x in final['checks']}
  for check in final['checks']:assert (evidence/'final'/platform/check['log']).is_file(),check
  assert final['sourceSha256']==frozen
  completed.append(platform)
 else:
  assert '--allow-pending' in sys.argv,'Clean aggregate still pending: '+platform
  assert platform=='windows' and read(evidence/'final'/platform/'pending-at-review.json')['complete'] is False
  final={'sourceSha256':frozen} # Hash comparison only; never synthesize a passing report.
 comparison={k:{'reviewedBaseline':baseline['sources'].get(k),'frozenReviewFix':final['sourceSha256'].get(k)} for k in baseline['sources'].keys()|final['sourceSha256'].keys() if baseline['sources'].get(k)!=final['sourceSha256'].get(k)}
 assert comparison==read(evidence/'final'/platform/'input-comparison.json')
 assert set(comparison)=={'Assets/Scripts/UI/GameUI.BuildPlans.cs','Tests/BuildPlanPageGeometryTests.py','Tests/BuildPlanPageGeometryBoundary.cs','Tools/cloud-validation.py','Tests/PresetReplacementTests.cs'}
 final_maps.append(final['sourceSha256'])
 current={k:(sha((root/k).read_bytes()) if (root/k).is_file() else None) for k in final['sourceSha256']}
 current_matches.append(current==final['sourceSha256'])
assert any(current_matches),'current checkout differs from frozen UI validation inputs'
assert {k for k in final_maps[0].keys()|final_maps[1].keys() if final_maps[0].get(k)!=final_maps[1].get(k)}=={'Tests/MobileFeedbackFontSourceTests.py','Assets/Editor/IOSBuild.cs'},'unexpected differences between platform repositories'
print('PASS evidence integrity and current frozen inputs; completed clean 279-check platforms: '+', '.join(completed)+('; Windows aggregate pending, not a passing full run' if len(completed)<2 else '; both complete'))
