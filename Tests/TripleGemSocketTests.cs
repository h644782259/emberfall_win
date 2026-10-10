using System;using System.IO;using System.Linq;using Emberfall;
public static class TripleGemSocketTests
{
 static int checks;static void C(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
 public static string Run(string dir){
 foreach(HeroClass hero in new[]{HeroClass.Vanguard,HeroClass.Arcanist,HeroClass.Ranger,HeroClass.Summoner}){
 var p=new ProgressionService(Path.Combine(dir,hero.ToString()));C(p.CreateNewSlot(hero),"create");p.Profile.level=100;p.Profile.mechanicMaterials=999;
 foreach(var mechanic in BuildCatalog.GemsFor(hero))p.Profile.attachments.Add(new MechanicAttachment{id=Guid.NewGuid().ToString("N"),mechanic=mechanic,level=100,upgradeRank=3,ascensionRank=1,variantUnlocked=true,mounted=false});
 foreach(var mechanic in BuildCatalog.GemsFor(hero).Where(BuildCatalog.IsAttributeGem)){C(p.SetAttachmentMounted(mechanic,true,true),"mount "+mechanic);C(p.Attachment(mechanic).mounted,"mounted");}
 C(p.Profile.attachments.Count(a=>a.mounted)==9,"all nine remain mounted");
 foreach(var mechanic in BuildCatalog.MechanicsFor(hero)){C(!p.SetAttachmentMounted(mechanic,true,true),"fourth rejected");C(p.Profile.attachments.Count(a=>a.mounted)==9,"full slot preserves existing");}
 C(!p.SetAttachmentMounted(EquipmentMechanic.WeaponPower,false,false),"outside camp rejected");
 var stats=p.GetStats();C(stats.GemHealthyDamage>0&&stats.GemLowHealthGuard>0&&stats.GemLowEnergyRecovery>0,"all slot forms contribute");
 C(p.SaveBuildPreset(0,true),"save nine-gem preset: "+p.LastError);
 C(p.SetAttachmentMounted(EquipmentMechanic.WeaponPower,false,true),"remove exactly one");C(p.Profile.attachments.Count(a=>a.mounted)==8,"others stay");
 var weaker=p.GetStats();C(weaker.Damage<stats.Damage&&weaker.GemHealthyDamage<stats.GemHealthyDamage,"removed gem stops contributing");
 C(p.ApplyBuildPreset(0,true),"restore nine-gem preset: "+p.LastError);C(p.Profile.attachments.Count(a=>a.mounted)==9,"preset restores three per slot");
 C(p.ToggleAttachmentVariant(EquipmentMechanic.WeaponPower,true),"individual variant");C(p.Attachment(EquipmentMechanic.WeaponPower).variant==1&&p.Attachment(EquipmentMechanic.WeaponPrecision).variant==0,"other variants unchanged");
 string before=File.ReadAllText(p.SaveFilePath);Directory.CreateDirectory(p.SaveFilePath+".tmp");C(!p.SetAttachmentMounted(EquipmentMechanic.WeaponPower,false,true),"failed save rejected");C(p.Attachment(EquipmentMechanic.WeaponPower).mounted&&File.ReadAllText(p.SaveFilePath)==before,"failed save retains disk and memory");Directory.Delete(p.SaveFilePath+".tmp");
 var reload=new ProgressionService(p.SaveDirectory);C(reload.LoadSlot(p.CurrentSlotId),"reload");C(reload.Profile.attachments.Count(a=>a.mounted)==9,"reload retains all nine");
 }
 return "PASS "+checks+" triple gem socket, stats, independent forms, preset and save transaction assertions";
 }
}
