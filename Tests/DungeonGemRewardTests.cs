using System;
using System.IO;
using System.Linq;
using Emberfall;
public static class DungeonGemRewardTests
{
 static int n;static void C(bool value,string message){n++;if(!value)throw new Exception(message);}
 public static string Run(string root)
 {
  for(int level=1;level<=100;level++)
  {int maximum=AdventureRewardRules.MaximumDungeonIndex(level);C(AdventureRewardRules.DungeonLevel(maximum)<=level+10,"level cap");C(maximum==10||AdventureRewardRules.DungeonLevel(maximum+1)>level+10,"highest eligible level");}
  var unique=new System.Collections.Generic.HashSet<EquipmentMechanic>();
  for(int mode=-1;mode<=3;mode++)
  {
   var p=new ProgressionService(Path.Combine(root,"gem-"+mode));C(p.CreateNewSlot(HeroClass.Arcanist),"create fixture");
   var gem=AdventureRewardRules.ExclusiveGem(mode);C(gem!=EquipmentMechanic.None&&unique.Add(gem),"every dungeon has distinct whole gem");
   for(int run=0;run<2;run++)
   {
    string id=Guid.NewGuid().ToString("N");
    C(p.TryGrantModeReward(id,0,0,0,40,mode),"durable qualification");
    int before=p.Profile.mechanicMaterials;
    if(run==0){MandatoryChestFaults.FailGrant=true;C(p.OpenDungeonChest()==null&&p.Attachment(gem)==null,"failed chest commit cannot grant gem");}
    C(p.OpenDungeonChest()!=null,"open committed chest: "+p.LastError);
    C(p.Attachment(gem)!=null&&!p.Attachment(gem).mounted&&p.Attachment(gem).rarity==Rarity.Epic,"whole epic gem stored unmounted");
    C(p.Profile.attachments.Count(a=>a.mechanic==gem)==1,"no duplicate gem entries");
    C(p.LastChestReward.gemMechanic==gem&&p.LastChestReward.duplicateGem==(run==1),"receipt describes actual gem result");
    C(p.Profile.mechanicMaterials-before==AdventureRewardRules.Materials(mode,40)+(run==1?3:0),"duplicate converts exactly three fragments");
    int fragments=p.Profile.mechanicMaterials;C(p.OpenDungeonChest()==null&&p.Profile.mechanicMaterials==fragments,"repeat click cannot grant twice");
    C(p.LoadSlot(p.CurrentSlotId)&&p.LastChestReward.gemMechanic==gem,"gem receipt survives save reload including max-tier duplicate materials");
    C(p.AcknowledgeChestReward(),"acknowledge receipt");
   }
  }
  return n+" exclusive whole-gem reward, retry and reload checks passed";
 }
}
