using System;
namespace Emberfall
{
    public partial class ProgressionService
    {

        public const int AffixReforgeStonePrice=5000;
        public string AffixReforgeLockReason(string id,bool inCamp)
        {
            if(IsPracticeOnly||!inCamp)return "请在铁匠处重铸";
            var item=FindItem(id);if(item==null)return "请选择装备";
            if(EquipmentAffixLimit(item.rarity)==0)return "普通装备无随机词条";
            if(Profile.affixReforgeCount==int.MaxValue)return "重铸次数已达上限";
            return Profile.affixReforgeStones<1?"重铸石不足":string.Empty;
        }
        public sealed class AffixReforgeQuote
        {
            internal ProgressionService Owner;internal string Fingerprint,Slot;
            public string Id {get;internal set;}
        }
        public AffixReforgeQuote PrepareAffixReforge(string id,bool inCamp)
        {return AffixReforgeLockReason(id,inCamp).Length>0?null:new AffixReforgeQuote{Owner=this,Slot=CurrentSlotId,Fingerprint=BuildStateFingerprint(),Id=id};}
        public bool ApplyAffixReforge(AffixReforgeQuote quote,bool inCamp)
        {
            if(quote==null||quote.Owner!=this||quote.Slot!=CurrentSlotId||quote.Fingerprint!=BuildStateFingerprint())return Fail("装备或材料已变化，请重新确认。");
            return ReforgeAffixes(quote.Id,inCamp);
        }
        public bool ReforgeAffixes(string id,bool inCamp)
        {
            string reason=AffixReforgeLockReason(id,inCamp);if(reason.Length>0)return Fail(reason);
            var candidate=Snapshot();var item=candidate.inventory.Find(x=>x.id==id);
            int seed=candidate.affixReforgeCount;unchecked{foreach(char c in id)seed=seed*31+c;}
            RollRandomEquipmentAffixes(item,(int)item.rarity,new Random(seed));
            candidate.affixReforgeStones--;candidate.affixReforgeCount++;
            return CommitCandidate(candidate,true);
        }
        public const int RefinementStonePrice=50;
        public static int DungeonRefinementStones(int tier)
        {return 2+TierRewardBand.Of(tier);}

