#!/usr/bin/env python3
"""Real friendly projectile contact branch; preserves collision, LOS, prop, volley and pierce guards.
Update lifecycle/movement/hostile branch and native physics are outside this managed probe.
"""
from CastReceiptFixtureSources import include_cast_receipt_source
import os,sys,tempfile,subprocess
from pathlib import Path
root=Path(__file__).resolve().parents[1];dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet')
source=(root/'Assets/Scripts/Combat/CombatEffects.cs').read_text()
# Restrict extraction to the actual projectile Update, preserving its whole friendly-contact branch.
a=source.index('private void Update()',source.index('internal sealed class CombatProjectile'));b=source.index('{',a)+1;depth=1
while depth:
 depth+=(source[b]=='{')-(source[b]=='}');b+=1
update=source[a:b]
a=update.index('                DestructibleProp hitProp;');b=update.index('\n            }\n            if (terrainHit)',a);branch=update[a:b]
mark='''                    if (LockedImpactMarkPolicy.ShouldApply(impactMarkTarget, enemy, !enemy.IsDead, impact.Amount, impactMarkStrength) && enemy.StatusEffects != null)
                        enemy.StatusEffects.Mark(4f, impactMarkStrength);
'''
assert branch.count(mark)==1
with tempfile.TemporaryDirectory(prefix='combat-review-impact-') as d:
 p=Path(d)
 for f in ['Core/GameTypes','Core/CombatBalance','Core/SkillDamageBudgets','Core/DestructiblePropRules','Core/LockedImpactMarkPolicy','Combat/CombatDamage','Combat/ProjectileVolleyBudget']:(p/(Path(f).name+'.cs')).write_text((root/('Assets/Scripts/'+f+'.cs')).read_text())
 (p/'Fixture.cs').write_text((root/'Tests/CombatReviewImpactProductionTests.cs').read_text())
 project=p/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NuGetAudit>false</NuGetAudit><NoWarn>0649;0414</NoWarn></PropertyGroup></Project>');include_cast_receipt_source(project);(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 cases=[('current',branch,None),('mark-after-damage',branch.replace(mark,'').replace('                    if (CombatReviewEvents.Enabled)',mark+'                    if (CombatReviewEvents.Enabled)'),'accepted locked hit marks before damage'),('lost-companion-callback',branch.replace('companionSource.OnConfirmedHit(enemy);',''),'companion callback survives expanded block and follows damage'),('bypass-los',branch.replace('if (!CombatSight.Direct(previous, enemy.transform.position)) continue;',''),'occluded candidate cannot mark or damage'),('lost-pierce-stop',branch.replace('if (!pierce) { Destroy(gameObject); return; }',''),'nonpiercing contact stops later candidates')]
 for name,body,expected in cases:
  if expected:assert body!=branch
  (p/'Contact.cs').write_text('using UnityEngine;namespace Emberfall{internal partial class CombatProjectile{public void Contacts(Vector3 previous){'+body+'}}}')
  build=subprocess.run([dotnet,'build',str(project),'--configfile',str(p/'NuGet.Config'),'-v:q'],capture_output=True,text=True)
  if build.returncode:print(build.stdout+build.stderr);build.check_returncode()
  r=subprocess.run([dotnet,str(p/'bin/Debug/net8.0/Test.dll')],capture_output=True,text=True)
  if expected:assert r.returncode and 'System.Exception: '+expected in r.stdout+r.stderr,r.stdout+r.stderr;print('PASS compiled precise negative:',name)
  else:print(r.stdout+r.stderr);r.check_returncode()
# All prior independent charge/pose/mark/audio/instrumentation source oracles remain enforced.
subprocess.run([sys.executable,str(root/'Tests/CombatReviewSourceTests.py')],check=True)
