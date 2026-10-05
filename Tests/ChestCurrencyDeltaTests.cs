using System;
using System.IO;
using Emberfall;
using UnityEngine;
public static class ChestCurrencyDeltaTests
{
    static int checks;static void Check(bool ok,string reason){checks++;if(!ok)throw new Exception(reason);}
    public static string Run(string root)
    {
        checks=0;var p=new ProgressionService(Path.Combine(root,Guid.NewGuid().ToString("N")));Check(p.CreateNewSlot(HeroClass.Vanguard),"saved fixture");
        for(int round=0;round<40;round++)
        {
            p.Profile.gold=round%2==0?999999998:60;p.Profile.fashionThreads=round%2==0?999998:0;
            // Own every fashion: a rolled collection is deterministically a duplicate.
            for(int slot=0;slot<2;slot++)for(int rank=0;rank<4;rank++)
            {string id="fashion-"+slot+"-"+rank;if(!p.Profile.fashions.Exists(x=>x.id==id))p.Profile.fashions.Add(new FashionData{id=id,slot=(FashionSlot)slot,rarity=(Rarity)rank});}
            p.Save();p.PrepareDungeonChest();int gold=p.Profile.gold,threads=p.Profile.fashionThreads;
            string before=JsonUtility.ToJson(p.Profile,true);Directory.CreateDirectory(p.SaveFilePath+".tmp");
            Check(p.OpenDungeonChest()==null&&JsonUtility.ToJson(p.Profile,true)==before,"failed draw publishes no delta or live reward");Directory.Delete(p.SaveFilePath+".tmp");
            Check(p.OpenDungeonChest()!=null,"durable open");var reward=p.LastChestReward;
            Check(reward.hasCurrencyDeltas&&reward.goldDelta==p.Profile.gold-gold&&reward.threadsDelta==p.Profile.fashionThreads-threads,"receipt matches actual currency changes including caps");
            Check(round%2!=0||reward.goldDelta==1&&reward.threadsDelta==1,"cap has real one-unit gains, not nominal award");
            Check(p.LoadSlot(p.CurrentSlotId)&&p.LastChestReward.goldDelta==reward.goldDelta&&p.LastChestReward.threadsDelta==reward.threadsDelta,"delta survives restart");
            Check(p.OpenDungeonChest()==null&&p.LastChestReward.id==reward.id,"repeat cannot replace receipt");Check(p.AcknowledgeChestReward(),"ack");
        }
        var legacy=JsonUtility.FromJson<ChestReward>("{\"gold\":150,\"rarityIndex\":-1}");Check(!legacy.hasCurrencyDeltas,"legacy absence never invents actual gain");
        return "PASS: "+checks+" chest actual currency delta transaction checks";
    }
}
