namespace Emberfall
{
    public partial class ProgressionService
    {
        public sealed class SmithUpgradeQuote
        {
            internal ProgressionService Owner;
            public string ItemId {get;internal set;}
            public ItemSlot Slot {get;internal set;}
            public int PaidRank {get;internal set;}
            public int GoldCost {get;internal set;}
        }
        public SmithUpgradeQuote PrepareSmithUpgrade(ItemSlot slot,bool atSmith)
        {
            var item=Equipped(slot);
            if(!atSmith||item==null||SlotUpgradeRank(slot)>=MaximumUpgrade||Profile.gold<UpgradeCost(item))return null;
            return new SmithUpgradeQuote{Owner=this,ItemId=item.id,Slot=slot,PaidRank=SlotUpgradeRank(slot),GoldCost=UpgradeCost(item)};
        }
        public bool UpgradeAtSmith(SmithUpgradeQuote quote,bool atSmith)
        {
            if(!atSmith||quote==null||quote.Owner!=this)return Fail("请在铁匠处核对并强化装备。");
            var item=Equipped(quote.Slot);
            if(item==null||item.id!=quote.ItemId||SlotUpgradeRank(quote.Slot)!=quote.PaidRank||UpgradeCost(item)!=quote.GoldCost)
                return Fail("装备或强化等级已变化，请重新核对；尚未扣费。");
            return Upgrade(item.id);
        }
    }
}
