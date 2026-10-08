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
 include_mobile_binding_sources(p,r,True)
 for path in ['Combat/PlayerController.SkillAvailability.cs','Core/CombatImpactBatch.cs','Core/SkillRuntime.cs','Core/GameTypes.cs','Core/CombatBalance.cs','Core/MobileCameraGesture.cs','Core/SkillDamageBudgets.cs','Combat/MobileSkillPolicy.cs','Combat/PlayerController.MobileFocus.cs','Combat/SkillChargeController.cs','UI/MobileControlLayout.cs','Core/CombatOpportunityState.cs','UI/MobileControls.OpportunityInput.cs','Core/CastFirstHitReceipt.cs','Combat/PlayerController.CastReceipts.cs']:(p/Path(path).name).write_text((r/'Assets/Scripts'/path).read_text())
 (p/'Fixture.cs').write_text((r/'Tests/MobilePinnedTargetProductionTests.cs').read_text())
 player=['internal int NewCastId()','private Vector3 ResolveMobileAim(','internal void PrepareMobileSkillAim(','internal void ResolveMobileSkillAim(','private static bool ValidAimTarget(','private static bool ProjectedBounds(','private void FaceAim(','private EnemyController MagicConeTarget(','private void BasicAttack(','internal bool CastImmediateSkill(','internal bool ConfirmTargetedSkill(','internal bool ExecuteChargedSkill(','private bool CanUseMovementSkill(','internal bool SkillTargetingReady(','internal bool CanBeginSkillTargeting(','internal Vector3 ResolveSkillGroundTarget(','private void CastSkill(','private void CastSkillCore(']
 controls=['private bool IsMovementStart(', 'public static bool IsScreenPointOverControls(', 'public bool ProcessPointer(','public static void ResetInput()','private void Update()']
 target=['public enum Shape','public struct Preview','public static Preview Describe(','public static bool RequiresConfirmation(','public bool Begin(','public void Cancel()']
 (p/'PlayerMethods.cs').write_text('using System.Collections.Generic;using UnityEngine;namespace Emberfall{public sealed partial class PlayerController{'+''.join(extract('Combat/PlayerController.cs',x) for x in player)+'}}')
 (p/'PointerMethods.cs').write_text('using System.Collections.Generic;using UnityEngine;namespace Emberfall{public sealed partial class MobileControls{'+''.join(extract('UI/MobileControls.cs',x) for x in controls)+'}}')
 (p/'TargetMethods.cs').write_text('using UnityEngine;namespace Emberfall{public partial class SkillTargetingController{'+''.join(extract('Combat/SkillTargetingController.cs',x) for x in target)+'}}')
 (p/'Floating.cs').write_text((r/'Tests/MobileFloatingJoystickTests.cs').read_text())
 (p/'Program.cs').write_text('System.Console.WriteLine(MobileFloatingJoystickTests.Run());')
 project=p/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NuGetAudit>false</NuGetAudit><NoWarn>0649;0414</NoWarn></PropertyGroup></Project>');include_cast_receipt_source(project);(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1');build=[dotnet,'build',str(project),'--configfile',str(p/'NuGet.Config'),'-v:q'];run=[dotnet,str(p/'bin/Debug/net8.0/Test.dll')]
 subprocess.run(build,env=env,check=True);subprocess.run(run,env=env,check=True)
