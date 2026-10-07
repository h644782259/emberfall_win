#!/usr/bin/env python3
"""Real branch/transition/reset/retry host chain with real state, choices and traversal; explicit Unity/IO doubles."""
from pathlib import Path
import os, subprocess, sys, tempfile
root=Path(__file__).resolve().parents[1]
dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet')
def source(path):
 return (root/path).read_text()
def replace_required(text,before,after):
 assert text.count(before)==1, 'fixture/production seam changed: '+before
 return text.replace(before,after)
def member(s,sig):
 a=s.index(sig);b=s.index('{',a)+1;d=1
 while d:d+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
room=source('Assets/Scripts/Core/GameSession.RoomChain.cs');session=source('Assets/Scripts/Core/GameSession.cs')
methods='\n'.join(member(room,s) for s in ['public bool NearRoomExit','private void BeginRoomChainScene()','private void RecordRoomDefeat(','public bool EnterNextRoom()','private bool EnterNextRoomAfterSave()','private bool ConfirmRoomInterlude(','private void ResetRoomChain('])+'\n'+'\n'.join(member(session,s) for s in ['public bool SaveBeforeLeaving()','public bool InputBlocked','private void UpdateTimeScale()','private bool ChangeZone(bool dungeon)'])
# Reuse the room host's explicit scene/enemy/persistence doubles, but replace its
# choice and pause shortcuts with the real production implementations.
fixture=source('Tests/RoomFreeSealHostTests.cs').split('public static class RoomFreeSealHostTests')[0]
fixture=replace_required(fixture,' public enum EnemyKind{Wisp,Guardian,Goblin,Slime}\n','')
fixture=replace_required(fixture,'public Color(float r,float g,float b)','public Color(float r,float g,float b,float a=1)')
start=fixture.index(' public class ChoiceDouble');end=fixture.index(' public static class MobileControls',start);fixture=fixture[:start]+fixture[end:]
fixture=replace_required(fixture,'public object Profile=new object();','public GameProfile Profile=new GameProfile{heroClass=HeroClass.Vanguard,skillRanks=new[]{1,1,1,0,1,1,1,1,0,1},equippedSkills=new[]{0,1,2,4,5}};')
fixture=replace_required(fixture,'public ChoiceDouble RunChoices=new ChoiceDouble();','public RunChoices RunChoices=new RunChoices();')
fixture=replace_required(fixture,'public bool InputBlocked=>Paused||BackgroundPaused||IsDead;','private ApplicationPauseState pauseState=new ApplicationPauseState();private bool uiBlocking,DungeonSelectionOpen;private bool ModeFinished=>RoomChainRun!=null&&RoomChainRun.Finished;')
fixture=replace_required(fixture,'void UpdateTimeScale(){}','')
math=source('Tests/DestructibleTraversalTests.cs');math='using System;using UnityEngine;'+math[math.index('namespace Emberfall'):]
math=replace_required(math,'    public enum ZoneKind{Wilderness,Dungeon}\n','');math=replace_required(math,'public static float time=0;','public static float time=0,deltaTime=.25f,timeScale=1;public static int frameCount;')
fixture=fixture.replace('public bool PracticeActive=>false;', 'public bool PracticeActive=>false;public CampPracticeRecord PracticeRecord=>throw new System.InvalidOperationException("ordinary room fixture cannot access practice record");')
files=['Core/CombatImpactBatch','Core/CampPracticeRecord','Core/RoomChainState','Core/RoomTactics','Core/RoomTacticalRegion','Core/DeferredRoomChoice','Core/EscapePostPolicy','Core/GameSession.RoomDiagnostics','Core/GameSession.RoomTactics','World/WorldTraversal','World/WorldTraversal.Platforms','World/TacticalRoomGeometry','World/EscapeRoomFormation','World/RoomBranchGeometry','UI/ChapterSealPresentation','UI/RoomObjectivePresentation','Core/GameTypes','Core/CombatBalance','Core/RunChoices','Core/RunChoices.Rooms','Core/ApplicationPauseState','Core/GameSession.RoomBranch']
# All inputs come from this checkout; no pinned Git revision or external probe files.
originals={Path(f).name+'.cs':source('Assets/Scripts/'+f+'.cs') for f in files}
fixture=replace_required(fixture,'void FinalizeRoomChain(){}','void FinalizeRoomChain(){}private bool roomResultRecorded;private string modeReceipt;public int SelectedArenaMode=3,SelectedDungeonTier=1;public bool ChallengeRun;')
fixture=fixture.replace('RoomChainRun=new RoomChainState(seed);','DungeonTier=1+seed%4;ChallengeRun=seed%2==1;ResetRoomChain(true);')
# Actual ChangeZone + ResetExpedition participate; unrelated chapter/arena/world/IO are explicit boundaries.
methods+='\n'+member(source('Assets/Scripts/Core/GameSession.Expedition.cs'),'private void ResetExpedition(bool dungeon)')
fixture=fixture.replace('public static GameObject Build','public static bool FailNextBuild;public static GameObject Build').replace('{WorldTraversal.Reset(kind);','{if(FailNextBuild){FailNextBuild=false;throw new Exception("injected world failure");}WorldTraversal.Reset(kind);')
fixture=fixture.replace('public void Tick(){','''public void Fresh(){ChangeZone(false);SelectedArenaMode=3;ChangeZone(true);}
 public void Fail(){RoomChainRun.Fail(RoomFailureReason.Death);IsDead=true;}
 public bool Changing=>changingZone;public string Receipt=>modeReceipt;public int History=>previousRoomSeed;public void Background(bool b){pauseState.SetSuspended(b);UpdateTimeScale();}
 public bool ExitForTest()=>ChangeZone(false);
 public void Tick(){''')
