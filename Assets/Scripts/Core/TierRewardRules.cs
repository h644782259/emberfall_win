using System;
namespace Emberfall
{
    /// <summary>Bounded rewards from the tier actually completed, never the menu selection.</summary>
    public static class TierRewardRules
    {
        public static int ClampTier(int tier) { return TierRewardBand.Clamp(tier); }
        public static int Band(int tier) { return TierRewardBand.Of(tier); }
        public static int ClearMaterials(int tier) { return TierRewardBand.Materials(3,tier); }
        public static int ChestGoldMinimum(int tier) { return 60 + 25 * Band(ClampTier(tier)); }
        public static int BossMechanicChance(int tier) { return 25 + 3 * Band(ClampTier(tier)); }
        public static int OrdinaryMechanicChance(int tier) { return 12 + 2 * Band(ClampTier(tier)); }
        public static Rarity DropRarity(bool boss, int tier, int roll)
        {
            if(tier<=0)return Rarity.Common;
            int band = Band(ClampTier(tier)); roll = Math.Max(0, Math.Min(99, roll));
            if (boss)
            {
                int legendary = 8 + 3 * band, epic = 37 + 2 * band;
                return roll < 100 - legendary - epic ? Rarity.Rare : roll < 100 - legendary ? Rarity.Epic : Rarity.Legendary;
            }
            int common = 54 - 4 * band, ordinaryLegendary = 2 + band;
            return roll < common ? Rarity.Common : roll < common + 31 ? Rarity.Rare : roll < 100 - ordinaryLegendary ? Rarity.Epic : Rarity.Legendary;
        }
    }
}
