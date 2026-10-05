using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Emberfall;
using UnityEngine;
public static class ChestChoicePresentationTests
{
    static int checks;
    static void Check(bool value,string why){checks++;if(!value)throw new Exception(why);}
    sealed class Roll:Random
    {
        readonly int rarity;
        public Roll(int rarity){this.rarity=rarity;}
        public override int Next(int max){return max==100?rarity:0;}
    }
    static void SetRoll(ProgressionService p,int rarity)
    {typeof(ProgressionService).GetField("random",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(p,new Roll(rarity));}
    public static void Run(string root)
    {
        var p=new ProgressionService(root);Check(p.CreateNewSlot(HeroClass.Ranger),"create real profile");
        var profile=p.Profile;
        profile.fashions.Add(new FashionData{slot=FashionSlot.Weapon,rarity=Rarity.Common});
        profile.fashions.Add(new FashionData{slot=FashionSlot.Weapon,rarity=Rarity.Common});
        profile.fashions.Add(new FashionData{slot=FashionSlot.Weapon,rarity=(Rarity)99});
        profile.fashions.Add(new FashionData{slot=(FashionSlot)99,rarity=Rarity.Epic});
        profile.fashions.Add(null);
        Check(ChestRevealPresentation.CollectionCount(profile,FashionSlot.Weapon)==1,"count distinct valid ranks, not list length");
        Check(ChestRevealPresentation.CollectionCount(profile,FashionSlot.Wings)==0,"collection isolated by actual part");
        Check(ChestRevealPresentation.CollectionCount(null,FashionSlot.Wings)==0,"missing collection safe");
        var full=new GameProfile();
        foreach(Rarity rarity in Enum.GetValues(typeof(Rarity)))full.fashions.Add(new FashionData{slot=FashionSlot.Wings,rarity=rarity});
        Check(ChestRevealPresentation.CollectionCount(full,FashionSlot.Wings)==4&&ChestRevealPresentation.CollectionProgress(full,FashionSlot.Wings)=="羽翼收藏 4 / 4","complete collection count from real records");
        var copy=JsonUtility.ToJson(profile,true);
        var traces=new HashSet<string>();
        for(int choice=0;choice<3;choice++)
        {
            foreach(float scale in new[]{1f,1.5f,2f})
            {
                GameUI.CheckCard(new Rect(20,30,158*scale,176*scale),choice,profile,scale);
                GameUI.CheckCard(new Rect(20,30,260*scale,342*scale),choice,profile,scale);
            }
            traces.Add(GameUI.EmblemTrace(choice));
        }
        Check(traces.Count==3,"three distinct production emblem silhouettes");
        Check(JsonUtility.ToJson(profile,true)==copy,"repeated actual UI drawing is read only");
        Check(ChestRevealPresentation.ChoiceDisclosure.Contains("40%")&&ChestRevealPresentation.ChoiceDisclosure.Contains("非必出")&&ChestRevealPresentation.ChoiceDisclosure.Contains("无时装"),"first screen promises explicit");
        profile.fashions.Clear();
        for(int choice=0;choice<3;choice++)
        {
            p.Profile.pendingFashionChest=true;SetRoll(p,choice==2?50:0);Check(p.OpenDungeonChest()!=null,"real chest saved");
            var receipt=p.LastChestReward;var disk=File.ReadAllText(p.SaveFilePath);string id=receipt.Id;
            var before=JsonUtility.ToJson(p.Profile,true);
            if(choice<2)
            {
                var slot=choice==0?FashionSlot.Wings:FashionSlot.Weapon;
                Check(ChestRevealPresentation.ResultWithCollection(receipt,p.Profile).Contains("当前"+ChestRevealPresentation.CollectionProgress(p.Profile,slot)),"actual receipt collection shown");
                Check(GameUI.Model(receipt)?.slot==slot,"actual model consumes receipt slot");
                Check(GameUI.Model(receipt)?.rarity==receipt.Rarity,"actual model consumes receipt quality");
            }
            else
            {
                Check(ChestRevealPresentation.ResultWithCollection(receipt,p.Profile).Contains("星烬碎片"),"single chest exposes actual material receipt");
                Check(GameUI.Model(receipt)==null,"supply never draws fashion model");
                Check(GameUI.Gold(receipt).Contains("+"+receipt.goldDelta+" 金币"),"gold art uses committed delta");
            }
            Check(JsonUtility.ToJson(p.Profile,true)==before&&File.ReadAllText(p.SaveFilePath)==disk,"result drawing does not mutate state or save");
            Check(p.Load()&&p.LastChestReward.Id==id,"reload retains exact receipt");Check(p.AcknowledgeChestReward(),"ack actual receipt");
        }
        // Force a real duplicate; the displayed collection must stay unique.
        p.Profile.pendingFashionChest=true;SetRoll(p,0);p.OpenDungeonChest();
        Check(p.LastChestReward.Duplicate&&ChestRevealPresentation.CollectionCount(p.Profile,FashionSlot.Weapon)==1,"duplicate does not increment progress");
        Check(ChestRevealPresentation.ResultWithCollection(p.LastChestReward,p.Profile).Contains("重复收藏"),"duplicate receipt preserved");p.AcknowledgeChestReward();
        // Old choice 2 legitimately held wings. Save/load and invoke the real model entry.
        var legacy=new ChestReward{id="legacy-wings",choice=2,slotIndex=0,rarityIndex=3,name="历史羽翼",gold=93,summary="历史回执"};
        p.Profile.lastChestReward=legacy;p.Profile.pendingChestReveal=true;p.Save();Check(p.Load(),"load old receipt");
        legacy=p.LastChestReward;copy=File.ReadAllText(p.SaveFilePath);
        Check(GameUI.Model(legacy)?.slot==FashionSlot.Wings,"legacy choice2 actual model stays wings");
        Check(ChestRevealPresentation.ResultWithCollection(legacy,p.Profile).Contains(legacy.Name)&&!ChestRevealPresentation.ResultWithCollection(legacy,p.Profile).Contains("补给"),"legacy fashion not reinterpreted");
        legacy=new ChestReward{id="legacy-gold",choice=2,gold=93};
        Check(!ChestRevealPresentation.IsSupplyReceipt(legacy)&&!ChestRevealPresentation.ResultWithCollection(legacy,p.Profile).Contains("×1.5"),"legacy gold not relabelled supply");
        Check(GameUI.Gold(legacy).Contains("金币奖励 93"),"legacy unknown delta not invented");
        Check(File.ReadAllText(p.SaveFilePath)==copy,"old receipt visualization never rewrites disk");
        Console.WriteLine("PASS: "+checks+" H03 collection/real receipt checks; 18 actual card draws plus 3 emblem and real model-entry routes");
    }
}
