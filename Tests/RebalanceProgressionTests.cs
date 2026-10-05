using System;
using System.IO;
using System.Text.Json.Nodes;
using Emberfall;

public static class RebalanceProgressionTests
{
    private static int checks, cases;
    private static string root;
    private static void Check(bool ok, string message) { checks++; if (!ok) throw new Exception("Rebalance: " + message); }
    private static bool Near(float a, float b) { return Math.Abs(a-b) < .01f; }
    private static ProgressionService Fresh(HeroClass hero = HeroClass.Arcanist)
    { var p = new ProgressionService(Path.Combine(root,"rebalance-"+(++cases))); p.NewGame(hero); return p; }
    private static ProgressionService Reload(ProgressionService p)
    { var next = new ProgressionService(p.SaveDirectory); Check(next.Load(),"reload"); return next; }
    public static string Run(string directory)
    {
        root=directory; checks=cases=0;
        SegmentAndMigration(); RecipesAndAtomicCosts(); CosmeticCollection(); ProductionStatBudget();
        return "PASS: "+checks+" rebalance progression assertions in "+cases+" isolated scenarios.";
    }
    private static void SegmentAndMigration()
    {
        foreach(int level in new[]{30,34,35,40,49,50,64,65,79,80,94,95,100})
        {
            var p=Fresh(); p.Profile.level=level; p.Save(); int expected=level<30?0:level<35?5:level<50?10:level<65?15:level<80?20:level<95?30:35;
            Check(ProgressionService.MasteryCap(level)==expected,"segmented unlock boundary");
            for(int i=0;i<expected;i++)Check(p.LearnMastery(MasteryType.Technique),"segment accepts lawful points");
            Check(!p.LearnMastery(MasteryType.Technique),"segment limit enforced");
            Check(p.Profile.skillPoints+p.Profile.masteryRanks[3]+p.Profile.skillRanks[0]==level-1,"lifetime point conservation");
            Check(Reload(p).Profile.masteryRanks[3]==expected,"segment stable after reload");
        }
        var old=Fresh();old.Profile.level=100;for(int i=0;i<10;i++)old.Profile.skillRanks[i]=3;old.Save();
        JsonNode save=JsonNode.Parse(File.ReadAllText(old.SaveFilePath));save["profile"]["masteryRanks"]=new JsonArray(23,23,23);
        save["profile"].AsObject().Remove("masteryRevision"); save["profile"]["masteryCore"]=0;
        File.WriteAllText(old.SaveFilePath,save.ToJsonString());old=Reload(old);
        Check(old.Profile.skillPoints==0&&old.Profile.masteryRanks[0]==23&&old.Profile.masteryRanks[1]==23&&old.Profile.masteryRanks[2]==23&&old.Profile.masteryRanks[3]==0&&old.Profile.masteryCore==0,"old three-track legal investments and core preserved");
        Check(!old.LearnMastery(MasteryType.Guard)&&Reload(old).Profile.masteryRanks[2]==23,"full budget is not minted or erased on reload");
    }
    private static void RecipesAndAtomicCosts()
    {
        var p=Fresh();p.Profile.level=20;p.Profile.mechanicMaterials=100;p.Profile.gold=9999;p.Save();
        ItemData core=p.CreateMechanicItem(EquipmentMechanic.FrostEcho);Check(p.CollectLoot(core)&&p.Equip(core.id),"acquire recipe and equip");
        p.Profile.slotUpgradeRanks[2]=5;p.Profile.level=50;p.Save();
        string id=core.id;int mats=p.Profile.mechanicMaterials;
        Check(!p.ReforgeMechanic(id,false)&&p.Profile.mechanicMaterials==mats,"field reforge denied free");
        Directory.CreateDirectory(p.SaveFilePath+".tmp");int events=0;p.Changed+=()=>events++;
        Check(!p.ReforgeMechanic(id,true)&&p.Profile.mechanicMaterials==mats&&p.Equipped(ItemSlot.Relic).level==20&&events==0,"failed reforge rolls back item currency and event");
        Check(!p.ToggleMechanicVariant(id,true)&&!p.Equipped(ItemSlot.Relic).mechanicVariantUnlocked&&events==0,"failed variant unlock rolls back");
        Check(!p.LearnMastery(MasteryType.Guard)&&p.Profile.masteryRanks[2]==0&&events==0,"failed mastery allocation rolls back");
        Directory.Delete(p.SaveFilePath+".tmp");
        Check(p.ReforgeMechanic(id,true),"reforge succeeds"); core=p.Equipped(ItemSlot.Relic);
        Check(core.id==id&&core.level==50&&core.upgradeLevel==5&&core.mechanic==EquipmentMechanic.FrostEcho&&core.locked,"reforge preserves identity slot rank mechanic and lock");
        Check(p.Profile.mechanicMaterials==mats&&p.HasDiscoveredMechanic(EquipmentMechanic.FrostEcho),"reforge leaves fragments intact");
        Check(!p.ReforgeMechanic(id,true)&&p.Profile.mechanicMaterials==mats,"samelevel cannot spend materials");
        Check(p.ToggleMechanicVariant(id,true)&&p.Profile.mechanicMaterials==mats-4,"unlock alternate costs4 once");
        Check(p.ToggleMechanicVariant(id,true)&&p.Profile.mechanicMaterials==mats-4&&p.Equipped(ItemSlot.Relic).mechanicVariant==0,"free subsequent variant toggle");
        p=Reload(p);Check(p.Equipped(ItemSlot.Relic).mechanicVariantUnlocked&&p.Equipped(ItemSlot.Relic).level==50,"recipe upgrade/variant survive load");
        Check(p.PreviewEquippedItem(p.Equipped(ItemSlot.Relic)).mechanicVariantUnlocked,"preview keeps variant contract");
    }
    private static void CosmeticCollection()
    {
        var p=Fresh();StatBlock baseline=p.GetStats();
        p.Profile.fashionThreads=30;p.Save();
        Check(!p.ChooseLegendaryFashion(FashionSlot.Wings,false)&&p.Profile.fashionThreads==30,"choice camp-only");
        Directory.CreateDirectory(p.SaveFilePath+".tmp");
        Check(!p.ChooseLegendaryFashion(FashionSlot.Wings,true)&&p.Profile.fashionThreads==30&&p.Profile.fashions.Count==0,"choice failed write atomic");
        Directory.Delete(p.SaveFilePath+".tmp");
        Check(p.ChooseLegendaryFashion(FashionSlot.Wings,true)&&p.Profile.fashionThreads==0,"30 deterministic threads buy specified missing slot");
        Check(Near(p.GetStats().MaxHealth,baseline.MaxHealth*1.12f)&&p.EquippedFashion(FashionSlot.Wings)==null,"collection grants stats independently of appearance");
        p.Profile.fashions.Add(new FashionData{id="fashion-0-0",slot=FashionSlot.Wings,rarity=Rarity.Common});p.Save();
        Check(p.EquipFashion("fashion-0-0")&&Near(p.GetStats().MaxHealth,baseline.MaxHealth*1.12f),"common appearance keeps legendary cultivation");
        Check(p.UnequipFashion(FashionSlot.Wings)&&Near(p.GetStats().MaxHealth,baseline.MaxHealth*1.12f),"hidden appearance keeps owned stats");
        p.Profile.fashionThreads=30;p.Save();Check(!p.ChooseLegendaryFashion(FashionSlot.Wings,true)&&p.Profile.fashionThreads==30,"alreadyowned choice does not spend");
        for(int i=0;i<5;i++)
        {
            p.PrepareDungeonChest();int prior=p.Profile.fashionThreads;
            Check(p.OpenDungeonChest()!=null&&p.Profile.fashionThreads>=prior+1,"every chest progresses deterministic accumulation");
            int after=p.Profile.fashionThreads;Check(p.OpenDungeonChest()==null&&p.Profile.fashionThreads==after,"receipt repeated click cannot mint threads");
            Check(p.AcknowledgeChestReward(),"close receipt");
        }
        Check(Reload(p).Profile.fashionThreads==p.Profile.fashionThreads,"accumulation persists");
    }
    private static void ProductionStatBudget()
    {
        var p=Fresh(HeroClass.Vanguard);p.Profile.level=100;p.Profile.skillRanks[3]=3;
        var weapon=p.Equipped(ItemSlot.Weapon);var armor=p.Equipped(ItemSlot.Armor);var relic=p.Equipped(ItemSlot.Relic);
        weapon.attack=459;armor.defense=221;armor.health=738;relic.attack=184;relic.health=551;
        foreach(var item in p.Profile.inventory){item.upgradeBaseInitialized=false;item.upgradeLevel=0;item.balanceRevision=0;item.level=100;item.rarity=Rarity.Epic;}
        p.Profile.slotUpgradeRanks=new[]{10,10,10};p.Profile.slotUpgradesInitialized=true;p.Save();
        StatBlock stats=p.GetStats();Check(Near(stats.Damage,1564.04f)&&Near(stats.MaxHealth,4084)&&Near(stats.Armor,484.6f),"actual production same epic+10 profile recalculates exact baseline");
        float boss=CombatBalance.EnemyHealth(100,1,true,EnemyKind.Guardian);
        Check(stats.Damage*3.04f<boss*.1f,"rank3 spin opening cannot erase even tier1 boss");
        float hit=CombatBalance.EnemyDamage(100,1,true)*1.4f*CombatBalance.ArmorDamageMultiplier(stats.Armor,100);
        Check(hit/stats.MaxHealth>.1f&&hit/stats.MaxHealth<.2f,"same profile slam meaningful unguarded hit");
        Check(CombatBalance.EnemyHealth(100,100,true,EnemyKind.Guardian)>CombatBalance.EnemyHealth(100,11,true,EnemyKind.Guardian)*2,"tier100 no flatline");
        var saved=Reload(p).GetStats();Check(Near(saved.Damage,stats.Damage)&&Near(saved.Armor,stats.Armor),"budget migration idempotent");
    }
}
