using System;using System.IO;using System.Linq;using Emberfall;
public static class LegendaryEquipmentPityTests
{
 static int checks;static void C(bool ok,string reason){checks++;if(!ok)throw new Exception(reason);}
 static string Receipt(int mode,bool legendary){for(;;){string id=Guid.NewGuid().ToString("N");uint h=2166136261;foreach(byte b in Guid.ParseExact(id,"N").ToByteArray())h=unchecked((h^b)*16777619);h=unchecked((h^(uint)(mode+2))*16777619);if((h%100<4)==legendary)return id;}}
 static void Qualify(ProgressionService p,string id,int mode){C(p.TryGrantModeReward(id,100,100,3,1,mode),"qualify chest");}
 public static string Run(string dir){
 for(int mode=-1;mode<=3;mode++)foreach(int tier in new[]{1,5,10,20,40,100}){int n=0;for(int roll=0;roll<100;roll++)if(AdventureRewardRules.EquipmentRarity(mode,tier,roll)==Rarity.Legendary)n++;C(n==4,"exact four percent every mode/tier");}
 var p=new ProgressionService(Path.Combine(dir,"pity"));C(p.CreateNewSlot(HeroClass.Arcanist),"create");
 for(int i=1;i<=29;i++){int mode=i%3;Qualify(p,Receipt(mode,false),mode);C(p.OpenChosenDungeonChest(i%3)!=null,"open");C(p.Profile.legendaryEquipmentMisses==i,"miss count per chest across modes");C(p.AcknowledgeChestReward(),"ack");if(i==10){var q=new ProgressionService(p.SaveDirectory);C(q.LoadSlot(p.CurrentSlotId)&&q.Profile.legendaryEquipmentMisses==10,"reload progress");p=q;}}
 string receipt=Receipt(0,false);Qualify(p,receipt,0);string saved=File.ReadAllText(p.SaveFilePath);Directory.CreateDirectory(p.SaveFilePath+".tmp");C(p.OpenChosenDungeonChest(2)==null,"failed save");C(p.Profile.legendaryEquipmentMisses==29&&File.ReadAllText(p.SaveFilePath)==saved,"failed save preserves streak and disk");Directory.Delete(p.SaveFilePath+".tmp");C(p.OpenChosenDungeonChest(2)!=null,"retry");C(p.Profile.inventory.Any(x=>x.id==receipt+"-clear-0"&&x.rarity==Rarity.Legendary)&&p.Profile.legendaryEquipmentMisses==0,"thirtieth guarantees legendary and resets");int count=p.Profile.inventory.Count;p.OpenChosenDungeonChest(2);C(p.Profile.inventory.Count==count&&p.Profile.legendaryEquipmentMisses==0,"repeat cannot duplicate gear or advance streak");C(p.AcknowledgeChestReward(),"ack pity");
 Qualify(p,Receipt(1,false),1);C(p.OpenChosenDungeonChest(0)!=null&&p.Profile.legendaryEquipmentMisses==1,"fresh streak");p.AcknowledgeChestReward();Qualify(p,Receipt(1,true),1);C(p.OpenChosenDungeonChest(0)!=null&&p.Profile.legendaryEquipmentMisses==0,"natural legendary resets early");var reload=new ProgressionService(p.SaveDirectory);C(reload.LoadSlot(p.CurrentSlotId)&&reload.Profile.legendaryEquipmentMisses==0,"reset persists");return "PASS "+checks+" equipment probability and persistent pity transaction assertions";
 }
}
