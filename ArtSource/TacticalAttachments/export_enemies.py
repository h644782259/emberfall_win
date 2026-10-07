"""Real CombatModel.Enemy + TacticalAttachmentArt + committed buffers; managed TRS, not Unity.
python3 export_enemies.py repo output dotnet
"""
from pathlib import Path
import sys,subprocess,os,re,json
root=Path(sys.argv[1]).resolve();out=Path(sys.argv[2]).resolve();dotnet=sys.argv[3]
subprocess.run([sys.executable,str(root/'ArtSource/ActorModules/export_constructions.py'),str(root),str(out),dotnet],check=True)
p=out/'export';source=(root/'Assets/Scripts/Combat/CombatModel.cs').read_text()
def extract(signature):
 a=source.index(signature);b=source.index('{',a)+1;depth=1
 while depth:depth+=(source[b]=='{')-(source[b]=='}');b+=1
 return source[a:b]
with (p/'Model.cs').open('a') as f:f.write('\nnamespace Emberfall {public sealed partial class CombatModel {private bool slime;'+''.join(extract(x) for x in ['public static CombatModel Enemy(','private void BuildEnemy(','private void EnhanceEnemy('])+'}}')
with (p/'Types.cs').open('a') as f:f.write('namespace Emberfall{'+re.search(r'public enum EnemyKind\s*\{[^}]*\}',(root/'Assets/Scripts/Core/GameTypes.cs').read_text()).group(0)+'}')
f=p/'Fixture.cs';s=f.read_text().replace('public class Component:Object {','public class Component:Object {public new string name=>gameObject.name;').replace('public class Shader {','public class Shader {public bool isSupported=true;').replace('public class Mesh:Object {','public struct Bounds{public Vector3 size,center;}public class Mesh:Object {public Bounds bounds;').replace('public void RecalculateBounds(){}','public void RecalculateBounds(){var lo=new Vector3(vertices.Min(v=>v.x),vertices.Min(v=>v.y),vertices.Min(v=>v.z));var hi=new Vector3(vertices.Max(v=>v.x),vertices.Max(v=>v.y),vertices.Max(v=>v.z));bounds=new Bounds{size=hi-lo,center=(hi+lo)*.5f};}').replace('public static float Exp(float a)', 'public static float Max(float a,float b)=>System.Math.Max(a,b);public static float Exp(float a)');f.write_text(s)
(p/'EnemySilhouetteArt.cs').write_text((root/'Assets/Scripts/Combat/EnemySilhouetteArt.cs').read_text())
(p/'TacticalAttachmentArt.cs').write_text((root/'Assets/Scripts/Combat/TacticalAttachmentArt.cs').read_text())
(p/'Exporter.cs').write_text('''using System;using System.Linq;using System.Collections.Generic;using System.Text.Json;using Emberfall;using UnityEngine;
class Exporter {static void Check(bool ok,string why){if(!ok)throw new Exception(why);}static void Main(){var cases=new List<object>();int count=0;
foreach(var kind in new[]{EnemyKind.Slime,EnemyKind.Wisp,EnemyKind.Goblin,EnemyKind.Guardian})for(int big=0;big<(kind==EnemyKind.Guardian?2:1);big++){
 var host=new GameObject("enemy");var enemy=host.AddComponent<EnemyController>();var model=CombatModel.Enemy(host.transform,kind,big==1);UnityEngine.Object.Flush();var fittings=TacticalAttachmentArt.Create(enemy);
 for(int mask=0;mask<8;mask++){
 for(int slot=0;slot<3;slot++)Check(fittings.Set(slot,(mask&(1<<slot))!=0),"actual enemy accepts mapped fitting");
 var parts=host.GetComponentsInChildren<MeshFilter>(false).Where(x=>x.sharedMesh?.vertices!=null&&x.GetComponent<Renderer>().enabled).ToArray();
 Check(parts.Count(x=>x.transform.name.StartsWith("Tactical attachment / "))==Enumerable.Range(0,3).Count(i=>(mask&(1<<i))!=0),"exact combination has no extra marker");
 if(kind==EnemyKind.Guardian&&(mask&2)!=0){var badge=parts.Single(x=>x.transform.name.EndsWith("HuntBadge"));var shell=parts.Where(x=>x.transform.name=="Guardian chest plate"||x.transform.name=="Guardian ember crystal");float front=shell.Max(x=>x.sharedMesh.vertices.Max(v=>x.transform.TransformPoint(v).z));float back=badge.sharedMesh.vertices.Min(v=>badge.transform.TransformPoint(v).z);Check(back>front+.001f,"guardian badge clears actual outer armour and raised crystal");if(mask==2)Console.WriteLine("CLEARANCE "+kind+" boss="+(big==1)+" badgeBack="+back+" outerShellFront="+front+" worldGap="+(back-front));}
 if((kind==EnemyKind.Slime||kind==EnemyKind.Wisp)&&(mask&2)!=0){var badge=parts.Single(x=>x.transform.name.EndsWith("HuntBadge"));var eyes=parts.Where(x=>x.transform.name=="Eye White"||x.transform.name=="Spirit Eyes");float faceBottom=eyes.Min(x=>x.sharedMesh.vertices.Min(v=>x.transform.TransformPoint(v).y));float badgeTop=badge.sharedMesh.vertices.Max(v=>badge.transform.TransformPoint(v).y);Check(badgeTop<faceBottom,"small enemy badge preserves real eye identity");if(mask==2)Console.WriteLine("FACE "+kind+" badgeTop="+badgeTop+" eyeBottom="+faceBottom);}
 if((kind==EnemyKind.Goblin||kind==EnemyKind.Guardian)&&(mask&4)!=0){var ward=parts.Single(x=>x.transform.name.EndsWith("SupportMantle"));var shoulders=parts.Where(x=>x.transform.name=="Pauldrons"||x.transform.name=="Guardian layered pauldron");float shoulderTop=shoulders.Max(x=>x.sharedMesh.vertices.Max(v=>x.transform.TransformPoint(v).y));float wardTop=ward.sharedMesh.vertices.Max(v=>ward.transform.TransformPoint(v).y);Check(wardTop>shoulderTop+.02f,"humanoid support fin clears actual shoulder silhouette");if(mask==4)Console.WriteLine("WARD "+kind+" boss="+(big==1)+" finTop="+wardTop+" shoulderTop="+shoulderTop);}
 var geometry=parts.Select(x=>new{name=x.transform.name,mesh=x.sharedMesh.name,color=new[]{x.GetComponent<Renderer>().sharedMaterial.color.r,x.GetComponent<Renderer>().sharedMaterial.color.g,x.GetComponent<Renderer>().sharedMaterial.color.b},vertices=x.sharedMesh.vertices.Select(v=>{var w=x.transform.TransformPoint(v);return new[]{w.x,w.y,w.z};}).ToArray(),triangles=x.sharedMesh.triangles}).ToArray();cases.Add(new{name=kind.ToString()+(big==1?"Boss":""),mask=mask,parts=geometry});count++;
 }
 fittings.Hide();Check(host.GetComponentsInChildren<MeshRenderer>(false).Where(x=>x.transform.name.StartsWith("Tactical attachment / ")).All(x=>!x.enabled),"all real-rig fitting renderers hide");UnityEngine.Object.Destroy(host);UnityEngine.Object.Flush();}
 System.IO.File.WriteAllText("OUTPUT",JsonSerializer.Serialize(cases));Console.WriteLine("PASS actual Enemy factory + real tactical adapter + decoded bytes: "+count+" enemy/state compositions, guardian outer-shell clearance, unique slot count and hide. Managed, NOT Unity.");}}
'''.replace('OUTPUT',(out/'enemies.json').as_posix()))
command=[dotnet,'run','--project',str(p/'Export.csproj')];env=dict(os.environ,DOTNET_CLI_HOME=str(out/'cli'))
subprocess.run(command,env=env,check=True)
f=p/'TacticalAttachmentArt.cs';good=f.read_text();bad=good.replace('front+bounds.size.z*.035f-back','bounds.center.z+bounds.size.z*.5f+bounds.size.z*.035f-back');assert bad!=good;f.write_text(bad)
r=subprocess.run(command,env=env,capture_output=True,text=True);f.write_text(good)
assert r.returncode and 'guardian badge clears actual outer armour' in r.stdout+r.stderr,r.stdout+r.stderr
print('PASS compiled torso-only socket regression rejected against real guardian geometry')

bad=good.replace('bounds.size.y*.35f','0');assert bad!=good;f.write_text(bad)
r=subprocess.run(command,env=env,capture_output=True,text=True);f.write_text(good)
assert r.returncode and 'humanoid support fin clears actual shoulder silhouette' in r.stdout+r.stderr,r.stdout+r.stderr
print('PASS compiled buried support-fin socket regression rejected against real shoulder geometry')
