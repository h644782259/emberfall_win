#!/usr/bin/env python3
"""Measure managed navigation allocations, not Unity frames/engine CPU or gameplay.
Actual companion pack selection/cache and Route/FindPath run with value/object shims.
"""
import os,subprocess,sys,tempfile,json
from pathlib import Path
root=Path(__file__).resolve().parents[1]
dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet')
def member(text,key):
 a=text.index(key);b=text.index('{',a)+1;depth=1
 while depth:depth+=(text[b]=='{')-(text[b]=='}');b+=1
 return text[a:b]
s=(root/'Assets/Scripts/Combat/SummonedCompanion.cs').read_text()
methods='\n'.join(member(s,k) for k in ['private struct PackPathProbe','private bool ValidTarget(', 'private bool LegalPackTarget(', 'private bool PackCanReach(', 'private EnemyController AcquirePackTarget()'])
program=r'''
using System;using System.Collections.Generic;using System.Text.Json;using UnityEngine;
namespace Emberfall {
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
 private WorldTraversal.Route route=new WorldTraversal.Route();
 METHODS
 static double Step(GameSession game,int tick,bool blocked,int[] trace=null,int offset=0){
  Time.time+=1f/60;
  // Controlled moving query endpoints isolate the hot algorithms; not simulated combat.
  for(int i=0;i<game.Enemies.Count;i++)game.Enemies[i].transform.position=new Vector3(-1+i*.4f+(float)Math.Sin(tick*.03)*.2f,0,blocked?6:-4);
  double sum=0;
  for(int i=0;i<active.Count;i++){
   var pet=active[i];pet.transform.position=new Vector3(-.6f+i*.4f+(float)Math.Sin(tick*.025)*.2f,0,blocked?-6:-8);
   pet.target=pet.AcquirePackTarget();if(pet.target==null)throw new Exception("query fixture lost target");
   var direction=pet.route.Direction(pet.transform.position,pet.target.transform.position,pet.NavigationRadius);
   int chosen=game.Enemies.IndexOf(pet.target);sum+=direction.x+direction.z*7+chosen*31;
   if(trace!=null){int at=offset+i*4;trace[at]=chosen;trace[at+1]=BitConverter.SingleToInt32Bits(direction.x);trace[at+2]=BitConverter.SingleToInt32Bits(direction.y);trace[at+3]=BitConverter.SingleToInt32Bits(direction.z);}
  }
  return sum;
 }
 public static object Measure(bool blocked,int count){
  WorldTraversal.Reset(ZoneKind.Dungeon);if(blocked)WorldTraversal.AddCircle(Vector3.zero,3.4f);
  Time.time=0;active.Clear();var game=new GameSession();var owner=new PlayerController();owner.transform.position=new Vector3(0,0,-5);
  for(int i=0;i<6;i++)game.Enemies.Add(new EnemyController());
  for(int i=0;i<count;i++)active.Add(new SummonedCompanion{Owner=owner,session=game});
  for(int tick=0;tick<180;tick++)Step(game,tick,blocked);
  var trace=new int[240*count*4];
  long before=GC.GetAllocatedBytesForCurrentThread();double checksum=0;
  for(int tick=180;tick<420;tick++)checksum+=Step(game,tick,blocked,trace,(tick-180)*count*4);
  long bytes=GC.GetAllocatedBytesForCurrentThread()-before;
  var traceBytes=new byte[trace.Length*4];Buffer.BlockCopy(trace,0,traceBytes,0,traceBytes.Length);
  string traceSha256=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(traceBytes));
  return new{traceSha256,scenario=blocked?"blocked-moving":"open-moving",companions=count,enemies=6,queryTicks=240,bytes,bytesPerTick=bytes/240d,checksum};
 }
 static void Main(){var rows=new List<object>();foreach(bool blocked in new[]{false,true})foreach(int count in new[]{1,4})rows.Add(Measure(blocked,count));Console.WriteLine(JsonSerializer.Serialize(rows));}
}}
'''.replace('METHODS',methods)
with tempfile.TemporaryDirectory(prefix='companion-path-alloc-') as directory:
 out=Path(directory)
 for p in [root/'Assets/Scripts/World/WorldTraversal.cs',root/'Assets/Scripts/World/WorldTraversal.Platforms.cs',root/'Tests/DestructibleTraversalTests.cs']:(out/p.name).write_text(p.read_text())
 (out/'Program.cs').write_text(program)
 project=out/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NoWarn>0649</NoWarn><NuGetAudit>false</NuGetAudit></PropertyGroup></Project>')
 config=out/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(out/'cli'),DOTNET_NOLOGO='1',DOTNET_PROCESSOR_COUNT='2')
 navigation=out/'WorldTraversal.cs';current=navigation.read_text()
 x='        private static readonly int[] NeighborX = { -1, 0, 1, -1, 1, -1, 0, 1 };'
 y='        private static readonly int[] NeighborY = { -1, -1, -1, 0, 0, 1, 1, 1 };'
 assert x in current and y in current
 # The arrays are private and only indexed reads occur outside their declarations.
 assert current.count('NeighborX')==3 and current.count('NeighborY')==3
 legacy=current.replace(x,'').replace(y,'').replace('NeighborX','dx').replace('NeighborY','dy')
 anchor='            var queue = new Heap(); cost[start] = 0; queue.Add(start, 0);'
 assert anchor in legacy
 legacy=legacy.replace(anchor,anchor+'\n            int[] dx = { -1, 0, 1, -1, 1, -1, 0, 1 }, dy = { -1, -1, -1, 0, 0, 1, 1, 1 };')
 results={}
 for label,code in [('current',current),('legacy',legacy)]:
  navigation.write_text(code)
  subprocess.run([dotnet,'build',str(project),'--configfile',str(config),'-c','Release','-v:q'],env=env,check=True)
  run=subprocess.run([dotnet,str(out/'bin/Release/net8.0/Test.dll')],env=env,capture_output=True,text=True,check=True)
  results[label]=json.loads(run.stdout)
 print(json.dumps(results,indent=2))
 for current,legacy in zip(results['current'],results['legacy']):
  assert current['traceSha256']==legacy['traceSha256'],'Target/direction bit traces changed'
  assert current['checksum']==legacy['checksum'],'Query outputs changed'
  if current['scenario']=='open-moving':
   assert current['bytes']==legacy['bytes']==0,'Clear path introduced managed allocations'
  else:
   saved=legacy['bytes']-current['bytes']
   assert saved>0 and saved%112==0,'Expected removal of two eight-int arrays per blocked search'
   print(f"PASS: {current['companions']} wolves saved {saved} B across {saved//112} searches ({saved/legacy['bytes']:.3%}); exact target/direction trace unchanged")
 print('PASS: four warmed production-algorithm scenarios match compiled legacy target/direction traces; open queries allocate zero')
