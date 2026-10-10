from pathlib import Path
import subprocess, tempfile, os, sys
root=Path(__file__).resolve().parents[1]; workspace=tempfile.TemporaryDirectory(prefix='sight-perf-');out=Path(workspace.name)
s=(root/'Assets/Scripts/World/WorldTraversal.cs').read_text(); old=s.replace('            if(ignoreWater&&OpenSightBounds(from,to,radius))return true;\n','')
a=old.index('            // Strictly disjoint bounds');b=old.index('            Vector3 center=',a);old=old[:a]+old[b:]

(out/'Current.cs').write_text(s)
(out/'Baseline.cs').write_text(old.replace('WorldTraversal','BaselineTraversal'))
for part in (root/'Assets/Scripts/World').glob('WorldTraversal.*.cs'):
 (out/part.name).write_text(part.read_text())
 (out/('Baseline'+part.name)).write_text(part.read_text().replace('WorldTraversal','BaselineTraversal'))
(out/'Stubs.cs').write_text((root/'Tests/DestructibleTraversalTests.cs').read_text())
(out/'Program.cs').write_text('''using System;using System.Diagnostics;using Emberfall;using UnityEngine;
class Program{
static void Main(){var r=new Random(41);int checks=0;for(int scene=0;scene<25;scene++){
WorldTraversal.Reset(ZoneKind.Dungeon);BaselineTraversal.Reset(ZoneKind.Dungeon);
for(int i=0;i<35;i++){var p=Point(r);if(i%2==0){WorldTraversal.AddCircle(p,.3f+i%3);BaselineTraversal.AddCircle(p,.3f+i%3);}else{var size=new Vector2(.2f+i%4,1.1f);WorldTraversal.AddBox(p,size);BaselineTraversal.AddBox(p,size);}}
for(int i=0;i<4000;i++){var a=Point(r);var b=Point(r);var c=Point(r);float radius=(float)r.NextDouble();
Check(WorldTraversal.HasLineOfSight(a,b)==BaselineTraversal.HasLineOfSight(a,b));
Check(WorldTraversal.HasClearVisualFootprint(a,b,radius)==BaselineTraversal.HasClearVisualFootprint(a,b,radius));
Check(WorldTraversal.HasClearVisualTriangle(a,b,c,a)==BaselineTraversal.HasClearVisualTriangle(a,b,c,a));checks+=3;}}
WorldTraversal.Reset(ZoneKind.Dungeon);BaselineTraversal.Reset(ZoneKind.Dungeon);
for(int i=0;i<40;i++){var p=new Vector3(10+i%5,0,10+i/5);WorldTraversal.AddBox(p,new Vector2(.5f,.5f));BaselineTraversal.AddBox(p,new Vector2(.5f,.5f));}
var from=new Vector3(-5,0,-5);var to=new Vector3(4,0,4);var w=Stopwatch.StartNew();for(int i=0;i<20000;i++)BaselineTraversal.HasLineOfSight(from,to);w.Stop();double before=w.Elapsed.TotalMilliseconds;w.Restart();for(int i=0;i<20000;i++)WorldTraversal.HasLineOfSight(from,to);w.Stop();Console.WriteLine("PASS "+checks+" matching geometry queries; open-field CPU benchmark: "+before.ToString("F1")+" ms -> "+w.Elapsed.TotalMilliseconds.ToString("F1")+" ms (managed, not game FPS)");}
static Vector3 Point(Random r)=>new Vector3((float)r.NextDouble()*40-20,r.Next(3)==0?1:0,(float)r.NextDouble()*40-20);
static void Check(bool b){if(!b)throw new Exception("geometry changed");}}
''')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
(out/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>')
subprocess.run([sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet'),'run','--project',str(out/'Test.csproj'),'-c','Release'],check=True)
