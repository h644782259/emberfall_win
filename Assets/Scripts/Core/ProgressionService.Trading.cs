namespace Emberfall
{
    public partial class ProgressionService
    {
        public sealed class MerchantBulkSaleQuote
        {
            internal ProgressionService Owner;internal string Fingerprint;
            internal readonly System.Collections.Generic.List<string> Ids=new System.Collections.Generic.List<string>();
            public int Count {get{return Ids.Count;}}
            public int Gold {get;internal set;}
        }
        public MerchantBulkSaleQuote PrepareMerchantBulkSale(bool atMerchant)
        {
            if(!atMerchant||IsPracticeOnly)return null;
            var quote=new MerchantBulkSaleQuote{Owner=this,Fingerprint=BuildStateFingerprint()};
            long value=0;
            foreach(var item in Profile.inventory)
            {
                if(item==null||item.locked||IsEquipped(Profile,item.id)||PresetReferences(item.id).Length>0)continue;
                var current=Equipped(item.slot);
                if(current==null||EquipmentScore(PreviewEquippedItem(item))>=EquipmentScore(current))continue;
                quote.Ids.Add(item.id);value+=SellValue(item);
            }
            if(value>MaximumGold-Profile.gold)return null;
            quote.Gold=(int)value;
            return quote.Count>0&&quote.Gold>0?quote:null;
        }
        public bool SellAtMerchant(MerchantBulkSaleQuote quote,bool atMerchant)
        {
            if(!atMerchant||IsPracticeOnly||quote==null||quote.Owner!=this||quote.Fingerprint!=BuildStateFingerprint())return Fail("装备或余额已变化，请重新核对出售清单。");
            var current=PrepareMerchantBulkSale(atMerchant);
            if(current==null||current.Count!=quote.Count||current.Gold!=quote.Gold)return Fail("可出售物品已变化，请重新核对。");
            for(int i=0;i<quote.Count;i++)if(current.Ids[i]!=quote.Ids[i])return Fail("可出售物品已变化，请重新核对。");
            var candidate=Snapshot();candidate.inventory.RemoveAll(item=>item!=null&&quote.Ids.Contains(item.id));
            candidate.gold+=quote.Gold;return CommitCandidate(candidate);
        }
        public int GemSellValue(EquipmentMechanic mechanic)
        {var gem=Attachment(mechanic);return gem==null?0:System.Math.Max(0,gem.upgradeRank)*AttachmentUpgradeCost+System.Math.Max(0,gem.ascensionRank)*AscensionCost;}
        public string GemSaleLock(EquipmentMechanic mechanic,bool atMerchant)
        {
            var gem=Attachment(mechanic);
            if(!atMerchant||IsPracticeOnly||gem==null)return "无法出售";
            if(gem.mounted)return "请先卸下";
            var plans=new System.Collections.Generic.List<BuildPreset>();
            if(Profile.buildPresets!=null)plans.AddRange(Profile.buildPresets);
            if(Profile.classStates!=null)foreach(var state in Profile.classStates)if(state!=null&&state.buildPresets!=null)plans.AddRange(state.buildPresets);
            foreach(var plan in plans)if(plan!=null&&plan.populated&&plan.mountedAttachments!=null&&System.Array.IndexOf(plan.mountedAttachments,mechanic)>=0)return "方案使用中";
            if(Profile.mechanicMaterials>999999-GemSellValue(mechanic))return "碎片已满";
            return "";
        }
        public bool SellGem(EquipmentMechanic mechanic,bool atMerchant)
        {
            string reason=GemSaleLock(mechanic,atMerchant);if(reason.Length>0)return Fail(reason);
            var candidate=Snapshot();int value=GemSellValue(mechanic);
            candidate.attachments.RemoveAll(a=>a!=null&&a.mechanic==mechanic);
            // Old equipment may still carry the migrated mechanism. Clear those legacy links,
            // otherwise normalizing a save could recreate a sold attachment.
            var items=new System.Collections.Generic.List<ItemData>(candidate.inventory);
            items.AddRange(candidate.pendingLoot);items.AddRange(candidate.recoveryLoot);
            foreach(var item in items)if(item!=null&&item.mechanic==mechanic){item.mechanic=EquipmentMechanic.None;item.mechanicVariant=0;item.mechanicVariantUnlocked=false;}
            candidate.mechanicMaterials+=value;return CommitCandidate(candidate,true);
        }
        public sealed class MerchantPurchaseQuote
        {
            internal ProgressionService Owner;
            internal int Gold,Materials,Potions;
            internal bool First;
            public EquipmentMechanic Mechanic {get;internal set;}
            public Rarity Rarity {get;internal set;}
        }
        public MerchantPurchaseQuote PrepareMerchantPurchase(EquipmentMechanic mechanic,bool atMerchant,Rarity rarity=Rarity.Epic)
        {
            if(!atMerchant||!System.Enum.IsDefined(typeof(Rarity),rarity))return null;
            bool first=Profile.pendingFirstClearReward&&!Profile.firstClearRewardClaimed;
            if(mechanic==EquipmentMechanic.None){if(Profile.potions>=99||Profile.gold<PotionPrice)return null;}
            else
            {
                if(!System.Enum.IsDefined(typeof(EquipmentMechanic),mechanic)||!BuildCatalog.GemCompatible(mechanic,Profile.heroClass))return null;
                var owned=Attachment(mechanic);
                bool free=first&&rarity==Rarity.Epic;
                if(owned!=null&&owned.rarity>=rarity&&!free)return null;
                if(free&&owned!=null&&owned.rarity>=rarity&&Profile.mechanicMaterials>999996)return null;
                if(!free&&Profile.mechanicMaterials<BuildCatalog.GemPrice(rarity))return null;
            }
            return new MerchantPurchaseQuote{Owner=this,Gold=Profile.gold,Materials=Profile.mechanicMaterials,Potions=Profile.potions,First=first,Mechanic=mechanic,Rarity=rarity};
        }
        public bool BuyAtMerchant(MerchantPurchaseQuote quote,bool atMerchant)
        {
            if(!atMerchant||quote==null||quote.Owner!=this)return Fail("请在商人处核对并交易。");
            if(quote.Gold!=Profile.gold||quote.Materials!=Profile.mechanicMaterials||quote.Potions!=Profile.potions||quote.First!=(Profile.pendingFirstClearReward&&!Profile.firstClearRewardClaimed)||PrepareMerchantPurchase(quote.Mechanic,atMerchant,quote.Rarity)==null)
                return Fail("余额、物品或兑换资格已变化，请重新核对；尚未扣费。");
            if(quote.Mechanic==EquipmentMechanic.None)return BuyPotion();
            bool free=quote.First&&quote.Rarity==Rarity.Epic;
            int cost=free?0:BuildCatalog.GemPrice(quote.Rarity);
            var candidate=Snapshot();candidate.mechanicMaterials-=cost;
            var gem=candidate.attachments.Find(a=>a.mechanic==quote.Mechanic);
            if(gem==null){gem=new MechanicAttachment{id=System.Guid.NewGuid().ToString("N"),mechanic=quote.Mechanic,rarity=Rarity.Common,ascensionRank=0,level=EquipmentGenerationLevel(Profile.level),mounted=false};candidate.attachments.Add(gem);}
            if(gem.rarity>=quote.Rarity&&free)candidate.mechanicMaterials+=3;
            else gem.rarity=(Rarity)System.Math.Max((int)gem.rarity,(int)quote.Rarity);
            if(free){candidate.firstClearRewardClaimed=true;candidate.pendingFirstClearReward=false;}
            if(!CommitCandidate(candidate,true))return false;
            PublishRewardMoment(free?RewardMomentKind.FirstCore:RewardMomentKind.MechanicExchange,materials:-cost,attachment:Attachment(quote.Mechanic));return true;
        }
    }
}
