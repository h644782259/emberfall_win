using System;
namespace Emberfall
{
    public partial class ProgressionService
    {
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
            item.level=target;item.upgradeLevel=0;item.upgradeBaseInitialized=false;SetRolledStats(item);EnsureUpgradeBasis(item);
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
