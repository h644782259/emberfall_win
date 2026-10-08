using System;using System.Linq;using UnityEngine;using Emberfall;
public static class WeaponFashionStructureProductionTests
{
 static int n;static void Check(bool ok,string why){n++;if(!ok)throw new Exception(why);}
 public static void Main()
 {
  foreach(HeroClass hero in Enum.GetValues(typeof(HeroClass)))for(int tier=1;tier<=4;tier++)
  {
   var host=new GameObject("weapon fashion");var m=CombatModel.Hero(host.transform,hero);m.ApplyEquipment(new ItemData{id="weapon",slot=ItemSlot.Weapon,level=(tier-1)*25+1,rarity=Rarity.Legendary,upgradeLevel=10},null,null);int common=0;
   foreach(var rarity in new[]{Rarity.Common,Rarity.Rare,Rarity.Epic,Rarity.Legendary})
   {
    m.ApplyFashion(null,new FashionData{id=rarity.ToString(),slot=FashionSlot.Weapon,rarity=rarity});UnityEngine.Object.Flush();var root=m.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Fashion Weapon");var parts=root.GetComponentsInChildren<MeshFilter>(false);
    if(rarity==Rarity.Common)common=parts.Length;else if(rarity==Rarity.Rare)Check(parts.Length>common&&parts.Count(p=>p.transform.name.StartsWith("Rare "))==2,"rare weapon fashion has distinct actual structural pieces beyond common palette");
    Check(parts.All(p=>p.sharedMesh.triangles.Length>0&&p.sharedMesh.vertices.Length>0),"fashion construction uses real nonempty mesh recipes");
    Check(root.parent.name==(hero==HeroClass.Vanguard?"Sword Wrist":hero==HeroClass.Ranger?"Bow":"Staff Wrist"),"rarity structure keeps the actual weapon anchor");
    var vertices=parts.SelectMany(p=>p.sharedMesh.vertices.Select(v=>root.InverseTransformPoint(p.transform.TransformPoint(v)))).ToArray();
    if(hero==HeroClass.Arcanist||hero==HeroClass.Summoner)Check(vertices.All(v=>v.y>.7f),"caster fashion stays above grip and safe lower shaft");
    else if(hero==HeroClass.Vanguard)Check(vertices.All(v=>!(Math.Abs(v.x)<.09f&&v.y<.08f)),"sword fashion preserves grip opening");
    else Check(vertices.All(v=>Math.Abs(v.y)>.25f),"bow fashion preserves grip string hand and arrow-rest opening");
   }
   UnityEngine.Object.Destroy(host);UnityEngine.Object.Flush();Check(!UnityEngine.Object.All.OfType<Material>().Any(v=>!v.destroyed),"fashion swap disposes owned palette");
  }
  foreach(HeroClass hero in Enum.GetValues(typeof(HeroClass)))foreach(bool mobile in new[]{false,true})foreach(bool reduced in new[]{false,true})
  {
   Application.isMobilePlatform=mobile;EffectPreferences.ReducedEffects=reduced;float previous=0;
   foreach(Rarity rarity in Enum.GetValues(typeof(Rarity)))
   {
    var host=new GameObject("wing budget");var model=CombatModel.Hero(host.transform,hero);model.ApplyFashion(new FashionData{id=rarity.ToString(),slot=FashionSlot.Wings,rarity=rarity},null);UnityEngine.Object.Flush();
    var wing=model.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Fashion Wings");var meshes=wing.GetComponentsInChildren<MeshFilter>(false);
    var vertices=meshes.SelectMany(p=>p.sharedMesh.vertices.Select(v=>wing.parent.InverseTransformPoint(p.transform.TransformPoint(v)))).ToArray();float span=vertices.Max(v=>v.x)-vertices.Min(v=>v.x);
    Check(span>previous+.10f,"actual transformed wing span grows at every rarity for each hero");previous=span;
    Check(meshes.Length<=20,"wing mesh budget never exceeds twenty parts");
    var trails=wing.GetComponentsInChildren<TrailRenderer>(true);int expected=reduced||(int)rarity<2?0:rarity==Rarity.Legendary&&!mobile?4:2;
    Check(trails.Length==expected,"mobile/reduced effects bound actual trail count");
    foreach(var trail in trails){Check(trail.startWidth<=.028f&&trail.time<=(mobile?.08f:.14f)&&trail.minVertexDistance>=.15f,"trail spatial and time budgets");var budget=trail.GetComponent<FashionTrailBudget>();GameObject.Call(budget,"Update");Check(trail.emitting,"enabled trail emits");trail.transform.position+=new Vector3(5,0,0);GameObject.Call(budget,"Update");Check(trail.Clears>0,"teleports clear trail history");EffectPreferences.ReducedEffects=true;GameObject.Call(budget,"Update");Check(!trail.emitting,"runtime reduced effects disables existing trail");EffectPreferences.ReducedEffects=reduced;}
    Console.WriteLine($"SPAN {hero} {rarity} mobile={mobile} reduced={reduced}: {span:F3}, parts={meshes.Length}, trails={trails.Length}");
    UnityEngine.Object.Destroy(host);UnityEngine.Object.Flush();Check(!UnityEngine.Object.All.OfType<Material>().Any(v=>!v.destroyed),"wing teardown releases owned palette");
   }
  }
  Console.WriteLine("PASS: "+n+" actual weapon and wing construction/budget checks; static managed geometry only");
 }
}
