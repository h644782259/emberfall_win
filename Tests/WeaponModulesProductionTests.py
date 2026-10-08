#!/usr/bin/env python3
"""Production Hero/equipment/fashion/anchors plus actual weapon resource loader; numerical TRS."""
from pathlib import Path
import sys,os,subprocess,tempfile,re,shutil
owned_output=len(sys.argv)<=2
root=Path(__file__).resolve().parents[1];dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet';out=Path(sys.argv[2]).resolve() if len(sys.argv)>2 else Path(tempfile.mkdtemp(prefix='weapon-modules-'));out.mkdir(exist_ok=True,parents=True)
source=(root/'ArtSource/ActorModules/export_constructions.py').read_text().split('subprocess.run(')[0];sys.argv=['export',str(root),str(out),dotnet];ns={'__file__':str(root/'ArtSource/ActorModules/export_constructions.py')};exec(source,ns);p=out/'export'
for name in ['WeaponModules','CombatModel.WeaponArt']:(p/(name+'.cs')).write_text((root/'Assets/Scripts/Combat'/(name+'.cs')).read_text())
b=p/'OptionalPilotBoundary.cs';s=b.read_text().replace('private void ApplyWeaponArt(ItemData weapon) {}','').replace('private void ApplyWeaponFashionArt() {}','');s=s.replace('private static bool PilotStarterCompatible(ItemData item,ItemSlot slot) { return false; }',ns['ns']['extract']((root/'Assets/Scripts/Combat/CombatModel.BlenderPilot.cs').read_text(),'private static bool PilotStarterCompatible('));b.write_text(s)
f=p/'Fixture.cs';s=f.read_text().replace('public static class Resources {','public static class Resources {public static string Missing,Corrupt;').replace('var path=System.IO.Path.Combine','if(name==Missing)return null;if(name==Corrupt)return new TextAsset{bytes=new byte[16]} as T;var path=System.IO.Path.Combine');game=(root/'Assets/Scripts/Core/GameTypes.cs').read_text();colors=re.search(r'public static readonly Color\[\] ClassColors = \{.*?\};',game,re.S).group(0)
start=s.index('public static class GameBalance {');end=s.index('}\n',start)+1
s=s[:start]+'public static class GameBalance {'+colors+ns['ns']['extract'](game,'public static Color ClassColor(')+ns['ns']['extract'](game,'public static Color RarityColor(')+'}'+s[end:];f.write_text(s)
(p/'Exporter.cs').write_text(r'''using System;using System.Linq;using System.Collections.Generic;using System.Reflection;using System.Text.Json;using UnityEngine;using Emberfall;
class Exporter {
 static int checks;static void C(bool ok,string label){checks++;if(!ok)throw new Exception(label);}
 static Transform Rig(CombatModel m)=>m.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Sword Wrist"||t.name=="Staff Wrist"||t.name=="Bow");
 static MeshFilter[] Visible(Transform t)=>t.GetComponentsInChildren<MeshFilter>(false).Where(f=>f.GetComponent<Renderer>()!=null&&f.GetComponent<Renderer>().enabled&&f.sharedMesh!=null).ToArray();
 static ItemData Gear(int h,int tier)=>new ItemData{id="weapon-"+tier,name=h==0&&tier==1?"初行长剑":"matrix tier weapon",slot=ItemSlot.Weapon,level=tier==1?1:76,rarity=tier==1?Rarity.Common:Rarity.Legendary,upgradeLevel=tier==1?0:10};
 static void Main(){var cases=new List<object>();var budgets=new List<object>();
 for(int h=0;h<4;h++)foreach(int tier in new[]{1,4})for(int fashion=0;fashion<2;fashion++){
  Vector3[] beforeAnchors=null;string[] beforeFashion=null;int beforeRenderers=0,beforeMats=0;
  for(int version=0;version<2;version++){
   WeaponModules.Enabled=version==1;var host=new GameObject("matrix");var m=CombatModel.Hero(host.transform,(HeroClass)h);var gear=Gear(h,tier);m.ApplyEquipment(gear,new ItemData{id="armor",slot=ItemSlot.Armor,level=tier==1?1:76,rarity=tier==1?Rarity.Common:Rarity.Legendary,upgradeLevel=tier==1?0:10},null);
   if(fashion==1)m.ApplyFashion(new FashionData{id="fashion-0-3",slot=FashionSlot.Wings,rarity=Rarity.Legendary},new FashionData{id="fashion-1-3",slot=FashionSlot.Weapon,rarity=Rarity.Legendary});ObjectFlush();
   var rig=Rig(m);var weapon=Visible(rig);var all=Visible(m.transform);int tris=weapon.Sum(f=>f.sharedMesh.triangles.Length/3);
   var anchors=Enum.GetValues(typeof(WeaponVisualAnchor)).Cast<WeaponVisualAnchor>().Select(a=>{Vector3 v;m.TryGetWeaponVisualAnchor(a,out v);return v;}).ToArray();var fashionNames=weapon.Where(f=>f.transform.name.StartsWith("Fashion")).Select(f=>f.transform.name).ToArray();
   if(version==0){beforeAnchors=anchors;beforeFashion=fashionNames;beforeRenderers=all.Length;beforeMats=all.Select(f=>f.GetComponent<Renderer>().sharedMaterial).Distinct().Count();}
   else{C(anchors.Zip(beforeAnchors,(a,b)=>(a-b).magnitude).All(d=>d<.00001f),"all weapon sockets unchanged");C(fashionNames.SequenceEqual(beforeFashion),"highest existing fashion part identity preserved");C(all.Length==beforeRenderers&&all.Select(f=>f.GetComponent<Renderer>().sharedMaterial).Distinct().Count()==beforeMats,"zero new renderer/material");C(tris<=800,"visible weapon set <=800 triangles: "+h+"/"+tier+"/"+fashion+"="+tris);
    if(h==0&&tier==1)C(weapon.Count(f=>f.sharedMesh.name.StartsWith("Blender weapon Star"))==4,"exact starter loads all four independent source parts");
    if(h==0&&tier==4)C(!weapon.Any(f=>f.sharedMesh.name.StartsWith("Blender weapon Star")),"T4 sword preserves tier blade/guard/forging identity");
    if(h==0){var blade=weapon.Single(f=>f.transform.name=="Tiered blade");var vertices=blade.sharedMesh.vertices.Select(v=>rig.InverseTransformPoint(blade.transform.TransformPoint(v))).ToArray();var structure=new WeaponStructure(tier);C(Math.Abs(vertices.Min(v=>v.y)-structure.SwordRoot)<.00001&&Math.Abs(vertices.Max(v=>v.y)-structure.SwordTip)<.00001,"blade exact root and tip agree with ribbon endpoints");}
    if(h==2){var line=m.GetComponentsInChildren<LineRenderer>(false).Single(l=>l.transform.name=="Drawn Bowstring");C(Math.Abs(line.points[0].y-new WeaponStructure(tier).BowReach)<.00001&&Math.Abs(line.points[2].y+new WeaponStructure(tier).BowReach)<.00001,"bow string endpoint reach retained");}
   }
   budgets.Add(new{hero=h,tier,fashion,version,weapon_triangles=tris,weapon_parts=weapon.Length});
   var parts=all.Select(f=>new{name=f.transform.name,mesh=f.sharedMesh.name,color=new[]{f.GetComponent<Renderer>().sharedMaterial.color.r,f.GetComponent<Renderer>().sharedMaterial.color.g,f.GetComponent<Renderer>().sharedMaterial.color.b},vertices=f.sharedMesh.vertices.Select(v=>{var w=f.transform.TransformPoint(v);return new[]{w.x,w.y,w.z};}).ToArray(),triangles=f.sharedMesh.triangles}).ToArray();
   var local=weapon.Select(f=>new{name=f.transform.name,mesh=f.sharedMesh.name,color=new[]{f.GetComponent<Renderer>().sharedMaterial.color.r,f.GetComponent<Renderer>().sharedMaterial.color.g,f.GetComponent<Renderer>().sharedMaterial.color.b},vertices=f.sharedMesh.vertices.Select(v=>{var w=rig.InverseTransformPoint(f.transform.TransformPoint(v));return new[]{w.x,w.y,w.z};}).ToArray(),triangles=f.sharedMesh.triangles}).ToArray();
   var lines=m.GetComponentsInChildren<LineRenderer>(false).Where(l=>l.enabled).Select(l=>new{width=l.startWidth,color=new[]{l.sharedMaterial.color.r,l.sharedMaterial.color.g,l.sharedMaterial.color.b},points=l.points.Select(v=>{var w=rig.InverseTransformPoint(l.transform.TransformPoint(v));return new[]{w.x,w.y,w.z};}).ToArray()}).ToArray();cases.Add(new{hero=h,tier,fashion,version,parts,weapon=local,lines});UnityEngine.Object.Destroy(host);ObjectFlush();
  }
 }
 for(int h=0;h<4;h++){WeaponModules.Enabled=true;var maxModel=CombatModel.Hero(new GameObject("maximum detail budget").transform,(HeroClass)h);var maxGear=Gear(h,4);maxGear.level=100;maxModel.ApplyEquipment(maxGear,null,null);maxModel.ApplyFashion(null,new FashionData{id="fashion-1-3",slot=FashionSlot.Weapon,rarity=Rarity.Legendary});ObjectFlush();C(Visible(Rig(maxModel)).Sum(f=>f.sharedMesh.triangles.Length/3)<=800,"level 100 highest fashion retains total 800-triangle weapon cap");UnityEngine.Object.Destroy(maxModel.gameObject);ObjectFlush();}
 WeaponModules.Enabled=true;var baseModel=CombatModel.Hero(new GameObject("base").transform,HeroClass.Vanguard);C(Visible(Rig(baseModel)).Count(f=>f.sharedMesh.name.StartsWith("Blender weapon Star"))==4,"base path loads independent source parts");var tipBefore=Rig(baseModel).TransformPoint(new Vector3(0,1.3f,0));baseModel.ApplyEquipment(Gear(0,1),null,null);baseModel.ApplyEquipment(null,null,null);C(Visible(Rig(baseModel)).Count(f=>f.sharedMesh.name.StartsWith("Blender weapon Star"))==4,"unequip restores authored base without duplicate sword");
 baseModel.ApplyFashion(null,new FashionData{id="fashion-1-3",slot=FashionSlot.Weapon,rarity=Rarity.Legendary});C(Visible(Rig(baseModel)).Sum(f=>f.sharedMesh.triangles.Length/3)<=800,"base sword plus highest existing fashion stays within cap");
 foreach(var missing in new[]{"StarBlade","StarGuard","StarGrip","StarPommel"}){Resources.Missing="WeaponModules/"+missing;typeof(WeaponModules).GetMethod("Reset",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);baseModel.ApplyEquipment(null,null,null);C(!Visible(Rig(baseModel)).Any(f=>f.sharedMesh.name.StartsWith("Blender weapon Star")),"each missing part atomically falls back complete sword");}Resources.Missing=null;
 Resources.Corrupt="WeaponModules/StarGuard";typeof(WeaponModules).GetMethod("Reset",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);baseModel.ApplyEquipment(null,null,null);C(!Visible(Rig(baseModel)).Any(f=>f.sharedMesh.name.StartsWith("Blender weapon Star")),"malformed source part falls back complete sword");Resources.Corrupt=null;
 WeaponModules.Enabled=false;baseModel.ApplyEquipment(null,null,null);C(!Visible(Rig(baseModel)).Any(f=>f.sharedMesh.name.StartsWith("Blender weapon")),"runtime disable restores original visible weapon");
 System.IO.File.WriteAllText(@"GEOMETRY",JsonSerializer.Serialize(cases));System.IO.File.WriteAllText(@"BUDGET",JsonSerializer.Serialize(budgets));Console.WriteLine("PASS "+checks+" actual factory/loader/16-combination before-after/anchors/fallback assertions; managed TRS, not Unity");
 }
 static void ObjectFlush()=>UnityEngine.Object.Flush();
}'''.replace('GEOMETRY',str(out/'geometry.json')).replace('BUDGET',str(out/'budgets.json')))
subprocess.run([dotnet,'run','--project',str(p/'Export.csproj')],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(out/'cli')))

original=(p/'CombatModel.WeaponArt.cs').read_text()
for label,before,after in [('wrong-tip','weaponStructure.SwordTip-weaponStructure.SwordRoot','1f'),('skip-base','if(swordRig!=null)','if(false)')]:
 (p/'CombatModel.WeaponArt.cs').write_text(original.replace(before,after,1));r=subprocess.run([dotnet,'run','--project',str(p/'Export.csproj')],env=dict(os.environ,DOTNET_CLI_HOME=str(out/'cli')),capture_output=True,text=True)
 assert r.returncode!=0 and 'Unhandled exception. System.Exception' in r.stderr,(label,r.stdout,r.stderr)
 print('PASS compiled weapon negative control:',label)
(p/'CombatModel.WeaponArt.cs').write_text(original)

if owned_output:shutil.rmtree(out)
