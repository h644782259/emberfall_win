using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Emberfall;
public static class EquipmentVisualIdentityProductionTests
{
    static int checks;
    static void C(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
    static string Geometry(Transform root)
    {
        return string.Join(";",root.GetComponentsInChildren<MeshFilter>(false).SelectMany(f=>f.sharedMesh.vertices.Select(v=>root.InverseTransformPoint(f.transform.TransformPoint(v)))).Select(v=>v.x.ToString("R",CultureInfo.InvariantCulture)+","+v.y.ToString("R",CultureInfo.InvariantCulture)+","+v.z.ToString("R",CultureInfo.InvariantCulture)).OrderBy(s=>s));
    }
    public static void Main()
    {
        foreach(HeroClass hero in Enum.GetValues(typeof(HeroClass)))foreach(ItemSlot slot in Enum.GetValues(typeof(ItemSlot)))
        {
            var host=new GameObject("visual identity");var model=CombatModel.Hero(host.transform,hero);var identities=new HashSet<string>();
            for(int band=0;band<=10;band++)foreach(Rarity rarity in Enum.GetValues(typeof(Rarity)))
            {
                var item=new ItemData{id=Guid.NewGuid().ToString("N"),slot=slot,level=Math.Max(1,band*10),rarity=rarity};
                model.ApplyEquipment(slot==ItemSlot.Weapon?item:null,slot==ItemSlot.Armor?item:null,slot==ItemSlot.Relic?item:null);UnityEngine.Object.Flush();
                var root=model.GetComponentsInChildren<Transform>(true).Single(t=>t.name==(slot==ItemSlot.Armor&&hero!=HeroClass.Vanguard?"Equipped class costume":"Equipped "+slot));var look=new EquipmentAppearance(item);
                C(look.LevelBand==band,"actual appearance preserves each ten-level band");
                C(identities.Add(Geometry(root)),"each level/quality changes actual mesh geometry, not only frame color: "+hero+"/"+slot+"/"+band+"/"+rarity);
                var parts=root.GetComponentsInChildren<MeshFilter>(false);
                C(parts.Length<110&&root.GetComponentsInChildren<TrailRenderer>(true).Length==0,"solid gear stays bounded without particles or trails");
                C(root.GetComponentsInChildren<Transform>(true).Count(t=>t.name.StartsWith("Ten-level inlay"))==(band+1)/2,"detail layer count grows monotonically and never exceeds five");
                C(root.GetComponentsInChildren<Transform>(true).Count(t=>t.name=="Legendary crown edge")==((int)rarity==3?2:0),"legendary uses real crown geometry");
                C(item.id!=null&&item.rarity==rarity&&item.level==Math.Max(1,band*10)&&item.upgradeLevel==0,"visual builders never mutate equipment identity or stats");
            }
            UnityEngine.Object.Destroy(host);UnityEngine.Object.Flush();
        }
        foreach(HeroClass hero in Enum.GetValues(typeof(HeroClass)))foreach(FashionSlot slot in Enum.GetValues(typeof(FashionSlot)))
        {
            var host=new GameObject("legacy legendary designs");var model=CombatModel.Hero(host.transform,hero);var identities=new HashSet<string>();
            for(int identity=0;identity<4;identity++)
            {
                var fashion=new FashionData{id="old-design-"+identity,rarity=Rarity.Legendary,appearanceTier=identity,slot=slot};
                model.ApplyFashion(slot==FashionSlot.Wings?fashion:null,slot==FashionSlot.Weapon?fashion:null);UnityEngine.Object.Flush();
                var root=model.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Fashion "+(slot==FashionSlot.Wings?"Wings":"Weapon"));
                C(identities.Add(Geometry(root)),"normalized legendary quality retains four different actual designs");
                C(fashion.id=="old-design-"+identity&&fashion.appearanceTier==identity&&fashion.rarity==Rarity.Legendary,"old ownership and appearance tier remain immutable");
                C(root.GetComponentsInChildren<Transform>(true).Count(t=>t.name==(slot==FashionSlot.Wings?"Legendary flight rim":"Legendary weapon suncrest"))==2,"all preserved designs gain two bounded legendary accents");
                C(slot!=FashionSlot.Wings||root.GetComponentsInChildren<MeshFilter>(false).Length<=20,"legendary wings stay under twenty meshes");
            }
            UnityEngine.Object.Destroy(host);UnityEngine.Object.Flush();
        }
        Console.WriteLine("PASS "+checks+" actual gear level/rarity geometry, bounded fashion accents and legacy design identity assertions; managed model boundary");
    }
}
