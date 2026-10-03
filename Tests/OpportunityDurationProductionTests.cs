using System;using System.Reflection;using Emberfall;using UnityEngine;
public static class OpportunityDurationProductionTests
{
 static int checks;
 static void C(bool b,string reason){checks++;if(!b)throw new Exception(reason);}
 static bool Near(float a,float b)=>Math.Abs(a-b)<.0001f;
 static void Pair(float remaining,float duration,float expectedRemaining,float expectedDuration,string why)
 {C(Near(remaining,expectedRemaining)&&Near(duration,expectedDuration),why);}
 static PlayerController Setup(){Time.deltaTime=0;Time.frameCount=0;Time.time=0;var p=new PlayerController();GameSession.Instance=new GameSession{Player=p};return p;}
 static EnemyController Enemy(float z=0,bool boss=false){var e=new EnemyController{IsBoss=boss};e.transform.position=new Vector3(0,0,z);e.StatusEffects=new EnemyStatusEffects{OwnerEnemy=e};typeof(EnemyStatusEffects).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(e.StatusEffects,null);GameSession.Instance.Enemies.Add(e);return e;}
 static void Tick(EnemyStatusEffects status,float delta){Time.frameCount++;Time.deltaTime=delta;typeof(EnemyStatusEffects).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(status,null);}
 public static string Run()
 {
  var owner=Setup();var e=Enemy();var s=e.StatusEffects;
  s.FrostMark(6);Tick(s,2);Pair(s.FrostRemaining,s.FrostWindowDuration,4,6,"frost grant/decay retains actual total");
  s.FrostMark(1);Pair(s.FrostRemaining,s.FrostWindowDuration,4,6,"short frost refresh retains denominator");
  s.FrostMark(8);Pair(s.FrostRemaining,s.FrostWindowDuration,8,8,"long frost refresh replaces total");
  C(s.ConsumeFrost()&&s.FrostRemaining==0,"frost consumption clears opportunity");s.FrostMark(2);Pair(s.FrostRemaining,s.FrostWindowDuration,2,2,"frost regeneration uses new grant");
  s.ConsumeFrost();s.Freeze(5);Tick(s,1);Pair(s.FrostRemaining,s.FrostWindowDuration,4,5,"freeze total survives decay");s.Freeze(1);Pair(s.FrostRemaining,s.FrostWindowDuration,4,5,"short freeze cannot fill meter");s.Freeze(7);Pair(s.FrostRemaining,s.FrostWindowDuration,7,7,"long freeze refresh actual total");
  s.FrostMark(10);Pair(s.FrostRemaining,s.FrostWindowDuration,10,10,"frost-vs-freeze selects matching longer clock");s.Freeze(12);Pair(s.FrostRemaining,s.FrostWindowDuration,12,12,"freeze dominates with own denominator");
  var boss=Enemy(0,true).StatusEffects;boss.Freeze(2);Pair(boss.FrostRemaining,boss.FrostWindowDuration,4,4,"boss freeze armor uses four-second frost mark");Tick(boss,1);boss.Freeze(.1f);Pair(boss.FrostRemaining,boss.FrostWindowDuration,3,4,"boss short refresh not fictitious full");
  s=Enemy().StatusEffects;for(int i=0;i<3;i++)s.Poison(owner,6,1);Tick(s,2);Pair(s.OwnPoisonOpportunityRemaining(owner),s.PoisonWindowDuration,4,6,"poison grant and decay");s.Poison(owner,1,1);Pair(s.PoisonRemaining,s.PoisonWindowDuration,4,6,"short poison refresh retains denominator");s.Poison(owner,8,1);Pair(s.PoisonRemaining,s.PoisonWindowDuration,8,8,"long poison refresh");float stored;C(s.ConsumePoison(owner,1,out stored)&&s.PoisonRemaining==0,"poison consume closes window");for(int i=0;i<3;i++)s.Poison(owner,2,1);Pair(s.OwnPoisonOpportunityRemaining(owner),s.PoisonWindowDuration,2,2,"poison regenerate new total");
  owner.CombatEpoch++;s.Poison(owner,3,1);Pair(s.PoisonRemaining,s.PoisonWindowDuration,3,3,"poison epoch replacement new total");C(s.OwnPoisonOpportunityRemaining(owner)==0,"replacement starts at one stack");
  s=Enemy().StatusEffects;s.Burn(owner,6,6);Tick(s,2);Pair(s.OwnBurnRemaining(owner),s.BurnWindowDuration,4,6,"burn grant and decay");s.Burn(owner,1,1);Pair(s.BurnRemaining,s.BurnWindowDuration,4,6,"short burn refresh retains total");s.Burn(owner,8,8);Pair(s.BurnRemaining,s.BurnWindowDuration,8,8,"long burn refresh");
  var consume=Enemy().StatusEffects;consume.Burn(owner,3,3);Time.deltaTime=0;C(consume.ResolveBurnFinale(owner,2,3)>0&&consume.BurnRemaining==0,"burn finale consumes opportunity");consume.Burn(owner,2,2);Pair(consume.BurnRemaining,consume.BurnWindowDuration,2,2,"burn regeneration duration");Tick(consume,2);C(consume.BurnRemaining==0,"burn expires");consume.Burn(owner,5,5);Pair(consume.BurnRemaining,consume.BurnWindowDuration,5,5,"post-expiry new burn total");
  // Both query and status classes are production. Geometry admission is an explicit managed boundary.
  owner=Setup();var a=Enemy(0);var b=Enemy(1);a.StatusEffects.FrostMark(9);Tick(a.StatusEffects,6);b.StatusEffects.FrostMark(6);Tick(b.StatusEffects,2);
  var result=owner.ElementalOpportunityWindow(1,CombatOpportunityKind.Shatter);Pair(result.Remaining,result.Duration,4,6,"multi target pairs duration with longest remaining");C(Near(result.Fraction,4f/6)&&result.Blocked("缺能").Duration==6,"source fraction and blocked copy retain total");
  b.IsDead=true;result=owner.ElementalOpportunityWindow(1,CombatOpportunityKind.Shatter);Pair(result.Remaining,result.Duration,3,9,"dead longest target excluded with own denominator");b.IsDead=false;b.transform.position=new Vector3(0,0,50);result=owner.ElementalOpportunityWindow(1,CombatOpportunityKind.Shatter);Pair(result.Remaining,result.Duration,3,9,"out of area target not selected");
  b.transform.position=new Vector3(0,0,1);a.StatusEffects.Burn(owner,10,10);Tick(a.StatusEffects,7);b.StatusEffects.Burn(owner,6,6);Tick(b.StatusEffects,2);result=owner.ElementalOpportunityWindow(9,CombatOpportunityKind.BurnFinale);Pair(result.Remaining,result.Duration,4,6,"burn multi target pairs own status total");
  b.StatusEffects.Burn(new PlayerController(),12,12);result=owner.ElementalOpportunityWindow(9,CombatOpportunityKind.BurnFinale);Pair(result.Remaining,result.Duration,3,10,"foreign burn excluded from finale metadata");result=owner.ElementalOpportunityWindow(1,CombatOpportunityKind.Reignite);Pair(result.Remaining,result.Duration,12,12,"reignite reads actual foreign burn as allowed");
  owner=Setup();e=Enemy();owner.BounceTarget=Enemy(1);owner.Bounce(e);Pair(owner.CounterOpportunityRemaining,owner.CounterOpportunityDuration,1.8f,1.8f,"return blade grants actual 1.8 duration");owner.AdvanceCounter(.8f);Pair(owner.CounterOpportunityRemaining,owner.CounterOpportunityDuration,1,1.8f,"return blade decay not full");
  owner.GrantPerfect(false);Pair(owner.CounterOpportunityRemaining,owner.CounterOpportunityDuration,2,2,"normal perfect counter grant duration");owner.AdvanceCounter(.1f);owner.ResetBounceProc();owner.Bounce(e);Pair(owner.CounterOpportunityRemaining,owner.CounterOpportunityDuration,1.9f,2,"short return proc does not reset longer counter duration");
  owner.GrantPerfect(true);Pair(owner.CounterOpportunityRemaining,owner.CounterOpportunityDuration,3,3,"variant perfect counter true three seconds");owner.AdvanceCounter(1);Pair(owner.CounterOpportunityRemaining,owner.CounterOpportunityDuration,2,3,"variant decay actual source fraction");owner.AdvanceCounter(5);C(owner.CounterOpportunityRemaining==0,"counter expires");owner.GrantPerfect(false);Pair(owner.CounterOpportunityRemaining,owner.CounterOpportunityDuration,2,2,"counter regeneration replaces duration");
  return "PASS: "+checks+" actual opportunity duration assertions (state, refresh, consume, boss, multi-target, counter); managed engine boundaries";
 }
}
