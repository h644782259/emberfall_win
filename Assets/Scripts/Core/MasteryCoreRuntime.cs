using System;
namespace Emberfall
{
    public static class MasteryCoreRules
    {
        public const int InitialInvestment = MasteryProgressionRules.InitialInvestment, EnhancedInvestment = MasteryProgressionRules.EnhancedInvestment;
        public static int Tier(int invested) { return invested < InitialInvestment ? 0 : invested < EnhancedInvestment ? 1 : 2; }
    }
    public struct MasteryResourceProc
    {
        public readonly float Energy, CooldownReduction;
        public MasteryResourceProc(float energy, float cooldownReduction) { Energy = energy; CooldownReduction = cooldownReduction; }
    }
    /// <summary>One runtime-only core. Call with confirmed combat events, advance only
    /// during combat, and Reset on owner/room epochs. No permanent profile mutations.</summary>
    public sealed class MasteryCoreRuntime
    {
        public const float ComboDuration=6f;
        public int Core { get; private set; } = -1;
        public int Tier { get; private set; }
        public float WardReduction { get { return Core == (int)MasteryType.Guard && Tier > 0 ? Tier == 1 ? .15f : .25f : 0; } }
        public float ComboRemaining { get { return Core == (int)MasteryType.Offense && Tier > 0 && cooldown <= 0 ? comboRemaining : 0; } }
        private float cooldown, comboRemaining, energySpent;
        private readonly CastFirstHitHistory standaloneCasts=new CastFirstHitHistory();
        public void Configure(int core, int invested)
        {
            int tier = core >= 0 && core < 4 ? MasteryCoreRules.Tier(invested) : 0;
            if (tier == 0) core = -1;
            if (Core == core && Tier == tier) return;
            Core = core; Tier = tier; Reset();
        }
        public void Reset() { cooldown = comboRemaining = energySpent = 0; standaloneCasts.Clear(); }
        public void Advance(float dt)
        {
            if (!Finite(dt) || dt <= 0) return;
            cooldown = Math.Max(0, cooldown - dt); comboRemaining = Math.Max(0, comboRemaining - dt);
        }
        public void SkillHit(int castId) { SkillHit(standaloneCasts.Get(castId)); }
        public void SkillHit(CastFirstHitReceipt cast)
        {
            if(Core!=(int)MasteryType.Offense||Tier==0||cast==null||!cast.FirstCoreHit())return;
            // First impact during cooldown is consumed; later ticks cannot bank it.
            if(cooldown<=0)comboRemaining = ComboDuration;
        }
        public float BasicHit()
        {
            if (Core != (int)MasteryType.Offense || Tier == 0 || comboRemaining <= 0 || cooldown > 0) return 0;
            comboRemaining = 0; cooldown = Tier == 1 ? 6 : 4;
            return Tier == 1 ? .60f : 1.00f;
        }
        public float DamageTaken(float healthFraction)
        {
            if (Core != (int)MasteryType.Vitality || Tier == 0 || cooldown > 0 || !Finite(healthFraction) || healthFraction <= 0 || healthFraction >= .5f) return 0;
            cooldown = Tier == 1 ? 12 : 10;
            return Tier == 1 ? .03f : .05f;
        }
        public float PerfectDodge()
        {
            if (Core != (int)MasteryType.Guard || Tier == 0 || cooldown > 0) return 0;
            cooldown = Tier == 1 ? 8 : 6;
            return Tier == 1 ? 2 : 3;
        }
        public MasteryResourceProc SkillSpent(float energy)
        {
            if (Core != (int)MasteryType.Technique || Tier == 0 || cooldown > 0 || !Finite(energy) || energy <= 0) return default(MasteryResourceProc);
            float threshold = Tier == 1 ? 60 : 45;
            energySpent = Math.Min(threshold, energySpent + Math.Min(120, energy));
            if (energySpent < threshold) return default(MasteryResourceProc);
            energySpent = 0; cooldown = Tier == 1 ? 8 : 6;
            return Tier == 1 ? new MasteryResourceProc(8, .4f) : new MasteryResourceProc(12, .7f);
        }
        private static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
    }
}
