using System;
using Emberfall;
public static class RewardViewingRulesTests
{
    static int n;static void Check(bool b,string why){n++;if(!b)throw new Exception(why);}
    public static string Run()
    {
        n=0;
        for(int step=0;step<=ChestCompositeRules.Steps;step++)
        {
            uint opaque=ChestCompositeRules.Blend(0xff0000ff,0xffff0000,step);
            Check((opaque>>24)==255,"opaque crossfade never leaks background");
            uint edge=ChestCompositeRules.Blend(0x0000ff00,0xff0000ff,step);
            Check(step==0?edge==0:(edge&255)==255&&((edge>>8)&255)==0,"transparent pixel hidden RGB cannot bleed into composite");
        }
        Check(ChestCompositeRules.Blend(0xff0000ff,0xffff0000,0)==0xff0000ff&&ChestCompositeRules.Blend(0xff0000ff,0xffff0000,8)==0xffff0000,"exact image endpoints");
        Check(ChestRevealPresentation.Travel(0)==0&&ChestRevealPresentation.Travel(1)==1,"skip and animation share exact destination");
        for(int i=0;i<=100;i++)Check(ChestRevealPresentation.Travel(i/100f)>=0&&ChestRevealPresentation.Travel(i/100f)<=1,"bounded movement");
        var view=new CollectionViewingState();FashionData wing=new FashionData{id="wing",slot=FashionSlot.Wings},weapon=new FashionData{id="blade",slot=FashionSlot.Weapon};
        view.TryOn(wing);view.Rotate(45);view.View(CollectionPreviewComposition.Weapon);view.TryOn(weapon);view.Rotate(-45);
        Check(view.Trial(FashionSlot.Wings)==wing&&view.Trial(FashionSlot.Weapon)==weapon,"both trials survive part selection");
        view.View(CollectionPreviewComposition.Back);view.Rotate(90);view.View(CollectionPreviewComposition.Full);Check(view.Yaw==65,"full view keeps independent manual angle");
        view.View(CollectionPreviewComposition.Weapon);Check(view.Yaw==335,"weapon view retains wrapped angle");view.View(CollectionPreviewComposition.Back);Check(view.Yaw==250,"back view retains angle");
        view.Restore();Check(view.Trial(FashionSlot.Wings)==null&&view.Trial(FashionSlot.Weapon)==null&&view.Yaw==250,"restore appearance does not move camera");
        view.Reset();Check(view.Mode==CollectionPreviewComposition.Full&&view.Yaw==20,"owner replacement resets bounded presentation state");
        var legacy=new ChestReward{gold=10,rarityIndex=-1};Check(ChestRevealPresentation.Result(legacy,12).Contains("未保存实际增量"),"legacy amounts never fabricated");
        var capped=new ChestReward{gold=800,rarityIndex=-1,hasCurrencyDeltas=true,goldDelta=1,threadsDelta=0};string result=ChestRevealPresentation.Result(capped,999999);
        Check(result.Contains("到账 +1 金币 · +0 星纹")&&!result.Contains("可在营地自选传说"),"exact delta does not infer collection eligibility from balance alone");
        var profile=new GameProfile{fashionThreads=30};
        Check(ChestRevealPresentation.ResultWithCollection(capped,profile).Contains("可在营地兑换缺少的传说兵装 / 羽翼"),"funded missing collection offers exact gaps");
        profile.fashions.Add(new FashionData{slot=FashionSlot.Weapon,rarity=Rarity.Legendary});
        Check(ChestRevealPresentation.LegendaryExchangeHint(profile)=="可在营地兑换缺少的传说羽翼","one owned legendary narrows eligible part");
        profile.fashions.Add(new FashionData{slot=FashionSlot.Wings,rarity=Rarity.Legendary});
        Check(ChestRevealPresentation.ResultWithCollection(capped,profile).Contains("两部位传说已收藏")&&!ChestRevealPresentation.LegendaryExchangeHint(profile).Contains("兑换缺少"),"full legendary collection does not promise unavailable exchange");
        return "PASS: "+n+" composite, transition and independent viewing state checks";
    }
}