        public static int ChapterRefinementStones(ChapterNode node,int tier)
        {return node==ChapterNode.Redrock?3+Math.Max(1,Math.Min(10,tier))/3:1;}
        public static int ChapterRefinementStonesMinimum(ChapterNode node,int tier)
        {return Math.Max(1,ChapterRefinementStones(node,tier)-1);}
        public static int ChapterRefinementStonesMaximum(ChapterNode node,int tier)
        {return ChapterRefinementStones(node,tier)+1;}
        public static int RollChapterRefinementStones(ChapterNode node,int tier,string receipt)
        {
            int seed=23;unchecked{foreach(char c in receipt??"")seed=seed*31+c;}
            return new Random(seed).Next(ChapterRefinementStonesMinimum(node,tier),ChapterRefinementStonesMaximum(node,tier)+1);
        }
        public ItemData PreviewRefinementLimit(string id)
        {
            var item=FindItem(id);if(item==null)return null;
            var result=PreviewUpgrade(item,item.upgradeLevel);int q=Clamp((int)item.rarity,0,3),level=EquipmentGenerationLevel(item.level);
            float max=EquipmentRollMaximum[q];
            if(result.baseAttack>0)result.baseAttack=Math.Max(result.baseAttack,Round((item.slot==ItemSlot.Weapon?5+level*2.5f:2+level)*max));
            if(result.baseDefense>0)result.baseDefense=Math.Max(result.baseDefense,Round((3+level*1.2f)*max));
            if(result.baseHealth>0)result.baseHealth=Math.Max(result.baseHealth,Round((item.slot==ItemSlot.Armor?10+level*4:6+level*3)*max));
            if(result.criticalChance>0)result.criticalChance=Math.Max(result.criticalChance,new[]{0f,.03f,.05f,.08f}[q]);
            if(result.criticalDamageBonus>0)result.criticalDamageBonus=Math.Max(result.criticalDamageBonus,new[]{0f,.10f,.15f,.20f}[q]);
            if(result.attackPercent>0)result.attackPercent=Math.Max(result.attackPercent,new[]{0f,.05f,.08f,.12f}[q]);
            ApplyUpgradeRank(result,item.upgradeLevel);return result;
        }
        public string RefinementLockReason(string id,bool inCamp)
        {
            if(IsPracticeOnly||!inCamp)return "请在铁匠处洗练";
            var current=FindItem(id);var cap=PreviewRefinementLimit(id);if(current==null||cap==null)return "请选择装备";
            if(current.attack>=cap.attack&&current.defense>=cap.defense&&current.health>=cap.health&&current.criticalChance>=cap.criticalChance&&current.criticalDamageBonus>=cap.criticalDamageBonus&&current.attackPercent>=cap.attackPercent)return "数值已满";
            if(Profile.refinementCount==int.MaxValue)return "洗练次数已达上限";
            return Profile.refinementStones<1?"需要装备洗练石 · 副本通关掉落／商店金币购买":string.Empty;
        }
        private static int RefinedValue(int value,int cap,Random random)
        {return value>=cap?value:Math.Min(cap,value+Math.Max(1,(int)Math.Ceiling((cap-value)*(.15+random.NextDouble()*.3)))) ;}
        public bool RefineEquipment(string id,bool inCamp)
        {
            string reason=RefinementLockReason(id,inCamp);if(reason.Length>0)return Fail(reason);
            var cap=PreviewRefinementLimit(id);var candidate=Snapshot();var item=candidate.inventory.Find(x=>x.id==id);EnsureUpgradeBasis(item);
            int seed=candidate.refinementCount;unchecked{foreach(char c in id)seed=seed*31+c;}var random=new Random(seed);
            item.baseAttack=RefinedValue(item.baseAttack,cap.baseAttack,random);item.baseDefense=RefinedValue(item.baseDefense,cap.baseDefense,random);item.baseHealth=RefinedValue(item.baseHealth,cap.baseHealth,random);
            item.criticalChance=Math.Max(item.criticalChance,RefinedValue(Round(item.criticalChance*10000),Round(cap.criticalChance*10000),random)/10000f);
            item.criticalDamageBonus=Math.Max(item.criticalDamageBonus,RefinedValue(Round(item.criticalDamageBonus*10000),Round(cap.criticalDamageBonus*10000),random)/10000f);
            item.attackPercent=Math.Max(item.attackPercent,RefinedValue(Round(item.attackPercent*10000),Round(cap.attackPercent*10000),random)/10000f);
            ApplyUpgradeRank(item,item.upgradeLevel);candidate.refinementStones--;candidate.refinementCount++;
            if(item.attack==cap.attack&&item.defense==cap.defense&&item.health==cap.health&&item.criticalChance>=cap.criticalChance&&item.criticalDamageBonus>=cap.criticalDamageBonus&&item.attackPercent>=cap.attackPercent)candidate.refinementMaxCount=Math.Min(int.MaxValue-1,candidate.refinementMaxCount)+1;
            return CommitCandidate(candidate,true);
        }

