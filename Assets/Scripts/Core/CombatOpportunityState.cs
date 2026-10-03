using System;
namespace Emberfall
{
    public enum CombatOpportunityKind { None, Counter, Shatter, Reignite, BurnFinale, PoisonDetonation, EmpoweredContract, BurnCash, EmpoweredHit, MasteryCombo }
    // A read-only observation, never a promise that a future projectile will hit.
    public readonly struct CombatOpportunityState
    {
        public readonly CombatOpportunityKind Kind;
        public readonly float Remaining, Duration;
        public float Fraction {get{return Duration>0?Math.Min(1,Remaining/Duration):0;}}
        public string Symbol {get{return Kind==CombatOpportunityKind.Counter?"反":Kind==CombatOpportunityKind.Shatter?"碎":Kind==CombatOpportunityKind.Reignite?"续":Kind==CombatOpportunityKind.BurnFinale?"兑":Kind==CombatOpportunityKind.PoisonDetonation?"毒":Kind==CombatOpportunityKind.EmpoweredContract?"契":Kind==CombatOpportunityKind.MasteryCombo?"连":"";}}
        public readonly int Count, Receipt;
        public readonly string BlockReason;
        public bool Window {get{return (Kind>=CombatOpportunityKind.Counter&&Kind<=CombatOpportunityKind.EmpoweredContract||Kind==CombatOpportunityKind.MasteryCombo)&&Remaining>0;}}
        public bool Actionable {get{return Window&&string.IsNullOrEmpty(BlockReason);}}
        public CombatOpportunityState(CombatOpportunityKind kind,float remaining,int count=0,string blockReason="",int receipt=0,float duration=0){Duration=float.IsNaN(duration)||float.IsInfinity(duration)?0:Math.Max(0,duration);Kind=remaining>0?kind:CombatOpportunityKind.None;Remaining=float.IsNaN(remaining)?0:Math.Max(0,remaining);Count=count;Receipt=receipt;BlockReason=blockReason??"";}
        public CombatOpportunityState Blocked(string reason){return new CombatOpportunityState(Kind,Remaining,Count,reason,Receipt,Duration);}
        public string Caption
        {
            get
            {
                string title=Kind==CombatOpportunityKind.Counter?"反击":Kind==CombatOpportunityKind.Shatter?"碎冰":Kind==CombatOpportunityKind.Reignite?"续燃":Kind==CombatOpportunityKind.BurnFinale?"兑燃":Kind==CombatOpportunityKind.PoisonDetonation?"三毒":Kind==CombatOpportunityKind.EmpoweredContract?"强化":Kind==CombatOpportunityKind.BurnCash?"兑燃 ×"+Count:Kind==CombatOpportunityKind.EmpoweredHit?"强化命中":Kind==CombatOpportunityKind.MasteryCombo?"连击":"";
                return title.Length==0?"":Window?title+" "+Remaining.ToString("0.0"):title;
            }
        }
    }
}
