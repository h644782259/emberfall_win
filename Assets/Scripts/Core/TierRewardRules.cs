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

    // One authoritative clear-loot table shared by previews and committed rewards.
    public static class AdventureRewardRules
    {
        public const int DuplicateGemMaterials=3;
        // Keep the stored difficulty index for save compatibility; gameplay uses its fixed level.
        public static int DungeonLevel(int index){return Math.Max(1,Math.Min(10,index))*10;}
        public static int MaximumDungeonIndex(int characterLevel){return Math.Max(1,Math.Min(10,(Math.Max(1,characterLevel)+10)/10));}
        public static EquipmentMechanic ExclusiveGem(int mode)
        {
            Validate(mode);
            return new[]{EquipmentMechanic.ReturningBlade,EquipmentMechanic.TwinSummonResonance,EquipmentMechanic.CinderTrail,EquipmentMechanic.FrostEcho,EquipmentMechanic.VenomSpread}[mode+1];
        }

        public static int EquipmentCount(int mode){Validate(mode);return mode==3?2:1;}
        public static int EquipmentCount(int mode,int tier){return EquipmentCount(mode)+TierRewardBand.Of(tier)/2;}
        public static int PotionChance(int tier){return 12+3*TierRewardBand.Of(tier);}
        public static int LegendaryChance(int tier){return 2+2*TierRewardBand.Of(tier);}
        private static void Validate(int mode){if(mode < -1 || mode > 3)throw new ArgumentOutOfRangeException(nameof(mode));}
        public static ItemSlot EquipmentSlot(int mode,int index)
        {Validate(mode);if(index<0||index>=8)throw new ArgumentOutOfRangeException(nameof(index));return mode==0?ItemSlot.Armor:mode==1?ItemSlot.Relic:mode==3?(index%2==0?ItemSlot.Armor:ItemSlot.Relic):ItemSlot.Weapon;}
        public static Rarity MinimumRarity(int mode){Validate(mode);return mode==2?Rarity.Epic:Rarity.Rare;}
        public static int UpgradeChance(int mode,int tier){Validate(mode);return mode==2?8+3*TierRewardBand.Of(tier):15+5*TierRewardBand.Of(tier);}
        public static Rarity EquipmentRarity(int mode,int tier,int roll)
        {return roll<LegendaryChance(tier)?Rarity.Legendary:roll<LegendaryChance(tier)+UpgradeChance(mode,tier)?(mode==2?Rarity.Legendary:Rarity.Epic):MinimumRarity(mode);}
        public static int Materials(int mode,int tier){Validate(mode);return (mode==0||mode==1?5:mode==3?4:3)+TierRewardBand.Of(tier);}
        public static int Gold(int mode,int tier,bool riskContract)
        {Validate(mode);tier=TierRewardBand.Clamp(tier);int value=mode==-1?120+tier*30:mode==0?110+tier*20:mode==1?100+tier*25:mode==2?180+tier*40:200+tier*35;return riskContract?(int)Math.Round(value*1.3f):value;}
        public static int Experience(int mode,int tier)
        {Validate(mode);tier=TierRewardBand.Clamp(tier);return mode==-1?100+tier*20:mode==0?90+tier*20:mode==1?100+tier*20:mode==2?140+tier*30:160+tier*25;}
        public static string EquipmentSummary(int mode,int tier)
        {Validate(mode);string slot=mode==0?"护甲":mode==1?"饰品":mode==3?"护甲 + 饰品":"武器";return "保底 "+(mode==2?"史诗":"稀有")+" "+slot+" · "+(mode==2?UpgradeChance(mode,tier)+LegendaryChance(tier):LegendaryChance(tier))+"% 传说"+(mode==2?"":" · "+UpgradeChance(mode,tier)+"% 史诗");}
    }
}

namespace Emberfall
{
    // Reachability is enumerated from the same integer rolls used by actual loot.
    public static class DropPreviewRules
    {
        public static Rarity[] ClearRarities(int mode,int tier)
        {
            var found=new System.Collections.Generic.SortedSet<Rarity>();
            for(int roll=0;roll<100;roll++)found.Add(AdventureRewardRules.EquipmentRarity(mode,tier,roll));
            return new System.Collections.Generic.List<Rarity>(found).ToArray();
        }
        public static Rarity[] EnemyRarities(int tier,bool hasBoss,bool mechanic)
        {
            var found=new System.Collections.Generic.SortedSet<Rarity>();
            for(int roll=0;roll<100;roll++)
            {
                var ordinary=TierRewardRules.DropRarity(false,tier,roll);
                if(!mechanic||ordinary>=Rarity.Epic)found.Add(ordinary);
                if(hasBoss)found.Add(TierRewardRules.DropRarity(true,tier,roll));
            }
            return new System.Collections.Generic.List<Rarity>(found).ToArray();
        }
        public static int SlotCount(int mode,int tier,ItemSlot slot)
        {
            int count=0;for(int i=0;i<AdventureRewardRules.EquipmentCount(mode,tier);i++)
                if(AdventureRewardRules.EquipmentSlot(mode,i)==slot)count++;
            return count;
        }
    }
}
