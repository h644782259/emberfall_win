using System;

namespace Emberfall
{
    /// <summary>
    /// Explicit combat budgets. Level controls the player's progression scale;
    /// dungeon tier is a separate, bounded challenge axis, including at level 100.
    /// Constants are design targets, not claims of engine/device playtesting.
    /// </summary>
    public static class CombatBalance
    {
        public const int MaximumLevel = 100;
        public const int MaximumTier = 100;
        public const int MaximumUpgradeRank = 20;
        public const float UpgradePerRank = .25f;
        public const float MinimumArmorDamageMultiplier = .30f;
        public const float MinimumCombinedDamageMultiplier = .16f;

        public static float UpgradeMultiplier(int rank)
        {
            return 1f + Clamp(rank, 0, MaximumUpgradeRank) * UpgradePerRank;
        }

        /// <summary>
        /// Always evaluates from the immutable original basis. It never compounds
        /// the visible value. Tiny nonzero stats keep their old per-rank minimum;
        /// missing attributes remain zero. Caps and intermediate math cannot overflow.
        /// </summary>
        public static int UpgradeValue(int basis, int rank, int minimumIncrease = 1, int cap = 1000000)
        {
            if (basis <= 0 || cap <= 0) return 0;
            int safeRank = Clamp(rank, 0, MaximumUpgradeRank);
            double linearGrowth = Math.Round((double)basis * .25d * safeRank, MidpointRounding.AwayFromZero);
            double minimumGrowth = (double)Math.Max(0, minimumIncrease) * 5 * safeRank;
            double value = (double)basis + Math.Max(linearGrowth, minimumGrowth);
            return (int)Math.Min(cap, value);
        }

        // Historical 100-rank curve is used only to recover old unenhanced bases.
        public static int LegacyUpgradeValue(int basis,int rank,int minimumIncrease=1,int cap=1000000)
        {
            if(basis<=0||cap<=0)return 0;int safeRank=Clamp(rank,0,100);
            return (int)Math.Min(cap,(double)basis+Math.Max(Math.Round((double)basis*.05d*safeRank,MidpointRounding.AwayFromZero),(double)Math.Max(0,minimumIncrease)*safeRank));
        }
        public static int UpgradeSuccessPercent(int targetRank)
        {return targetRank<1||targetRank>MaximumUpgradeRank?0:95-4*(targetRank-1);}
        public static int ConvertLegacyUpgradeRank(int rank)
        {rank=Clamp(rank,0,100);return rank>20?rank/5:rank;}

        public static float RankPower(int rank)
        {
            return 1f + (Clamp(rank, 1, 3) - 1) * .3f;
        }

        /// <summary>
        /// K grows with defender level, preventing a fixed denominator from turning
        /// ordinary high-level equipment into 97% passive damage reduction.
        /// Even unbounded armor cannot reduce more than 70% by itself. True dodge
        /// invulnerability is handled by the caller before this function is used.
        /// </summary>
        public static float ArmorDamageMultiplier(float armor, int defenderLevel)
        {
            if (float.IsNaN(armor) || armor <= 0) return 1f;
            if (float.IsPositiveInfinity(armor)) return MinimumArmorDamageMultiplier;
            double k = 50d + 4d * Clamp(defenderLevel, 1, MaximumLevel);
            return (float)Math.Max(MinimumArmorDamageMultiplier, k / (k + armor));
        }

        public static float TierHealthMultiplier(int tier)
        {
            double step = Clamp(tier, 1, MaximumTier) - 1d;
            return (float)(1d + .009d * step + .000035d * step * step);
        }

        public static float TierDamageMultiplier(int tier)
        {
            double step = Clamp(tier, 1, MaximumTier) - 1d;
            // Threat grows faster than health: high tiers demand defensive decisions
            // instead of merely extending the same harmless fight indefinitely.
            return (float)(1d + .014d * step + .00008d * step * step);
        }

        public static float DungeonHealthMultiplier(int level)
        {return level<30?1f:1.15f+Math.Min(.35f,(Math.Min(100,level)-30)*.005f);}
        public static float DungeonDamageMultiplier(int level)
        {return level<30?1f:1.10f+Math.Min(.25f,(Math.Min(100,level)-30)*.004f);}

        public static float EnemyHealth(int level, int tier, bool boss, EnemyKind kind)
        {
            int safeLevel = Clamp(level, 1, MaximumLevel);
            float basis;
            if (boss) basis = 750f + 590f * safeLevel;
            else
            {
                switch (kind)
                {
                    case EnemyKind.Goblin: basis = 95f + 36f * safeLevel; break;
                    case EnemyKind.Wisp: basis = 70f + 28f * safeLevel; break;
                    case EnemyKind.Guardian: basis = 180f + 60f * safeLevel; break;
                    default: basis = 80f + 30f * safeLevel; break;
                }
            }
            return basis * TierHealthMultiplier(tier);
        }

        public static float EnemyDamage(int level, int tier, bool boss)
        {
            int safeLevel = Clamp(level, 1, MaximumLevel);
            return (boss ? 35f + 8f * safeLevel : 10f + 3.8f * safeLevel) * TierDamageMultiplier(tier);
        }

        private static int Clamp(int value, int min, int max)
        {
            return value < min ? min : value > max ? max : value;
        }
    }
}
