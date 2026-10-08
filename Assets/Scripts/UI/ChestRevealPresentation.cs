using System;
using System.Collections.Generic;
namespace Emberfall
{
    public enum ChestResultKind { Gold, FirstCollection, Duplicate, Resources }
    // Presentation reads the committed receipt; progress/skip never grant a reward.
    public static class ChestRevealPresentation
    {
        public static float Progress(float elapsed,float duration)
        {
            if(float.IsNaN(elapsed)||float.IsNaN(duration)||duration<=0)return 1;
            return Math.Max(0,Math.Min(1,elapsed/duration));
        }
        public static float UnselectedOpacity(float progress){return Math.Max(0,1-Math.Max(0,progress)/.4f);}
        public static ChestResultKind Kind(ChestReward reward)
        {return reward==null||!reward.Rarity.HasValue?(reward!=null&&reward.materialKind==RewardMaterialKind.StarAshFragment?ChestResultKind.Resources:ChestResultKind.Gold):reward.Duplicate?ChestResultKind.Duplicate:ChestResultKind.FirstCollection;}
        public static string Outcome(ChestReward reward)
        {
            switch(Kind(reward))
            {case ChestResultKind.Resources:return "通关资源 · 已入账";case ChestResultKind.FirstCollection:return "首次收藏 · 外观已入藏";case ChestResultKind.Duplicate:return "重复收藏 · 已转金币与星纹";default:return "金币奖励 · 已入账";}
        }
        public static string Result(ChestReward reward,int threads)
        {
            if(reward==null)return "正在读取已保存的奖励";
            string identity=reward.Rarity.HasValue?GameBalance.RarityName(reward.Rarity.Value)+" · "+reward.Name+"\n":"";
            string gain=reward.hasCurrencyDeltas?"到账 +"+reward.goldDelta+" 金币 · +"+reward.threadsDelta+" 星纹":"金币奖励 "+reward.Gold+"（旧记录未保存实际增量）";
            if(reward.hasCurrencyDeltas&&reward.materialKind==RewardMaterialKind.StarAshFragment)gain+=" · +"+reward.materialsDelta+" 星烬碎片";
            return identity+Outcome(reward)+"\n\n"+gain+"\n星纹余额 "+threads+" / "+ProgressionService.FashionChoiceCost+" · "+"可在收藏页查看兑换缺口";
        }
        public const string ChoiceDisclosure = "奖励随副本与阶数变化 · 装备等级匹配角色";
        public static int CollectionCount(GameProfile profile,FashionSlot slot)
        {
            var ranks=new HashSet<Rarity>();
            if(profile!=null&&profile.fashions!=null&&Enum.IsDefined(typeof(FashionSlot),slot))
                foreach(var fashion in profile.fashions)
                    if(fashion!=null&&fashion.slot==slot&&Enum.IsDefined(typeof(Rarity),fashion.rarity))ranks.Add(fashion.rarity);
            return ranks.Count;
        }
        public static string CollectionProgress(GameProfile profile,FashionSlot slot)
        {return (slot==FashionSlot.Weapon?"兵装":"羽翼")+"收藏 "+CollectionCount(profile,slot)+" / "+Enum.GetValues(typeof(Rarity)).Length;}
        public static string ChoiceDetail(GameProfile profile,int choice)
        {
            if(choice==2)return "金币 ×1.5 · 无时装\n基础星纹 +1";
            return "40% 时装 · 非必出\n"+CollectionProgress(profile,choice==0?FashionSlot.Weapon:FashionSlot.Wings);
        }
        // A historical choice index is not a box type. Only revision 1 defines supply.
        public static bool IsSupplyReceipt(ChestReward reward)
        {return reward!=null&&reward.rulesRevision==1&&reward.choice==2&&!reward.Rarity.HasValue;}
        public static string GoldHeadline(ChestReward reward)
        {return reward==null?"金币奖励":reward.hasCurrencyDeltas?"+"+reward.goldDelta+" 金币":"金币奖励 "+reward.Gold;}
        public static string ResultWithCollection(ChestReward reward,GameProfile profile)
        {
            string result=Result(reward,profile==null?0:profile.fashionThreads);
            result+="\n"+LegendaryExchangeHint(profile);
            if(IsSupplyReceipt(reward))result="补给 · 更多金币（×1.5，向下取整）\n"+result;
            if(reward!=null&&reward.Rarity.HasValue&&reward.Slot.HasValue&&Enum.IsDefined(typeof(FashionSlot),reward.Slot.Value))
                result+="\n当前"+CollectionProgress(profile,reward.Slot.Value);
            return result;
        }
        public static string LegendaryExchangeHint(GameProfile profile)
        {
            if(profile==null)return "收藏状态未知，请查看收藏页";
            bool weapon=profile.fashions!=null&&profile.fashions.Exists(f=>f!=null&&f.slot==FashionSlot.Weapon&&f.rarity==Rarity.Legendary);
            bool wings=profile.fashions!=null&&profile.fashions.Exists(f=>f!=null&&f.slot==FashionSlot.Wings&&f.rarity==Rarity.Legendary);
            if(weapon&&wings)return "两部位传说已收藏 · 星纹仍可手动换碎片";
            string missing=weapon?"羽翼":wings?"兵装":"兵装 / 羽翼";
            return profile.fashionThreads>=ProgressionService.FashionChoiceCost?"可在营地兑换缺少的传说"+missing:"传说"+missing+"尚缺 · 距兑换还差 "+(ProgressionService.FashionChoiceCost-profile.fashionThreads)+"星纹";
        }
        public static float Travel(float progress)
        {float t=Math.Max(0,Math.Min(1,(progress-.15f)/.65f));return t*t*(3-2*t);}
        public static float DesktopArtSize(float bodyHeight){return Math.Max(0,Math.Min(320,bodyHeight));}
    }
}
