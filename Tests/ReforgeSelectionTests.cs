using System;using System.IO;using UnityEngine;
namespace Emberfall
{
 public sealed partial class GameUI
 {
  public static string VerifyReforgeSelection(string root)
  {
   int n=0;Action<bool,string> check=(ok,why)=>{n++;if(!ok)throw new Exception(why);};
   var p=new ProgressionService(Path.Combine(root,"selection"));check(p.CreateNewSlot(HeroClass.Arcanist),"create actual character");
   p.Profile.level=10;p.Profile.gold=500;p.Save();var gear=p.CreateMechanicItem(EquipmentMechanic.FrostEcho);gear.mechanicVariantUnlocked=true;p.Profile.variantKnowledge.Add(EquipmentMechanic.FrostEcho);gear.mechanicVariant=1;
   check(p.CollectLoot(gear)&&p.Equip(gear.id),"collect equip mechanic");p.Profile.level=50;p.Profile.slotUpgradeRanks[(int)gear.slot]=4;p.Save();
   var choices=p.ReforgeChoices(gear.id);check(choices.Length==3,"three distinct actual choices");
   check(choices[0].Quote.TargetLevel==15&&choices[0].Quote.GoldCost==230,"plus5 uses real fixed230 quote");
   check(choices[1].Quote.TargetLevel==19&&choices[1].Quote.GoldCost==450,"500 gold affordable target must be19 cost450");
   check(choices[2].Quote.TargetLevel==50&&choices[2].Quote.GoldCost==3240&&p.ReforgeLockReason(choices[2].Quote,true).Length>0,"catchup50 disabled at3240 gold");
   string initial=JsonUtility.ToJson(p.Profile,true);string initialDisk=File.ReadAllText(p.SaveFilePath);int events=0;p.Changed+=()=>events++;
   var preview=p.PreviewReforge(choices[1].Quote);check(preview.level==19&&preview.id==gear.id&&preview.mechanicVariant==1&&preview.upgradeLevel==4,"preview keeps real identity variant slot upgrade");
   check(initial==JsonUtility.ToJson(p.Profile,true)&&initialDisk==File.ReadAllText(p.SaveFilePath)&&events==0,"choices and preview never mutate persistence");
   var ui=new GameUI{session=new Context{Progression=p}};ui.OpenReforgeSurface(gear.id);check(ui.reforgeSelected.TargetLevel==15,"desktop mobile shared open defaults plus5");
   ui.clickReforgeOption=2;ui.DrawReforgeSurface();check(ui.reforgeSelected.TargetLevel==50,"catchup may be inspected without buying");ui.clickReforgeExecute=true;ui.DrawReforgeSurface();check(events==0&&p.Profile.gold==500,"unaffordable execution button disabled");ui.click=null;ui.clickReforgeExecute=false;
   ui.clickReforgeOption=1;ui.DrawReforgeSurface();check(ui.reforgeSelected.TargetLevel==19,"actual choice button selects affordable quote");
   ui.shown.Clear();ui.DrawReforgeSurface();check(ui.shown.Exists(x=>x.Contains("执行后余金 50")&&x.Contains("攻击")&&x.Contains("防御")&&x.Contains("生命")),"actual preview displays stats and remaining gold");
   ui.click="追踪此目标";ui.DrawReforgeSurface();check(p.Profile.progressionGoalLevel==19&&p.SelectedProgressionGoal(true).GoldCost==450,"actual track button freezes chosen19 not player50");
   p.Profile.level=60;p.Save();check(p.SelectedProgressionGoal(true).ReforgeQuote.TargetLevel==19,"levelup never raises tracked target");
   string before=JsonUtility.ToJson(p.Profile,true),disk=File.ReadAllText(p.SaveFilePath);int beforeEvents=events;
   Directory.CreateDirectory(p.SaveFilePath+".tmp");check(!ui.ExecuteReforgeSelection(false)&&before==JsonUtility.ToJson(p.Profile,true)&&disk==File.ReadAllText(p.SaveFilePath)&&events==beforeEvents&&ui.reforgeSelected.TargetLevel==19,"save failure keeps selected quote money item and retry surface");Directory.Delete(p.SaveFilePath+".tmp");
   var quote=ui.reforgeSelected;ui.clickReforgeExecute=true;ui.DrawReforgeSurface();var actual=p.Profile.inventory.Find(x=>x.id==gear.id);
   check(actual.level==19&&p.Profile.gold==50&&actual.attack==preview.attack&&actual.defense==preview.defense&&actual.health==preview.health,"UI commit matches actual preview and charges450 once");
   check(actual.id==gear.id&&actual.mechanicVariant==1&&actual.upgradeLevel==4&&ui.reforgeOwner==null,"successful commit preserves identity and closes stale surface");
   before=JsonUtility.ToJson(p.Profile,true);check(!p.ReforgeMechanic(quote,true)&&before==JsonUtility.ToJson(p.Profile,true),"stale old quote rejected without charge");
   var other=new ProgressionService(Path.Combine(root,"other"));other.CreateNewSlot(HeroClass.Arcanist);check(!other.ReforgeMechanic(quote,true),"cross save quote rejected");
   ui.OpenReforgeSurface(gear.id);check(ui.CloseProgressionGoalSurface()&&ui.reforgeOwner==null,"existing Back gate closes only nested reforge");
   ui.OpenReforgeSurface(gear.id);ui.panel=Panel.Inventory;ui.ReconcileProgressionGoalSurface();check(ui.reforgeOwner==null,"panel change drops nested quote");ui.panel=Panel.Camp;
   ui.OpenReforgeSurface(gear.id);ui.session.Progression=other;ui.ReconcileProgressionGoalSurface();check(ui.reforgeOwner==null,"owner switch drops quote");ui.session.Progression=p;
   ui.OpenReforgeSurface(gear.id);ui.session.IsInCamp=false;check(!ui.ExecuteReforgeSelection(false)&&ui.reforgeOwner==null,"field cannot consume captured camp action");ui.session.IsInCamp=true;
   p.Profile.level=22;p.Profile.gold=500;p.Save();choices=p.ReforgeChoices(gear.id);check(choices.Length==1&&choices[0].Quote.TargetLevel==22,"clamped plus5 affordable and catchup deduplicate");
   check(ProgressionService.ReforgeGoldCost(10,15)+ProgressionService.ReforgeGoldCost(15,19)==450,"segmented exact price unchanged");
   check(!p.SelectProgressionGoal(ProgressionGoalKind.Reforge,gear.id,0,23),"service refuses beyond player explicit target");
   ui.OpenReforgeSurface(gear.id);check(p.CreateNewSlot(HeroClass.Arcanist),"switch character within same service");ui.ReconcileProgressionGoalSurface();check(ui.reforgeOwner==null,"same service different slot clears captured quote");
   return "PASS: "+n+" real reforge choice preview UI target and atomic-save assertions";
  }
 }
}
