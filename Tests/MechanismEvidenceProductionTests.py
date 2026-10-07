"""Actual health mutation ordering + per-instance ledger. Engine and reward callback are explicit doubles."""
from CastReceiptFixtureSources import include_cast_receipt_source
import importlib.util,os,re,subprocess,sys,tempfile
from pathlib import Path
root=Path(__file__).resolve().parents[1];dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
spec=importlib.util.spec_from_file_location('chapter',root/'Tests/ChapterCombatProductionTests.py');chapter=importlib.util.module_from_spec(spec);spec.loader.exec_module(chapter)
source=(root/'Assets/Scripts/Combat/EnemyController.cs').read_text();damage=chapter.member(source,'public void TakeDamage(')
area=chapter.member((root/'Assets/Scripts/Combat/CombatEffects.cs').read_text(),'internal sealed class CombatArea')
player=(root/'Assets/Scripts/Combat/PlayerController.cs').read_text()
assert player.count('trackedMechanic:0')==1 and player.count('trackedMechanic:1')==1
assert 'area.mechanismInstance=game.MechanismEvidence.Register(player,area.epoch,trackedMechanic)' in area
assert 'if(IsCurrentCast&&mechanismInstance!=null)mechanismInstance.Record(session.Player,owner.CombatEpoch,amount)' in area
assert 'actualHealthLoss:mechanismInstance==null?(System.Action<float>)null:RecordMechanismHealthLoss' in area
fixture=(root/'Tests/DestructibleTraversalTests.cs').read_text();fixture='using System;using UnityEngine;'+fixture[fixture.index('namespace Emberfall'):]
for declaration in ['public static class CombatFx','public struct Vector2','public struct Vector3','public static class Time','public static class Mathf']:fixture=fixture.replace(declaration,declaration.replace('class ','partial class ').replace('struct ','partial struct '))
enums='namespace Emberfall{'+''.join(re.findall(r'public enum Chapter(?:Node|Difficulty)\s*\{[^}]*\}',(root/'Assets/Scripts/Core/ChapterProgression.cs').read_text()))+'}'
area_host='using UnityEngine;namespace Emberfall {internal sealed class AreaEvidenceHost {PlayerController owner;GameSession session;int epoch;RunMechanismEvidence.Instance mechanismInstance;'+chapter.member(area,'private bool IsCurrentCast')+chapter.member(area,'private void RecordMechanismHealthLoss(')+'public AreaEvidenceHost(GameSession game,RunMechanismEvidence.Instance token){session=game;owner=game.Player;epoch=owner.CombatEpoch;mechanismInstance=token;}public void Report(float amount){RecordMechanismHealthLoss(amount);}}}'
program=''' using System;using System.Reflection;using Emberfall;using UnityEngine;
class EvidenceTest {static int n;static void Check(bool b,string m){n++;if(!b)throw new Exception(m);}static void Main(){
 var ledger=new RunMechanismEvidence();var owner=new object();var tokens=new RunMechanismEvidence.Instance[6];for(int i=0;i<6;i++)tokens[i]=ledger.Register(owner,4,0);
 Check(ledger.Snapshot()[0]==6&&ledger.Snapshot()[1]==0,"six empty instances are six/zero");for(int i=0;i<4;i++)for(int tick=0;tick<20;tick++)tokens[i].Record(owner,4,3);
 Check(ledger.Snapshot()[0]==6&&ledger.Snapshot()[1]==4,"six created four distinct effective despite repeated targets and ticks");
 tokens[4].Record(new object(),4,5);tokens[4].Record(owner,5,5);tokens[4].Record(owner,4,0);tokens[4].Record(owner,4,float.NaN);tokens[4].Record(owner,4,float.PositiveInfinity);Check(ledger.Snapshot()[1]==4,"stale owner epoch invalid damage rejected");
 ledger.Reset();tokens[5].Record(owner,4,5);Check(ledger.Snapshot()[0]==0&&ledger.Snapshot()[1]==0,"reset invalidates previous attempt tokens");
 var game=new GameSession();game.HasStarted=true;GameSession.Instance=game;game.Progression=new ProgressionService();var enemy=new EnemyController();enemy.Health=5;enemy.MaxHealth=5;typeof(EnemyController).GetField("session",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(enemy,game);
 var lethal=ledger.Register(game.Player,game.Player.CombatEpoch,1);var areaHost=new AreaEvidenceHost(game,lethal);int atDeath=-1;game.KilledEvidence=()=>{atDeath=ledger.Snapshot()[3];game.CombatEnded=true;};
 enemy.TakeDamage(50,new Vector3(),impact:false,actualHealthLoss:areaHost.Report);Check(atDeath==1,"lethal actual health loss recorded before synchronous result snapshot");Check(ledger.Snapshot()[2]==1&&ledger.Snapshot()[3]==1,"lethal instance once only");enemy.TakeDamage(50,new Vector3(),impact:false,actualHealthLoss:areaHost.Report);Check(ledger.Snapshot()[3]==1,"finished/dead damage does not add evidence");
 var snapshot=ledger.Snapshot();ledger.Reset();Check(snapshot[2]==1&&snapshot[3]==1,"result snapshot independent of next reset");
 game.CombatEnded=false;var stale=ledger.Register(game.Player,game.Player.CombatEpoch,1);var staleArea=new AreaEvidenceHost(game,stale);game.Player.CombatEpoch++;staleArea.Report(2);Check(ledger.Snapshot()[3]==0,"production area callback rejects retired epoch");
 var dead=ledger.Register(game.Player,game.Player.CombatEpoch,1);var deadArea=new AreaEvidenceHost(game,dead);game.Player.IsDead=true;deadArea.Report(2);Check(ledger.Snapshot()[3]==0,"production area callback rejects dead owner");game.Player.IsDead=false;game.CombatEnded=true;deadArea.Report(2);Check(ledger.Snapshot()[3]==0,"production area callback rejects finished run");game.CombatEnded=false;game.Player=new PlayerController();deadArea.Report(2);Check(ledger.Snapshot()[3]==0,"production area callback rejects replaced player");Console.WriteLine("PASS: "+n+" actual damage ordering and instance evidence assertions");}}
'''
with tempfile.TemporaryDirectory(prefix='mechanism-evidence-') as temp:
 p=Path(temp)
 # Evidence suite: rigid anchor art does not own health loss or result ordering.
 # Actual F2 resource/mesh application is covered by the combined actor-art suite.
 (p/'AnchorArtBoundary.cs').write_text('using UnityEngine;namespace Emberfall { internal static class EnemySilhouetteArt { internal static void ApplyAnchors(Transform root) {} } }')
 (p/'AreaCallback.cs').write_text(area_host);(p/'Math.cs').write_text(fixture);(p/'Enums.cs').write_text(enums)
 for name in ['Combat/EnemyControlPolicy','Core/RunMechanismEvidence','Core/CampPracticeRecord','Core/GuardArmorRules','Core/AdventureResultPolicy','Core/CombatVisualBudget','Core/CombatImpactBatch','Core/LargeBossPhaseState','Core/ChapterBossPattern','Core/ArenaPulseRules','Core/CombatSightRules','World/ChapterRoomGeometry','World/WorldTraversal','World/WorldTraversal.Platforms','World/ChapterHazards','World/ChapterHazardGeometry','Combat/LargeExpeditionBoss','Combat/CombatSight','Combat/ThreatVisualStyle']:(p/(Path(name).name+'.cs')).write_text((root/'Assets/Scripts'/(name+'.cs')).read_text())
 for name in ['ChapterCombatProductionTests','ChapterCombatProductionAttackFixture']:
  s=(root/'Tests'/(name+'.cs')).read_text().replace('public void OnEnemyKilled(EnemyController e){}','public System.Action KilledEvidence;public void OnEnemyKilled(EnemyController e){KilledEvidence?.Invoke();}')
  (p/(name+'.cs')).write_text(s)
 (p/'ActualDamage.cs').write_text('using UnityEngine;namespace Emberfall{public partial class EnemyController{'+damage+chapter.member(source,'internal bool TrySkillInterrupt(')+'}}');(p/'Program.cs').write_text(program)
 project=p/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NuGetAudit>false</NuGetAudit><NoWarn>0649;0414</NoWarn></PropertyGroup></Project>');include_cast_receipt_source(project);(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1')
 subprocess.run([dotnet,'build',str(project),'--configfile',str(p/'NuGet.Config'),'-v:q'],env=env,check=True);cmd=[dotnet,str(p/'bin/Debug/net8.0/Test.dll')];subprocess.run(cmd,env=env,check=True)
 file=p/'ActualDamage.cs';original=file.read_text();callback='if(actualHealthLoss!=null&&Health<previousHealth)actualHealthLoss(previousHealth-Health);';file.write_text(original.replace(callback,'').replace('session.OnEnemyKilled(this);','session.OnEnemyKilled(this);'+callback))
 subprocess.run([dotnet,'build',str(project),'--no-restore','-v:q'],env=env,check=True,stdout=subprocess.DEVNULL);result=subprocess.run(cmd,env=env,capture_output=True,text=True)
 if result.returncode==0 or 'lethal actual health loss recorded before synchronous result snapshot' not in result.stdout+result.stderr:raise AssertionError(result.stdout+result.stderr)
 print('PASS: compiled late-callback control rejected by lethal result snapshot assertion')
