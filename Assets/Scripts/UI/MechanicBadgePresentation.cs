namespace Emberfall
{
    public static class MechanicBadgePresentation
    {
        public static string Title(ItemData item, HeroClass hero)
        { return EquipmentComparisonPresentation.Label(item,hero); }
        public static string Benefit(ItemData item, HeroClass hero)
        {
            switch(EquipmentComparisonPresentation.ActiveMechanic(item,hero))
            {
                case EquipmentMechanic.FrostEcho:return "回响伤害与二次控制"+(VariantB(item)?" · 范围 +35%":"");
                case EquipmentMechanic.CinderTrail:return "落点留下2秒火场"+(VariantB(item)?" · 每跳伤害提高":"");
                case EquipmentMechanic.ReturningBlade:return VariantB(item)?"完美闪避反击3秒 · 175%窄刺 · 最多前进2米":"回刃弹射 · 回收强化下一刀";
                case EquipmentMechanic.VenomSpread:return BuildCatalog.VenomModifier(VariantB(item))+(VariantB(item)?" · 240%/360%/480%直伤 · 普通三毒引爆":" · 向附近目标传播毒素");
                case EquipmentMechanic.TwinSummonResonance:return "伙伴伤害 +60% / 生命 +20% · 异种共鸣";
                default:return "无生效机制收益";
            }
        }
        public static string Cost(ItemData item, HeroClass hero)
        {
            switch(EquipmentComparisonPresentation.ActiveMechanic(item,hero))
            {
                case EquipmentMechanic.FrostEcho:return "新星首击 -20%"+(VariantB(item)?" · 回响伤害降低":"");
                case EquipmentMechanic.CinderTrail:return "陨星直伤 -20%"+(VariantB(item)?" · 火场半径 -30%":"");
                case EquipmentMechanic.ReturningBlade:return VariantB(item)?"放弃弹射、回收强化与击杀再弹；无普攻折损":"普攻伤害 -8%";
                case EquipmentMechanic.VenomSpread:return VariantB(item)?"仅首个实际拦截目标；取消扇形、爆炸及传播":"引爆加成伤害 -20%";
                case EquipmentMechanic.TwinSummonResonance:return "普通召唤上限改为2";
                default:return "无机制取舍";
            }
        }
        private static bool VariantB(ItemData item) { return item!=null&&item.mechanicVariantUnlocked&&item.mechanicVariant==1; }
    }
}
