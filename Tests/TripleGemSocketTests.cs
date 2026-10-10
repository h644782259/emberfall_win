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
 var stats=p.GetStats();C(stats.GemHealthyDamage>0&&stats.GemLowHealthGuard>0&&p.Attachment(EquipmentMechanic.RelicPower).mounted,"weapon and armor forms contribute while relic combos remain mounted");
 C(p.SetAttachmentMounted(EquipmentMechanic.WeaponPower,false,true),"remove exactly one");C(p.Profile.attachments.Count(a=>a.mounted)==8,"others stay");
 var weaker=p.GetStats();C(weaker.Damage<stats.Damage&&weaker.GemHealthyDamage<stats.GemHealthyDamage,"removed gem stops contributing");
 C(p.ReplaceAttachment(EquipmentMechanic.WeaponPower,EquipmentMechanic.None,true),"fill empty socket");C(p.Profile.attachments.Count(a=>a.mounted)==9,"fill restores three per slot");
 var replacement=BuildCatalog.MechanicsFor(hero).First();var oldGem=BuildCatalog.GemsFor(hero).First(m=>BuildCatalog.IsAttributeGem(m)&&BuildCatalog.MechanicSlot(m)==BuildCatalog.MechanicSlot(replacement));
 C(p.AttachmentReplacementLock(replacement,oldGem,true)=="","full socket allows targeted replacement");
 string original=File.ReadAllText(p.SaveFilePath);Directory.CreateDirectory(p.SaveFilePath+".tmp");
 C(!p.ReplaceAttachment(replacement,oldGem,true),"replacement write failure rejected");
 C(p.Attachment(oldGem).mounted&&!p.Attachment(replacement).mounted&&p.Profile.attachments.Count(a=>a.mounted)==9&&File.ReadAllText(p.SaveFilePath)==original,"replacement failure preserves both gems, other sockets and disk");Directory.Delete(p.SaveFilePath+".tmp");
 C(!p.ReplaceAttachment(replacement,oldGem,false),"replacement outside camp rejected");
 C(p.ReplaceAttachment(replacement,oldGem,true),"replace without first removing");
 C(!p.Attachment(oldGem).mounted&&p.Attachment(replacement).mounted&&p.Profile.attachments.Count(a=>a.mounted)==9,"only clicked socket replaced");
 C(!p.ReplaceAttachment(oldGem,oldGem,true),"stale original socket rejected");
 C(!p.ReplaceAttachment(EquipmentMechanic.WeaponPrecision,replacement,true),"already mounted or cross-slot replacement rejected");
 C(!p.ReplaceAttachment(EquipmentMechanic.ArmorPower,EquipmentMechanic.WeaponPower,true),"cross-slot replacement rejected");
 var replacedReload=new ProgressionService(p.SaveDirectory);C(replacedReload.LoadSlot(p.CurrentSlotId)&&replacedReload.Attachment(replacement).mounted&&!replacedReload.Attachment(oldGem).mounted,"replacement survives reload");
 C(p.ReplaceAttachment(oldGem,replacement,true),"restore original directly");
 C(p.ToggleAttachmentVariant(EquipmentMechanic.WeaponPower,true),"individual variant");C(p.Attachment(EquipmentMechanic.WeaponPower).variant==1&&p.Attachment(EquipmentMechanic.WeaponPrecision).variant==0,"other variants unchanged");
 string before=File.ReadAllText(p.SaveFilePath);Directory.CreateDirectory(p.SaveFilePath+".tmp");C(!p.SetAttachmentMounted(EquipmentMechanic.WeaponPower,false,true),"failed save rejected");C(p.Attachment(EquipmentMechanic.WeaponPower).mounted&&File.ReadAllText(p.SaveFilePath)==before,"failed save retains disk and memory");Directory.Delete(p.SaveFilePath+".tmp");
 var reload=new ProgressionService(p.SaveDirectory);C(reload.LoadSlot(p.CurrentSlotId),"reload");C(reload.Profile.attachments.Count(a=>a.mounted)==9,"reload retains all nine");
 }
 return "PASS "+checks+" triple gem socket, stats, independent forms, replacement and save transaction assertions";
 }
}
