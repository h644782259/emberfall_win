using System;
using Emberfall;
public static class MobileSkillPolicyTests
{
 static int n;static void Check(bool b,string m){n++;if(!b)throw new Exception(m);}
 public static string Run()
 {
  n=0;int[][] pages={new[]{0,1,2,4,9},new[]{5,6,7,-1,9}};var seen=new System.Collections.Generic.HashSet<int>();
  for(int page=0;page<2;page++)for(int button=0;button<5;button++){int mapped=MobileSkillPolicy.SkillAtButton(button,page);Check(mapped==pages[page][button],"exact four-plus-three pages with fixed ultimate");if(mapped>=0)seen.Add(mapped);}
  Check(seen.Count==8&&MobileSkillPolicy.SkillAtButton(4,0)==9&&MobileSkillPolicy.SkillAtButton(4,1)==9,"every real active appears and ultimate never moves");
  Check(!MobileSkillPolicy.IsActiveSkill(3)&&!MobileSkillPolicy.IsActiveSkill(8)&&MobileSkillPolicy.IsActiveSkill(9),"passives noninteractive and ultimate reachable");
  Check(MobileSkillPolicy.SkillAtButton(-1)==-1&&MobileSkillPolicy.SkillAtButton(8)==-1,"bounds");
  var bindings=MobileSkillPolicy.DefaultBindings();
  Check(MobileSkillPolicy.ValidBindings(bindings),"default persistent mapping validates");
  Check(MobileSkillPolicy.SwapBinding(bindings,5,0)&&bindings[5]==0&&bindings[0]==6,"cross-page reorder swaps without losing skills");
  Check(MobileSkillPolicy.SwapBinding(bindings,7,1)&&bindings[7]==1&&bindings[1]==-1,"drop into empty position preserves one empty slot");
  Check(!MobileSkillPolicy.SwapBinding(bindings,8,2)&&!MobileSkillPolicy.SwapBinding(bindings,0,9)&&bindings[8]==9,"ultimate is immutable in both directions");
  Check(!MobileSkillPolicy.SwapBinding(bindings,0,3)&&!MobileSkillPolicy.SwapBinding(bindings,0,8),"passives cannot be assigned");
  Check(MobileSkillPolicy.ValidBindings(bindings),"reorder retains every active skill exactly once");
  var corrupt=(int[])bindings.Clone();corrupt[0]=corrupt[1];Check(!MobileSkillPolicy.ValidBindings(corrupt),"duplicate saved mapping rejected");
  corrupt=(int[])bindings.Clone();corrupt[8]=2;Check(!MobileSkillPolicy.ValidBindings(corrupt),"corrupt ultimate mapping rejected");
  Check(MobileSkillPolicy.BindingIndex(4,0)==8&&MobileSkillPolicy.BindingIndex(4,1)==8&&MobileSkillPolicy.BindingIndex(3,1)==7,"both pages share only fixed ultimate");
  var targets=new[]{new MobileSkillPolicy.Candidate(4,true,false),new MobileSkillPolicy.Candidate(25,true,true),new MobileSkillPolicy.Candidate(1,false,false)};
  Check(MobileSkillPolicy.SelectTarget(targets,8)==1,"current valid focus takes priority");
  Check(MobileSkillPolicy.SelectTarget(targets,4)==0,"out-of-range focus falls back to nearest");
  targets[1]=new MobileSkillPolicy.Candidate(1,false,true);Check(MobileSkillPolicy.SelectTarget(targets,8)==0,"dead or occluded focus ignored");
  Check(MobileSkillPolicy.SelectTarget(targets,1)==-1,"no valid targets requests facing fallback");
  Check(MobileSkillPolicy.SelectTarget(null,8)==-1&&MobileSkillPolicy.SelectTarget(targets,float.NaN)==-1,"invalid input safe");
  targets[0]=new MobileSkillPolicy.Candidate(float.NaN,true,false);Check(MobileSkillPolicy.SelectTarget(targets,8)==-1,"non-finite enemy excluded");
  var tap=new MobileSkillTap();int skill;Check(!tap.Begin(1,3)&&!tap.Begin(1,8),"passive taps cannot cast");Check(tap.Begin(1,9),"capture learned skill");Check(!tap.Begin(2,1),"second finger cannot steal cast");
  Check(!tap.Release(2,true,false,out skill)&&tap.Active,"unowned release leaves original touch");
  Check(tap.Release(1,true,false,out skill)&&skill==9&&!tap.Active,"one release yields one cast");
  Check(!tap.Release(1,true,false,out skill),"repeated release no duplicate resource spend");
  tap.Begin(3,1);Check(!tap.Release(3,false,false,out skill)&&!tap.Active,"dragging off cancels");
  tap.Begin(4,1);Check(!tap.Release(4,true,true,out skill),"system cancel never casts");
  tap.Begin(5,1);tap.Cancel();Check(!tap.Release(5,true,false,out skill),"background cancel never casts on stale release");
  return n+" mobile direct-skill/auto-target/touch ownership assertions passed";
 }
}
