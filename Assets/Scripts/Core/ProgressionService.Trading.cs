namespace Emberfall
{
    public partial class ProgressionService
    {
        public sealed class MerchantPurchaseQuote
        {
            internal ProgressionService Owner;
            internal int Gold,Materials,Potions;
            internal bool First;
            public EquipmentMechanic Mechanic {get;internal set;}
        }
        public MerchantPurchaseQuote PrepareMerchantPurchase(EquipmentMechanic mechanic,bool atMerchant)
        {
            if(!atMerchant)return null;
            bool first=Profile.pendingFirstClearReward&&!Profile.firstClearRewardClaimed;
            if(mechanic==EquipmentMechanic.None){if(Profile.potions>=99||Profile.gold<PotionPrice)return null;}
            else if(BuildCatalog.MechanicClass(mechanic)!=Profile.heroClass||Attachment(mechanic)!=null||!first&&Profile.mechanicMaterials<MechanicExchangeCost)return null;
            return new MerchantPurchaseQuote{Owner=this,Gold=Profile.gold,Materials=Profile.mechanicMaterials,Potions=Profile.potions,First=first,Mechanic=mechanic};
        }
        public bool BuyAtMerchant(MerchantPurchaseQuote quote,bool atMerchant)
        {
            if(!atMerchant||quote==null||quote.Owner!=this)return Fail("请在商人处核对并交易。");
            if(quote.Gold!=Profile.gold||quote.Materials!=Profile.mechanicMaterials||quote.Potions!=Profile.potions||quote.First!=(Profile.pendingFirstClearReward&&!Profile.firstClearRewardClaimed))
                return Fail("余额、数量或兑换资格已变化，请重新核对；尚未扣费。");
            if(quote.Mechanic==EquipmentMechanic.None)return BuyPotion();
            return quote.First?ClaimFirstClearReward(quote.Mechanic):ExchangeMechanic(quote.Mechanic);
        }
    }
}
