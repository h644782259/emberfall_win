using System;
using System.IO;
using System.Linq;
using Emberfall;
public static class MandatoryChestFaults { public static bool FailGrant; }
public static class MandatoryChestServiceTests
{
    static int checks;
    static void C(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
    static ProgressionService New(string root){var p=new ProgressionService(Path.Combine(root,Guid.NewGuid().ToString("N")));C(p.CreateNewSlot(HeroClass.Ranger),"new durable fixture");return p;}
    static ItemData Item(){return new ItemData{id=Guid.NewGuid().ToString("N"),name="容量测试",slot=ItemSlot.Relic,rarity=Rarity.Common,level=1,attack=1};}
    public static string Run(string root)
    {
        Directory.CreateDirectory(root);
        var full=New(root);while(full.Profile.inventory.Count<ProgressionService.InventoryCapacity)full.Profile.inventory.Add(Item());
        full.Save();C(File.Exists(full.SaveFilePath)&&full.PrepareDungeonChest(40),"full normal bag and qualification saved");int before=full.Profile.inventory.Count;
        C(full.OpenDungeonChest()!=null&&full.Profile.inventory.Count==before+AdventureRewardRules.EquipmentCount(-1,40),"full normal bag retains all clear equipment in visible overflow");
        string receipt=full.LastChestReward.Id;int gold=full.Profile.gold,materials=full.Profile.mechanicMaterials,count=full.Profile.inventory.Count;
        C(full.OpenDungeonChest()==null&&full.LastChestReward.Id==receipt&&full.Profile.gold==gold&&full.Profile.mechanicMaterials==materials&&full.Profile.inventory.Count==count,"double click grants neither money nor items twice");
        Directory.CreateDirectory(full.SaveFilePath+".tmp");C(!full.AcknowledgeChestReward()&&full.Profile.pendingChestReveal,"failed acknowledgement retains required receipt");Directory.Delete(full.SaveFilePath+".tmp");C(full.AcknowledgeChestReward(),"acknowledgement can retry");
        C(full.LoadSlot(full.CurrentSlotId)&&!full.Profile.pendingFashionChest&&!full.Profile.pendingChestReveal&&full.Profile.inventory.Count==count,"restart keeps once-only result and visible overflow");

        var bounded=New(root);while(bounded.Profile.inventory.Count<ProgressionService.MaximumSavedEquipment)bounded.Profile.inventory.Add(Item());
        bounded.Save();C(File.Exists(bounded.SaveFilePath)&&bounded.PrepareDungeonChest(40),"absolute preservation capacity fixture saved");gold=bounded.Profile.gold;materials=bounded.Profile.mechanicMaterials;
        C(bounded.OpenDungeonChest()==null&&bounded.Profile.pendingFashionChest&&!bounded.Profile.pendingChestReveal&&bounded.Profile.inventory.Count==ProgressionService.MaximumSavedEquipment&&bounded.Profile.gold==gold&&bounded.Profile.mechanicMaterials==materials,"absolute capacity cannot consume entitlement or silently discard/sell items");
        string frozen=bounded.Profile.pendingChestDraw.id;
        C(bounded.LoadSlot(bounded.CurrentSlotId)&&bounded.Profile.pendingFashionChest&&bounded.Profile.pendingChestDraw.id==frozen,"blocked chest survives reload with frozen identity");
        foreach(var item in bounded.Profile.inventory.Where(i=>!i.locked&&i.id!=bounded.Profile.weaponId&&i.id!=bounded.Profile.armorId&&i.id!=bounded.Profile.relicId).Take(4).ToArray())C(bounded.Sell(item.id),"explicit merchant sale frees capacity atomically");
        C(bounded.OpenDungeonChest()!=null&&bounded.LastChestReward.Id==frozen&&bounded.Profile.inventory.Count==ProgressionService.MaximumSavedEquipment-4+AdventureRewardRules.EquipmentCount(-1,40),"capacity recovery retries the original reward with all equipment");

        var disk=New(root);C(disk.PrepareDungeonChest(10),"disk fault qualification");before=disk.Profile.inventory.Count;gold=disk.Profile.gold;
        Directory.CreateDirectory(disk.SaveFilePath+".tmp");C(disk.OpenDungeonChest()==null&&disk.Profile.pendingFashionChest&&!disk.Profile.pendingChestReveal&&disk.Profile.inventory.Count==before&&disk.Profile.gold==gold,"freeze write failure has no award side effects");Directory.Delete(disk.SaveFilePath+".tmp");
        MandatoryChestFaults.FailGrant=true;C(disk.OpenDungeonChest()==null&&disk.Profile.pendingFashionChest&&!disk.Profile.pendingChestReveal&&disk.Profile.inventory.Count==before,"grant fault preserves frozen qualification");frozen=disk.Profile.pendingChestDraw.id;
        var restart=new ProgressionService(disk.SaveDirectory);C(restart.LoadSlot(disk.CurrentSlotId)&&restart.Profile.pendingChestDraw.id==frozen,"process restart restores frozen failed grant");
        C(restart.OpenDungeonChest()!=null&&restart.LastChestReward.Id==frozen&&restart.Profile.inventory.Count==before+AdventureRewardRules.EquipmentCount(-1,10),"reentry commits original reward once");count=restart.Profile.inventory.Count;gold=restart.Profile.gold;
        C(restart.OpenDungeonChest()==null&&restart.Profile.inventory.Count==count&&restart.Profile.gold==gold,"reentry repeated open cannot duplicate");
        foreach(Rarity appearance in new[]{Rarity.Rare,Rarity.Epic,Rarity.Legendary})
        {
            var p=New(root);C(p.PrepareDungeonChest(10),"appearance qualification");
            for(int slot=0;slot<2;slot++)p.Profile.fashions.Add(new FashionData{id="fashion-"+slot+"-"+(int)appearance,slot=(FashionSlot)slot,rarity=Rarity.Legendary,appearanceTier=(int)appearance});
            int quality=appearance==Rarity.Legendary?0:appearance==Rarity.Epic?12:99;
            var draw=ProgressionService.BuildSingleChestRoll(p.Profile,quality,0,0,false,Guid.NewGuid().ToString("N"));
            if(draw.Rarity==appearance)C(draw.duplicate,"appearance ownership survives unified legendary quality");
            // Reproduce a persisted pre-fix draw that incorrectly recorded a new item.
            draw.rarityIndex=(int)appearance;draw.name=ProgressionService.FashionName(FashionSlot.Weapon,appearance,p.Profile.heroClass);draw.duplicate=false;draw.slotIndex=0;p.Profile.pendingChestDraw=draw;p.Save();
            var reload=new ProgressionService(p.SaveDirectory);C(reload.LoadSlot(p.CurrentSlotId),"load previously stuck chest");
            int owned=reload.Profile.fashions.Count;gold=reload.Profile.gold;
            MandatoryChestFaults.FailGrant=true;C(reload.OpenDungeonChest()==null&&reload.Profile.pendingFashionChest&&reload.Profile.gold==gold&&reload.Profile.fashions.Count==owned,"failed reconciled grant preserves original save");
            var retry=new ProgressionService(p.SaveDirectory);C(retry.LoadSlot(p.CurrentSlotId)&&retry.OpenDungeonChest()!=null,"restart resumes stuck duplicate without reroll");
            C(retry.LastChestReward.id==draw.id&&retry.LastChestReward.duplicate&&retry.LastChestReward.duplicateGold==800&&retry.LastChestReward.duplicateThreads==8&&retry.Profile.fashions.Count==owned,"same draw converts duplicate exactly once");
            gold=retry.Profile.gold;C(retry.OpenDungeonChest()==null&&retry.Profile.gold==gold,"reconciled chest cannot be claimed twice");
        }
        return "PASS "+checks+" mandatory chest capacity, explicit recovery, write faults, reentry and idempotence checks; managed persistence boundary";
    }
}
