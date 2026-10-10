"""Actual random rolls, previews, stats and persisted affixes at managed filesystem boundary."""
from pathlib import Path
import importlib.util,tempfile,subprocess,os,sys
root=Path(__file__).resolve().parents[1];spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
fixture=r'''
using System;using System.IO;using System.Reflection;using System.Collections.Generic;using Emberfall;
class Program{
static void C(bool v,string why){if(!v)throw new Exception(why);}static void Roll(ItemData i)=>typeof(ProgressionService).GetMethod("SetRolledStats",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{i});
static void Main(string[] args){
var p=new ProgressionService(args[0]);C(p.CreateNewSlot(HeroClass.Arcanist),"create");p.Profile.level=30;float[] low={.85f,1.10f,1.45f,1.90f},high={1.05f,1.40f,1.85f,2.50f};int rate=0,damage=0,both=0;int[] maxSeen=new int[4];
for(int quality=0;quality<4;quality++)for(int slot=0;slot<3;slot++){
var seen=new HashSet<string>();for(int seed=0;seed<100;seed++){
var item=new ItemData{id="roll-"+seed,slot=(ItemSlot)slot,rarity=(Rarity)quality,level=20};Roll(item);C(item.level==20,"generated item level preserved");
float[] bases=slot==0?new[]{55f,0f,0f}:slot==1?new[]{0f,27f,90f}:new[]{22f,0f,66f};int[] values={item.attack,item.defense,item.health};for(int n=0;n<3;n++)C(values[n]>=Math.Floor(bases[n]*low[quality])&&values[n]<=Math.Ceiling(bases[n]*high[quality]),"rolled stat stays within quality band");
int affixes=(item.criticalChance>0?1:0)+(item.criticalDamageBonus>0?1:0)+(item.attackPercent>0?1:0);C(affixes<=ProgressionService.EquipmentAffixLimit(item.rarity),"rarity affix cap");maxSeen[quality]=Math.Max(maxSeen[quality],affixes);seen.Add(item.attack+"/"+item.defense+"/"+item.health);C(item.criticalChance>=0&&item.criticalChance<=.08f&&item.criticalDamageBonus>=0&&item.criticalDamageBonus<=.20f,"affix bounded");if(quality==0)C(item.criticalChance==0&&item.criticalDamageBonus==0,"common has no critical affix");
if(item.criticalChance>0)rate++;if(item.criticalDamageBonus>0)damage++;if(item.criticalChance>0&&item.criticalDamageBonus>0)both++;
float c=item.criticalChance,d=item.criticalDamageBonus;var preview=p.PreviewUpgrade(item,12);C(preview.criticalChance==c&&preview.criticalDamageBonus==d,"upgrade preview preserves affixes");int a=item.attack,h=item.health;Roll(item);C(item.attack==a&&item.health==h&&item.criticalChance==c&&item.criticalDamageBonus==d,"same identity keeps exact roll");}
C(seen.Count>1,"same slot level rarity has real stat variation");}
C(maxSeen[1]==2&&maxSeen[2]==2&&maxSeen[3]==3,"rare and epic roll two affixes; legendary rolls three");C(ProgressionService.EquipmentDropPreview(ItemSlot.Weapon,Rarity.Rare,20).Contains("最多2项")&&ProgressionService.EquipmentDropPreview(ItemSlot.Weapon,Rarity.Legendary,20).Contains("最多3项"),"preview matches actual affix limits");C(rate>0&&damage>0&&both>0,"single and double critical affixes generated");
var gear=new ItemData{id="crit-test",name="test",slot=ItemSlot.Weapon,rarity=Rarity.Legendary,level=20,attack=100,criticalChance=.05f,criticalDamageBonus=.20f};p.Profile.inventory.Add(gear);float before=p.GetStats().CritChance;C(p.Equip(gear.id),p.LastError);var stats=p.GetStats();C(Math.Abs(stats.CritChance-before-.05f)<.0001&&Math.Abs(stats.CritDamageBonus-.20f)<.0001,"equipped affixes reach live stats");
var hit=CombatDamage.Roll(100,stats.CritChance,0,1.65f+stats.CritDamageBonus);C(hit.IsCritical&&Math.Abs(hit.Amount-185)<.001,"critical damage bonus affects actual damage payload");var plain=new ItemData{attack=100,level=20,slot=ItemSlot.Weapon,rarity=Rarity.Legendary};C(ProgressionService.EquipmentScore(gear)>ProgressionService.EquipmentScore(plain),"critical affixes contribute to score");p.Save();C(p.LoadSlot(p.CurrentSlotId),"reload");C(p.Equipped(ItemSlot.Weapon).criticalChance==.05f&&p.Equipped(ItemSlot.Weapon).criticalDamageBonus==.20f,"affixes persist without reroll");
Console.WriteLine("PASS 1200 rolled items: quality bounds, variation, affixes, deterministic preview, live stats, critical payload, scoring and reload");}}
'''
with tempfile.TemporaryDirectory(prefix='random-gear-') as d:
 p=Path(d);(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 names=['SkillRuntime','GameTypes','ProgressionService','ProgressionService.Attachments','ProgressionService.AutomaticGrowth','CombatBalance','SkillDamageBudgets','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState','ChapterProgression','ProgressionService.Chapter','ProgressionService.Reforge','ReforgeQuote','RoomTactics','CombatImpactBatch','SafeSaveFlow']
 sources=[root/('Assets/Scripts/Core/'+n+'.cs') for n in names]+[root/'Tests/ProgressionTests.cs',root/'Assets/Scripts/Combat/CombatDamage.cs']
 project=cv.write_project(p/'project',sources,fixture)
 subprocess.run([sys.argv[1],'run','--project',str(project),'--',str(p/'saves')],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'))
