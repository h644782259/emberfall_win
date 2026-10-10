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

        public void ReduceNonUltimateCooldowns(float seconds)
        {
            if(seconds<=0||float.IsNaN(seconds)||float.IsInfinity(seconds))return;
            for(int i=0;i<cooldowns.Length;i++)if(i!=9)cooldowns[i]=Math.Max(0,cooldowns[i]-seconds);
            AdvanceStock(seconds);
        }

        public void ResetCooldowns() { Array.Clear(cooldowns, 0, cooldowns.Length);stock=2;stockRemaining=stockActionLock=0;if(StockChanged!=null)StockChanged(stock,0,stockPeriod); }

        public void FillEnergy() { Energy = MaximumEnergy; }
    }
}

namespace Emberfall
{
    internal struct RelicGemProc
    {
        internal float Energy,Cooldown,OtherCooldown,Burst;
        internal bool ResetDodge;
    }
    // Only successful player actions advance these chains; proc damage never feeds them.
    internal sealed class RelicGemRuntime
    {
        private readonly int[] ranks=new int[3],variants=new int[3],masks=new int[3];
        private readonly float[] windows=new float[3],gates=new float[3];
        private float echoWindow,dodgeWindow;private int echoes;
        internal void Configure(int index,int rank,int variant)
        {
            rank=System.Math.Max(0,System.Math.Min(3,rank));variant=variant==1?1:0;
            if(ranks[index]==rank&&variants[index]==variant)return;
            ranks[index]=rank;variants[index]=variant;masks[index]=0;windows[index]=gates[index]=0;
            if(index==0){echoes=0;echoWindow=0;}if(index==2)dodgeWindow=0;
        }
        internal void Reset(){for(int i=0;i<3;i++){masks[i]=0;windows[i]=gates[i]=0;}echoes=0;echoWindow=dodgeWindow=0;}
        internal void Advance(float dt)
        {
            if(dt<=0||float.IsNaN(dt)||float.IsInfinity(dt))return;
            for(int i=0;i<3;i++){gates[i]=System.Math.Max(0,gates[i]-dt);windows[i]=System.Math.Max(0,windows[i]-dt);if(windows[i]==0)masks[i]=0;}
            echoWindow=System.Math.Max(0,echoWindow-dt);dodgeWindow=System.Math.Max(0,dodgeWindow-dt);if(echoWindow==0)echoes=0;
        }
        private bool Chain(int index,int slot,int required)
        {
            if(gates[index]>0)return false;
            if(windows[index]<=0)windows[index]=6;
            masks[index]|=1<<slot;int bits=masks[index],count=0;while(bits!=0){count+=bits&1;bits>>=1;}
            if(count<required)return false;masks[index]=0;windows[index]=0;gates[index]=8;return true;
        }
        internal RelicGemProc SkillCast(int slot)
        {
            var p=new RelicGemProc();if(slot<0||slot>=GameBalance.SkillCount||GameBalance.IsPassive(slot))return p;
            if(ranks[0]>0){if(variants[0]==0){if(Chain(0,slot,2))p.Energy=8+4*ranks[0];}
                else if(slot==9){echoes=3;echoWindow=8;}else if(echoes>0&&echoWindow>0){echoes--;p.Energy=6+2*ranks[0];}}
            if(ranks[1]>0){if(variants[1]==0){if(Chain(1,slot,3))p.Cooldown=.5f+.5f*ranks[1];}else if(slot==9)p.OtherCooldown=1+ranks[1];}
            if(ranks[2]>0){if(variants[2]==0&&dodgeWindow>0&&gates[2]<=0){p.Burst=.4f+.3f*ranks[2];dodgeWindow=0;gates[2]=6;}
                else if(variants[2]==1&&Chain(2,slot,2)){p.ResetDodge=true;dodgeWindow=4;}}
            return p;
        }
        internal RelicGemProc PerfectDodge()
        {
            var p=new RelicGemProc();if(ranks[2]<=0)return p;
            if(variants[2]==0&&gates[2]<=0)dodgeWindow=4;
            else if(variants[2]==1&&dodgeWindow>0){p.Burst=.6f+.4f*ranks[2];dodgeWindow=0;}
            return p;
        }
    }
}
