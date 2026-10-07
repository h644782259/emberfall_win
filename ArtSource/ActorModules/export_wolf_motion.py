"""Actual Companion factory + unmodified quadruped Animate branch, managed TRS only.
python3 export_wolf_motion.py repo scratch dotnet
"""
from pathlib import Path
import sys,subprocess,os
root=Path(sys.argv[1]).resolve();out=Path(sys.argv[2]).resolve();dotnet=sys.argv[3]
subprocess.run([sys.executable,str(root/'ArtSource/ActorModules/export_constructions.py'),str(root),str(out),dotnet],check=True)
p=out/'export';source=(root/'Assets/Scripts/Combat/CombatModel.cs').read_text()
a=source.index('else if (quadruped)',source.index('public void Animate('));a=source.index('{',a)+1;b=a;depth=1
while depth:depth+=(source[b]=='{')-(source[b]=='}');b+=1
branch=source[a:b-1]
with (p/'Model.cs').open('a') as f:f.write('\nnamespace Emberfall { public sealed partial class CombatModel { public void ReplayWolf(float gaitPhase,float speed,float attack){smoothedSpeed=speed;float stride=Mathf.Sin(gaitPhase);'+branch+'}}}')
f=p/'Fixture.cs';s=f.read_text().replace('public static float Exp(float a)', 'public static float Max(float a,float b)=>System.Math.Max(a,b);public static float Exp(float a)');f.write_text(s)
(p/'Exporter.cs').write_text('''using System;using System.Linq;using System.Collections.Generic;using System.Text.Json;using Emberfall;using UnityEngine;
class Exporter {
static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
static void Main(){var host=new GameObject("wolf");host.transform.position=new Vector3(3,0,7);var model=CombatModel.Companion(host.transform,SummonedCompanion.Kind.Wolf);var parts=model.GetComponentsInChildren<MeshFilter>(false);var saved=parts.Select(p=>p.sharedMesh).ToArray();
Check(parts.Count(p=>p.sharedMesh.name=="Blender actor Paw")==2&&parts.Count(p=>p.sharedMesh.name=="Blender actor WolfHindLeg")==2,"actual front and hind limb modules");
Check(parts.Where(p=>p.transform.name=="Ear").All(p=>p.sharedMesh.name=="Blender actor WolfEar"),"both tapered ear sockets");Check(parts.Single(p=>p.transform.name=="Muzzle").transform.parent.name=="Wolf bite hinge","muzzle retains bite pivot");
Check(AuthoredActorMeshes.Key("Ear",PrimitiveType.Cube)==null&&AuthoredActorMeshes.Key("Muzzle",PrimitiveType.Cube)==null&&AuthoredActorMeshes.Key("Tail",PrimitiveType.Capsule)==null&&AuthoredActorMeshes.Key("Paw",PrimitiveType.Capsule)==null,"no generic other-animal dispatch");
var cases=new List<object>();var jaw=parts.Single(p=>p.transform.name=="Muzzle");var initial=jaw.transform.TransformPoint(jaw.sharedMesh.vertices[0]);bool moved=false,hingeMoved=false;
for(int step=0;step<49;step++){float attack=step/48f;model.ReplayWolf(step*.13f,1,attack);Check(host.transform.position.x==3&&host.transform.position.z==7,"cosmetic pose never moves navigation owner");for(int k=0;k<parts.Length;k++){Check(object.ReferenceEquals(saved[k],parts[k].sharedMesh),"pose does not replace shared geometry");foreach(var v in parts[k].sharedMesh.vertices){var q=parts[k].transform.TransformPoint(v);Check(!float.IsNaN(q.x)&&!float.IsNaN(q.y)&&!float.IsNaN(q.z),"finite moving mesh");}}
var tip=jaw.transform.TransformPoint(jaw.sharedMesh.vertices[0]);moved|=Math.Abs(tip.y-initial.y)>.02f;hingeMoved|=!jaw.transform.parent.localRotation.Equals(Quaternion.identity);
if(step==0||step==12||step==24){var geometry=parts.Select(x=>new{name=x.transform.name,mesh=x.sharedMesh.name,vertices=x.sharedMesh.vertices.Select(v=>{var w=x.transform.TransformPoint(v)-host.transform.position;return new[]{w.x,w.y,w.z};}).ToArray(),triangles=x.sharedMesh.triangles}).ToArray();cases.Add(new{name="Wolf-pose-"+step,parts=geometry});}}
Check(moved&&hingeMoved,"jaw geometry follows live animated hinge");System.IO.File.WriteAllText("OUTPUT",JsonSerializer.Serialize(cases));Console.WriteLine("PASS actual wolf factory/decoded buffers plus exact quadruped Animate branch: 49 poses, fore/hind dispatch, original bite hinge, finite vertices, stable shared meshes, navigation owner unchanged. Managed TRS, NOT Unity.");}}
'''.replace('OUTPUT',(out/'wolf-motion.json').as_posix()))
subprocess.run([dotnet,'run','--project',str(p/'Export.csproj')],env=dict(os.environ,DOTNET_CLI_HOME=str(out/'cli')),check=True)

model=p/'Model.cs';good=model.read_text();mutant=good.replace('Quaternion.Euler(-Mathf.Sin(attack*Mathf.PI)*26f,0,0)','Quaternion.identity');assert mutant!=good;model.write_text(mutant)
result=subprocess.run([dotnet,'run','--project',str(p/'Export.csproj')],env=dict(os.environ,DOTNET_CLI_HOME=str(out/'cli')),capture_output=True,text=True)
model.write_text(good)
assert result.returncode and 'jaw geometry follows live animated hinge' in result.stdout+result.stderr,result.stdout+result.stderr
print('PASS compiled frozen bite hinge negative control rejected')