        public static int ReforgeGoldCost(int fromLevel,int targetLevel)
        {
            if(fromLevel<1||targetLevel>100||targetLevel<=fromLevel)throw new ArgumentOutOfRangeException();
            return (targetLevel-fromLevel)*(fromLevel+targetLevel+21);
        }
        public ReforgeQuote QuoteReforge(string id,int targetLevel=0)
        {
            ItemData item=FindItem(id);int target=EquipmentGenerationLevel(targetLevel==0?Profile.level:targetLevel);
            if(item==null||target<=item.level||target>Profile.level||target>100||item.level<1||
                MechanicGoalEligibility(id,ProgressionGoalKind.Reforge).Length>0)return null;
            return new ReforgeQuote(id,item.level,target,ReforgeGoldCost(item.level,target),SaveFilePath);
        }
        public ReforgeQuote QuoteReforgeChoice(string id,ReforgeTargetKind kind)
        {
            ItemData item=FindItem(id);if(item==null||!Enum.IsDefined(typeof(ReforgeTargetKind),kind))return null;
            int target=EquipmentGenerationLevel(Profile.level);
            if(kind==ReforgeTargetKind.FiveLevels)target=Math.Min(target,item.level+10);
            if(kind==ReforgeTargetKind.Affordable)
            {int low=item.level,high=target;while(low<high){int mid=low+(high-low+1)/2;if(ReforgeGoldCost(item.level,mid)<=Profile.gold)low=mid;else high=mid-1;}target=low;}
            return QuoteReforge(id,target);
        }
        public ReforgeChoice[] ReforgeChoices(string id)
        {
            var result=new System.Collections.Generic.List<ReforgeChoice>(3);
            foreach(ReforgeTargetKind kind in Enum.GetValues(typeof(ReforgeTargetKind)))
            {var quote=QuoteReforgeChoice(id,kind);if(quote!=null&&!result.Exists(x=>x.Quote.TargetLevel==quote.TargetLevel))result.Add(new ReforgeChoice(kind,quote));}
            return result.ToArray();
        }
        public ItemData PreviewReforge(ReforgeQuote quote)
        {
            if(quote==null||quote.SavePath!=SaveFilePath)return null;
            ItemData current=FindItem(quote.ItemId);
            if(current==null||current.level!=quote.FromLevel||quote.TargetLevel>Profile.level||quote.TargetLevel<=current.level||quote.GoldCost!=ReforgeGoldCost(current.level,quote.TargetLevel))return null;
            ItemData preview=PreviewUpgrade(current,current.upgradeLevel);ApplyReforgeStats(Profile,preview,quote.TargetLevel);return preview;
        }
        private static void ApplyReforgeStats(GameProfile profile,ItemData item,int target)
        {
            EnsureUpgradeBasis(item);int attack=item.baseAttack,defense=item.baseDefense,health=item.baseHealth;float crit=item.criticalChance,bonus=item.criticalDamageBonus,attackPercent=item.attackPercent;
            item.level=target;item.upgradeLevel=0;item.upgradeBaseInitialized=false;SetRolledStats(item);
            item.attack=Math.Max(attack,item.attack);item.defense=Math.Max(defense,item.defense);item.health=Math.Max(health,item.health);item.attackPercent=Math.Max(attackPercent,item.attackPercent);item.criticalChance=Math.Max(crit,item.criticalChance);item.criticalDamageBonus=Math.Max(bonus,item.criticalDamageBonus);EnsureUpgradeBasis(item);
            if(IsEquipped(profile,item.id))ApplyUpgradeRank(item,profile.slotUpgradeRanks[(int)item.slot]);
        }
        public string ReforgeLockReason(ReforgeQuote quote,bool inCamp)
        {
            if(!inCamp)return "只能在营地重铸机制装备。";
            if(quote==null)return "请选择可成长的本职业机制装备及有效目标等级。";
            ItemData item=FindItem(quote.ItemId);
            if(quote.SavePath!=SaveFilePath||item==null||item.level!=quote.FromLevel||quote.TargetLevel>Profile.level||
                quote.TargetLevel<=item.level||quote.GoldCost!=ReforgeGoldCost(item.level,quote.TargetLevel))return "重铸报价已变化，请重新确认。";
            string reason=MechanicGoalEligibility(quote.ItemId,ProgressionGoalKind.Reforge);if(reason.Length>0)return reason;
            return Profile.gold<quote.GoldCost?"重铸需要 "+quote.GoldCost+" 金币（现有 "+Profile.gold+"），不消耗星烬碎片。":string.Empty;
        }
        public bool ReforgeMechanic(ReforgeQuote quote,bool inCamp)
        {
            string reason=ReforgeLockReason(quote,inCamp);if(reason.Length>0)return Fail(reason);
            GameProfile candidate=Snapshot();candidate.gold-=quote.GoldCost;
            ItemData item=candidate.inventory.Find(x=>x.id==quote.ItemId);ApplyReforgeStats(candidate,item,quote.TargetLevel);
            return CommitCandidate(candidate);
        }
    }
}
