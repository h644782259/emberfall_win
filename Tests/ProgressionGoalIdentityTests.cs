using System;
using System.IO;
using System.Text.Json.Nodes;
using Emberfall;
using UnityEngine;
public static class ProgressionGoalIdentityTests
{
    static int checks;
    static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
    static string State(ProgressionService p)=>JsonUtility.ToJson(p.Profile,true);
    static ProgressionService Fresh(string root)
    {var p=new ProgressionService(Path.Combine(root,Guid.NewGuid().ToString("N")));Check(p.CreateNewSlot(HeroClass.Arcanist),"create actual persisted character");return p;}
    public static string Run(string root)
    {
        checks=0;var p=Fresh(root);p.Profile.mechanicMaterials=80;
        var ice=p.CreateMechanicItem(EquipmentMechanic.FrostEcho);Check(p.CollectLoot(ice),"collect first core");
        Check(p.SelectCoreGoal(EquipmentMechanic.CinderTrail),"select missing second core");var fireGoal=p.SelectedProgressionGoal(true);
        Check(!fireGoal.Done&&fireGoal.Title.Contains("烬")&&fireGoal.Action==ProgressionGoalAction.ExchangeCore,"different owned core never completes selected target");
        string identity=fireGoal.Identity,before=State(p),disk=File.ReadAllText(p.SaveFilePath);int events=0;p.Changed+=()=>events++;
        Directory.CreateDirectory(p.SaveFilePath+".tmp");
        Check(!p.ExecuteProgressionGoal(fireGoal.ActionIdentity,true)&&State(p)==before&&events==0,"failed exchange leaves money, inventory, identity and events unchanged");
        Check(File.ReadAllText(p.SaveFilePath)==disk,"failed action leaves durable profile unchanged");
        Check(!p.SelectCoreGoal(EquipmentMechanic.FrostEcho)&&State(p)==before&&events==0,"failed goal change preserves live identity");
        Directory.Delete(p.SaveFilePath+".tmp");
        Check(p.ExecuteProgressionGoal(fireGoal.ActionIdentity,true),"target core exchange succeeds");var acquired=p.SelectedProgressionGoal(true);
        Check(acquired.Done&&acquired.Identity==identity&&acquired.Action==ProgressionGoalAction.None&&p.Attachment(EquipmentMechanic.CinderTrail).mounted,"acquisition keeps goal identity and mounts independent attachment");
        before=State(p);Check(!p.ExecuteProgressionGoal(fireGoal.ActionIdentity,true)&&State(p)==before,"replayed exchange token cannot buy again or silently execute next action");
        Check(p.ExecuteProgressionGoal(acquired.ActionIdentity,true),"completed attachment acquisition is idempotent");
        var legacyItem=p.CreateMechanicItem(EquipmentMechanic.CinderTrail);Check(p.CollectLoot(legacyItem),"retain legacy equipment investment path");
        acquired.ItemId=legacyItem.id;
        Check(p.SelectProgressionGoal(ProgressionGoalKind.Variant,acquired.ItemId),"variant targets actual item");var variant=p.SelectedProgressionGoal(true);
        before=State(p);Directory.CreateDirectory(p.SaveFilePath+".tmp");Check(!p.ExecuteProgressionGoal(variant.ActionIdentity,true)&&State(p)==before,"failed unlock does not spend materials or mutate variant");Directory.Delete(p.SaveFilePath+".tmp");
        Check(p.ExecuteProgressionGoal(variant.ActionIdentity,true),"unlock selected variant");before=State(p);
        Check(!p.ExecuteProgressionGoal(variant.ActionIdentity,true)&&State(p)==before,"repeated unlock token cannot toggle variant back");
        Check(p.UnlockMechanicVariant(acquired.ItemId,true)&&State(p)==before,"explicit unlock is idempotent");
        var item=p.Profile.inventory.Find(x=>x.id==acquired.ItemId);item.rarity=Rarity.Rare;p.Attachment(item.mechanic).rarity=Rarity.Rare;
        Check(!p.SelectProgressionGoal(ProgressionGoalKind.Ascension,item.id)&&p.LastError.Contains("史诗"),"rare cannot select impossible ascension goal");
        Check(p.AscensionLockReason(item.id,true)==p.MechanicGoalEligibility(item.id,ProgressionGoalKind.Ascension),"target and action share item quality eligibility");
        Check(p.SelectCoreGoal(item.mechanic,Rarity.Epic)&&!p.SelectedProgressionGoal(true).Done,"rare owner may explicitly track corresponding epic prerequisite");
        var epic=p.CreateMechanicItem(item.mechanic);Check(p.CollectLoot(epic)&&p.SelectedProgressionGoal(true).Done,"actual epic completes acquisition without auto-selecting ascension");
        Check(p.Profile.progressionGoal==ProgressionGoalKind.Core&&p.SelectProgressionGoal(ProgressionGoalKind.Ascension,epic.id),"ascension is a separate explicit selection");
        Check(!p.SelectedProgressionGoal(true).CanAct,"milestone blocks same action offered by target");
        p.Profile.highestAdventureTier=5;p.Profile.mechanicMaterials=100;
        var ascend=p.SelectedProgressionGoal(true);Check(ascend.CanAct&&ascend.MaterialCost==ProgressionService.AscensionCost,"shared milestone/material qualification");
        before=State(p);Directory.CreateDirectory(p.SaveFilePath+".tmp");Check(!p.ExecuteProgressionGoal(ascend.ActionIdentity,true)&&State(p)==before,"failed ascension is atomic");Directory.Delete(p.SaveFilePath+".tmp");
        Check(p.ExecuteProgressionGoal(ascend.ActionIdentity,true)&&p.SelectedProgressionGoal(true).Done,"ascend targeted identity");before=State(p);
        Check(!p.ExecuteProgressionGoal(ascend.ActionIdentity,true)&&State(p)==before,"duplicate ascension token pays nothing");
        p.Profile.level=12;p.Profile.gold=99999;Check(p.SelectProgressionGoal(ProgressionGoalKind.Reforge,epic.id),"track reforge to captured level");
        var reforge=p.SelectedProgressionGoal(true);p.Profile.level=15;
        Check(p.Profile.progressionGoalLevel==10&&p.ExecuteProgressionGoal(reforge.ActionIdentity,true)&&p.SelectedProgressionGoal(true).Done,"reforge captures executable equipment tier without moving selected target");
        string persistedIdentity=p.SelectedProgressionGoal(true).Identity;
        var reload=new ProgressionService(Path.GetDirectoryName(p.SaveFilePath));Check(reload.LoadSlot(p.CurrentSlotId),"reload selected slot");
        Check(reload.SelectedProgressionGoal(true).Identity==persistedIdentity,"reload retains concrete target identity");
        Check(reload.ProgressionGoalStatus(5).Contains("本局 +5")&&reload.ProgressionGoalStatus().Contains("已完成"),"same status supports camp entry results and run gains");
        var legacy=Fresh(root);legacy.Profile.progressionGoal=ProgressionGoalKind.Core;legacy.Save();Check(string.IsNullOrEmpty(legacy.LastError),"save legacy generic target");
        var json=JsonNode.Parse(File.ReadAllText(legacy.SaveFilePath));json["profile"].AsObject().Remove("automaticGrowth");json["profile"].AsObject().Remove("growthRevision");json["profile"].AsObject().Remove("progressionGoalMechanic");json["profile"].AsObject().Remove("progressionGoalMinimumRarity");json["profile"].AsObject().Remove("progressionGoalLevel");File.WriteAllText(legacy.SaveFilePath,json.ToJsonString());disk=File.ReadAllText(legacy.SaveFilePath);
        var old=new ProgressionService(Path.GetDirectoryName(legacy.SaveFilePath));Check(old.LoadSlot(legacy.CurrentSlotId),"load legacy slot");
        Check(old.Profile.progressionGoal==ProgressionGoalKind.Core&&old.Profile.progressionGoalMechanic==EquipmentMechanic.None&&old.SelectedProgressionGoal(true).Identity=="Core/legacy","old goal does not invent a specific mechanic");
        Check(!old.SelectedProgressionGoal(true).CanAct&&File.ReadAllText(legacy.SaveFilePath)==disk,"legacy compatibility has no directed action or migration write");
        var removed=Fresh(root);removed.Profile.mechanicMaterials=ProgressionService.VariantCost;var target=removed.CreateMechanicItem(EquipmentMechanic.FrostEcho);removed.CollectLoot(target);removed.SelectProgressionGoal(ProgressionGoalKind.Variant,target.id);removed.Profile.inventory.RemoveAll(x=>x.id==target.id);removed.CollectLoot(removed.CreateMechanicItem(EquipmentMechanic.FrostEcho));
        Check(removed.SelectedProgressionGoal(true).CanAct&&!removed.SelectedProgressionGoal(true).Done&&removed.SelectedProgressionGoal(true).ItemId==removed.Attachment(target.mechanic).id,"lost legacy equipment goal migrates to persistent same-mechanism attachment");
        var queued=Fresh(root);queued.Profile.mechanicMaterials=12;
        while(queued.Profile.inventory.Count<ProgressionService.InventoryCapacity)queued.Profile.inventory.Add(new ItemData{id=Guid.NewGuid().ToString("N"),name="fixture",level=1,slot=ItemSlot.Armor});
        Check(queued.SelectCoreGoal(EquipmentMechanic.CinderTrail),"select core while bag full");var queueExchange=queued.SelectedProgressionGoal(true);
        Check(queued.ExecuteProgressionGoal(queueExchange.ActionIdentity,true),"full bag still permits independent attachment acquisition");var pending=queued.SelectedProgressionGoal(true);
        Check(pending.Identity==queueExchange.Identity&&pending.Done&&pending.Action==ProgressionGoalAction.None&&queued.Profile.pendingLoot.Count==0,"attachment acquisition preserves identity without needing inventory space");
        queued.Profile.inventory.RemoveAt(queued.Profile.inventory.Count-1);
        Check(!queued.SelectedProgressionGoal(false).CanAct&&queued.SelectedProgressionGoal(false).Done,"completed attachment needs no outside-camp action");
        Check(queued.ExecuteProgressionGoal(queued.SelectedProgressionGoal(true).ActionIdentity,true)&&queued.SelectedProgressionGoal(true).Done,"claim exact queued item completes same acquisition");
        var first=Fresh(root);first.Profile.highestAdventureTier=1;first.Profile.pendingFirstClearReward=true;first.SelectCoreGoal(EquipmentMechanic.FrostEcho);
        var reward=first.SelectedProgressionGoal(true);Check(reward.Action==ProgressionGoalAction.ClaimCore&&reward.MaterialCost==0,"specific first-clear target uses free entitlement");
        before=State(first);Directory.CreateDirectory(first.SaveFilePath+".tmp");Check(!first.ExecuteProgressionGoal(reward.ActionIdentity,true)&&State(first)==before,"failed first-clear target claim preserves entitlement and identity");Directory.Delete(first.SaveFilePath+".tmp");
        Check(first.ExecuteProgressionGoal(reward.ActionIdentity,true)&&first.Profile.firstClearRewardClaimed&&first.Profile.mechanicMaterials==0,"first-clear target consumed exactly once without shards");
        before=State(first);Check(!first.ExecuteProgressionGoal(reward.ActionIdentity,true)&&State(first)==before,"replayed free claim cannot turn into exchange/equip");
        int changed=0;first.Changed+=()=>changed++;Check(first.SelectCoreGoal(EquipmentMechanic.FrostEcho)&&changed==0,"same explicit selection writes no new transaction");
        Check(!first.SelectCoreGoal(EquipmentMechanic.VenomSpread)&&changed==0,"foreign-class target rejected");
        var oldJson=JsonNode.Parse(File.ReadAllText(old.SaveFilePath));oldJson["profile"]["progressionGoalMechanic"]=(int)EquipmentMechanic.VenomSpread;File.WriteAllText(old.SaveFilePath,oldJson.ToJsonString());
        Check(old.LoadSlot(old.CurrentSlotId)&&old.Profile.progressionGoalMechanic==EquipmentMechanic.None&&!old.SelectedProgressionGoal(true).CanAct,"cross-class corrupt target normalizes to unchosen compatibility state");
        return "PASS: "+checks+" production goal identity, qualification, failure transaction and replay checks";
    }
}
