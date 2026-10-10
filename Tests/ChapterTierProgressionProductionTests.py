"""Actual chapter tier admission, progression, persistence and source-specific rewards."""
from pathlib import Path
import importlib.util,tempfile,subprocess,os,sys
root=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
program=r'''using System;using System.IO;using System.Linq;using System.Text.Json.Nodes;using Emberfall;
class Program{static int n;static void C(bool v,string s){n++;if(!v)throw new Exception(s);}static ProgressionService New(string root){var p=new ProgressionService(Path.Combine(root,Guid.NewGuid().ToString("N")));C(p.CreateNewSlot(HeroClass.Arcanist),"new");p.Profile.level=100;p.Save();return p;}
static void Register(ProgressionService p,ChapterRunReceipt r){for(int room=0;room<ChapterDefinition.RoomCount(r.Node);room++)for(int i=0;i<(r.Node==ChapterNode.StarPlatform?7:10);i++)C(p.RegisterChapterEnemy(r,room,i,r.Node==ChapterNode.StarPlatform&&i==0),"actual experience registration");}
static void Collect(ProgressionService p){C(p.OpenChosenDungeonChest(0)!=null,"collect chest");C(p.AcknowledgeChestReward(),"ack chest");}
static void Main(string[] args){
foreach(ChapterNode node in Enum.GetValues(typeof(ChapterNode)))foreach(ChapterDifficulty difficulty in Enum.GetValues(typeof(ChapterDifficulty))){
var p=New(args[0]);ChapterRunReceipt r;C(ChapterProgression.AvailableTier(p.Profile,node,difficulty)==1,"start first tier");C(!p.TryBeginChapterNode(node,difficulty,2,out r),"cannot skip first tier");
for(int tier=1;tier<=3;tier++){
C(p.TryBeginChapterNode(node,difficulty,tier,out r)&&r.Tier==tier&&r.Difficulty==difficulty,"select any unlocked difficulty and tier");Register(p,r);int shards=p.Profile.mechanicMaterials;
C(p.TryCompleteChapterNode(r),"commit clear");C(ChapterProgression.CompletedTier(p.Profile,node,difficulty)==tier&&ChapterProgression.AvailableTier(p.Profile,node,difficulty)==tier+1,"unlock next on committed clear");
C(p.Profile.pendingChestChapterSource&&p.Profile.pendingChestGemSource==(node==ChapterNode.StarPlatform),"reward source tracks selected chapter");
C(p.TryCompleteChapterNode(r)&&p.Profile.mechanicMaterials>=shards,"receipt replay does not double unlock");
Collect(p);C(p.LoadSlot(p.CurrentSlotId)&&ChapterProgression.CompletedTier(p.Profile,node,difficulty)==tier,"per difficulty tiers persist");
foreach(ChapterDifficulty other in Enum.GetValues(typeof(ChapterDifficulty)))if(other!=difficulty)C(ChapterProgression.AvailableTier(p.Profile,node,other)==1,"other difficulties independent");
}
C(p.TryBeginChapterNode(node,difficulty,1,out r),"lower tier replay allowed");p.CancelChapterRun();C(!p.TryCompleteChapterNode(r)&&ChapterProgression.AvailableTier(p.Profile,node,difficulty)==4,"cancelled attempt does not unlock");
}
foreach(int high in new[]{10,100,1000,1000000,int.MaxValue-1}){
var p=New(args[0]);p.Profile.chapterDifficultyBestTiers[0]=high;p.Profile.chapterBestTiers[0]=high;p.Save();ChapterRunReceipt r;
C(p.LoadSlot(p.CurrentSlotId)&&ChapterProgression.AvailableTier(p.Profile,ChapterNode.ForestCourt)==high+1,"no level-based or tier100 truncation");
C(p.TryBeginChapterNode(ChapterNode.ForestCourt,ChapterDifficulty.Normal,high+1,out r),"admit next high tier");Register(p,r);
C(p.TryCompleteChapterNode(r)&&ChapterProgression.CompletedTier(p.Profile,ChapterNode.ForestCourt,ChapterDifficulty.Normal)==high+1,"commit high tier without overflow");
Collect(p);C(p.LoadSlot(p.CurrentSlotId)&&ChapterProgression.CompletedTier(p.Profile,ChapterNode.ForestCourt,ChapterDifficulty.Normal)==high+1,"preserve high record after save");
float health=ChapterProgression.TierHealthMultiplier(high),damage=ChapterProgression.TierDamageMultiplier(high);C(!float.IsInfinity(health)&&!float.IsNaN(health)&&!float.IsInfinity(damage)&&health>0&&damage>0,"finite high tier scaling");
}
C(ChapterProgression.TierHealthMultiplier(101)>ChapterProgression.TierHealthMultiplier(100)&&ChapterProgression.TierDamageMultiplier(1000)>ChapterProgression.TierDamageMultiplier(999),"enemy challenge keeps growing beyond old cap");
var atomic=New(args[0]);ChapterRunReceipt receipt;C(atomic.TryBeginChapterNode(ChapterNode.Redrock,ChapterDifficulty.Hard,1,out receipt),"atomic attempt");Register(atomic,receipt);int before=atomic.Profile.mechanicMaterials;Directory.CreateDirectory(atomic.SaveFilePath+".tmp");C(!atomic.TryCompleteChapterNode(receipt)&&ChapterProgression.AvailableTier(atomic.Profile,ChapterNode.Redrock,ChapterDifficulty.Hard)==1&&atomic.Profile.mechanicMaterials==before&&!atomic.Profile.pendingFashionChest,"failed clear does not unlock or reward");Directory.Delete(atomic.SaveFilePath+".tmp");C(atomic.TryCompleteChapterNode(receipt)&&ChapterProgression.AvailableTier(atomic.Profile,ChapterNode.Redrock,ChapterDifficulty.Hard)==2,"retry same receipt once");
var migrated=New(args[0]);var json=JsonNode.Parse(File.ReadAllText(migrated.SaveFilePath));var data=json["profile"];data["chapterTierRevision"]=0;data["chapterCompletedMask"]=1;data["chapterHighestDifficulties"]=new JsonArray(3,0,0);data["chapterBestTiers"]=new JsonArray(55,0,0);data["chapterDifficultyBestTiers"]=null;File.WriteAllText(migrated.SaveFilePath,json.ToJsonString());C(migrated.LoadSlot(migrated.CurrentSlotId)&&ChapterProgression.AvailableTier(migrated.Profile,ChapterNode.ForestCourt,ChapterDifficulty.Heroic)==56,"legacy highest difficulty record preserved");C(ChapterProgression.AvailableTier(migrated.Profile,ChapterNode.ForestCourt,ChapterDifficulty.Normal)==1,"legacy never invents uncleared lower difficulty tiers");C(migrated.LoadSlot(migrated.CurrentSlotId)&&ChapterProgression.AvailableTier(migrated.Profile,ChapterNode.ForestCourt,ChapterDifficulty.Heroic)==56,"migration idempotent");
Console.WriteLine("PASS "+n+" chapter tier assertions");}}
'''
with tempfile.TemporaryDirectory(prefix='chapter-tiers-') as tmp:
 p=Path(tmp);(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 names=['SkillRuntime','GameTypes','ProgressionService','ProgressionService.Attachments','ProgressionService.AutomaticGrowth','ProgressionService.Trading','ProgressionService.Smith','CombatBalance','SkillDamageBudgets','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState','ChapterProgression','ProgressionService.Chapter','ProgressionService.Reforge','ReforgeQuote','RoomTactics','CombatImpactBatch','SafeSaveFlow']
 sources=[root/('Assets/Scripts/Core/'+name+'.cs') for name in names]+[root/'Tests/ProgressionTests.cs']
 project=cv.write_project(p/'project',sources,program)
 subprocess.run([sys.argv[1] if len(sys.argv)>1 else '/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet','run','--project',str(project),'--',str(p/'saves')],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'))
