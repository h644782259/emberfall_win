using System;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using Emberfall;
using UnityEngine;

public static class ProgressionGrowthTests
{
    private static int assertions;
    private static void Check(bool value, string message) { assertions++; if (!value) throw new Exception(message); }
    private static bool Near(float a, float b) { return Math.Abs(a-b) < .0001f; }
    private static string State(ProgressionService p) { return JsonUtility.ToJson(p.Profile,true); }
    private static ProgressionService Fresh(string root, HeroClass hero = HeroClass.Arcanist)
    {
        var p = new ProgressionService(Path.Combine(root,Guid.NewGuid().ToString("N")));
        Check(p.CreateNewSlot(hero),"fake character created"); return p;
    }
    public static string Run(string directory)
    {
        assertions=0;
        string root=Path.Combine(directory,"progression-growth-"+Guid.NewGuid().ToString("N"));
        CoreRuntime(); RefundAndMigration(root); Ascension(root); TierAndClear(root);
        return assertions+" mastery/refund/ascension/tier transaction assertions passed";
    }
    private static void CoreRuntime()
    {
        Check(MasteryCoreRules.Tier(9)==0&&MasteryCoreRules.Tier(10)==1&&MasteryCoreRules.Tier(19)==1&&MasteryCoreRules.Tier(20)==2,"ten/twenty exact core boundaries");
        foreach(int rank in new[]{10,20})
        {
            bool enhanced=rank==20; var core=new MasteryCoreRuntime();
            core.Configure((int)MasteryType.Offense,rank);
            Check(core.BasicHit()==0&&core.PerfectDodge()==0&&core.DamageTaken(.1f)==0&&core.SkillSpent(120).Energy==0,"offense only follows combo events");
            core.SkillHit(1);Check(Near(core.BasicHit(),enhanced?1f:.6f)&&core.BasicHit()==0,"skill arms one confirmed basic follow-up");
            core.SkillHit(2);core.Advance(enhanced?4:6);core.SkillHit(2);core.SkillHit(1);
            Check(core.BasicHit()==0,"casts first seen during cooldown and stale DoT cannot rearm");
            core.SkillHit(3);core.Configure((int)MasteryType.Offense,rank+1);
            core.Advance(5.99f);Check(core.BasicHit()>0,"same-tier stat refresh preserves six-second opportunity");
            core.Advance(enhanced?4:6);core.SkillHit(4);core.Advance(6);
            Check(core.BasicHit()==0,"expired combo cannot be spent");
            core.SkillHit(5);core.Advance(float.NaN);core.Advance(float.PositiveInfinity);core.Advance(-1);
            Check(core.BasicHit()>0,"invalid time never advances or poisons valid opportunity");
            core.Reset();core.SkillHit(1);Check(core.BasicHit()>0,"new combat epoch resets bounded cast history");
            core.Configure((int)MasteryType.Vitality,rank);
            Check(core.DamageTaken(0)==0&&core.DamageTaken(-.1f)==0&&core.DamageTaken(.5f)==0&&core.DamageTaken(float.NaN)==0,"vitality cannot revive or proc above threshold");
            Check(Near(core.DamageTaken(.49f),enhanced?.05f:.03f)&&core.DamageTaken(.1f)==0,"low-health damage heals once per cooldown");
            core.Configure((int)MasteryType.Vitality,rank);core.Advance((enhanced?10:12)-.01f);
            Check(core.DamageTaken(.2f)==0,"routine configure cannot bypass healing cooldown");
            core.Advance(.02f);Check(core.DamageTaken(.2f)>0,"healing cooldown expires normally");
            core.Configure((int)MasteryType.Guard,rank);
            Check(core.DamageTaken(.1f)==0&&core.BasicHit()==0&&core.SkillSpent(120).Energy==0,"guard does not share other triggers");
            Check(Near(core.PerfectDodge(),enhanced?3:2)&&Near(core.WardReduction,enhanced?.25f:.15f)&&core.PerfectDodge()==0,"ward duration/reduction and repeat-dodge bound");
            core.Advance(enhanced?6:8);Check(core.PerfectDodge()>0,"ward internal cooldown recovers");
            core.Configure((int)MasteryType.Technique,rank);
            float threshold=enhanced?45:60;
            Check(core.SkillSpent(threshold-1).Energy==0&&core.PerfectDodge()==0&&core.DamageTaken(.1f)==0,"technique requires real resource cost");
            MasteryResourceProc result=core.SkillSpent(1);
            Check(Near(result.Energy,enhanced?12:8)&&Near(result.CooldownReduction,enhanced?.7f:.4f),"exact resource threshold yields bounded refund/cooldown");
            for(int i=0;i<1000;i++)Check(core.SkillSpent(float.MaxValue).Energy==0,"resource spam cannot bank or repeat during cooldown");
            core.Advance(enhanced?6:8);
            Check(core.SkillSpent(1).Energy==0,"cooldown spending was discarded, not banked");
            Check(core.SkillSpent(float.NaN).Energy==0&&core.SkillSpent(float.PositiveInfinity).Energy==0&&core.SkillSpent(-999).Energy==0,"invalid cost has no proc");
            Check(core.SkillSpent(threshold-1).Energy>0,"new lawful costs can proc after cooldown");
            core.Configure(99,rank);Check(core.Tier==0&&core.Core==-1&&core.WardReduction==0&&core.SkillSpent(120).Energy==0,"invalid core disabled");
            core.Configure(0,9);core.SkillHit(99);Check(core.BasicHit()==0,"underinvested core disabled");
        }
    }
    private static void RefundAndMigration(string root)
    {
        var p=Fresh(root);p.Profile.level=100;p.Profile.skillRanks=Enumerable.Repeat(3,GameBalance.SkillCount).ToArray();
        p.Profile.masteryRevision=1;p.Profile.masteryRanks[0]=20;p.Profile.masteryCore=0;p.Save();
        int free=p.Profile.skillPoints;string bar=string.Join(",",p.Profile.equippedSkills),before=State(p),disk=File.ReadAllText(p.SaveFilePath),backup=File.ReadAllText(p.SaveFilePath+".bak");
        Check(p.RefundableSkillRanks==20&&!p.RefundSkillRanks(false)&&State(p)==before,"only learned rank2/3 refundable, camp required");
        int events=0;p.Changed+=()=>events++;GameProfile original=p.Profile;
        Directory.CreateDirectory(p.SaveFilePath+".tmp");
        Check(!p.RefundSkillRanks(true)&&State(p)==before&&ReferenceEquals(original,p.Profile)&&events==0,"failed skill refund is atomic");
        Check(File.ReadAllText(p.SaveFilePath)==disk&&File.ReadAllText(p.SaveFilePath+".bak")==backup,"failed refund keeps both files");
        Directory.Delete(p.SaveFilePath+".tmp");
        Check(p.RefundSkillRanks(true)&&p.Profile.skillPoints==free+20&&p.Profile.skillRanks.All(r=>r==1)&&events==1,"all upgrade ranks refunded exactly");
        Check(p.Profile.masteryRanks[0]==20&&p.Profile.masteryCore==0&&string.Join(",",p.Profile.equippedSkills)==bar,"refund retains learned tree, hotbar and chosen mastery");
        before=State(p);disk=File.ReadAllText(p.SaveFilePath);
        Check(!p.RefundSkillRanks(true)&&State(p)==before&&File.ReadAllText(p.SaveFilePath)==disk,"repeat refund cannot mint points or rotate files");
        Check(p.LoadSlot(p.CurrentSlotId)&&p.Profile.skillPoints==free+20&&p.Profile.skillRanks.All(r=>r==1),"refund survives reload without repeated migration");
        Check(p.ResetMastery(true)&&p.Profile.skillPoints==89&&p.Profile.masteryCore==-1&&p.Profile.skillRanks.All(r=>r==1),"mastery reset remains independent with exact shared budget");
        p=Fresh(root);p.Profile.level=50;p.Save();for(int i=0;i<10;i++)Check(p.LearnMastery(MasteryType.Guard),"ten lawful mastery points learned");
        Check(p.SelectMasteryCore(MasteryType.Guard,true)&&p.MasteryCoreTier(MasteryType.Guard)==1,"weak core is selectable at ten");
        Check(p.LoadSlot(p.CurrentSlotId)&&p.HasMasteryCore(MasteryType.Guard)&&p.MasteryCoreTier(MasteryType.Guard)==1,"new ten-point selection survives save repair");
        p.Profile.level=65;p.Save();for(int i=0;i<10;i++)Check(p.LearnMastery(MasteryType.Guard),"enhanced core investment learned");
        Check(p.MasteryCoreTier(MasteryType.Guard)==2&&!p.HasMasteryCore(MasteryType.Offense),"twenty upgrades same selected core without enabling others");
        JsonObject save=JsonNode.Parse(File.ReadAllText(p.SaveFilePath)).AsObject();save["profile"].AsObject().Remove("pendingChestTier");save["profile"].AsObject().Remove("lastDungeonRewardId");
        File.WriteAllText(p.SaveFilePath,save.ToJsonString());
        Check(p.LoadSlot(p.CurrentSlotId)&&p.Profile.pendingChestTier==1&&p.Profile.lastDungeonRewardId==null&&p.MasteryCoreTier(MasteryType.Guard)==2,"new fields migrate safely without refunding existing valid core");
    }
    private static void Ascension(string root)
    {
        foreach(HeroClass hero in Enum.GetValues(typeof(HeroClass)))
        foreach(EquipmentMechanic mechanic in BuildCatalog.MechanicsFor(hero))
        {
            var p=Fresh(root,hero);p.Profile.level=20;p.Save();ItemData item=p.CreateMechanicItem(mechanic);
            Check(p.CollectLoot(item)&&p.Equip(item.id),"known epic mechanism equipped");
            p.Profile.slotUpgradeRanks[(int)item.slot]=7;p.Profile.mechanicMaterials=24;p.Profile.bestFloor=5;
            if(hero==HeroClass.Arcanist){p.Profile.variantKnowledge.Add(mechanic);item.mechanicVariantUnlocked=true;item.mechanicVariant=1;}
            p.Save();item=p.Profile.inventory.Find(i=>i.id==item.id);
            string id=item.id,name=item.name;int slotRank=p.SlotUpgradeRank(item.slot),attack=item.baseAttack,defense=item.baseDefense,health=item.baseHealth,variant=item.mechanicVariant;
            bool variantUnlocked=item.mechanicVariantUnlocked,locked=item.locked;int level=item.level;
            string before=State(p),disk=File.ReadAllText(p.SaveFilePath),backup=File.ReadAllText(p.SaveFilePath+".bak");int events=0;p.Changed+=()=>events++;
            Check(!p.AscendMechanic(id,false)&&State(p)==before,"ascension requires camp");
            p.Profile.bestFloor=4;p.Profile.highestAdventureTier=4;Check(!p.AscendMechanic(id,true),"shared fifth-tier milestone enforced");
            p.Profile.highestAdventureTier=5;Check(string.IsNullOrEmpty(p.AscensionLockReason(id,true)),"alternate-mode shared fifth tier opens ascension without ordinary fifth tier");p.Profile.bestFloor=5;
            p.Profile.mechanicMaterials=23;Check(!p.AscendMechanic(id,true),"ascension cost checked before mutation");p.Profile.mechanicMaterials=24;
            Directory.CreateDirectory(p.SaveFilePath+".tmp");
            Check(!p.AscendMechanic(id,true)&&State(p)==before&&events==0,"failed ascension retains complete profile and no event");
            Check(File.ReadAllText(p.SaveFilePath)==disk&&File.ReadAllText(p.SaveFilePath+".bak")==backup,"failed ascension preserves original and backup");
            Directory.Delete(p.SaveFilePath+".tmp");
            Check(p.AscendMechanic(id,true)&&p.Profile.mechanicMaterials==0&&events==1,"ascension spends exactly24 only after commit");
            item=p.Profile.inventory.Find(i=>i.id==id);
            Check(item.rarity==Rarity.Legendary&&item.name==name&&item.level==level&&item.mechanic==mechanic&&item.locked==locked&&
                item.mechanicVariant==variant&&item.mechanicVariantUnlocked==variantUnlocked&&p.SlotUpgradeRank(item.slot)==slotRank&&item.upgradeLevel==slotRank,
                "identity, name, level, mechanism, lock, variant and permanent slot rank retained");
            Check(item.baseAttack==(attack*25+17)/18&&item.baseDefense==(defense*25+17)/18&&item.baseHealth==(health*25+17)/18,"ascension deterministically scales existing base without reroll");
            before=State(p);disk=File.ReadAllText(p.SaveFilePath);
            Check(!p.AscendMechanic(id,true)&&State(p)==before&&File.ReadAllText(p.SaveFilePath)==disk,"repeat legendary ascension cannot compound stats or spend twice");
            Check(p.LoadSlot(p.CurrentSlotId)&&p.Profile.inventory.Find(i=>i.id==id).baseAttack==item.baseAttack&&p.SlotUpgradeRank(item.slot)==7,"ascension remains stable across validation and reload");
        }
        var invalid=Fresh(root);invalid.Profile.level=30;invalid.Profile.bestFloor=5;invalid.Profile.mechanicMaterials=24;invalid.Save();
        ItemData wrong=invalid.CreateMechanicItem(EquipmentMechanic.ReturningBlade);invalid.CollectLoot(wrong);
        Check(!invalid.AscendMechanic(wrong.id,true)&&invalid.Profile.mechanicMaterials==24,"foreign-class mechanism cannot ascend");
        ItemData ordinary=invalid.Profile.inventory.Find(i=>i.id==invalid.Profile.weaponId);ordinary.rarity=Rarity.Epic;
        Check(!invalid.AscendMechanic(ordinary.id,true),"ordinary epic is not a known mechanism");
    }
    private static void TierAndClear(string root)
    {
        int[] tiers={1,5,10,20,40,100};int lastBossLegend=0,lastOrdinaryEpic=0;
        foreach(int tier in tiers)
        {
            int[] boss=new int[4],ordinary=new int[4];
            for(int roll=0;roll<100;roll++){boss[(int)TierRewardRules.DropRarity(true,tier,roll)]++;ordinary[(int)TierRewardRules.DropRarity(false,tier,roll)]++;}
            Check(boss.Sum()==100&&ordinary.Sum()==100&&boss[0]==0,"tier odds exhaust exactly100 outcomes");
            Check(boss[3]>=lastBossLegend&&ordinary[2]>=lastOrdinaryEpic&&boss[3]<=20&&ordinary[3]<=6,"rarity progression monotonic and capped");
            Check(TierRewardRules.ClearMaterials(tier)>=3&&TierRewardRules.ClearMaterials(tier)<=7,"material rewards bounded");
            lastBossLegend=boss[3];lastOrdinaryEpic=ordinary[2];
        }
        Check(TierRewardRules.DropRarity(true,1,54)==Rarity.Rare&&TierRewardRules.DropRarity(true,1,55)==Rarity.Epic&&TierRewardRules.DropRarity(true,1,92)==Rarity.Legendary,"tier1 preserves previous boss distribution");
        Check(TierRewardRules.ClearMaterials(int.MaxValue)==7&&TierRewardRules.ClearMaterials(int.MinValue)==3,"bad tier inputs bounded");
        var p=Fresh(root);string receipt=Guid.NewGuid().ToString("N"),before=State(p),disk=File.ReadAllText(p.SaveFilePath),backup=File.ReadAllText(p.SaveFilePath+".bak");
        int events=0,levels=0;p.Changed+=()=>events++;p.LeveledUp+=_=>levels++;
        Directory.CreateDirectory(p.SaveFilePath+".tmp");
        Check(!p.TryCompleteDungeonRun(receipt,5,270,200)&&State(p)==before&&events==0&&levels==0,"ordinary clear rollback covers gold, XP, level, milestone, materials and entitlements");
        Check(File.ReadAllText(p.SaveFilePath)==disk&&File.ReadAllText(p.SaveFilePath+".bak")==backup,"failed completion preserves durable bytes");
        Directory.Delete(p.SaveFilePath+".tmp");
        Check(p.TryCompleteDungeonRun(receipt,5,270,200)&&events==1&&p.Profile.gold==330&&p.Profile.clearedRuns==1&&p.Profile.bestFloor==5&&
            p.Profile.mechanicMaterials==0&&p.Profile.pendingFashionChest&&p.Profile.pendingChestTier==5&&p.Profile.pendingFirstClearReward&&p.Profile.lastDungeonRewardId==receipt,"ordinary completion publishes all rewards together");
        Check(levels==p.Profile.level-1,"levelup events emitted after successful commit only");
        before=State(p);disk=File.ReadAllText(p.SaveFilePath);backup=File.ReadAllText(p.SaveFilePath+".bak");int previousEvents=events;
        for(int i=0;i<20;i++)Check(p.TryCompleteDungeonRun(receipt,5,270,200),"confirmed receipt retry succeeds idempotently");
        Check(p.TryCompleteDungeonRun(receipt.ToUpperInvariant(),5,270,200),"receipt identity is case-normalized");
        Check(State(p)==before&&File.ReadAllText(p.SaveFilePath)==disk&&File.ReadAllText(p.SaveFilePath+".bak")==backup&&events==previousEvents,"duplicate completion never pays or rotates backups");
        Check(!p.TryCompleteDungeonRun(Guid.NewGuid().ToString("N"),40,100,0)&&State(p)==before,"new reward cannot replace unopened old entitlement");
        Check(p.LoadSlot(p.CurrentSlotId)&&p.TryCompleteDungeonRun(receipt,5,270,200)&&p.Profile.clearedRuns==1,"receipt survives reload");
        Check(p.PrepareDungeonChest(100)&&p.Profile.pendingChestTier==5&&p.Profile.mechanicMaterials==0,"repeat preparation cannot upgrade earned tier or regrant materials");
        Check(p.OpenDungeonChest()!=null&&p.LastChestReward.Gold>=85&&p.Profile.mechanicMaterials==AdventureRewardRules.Materials(-1,5)&&p.AcknowledgeChestReward(),"storedtier controls chest gold");
        receipt=Guid.NewGuid().ToString("N");Check(p.TryCompleteDungeonRun(receipt,40,100,0)&&p.Profile.mechanicMaterials==AdventureRewardRules.Materials(-1,5)&&p.Profile.bestFloor==40&&p.Profile.pendingChestTier==40,"next real clear captures tier and defers materials until opening");
        p.Profile.bestFloor=1;p.Save(); // Menus/profile milestone cannot down-reroll the pending reward.
        Check(p.OpenDungeonChest()!=null&&p.LastChestReward.Gold>=160&&p.LastChestReward.Gold<=1000&&p.Profile.mechanicMaterials==AdventureRewardRules.Materials(-1,5)+AdventureRewardRules.Materials(-1,40),"chest uses captured tier and grants deferred materials once");
        string chest=p.LastChestReward.Id;disk=File.ReadAllText(p.SaveFilePath);
        Check(p.OpenDungeonChest()==null&&p.LastChestReward.Id==chest&&File.ReadAllText(p.SaveFilePath)==disk,"repeated reveal cannot reroll reward");
        p.LastChestReward.gold=1000;p.Save();Check(p.LoadSlot(p.CurrentSlotId)&&p.LastChestReward!=null&&p.LastChestReward.Gold==1000,"new legitimate high-tier duplicate-gold receipt survives migration bound");
        before=State(p);Check(!p.TryCompleteDungeonRun("bad",1,1,1)&&!p.TryCompleteDungeonRun(Guid.NewGuid().ToString("N"),101,1,1)&&!p.TryCompleteDungeonRun(Guid.NewGuid().ToString("N"),1,-1,1)&&State(p)==before,"invalid receipt,tier,budget rejected");
        var mode=Fresh(root);string modeReceipt=Guid.NewGuid().ToString("N");
        Check(mode.TryGrantModeReward(modeReceipt.ToUpperInvariant(),10,0,1),"mode receipt canonicalized on first grant");
        before=State(mode);disk=File.ReadAllText(mode.SaveFilePath);
        Check(mode.TryGrantModeReward(modeReceipt,10,0,1)&&State(mode)==before&&File.ReadAllText(mode.SaveFilePath)==disk,"case variant cannot duplicate an arena reward either");
    }
}
