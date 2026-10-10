"""Actual supply, fashion transactions and preview invalidation against the save service."""
from pathlib import Path
import importlib.util, tempfile, subprocess, os, sys
root=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
# Shared rendering contracts supplement the production numeric checks below.
modes=(root/'Assets/Scripts/UI/GameUI.Modes.cs').read_text()
recap=(root/'Assets/Scripts/UI/GameUI.RunRecap.cs').read_text()
grid=(root/'Assets/Scripts/UI/GameUI.InventoryGrid.cs').read_text()
wear=(root/'Assets/Scripts/UI/GameUI.WearMap.cs').read_text()
smith=(root/'Assets/Scripts/UI/GameUI.Smith.cs').read_text()
assert 'public Color QualityColor {get{return GameBalance.RarityColor(Rarity);}}' in modes
assert '.Tint' not in modes and '.Tint' not in recap
assert 'DrawItemLockAction(tile,item,u)' in grid and 'DrawItemLockAction(r,item,u)' not in wear
assert '加锁' not in modes and 'DrawInventoryLock(new Rect(nameRect.xMax' not in grid
assert '(fashion?80:equipmentSize+8)' not in wear
assert 'DrawSmithMaxBadge(action,"数值已满",u)' in smith and 'DrawSmithMaxBadge(action,"已满级",u)' in smith
assert 'UIIconAtlas.Utility("gem"),GameBalance.RarityColor(Rarity.Epic));' in smith
assert 'UIIconAtlas.Utility("gem"),p.Profile.refinementStones>0?gold:muted)' not in smith
assert 'FashionBaseStat(f,pair.Key)' in modes and 'FashionRankStat(f,pair.Key)' in modes
assert 'string[] keys={"refinement","shard","thread","affix-reforge"}' in wear
assert 'Profile.mechanicMaterials,session.Progression.Profile.fashionThreads' in wear
assert 'InspectRewardItem(hit,item)' in wear and 'InspectRewardItem(rect,ResourceItemPreview(resourceKey,amount))' in smith

assert 'UnenhancedReward||col==0||selectedWorn?0:bonus.CompareTo' in modes
assert 'UnenhancedReward||col==0||selectedWorn?0:rank.CompareTo' in modes
assert 'Fill(badge,' not in grid
assert 'ResourceDescription(item.Key)' in modes
program=r'''using System;using Emberfall;
class Program{static int n;static void C(bool b,string m){n++;if(!b)throw new Exception(m);}static void Main(){
foreach(FashionSlot slot in Enum.GetValues(typeof(FashionSlot)))foreach(Rarity rarity in Enum.GetValues(typeof(Rarity)))for(int rank=0;rank<=3;rank++){
 var f=new FashionData{slot=slot,rarity=rarity,upgradeRank=rank};var plain=new FashionData{slot=slot,rarity=rarity};
 foreach(string row in ProgressionService.FashionBonus(f).Split(new[]{" · "},StringSplitOptions.RemoveEmptyEntries)){
 int split=row.LastIndexOf(' ');string stat=row.Substring(0,split);int total=int.Parse(row.Substring(split+1).TrimStart('+','×').TrimEnd('%'));
 int basis=ProgressionService.FashionBaseStat(f,stat),bonus=ProgressionService.FashionRankStat(f,stat);
 C(basis+bonus==total,"base plus rank matches authoritative displayed total");C(basis==ProgressionService.FashionBaseStat(plain,stat),"base independent of rank");C(ProgressionService.FashionRankStat(plain,stat)==0,"unranked reward has no upgrade bonus");
 }
}
var rewards=new ProgressionService(System.IO.Path.Combine(System.IO.Path.GetTempPath(),Guid.NewGuid().ToString("N")));rewards.NewGame(HeroClass.Arcanist);
 bool classified=false;rewards.Changed+=()=>classified=rewards.EnemyRewardWithoutBuildChange;
 rewards.GrantEnemyKillReward(1,0,deferSave:true);C(classified&&!rewards.EnemyRewardWithoutBuildChange,"currency-only event skips build refresh only within callback");
 rewards.GrantEnemyKillReward(1,10000,deferSave:true);C(!classified&&!rewards.EnemyRewardWithoutBuildChange,"level rewards retain build refresh");
 System.IO.Directory.Delete(rewards.SaveDirectory,true);
Console.WriteLine("PASS "+n+" fashion base/rank comparison assertions");}}
'''
with tempfile.TemporaryDirectory(prefix='fashion-refinement-') as tmp:
 p=Path(tmp);(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 names=['SkillRuntime','GameTypes','ProgressionService','ProgressionService.Attachments','ProgressionService.AutomaticGrowth','ProgressionService.Trading','CombatBalance','SkillDamageBudgets','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState','ChapterProgression','ProgressionService.Chapter','ProgressionService.Reforge','ReforgeQuote','RoomTactics','CombatImpactBatch','SafeSaveFlow']
 sources=[root/('Assets/Scripts/Core/'+name+'.cs') for name in names]+[root/'Tests/ProgressionTests.cs',root/'Assets/Scripts/UI/CollectionPreviewState.cs']
 project=cv.write_project(p/'project',sources,program)
 subprocess.run([sys.argv[1] if len(sys.argv)>1 else '/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet','run','--project',str(project),'--',str(p/'saves')],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'))
