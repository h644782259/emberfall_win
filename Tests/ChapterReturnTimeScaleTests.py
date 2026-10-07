"""Real chapter/UI return and session time-scale/reset methods; managed scene doubles, not Unity play."""
from CastReceiptFixtureSources import include_cast_receipt_source
from pathlib import Path
import os,sys,tempfile,subprocess
root=Path(__file__).resolve().parents[1]
dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
def member(file,signature):
 s=(root/file).read_text();a=s.index(signature);b=s.index('{',a)+1;d=1
 while d:d+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
core=['CombatImpactBatch','CampPracticeRecord','ThreatAdmissionPolicy','RunMechanismEvidence','RunChoices','RunChoices.Rooms','RunChoices.Chapter','GameTypes','ProgressionService','ProgressionService.Chapter','ProgressionService.Attachments','ProgressionService.AutomaticGrowth','ProgressionService.Reforge','ReforgeQuote','ChapterProgression','ChapterResultSnapshot','ChapterCombatRun','RoomTactics','RoomTacticalRegion','CombatBalance','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState','AdventureResultPolicy','GameSession.Chapter','GameSession.ChapterSeals','EscapePostPolicy','ApplicationPauseState','SafeSaveFlow','SaveLifecycleGate','RoomChainState','ExpeditionModeState']
with tempfile.TemporaryDirectory(prefix='chapter-return-clock-') as t:
 p=Path(t)
 for n in core:(p/(n+'.cs')).write_text((root/'Assets/Scripts/Core'/(n+'.cs')).read_text())
 (p/'EnemyControlPolicy.cs').write_text((root/'Assets/Scripts/Combat/EnemyControlPolicy.cs').read_text())
 for path in ['Assets/Scripts/UI/ChapterSealPresentation.cs','Assets/Scripts/World/ChapterRoomGeometry.cs','Assets/Scripts/UI/ChapterEntryPresentation.cs','Tests/ProgressionTests.cs','Tests/ChapterReturnTimeScaleTests.cs']:(p/Path(path).name).write_text((root/path).read_text())
 fixture=(root/'Tests/ChapterHostFixture.cs').read_text()
 replacements={
 'deltaTime=.25f,time;':'deltaTime=.25f,time,timeScale=1;',
 'public int CombatEpoch=1,':'public void Initialize(GameSession s,HeroClass h){}public int CombatEpoch=1,',
 'public class FakeChoices {public void Reset(){}public void Cancel(){}}':'public class FakeChoices {public bool AwaitingChoice;public void Reset(){AwaitingChoice=false;}public void Cancel(){AwaitingChoice=false;}}',
 'Paused,BackgroundPaused,IsInCamp=true;':'Paused,IsInCamp=true;public bool BackgroundPaused=>pauseState.BackgroundPaused;',
 'public bool InputBlocked=>Paused||BackgroundPaused||IsDead||ChapterFinished;':member('Assets/Scripts/Core/GameSession.cs','public bool InputBlocked'),
 'void UpdateTimeScale(){}':'',
 'public void Notify(string s)':'string SaveLoadError;GameUI ui;float autosaveTimer;readonly SaveLifecycleGate lifecycleSave=new SaveLifecycleGate();void OnProgressChanged(){}void OnLevelUp(int level){}void DiscardForeignSideEventRewards(){}readonly ApplicationPauseState pauseState=new ApplicationPauseState();string equipmentFingerprint;readonly Dictionary<string,PendingLoot> pendingLoot=new Dictionary<string,PendingLoot>();readonly HashSet<string> collectedGroundLoot=new HashSet<string>();class PendingLoot{public FakePickup Pickup;}class FakePickup{public void Retire(){}}void StopAllCoroutines(){}\npublic void Notify(string s)'
 }
 for a,b in replacements.items():
  assert a in fixture,a
  fixture=fixture.replace(a,b)
 (p/'Fixture.cs').write_text(fixture)
 methods=[member('Assets/Scripts/Core/GameSession.cs',sig) for sig in ['private bool ContinueAdventure(string slotId, bool discardUnsaved = false, bool alreadySaved = false)','public bool LoadSaveFromPause(','private bool ChangeZone(bool dungeon)','public bool SaveBeforeLeaving()','private void SpawnEnemy(','public void OnEnemyKilled(','public void OnPlayerDied()','public void ReturnToCamp()','private void UpdateTimeScale()','private void BeginAdventure()','private void DiscardTransientAdventureForLoad()','public void Respawn()','public bool QuitToTitle(']]
 methods.append(member('Assets/Scripts/Core/GameSession.RoomTactics.cs','private static bool LiveRoomEnemy('))
 methods+=[member('Assets/Scripts/Core/GameSession.Expedition.cs','private void ResetExpedition('),member('Assets/Scripts/Core/GameSession.Modes.cs','public bool ModeFinished')]
 # NPC pose/audio cleanup has its own Unity fixture; this replay isolates the chapter clock.
 (p/'Lifecycle.cs').write_text('using System.Collections.Generic;using UnityEngine;namespace Emberfall{public sealed partial class GameSession{'+''.join(methods)+'}}')
 ui='namespace Emberfall{public sealed class GameUI{GameSession session;public void RebindProgressionNotifications(ProgressionService a,ProgressionService b){}void BlockUITransition(){}public GameUI(GameSession s){session=s;}public void Return()=>ReturnFromChapter();'+member('Assets/Scripts/UI/GameUI.Chapter.cs','private void ReturnFromChapter()')+'}}'
 (p/'UI.cs').write_text(ui)
 enemy=(root/'Assets/Scripts/Combat/EnemyController.cs').read_text();a=enemy.index('            int challengeTier = game.InDungeon ? game.DungeonTier : 1;');b=enemy.index('            Health = MaxHealth;',a)+len('            Health = MaxHealth;')
 (p/'EnemyStats.cs').write_text('using UnityEngine;namespace Emberfall{public sealed partial class EnemyController{public float MaxHealth,Health,damage;void ApplySpawnStats(GameSession game,int level,bool boss){var kind=Kind;'+enemy[a:b]+'}}}')
 (p/'Program.cs').write_text('System.Console.WriteLine(Emberfall.GameSession.VerifyReturnClock(args[0]));')
 project=p/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NuGetAudit>false</NuGetAudit><NoWarn>0649;0414</NoWarn></PropertyGroup></Project>');include_cast_receipt_source(project)
 (p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
 build=[dotnet,'build',str(project),'--configfile',str(p/'NuGet.Config'),'-v:q'];run=[dotnet,str(p/'bin/Debug/net8.0/Test.dll')]
 subprocess.run(build,env=env,check=True);subprocess.run(run+[str(p/'saves')],env=env,check=True)
 source=p/'Lifecycle.cs';original=source.read_text()
 # Mutants must compile before exact runtime failure can count as evidence.
 for signature,expected in [('private bool ChangeZone(bool dungeon)','successful UI chapter return resumes camp time'),('private void DiscardTransientAdventureForLoad()','discarded role cannot leave an advancing clock')]:
  body=member('Assets/Scripts/Core/GameSession.cs',signature);assert 'UpdateTimeScale();' in body
  source.write_text(original.replace(body,body.replace('UpdateTimeScale();','')))
  subprocess.run(build,env=env,check=True,stdout=subprocess.DEVNULL)
  failed=subprocess.run(run+[str(p/('mutant-'+str(len(signature))))],env=env,capture_output=True,text=True)
  assert failed.returncode and 'System.Exception: '+expected in failed.stdout+failed.stderr,failed.stdout+failed.stderr
 source.write_text(original)
 print('PASS: both compiled old transition/discard clock mutations fail exact production-chain assertions')
