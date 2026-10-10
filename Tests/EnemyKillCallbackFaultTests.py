"""Real complete OnEnemyKilled + actual ProgressionService; engine/delivery recipients are recorders."""
from pathlib import Path
import os,sys,tempfile,subprocess
root=Path(__file__).resolve().parents[1];dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
def member(s,key):
 a=s.index(key);b=s.index('{',a)+1;depth=1
 while depth:depth+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
method=member((root/'Assets/Scripts/Core/GameSession.cs').read_text(),'public void OnEnemyKilled(')
fixture='''using System;using System.Collections;using System.Collections.Generic;using UnityEngine;using Random=UnityEngine.Random;
namespace UnityEngine{
 public struct Vector3{public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}public static Vector3 up=>new Vector3(0,1,0);public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator *(Vector3 a,float n)=>new Vector3(a.x*n,a.y*n,a.z*n);}
 public class Transform{public Vector3 position;}public class GameObject{}
 public static class Mathf{public static int Max(int a,int b)=>Math.Max(a,b);public static float Clamp(float a,float min,float max)=>Math.Max(min,Math.Min(max,a));public static int RoundToInt(float a)=>(int)Math.Round(a);}
 public static class Random{public static float value=>0;public static int Range(int a,int b)=>a;}
}
namespace Emberfall{
 public class PracticeRecordStub{public bool HasSupplier;public void Defeat(string name,bool supplier,bool last){}}
 public class EnemyController{public enum ThreatTier{Normal,Elite} public ThreatTier Tier;public Transform transform=new Transform();public GameObject gameObject=new GameObject();public bool IsBoss;public EnemyKind Kind;public int DeathCalls;public void BeginDeath(){DeathCalls++;}}
 public sealed partial class GameSession{
 public bool PracticeActive,HasStarted=true,CombatEnded,ChapterActive,InDungeon=true,changingZone,DungeonCleared;public PracticeRecordStub PracticeRecord=new PracticeRecordStub();
 public ProgressionService Progression;public List<EnemyController> Enemies=new List<EnemyController>();public List<GameObject> transientObjects=new List<GameObject>();
 public object ModeRun,RoomChainRun,waveRoutine;public int DungeonTier=1,wavePopulation=6,DungeonEntryLevel=1,runEnemyExperience,runPickupGold,RunPickupPotions;public Queue<int> reinforcementQueue=new Queue<int>();
 public int ExpeditionRecorded,ArenaRecorded,RoomRecorded,LootDelivered,ChapterFinalized,RoomFinalized,ArenaFinalized,WavesScheduled;
 bool RecordChapterDefeat(EnemyController e,out int xp){xp=112;return true;}
 void OnExpeditionEnemyKilled(EnemyController e){ExpeditionRecorded++;}void RecordArenaDefeat(EnemyController e){ArenaRecorded++;}void RecordRoomDefeat(EnemyController e){RoomRecorded++;}
 void LogSystem(string s){}void SpawnFloatingText(Vector3 p,string s,Color c){}
 void DeliverEnemyLoot(ItemData item,Vector3 at){if(item==null||string.IsNullOrEmpty(item.id))throw new Exception("real RollLoot missing identity");LootDelivered++;Progression.RecordDungeonLoot(item);}
 void FinalizeChapterBoss(){ChapterFinalized++;}void FinalizeRoomChain(){RoomFinalized++;}void FinalizeArenaResult(){ArenaFinalized++;}
 void TrySpawnReinforcements(){}IEnumerator NextWave(){yield break;}object StartCoroutine(IEnumerator next){WavesScheduled++;return new object();}
'''+method+'}}'
core=['CombatImpactBatch','SafeSaveFlow','GameTypes','ProgressionService','ProgressionService.Attachments','ProgressionService.AutomaticGrowth','ProgressionService.Reforge','ReforgeQuote','ProgressionService.Chapter','ChapterProgression','RoomTactics','CombatBalance','HubTravelRules','MasteryCoreRuntime','CastFirstHitReceipt','TierRewardRules','TierRewardBand','ProgressionGoalState','AdventureResultPolicy']
with tempfile.TemporaryDirectory(prefix='enemy-kill-callback-fault-') as tmp:
 p=Path(tmp)
 core+=['ProgressionService.Smith','ProgressionService.Trading']
 for name in core:(p/(name+'.cs')).write_text((root/'Assets/Scripts/Core'/(name+'.cs')).read_text())
 for name in ['ProgressionTests','EnemyKillCallbackFaultTests']:(p/(name+'.cs')).write_text((root/'Tests'/(name+'.cs')).read_text())
 (p/'Host.cs').write_text(fixture);(p/'Program.cs').write_text('System.Console.WriteLine(EnemyKillCallbackFaultTests.Run(args[0]));')
 (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>')
 (p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1')
 subprocess.run([dotnet,'run','--project',str(p/'Test.csproj'),'--',str(p/'saves')],env=env,check=True)
