from MobileBindingFixtureSources import include_mobile_binding_sources
"""Real pointer/aim/targeting/charge, full CastSkillCore and SkillRuntime.
Only engine, optional mechanics and final emission recipients are managed boundaries;
readiness, cost/cooldown commitment and final emitted intent execute production code.
"""
from CastReceiptFixtureSources import include_cast_receipt_source
from pathlib import Path
import os,sys,tempfile,subprocess
r=Path(__file__).resolve().parents[1];dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
def extract(path,signature):
 s=(r/'Assets/Scripts'/path).read_text();a=s.index(signature);b=s.index('{',a)+1;d=1
 while d:d+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
with tempfile.TemporaryDirectory(prefix='mobile-pin-production-') as t:
 p=Path(t)
 for path in ['Combat/PlayerController.SkillAvailability.cs','Core/CombatImpactBatch.cs','Core/SkillRuntime.cs','Core/GameTypes.cs','Core/CombatBalance.cs','Core/MobileCameraGesture.cs','Core/SkillDamageBudgets.cs','Combat/MobileSkillPolicy.cs','Combat/PlayerController.MobileFocus.cs','Combat/SkillChargeController.cs','UI/MobileControlLayout.cs','Core/CombatOpportunityState.cs','UI/MobileControls.OpportunityInput.cs','Core/CastFirstHitReceipt.cs','Combat/PlayerController.CastReceipts.cs']:(p/Path(path).name).write_text((r/'Assets/Scripts'/path).read_text())
 include_mobile_binding_sources(p,r,True)
 (p/'Fixture.cs').write_text((r/'Tests/MobilePinnedTargetProductionTests.cs').read_text())
 player=['internal int NewCastId()','private Vector3 ResolveMobileAim(','internal void PrepareMobileSkillAim(','internal void ResolveMobileSkillAim(','private static bool ValidAimTarget(','private static bool ProjectedBounds(','private void FaceAim(','private EnemyController MagicConeTarget(','private void BasicAttack(','internal bool CastImmediateSkill(','internal bool ConfirmTargetedSkill(','internal bool ExecuteChargedSkill(','private bool CanUseMovementSkill(','internal bool SkillTargetingReady(','internal bool CanBeginSkillTargeting(','internal Vector3 ResolveSkillGroundTarget(','private void CastSkill(','private void CastSkillCore(']
 controls=['private bool IsMovementStart(', 'public static bool IsScreenPointOverControls(', 'public bool ProcessPointer(','public static void ResetInput()','private void Update()']
 target=['public enum Shape','public struct Preview','public static Preview Describe(','public static bool RequiresConfirmation(','public bool Begin(','public void Cancel()']
 (p/'PlayerMethods.cs').write_text('using System.Collections.Generic;using UnityEngine;namespace Emberfall{public sealed partial class PlayerController{'+''.join(extract('Combat/PlayerController.cs',x) for x in player)+'}}')
 (p/'PointerMethods.cs').write_text('using System.Collections.Generic;using UnityEngine;namespace Emberfall{public sealed partial class MobileControls{'+''.join(extract('UI/MobileControls.cs',x) for x in controls)+'}}')
 (p/'TargetMethods.cs').write_text('using UnityEngine;namespace Emberfall{public partial class SkillTargetingController{'+''.join(extract('Combat/SkillTargetingController.cs',x) for x in target)+'}}')
 (p/'Program.cs').write_text('System.Console.WriteLine(MobilePinnedTargetProductionTests.Run());')
 project=p/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NuGetAudit>false</NuGetAudit><NoWarn>0649;0414</NoWarn></PropertyGroup></Project>');include_cast_receipt_source(project);(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1');build=[dotnet,'build',str(project),'--configfile',str(p/'NuGet.Config'),'-v:q'];run=[dotnet,str(p/'bin/Debug/net8.0/Test.dll')]
 subprocess.run(build,env=env,check=True);subprocess.run(run,env=env,check=True)
 mutations=[('MobileCameraGesture.cs','((x-startX)*(x-startX)+(y-startY)*(y-startY))/(density*density)>=Threshold*Threshold','Math.Abs(y-startY)/density>=Threshold','horizontal out-and-back never taps'),('PlayerMethods.cs','if(pinned!=null){AimTarget=pinned;return CombatFx.Flat(pinned.transform.position);}','if(false){AimTarget=pinned;return CombatFx.Flat(pinned.transform.position);}','movement and closer enemy cannot replace pin'),('TargetMethods.cs','if(!owner.MobilePinnedActionAllowed(index,true))return false;','','blocked pin rejects actual targeted skill without charge or resource spending')]
 for name,before,after,oracle in mutations:
  file=p/name;original=file.read_text();assert before in original;file.write_text(original.replace(before,after))
  # For the final mutant remove all new-action pin guards to reproduce old fallback behavior.
  playerfile=p/'PlayerMethods.cs';playerOriginal=playerfile.read_text()
  if name=='TargetMethods.cs':playerfile.write_text(playerOriginal.replace(' || !MobilePinnedActionAllowed(skill,true)',''))
  subprocess.run(build,env=env,check=True,stdout=subprocess.DEVNULL)
  failed=subprocess.run(run,env=env,capture_output=True,text=True)
  assert failed.returncode and 'System.Exception: '+oracle in failed.stdout+failed.stderr,failed.stdout+failed.stderr
  file.write_text(original)
  if name=='TargetMethods.cs':playerfile.write_text(playerOriginal)
 # A pin guard is appropriate for a new action, but putting it inside shared
 # CanBeginSkillTargeting would reject an already captured charge before its
 # executingChargedSkill flag is set. Both pin failure classes must detect it.
 playerfile=p/'PlayerMethods.cs';good=playerfile.read_text()
 for reason,oracle in [('目标被遮挡','actual readiness and full cast preserve confirmed point after later pin becomes blocked'),('距离不足','actual readiness and full cast preserve confirmed point after later pin exceeds skill range')]:
  before='if(SkillTargetingReady(skill))return true;';assert before in good
  playerfile.write_text(good.replace(before,'if(charge!=null&&charge.TargetPoint.sqrMagnitude>0&&MobilePinnedActionReason(skill)=="'+reason+'")return false;'+before))
  subprocess.run(build,env=env,check=True,stdout=subprocess.DEVNULL)
  failed=subprocess.run(run,env=env,capture_output=True,text=True)
  assert failed.returncode and 'System.Exception: '+oracle in failed.stdout+failed.stderr,failed.stdout+failed.stderr
  playerfile.write_text(good)
 before='executingChargedSkill ? charge.TargetEnemy : AimTarget';assert before in good
 playerfile.write_text(good.replace(before,'SummonedCompanion.ExplicitFocus(this)'))
 subprocess.run(build,env=env,check=True,stdout=subprocess.DEVNULL)
 failed=subprocess.run(run,env=env,capture_output=True,text=True)
 assert failed.returncode and 'System.Exception: full contract cast sends captured enemy and point despite later blocked pin and changed team' in failed.stdout+failed.stderr,failed.stdout+failed.stderr
 playerfile.write_text(good)
 print('PASS: compiled live-team-at-release mutation fails actual contract emitter identity assertion')
 print('PASS: two compiled shared-readiness pin guards fail exact blocked/range committed-charge assertions')
 print('PASS: three compiled old gesture/automatic-selection/action-fallback mutations fail exact production assertions')

 availability=p/'PlayerController.SkillAvailability.cs';goodAvailability=availability.read_text()
 assert 'if (!movement) return true;' in goodAvailability
 availability.write_text(goodAvailability.replace('if (!movement) return true;',''))
 subprocess.run(build,env=env,check=True,stdout=subprocess.DEVNULL)
 failed=subprocess.run(run,env=env,capture_output=True,text=True)
 assert failed.returncode and 'readiness observation never mutates companion focus' in failed.stdout+failed.stderr,failed.stdout+failed.stderr
 availability.write_text(goodAvailability)
 print('PASS: compiled companion-focus observation mutation rejected')


# UI wiring is source evidence, separate from executed combat semantics above.
feedback=(r/'Assets/Scripts/UI/MobileControls.Feedback.cs').read_text()
head=feedback[feedback.index('var pinned='):feedback.index('private static Rect VisualRect')]
assert 'MobilePinnedActionReason' not in head and 'pinned.DisplayName' in head
assert 'LabelControl(Attack,basicReason,true)' in feedback
feedback=(r/'Assets/Scripts/UI/GameUI.MobileFeedback.cs').read_text()
assert 'state.Length==0&&session.Player!=null?session.Player.MobilePinnedActionReason(skill)' in feedback
assert 'Text(caption,MobileCombatPresentation.SkillRejectionCaption(reason)' in feedback and 'state=="缺能"' in feedback
assert '点敌人固定目标；点战场空白取消' in (r/'Assets/Scripts/UI/GameUI.Mobile.cs').read_text()
print('PASS: source wiring for identity-only pin, per-action target/resource reasons and touch help (not rendered UI)')
