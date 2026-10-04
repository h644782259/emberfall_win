using System;
using System.IO;
using Emberfall;
using UnityEngine;
public static class CampPracticeTests
{
 static int assertions;
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static string State(ProgressionService p){return JsonUtility.ToJson(p.Profile,true);}
 public static string Run(string root)
 {
  var p=new ProgressionService(root);Check(p.CreateNewSlot(HeroClass.Vanguard),"create original");p.Profile.level=50;p.Profile.masteryRanks=new[]{10,0,0,0};p.Profile.masteryCore=0;p.Profile.skillRanks[0]=3;p.Save();
  var profile=p.Profile;var disk=File.ReadAllText(p.SaveFilePath);string original=State(p);var draft=p.BeginBuildDraft(true);
  Check(p.PracticeConfigurationSummary().Contains("等级 50")&&p.PracticeConfigurationSummary().Contains(p.CurrentBuildSummary()),"human practice configuration uses frozen authoritative build summary");
  var a=draft.CreatePracticeCopy();Check(a.IsPracticeOnly&&!ReferenceEquals(a.Profile,profile),"deep isolated practice profile");
  Check(a.SaveDirectory==null&&a.SaveFilePath==null&&a.CurrentSlotId==null&&!a.HasActiveSave&&!a.HasSave,"practice has no storage destination or real slot");
  var practiceProfile=a.Profile;string practiceState=State(a);bool newGameThrew=false;try{a.NewGame(HeroClass.Ranger);}catch(Exception){newGameThrew=true;}
  Check(!newGameThrew&&ReferenceEquals(a.Profile,practiceProfile)&&State(a)==practiceState&&a.LastError.Contains("试招")&&File.ReadAllText(p.SaveFilePath)==disk,"practice NewGame refuses without exception or profile and disk mutation");
  Check(a.BeginBuildDraft(true)==null,"memory practice cannot create a real-path draft preview");
  ProgressionService candidate;string transitionError;bool rejected=false;try{rejected=!SaveSlotTransition.TryStage(a,p.CurrentSlotId,out candidate,out transitionError)&&candidate==null&&transitionError.Contains("试招");}catch(Exception){}Check(rejected,"practice transition rejects before default storage constructor");
  a.Profile.pendingFashionChest=true;bool chestRefused=false;try{chestRefused=a.OpenDungeonChest(0)==null&&a.LastError.Contains("试招")&&a.Profile.pendingFashionChest;}catch(Exception){}Check(chestRefused,"practice cannot open inherited chest receipt");
  var write=typeof(ProgressionService).GetMethod("TryWriteProfile",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);object[] args={a.Profile,a.SaveFilePath,false,null};
  Check(!(bool)write.Invoke(null,args)&&Directory.GetFiles(root).Length==2&&File.ReadAllText(p.SaveFilePath)==disk,"low-level missing guard cannot target real saves");
  int files=Directory.GetFiles(root).Length;a.Profile.gold+=100;a.Profile.skillRanks[0]=1;a.Save();Check(string.IsNullOrEmpty(a.LastError),"practice memory save succeeds without persistence");Check(Directory.GetFiles(root).Length==files&&File.ReadAllText(p.SaveFilePath)==disk&&State(p)==original&&draft.IsCurrent,"practice save never persists or invalidates draft");
  a.Profile.potions=2;Check(a.UsePotion()&&a.Profile.potions==1&&File.ReadAllText(p.SaveFilePath)==disk&&State(p)==original&&draft.IsCurrent,"actual practice potion transaction changes only temporary inventory");
  Check(!a.SaveAsNewSlot()&&!a.CreateNewSlot(HeroClass.Ranger)&&!a.LoadSlot(p.CurrentSlotId),"practice cannot create or load saves");
  SaveDeletionRequest request;p.PrepareSaveDeletion(p.CurrentSlotId,out request);Check(!a.DeleteSaveSlot(request),"practice cannot delete real saves");
  for(int i=0;i<3;i++)Check(draft.ChangeMastery(0,-1)&&draft.ChangeMastery(1,1),"move three legal points");
  Check(draft.ChangeSummary.Contains("退回 3 / 投入 3点")&&draft.CoreChangeSummary.Contains("失效"),"fixed draft summary exposes transferred points and lost core capability");
  var b=draft.CreatePracticeCopy();Check(b.Profile.masteryRanks[0]==7&&b.Profile.masteryRanks[1]==3&&a.Profile.masteryRanks[0]==10,"second practice uses new draft snapshot and A remains frozen");
  Directory.CreateDirectory(p.SaveFilePath+".tmp");Check(!draft.Apply(true)&&draft.IsCurrent&&ReferenceEquals(profile,p.Profile)&&File.ReadAllText(p.SaveFilePath)==disk,"failed apply preserves real profile draft and disk");Directory.Delete(p.SaveFilePath+".tmp");
  Check(draft.Apply(true)&&p.Profile.masteryRanks[1]==3&&!draft.Apply(true),"apply once after two practices");
  string appliedDisk=File.ReadAllText(p.SaveFilePath);string applied=State(p);draft=p.BeginBuildDraft(true);draft.ChangeMastery(1,-1);var cancelled=draft.CreatePracticeCopy();draft.Cancel();cancelled.Save();Check(!draft.Apply(true)&&State(p)==applied,"cancel after practice cannot mutate owner");
  var r=new CampPracticeRecord(CampPracticeScenario.Stationary,10,"A");var energy=new SkillRuntime(HeroClass.Vanguard);energy.EnergyChanged=r.Energy;
  Check(energy.TryConsume(0,1),"real skill consumes");float spent=100-energy.Energy;energy.RestoreEnergy(1000);energy.RestoreEnergy(5);Check(r.EnergySpent==spent&&r.EnergyRestored==spent,"energy observer uses actual clipped restoration");
  r.Cast(10,0);r.Cast(10,0);r.Cast(11,0);r.ConfirmedHealthLoss(0,10);Check(r.EffectiveSkillCasts.Count==0,"immune and zero loss do not count skill hits");r.ConfirmedHealthLoss(30,10);r.ConfirmedHealthLoss(5,10);r.ConfirmedHealthLoss(2,0);r.ConfirmedHealthLoss(0,999);r.Damage(float.NaN);r.Mechanism("破敌兑现");
  Check(r.SkillCasts[0]==2&&r.EffectiveSkillCasts[0]==1&&r.ActualDamage==37&&r.Mechanisms["破敌兑现"]==1,"actual hits deduplicate multihit casts; misses excluded");
  r.Advance(9);r.Advance(2);r.Damage(20);r.Energy(-10);Check(r.Finished&&r.Elapsed==10&&r.ActualDamage==37&&r.EnergySpent==spent,"finished records immutable to late events");
  var kill=new CampPracticeRecord(CampPracticeScenario.Stationary,10,"kill");kill.Cast(1,0);kill.ConfirmedHealthLoss(3,1);kill.ConfirmedHealthLoss(0,1);kill.ConfirmedHealthLoss(2);kill.ConfirmedHealthLoss(4);Check(kill.ActualDamage==9&&kill.EffectiveSkillCasts[0]==1,"clipped killing blow plus DOT and companion damage counted once without extra cast credits");
  var other=new CampPracticeRecord(CampPracticeScenario.Stationary,10,"B");other.Advance(10);Check(r.ComparableConditions(other)&&r.Comparison(other).Contains("配置不同"),"A B preserves exact config distinction");
  foreach(var scenario in new[]{CampPracticeScenario.Moving,CampPracticeScenario.FrontAndSupplier}){var x=new CampPracticeRecord(scenario,10,"A");x.Advance(10);Check(!r.ComparableConditions(x),"different scene not comparable");}
  var dead=new CampPracticeRecord(CampPracticeScenario.Stationary,10,"A");dead.Advance(2);dead.Finish("死亡");dead.Advance(30);Check(!r.ComparableConditions(dead)&&dead.Elapsed==2,"death and exit preserve partial duration");
  var longRun=new CampPracticeRecord(CampPracticeScenario.Stationary,60,"A");longRun.Advance(60);Check(longRun.Finished&&!r.ComparableConditions(longRun),"sixty seconds separate condition");
  Check(ReferenceEquals(p.Profile,profile)==false&&File.ReadAllText(p.SaveFilePath)==appliedDisk,"only explicit real apply persisted");
  return "PASS "+assertions+" practice isolation, draft transaction and measured event assertions";
 }
}
