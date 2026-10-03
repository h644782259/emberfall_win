using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace Emberfall
{
    public enum CampPracticeScenario { Stationary, Moving, FrontAndSupplier, GuardAndWispPressure, SupplierPressure }
    // Captures observed events only; no stat-derived DPS or synthetic aggregate score.
    public sealed class CampPracticeRecord
    {
        public readonly CampPracticeScenario Scenario;
        public readonly int Duration, Seed;
        public readonly string Configuration;
        public readonly string ConfigurationSummary;
        public bool Started { get; private set; } = true;
        public bool UsesEnemyAI { get { return Scenario==CampPracticeScenario.GuardAndWispPressure||Scenario==CampPracticeScenario.SupplierPressure; } }
        public bool HasSupplier { get { return Scenario==CampPracticeScenario.FrontAndSupplier||Scenario==CampPracticeScenario.SupplierPressure; } }
        public float DamageTaken { get; private set; }
        public float EffectiveHealing { get; private set; }
        public bool Survived { get; private set; } = true;
        public bool ObjectiveCompleted { get; private set; }
        public float SupplyBrokenAt { get; private set; } = -1;
        private readonly List<string> killOrder=new List<string>();
        public IReadOnlyList<string> KillOrder { get; private set; }
        public void Prepare(){if(Elapsed==0&&!Finished)Started=false;}
        public bool Start(){if(Started||Finished)return false;Started=true;return true;}
        public void IncomingDamage(float amount){if(Started&&!Finished&&Valid(amount))DamageTaken+=amount;}
        public void Healing(float amount){if(Started&&!Finished&&Valid(amount))EffectiveHealing+=amount;}
        public void Defeat(string identity,bool supplier,bool cleared)
        {
            if(!Started||Finished)return;
            identity=identity=="Guardian"?"守卫":identity=="Wisp"?"魔灵":identity=="Goblin"?"哥布林":identity=="Slime"?"史莱姆":identity;
            killOrder.Add(identity+" @ "+Elapsed.ToString("0.00")+"s");
            if(supplier&&SupplyBrokenAt<0){SupplyBrokenAt=Elapsed;Mechanism("断供");}
            if(cleared&&UsesEnemyAI){ObjectiveCompleted=true;RequestCombatFinish("目标全部击败");}
        }
        public void PlayerDefeated(){if(!Finished){Survived=false;RequestCombatFinish("角色倒下 · 记录提前结束");}}
        private string pendingCombatEnd;
        private void RequestCombatFinish(string reason)
        {
            // Same-action death overrides clearing the targets. The action's existing
            // energy, mechanism and incoming-damage callbacks settle before sealing.
            if(pendingCombatEnd==null||!Survived)pendingCombatEnd=reason;
            CombatImpactBatch.AfterCurrentAction(CompleteCombatAction);
        }
        private void CompleteCombatAction(){Finish(pendingCombatEnd);}
        public float Elapsed { get; private set; }
        public float ActualDamage { get; private set; }
        public float EnergySpent { get; private set; }
        public float EnergyRestored { get; private set; }
        public bool Finished { get; private set; }
        public string EndReason { get; private set; }
        private readonly Dictionary<string,int> mechanisms=new Dictionary<string,int>();
        private readonly Dictionary<int,int> skillCasts=new Dictionary<int,int>(),effectiveSkillCasts=new Dictionary<int,int>();
        public IReadOnlyDictionary<string,int> Mechanisms { get; private set; }
        public IReadOnlyDictionary<int,int> SkillCasts { get; private set; }
        public IReadOnlyDictionary<int,int> EffectiveSkillCasts { get; private set; }
        public readonly string HeroIdentity;
        public readonly int Level, RulesVersion;
        private readonly string[] skillNames;
        public int SkillCount { get { return skillNames.Length; } }
        public string SkillLabel(int skill){return skill>=0&&skill<skillNames.Length?skillNames[skill]:"技能 "+(skill+1);}
        public static string ScenarioLabel(CampPracticeScenario scenario)
        {switch(scenario){case CampPracticeScenario.Stationary:return "静止单目标";case CampPracticeScenario.Moving:return "持续移动目标";case CampPracticeScenario.FrontAndSupplier:return "前排 + 后排供能";case CampPracticeScenario.GuardAndWispPressure:return "守卫 + 魔灵 · 受压";default:return "前排 + 供能者 · 受压";}}
        public float DamagePerSecond { get { return Elapsed>0?ActualDamage/Elapsed:0; } }
        public CampPracticeRecord FrozenCopy()
        {
            if(!Finished)throw new InvalidOperationException("Only finished practice records may be pinned.");
            var copy=new CampPracticeRecord(Scenario,Duration,Configuration,Seed,ConfigurationSummary,HeroIdentity,Level,skillNames,RulesVersion);
            copy.Started=Started;copy.Elapsed=Elapsed;copy.ActualDamage=ActualDamage;copy.EnergySpent=EnergySpent;copy.EnergyRestored=EnergyRestored;
            copy.DamageTaken=DamageTaken;copy.EffectiveHealing=EffectiveHealing;copy.Survived=Survived;copy.ObjectiveCompleted=ObjectiveCompleted;copy.SupplyBrokenAt=SupplyBrokenAt;
            foreach(var entry in mechanisms)copy.mechanisms.Add(entry.Key,entry.Value);
            foreach(var entry in skillCasts)copy.skillCasts.Add(entry.Key,entry.Value);
            foreach(var entry in effectiveSkillCasts)copy.effectiveSkillCasts.Add(entry.Key,entry.Value);
            copy.killOrder.AddRange(killOrder);copy.Finish(EndReason);return copy;
        }
        private readonly Dictionary<int,int> casts=new Dictionary<int,int>();
        private readonly HashSet<int> hitCasts=new HashSet<int>();
        public CampPracticeRecord(CampPracticeScenario scenario,int duration,string configuration,int seed=7319,string configurationSummary="",string heroIdentity="",int level=0,string[] skillNames=null,int rulesVersion=2)
        {if(duration!=10&&duration!=60)throw new ArgumentOutOfRangeException("duration");Scenario=scenario;Duration=duration;Configuration=configuration;ConfigurationSummary=configurationSummary;Seed=seed;
            HeroIdentity=heroIdentity;Level=level;RulesVersion=rulesVersion;this.skillNames=skillNames==null?new string[0]:(string[])skillNames.Clone();
            Mechanisms=new ReadOnlyDictionary<string,int>(mechanisms);SkillCasts=new ReadOnlyDictionary<int,int>(skillCasts);EffectiveSkillCasts=new ReadOnlyDictionary<int,int>(effectiveSkillCasts);KillOrder=killOrder.AsReadOnly();
        }
        private static bool Valid(float n){return n>0&&!float.IsNaN(n)&&!float.IsInfinity(n);}
        public void Advance(float dt){if(Started&&!Finished&&pendingCombatEnd==null&&Valid(dt)){Elapsed=Math.Min(Duration,Elapsed+dt);if(Elapsed>=Duration)Finish("计时完成");}}
        public void ConfirmedHealthLoss(float amount,int castId=0){if(Started&&!Finished&&Valid(amount)){Damage(amount);Hit(castId);}}
        public void Damage(float amount){if(Started&&!Finished&&Valid(amount))ActualDamage+=amount;}
        public void Energy(float delta){if(!Started||Finished||float.IsNaN(delta)||float.IsInfinity(delta))return;if(delta<0)EnergySpent-=delta;else EnergyRestored+=delta;}
        private static void Count<T>(Dictionary<T,int> counts,T key){int n;counts.TryGetValue(key,out n);counts[key]=n+1;}
        public void Cast(int id,int skill){if(!Started||Finished||id<=0||skill<0||casts.ContainsKey(id))return;casts[id]=skill;Count(skillCasts,skill);}
        public void Hit(int id){int skill;if(Started&&!Finished&&casts.TryGetValue(id,out skill)&&hitCasts.Add(id))Count(effectiveSkillCasts,skill);}
        public void Mechanism(string key){if(Started&&!Finished&&!string.IsNullOrEmpty(key))Count(mechanisms,key);}
        public void Finish(string reason){if(Finished)return;Finished=true;EndReason=reason;}
        public bool ComparableConditions(CampPracticeRecord other){return other!=null&&Finished&&other.Finished&&Survived&&other.Survived&&(Elapsed==Duration||ObjectiveCompleted)&&(other.Elapsed==other.Duration||other.ObjectiveCompleted)&&Scenario==other.Scenario&&Duration==other.Duration&&Seed==other.Seed&&RulesVersion==other.RulesVersion&&HeroIdentity==other.HeroIdentity&&Level==other.Level;}
        public string Comparison(CampPracticeRecord other){return !ComparableConditions(other)?"提前结束或场景 / 时长 / 种子 / 职业等级 / 目标规则不同，不能直接比较":Configuration==other.Configuration?"同配置、同场景实测；操作差异仍影响结果":"配置不同的 A/B 实测；同场景时长，详见各自固定快照，操作差异仍影响结果";}
    }
}
