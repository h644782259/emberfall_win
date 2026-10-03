using System;
using System.Collections.Generic;
namespace Emberfall
{
    public sealed class PracticeResultPresentation
    {
        public readonly struct Row
        {
            public readonly string Label,A,B;
            public Row(string label,string a,string b){Label=label;A=a;B=b;}
        }
        public readonly List<Row> Rows=new List<Row>();
        private static string Number(float n){return n.ToString("0.0");}
        private static string Value(CampPracticeRecord r,Func<CampPracticeRecord,string> read){return r==null?"—":read(r);}
        private void Add(string label,CampPracticeRecord a,CampPracticeRecord b,Func<CampPracticeRecord,string> read){Rows.Add(new Row(label,Value(a,read),Value(b,read)));}
        public PracticeResultPresentation(CampPracticeRecord a,CampPracticeRecord b)
        {
            Add("场景",a,b,r=>CampPracticeRecord.ScenarioLabel(r.Scenario));
            Add("实际时间 / 上限",a,b,r=>Number(r.Elapsed)+" / "+r.Duration+"秒");
            Add("结束原因",a,b,r=>r.EndReason);
            Add("总伤害",a,b,r=>Number(r.ActualDamage));
            Add("DPS（按实际秒数）",a,b,r=>r.Elapsed>0?Number(r.DamagePerSecond):"—");
            Add("能量消耗",a,b,r=>Number(r.EnergySpent));Add("实际能量回复",a,b,r=>Number(r.EnergyRestored));
            Add("实际受伤",a,b,r=>Number(r.DamageTaken));Add("有效治疗",a,b,r=>Number(r.EffectiveHealing));
            Add("存活 / 完成目标",a,b,r=>(r.Survived?"存活":"倒下")+" / "+(r.ObjectiveCompleted?"完成":"未完成"));
            Add("断供时刻",a,b,r=>r.SupplyBrokenAt<0?"未发生":Number(r.SupplyBrokenAt)+"秒");
            Add("击杀顺序",a,b,r=>r.KillOrder.Count==0?"无":string.Join(" → ",r.KillOrder));
            int skills=Math.Max(a==null?0:a.SkillCount,b==null?0:b.SkillCount);
            if(a!=null)foreach(int skill in a.SkillCasts.Keys)skills=Math.Max(skills,skill+1);
            if(b!=null)foreach(int skill in b.SkillCasts.Keys)skills=Math.Max(skills,skill+1);
            for(int i=0;i<skills;i++)
            {
                int slot=i;
                Add("技能 "+(slot+1)+" 造成扣血的施法 / 施放",a,b,r=>{int total,effective;r.SkillCasts.TryGetValue(slot,out total);r.EffectiveSkillCasts.TryGetValue(slot,out effective);return r.SkillLabel(slot)+"\n"+effective+" / "+total;});
            }
            var keys=new SortedSet<string>(StringComparer.Ordinal);
            if(a!=null)foreach(string key in a.Mechanisms.Keys)keys.Add(key);
            if(b!=null)foreach(string key in b.Mechanisms.Keys)keys.Add(key);
            foreach(string key in keys)Add("机制 · "+key,a,b,r=>{int value;r.Mechanisms.TryGetValue(key,out value);return value.ToString();});
        }
    }
}
