#!/usr/bin/env python3
"""Compile actual session snapshot/evidence methods with explicit host substitutes."""
import os,sys,tempfile,subprocess
from pathlib import Path
root=Path(__file__).resolve().parents[1];dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet')
def method(source,signature):
 start=source.index(signature);brace=source.index('{',start);depth=1;end=brace+1
 while depth:
  depth+=(source[end]=='{')-(source[end]=='}');end+=1
 return source[start:end]
feedback=(root/'Assets/Scripts/Core/GameSession.Feedback.cs').read_text();expedition=(root/'Assets/Scripts/Core/GameSession.Expedition.cs').read_text()
methods='\n'.join([method((root/'Assets/Scripts/UI/GameUI.RunRecap.cs').read_text(),'private static float ProgressCardHeight('),method(feedback,'private string BuildRunSummary('),method(expedition,'public void RecordIncomingDamage('),method(expedition,'public void RecordActualHealing(')])
with tempfile.TemporaryDirectory(prefix='room-failure-evidence-') as temporary:
 path=Path(temporary)
 for relative in ['Assets/Scripts/Core/CombatImpactBatch.cs','Assets/Scripts/Core/CampPracticeRecord.cs','Assets/Scripts/Core/RunMechanismEvidence.cs','Assets/Scripts/Core/RoomChainState.cs','Assets/Scripts/Core/RoomTactics.cs','Assets/Scripts/Core/SideEventRun.cs','Assets/Scripts/UI/RunRecapPresentation.cs','Tests/RoomFailureEvidenceTests.cs','Tests/RunRecapPresentationTests.cs']:
  (path/Path(relative).name).write_text((root/relative).read_text())
 host=path/'Session.cs';host.write_text('using UnityEngine;using System.Collections.Generic;namespace Emberfall {public partial class GameSession { '+'public static float RewardCardHeight(RunRecapSnapshot s)=>ProgressCardHeight(s);public bool modeRewardDetailsUnavailable;public bool PracticeActive=>false;public CampPracticeRecord PracticeRecord=>throw new System.InvalidOperationException("ordinary room cannot access practice result");public RunMechanismEvidence MechanismEvidence {get;}=new RunMechanismEvidence();'+methods+' }}')
 (path/'Program.cs').write_text('System.Console.WriteLine(RoomFailureEvidenceTests.Run());System.Console.WriteLine(RunRecapPresentationTests.Run());')
 project=path/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>')
 config=path/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 subprocess.run([dotnet,'restore',str(project),'--configfile',str(config)],check=True)
 command=[dotnet,'run','--project',str(project),'--no-restore'];subprocess.run(command,check=True)
 original=host.read_text()
 mutated=original.replace('RoomChainRun.SealProgress(1).ToString', 'RoomChainRun.SealProgress(0).ToString');assert mutated!=original
 host.write_text(mutated);result=subprocess.run(command,capture_output=True,text=True)
 assert result.returncode and 'actual failed result preserves independent partial B-first evidence' in result.stdout+result.stderr,result.stdout+result.stderr
 print('PASS: compiled collapsed B-result evidence mutation fails')
 host.write_text(original.replace('RoomChainRun.Failure.ToString()','(RoomChainRun.Failed?"Abandoned":null)'))
 failed=subprocess.run(command,capture_output=True,text=True)
 assert failed.returncode and 'actual session snapshot must preserve exact room failure reason' in failed.stdout+failed.stderr,failed.stdout+failed.stderr
 print('PASS: old room-failure-to-Abandoned production snapshot mutation fails')
# Check production callsites which the managed host does not execute.
main=(root/'Assets/Scripts/Core/GameSession.cs').read_text()
assert 'RoomChainRun.Fail(RoomFailureReason.Death)' in method(main,'public void OnPlayerDied(')
for name in ['GameSession.RoomChain.cs','GameSession.RoomTactics.cs']:
 source=(root/'Assets/Scripts/Core'/name).read_text();assert 'RoomChainRun.Fail();' not in source and 'FailRoomGeneration(' in source
diagnostics=(root/'Assets/Scripts/Core/GameSession.RoomDiagnostics.cs').read_text()
assert 'RoomChainRun.Fail(RoomFailureReason.GenerationOrPathFailure)' in method(diagnostics,'private void FailRoomGeneration(')
assert main.index('if (!loadingSaveSnapshot && !enteringChapter && !retryingRoomChain && !SaveBeforeLeaving()) return false;')<main.index('LastRunSummary=BuildRunSummary(false, "Abandoned")')<main.index('int previousCombatEpoch = Player.CombatEpoch;',main.index('private bool ChangeZone'))
print('PASS: death/path/abandon callsite contracts; no Unity engine execution')

player=(root/"Assets/Scripts/Combat/PlayerController.cs").read_text()
heal=method(player,"public void Heal(")
assert heal.index("Health += healed")<heal.index("session.RecordActualHealing(healed)")
