"""Real resolved formation/traversal and extracted production voluntary movement; no Unity playback."""
from pathlib import Path
import tempfile,subprocess,os,sys
root=Path(__file__).resolve().parents[1];dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
def member(signature):
 s=(root/'Assets/Scripts/Combat/EnemyController.cs').read_text();a=s.index(signature);i=s.index('{',a)+1;d=1
 while d:d+=(s[i]=='{')-(s[i]=='}');i+=1
 return s[a:i]
fixture='''using System;using System.Collections.Generic;using UnityEngine;using Emberfall;
namespace UnityEngine {public class Transform {public Vector3 position;}}
namespace Emberfall {public sealed class EnemyController {
 public Transform transform=new Transform(); public bool IsDead,isActiveAndEnabled=true;bool aggro;public bool IsAggro=>aggro;public void Provoke(){aggro=true;}bool preparing;float chargeTime,attackAnimation,hurtTime,escapeChaseMovement;Vector3 walkingDisplacement;EscapePostPolicy escapePost;
 float NavigationRadius=>.5f;WorldTraversal.Route route=new WorldTraversal.Route();EnemyController mobileSupplier,mobileFirst,mobileSecond;
 void AnimateModel(float a,float b,bool c){}void ClampPosition(){}
 public void WalkTo(Vector3 target){transform.position=WalkForAnimation(route.Direction(transform.position,target,.5f)*.18f);}
 public void Follow(){RegroupMobileSupport(.1f,3);}
 METHODS
}}
class Program {static int n;static void Check(bool b,string m){n++;if(!b)throw new Exception(m);}
 static void Main(){foreach(int seed in new[]{0,1,2,3,17,64,255,256,int.MinValue,int.MaxValue}){
 var p=ChapterRoomGeometry.Plan(ChapterNode.ForestCourt,0,seed);WorldTraversal.Reset(ZoneKind.Dungeon);ChapterRoomGeometry.Register(p);var placed=new List<Vector3>();var actors=new EnemyController[6];
 for(int i=0;i<6;i++){Vector3 point;Check(ChapterRoomGeometry.TrySpawnAt(p,ChapterRoomGeometry.ForestMobileSpawn(seed,i),placed,.65f,out point),"B six reachable resolved spawns");foreach(var prior in placed)Check(Vector3.Distance(prior,point)>=2.4f,"B real spawn spacing");placed.Add(point);actors[i]=new EnemyController();actors[i].transform.position=point;}
 var supplier=actors[0];var one=actors[2];var two=actors[5];supplier.ConfigureMobileSupport(null,one,two);one.ConfigureMobileSupport(supplier,null,null);two.ConfigureMobileSupport(supplier,null,null);
 foreach(var e in new[]{one,two})Check(Vector3.Distance(e.transform.position,supplier.transform.position)<6&&WorldTraversal.HasLineOfSight(e.transform.position,supplier.transform.position),"B resolved mobile partners actually supported");
 one.Provoke();supplier.Follow();Check(supplier.IsAggro&&two.IsAggro,"B attacking one member engages the actual mobile group");float moved=0;var start=supplier.transform.position;int support=0;
 for(int tick=0;tick<160;tick++){Time.time+=.1f;int mirror=(seed&1)==0?1:-1;var target=new Vector3(-mirror*7,0,-7);one.WalkTo(target);two.WalkTo(target);supplier.Follow();foreach(var e in new[]{one,two})if(Vector3.Distance(e.transform.position,supplier.transform.position)<6&&WorldTraversal.HasLineOfSight(e.transform.position,supplier.transform.position))support++;}
 moved=Vector3.Distance(start,supplier.transform.position);Check(moved>4,"B provider follows engaged melee rather than only supporting spawn");Check(support>=290,"B actual moving positions retain six-metre LOS support");
 supplier.IsDead=true;var priorPos=one.transform.position;one.WalkTo(new Vector3(0,0,-10));Check(Vector3.Distance(priorPos,one.transform.position)>.01f,"dead provider releases tether");
 }Console.WriteLine("PASS: "+n+" resolved formation/live production movement assertions; not rendered gameplay");}}
'''
methods=''.join(member(x) for x in ['internal void ConfigureMobileSupport(','private bool RegroupMobileSupport(','private Vector3 WalkForAnimation(']);fixture=fixture.replace('METHODS',methods)
with tempfile.TemporaryDirectory(prefix='chapter-mobile-') as tmp:
 p=Path(tmp)
 for file in ['Core/ChapterProgression','Core/EscapePostPolicy','Core/RoomTactics','World/WorldTraversal','World/WorldTraversal.Platforms','World/ChapterRoomGeometry']:(p/(Path(file).name+'.cs')).write_text((root/'Assets/Scripts'/(file+'.cs')).read_text())
 for file in ['ChapterGeometryFixture','DestructibleTraversalTests']:
  s=(root/'Tests'/(file+'.cs')).read_text().replace('public static float time=0;','public static float time=0,deltaTime=.1f;');(p/(file+'.cs')).write_text(s)
 (p/'Host.cs').write_text(fixture)
 project=p/'Tests.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NuGetAudit>false</NuGetAudit><NoWarn>0649;0414</NoWarn></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1')
 subprocess.run([dotnet,'restore',str(project),'--configfile',str(p/'NuGet.Config'),'-v:q'],env=env,check=True)
 cmd=[dotnet,'run','--project',str(project),'--no-restore'];subprocess.run(cmd,env=env,check=True)
 original=(p/'Host.cs').read_text();(p/'Host.cs').write_text(original.replace('supplier.Follow();foreach','foreach'))
 subprocess.run([dotnet,'build',str(project),'--no-restore','-v:q'],env=env,check=True,stdout=subprocess.DEVNULL)
 result=subprocess.run(cmd+['--no-build'],env=env,capture_output=True,text=True)
 if result.returncode==0 or 'B provider follows engaged melee rather than only supporting spawn' not in result.stdout+result.stderr:raise AssertionError(result.stdout+result.stderr)
 print('PASS: compiled stationary-provider control rejected at actual movement assertion')
