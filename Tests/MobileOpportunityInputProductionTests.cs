using System;using Emberfall;using UnityEngine;
public static class MobileOpportunityInputProductionTests
{
 static int count;static void C(bool value,string text){count++;if(!value)throw new Exception(text);}
 static void Tap(MobileControls c,int id,Vector2 point){c.ProcessPointer(id,TouchPhase.Began,point);c.ProcessPointer(id,TouchPhase.Ended,point);}
 static CombatOpportunityState Window()=>new CombatOpportunityState(CombatOpportunityKind.Shatter,1.5f,blockReason:"缺能",duration:4);
 public static string Run()
 {
  foreach(int preset in new[]{-1,0,1})for(int slot=0;slot<12;slot++)
  {
   MobileControls.ResetInput();MobileControls.Layout=new MobileControlLayout(568,320,163,preset);
   var session=new GameSession();var hero=new PlayerController(session);var ui=new GameUI{mobileSkillPage=slot>=5&&slot<9?1:0};var controls=new MobileControls(session,ui);var enemy=new EnemyController(8);session.Enemies.Add(enemy);hero.PinMobileTarget(enemy);
   var area=slot<10?ui.MobileOpportunityArea(slot):slot==10?MobileControls.Layout.CounterOpportunity:MobileControls.Layout.ComboOpportunity;
   if(area.Width==0||area.Height==0){C(slot==3||slot==8,"only hidden passive identities have no opportunity hit area");continue;}
   foreach(var skillBox in MobileControls.Layout.Skills)C(!area.Overlaps(skillBox),"hint interception never enlarges or replaces a skill button");
   var point=controls.Control(area);C(!MobileControls.IsScreenPointOverControls(point),"invisible hint preserves world hit area");
   if(slot<10)hero.OpportunityWindows[slot]=Window();else if(slot==10)hero.CounterWindow=Window();else hero.ComboWindow=Window();
   C(MobileControls.IsScreenPointOverControls(point),"visible hint is reported as UI control");
   float energy=hero.Energy,pitch=AdventureCamera.Pitch;int casts=hero.Casts;
   C(controls.ProcessPointer(81,TouchPhase.Began,point),"visible hint owns pointer");
   controls.ProcessPointer(81,TouchPhase.Moved,new Vector2(point.x+20,point.y));controls.ProcessPointer(81,TouchPhase.Ended,new Vector2(220,180));
   C(hero.MobilePinnedTarget==enemy&&hero.Casts==casts&&hero.Energy==energy&&!MobileControls.AttackHeld&&AdventureCamera.Pitch==pitch,"visible hint never changes pin or combat state");
   hero.targeting.BeginAimForTest();var aim=hero.targeting.TargetPoint;
   Tap(controls,82,point);C(hero.targeting.AimWrites==0&&hero.targeting.Confirms==0&&hero.targeting.TargetPoint.sqrMagnitude==aim.sqrMagnitude,"hint never updates or confirms live ground targeting");hero.targeting.Cancel();
   controls.ProcessPointer(83,TouchPhase.Began,point);if(slot<10)hero.OpportunityWindows[slot]=default;else if(slot==10)hero.CounterWindow=default;else hero.ComboWindow=default;
   C(!MobileControls.IsScreenPointOverControls(point),"expired hint restores world area immediately");controls.ProcessPointer(83,TouchPhase.Ended,point);C(hero.MobilePinnedTarget==enemy,"expiry cannot turn an owned hint pointer into world tap");
   Tap(controls,84,point);C(hero.MobilePinnedTarget==null,"new tap at expired hint follows original world path");
   if(slot<10)hero.OpportunityWindows[slot]=Window();else if(slot==10)hero.CounterWindow=Window();else hero.ComboWindow=Window();
   session.InputBlocked=true;C(!MobileControls.IsScreenPointOverControls(point)&&!controls.ProcessPointer(85,TouchPhase.Began,point),"pause suppresses hint input admission");session.InputBlocked=false;
   hero.PinMobileTarget(enemy);controls.ProcessPointer(86,TouchPhase.Began,point);hero.CombatEpoch++;controls.ProcessPointer(86,TouchPhase.Canceled,point);C(hero.Casts==casts&&!MobileControls.AttackHeld,"epoch and cancel cannot release a hint as action");
   Tap(controls,87,new Vector2(220,180));C(hero.MobilePinnedTarget==null,"nonhint world tap remains functional");
   var attack=controls.Control(MobileControls.Layout.Attack);controls.ProcessPointer(88,TouchPhase.Began,attack);C(MobileControls.AttackHeld,"original attack button still owns attack");controls.ProcessPointer(88,TouchPhase.Ended,attack);C(!MobileControls.AttackHeld,"original attack release unchanged");
  }
  return "PASS: "+count+" actual ProcessPointer/IsScreenPointOverControls assertions, visible areas x 3 presets, hidden passive areas explicitly excluded; opportunity observations and Unity are explicit boundaries";
 }
}
