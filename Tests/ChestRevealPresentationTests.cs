using System;
using System.IO;
using Emberfall;
using UnityEngine;
public static class ChestRevealPresentationTests
{
    static int checks;
    static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
    public static string Run(string root)
    {
        checks=0;
        foreach(var kind in new[]{ChestResultKind.Gold,ChestResultKind.FirstCollection,ChestResultKind.Duplicate})
        {
            var reward=new ChestReward{id="saved-receipt",choice=2,gold=140,rarityIndex=kind==ChestResultKind.Gold?-1:3,slotIndex=0,name="烬王之翼",duplicate=kind==ChestResultKind.Duplicate};
            string before=JsonUtility.ToJson(reward,false),result=ChestRevealPresentation.Result(reward,37);
            Check(ChestRevealPresentation.Kind(reward)==kind,"three committed outcome categories");
            Check(result.Contains("140")&&result.Contains("37")&&result.Contains(ChestRevealPresentation.Outcome(reward)),"full gold, thread balance and outcome");
            if(kind!=ChestResultKind.Gold)Check(result.Contains(reward.name)&&result.Contains(GameBalance.RarityName(Rarity.Legendary)),"collection identity and rarity remain visible");
            for(int frame=0;frame<=110;frame++)
            {
                float progress=ChestRevealPresentation.Progress(frame*.01f,1.1f);
                Check(progress>=0&&progress<=1,"bounded animation progress");
                Check(ChestRevealPresentation.Result(reward,37)==result&&JsonUtility.ToJson(reward,false)==before,"animation never mutates or truncates committed reward");
            }
            Check(ChestRevealPresentation.Progress(1.1f,1.1f)==1&&ChestRevealPresentation.Result(reward,37)==result,"skip exposes same full result");
        }
        Check(ChestRevealPresentation.UnselectedOpacity(0)==1&&ChestRevealPresentation.UnselectedOpacity(.2f)==.5f&&ChestRevealPresentation.UnselectedOpacity(.4f)==0&&ChestRevealPresentation.UnselectedOpacity(1)==0,"unselected choices fade away before reveal ends");
        Check(ChestRevealPresentation.DesktopArtSize(342)==320&&ChestRevealPresentation.DesktopArtSize(292)==292,"desktop result uses 280-320px on supported desktop body sizes");
        var p=new ProgressionService(Path.Combine(root,Guid.NewGuid().ToString("N")));
        Check(p.CreateNewSlot(HeroClass.Vanguard),"real saved character");p.PrepareDungeonChest();
        Directory.CreateDirectory(p.SaveFilePath+".tmp");
        Check(p.OpenDungeonChest()==null&&p.Profile.pendingFashionChest&&!p.Profile.pendingChestReveal,"failed opening preserves choice eligibility");Directory.Delete(p.SaveFilePath+".tmp");
        Check(p.OpenDungeonChest()!=null&&p.Profile.pendingChestReveal,"successful explicit choice persists receipt");
        string live=JsonUtility.ToJson(p.Profile,false),disk=File.ReadAllText(p.SaveFilePath),full=ChestRevealPresentation.Result(p.LastChestReward,p.Profile.fashionThreads),id=p.LastChestReward.Id;
        for(int frame=0;frame<120;frame++){ChestRevealPresentation.Progress(frame/60f,1.1f);ChestRevealPresentation.Result(p.LastChestReward,p.Profile.fashionThreads);}
        Check(JsonUtility.ToJson(p.Profile,false)==live&&File.ReadAllText(p.SaveFilePath)==disk,"presentation has no persistence or currency side effects");
        Check(p.OpenDungeonChest()==null&&p.LastChestReward.Id==id,"unselected chest cannot reroll committed result");
        var reload=new ProgressionService(Path.GetDirectoryName(p.SaveFilePath));Check(reload.LoadSlot(p.CurrentSlotId),"restart loads receipt");
        Check(reload.Profile.pendingChestReveal&&reload.LastChestReward.choice==-1&&ChestRevealPresentation.Result(reload.LastChestReward,reload.Profile.fashionThreads)==full,"restart restores full exact result, not choices");
        Directory.CreateDirectory(reload.SaveFilePath+".tmp");Check(!reload.AcknowledgeChestReward()&&reload.Profile.pendingChestReveal&&reload.LastChestReward.Id==id,"failed acknowledgement keeps large result retryable");Directory.Delete(reload.SaveFilePath+".tmp");
        Check(reload.AcknowledgeChestReward()&&!reload.Profile.pendingChestReveal,"acknowledgement closes only after durable success");
        return checks+" chest presentation and real receipt lifecycle checks passed";
    }
}
