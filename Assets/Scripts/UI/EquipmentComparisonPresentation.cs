using System;
namespace Emberfall
{
    public static class EquipmentComparisonPresentation
    {
        // Ranges that straddle the equipped value have no definite direction.
        public static int StatDirection(string candidate,string current)
        {
            var values=System.Text.RegularExpressions.Regex.Matches(candidate??"",@"[+-]?\d+(?:[.,]\d+)?");
            var baseline=System.Text.RegularExpressions.Regex.Match(current??"",@"[+-]?\d+(?:[.,]\d+)?");
            if(values.Count==0||!baseline.Success)return 0;
            double oldValue,low,high;
            var culture=System.Globalization.CultureInfo.InvariantCulture;
            if(!double.TryParse(baseline.Value.Replace(',','.'),System.Globalization.NumberStyles.Float,culture,out oldValue)||!double.TryParse(values[0].Value.Replace(',','.'),System.Globalization.NumberStyles.Float,culture,out low))return 0;
            high=low;
            if(values.Count>1&&!double.TryParse(values[1].Value.Replace(',','.'),System.Globalization.NumberStyles.Float,culture,out high))return 0;
            return Math.Min(low,high)>oldValue?1:Math.Max(low,high)<oldValue?-1:0;
        }
        public static EquipmentMechanic ActiveMechanic(ItemData item, HeroClass hero)
        {
            if(item==null || item.mechanic==EquipmentMechanic.None || !Enum.IsDefined(typeof(EquipmentMechanic),item.mechanic))return EquipmentMechanic.None;
            return BuildCatalog.MechanicClass(item.mechanic)==hero && BuildCatalog.MechanicSlot(item.mechanic)==item.slot ? item.mechanic:EquipmentMechanic.None;
        }
        public static string Label(ItemData item, HeroClass hero)
        {
            var mechanic=ActiveMechanic(item,hero);
            if(mechanic==EquipmentMechanic.None)return "无生效机制";
            return BuildCatalog.MechanicName(mechanic)+((BuildCatalog.HasMechanicVariant(mechanic))?" · "+(item.mechanicVariantUnlocked&&item.mechanicVariant==1?"变体 B":"变体 A"):"");
        }
        public static bool SameMechanism(ItemData current, ItemData next, HeroClass hero)
        {
            var a=ActiveMechanic(current,hero);var b=ActiveMechanic(next,hero);
            return a==b && (a==EquipmentMechanic.None||Label(current,hero)==Label(next,hero));
        }
        public static string Changes(ItemData current, ItemData next, HeroClass hero)
        {
            string gained=Label(next,hero),lost=Label(current,hero);
            if(SameMechanism(current,next,hero))return "机制保留 · "+gained;
            return "获得 · "+gained+"\n失去 · "+lost;
        }
        public static string Description(ItemData item,HeroClass hero)
        {
            if(item!=null&&item.mechanic!=EquipmentMechanic.None&&ActiveMechanic(item,hero)==EquipmentMechanic.None)
                return "该机制与当前职业或部位不匹配，不生效。";
            return ActiveMechanic(item,hero)==EquipmentMechanic.None?"无特殊效果。":BuildCatalog.MechanicDescription(item.mechanic);
        }
        public static string Description(ItemData item,ProgressionService progression)
        {
            if(item==null)return "宝石槽为空";
            var text=new System.Text.StringBuilder();
            foreach(var mechanic in BuildCatalog.GemsFor(progression.Profile.heroClass))
            {
                if(BuildCatalog.MechanicSlot(mechanic)!=item.slot)continue;
                var gem=progression.Attachment(mechanic);
                if(gem==null||!gem.mounted)continue;
                if(text.Length>0)text.Append("\n");
                string effect=BuildCatalog.IsAttributeGem(mechanic)?BuildCatalog.GemAttributeSummary(mechanic,gem.rarity,gem.upgradeRank):BuildCatalog.AttributeLabel(BuildCatalog.MechanicAttribute(mechanic))+" +"+(BuildCatalog.MechanicAttributeValue(mechanic,gem.upgradeRank)*100).ToString("0.#")+"%";
                effect+="\n"+(gem.ascensionRank>0?BuildCatalog.GemFormName(mechanic,gem.variant)+" · "+BuildCatalog.GemFormDescription(mechanic,gem.variant,gem.ascensionRank):"首次升华解锁机制形态");
                text.Append(BuildCatalog.GemName(mechanic)).Append(" · 已镶嵌\n").Append(effect);
            }
            return text.Length==0?"宝石槽为空 · 未镶嵌":text.ToString();
        }
        public static string Changes(ItemData current,ItemData next,ProgressionService progression)
        {return "宝石镶嵌随部位保留";}
        public static string CollectionState(bool owned,bool worn,bool strongest)
        {return !owned?"未获得 · 仅试穿":(worn?"已穿戴":"已收藏")+(strongest?" · 属性来源":" · 非属性来源");}
        public static FashionData Trial(FashionSlot slot,Rarity rarity)
        {
            if(!Enum.IsDefined(typeof(FashionSlot),slot)||!Enum.IsDefined(typeof(Rarity),rarity))throw new ArgumentOutOfRangeException();
            return new FashionData{id="fashion-"+(int)slot+"-"+(int)rarity,slot=slot,rarity=rarity};
        }
        public static FashionData Receipt(ChestReward receipt)
        {
            if(receipt==null||!receipt.Slot.HasValue||!receipt.Rarity.HasValue||!Enum.IsDefined(typeof(FashionSlot),receipt.Slot.Value)||!Enum.IsDefined(typeof(Rarity),receipt.Rarity.Value))return null;
            var fashion=Trial(receipt.Slot.Value,receipt.appearanceTier>=0?(Rarity)receipt.appearanceTier:receipt.Rarity.Value);fashion.appearanceTier=(int)fashion.rarity;fashion.rarity=Rarity.Legendary;return fashion;
        }
    }
}
