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
        {var gem=Attachment(mechanic);return gem==null?0:BuildCatalog.GemPrice(gem.rarity)/2+System.Math.Max(0,gem.upgradeRank)*AttachmentUpgradeCost+System.Math.Max(0,gem.ascensionRank)*AscensionCost;}
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
            internal int Stones;
            public int RefinementStoneCount {get;internal set;}
            public EquipmentMechanic Mechanic {get;internal set;}
            public Rarity Rarity {get;internal set;}
        }
        public MerchantPurchaseQuote PrepareRefinementPurchase(bool atMerchant,int count=1)
        {
            if(!atMerchant||IsPracticeOnly||count<1||count>99||Profile.refinementStones>999999-count||Profile.gold<count*RefinementStonePrice)return null;
            return new MerchantPurchaseQuote{Owner=this,Gold=Profile.gold,Stones=Profile.refinementStones,RefinementStoneCount=count};
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
            if(quote.RefinementStoneCount>0)
            {
                if(quote.Gold!=Profile.gold||quote.Stones!=Profile.refinementStones||PrepareRefinementPurchase(atMerchant,quote.RefinementStoneCount)==null)
                    return Fail("余额或洗练石数量已变化，请重新核对；尚未扣费。");
                var purchase=Snapshot();purchase.gold-=quote.RefinementStoneCount*RefinementStonePrice;purchase.refinementStones+=quote.RefinementStoneCount;
                return CommitCandidate(purchase,true);
            }
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

        public const int MaximumFashionRank=3;
        public static int FashionUpgradeCost(FashionData fashion)
        {return fashion==null||fashion.upgradeRank>=MaximumFashionRank?0:6*(fashion.upgradeRank+1);}
        public static int FashionDismantleValue(FashionData fashion)
        {return fashion==null?0:2+2*(int)fashion.AppearanceRarity+3*fashion.upgradeRank*(fashion.upgradeRank+1);}
        public static string FashionBonus(FashionData fashion)
        {
            if(fashion==null)return "";
            int q=(int)fashion.rarity,r=System.Math.Max(0,System.Math.Min(MaximumFashionRank,fashion.upgradeRank));
            return fashion.slot==FashionSlot.Wings?
                "生命 +"+(WingHealthPercents[q]+5*r)+"% · 防御 +"+(WingArmorPercents[q]+4*r)+"% · 移动速度 +"+(WingMovePercents[q]+r)+"%"+(r>=2?" · 受到伤害减免 +"+(2*(r-1))+"%":""):
                "攻击 +"+(WeaponPercents[q]+5*r)+"% · 暴击几率 ×"+(100+WeaponPercents[q]+3*r)+"%"+(r>=2?" · 暴击伤害 +"+(5*(r-1))+"%":"");
        }
        public FashionData PreviewFashionUpgrade(string id)
        {
            var fashion=Profile.fashions.Find(f=>f!=null&&f.id==id);
            if(fashion==null||fashion.upgradeRank>=MaximumFashionRank)return null;
            return new FashionData{id=fashion.id,slot=fashion.slot,rarity=fashion.rarity,name=fashion.name,appearanceTier=fashion.appearanceTier,upgradeRank=fashion.upgradeRank+1};
        }
        public string FashionServiceLock(string id,bool dismantle,bool atSmith)
        {
            if(IsPracticeOnly||!atSmith)return "请在铁匠处操作";
            var fashion=Profile.fashions.Find(f=>f!=null&&f.id==id);
            if(fashion==null)return "请选择已拥有的时装";
            if(dismantle)
            {
                if(EquippedFashion(fashion.slot)?.id==id)return "请先卸下时装";
                return Profile.fashionThreads>999999-FashionDismantleValue(fashion)?"星纹已满":"";
            }
            return fashion.upgradeRank>=MaximumFashionRank?"已满阶":Profile.fashionThreads<FashionUpgradeCost(fashion)?"星纹不足":"";
        }
        public sealed class FashionServiceQuote
        {
            internal ProgressionService Owner;internal string Fingerprint;
            public string Id {get;internal set;}
            public bool Dismantle {get;internal set;}
            public int Threads {get;internal set;}
        }
        public FashionServiceQuote PrepareFashionService(string id,bool dismantle,bool atSmith)
        {
            if(FashionServiceLock(id,dismantle,atSmith).Length>0)return null;
            var fashion=Profile.fashions.Find(f=>f.id==id);
            return new FashionServiceQuote{Owner=this,Fingerprint=BuildStateFingerprint(),Id=id,Dismantle=dismantle,Threads=dismantle?FashionDismantleValue(fashion):FashionUpgradeCost(fashion)};
        }
        public bool ApplyFashionService(FashionServiceQuote quote,bool atSmith)
        {
            if(quote==null||quote.Owner!=this||quote.Fingerprint!=BuildStateFingerprint())return Fail("时装或材料已变化，请重新核对。");
            string reason=FashionServiceLock(quote.Id,quote.Dismantle,atSmith);if(reason.Length>0)return Fail(reason);
            var candidate=Snapshot();var fashion=candidate.fashions.Find(f=>f.id==quote.Id);
            if(quote.Dismantle){candidate.fashions.Remove(fashion);candidate.fashionThreads+=quote.Threads;}
            else {candidate.fashionThreads-=quote.Threads;fashion.upgradeRank++;}
            return CommitCandidate(candidate,true);
        }
    }
}
