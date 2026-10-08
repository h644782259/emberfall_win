using System;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using Emberfall;
public static class ServiceIntegrationTests
{
    private static int assertions,scenarios;private static string root;
    private static void C(bool ok,string why){assertions++;if(!ok)throw new Exception(why);}
    private static ProgressionService Fresh(HeroClass hero=HeroClass.Arcanist)
    {var p=new ProgressionService(Path.Combine(root,"service-"+(++scenarios)));C(p.CreateNewSlot(hero),"create isolated profile");p.Profile.level=40;p.Profile.gold=999999;p.Profile.mechanicMaterials=100;p.Save();return p;}
    private static string State(ProgressionService p){return UnityEngine.JsonUtility.ToJson(p.Profile,true);}
    public static string Run(string directory)
    {
        root=Path.Combine(directory,"services-"+Guid.NewGuid().ToString("N"));assertions=scenarios=0;
        SmithQuotes();MerchantQuotes();MigrationAndAttachments();Layouts();
        return "PASS: "+assertions+" smith/merchant state and measured-layout assertions in "+scenarios+" isolated scenarios";
    }
    private static void SmithQuotes()
    {
        var p=Fresh();C(p.PrepareSmithUpgrade(ItemSlot.Weapon,false)==null,"smith quote requires service context");
        var quote=p.PrepareSmithUpgrade(ItemSlot.Weapon,true);int gold=p.Profile.gold;string snapshot=State(p),disk=File.ReadAllText(p.SaveFilePath);
        Directory.CreateDirectory(p.SaveFilePath+".tmp");C(!p.UpgradeAtSmith(quote,true)&&State(p)==snapshot&&File.ReadAllText(p.SaveFilePath)==disk,"failed smith write rolls back fee and rank");Directory.Delete(p.SaveFilePath+".tmp");
        C(!p.UpgradeAtSmith(quote,false)&&State(p)==snapshot,"cannot upgrade after leaving smith");
        C(p.UpgradeAtSmith(quote,true)&&p.Profile.gold==gold-quote.GoldCost&&p.SlotUpgradeRank(ItemSlot.Weapon)==1,"one quote spends exact fee for one rank");
        snapshot=State(p);C(!p.UpgradeAtSmith(quote,true)&&State(p)==snapshot,"duplicate/stale upgrade callback cannot spend twice");
        var nextQuote=p.PrepareSmithUpgrade(ItemSlot.Weapon,true);
        var other=new ItemData{id="new-distinct-weapon",name="不同基础武器",slot=ItemSlot.Weapon,rarity=Rarity.Rare,level=1,attack=51,health=8};
        C(p.CollectLoot(other)&&p.Equip(other.id),"replace selected equipped item");gold=p.Profile.gold;snapshot=State(p);
        C(!p.UpgradeAtSmith(nextQuote,true)&&State(p)==snapshot,"old equipment quote cannot upgrade replacement");
        int attack=p.Equipped(ItemSlot.Weapon).attack;
        string original=p.Profile.inventory.First(x=>x.slot==ItemSlot.Weapon&&x.id!=other.id).id;
        for(int i=0;i<5;i++){C(p.Equip(original)&&p.Equip(other.id)&&p.Equipped(ItemSlot.Weapon).attack==attack&&p.Profile.gold==gold,"free inheritance recomputes new base without compounding or fees");p.Save();C(p.LoadSlot(p.CurrentSlotId)&&p.Equipped(ItemSlot.Weapon).attack==attack,"reload preserves exact inherited strength");}
        C(p.SlotUpgradeRank(ItemSlot.Armor)==0&&p.SlotUpgradeRank(ItemSlot.Relic)==0,"independent slot investments never transfer to other slots");
    }
    private static void MerchantQuotes()
    {
        var p=Fresh();C(p.PrepareMerchantPurchase(EquipmentMechanic.None,false)==null,"purchase quote requires merchant context");
        var quote=p.PrepareMerchantPurchase(EquipmentMechanic.None,true);int potions=p.Profile.potions,gold=p.Profile.gold;string before=State(p);
        Directory.CreateDirectory(p.SaveFilePath+".tmp");C(!p.BuyAtMerchant(quote,true)&&State(p)==before,"failed purchase preserves currency and count");Directory.Delete(p.SaveFilePath+".tmp");
        C(!p.BuyAtMerchant(quote,false)&&State(p)==before,"purchase rejected after service context ends");
        C(p.BuyAtMerchant(quote,true)&&p.Profile.potions==potions+1&&p.Profile.gold==gold-ProgressionService.PotionPrice,"one purchase has exact price and quantity");before=State(p);C(!p.BuyAtMerchant(quote,true)&&State(p)==before,"duplicate purchase callback cannot repeat fee or potion");
        p.Profile.potions=99;p.Save();C(p.PrepareMerchantPurchase(EquipmentMechanic.None,true)==null,"quantity cap hides invalid purchase action");
        p.Profile.gold=0;p.Profile.potions=0;p.Save();C(p.PrepareMerchantPurchase(EquipmentMechanic.None,true)==null,"insufficient balance cannot buy");
        p.Profile.gold=123456;p.Profile.mechanicMaterials=24;p.Save();quote=p.PrepareMerchantPurchase(EquipmentMechanic.FrostEcho,true);
        C(p.BuyAtMerchant(quote,true)&&p.Profile.mechanicMaterials==12&&p.Attachment(EquipmentMechanic.FrostEcho)!=null&&!p.Attachment(EquipmentMechanic.FrostEcho).mounted,"merchant exchange owns one unmounted attachment for smith");before=State(p);C(!p.BuyAtMerchant(quote,true)&&State(p)==before,"same exchange quote cannot create another attachment");
        C(p.PrepareMerchantPurchase(EquipmentMechanic.FrostEcho,true)==null,"owned mechanism not offered again");
        p.Profile.autoSellCommon=p.Profile.autoSellRare=true;p.Save();C(!p.Profile.autoSellCommon&&!p.Profile.autoSellRare&&!p.SetAutoSell(Rarity.Common,true),"legacy autosell settings disabled and cannot be enabled");
        var item=new ItemData{id="ordinary-owned",name="保全普通装备",slot=ItemSlot.Weapon,level=1,rarity=Rarity.Common,attack=12};gold=p.Profile.gold;C(p.CollectLoot(item)&&p.Profile.gold==gold&&p.Profile.inventory.Any(x=>x.id==item.id),"ordinary rewards stay owned after former autosell setting");
        C(!p.Sell(p.Profile.weaponId),"equipped gear cannot be sold");C(p.SetItemLocked(item.id,true)&&!p.Sell(item.id),"locked gear cannot be sold");C(p.SetItemLocked(item.id,false)&&p.Sell(item.id),"explicit sale remains possible");gold=p.Profile.gold;C(!p.Sell(item.id)&&p.Profile.gold==gold,"duplicate sale cannot pay twice");
    }
    private static void MigrationAndAttachments()
    {
        foreach(HeroClass hero in Enum.GetValues(typeof(HeroClass)))
        {
            var p=Fresh(hero);var mechanic=BuildCatalog.MechanicsFor(hero)[0];var old=p.CreateMechanicItem(mechanic);old.upgradeLevel=3;old.locked=true;
            C(p.CollectLoot(old)&&p.Equip(old.id),"legacy mechanic source equipment owned");for(int rank=0;rank<3;rank++)C(p.Upgrade(old.id),"earn actual permanent slot training");p.Save();
            var doc=JsonNode.Parse(File.ReadAllText(p.SaveFilePath));doc["profile"]["attachmentRevision"]=0;doc["profile"]["attachments"]=new JsonArray();File.WriteAllText(p.SaveFilePath,doc.ToJsonString());
            C(p.LoadSlot(p.CurrentSlotId)&&p.Attachment(mechanic)!=null&&p.Attachment(mechanic).mounted,"legacy equipped mechanism migrates to mounted independent attachment");
            string aid=p.Attachment(mechanic).id;int gearCount=p.Profile.inventory.Count;int strength=p.Equipped(old.slot).attack;
            C(p.SetAttachmentMounted(mechanic,false,true)&&p.SetAttachmentMounted(mechanic,true,true),"smith mount/unmount keeps one ownership record");
            int materials=p.Profile.mechanicMaterials;
            Directory.CreateDirectory(p.SaveFilePath+".tmp");string before=State(p);C(!p.UpgradeAttachment(mechanic,true)&&State(p)==before,"failed attachment upgrade cannot consume materials or investment");Directory.Delete(p.SaveFilePath+".tmp");
            C(p.UpgradeAttachment(mechanic,true)&&p.Profile.mechanicMaterials==materials-6,"one attachment rank consumes one cost");
            for(int j=0;j<4;j++){p.Save();C(p.LoadSlot(p.CurrentSlotId)&&p.Attachment(mechanic).id==aid&&p.Profile.attachments.Count(a=>a.mechanic==mechanic)==1&&p.Profile.inventory.Count==gearCount&&p.Equipped(old.slot).attack==strength,"migration reload never duplicates ownership or compounds equipment stats");}
            C(p.Profile.inventory.Any(x=>x.id==old.id)&&p.SlotUpgradeRank(old.slot)==3,"original paid gear and training retained");
        }
    }
    private static bool Overlap(MobilePanelLayout.Area a,MobilePanelLayout.Area b)
    {return a.X<b.X+b.Width&&b.X<a.X+a.Width&&a.Y<b.Y+b.Height&&b.Y<a.Y+a.Height;}
    private static void Layouts()
    {
        foreach(var size in new[]{(568f,320f),(812f,375f),(1024f,768f),(1366f,1024f)})
        {
            var m=new MerchantServiceLayout(size.Item1,size.Item2);C(!Overlap(m.Balance,m.Close)&&!Overlap(m.Balance,m.Header)&&m.Balance.Width>=128,"multi-digit coin region is separate from title and close hitbox");
            C(m.Action.Width<=136&&!Overlap(m.Info,m.Action)&&!Overlap(m.Tab(0),m.Tab(1)),"purchase/sale tabs and compact footer actions do not collide");
            for(int j=0;j<9;j++){var a=m.Tile(j);C(a.Width<=132&&a.Width>=100&&a.X+a.Width<=m.Body.Width,"three-column compact products and price strips fit scroll width");for(int k=j+1;k<9;k++)C(!Overlap(a,m.Tile(k)),"product grid hitboxes are disjoint");}
            var s=new SmithServiceLayout(size.Item1,size.Item2);C(!Overlap(s.Header,s.Close)&&!Overlap(s.Detail,s.Primary),"smith detail scroll cannot overlap primary/close");
            for(int j=0;j<3;j++){C(!Overlap(s.Category(j),s.Detail)&&!Overlap(s.Equipment(j),s.Detail)&&s.Equipment(j).Height>=44,"categories and equipped-slot selectors stay separate and tappable");C(!Overlap(s.Equipment(j),s.Primary),"all three equipment slots fit above footer");}
        }
    }
}
