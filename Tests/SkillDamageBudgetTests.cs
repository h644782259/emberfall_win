using System;using Emberfall;
public static class SkillDamageBudgetTests
{
 private static int checks;
 private static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 private static bool Near(float a,float b){return Math.Abs(a-b)<.001f;}
 public static string Run()
 {
  Check(Near(.95f*1.6f*9+2f*1.6f,16.88f),"reproduce old blade storm");
  Check(Near(.9f*1.6f*10+2f*1.6f,17.6f),"reproduce old arcane storm");
  Check(Near(9*(1.15f+.4f)*1.6f,22.32f),"reproduce old fan pointblank ceiling");
  foreach(HeroClass hero in new[]{HeroClass.Vanguard,HeroClass.Arcanist,HeroClass.Ranger})
  {
   float prior=0;
   for(int rank=1;rank<=3;rank++)
   {
    var field=SkillDamageBudgets.EarlyField(hero,rank);
    Check(field.Ticks>0&&field.Ticks<=16,"bounded field ticks");
    Check(field.TotalCoefficient>prior,"rank upgrade meaningfully improves field");prior=field.TotalCoefficient;
    Check(field.TickCoefficient>0&&field.FinisherCoefficient>=0,"finite positive allocated events");
    Check(field.TotalCoefficient<8,"early sustained field no longer outvalues a full ultimate");
    int steps=SkillDamageBudgets.AdvancedSteps(hero,9,rank);float total=0;
    for(int step=0;step<steps;step++)total+=SkillDamageBudgets.AdvancedImpact(hero,9,rank,step);
    total*=SkillDamageBudgets.AdvancedScale(hero,9)*CombatBalance.RankPower(rank);
    Check(total>field.TotalCoefficient,"ultimate core sequence exceeds early field before residual effects");
   }
  }
  Check(SkillDamageBudgets.AdvancedScale(HeroClass.Arcanist,4)!=SkillDamageBudgets.AdvancedScale(HeroClass.Arcanist,9),"chain and ultimate have independent budgets");
  Check(Near(SkillDamageBudgets.EarlyField(HeroClass.Vanguard,3).TotalCoefficient,6.4f),"blade field exact total");
  Check(Near(SkillDamageBudgets.EarlyField(HeroClass.Arcanist,3).TotalCoefficient,6.8f),"arcane field exact total");
  Check(Near(SkillDamageBudgets.EarlyField(HeroClass.Ranger,3).TotalCoefficient,7.2f),"arrow field exact total");
  for(int rank=1;rank<=3;rank++)
  {
   var volley=new ProjectileVolleyBudget<object>(100,SkillDamageBudgets.FanTargetCap(rank));var boss=new object();float total=0;
   float first=volley.Apply(boss,new CombatDamage(100*SkillDamageBudgets.OpeningImpact(HeroClass.Ranger,0,rank),false),false).Amount;
   total+=first;
   for(int arrow=1;arrow<5+(rank-1)*2;arrow++)total+=volley.Apply(boss,new CombatDamage(100*SkillDamageBudgets.OpeningImpact(HeroClass.Ranger,0,rank),false),false).Amount;
   if(rank==3)for(int arrow=0;arrow<9;arrow++)total+=volley.Apply(boss,new CombatDamage(64,false),true).Amount;
   Check(total<=100*SkillDamageBudgets.FanTargetCap(rank)+.01f,"shared direct/explosion ceiling");
   Check(Near(first,100*SkillDamageBudgets.OpeningImpact(HeroClass.Ranger,0,rank)),"first direct impact unchanged");
   Check(Near(volley.Apply(new object(),new CombatDamage(first,false),false).Amount,first),"another target gets independent first impact");
  }
  var crit=new ProjectileVolleyBudget<object>(100,6.4f);var target=new object();float critTotal=0;
  for(int n=0;n<100;n++)critTotal+=crit.Apply(target,new CombatDamage(100*1.95f,true,1.95f),n%2==0).Amount;
  Check(Near(critTotal,640*1.95f),"cap uses uncritical contribution once, preserves real critical multiplier");
  Check(crit.Apply(target,new CombatDamage(100,false),false).Amount==0,"no over-budget damage later");
  for(int n=0;n<1000;n++)crit.Apply(new object(),new CombatDamage(10,false),false);
  Check(crit.TrackedTargets==64,"cast target memory bounded");
  Check(crit.Apply(null,new CombatDamage(10,false),false).Amount==0,"null target safe");
  Check(crit.Apply(target,new CombatDamage(float.NaN,false),false).Amount==0,"invalid damage safe");
  Check(Near(SkillDamageBudgets.MeteorAftermath(3).TotalCoefficient,1.6f),"meteor residual no hidden fivefold budget");
  Check(SkillDamageBudgets.TickCount(0,.5f)==1&&SkillDamageBudgets.TickCount(2,.5f)==5&&SkillDamageBudgets.TickCount(4.5f,.6f)==8,"inclusive startup tick conventions match scheduler");
  for(int rank=1;rank<=3;rank++){
   float expected=.32f*(1+(rank-1)*.3f)*(rank+2)*1.65f;
   Check(Near(SkillDamageBudgets.RangerVault(rank),expected),"vault preserves prior volley ceiling");
   Check(SkillDamageBudgets.RangerVault(rank)/GameBalance.EffectiveCooldown(HeroClass.Ranger,4,rank)<.42f,"vault cooldown-normalized coefficient cap");
   Check(Near(SkillDamageBudgets.FlameRideTick/SkillDamageBudgets.FlameRideInterval,1.3f),"nonstacking flame ride active DPS");
  }
  for(int rank=1;rank<=3;rank++){int steps=SkillDamageBudgets.AdvancedSteps(HeroClass.Ranger,9,rank);float budget=0;for(int hit=0;hit<steps;hit++)budget+=SkillDamageBudgets.AdvancedImpact(HeroClass.Ranger,9,rank,hit);Check(budget>=((steps-1)*1.35f+8.5f)*1.4f,"ranger ultimate gains at least forty percent damage at every rank");}
  return "PASS: "+checks+" per-skill coefficient and shared-volley budget assertions";
 }
}
