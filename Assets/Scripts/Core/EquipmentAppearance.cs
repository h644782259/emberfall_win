using UnityEngine;

namespace Emberfall
{
    /// <summary>Shared visual progression for worn gear and its world pickup.</summary>
    public struct EquipmentAppearance
    {
        public readonly int Tier;
        public readonly int LevelBand;
        public readonly int RarityRank;
        public readonly int UpgradeRank;
        public readonly Color Metal;
        public readonly Color Accent;
        public readonly Color Glow;

        public EquipmentAppearance(ItemData item)
        {
            int level = Mathf.Clamp(item.level, 1, ProgressionService.MaximumLevel);
            LevelBand = Mathf.Clamp(level/10,0,10);
            Tier = Mathf.Min(4, 1 + (level - 1) / 25);
            RarityRank = Mathf.Clamp((int)item.rarity, 0, 3);
            UpgradeRank = Mathf.Clamp(item.upgradeLevel, 0, ProgressionService.MaximumUpgrade);
            Accent = GameBalance.RarityColor((Rarity)RarityRank);
            Metal = Color.Lerp(new Color(.42f, .48f, .55f), new Color(.84f, .89f, .94f),
                Mathf.Clamp01((Tier - 1) / 3f*.6f+RarityRank*.13f));
            Glow = Color.Lerp(Accent, Color.white, .18f + UpgradeRank * .025f);
        }

        public bool HasRunes { get { return UpgradeRank >= 3 || RarityRank >= 1; } }
        public bool HasAura { get { return UpgradeRank >= 7 || RarityRank >= 2; } }
        public bool HasCrown { get { return UpgradeRank >= 10 || RarityRank == 3; } }
    }
}
