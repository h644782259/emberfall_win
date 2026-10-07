using System;
using System.IO;
using Emberfall;
public static class BuildDraftVariantEffectsTests
{
 static int n;static void Check(bool value,string message){n++;if(!value)throw new Exception(message);}
 static ProgressionService Fresh(string root,HeroClass hero,EquipmentMechanic mechanic,out string id)
 {
  var p=new ProgressionService(Path.Combine(root,Guid.NewGuid().ToString("N")));Check(p.CreateNewSlot(hero),"create real slot");p.Profile.level=50;p.Profile.mechanicMaterials=40;p.Profile.skillRanks[0]=2;p.Profile.skillRanks[1]=2;p.Save();
  var item=p.CreateMechanicItem(mechanic);id=item.id;Check(p.CollectLoot(item)&&p.Equip(id),"collect and equip real class mechanism");return p;
 }
 static string Change(ProgressionService p,int skill,int from,int to)
 {
  p.Profile.skillRanks[skill]=from;p.Save();string state=UnityEngine.JsonUtility.ToJson(p.Profile,true),disk=File.ReadAllText(p.SaveFilePath);var d=p.BeginBuildDraft(true);Check(d.ChangeSkill(skill,to-from),"actual legal draft rank transition");
  string text=d.SkillChangeEffects(skill);Check(text.Contains(p.SkillEffectSummary(skill,from)),"before uses actual equipped current mechanism");Check(text.Contains(GameBalance.EffectiveCooldown(p.Profile.heroClass,skill,from).ToString("0.##")+" → "+GameBalance.EffectiveCooldown(p.Profile.heroClass,skill,to).ToString("0.##")),"authoritative cooldown preserved");Check(text.Contains(GameBalance.SkillEnergyCost(p.Profile.heroClass,skill).ToString("0.##")+"（不变）"),"authoritative energy preserved");
  Check(state==UnityEngine.JsonUtility.ToJson(p.Profile,true)&&disk==File.ReadAllText(p.SaveFilePath),"preview never mutates live profile or save");d.Cancel();return text;
 }
 public static string Run(string root)
 {
  string id;var venom=Fresh(root,HeroClass.Ranger,EquipmentMechanic.VenomSpread,out id);
  Check(venom.ToggleMechanicVariant(id,true),"unlock actual worn B transaction");
  Check(venom.Equipped(ItemSlot.Relic).mechanicVariantUnlocked&&venom.Equipped(ItemSlot.Relic).mechanicVariant==1,"worn unlocked B precondition");
  foreach(var ranks in new[]{new[]{2,3},new[]{3,2}})
  {
   string text=Change(venom,0,ranks[0],ranks[1]);
   Check(text.Contains("原 "+ranks[0]+"阶："+GameBalance.SkillName(HeroClass.Ranger,0)+" · "+BuildCatalog.VenomModifier(true))&&text.Contains("新 "+ranks[1]+"阶："+GameBalance.SkillName(HeroClass.Ranger,0)+" · "+BuildCatalog.VenomModifier(true)),"B both before and after replace fan descriptions");
   Check(text.Contains("360%攻击")&&text.Contains("480%攻击")&&!text.Contains("支箭矢")&&!text.Contains("穿透箭")&&!text.Contains("爆裂"),"B 2-3 actual coefficients and no obsolete fan/piercing/explosion upgrade");
   Check(text.Contains("普通三毒引爆，独立且一次")&&text.Contains("首个实际拦截目标"),"B preserves single poison detonation and collision constraint");
  }
  for(int rank=1;rank<=3;rank++){Check(BuildCatalog.VenomSkillOverride(venom.Profile,0,rank)==venom.SkillEffectSummary(0,rank)&&BuildCatalog.ConcentratedVenomEquipped(venom.Profile),"equipped skill and draft descriptions share identity and budget");Check(venom.SkillEffectSummary(0,rank).Contains((100*ConcentratedVenomRules.DirectCoefficient(rank)).ToString("0.##")+"%攻击"),"summary agrees with actual projectile coefficient rule");}
  Check(venom.ToggleMechanicVariant(id,true),"switch back to real A");string a=Change(venom,0,2,3);Check(a.Contains(GameBalance.SkillEvolution(HeroClass.Ranger,0,3))&&a.Contains("变体A：")&&!a.Contains("收束毒矢"),"A keeps fan advancement and propagation only");
  var item=venom.Equipped(ItemSlot.Relic);item.mechanicVariant=1;item.mechanicVariantUnlocked=false;var attachment=venom.Attachment(EquipmentMechanic.VenomSpread);attachment.variant=1;attachment.variantUnlocked=false;Check(!venom.SkillEffectSummary(0,3).Contains("收束毒矢"),"locked forged B cannot override preview");item.mechanicVariantUnlocked=true;attachment.variantUnlocked=true;venom.Profile.relicId=null;Check(venom.SkillEffectSummary(0,3).Contains("收束毒矢"),"independent B persists after equipment removal");attachment.mounted=false;Check(venom.SkillEffectSummary(0,3)==GameBalance.SkillEvolution(HeroClass.Ranger,0,3),"explicitly unmounted B has no effect");venom.Profile.relicId=id;venom.Profile.heroClass=HeroClass.Arcanist;Check(!venom.SkillEffectSummary(0,3).Contains("收束毒矢"),"wrong class worn mechanism does not override");
  foreach(var mechanic in new[]{EquipmentMechanic.FrostEcho,EquipmentMechanic.CinderTrail})
  {
   var p=Fresh(root,HeroClass.Arcanist,mechanic,out id);int skill=mechanic==EquipmentMechanic.FrostEcho?0:1;
   foreach(bool b in new[]{false,true})
   {
    if(b)Check(p.ToggleMechanicVariant(id,true),"unlock real caster B");string text=Change(p,skill,2,3);
    if(mechanic==EquipmentMechanic.FrostEcho)Check(text.Contains("当前霜回 "+(b?"B":"A"))&&text.Contains("首击倍率 ×"+BuildCatalog.FrostEchoOpeningMultiplier(b).ToString("0.##"))&&text.Contains("回响范围倍率 ×"+BuildCatalog.FrostEchoRadiusMultiplier(b).ToString("0.##")),"frost variant preview matches combat modifiers");
    else Check(text.Contains("当前余烬 "+(b?"B":"A"))&&text.Contains("火场半径倍率 ×"+BuildCatalog.CinderTrailRadiusMultiplier(b).ToString("0.##"))&&text.Contains("每跳相对本阶首陨伤害 ×"+BuildCatalog.CinderTrailTickMultiplier(b).ToString("0.####")),"cinder variant preview matches combat radius and tick budget");
   }
   Check(p.SetSpecialization(ElementalistSpecialization.Burn,true),"set actual caster specialization");Check(p.SkillEffectSummary(skill,3).Contains(BuildCatalog.SpecializationDescription(ElementalistSpecialization.Burn)),"burn control/damage specialization override is explicit");
  }
  var blade=Fresh(root,HeroClass.Vanguard,EquipmentMechanic.ReturningBlade,out id);Check(blade.ToggleMechanicVariant(id,true),"unlock counter B");string counter=Change(blade,0,2,3);Check(counter.Contains("回刃 B：放弃全部弹射")&&!counter.Contains("弹向4米"),"counter B does not claim original ricochet capability");
  var twin=Fresh(root,HeroClass.Summoner,EquipmentMechanic.TwinSummonResonance,out id);twin.Profile.skillRanks[2]=2;string summon=Change(twin,2,2,3);Check(summon.Contains("数量与继承倍率以上限机制为准")&&summon.Contains(BuildCatalog.MechanicDescription(EquipmentMechanic.TwinSummonResonance)),"summon cap replacement explicitly overrides baseline rank description");
  return "PASS "+n+" actual equipped variant / draft before-after / persistence / authoritative budget assertions";
 }
}
