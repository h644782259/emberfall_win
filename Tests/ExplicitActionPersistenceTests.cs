using System;using System.IO;using System.Collections.Generic;using Emberfall;using UnityEngine;
public static class ExplicitActionPersistenceTests
{
 private static int checks;
 private static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 private static string State(ProgressionService p){return JsonUtility.ToJson(p.Profile,true);}
 public static string Run(string directory)
 {
  checks=0;string root=Path.Combine(directory,"explicit-actions-"+Guid.NewGuid().ToString("N"));
  Action<string,Action<ProgressionService>,Func<ProgressionService,bool>> test=(name,prepare,action)=>
  {
   var p=new ProgressionService(Path.Combine(root,name));Check(p.CreateNewSlot(HeroClass.Arcanist),name+" fixture");
   p.Profile.level=50;p.Profile.skillRanks[0]=1;p.Profile.skillRanks[1]=1;p.Profile.potions=4;
   prepare(p);p.Save();Check(string.IsNullOrEmpty(p.LastError),name+" baseline durable");
   string before=State(p),primary=File.ReadAllText(p.SaveFilePath),backup=File.Exists(p.SaveFilePath+".bak")?File.ReadAllText(p.SaveFilePath+".bak"):null;
   int changed=0;p.Changed+=()=>changed++;
   Directory.CreateDirectory(p.SaveFilePath+".tmp");
   Check(!action(p)&&!string.IsNullOrEmpty(p.LastError),name+" reports failed commit");
   Check(State(p)==before&&changed==0,name+" leaves live identity and state unchanged");
   Check(File.ReadAllText(p.SaveFilePath)==primary&&(!File.Exists(p.SaveFilePath+".bak")?null:File.ReadAllText(p.SaveFilePath+".bak"))==backup,name+" leaves both durable documents intact");
   Directory.Delete(p.SaveFilePath+".tmp");
   Check(action(p)&&string.IsNullOrEmpty(p.LastError)&&changed==1,name+" retries once and emits one event after commit");
   string expected=State(p);Check(p.LoadSlot(p.CurrentSlotId)&&State(p)==expected,name+" reloads exact committed state");
  };
  test("learn",p=>{},p=>p.LearnSkill(0));
  test("specialization",p=>{},p=>p.SetSpecialization(ElementalistSpecialization.Burn,true));
  test("lock",p=>{p.Profile.inventory[0].locked=false;},p=>p.SetItemLocked(p.Profile.inventory[0].id,true));
  test("unlock",p=>{p.Profile.inventory[0].locked=true;},p=>p.SetItemLocked(p.Profile.inventory[0].id,false));
  test("pending-lock",p=>{var item=p.CreateMechanicItem(EquipmentMechanic.FrostEcho);p.Profile.pendingLoot.Add(item);},p=>p.SetItemLocked(p.Profile.inventory.Find(x=>x.mechanic==EquipmentMechanic.FrostEcho).id,false));
  test("disable-legacy-autosell",p=>{p.Profile.autoSellCommon=true;},p=>p.SetAutoSell(Rarity.Common,false));
  test("potion",p=>{},p=>p.UsePotion());
  test("assign-skill",p=>{},p=>p.AssignSkill(5,0));
  test("assign-potion",p=>{},p=>p.AssignConsumable(5));
  test("move-hotbar",p=>{p.Profile.equippedSkills[0]=0;p.Profile.equippedSkills[1]=1;},p=>p.MoveHotbarSkill(0,1));
  test("hotbar-key",p=>{},p=>p.SetHotbarKey(0,113));
  test("legacy-page",p=>{},p=>p.SetHotbarPage(1));
  Action<ProgressionService> fashion=p=>{p.Profile.fashions.Add(new FashionData{id="fashion-0-3",slot=FashionSlot.Wings,rarity=Rarity.Legendary,name="Fixture"});};
  test("fashion-equip",fashion,p=>p.EquipFashion("fashion-0-3"));
  test("fashion-unequip",p=>{fashion(p);p.Profile.wingsFashionId="fashion-0-3";},p=>p.UnequipFashion(FashionSlot.Wings));
  var names=new HashSet<string>();
  foreach(HeroClass hero in Enum.GetValues(typeof(HeroClass)))
  {
   var p=new ProgressionService(Path.Combine(root,"legacy-fashion-"+hero));Check(p.CreateNewSlot(hero),"legacy fashion profile created");p.Profile.fashions.Clear();
   foreach(FashionSlot slot in Enum.GetValues(typeof(FashionSlot)))foreach(Rarity rarity in Enum.GetValues(typeof(Rarity)))p.Profile.fashions.Add(new FashionData{id="fashion-"+(int)slot+"-"+(int)rarity,slot=slot,rarity=rarity,name="旧兵装"});
   p.Profile.wingsFashionId="fashion-0-3";p.Profile.weaponFashionId="fashion-1-3";var before=p.GetStats();p.Save();Check(p.Load(),"legacy fashion reload");
   Check(p.Profile.fashions.Count==8&&p.Profile.wingsFashionId=="fashion-0-3"&&p.Profile.weaponFashionId=="fashion-1-3","migration preserves ownership and both equipped IDs");
   foreach(var f in p.Profile.fashions){Check(f.id=="fashion-"+(int)f.slot+"-"+(int)f.rarity&&f.name==ProgressionService.FashionName(f.slot,f.rarity,hero)&&!f.name.Contains("兵装"),"display rename retains canonical identity");if(f.slot==FashionSlot.Weapon)Check(names.Add(f.name),"all sixteen class weapon fashion names are distinct");}
   var after=p.GetStats();Check(before.Damage==after.Damage&&before.MaxHealth==after.MaxHealth&&before.Armor==after.Armor,"cosmetic migration preserves combat stats");
  }
  return "PASS: "+checks+" explicit-action rollback/retry/durable-state assertions in14 fake fixtures";
 }
}
