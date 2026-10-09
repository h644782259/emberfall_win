"""Verify probabilistic fashion grants and quality persistence using production save code."""
from pathlib import Path
import importlib.util,tempfile,subprocess,os,sys
root=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
fixture=r'''
using System;using System.IO;using Emberfall;class Program {static void C(bool v,string why){if(!v)throw new Exception(why);} static void Main(string[] args){var p=new ProgressionService(args[0]);p.NewGame(HeroClass.Vanguard);var qualityCounts=new int[5];for(int roll=0;roll<100;roll++){var quality=ProgressionService.RollFashionRarity(roll);qualityCounts[quality.HasValue?(int)quality.Value:4]++;var previewProfile=new GameProfile{pendingAdventureChest=true,pendingChestMode=-1,pendingChestTier=2};C(ProgressionService.BuildSingleChestRoll(previewProfile,roll,0,0,false,"preview").Rarity==quality,"adventure chest uses the same probability table");}C(qualityCounts[0]==22&&qualityCounts[1]==12&&qualityCounts[2]==5&&qualityCounts[3]==1&&qualityCounts[4]==60,"four fashion qualities and no-drop chance");
foreach(int qualityRoll in new[]{0,1,6,18,40}){
 var q=new ProgressionService(Path.Combine(args[0],"quality-"+qualityRoll));q.NewGame(HeroClass.Vanguard);C(q.PrepareDungeonChest(),"prepare quality chest");var expected=ProgressionService.RollFashionRarity(qualityRoll);var frozen=ProgressionService.BuildSingleChestRoll(q.Profile,qualityRoll,0,0,false,"quality-"+qualityRoll);q.Profile.pendingChestDraw=frozen;q.Save();C(q.LoadSlot(q.CurrentSlotId),"load frozen quality");C(q.OpenDungeonChest()!=null,q.LastError);C(q.LastChestReward.Rarity==expected,"grant retains drawn quality");C(q.LoadSlot(q.CurrentSlotId),"reload quality receipt");C(q.LastChestReward.Rarity==expected,"receipt retains quality after reload");if(expected.HasValue)C(q.Profile.fashions.Exists(f=>f.rarity==expected.Value&&f.AppearanceRarity==expected.Value),"collection retains drawn quality");
}
p.Profile.fashions.Add(new FashionData{id="fashion-1-0",slot=FashionSlot.Weapon,rarity=Rarity.Common});p.Profile.fashions.Add(new FashionData{id="fashion-1-1",slot=FashionSlot.Weapon,rarity=Rarity.Rare,appearanceTier=0});p.Profile.fashionQualityRevision=0;p.Profile.weaponFashionId="fashion-1-0";p.Save();C(p.LoadSlot(p.CurrentSlotId),"migrate fashion");
C(p.EquippedFashion(FashionSlot.Weapon).rarity==Rarity.Common&&p.Profile.weaponFashionId=="fashion-1-0","loading retains quality and equipped identity");C(p.Profile.fashions.Exists(f=>f.id=="fashion-1-1"&&f.rarity==Rarity.Rare&&f.AppearanceRarity==Rarity.Rare),"migration preserves collected appearances");p.Save();C(p.LoadSlot(p.CurrentSlotId),"migration persists");C(p.EquippedFashion(FashionSlot.Weapon).AppearanceRarity==Rarity.Common,"appearance survives second load");
Console.WriteLine("PASS: 100 probability rolls, production grants for all four qualities/no drop, frozen reward reload and collected quality persistence");}}
'''

with tempfile.TemporaryDirectory(prefix='fashion-quality-') as d:
 p=Path(d);(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 names=['SkillRuntime','GameTypes','ProgressionService','ProgressionService.Attachments','ProgressionService.AutomaticGrowth','CombatBalance','SkillDamageBudgets','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState','ChapterProgression','ProgressionService.Chapter','ProgressionService.Reforge','ReforgeQuote','RoomTactics','CombatImpactBatch','SafeSaveFlow']
 sources=[root/('Assets/Scripts/Core/'+n+'.cs') for n in names]+[root/'Tests/ProgressionTests.cs',root/'Assets/Scripts/UI/EquipmentComparisonPresentation.cs']
 project=cv.write_project(p/'project',sources,fixture)
 subprocess.run([sys.argv[1] if len(sys.argv)>1 else 'dotnet','run','--project',str(project),'--',str(p/'saves')],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'))
