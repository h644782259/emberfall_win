using System;using System.IO;using System.Linq;using Emberfall;using UnityEngine;
public static class CampBuildDraftTests
{
 static int n;static void Check(bool ok,string why){n++;if(!ok)throw new Exception(why);}static string State(ProgressionService p)=>JsonUtility.ToJson(p.Profile,true);
 public static string Run(string root)
 {
  Check(GameBalance.SkillPointBudget(1)==1&&GameBalance.SkillPointBudget(2)==1,"level-one minimum budget");
  foreach(int level in new[]{35,50,100})foreach(int save in new[]{-1,0,1})
  {
   var p=Fresh(root,level);int events=0;p.Changed+=()=>events++;var live=p.Profile;string state=State(p),disk=File.ReadAllText(p.SaveFilePath);var d=p.BeginBuildDraft(true);
   Check(p.BeginBuildDraft(false)==null,"draft camp gate");Check(d.Points==GameBalance.SkillPointBudget(level)-13,"shared max(1,L-1) budget");
   Check(d.ChangeSkill(0,-1)&&d.SkillRank(0)==2,"localized 3 to 2");Check(d.ChangeSkill(0,-1)&&!d.ChangeSkill(0,-1),"rank one immutable");Check(d.Undo()&&d.SkillRank(0)==2,"undo one localized change");Check(d.Undo()&&d.SkillRank(0)==3,"undo restores original");
   Check(d.ChangeMastery(0,-1)&&d.Core==-1,"core below threshold closes in draft");Check(d.Undo()&&d.Core==0,"undo restores core");
   for(int i=0;i<3;i++)Check(d.ChangeMastery(0,-1)&&d.ChangeMastery(1,1),"move three points without full reset");
   Check(d.MasteryRank(0)==7&&d.MasteryRank(1)==3&&d.Points==GameBalance.SkillPointBudget(level)-13,"three points conserved");
   Check(d.ChangeSkill(0,-1)&&d.SkillRank(0)==2,"single-skill refund after transfer");
   Check(d.Stats.MaxHealth>p.GetStats().MaxHealth&&d.Stats.Damage<p.GetStats().Damage,"preview uses actual shared stats");
   Check(State(p)==state&&ReferenceEquals(p.Profile,live)&&File.ReadAllText(p.SaveFilePath)==disk&&events==0,"draft editing/preview is fully isolated");
   Directory.CreateDirectory(p.SaveFilePath+".tmp");Check(!d.Apply(true,save)&&d.IsCurrent&&State(p)==state&&ReferenceEquals(p.Profile,live)&&File.ReadAllText(p.SaveFilePath)==disk&&events==0,"failed apply/save publishes neither profile nor A/B");Directory.Delete(p.SaveFilePath+".tmp");
   Check(!d.Apply(false,save)&&!d.Apply(true,2)&&State(p)==state,"camp and slot application gates");
   Check(d.Apply(true,save)&&events==1&&!d.IsCurrent&&!d.Apply(true,save),"successful single atomic commit cannot replay");
   Check(p.Profile.skillRanks[0]==2&&p.Profile.masteryRanks[0]==7&&p.Profile.masteryRanks[1]==3&&p.Profile.skillPoints==GameBalance.SkillPointBudget(level)-12,"exact postcommit point accounting");
   Check(save<0?!p.HasBuildPreset(0)&&!p.HasBuildPreset(1):p.HasBuildPreset(save)&&p.Profile.buildPresets[save].skillRanks[0]==2&&p.Profile.buildPresets[save].masteryRanks[1]==3,"optional preset matches applied draft");
   var reloaded=new ProgressionService(Path.GetDirectoryName(p.SaveFilePath));Check(reloaded.LoadSlot(p.CurrentSlotId)&&State(reloaded)==State(p),"persisted profile/preset reload exactly");
  }
  {var p=Fresh(root,35);string state=State(p);var d=p.BeginBuildDraft(true);d.ChangeSkill(0,-1);d.Cancel();Check(!d.Apply(true)&&State(p)==state,"cancel invalidates handle and leaves profile intact");d=p.BeginBuildDraft(true);p.Profile.gold++;Check(!d.IsCurrent&&!d.ChangeMastery(0,-1)&&!d.Apply(true),"in-place source mutation invalidates draft");d=p.BeginBuildDraft(true);p.SaveBuildPreset(0,true);Check(!d.IsCurrent&&!d.Apply(true),"external commit invalidates draft");}
  {var p=Fresh(root,35);var d=p.BeginBuildDraft(true);Check(!d.ChangeSkill(1,1),"new unlock excluded from respec");Check(!d.SelectCore(1)&&d.SelectCore(-1),"core gate and explicit off");int cap=ProgressionService.MasteryCap(35);while(d.MasteryRank(0)<cap)Check(d.ChangeMastery(0,1),"up to mastery cap");Check(!d.ChangeMastery(0,1),"per-level cap enforced");while(d.Points>0){bool ok=false;for(int i=1;i<4&&!ok;i++)ok=d.ChangeMastery(i,1);Check(ok,"spend remaining shared budget");}Check(!d.ChangeSkill(0,1)&&!d.ChangeMastery(3,1)&&d.Points==0,"no overspend");}
  foreach(int level in new[]{1,2,3,10,20,35,100})
  {
   var p=new ProgressionService(Path.Combine(root,Guid.NewGuid().ToString("N")));Check(p.CreateNewSlot(HeroClass.Vanguard),"create draft budget boundary fixture");p.Profile.level=level;p.Save();
   var d=p.BeginBuildDraft(true);int budget=GameBalance.SkillPointBudget(level);Check(d.SkillRank(0)==1&&d.Points==budget-1,"starter consumes one shared point at every boundary level");
   int allocated=Math.Min(budget-1,4*ProgressionService.MasteryCap(level));
   for(int point=0;point<allocated;point++){bool moved=false;for(int i=0;i<4&&!moved;i++)moved=d.ChangeMastery(i,1);Check(moved,"legal boundary mastery allocation respects both caps and shared budget");}
   int remaining=budget-1-allocated;string before=State(p);Check(!d.ChangeMastery(0,1)&&(remaining>0||!d.ChangeSkill(0,1))&&State(p)==before,"exhausted budget or mastery cap rejects extra investment without live mutation");
   Check(d.Apply(true)&&p.Profile.skillPoints==remaining&&p.Profile.skillRanks[0]==1,"boundary draft preserves unspent points above mastery caps and learned first rank");
   int spent=0;foreach(int rank in p.Profile.skillRanks)spent+=rank;foreach(int rank in p.Profile.masteryRanks)spent+=rank;Check(spent+remaining==budget&&p.Load()&&p.Profile.skillPoints==remaining,"boundary budget survives durable normalization without minting or losing points");
  }
  foreach(bool mobile in new[]{false,true}){GameUI.TestDraftFlow(Fresh(root,50),mobile,Check);GameUI.TestDraftVitals(Fresh(root,50),mobile,Check);}
  return "PASS "+n+" actual service + desktop/mobile UI draft assertions";
 }
 static ProgressionService Fresh(string root,int level){var p=new ProgressionService(Path.Combine(root,Guid.NewGuid().ToString("N")));Check(p.CreateNewSlot(HeroClass.Vanguard),"isolated save");p.Profile.level=level;p.Profile.skillRanks=new int[GameBalance.SkillCount];p.Profile.skillRanks[0]=3;p.Profile.masteryRanks=new[]{10,0,0,0};p.Profile.masteryCore=0;p.Save();return p;}
}
