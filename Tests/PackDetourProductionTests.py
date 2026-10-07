#!/usr/bin/env python3
"""Actual pack selection plus actual WorldTraversal and actual Forest room geometry.
Only Unity value/object shells and unrelated companion combat services are replaced.
"""
from pathlib import Path
import os,subprocess,sys,tempfile
root=Path(__file__).resolve().parents[1]
def member(text,key):
 a=text.index(key);b=text.index('{',a)+1;depth=1
 while depth:depth+=(text[b]=='{')-(text[b]=='}');b+=1
 return text[a:b]
s=(root/'Assets/Scripts/Combat/SummonedCompanion.cs').read_text()
methods='\n'.join(member(s,k) for k in ['private struct PackPathProbe','private bool ValidTarget(', 'private bool LegalPackTarget(', 'private bool PackCanReach(', 'private EnemyController AcquirePackTarget()'])
node=member((root/'Assets/Scripts/Core/ChapterProgression.cs').read_text(),'public enum ChapterNode')
program=r'''
using System;using System.Collections.Generic;using UnityEngine;
namespace Emberfall {
NODE
public class GameObject{public bool activeInHierarchy=true;}public class Transform{public Vector3 position;}
public class EnemyController{public bool IsDead,IsAggro=true;public GameObject gameObject=new GameObject();public Transform transform=new Transform();}
public class PlayerController{public Transform transform=new Transform();}
public class GameSession{public bool InDungeon=true;public List<EnemyController> Enemies=new List<EnemyController>();}
public class SummonedCompanion {
 public enum Kind{Wolf,Spirit,Treant}private Kind Form=Kind.Wolf;private bool IsAlive=true;
 private PlayerController Owner;private GameSession session;private Transform transform=new Transform();private float NavigationRadius=>.4f;
 private float packTargetHoldUntil,commandTime;private EnemyController target,commandedTarget;
 private static List<SummonedCompanion> active=new List<SummonedCompanion>();
 private readonly Dictionary<EnemyController,PackPathProbe> packPaths=new Dictionary<EnemyController,PackPathProbe>();
 METHODS
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 public static void Run(){
  for(int seed=0;seed<4;seed++) {
   Time.time=0;WorldTraversal.Reset(ZoneKind.Dungeon);ChapterRoomGeometry.Register(ChapterRoomGeometry.Plan(ChapterNode.ForestCourt,0,seed));
   var owner=new PlayerController();owner.transform.position=new Vector3(0,0,-5);
   var game=new GameSession();var enemy=new EnemyController();enemy.transform.position=new Vector3(0,0,5);game.Enemies.Add(enemy);
   var wolf=new SummonedCompanion{Owner=owner,session=game};wolf.transform.position=owner.transform.position;active.Clear();active.Add(wolf);
   Check(!WorldTraversal.HasGroundPath(wolf.transform.position,enemy.transform.position,.12f),"central island blocks the old direct segment");
   Check(WorldTraversal.CanReach(wolf.transform.position,enemy.transform.position,.4f),"actual pathfinder routes around central island");
   wolf.target=wolf.AcquirePackTarget();Check(wolf.target==enemy,"reinforcement admits reachable enemy behind Forest central island");
   float until=wolf.packPaths[enemy].Until;Time.time=.05f;Check(wolf.LegalPackTarget(enemy)&&wolf.packPaths[enemy].Until==until,"unchanged reachable query reuses bounded cache");
   var wall=WorldTraversal.AddDynamicBox(Vector3.zero,new Vector2(50,2));
   Check(!wolf.LegalPackTarget(enemy)&&wolf.AcquirePackTarget()==null,"terrain revision invalidates cached positive and target hold immediately");
   WorldTraversal.RemoveDynamicObstacle(wall);Check(wolf.LegalPackTarget(enemy),"removed wall invalidates cached negative immediately");
   Time.time=.4f;Check(wolf.LegalPackTarget(enemy)&&wolf.packPaths[enemy].Until>.4f,"cache expires after bounded interval");
   game.InDungeon=false;enemy.IsAggro=false;Check(!wolf.LegalPackTarget(enemy),"unengaged wilderness target still excluded");game.InDungeon=true;
   var route=new WorldTraversal.Route();int steps=0;
   while(Vector3.Distance(wolf.transform.position,enemy.transform.position)>1.35f&&steps++<200) {
    Time.time+=.1f;var heading=route.Direction(wolf.transform.position,enemy.transform.position,.4f);
    Check(heading.sqrMagnitude>.01f,"actual route produces detour direction");
    wolf.transform.position=WorldTraversal.Move(wolf.transform.position,heading*.35f,.4f);
   }
   Check(steps<200&&WorldTraversal.HasGroundPath(wolf.transform.position,enemy.transform.position,.12f),"admitted reinforcement can walk actual route into attack contact");
   wolf.transform.position=owner.transform.position;
   for(int i=0;i<70;i++){var other=new EnemyController();other.transform.position=enemy.transform.position;game.Enemies.Add(other);Check(wolf.LegalPackTarget(other),"bounded cache admits repeated legal candidates");Check(wolf.packPaths.Count<=32,"cache never exceeds32 enemy entries");}
  }
  Console.WriteLine("PASS: "+checks+" actual Forest detour admission movement and cache assertions");
 }
}
class Program{static void Main(){SummonedCompanion.Run();}}
}
'''.replace('NODE',node).replace('METHODS',methods)
with tempfile.TemporaryDirectory(prefix='pack-detour-') as directory:
 out=Path(directory);code=out/'Replay.cs';sources=[root/'Assets/Scripts/World/WorldTraversal.cs',root/'Assets/Scripts/World/WorldTraversal.Platforms.cs',root/'Assets/Scripts/World/ChapterRoomGeometry.cs',root/'Tests/DestructibleTraversalTests.cs']
 for p in sources:(out/p.name).write_text(p.read_text())
 project=out/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>')
 config=out/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>');env=dict(os.environ,DOTNET_CLI_HOME=str(out/'cli'),DOTNET_NOLOGO='1');dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
 for mutant in [False,True]:
  code.write_text(program.replace('PackCanReach(enemy);','WorldTraversal.HasGroundPath(transform.position, enemy.transform.position, .12f);') if mutant else program)
  build=subprocess.run([dotnet,'build',str(project),'--configfile',str(config),'-v:q'],env=env,capture_output=True,text=True);assert build.returncode==0,build.stdout+build.stderr
  run=subprocess.run([dotnet,str(out/'bin/Debug/net8.0/Test.dll')],env=env,capture_output=True,text=True)
  if mutant:assert run.returncode!=0 and 'reinforcement admits reachable enemy behind Forest central island' in run.stderr,run.stdout+run.stderr;print('PASS: compiled old direct-segment selection rejects actual Forest detour fixture')
  else:assert run.returncode==0,run.stdout+run.stderr;print(run.stdout,end='')
