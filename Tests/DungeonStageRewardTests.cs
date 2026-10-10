using System;
using System.IO;
using System.Diagnostics;
using Emberfall;
using UnityEngine;
public static class DungeonStageRewardTests
{
    static int checks;
    static void C(bool value,string why){checks++;if(!value)throw new Exception(why);}
    static ProgressionService Fresh(string root){var p=new ProgressionService(Path.Combine(root,Guid.NewGuid().ToString("N")));C(p.CreateNewSlot(HeroClass.Ranger),"create character");return p;}
    static long XP(GameProfile p){long total=p.xp;for(int i=1;i<p.level;i++)total+=GameBalance.XpToNext(i);return total;}
    static ItemData Item(int i){return new ItemData{id="stage-item-"+i,slot=ItemSlot.Weapon,rarity=Rarity.Rare,level=1,name="Stage reward",attack=4};}
    public static string Run(string root)
    {
        foreach(bool failure in new[]{false,true})
        {
            var p=Fresh(root);int gold=p.Profile.gold,items=p.Profile.inventory.Count,level=p.Profile.level;long xp=XP(p.Profile);int changes=0,levels=0;
            p.Changed+=()=>changes++;p.LeveledUp+=_=>levels++;
            string before=File.ReadAllText(p.SaveFilePath),backup=File.ReadAllText(p.SaveFilePath+".bak");
            p.BeginDungeonStage();int calls=JsonUtility.SerializationCount;
            for(int i=0;i<24;i++){p.RecordDungeonKill(10,20,1);C(p.CollectLoot(Item(i)),"drop buffered");p.Save();}
            C(p.CommitSkillStock(HeroClass.Ranger,1,0,1),"charge spending joins checkpoint");
            C(JsonUtility.SerializationCount==calls,"24 kills, drops, charge, saves do not serialize");
            C(File.ReadAllText(p.SaveFilePath)==before&&File.ReadAllText(p.SaveFilePath+".bak")==backup,"stage writes zero primary or backup files");
            C(p.Profile.gold==gold&&XP(p.Profile)==xp&&p.Profile.level==level&&p.Profile.inventory.Count==items&&changes==0&&levels==0,"no immediate loot, currency, XP, stat refresh or level event");
            if(failure)Directory.CreateDirectory(p.SaveFilePath+".tmp");
            C(p.SaveDungeonCheckpoint()==!failure,"checkpoint reports real storage result");
            if(failure){C(p.Profile.dungeonExperience==480&&File.ReadAllText(p.SaveFilePath)==before,"checkpoint failure retains ledger");Directory.Delete(p.SaveFilePath+".tmp");C(p.SaveDungeonCheckpoint(),"checkpoint retry");}
            string checkpoint=File.ReadAllText(p.SaveFilePath);
            C(File.ReadAllText(p.SaveFilePath+".bak")==before,"one stage rotates only the pre-stage backup");
            var recovered=new ProgressionService(p.SaveDirectory);C(recovered.LoadSlot(p.CurrentSlotId)&&recovered.Profile.dungeonLoot.Count==24&&recovered.Profile.level==level,"reload retains unclaimed drops without levelling");
            C(recovered.FinishDungeonRewards()&&XP(recovered.Profile)==xp+480&&recovered.Profile.dungeonLoot.Count==0,"interrupted run can settle recovered ledger");
            // Use the original in-memory state with a blocked write to test rollback.
            if(failure)Directory.CreateDirectory(p.SaveFilePath+".tmp");
            C(p.FinishDungeonRewards()==!failure,"result write status is honest");
            if(failure){C(p.Profile.gold==gold&&XP(p.Profile)==xp&&p.Profile.dungeonLoot.Count==24&&levels==0,"failed result cannot grant or clear rewards");Directory.Delete(p.SaveFilePath+".tmp");C(p.FinishDungeonRewards(),"result retry");}
            C(!p.DungeonStageActive&&p.Profile.gold==gold+240&&XP(p.Profile)==xp+480&&p.Profile.inventory.Count==items+24,"result pays all rewards once");
            C(p.Profile.dungeonExperience==0&&p.Profile.dungeonGold==0&&p.Profile.dungeonPotions==0&&p.Profile.dungeonLoot.Count==0,"result clears pending ledger atomically");
            C(changes==1&&levels==p.Profile.level-level,"one stat notification and one event per earned level");
            string paid=File.ReadAllText(p.SaveFilePath);C(p.FinishDungeonRewards()&&File.ReadAllText(p.SaveFilePath)==paid&&changes==1,"duplicate result is idle");
        }
        foreach(bool mode in new[]{false,true})
        {
            var p=Fresh(root);long xp=XP(p.Profile);int gold=p.Profile.gold;string old=File.ReadAllText(p.SaveFilePath),receipt=Guid.NewGuid().ToString("N");
            p.BeginDungeonStage();p.RecordDungeonKill(10,20,1);p.RecordDungeonLoot(Item(1));
            Func<bool> finish=()=>mode?p.TryGrantModeReward(receipt,100,100,3,1,0):p.TryCompleteDungeonRun(receipt,1,100,100);
            Directory.CreateDirectory(p.SaveFilePath+".tmp");C(!finish()&&p.Profile.dungeonExperience==20&&XP(p.Profile)==xp,"completion failure retains both reward sources");Directory.Delete(p.SaveFilePath+".tmp");
            C(finish()&&XP(p.Profile)==xp+120&&p.Profile.gold==gold+110&&!p.DungeonStageActive,"completion and kills share a single result transaction");
            C(File.ReadAllText(p.SaveFilePath+".bak")==old,"completion only writes once");string paid=File.ReadAllText(p.SaveFilePath);C(finish()&&File.ReadAllText(p.SaveFilePath)==paid,"completion receipt cannot double pay ledger");
        }
        var full=Fresh(root);for(int i=full.Profile.inventory.Count;i<ProgressionService.InventoryCapacity;i++)full.Profile.inventory.Add(Item(1000+i));full.Save();full.BeginDungeonStage();
        C(full.RecordDungeonLoot(Item(1))&&full.RecordDungeonLoot(Item(1))&&full.Profile.dungeonLoot.Count==1,"duplicate drop is not retained twice");
        C(full.FinishDungeonRewards()&&full.Profile.inventory.Exists(x=>x.id=="stage-item-1")&&full.Profile.inventory.Count==ProgressionService.InventoryCapacity+1,"full inventory retains settled equipment using existing visible overflow");
        foreach(bool staged in new[]{false,true})
        {
            var p=Fresh(root);if(staged)p.BeginDungeonStage();int start=JsonUtility.SerializationCount;var watch=Stopwatch.StartNew();
            for(int i=0;i<24;i++){if(staged)p.RecordDungeonKill(10,20,0);else p.GrantEnemyKillReward(10,20);p.CollectLoot(Item(i));p.Save();}
            watch.Stop();Console.WriteLine("BENCH "+(staged?"stage":"per-kill")+": 24 kills/drops, "+(JsonUtility.SerializationCount-start)+" serializations, "+watch.Elapsed.TotalMilliseconds.ToString("F2")+" ms (managed fixture, not frame time)");
        }
        return "PASS: "+checks+" dungeon stage reward assertions";
    }
}
