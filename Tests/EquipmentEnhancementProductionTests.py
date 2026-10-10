"""Twenty-rank enhancement, saved failures/migration, and zero reward filtering."""
from pathlib import Path
import importlib.util,tempfile,subprocess,os,sys
root=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
program=r'''using System;using System.IO;using System.Linq;using System.Reflection;using System.Text.Json.Nodes;using Emberfall;
class Program{
static int n;static void C(bool v,string m){n++;if(!v)throw new Exception(m);}
static bool Attempt(ProgressionService p,string id,int roll)=>(bool)typeof(ProgressionService).GetMethod("ApplyUpgradeAttempt",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(p,new object[]{id,roll});
static ProgressionService New(string root){var p=new ProgressionService(Path.Combine(root,Guid.NewGuid().ToString("N")));C(p.CreateNewSlot(HeroClass.Arcanist),"new");p.Profile.level=100;p.Profile.gold=1000000;p.Save();return p;}
static void Main(string[] args){
C(ProgressionService.MaximumUpgrade==20&&CombatBalance.MaximumUpgradeRank==20,"twenty rank cap");
for(int target=1;target<=20;target++){int chance=CombatBalance.UpgradeSuccessPercent(target);C(chance>0&&chance<100,"failure at every rank");if(target>1)C(chance<CombatBalance.UpgradeSuccessPercent(target-1),"strictly decreasing chance");}
C(CombatBalance.UpgradeSuccessPercent(0)==0&&CombatBalance.UpgradeSuccessPercent(21)==0,"out of range chance");
for(int basis=1;basis<=500;basis+=7)for(int rank=0;rank<=20;rank++)foreach(int minimum in new[]{1,2})C(CombatBalance.UpgradeValue(basis,rank,minimum)==CombatBalance.LegacyUpgradeValue(basis,rank*5,minimum),"twenty evenly distributed old ranks");
C(CombatBalance.UpgradeValue(100,20)==600&&CombatBalance.UpgradeValue(0,20)==0,"full old budget and absent stats");
foreach(ItemSlot slot in Enum.GetValues(typeof(ItemSlot))){
var p=New(args[0]);string id=p.Equipped(slot).id;int basis=p.Equipped(slot).baseAttack;
for(int rank=0;rank<20;rank++){
int chance=CombatBalance.UpgradeSuccessPercent(rank+1),money=p.Profile.gold,price=p.UpgradeCost(p.Equipped(slot)),attempts=p.Profile.equipmentUpgradeAttempts;var q=p.PrepareSmithUpgrade(slot,true);C(q!=null,"smith quote");
C(!Attempt(p,id,chance)&&!p.LastUpgradeSucceeded&&p.SlotUpgradeRank(slot)==rank&&p.Profile.gold==money-price&&p.Profile.equipmentUpgradeAttempts==attempts+1,"failure threshold spends cost and keeps rank");
C(!p.UpgradeAtSmith(q,true)&&p.Profile.gold==money-price,"old quote cannot repeat a paid failure");
C(p.LoadSlot(p.CurrentSlotId)&&p.Profile.equipmentUpgradeAttempts==attempts+1&&p.SlotUpgradeRank(slot)==rank,"failure survives reload");
money=p.Profile.gold;C(Attempt(p,id,chance-1)&&p.LastUpgradeSucceeded&&p.SlotUpgradeRank(slot)==rank+1&&p.Profile.gold==money-price,"success threshold advances exactly once");
C(p.Equipped(slot).baseAttack==basis&&p.PreviewUpgrade(p.Equipped(slot),rank+1).attack==p.Equipped(slot).attack,"stable bases and preview");
}
C(p.CurrentUpgradeLimit==20&&!p.Upgrade(id)&&p.UpgradeCost(p.Equipped(slot))==0,"max rejects without spending");
}
var atomic=New(args[0]);string weapon=atomic.Equipped(ItemSlot.Weapon).id;int gold=atomic.Profile.gold,count=atomic.Profile.equipmentUpgradeAttempts,attack=atomic.Equipped(ItemSlot.Weapon).attack;int events=0;atomic.Changed+=()=>events++;
Directory.CreateDirectory(atomic.SaveFilePath+".tmp");foreach(int roll in new[]{0,99})C(!Attempt(atomic,weapon,roll)&&atomic.Profile.gold==gold&&atomic.Profile.equipmentUpgradeAttempts==count&&atomic.SlotUpgradeRank(ItemSlot.Weapon)==0&&atomic.Equipped(ItemSlot.Weapon).attack==attack&&events==0,"both outcomes roll back failed writes");Directory.Delete(atomic.SaveFilePath+".tmp");
C(Attempt(atomic,weapon,0)&&events==1,"retry commits once");
// Patch actual old-format disk fixtures; load migrates and persists before publishing.
foreach(bool initialized in new[]{false,true})foreach(int oldRank in new[]{0,1,20,21,24,25,50,99,100}){
var p=New(args[0]);var disk=JsonNode.Parse(File.ReadAllText(p.SaveFilePath));var profile=disk["profile"].AsObject();profile["equipmentEnhancementRevision"]=0;profile["slotUpgradesInitialized"]=initialized;profile["slotUpgradeRanks"]=new JsonArray(oldRank,oldRank,oldRank);
foreach(var node in profile["inventory"].AsArray()){
node["enhancementRevision"]=0;node["upgradeLevel"]=oldRank;
node["attack"]=CombatBalance.LegacyUpgradeValue((int)node["baseAttack"],oldRank);node["defense"]=CombatBalance.LegacyUpgradeValue((int)node["baseDefense"],oldRank);node["health"]=CombatBalance.LegacyUpgradeValue((int)node["baseHealth"],oldRank,2);
}
File.WriteAllText(p.SaveFilePath,disk.ToJsonString());
int expected=oldRank>20?oldRank/5:oldRank;C(p.LoadSlot(p.CurrentSlotId),"legacy loads");foreach(ItemSlot slot in Enum.GetValues(typeof(ItemSlot))){var item=p.Equipped(slot);C(p.SlotUpgradeRank(slot)==expected&&item.upgradeLevel==expected&&item.enhancementRevision==1,"slot and equipped cache converted once");C(item.attack==CombatBalance.UpgradeValue(item.baseAttack,expected)&&item.health==CombatBalance.UpgradeValue(item.baseHealth,expected,2),"migration keeps original bases");}
var saved=JsonNode.Parse(File.ReadAllText(p.SaveFilePath));C((int)saved["profile"]["equipmentEnhancementRevision"]==1,"load durably records revision");C(p.LoadSlot(p.CurrentSlotId)&&p.SlotUpgradeRank(ItemSlot.Weapon)==expected,"repeated load never divides again");
}
// Migration failure preserves active role and original bytes.
var blocked=New(args[0]);var old=JsonNode.Parse(File.ReadAllText(blocked.SaveFilePath));old["profile"]["equipmentEnhancementRevision"]=0;old["profile"]["slotUpgradeRanks"]=new JsonArray(100,50,21);File.WriteAllText(blocked.SaveFilePath,old.ToJsonString());string bytes=File.ReadAllText(blocked.SaveFilePath);Directory.CreateDirectory(blocked.SaveFilePath+".tmp");C(!blocked.LoadSlot(blocked.CurrentSlotId)&&blocked.SlotUpgradeRank(ItemSlot.Weapon)==0&&File.ReadAllText(blocked.SaveFilePath)==bytes,"failed migration does not publish or replace");Directory.Delete(blocked.SaveFilePath+".tmp");C(blocked.LoadSlot(blocked.CurrentSlotId)&&blocked.SlotUpgradeRank(ItemSlot.Weapon)==20,"migration retry");
C(!EquipmentComparisonPresentation.HasNonzeroStat("0%")&&!EquipmentComparisonPresentation.HasNonzeroStat("+0")&&!EquipmentComparisonPresentation.HasNonzeroStat("0～0%"),"zero affixes hidden");C(EquipmentComparisonPresentation.HasNonzeroStat("0～5%")&&EquipmentComparisonPresentation.HasNonzeroStat("2.5%"),"ranges and positive affixes retained");
C(RunRecapPresentation.PositiveRewardIndices(new long[]{0,2,0,4,0,0}).SequenceEqual(new[]{1,3}),"only positive rewards keep ordered icons");C(RunRecapPresentation.PositiveRewardIndices(new long[]{0,0,0}).Length==0,"no icons for empty settlement");C(RunRecapPresentation.PositiveRewardIndices(new long[]{0,2,3},2).SequenceEqual(new[]{1}),"legacy settlement duplicate exclusion");
Console.WriteLine("PASS "+n+" enhancement and zero-value presentation assertions");}}
'''
with tempfile.TemporaryDirectory(prefix='equipment-enhancement-') as tmp:
 p=Path(tmp);(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 names=['SkillRuntime','GameTypes','ProgressionService','ProgressionService.Attachments','ProgressionService.AutomaticGrowth','ProgressionService.Trading','ProgressionService.Smith','CombatBalance','SkillDamageBudgets','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState','ChapterProgression','ProgressionService.Chapter','ProgressionService.Reforge','ReforgeQuote','RoomTactics','CombatImpactBatch','SafeSaveFlow']
 sources=[root/('Assets/Scripts/Core/'+name+'.cs') for name in names]+[root/'Tests/ProgressionTests.cs',root/'Assets/Scripts/UI/EquipmentComparisonPresentation.cs',root/'Assets/Scripts/UI/RunRecapPresentation.cs']
 project=cv.write_project(p/'project',sources,program)
 subprocess.run([sys.argv[1] if len(sys.argv)>1 else '/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet','run','--project',str(project),'--',str(p/'saves')],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'))
