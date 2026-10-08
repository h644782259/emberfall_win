using System;

namespace Emberfall
{
    /// <summary>Combat resources are keyed by learned skill, never by page or keyboard key.</summary>
    public sealed class SkillRuntime
    {
        public Action<float> EnergyChanged;
        public const float MaximumEnergy = 100f;
        public const float EnergyPerSecond = 4f;
        public HeroClass HeroClass { get; }
        public float Energy { get; private set; } = MaximumEnergy;
        private readonly float[] cooldowns = new float[GameBalance.SkillCount];

        // One selected active skill per class stores two sequential uses.
        public static int StockSkill(HeroClass hero){return SkillStockRules.Skill(hero);}
        public static float StockSeconds(HeroClass hero){return SkillStockRules.Seconds(hero);}
        private int stock=2;
        private float stockRemaining,stockPeriod,stockActionLock;
        public Func<int,float,float,bool> CommitStock;
        public Action<int,float,float> StockChanged;
        public bool StockPersistenceFailed {get;private set;}
        public int Charges(int skill){return skill==StockSkill(HeroClass)?stock:0;}
        public float RechargeRemaining(int skill){return skill==StockSkill(HeroClass)?stockRemaining:0;}
        public float RechargePeriod(int skill){return skill==StockSkill(HeroClass)?stockPeriod:0;}
        public void RestoreStock(int count,float remaining,float period)
        {
            stock=Math.Max(0,Math.Min(2,count));float basis=StockSeconds(HeroClass);
            stockPeriod=float.IsNaN(period)||float.IsInfinity(period)||period<basis*.7f||period>basis?basis:period;
            stockRemaining=stock==2?0:float.IsNaN(remaining)||float.IsInfinity(remaining)||remaining<=0?stockPeriod:Math.Min(stockPeriod,remaining);
            stockActionLock=stock<2?(HeroClass==HeroClass.Vanguard?.55f:HeroClass==HeroClass.Ranger?.6f:.25f):0;
        }
        private void AdvanceStock(float seconds)
        {
            if(stock>=2)return;
            stockRemaining-=seconds;
            while(stockRemaining<=.00001f&&stock<2){stock++;if(stock<2)stockRemaining+=stockPeriod;}
            if(stock==2)stockRemaining=0;
            if(StockChanged!=null)StockChanged(stock,stockRemaining,stockPeriod);
        }

        public SkillRuntime(HeroClass heroClass)
        {
            if ((int)heroClass < 0 || (int)heroClass >= GameBalance.ClassNames.Length)
                throw new ArgumentOutOfRangeException(nameof(heroClass));
            HeroClass = heroClass;stockPeriod=StockSeconds(heroClass);
        }

        // Detached runtime transfer: never restores energy or clears an existing timer.
        internal SkillRuntime CopyForClass(HeroClass target,float elapsed,float currentEnergy,SkillRuntime cooldownFloor=null)
        {
            var copy=new SkillRuntime(target);elapsed=ValidElapsed(elapsed);
            copy.Energy=float.IsNaN(currentEnergy)||float.IsInfinity(currentEnergy)?0:Math.Max(0,Math.Min(MaximumEnergy,currentEnergy));
            for(int i=0;i<cooldowns.Length;i++)copy.cooldowns[i]=Math.Max(Math.Max(0,cooldowns[i]-elapsed),cooldownFloor==null?0:cooldownFloor.Remaining(i));
            if(target==HeroClass){copy.RestoreStock(stock,stockRemaining,stockPeriod);copy.stockActionLock=stockActionLock;copy.AdvanceStock(elapsed);}
            return copy;
        }
        private static float ValidElapsed(float value){return value<0||float.IsNaN(value)||float.IsInfinity(value)?0:value;}

        public float Remaining(int skill)
        {
            if(skill==StockSkill(HeroClass))return Math.Max(cooldowns[skill],stock>0?stockActionLock:Math.Max(stockActionLock,stockRemaining));
            return skill >= 0 && skill < cooldowns.Length ? cooldowns[skill] : 0;
        }

        public void Advance(float deltaTime)
        {
            if (deltaTime <= 0 || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;
            for (int i = 0; i < cooldowns.Length; i++) cooldowns[i] = Math.Max(0, cooldowns[i] - deltaTime);
            stockActionLock=Math.Max(0,stockActionLock-deltaTime);AdvanceStock(deltaTime);
            RestoreEnergy(deltaTime * EnergyPerSecond);
        }

        public bool TryConsume(int skill, int rank, float cooldownMultiplier = 1f)
        {
            StockPersistenceFailed=false;
            if (skill < 0 || skill >= cooldowns.Length || GameBalance.IsPassive(skill) || rank < 1 || rank > 3 || Remaining(skill) > 0) return false;
            float cost = GameBalance.SkillEnergyCost(HeroClass, skill);
            if (Energy < cost) return false;
            if(skill==StockSkill(HeroClass))
            {
                float period=stock==2?ModifiedCooldown(StockSeconds(HeroClass),cooldownMultiplier):stockPeriod;
                float remaining=stock==2?period:stockRemaining;
                if(CommitStock!=null&&!CommitStock(stock-1,remaining,period)){StockPersistenceFailed=true;return false;}
                stock--;stockPeriod=period;stockRemaining=remaining;stockActionLock=HeroClass==HeroClass.Vanguard?.55f:HeroClass==HeroClass.Ranger?.6f:.25f;
                if(StockChanged!=null)StockChanged(stock,stockRemaining,stockPeriod);
            }
            Energy -= cost;
            if(cost>0 && EnergyChanged!=null)EnergyChanged(-cost);
            if(skill!=StockSkill(HeroClass))cooldowns[skill] = ModifiedCooldown(GameBalance.EffectiveCooldown(HeroClass, skill, rank), cooldownMultiplier);
            return true;
        }

        public static float ModifiedCooldown(float baseSeconds, float multiplier)
        {
            if (float.IsNaN(baseSeconds) || float.IsInfinity(baseSeconds) || baseSeconds <= 0) return 1f;
            if (float.IsNaN(multiplier) || float.IsInfinity(multiplier)) multiplier = 1f;
            // Run bonuses never compound an existing timer or create zero cooldown.
            return Math.Max(1f, baseSeconds * Math.Max(.7f, Math.Min(1f, multiplier)));
        }

        public void RestoreEnergy(float amount)
        {
            if (amount <= 0 || float.IsNaN(amount) || float.IsInfinity(amount)) return;
            float before=Energy;Energy = Math.Min(MaximumEnergy, Energy + amount);
            if(EnergyChanged!=null)EnergyChanged(Energy-before);
        }

        public void ReduceCooldowns(float seconds)
        {
            if (seconds <= 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
            for (int i = 0; i < cooldowns.Length; i++) cooldowns[i] = Math.Max(0, cooldowns[i] - seconds);
            AdvanceStock(seconds);
        }

        public void ResetCooldowns() { Array.Clear(cooldowns, 0, cooldowns.Length);stock=2;stockRemaining=stockActionLock=0;if(StockChanged!=null)StockChanged(stock,0,stockPeriod); }

        public void FillEnergy() { Energy = MaximumEnergy; }
    }
}