fixture=fixture.replace('public void Heal(float n){}','public void Heal(float n){}public void RefreshStats(bool full){}public void ResetCooldownsForDungeonEntry(){}')
fixture=fixture.replace('Build(ZoneKind kind,int layout,int tier)','Build(ZoneKind kind,int layout,int tier,int hub=0,int seed=0)')
fixture=fixture.replace('public static GameObject MakeLootBeacon','public static void ApplyChapterLandmark(GameObject o,int mask){}public static GameObject MakeLootBeacon')
fixture=fixture.replace('private bool objectiveHealedThisWave,changingZone;','''private bool objectiveHealedThisWave,changingZone,loadingSaveSnapshot,enteringChapter,DungeonCleared;
 private object waveRoutine;private int CurrentHub=1,ChapterSeed,ActiveChapterNode,ChapterRoomIndex,HealingCharges,recapGoldLost;
 private int MaximumDungeonTier=10;private float respawnTimer,runDamageTaken,runHealingReceived,lastDamageAmount,lastInterruptAt,nextReinforcementAt;
 private string LastRunSummary,lastDamageSource;private ChapterBoundary chapterReceipt=new ChapterBoundary(),chapterPlan=new ChapterBoundary();
 private readonly System.Collections.Generic.List<int> reinforcementQueue=new System.Collections.Generic.List<int>();
 private readonly System.Collections.Generic.Dictionary<string,int> combatActions=new System.Collections.Generic.Dictionary<string,int>();
 public RunMechanismEvidence MechanismEvidence=new RunMechanismEvidence();
 private ArenaBoundary ModeRun;private void EndHubNpcConversation(){}private void StopCoroutine(object o){}private string BuildRunSummary(bool b,string f)=>f;
 private void BeginChapterRoom(){throw new Exception("chapter branch must remain excluded");}
 private void BeginArenaScene(){throw new Exception("arena branch must remain excluded");}
 private void SpawnDungeonWave(){throw new Exception("wave branch must remain excluded");}
 private void SpawnWildernessEnemy(){}private void ClearDungeonSettlement(){}private void ResetChapterRun(){}
 private void ResetArenaMode(bool dungeon){ResetRoomChain(dungeon);}
''')
fixture+='\nnamespace Emberfall{public class ChapterBoundary{public int Tier=1,Layout;public UnityEngine.Vector3 Entrance;}public static class ChapterRoomGeometry{public static ChapterBoundary Plan(int n,int i,int s)=>new ChapterBoundary();}public enum ExpeditionModeFailure{Abandoned}public class ArenaBoundary{public void Fail(ExpeditionModeFailure f){}}public class RunMechanismEvidence{public void Reset(){}}}\nnamespace UnityEngine{public static class Random{public static int Calls;public static int Range(int a,int b){Calls++;return 999;}}}'
fixture=fixture.replace('public bool Fail;public string LastError;', 'public int FailOnSave=-1;public bool Fail;public string LastError;').replace('LastError=Fail?', 'LastError=(Fail||Saves==FailOnSave)?')
expedition=source('Assets/Scripts/Core/GameSession.Expedition.cs')
methods+='\n'+'\n'.join(member(expedition,sig) for sig in ['public bool SideEventAvailable','private void BuildSideEvent()','public bool StartSideEvent()','private void AbandonSideEvent()','private object SideEventContext'])+'\n'+member(session,'private bool TrySafeSpawn(')
fixture=fixture.replace('void AbandonSideEvent(){}','').replace('void BuildSideEvent(){}','')
fixture=fixture.replace('bool TrySafeSpawn(Vector3 desired,float radius,float safe,out Vector3 p){p=desired;return true;}','')
fixture=fixture.replace('public static GameObject MakeLootBeacon','public static GameObject MakeSideEventCrystal(Vector3 p)=>MakeRoomObjective(p);public static GameObject MakeLootBeacon')
fixture=fixture.replace('public bool PracticeActive=>false;','public bool PracticeActive=>false;private float ArenaRadius=18;private SideEventRun sideEventRun;private GameObject sideCrystal;private bool sideEventStarted,sideEventOfferShown;private Vector3 sideEventPosition;private System.Collections.Generic.HashSet<EnemyController> sideEventEnemies=new System.Collections.Generic.HashSet<EnemyController>();')
fixture+='\nnamespace Emberfall{public static class EncounterPlan{public const int MaximumSimultaneous=8;}public static class SideEventEnemyMarker{public static void Attach(EnemyController e,GameSession s){}}}'
originals['SideEventRun.cs']=source('Assets/Scripts/Core/SideEventRun.cs')
fixture=fixture.replace('DungeonTier=1+seed%4;ChallengeRun=seed%2==1;', 'DungeonTier=15;ChallengeRun=true;')
from RoomWorldFixture import attach
fixture,math=attach(root,fixture,math,originals,member)
probe=source('Tests/RoomSideBranchProductionTests.cs')
with tempfile.TemporaryDirectory(prefix='room-branch-') as temporary:
 base=Path(temporary)
 env=dict(os.environ,DOTNET_CLI_HOME=str(base/'cli'),DOTNET_NOLOGO='1')
 for mode,expected in [('current',None),('unreserved-pair','reachable optional crystal starts both enemies')]:
  p=base/mode;p.mkdir(exist_ok=True)
  for name,s in originals.items():
   if mode=='double-save-branch' and name=='GameSession.RoomBranch.cs':s=replace_required(s,'UpdateTimeScale();return EnterNextRoomAfterSave();','UpdateTimeScale();return EnterNextRoom();')
   if mode=='old-third-room' and name=='RoomChainState.cs':s=replace_required(s,'Room.Index+1,Room.Seed,SelectedBranch','Room.Index+1,Room.Seed')
   (p/name).write_text(s)
  if mode=='unreserved-pair':methods=replace_required(methods,'out wispPosition,guardPosition,2.5f)', 'out wispPosition)')
  body=replace_required(methods,'if(retryingRoomChain)runSeed=roomRetrySeed;','if(retryingRoomChain)runSeed=roomRetrySeed+1;') if mode=='reroll-retry' else methods
  if mode=='double-save-retry':body=replace_required(body,' && !retryingRoomChain && !SaveBeforeLeaving()', ' && !SaveBeforeLeaving()')
  (p/'Methods.cs').write_text('using UnityEngine;using System.Collections.Generic;namespace Emberfall{public sealed partial class GameSession{'+body+'}}')
  (p/'Fixture.cs').write_text(fixture);(p/'Math.cs').write_text(math);(p/'Probe.cs').write_text(probe)
  project=p/'Probe.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NuGetAudit>false</NuGetAudit><NoWarn>0649;0414</NoWarn></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
  build=subprocess.run([dotnet,'build',str(project),'--configfile',str(p/'NuGet.Config'),'-v:q'],env=env,capture_output=True,text=True);(p/'build.log').write_text(build.stdout+build.stderr);print('BUILD '+mode+'\n'+build.stdout+build.stderr,flush=True)
  if build.returncode:print(build.stdout+build.stderr);build.check_returncode()
  run=subprocess.run([dotnet,str(p/'bin/Debug/net8.0/Probe.dll')],env=env,capture_output=True,text=True);(p/'run.log').write_text(run.stdout+run.stderr)
  if expected:print('NEGATIVE RAW '+mode+'\n'+run.stdout+run.stderr,flush=True);assert run.returncode and 'System.Exception: '+expected in run.stdout+run.stderr,run.stdout+run.stderr;print('PASS compiled negative:',mode)
  else:print(run.stdout+run.stderr);run.check_returncode()
