namespace Emberfall
{
    public enum ProgressionGoalAction { None, ClaimCore, ExchangeCore, ClaimPending, ClaimRecovery, Equip, UnlockVariant, Ascend, Reforge, OpenPresets, UpgradeAttachment }
    // One selected identity and one next action, shared by camp, entry and results.
    public sealed class ProgressionGoalState
    {
        public string Identity,Title,Step,ItemId;
        public int MaterialCost,GoldCost,RequiredAdventureTier;
        public ReforgeQuote ReforgeQuote;
        public string Requirements=string.Empty;
        public bool Done,CanAct;
        public ProgressionGoalAction Action;
        public string ActionIdentity {get{return Identity+"/"+(int)Action+"/"+ItemId+(ReforgeQuote==null?"":"/"+ReforgeQuote.FromLevel+"/"+ReforgeQuote.TargetLevel+"/"+ReforgeQuote.GoldCost);}}
        public string ResourceRequirements(GameProfile profile,int highestTier)
        {
            string result="";
            if(MaterialCost>0)result="碎片 "+profile.mechanicMaterials+"/"+MaterialCost+(profile.mechanicMaterials>=MaterialCost?" 已齐":" · 仍缺 "+(MaterialCost-profile.mechanicMaterials)+"枚");
            if(GoldCost>0)result+=(result.Length>0?" · ":"")+"金币 "+profile.gold+"/"+GoldCost+(profile.gold>=GoldCost?" 已齐":" · 仍缺 "+(GoldCost-profile.gold));
            if(RequiredAdventureTier>0)result+=(result.Length>0?" · ":"")+"冒险 "+highestTier+"/"+RequiredAdventureTier+"阶"+(highestTier>=RequiredAdventureTier?" 已达":" · 仍需第"+RequiredAdventureTier+"阶");
            return result;
        }
        public string ActionLabel
        {
            get
            {
                switch(Action)
                {
                    case ProgressionGoalAction.ClaimCore:return "前往商人兑换";
                    case ProgressionGoalAction.ExchangeCore:return "前往商人兑换";
                    case ProgressionGoalAction.ClaimPending:case ProgressionGoalAction.ClaimRecovery:return "领取目标装备";
                    case ProgressionGoalAction.Equip:return "穿戴目标装备";
                    case ProgressionGoalAction.UnlockVariant:return "解锁目标变体";
                    case ProgressionGoalAction.Ascend:return "升华目标装备";
                    case ProgressionGoalAction.Reforge:return "重铸目标装备";
                    case ProgressionGoalAction.UpgradeAttachment:return "升级目标挂件";
                    case ProgressionGoalAction.OpenPresets:return "打开配装方案";
                    default:return "继续当前目标";
                }
            }
        }
    }
}
