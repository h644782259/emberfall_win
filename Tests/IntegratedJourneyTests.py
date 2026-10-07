"""Integrated real service/session journey with real temp IO; Unity/JSON/loot boundaries remain explicit."""
from CastReceiptFixtureSources import include_cast_receipt_source
from pathlib import Path
from IntegratedJourneyNegativeControls import run_negative_controls
import os,sys,tempfile,subprocess,argparse,json,hashlib,datetime
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('sdk',nargs='?',help='Dotnet executable used by cloud-validation')
parser.add_argument('--root',type=Path,default=Path(__file__).resolve().parents[1],help='Exact source worktree/snapshot root; source files are read only')
parser.add_argument('--dotnet',default=None)
parser.add_argument('--fixture',type=Path,default=Path(__file__).with_name('IntegratedJourneyFixture.cs'))
parser.add_argument('--extra-fixture',type=Path,action='append',default=[],help='Additional partial fixture; repeatable. Adjacent IntegratedBuildJourneyFixture.cs is also included when present.')
parser.add_argument('--output',type=Path,default=None,help='Optional retained evidence directory; otherwise uses temporary storage outside the repository')
args=parser.parse_args();root=args.root.resolve();dotnet=args.dotnet or args.sdk or os.environ.get('DOTNET','dotnet')
output=args.output.resolve() if args.output else Path(tempfile.mkdtemp(prefix='journey-evidence-'))
run_dir=output/datetime.datetime.now(datetime.timezone.utc).strftime('%Y%m%dT%H%M%S%fZ');run_dir.mkdir(parents=True)
assert args.fixture.is_file(),str(args.fixture)
def member(file,signature):
 s=(root/file).read_text();a=s.index(signature);b=s.index('{',a)+1;d=1
 while d:d+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
core=['CombatImpactBatch','CampPracticeRecord','ThreatAdmissionPolicy','RunMechanismEvidence','RunChoices','RunChoices.Rooms','RunChoices.Chapter','GameTypes','ProgressionService','ProgressionService.Chapter','ProgressionService.Attachments','ProgressionService.AutomaticGrowth','ProgressionService.Reforge','ReforgeQuote','ChapterProgression','ChapterResultSnapshot','ChapterCombatRun','RoomTactics','RoomTacticalRegion','CombatBalance','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState','AdventureResultPolicy','GameSession.Chapter','GameSession.ChapterSeals','EscapePostPolicy','ApplicationPauseState','SafeSaveFlow','SaveLifecycleGate','RoomChainState','ExpeditionModeState']
with tempfile.TemporaryDirectory(prefix='integrated-player-journey-') as t:
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
 (p/'Lifecycle.cs').write_text('using System.Collections.Generic;using UnityEngine;namespace Emberfall{public sealed partial class GameSession{'+''.join(methods)+'}}')
 ui='namespace Emberfall{public sealed class GameUI{GameSession session;public void RebindProgressionNotifications(ProgressionService a,ProgressionService b){}void BlockUITransition(){}public GameUI(GameSession s){session=s;}public void Return()=>ReturnFromChapter();'+member('Assets/Scripts/UI/GameUI.Chapter.cs','private void ReturnFromChapter()')+'}}'
 (p/'UI.cs').write_text(ui)
 enemy=(root/'Assets/Scripts/Combat/EnemyController.cs').read_text();a=enemy.index('            int challengeTier = game.InDungeon ? game.DungeonTier : 1;');b=enemy.index('            Health = MaxHealth;',a)+len('            Health = MaxHealth;')
 (p/'EnemyStats.cs').write_text('using UnityEngine;namespace Emberfall{public sealed partial class EnemyController{public float MaxHealth,Health,damage;void ApplySpawnStats(GameSession game,int level,bool boss){var kind=Kind;'+enemy[a:b]+'}}}')
 (p/'IntegratedJourneyFixture.cs').write_text(args.fixture.read_text())
 extras=list(args.extra_fixture);adjacent=args.fixture.with_name('IntegratedBuildJourneyFixture.cs')
 if adjacent.is_file() and adjacent.resolve() not in [f.resolve() for f in extras]:extras.append(adjacent)
 for extra in extras:
  assert extra.is_file(),str(extra)
  assert not (p/extra.name).exists(),'Duplicate host source name: '+extra.name
  (p/extra.name).write_text(extra.read_text())
 (p/'Program.cs').write_text('System.Console.WriteLine(Emberfall.GameSession.VerifyIntegratedJourney(args[0]));')
 project=p/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NuGetAudit>false</NuGetAudit><NoWarn>0649;0414</NoWarn></PropertyGroup></Project>');include_cast_receipt_source(project)
 (p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
 build=[dotnet,'build',str(project),'--configfile',str(p/'NuGet.Config'),'-v:q'];run=[dotnet,str(p/'bin/Debug/net8.0/Test.dll')]
 # Archive exact generated sources and source identity without changing the worktree.
 archive=run_dir/'host';archive.mkdir()
 for f in p.iterdir():
  if f.is_file():(archive/f.name).write_bytes(f.read_bytes())
 revision=subprocess.run(['git','-C',str(root),'rev-parse','HEAD'],capture_output=True,text=True)
 manifest={'root':str(root),'head':revision.stdout.strip() if revision.returncode==0 else None,
           'fixture':str(args.fixture.resolve()),'extraFixtures':[str(f.resolve()) for f in extras],'generatedSourceHashes':{f.name:hashlib.sha256(f.read_bytes()).hexdigest() for f in archive.iterdir()},
           'scope':'Actual chapter/session/progression methods and temporary filesystem. Managed scene, player/AI, System.Text.Json shim and cleanup boundaries; enemy loot delivery, world-loot retention and side-event settlement are stubbed. Skill respec/shared point budget is tested with zero mastery. Not Unity gameplay or complete loot lifecycle.'}
 (run_dir/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
 for label,command in [('build',build),('journey',run+[str(run_dir/'saves')])]:
  result=subprocess.run(command,env=env,capture_output=True,text=True)
  (run_dir/(label+'.log')).write_text(result.stdout+result.stderr)
  print(result.stdout+result.stderr,end='',flush=True)
  manifest[label+'ExitCode']=result.returncode
  (run_dir/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
  if result.returncode:raise SystemExit(result.returncode)
 negatives=run_negative_controls(p,dotnet,env,run_dir/'negative-controls')
 manifest['negativeControlsPassed']=negatives['passed'];manifest['negativeControlCount']=len(negatives['controls'])
 (run_dir/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
 print('PASS: '+str(len(negatives['controls']))+' compiled journey negative controls; original generated host unchanged')
 print('ARCHIVE: '+str(run_dir))
