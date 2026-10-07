using System;
namespace Emberfall
{
    public enum ReforgeTargetKind { FiveLevels, Affordable, PlayerLevel }
    public sealed class ReforgeChoice
    {
        public readonly ReforgeTargetKind Kind;public readonly ReforgeQuote Quote;
        public ReforgeChoice(ReforgeTargetKind kind,ReforgeQuote quote){Kind=kind;Quote=quote;}
        public string Label {get{return Kind==ReforgeTargetKind.FiveLevels?"提升下一档（10级）":Kind==ReforgeTargetKind.Affordable?"当前金币可达":"追平角色等级";}}
    }
    // A quote captures the target, not a promise to chase later player levels.
    public sealed class ReforgeQuote
    {
        public string ItemId {get;}
        public int FromLevel {get;}
        public int TargetLevel {get;}
        public int GoldCost {get;}
        internal string SavePath {get;}
        internal ReforgeQuote(string id,int from,int target,int gold,string savePath)
        {ItemId=id;FromLevel=from;TargetLevel=target;GoldCost=gold;SavePath=savePath;}
    }
}
