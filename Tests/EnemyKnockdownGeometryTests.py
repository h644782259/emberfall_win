#!/usr/bin/env python3
"""Export actual enemy factories and production knockdown poses for offline Blender review.
Managed mesh/TRS sampling, not Unity rendering or gameplay collision validation.
"""
import os
import json
import argparse
import sys
from pathlib import Path
import subprocess
import tempfile
root=Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('dotnet',nargs='?',default=os.environ.get('DOTNET','dotnet'))
parser.add_argument('--output',type=Path,default=Path(tempfile.gettempdir())/'enemy-knockdown-geometry.json')
args=parser.parse_args()
dotnet=args.dotnet
output=args.output
if output.suffix.lower()!='.json':parser.error('--output must name a .json file')
def member(source,signature):
    start=source.index(signature);end=source.index('{',start)+1;depth=1
    while depth:
        depth+=(source[end]=='{')-(source[end]=='}');end+=1
    return source[start:end]
source=(root/'Assets/Scripts/Combat/CombatModel.cs').read_text();status=(root/'Assets/Scripts/Combat/EnemyStatusEffects.cs').read_text()
f=(root/'Tests/EquipmentCompositionProductionTests.Fixture.cs').read_text().split('public static class EquipmentCompositionProductionTests')[0]
f=f.replace('public class EnemyController:MonoBehaviour{}','')
f=f.replace('public static float deltaTime=.016f;', 'public static float deltaTime=.016f,time;public static int frameCount;')
f=f.replace('public static Quaternion identity=>', 'public static Quaternion Slerp(Quaternion a,Quaternion b,float t)=>new Quaternion{q=System.Numerics.Quaternion.Slerp(a.q,b.q,t)};public static float Angle(Quaternion a,Quaternion b)=>0;public Vector3 Rotate(Vector3 v)=>this*v;public static Quaternion identity=>')
f=f.replace('public Vector3 position{', 'public Vector3 InverseTransformDirection(Vector3 v)=>Quaternion.Inverse(rotation)*v;public void Rotate(float x,float y,float z,Space s){localRotation=localRotation*Quaternion.Euler(x,y,z);}public Vector3 position{')
f=f.replace('public class MonoBehaviour:Component {}', 'public class MonoBehaviour:Component {public T GetComponentInChildren<T>()where T:Component=>GetComponentsInChildren<T>(false).FirstOrDefault();}')
f=f.replace('public enum PrimitiveType{', 'public enum Space{Self}public enum PrimitiveType{')
f=f.replace('public static class Mathf{', 'public static class Mathf{public static float Exp(float v)=>(float)Math.Exp(v);public static float Max(float a,float b)=>Math.Max(a,b);public static float Min(float a,float b)=>Math.Min(a,b);public static float Lerp(float a,float b,float t)=>a+(b-a)*Clamp01(t);public static float Clamp01(float a)=>Math.Max(0,Math.Min(1,a));public static float SmoothStep(float a,float b,float t){t=Clamp01(t);return a+(b-a)*t*t*(3-2*t);}public static float MoveTowards(float a,float b,float d)=>Math.Abs(b-a)<=d?b:a+Math.Sign(b-a)*d;')
f+='''\nnamespace Emberfall{public enum HeroClass{Vanguard,Ranger,Arcanist,Summoner}public enum Rarity{Common}public class GameSession{public static GameSession Instance;public bool HasStarted,InputBlocked;} }\n'''
f+='namespace UnityEngine{public class TextAsset:Object{public byte[] bytes;}public static class Resources{public static T Load<T>(string name)where T:class{var p=System.IO.Path.Combine(@"'+str(root/'Assets/Resources')+'",name+".bytes");return System.IO.File.Exists(p)?new TextAsset{bytes=System.IO.File.ReadAllBytes(p)} as T:null;}}}'
with tempfile.TemporaryDirectory(prefix='enemy-knockdown-geometry-') as directory:
    p=Path(directory)
    for name in ['ActorSilhouetteF1','CombatModel.Knockdown','EnemySilhouetteArt','AuthoredActorMeshes','ProceduralVisuals','VisualMeshRecipes']:(p/(name+'.cs')).write_text((root/'Assets/Scripts/Combat'/(name+'.cs')).read_text())
    (p/'LocomotionPoseState.cs').write_text((root/'Assets/Scripts/Core/LocomotionPoseState.cs').read_text())
    (p/'ThreatAdmissionPolicy.cs').write_text((root/'Assets/Scripts/Core/ThreatAdmissionPolicy.cs').read_text())
    # Use the same animation field fixture; Setup's synthetic rig is not used by this factory exporter.
    (p/'AnimationFields.cs').write_text((root/'Tests/EnemyKnockdownProductionTests.cs').read_text().split('public static class EnemyKnockdownProductionTests')[0])
    methods=['public static CombatModel Enemy(','private static CombatModel Create(','private Material Mat(','private Transform Part(','private Transform Joint(','private void Humanoid(','private static Transform NewJoint(','private static void RemovePart(','private Transform ArticulateArm(','private Transform ArticulateLeg(','private Transform MeshPart(','private Transform Tapered(','private void BuildEnemy(','private void EnhanceEnemy(','public void Animate(','public void Recoil(','private void ApplyRecoil(']
    fields=source[source.index('        private struct SurfaceKey'):source.index('        private TailoredCloth')]
    (p/'Factory.cs').write_text('using UnityEngine;using System.Collections.Generic;namespace Emberfall{public sealed partial class CombatModel{'+fields+''.join(member(source,m) for m in methods)+'}'+member(source,'internal sealed class OwnedCombatMesh')+'public partial class EnemyStatusEffects{'+member(status,'private void LateUpdate(')+'}}')
    (p/'Fixture.cs').write_text(f)
    (p/'Program.cs').write_text('''using System;using System.Linq;using System.Collections.Generic;using System.Text.Json;using UnityEngine;using Emberfall;
class Export{static float Floor(CombatModel model)=>model.GetComponentsInChildren<MeshFilter>(false).Where(m=>m.sharedMesh!=null).SelectMany(m=>m.sharedMesh.vertices.Select(v=>m.transform.TransformPoint(v).y)).Min();static void Main(string[] args){var cases=new List<object>();foreach(var kind in new[]{EnemyKind.Goblin,EnemyKind.Guardian}){var host=new GameObject("enemy");var owner=host.AddComponent<EnemyController>();owner.Kind=kind;owner.StatusEffects=host.AddComponent<EnemyStatusEffects>();var model=CombatModel.Enemy(host.transform,kind,false);UnityEngine.Object.Flush();foreach(var stage in new[]{"upright","fall","down","rise","standing"}){int frames=stage=="down"?20:stage=="standing"?30:stage=="upright"?1:3;float left=stage=="upright"||stage=="standing"?0:stage=="rise"?.14f:1;for(int i=0;i<frames;i++){Time.frameCount++;Time.time+=.016f;model.Animate(0,0,false);model.TryAnimateKnockdown(left,false,.016f);}var parts=model.GetComponentsInChildren<MeshFilter>(false).Where(m=>m.sharedMesh!=null).Select(m=>new{name=m.transform.name,vertices=m.sharedMesh.vertices.Select(v=>{var w=m.transform.TransformPoint(v);return new[]{w.x,w.y,w.z};}).ToArray(),triangles=m.sharedMesh.triangles,color=new[]{m.GetComponent<Renderer>().sharedMaterial.color.r,m.GetComponent<Renderer>().sharedMaterial.color.g,m.GetComponent<Renderer>().sharedMaterial.color.b}}).ToArray();float minimum=parts.SelectMany(p=>p.vertices).Min(v=>v[1]);Console.WriteLine("FACTORY kind="+kind+" stage="+stage+" parts="+parts.Length+" minY="+minimum);cases.Add(new{name=kind+" "+stage,parts});}float lowest=100;int samples=0;foreach(float dt in new[]{1f/120,1f/60,1f/30,.1f})foreach(float duration in new[]{.08f,.3f,.8f,1.5f}){for(int i=0;i<30;i++){Time.frameCount++;model.Animate(0,0,false);model.TryAnimateKnockdown(0,false,.016f);}for(float age=0;age<duration+.4f;age+=dt){Time.frameCount++;Time.deltaTime=dt;model.Animate(0,0,false);model.TryAnimateKnockdown(Math.Max(0,duration-age),false,dt);float floor=Floor(model);lowest=Math.Min(lowest,floor);samples++;if(floor<-.015f)throw new Exception("actual factory pose penetrates floor kind="+kind+" duration="+duration+" dt="+dt+" age="+age+" minY="+floor);}}Console.WriteLine("PASS actual factory floor kind="+kind+" samples="+samples+" lowest="+lowest);}System.IO.File.WriteAllText(args[0],JsonSerializer.Serialize(cases));}}
''')
    project=p/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><NoWarn>0649;0169;0414</NoWarn></PropertyGroup></Project>')
    (p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
    command=[dotnet,'run','--project',str(project),'--',str(output)]
    env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1')
    subprocess.run(command,env=env,check=True)
    guardian=next(case for case in json.loads(output.read_text()) if case['name']=='Guardian down')
    minimum=lambda part:min(v[1] for v in part['vertices'])
    feet=[minimum(part) for part in guardian['parts'] if part['name']=='Boot']
    shoulders=[minimum(part) for part in guardian['parts'] if part['name']=='Guardian layered pauldron']
    hammer=next(minimum(part) for part in guardian['parts'] if part['name']=='Great Hammer')
    assert len(feet)==2 and all(-.005<=y<=.025 for y in feet), ('both actual boots must support the down pose',feet)
    assert -.005<=min(shoulders)<=.025, ('body armor support must contact floor independently of weapon',shoulders)
    assert hammer>.20, ('hammer is not the claimed body support',hammer)
    print('PASS Guardian down independent support: boot minima='+str(feet)+' body-shoulder minimum='+str(min(shoulders))+' hammer minimum='+str(hammer),flush=True)
    overlay=p/'CombatModel.Knockdown.cs';original=overlay.read_text()
    assert 'new Vector3(.06f,.30f,-.06f)' in original
    overlay.write_text(original.replace('new Vector3(.06f,.30f,-.06f)','new Vector3(.06f,0,-.06f)'))
    failure=subprocess.run(command,env=env,capture_output=True,text=True)
    if failure.returncode==0 or 'actual factory pose penetrates floor' not in failure.stdout+failure.stderr:
        raise AssertionError('Missing ground compensation mutation was not caught: '+failure.stdout+failure.stderr)
    print('PASS: compiled no-ground-compensation mutation fails actual factory vertex floor assertion')
