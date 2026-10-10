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
for(int tier=1;tier<=(difficulty==ChapterDifficulty.Heroic?3:1);tier++){
C(p.TryBeginChapterNode(node,difficulty,tier,out r)&&r.Tier==tier&&r.Difficulty==difficulty,"select any unlocked difficulty and tier");Register(p,r);int shards=p.Profile.mechanicMaterials;
C(p.TryCompleteChapterNode(r),"commit clear");C(ChapterProgression.CompletedTier(p.Profile,node,difficulty)==tier&&ChapterProgression.AvailableTier(p.Profile,node,difficulty)==(difficulty==ChapterDifficulty.Heroic?tier+1:1),"unlock next on committed clear");
C(p.Profile.pendingChestChapterSource&&p.Profile.pendingChestGemSource==(node==ChapterNode.StarPlatform),"reward source tracks selected chapter");
C(p.TryCompleteChapterNode(r)&&p.Profile.mechanicMaterials>=shards,"receipt replay does not double unlock");
Collect(p);C(p.LoadSlot(p.CurrentSlotId)&&ChapterProgression.CompletedTier(p.Profile,node,difficulty)==tier,"per difficulty tiers persist");
foreach(ChapterDifficulty other in Enum.GetValues(typeof(ChapterDifficulty)))if(other!=difficulty)C(ChapterProgression.AvailableTier(p.Profile,node,other)==1,"other difficulties independent");
}
C(p.TryBeginChapterNode(node,difficulty,1,out r),"lower tier replay allowed");p.CancelChapterRun();C(!p.TryCompleteChapterNode(r)&&ChapterProgression.AvailableTier(p.Profile,node,difficulty)==(difficulty==ChapterDifficulty.Heroic?4:1),"cancelled attempt does not unlock");
}
foreach(int high in new[]{10,100,1000,1000000,int.MaxValue-1}){
var p=New(args[0]);p.Profile.chapterDifficultyBestTiers[2]=high;p.Profile.chapterBestTiers[0]=high;p.Save();ChapterRunReceipt r;
C(p.LoadSlot(p.CurrentSlotId)&&ChapterProgression.AvailableTier(p.Profile,ChapterNode.ForestCourt,ChapterDifficulty.Heroic)==high+1,"no level-based or tier100 truncation");
C(p.TryBeginChapterNode(ChapterNode.ForestCourt,ChapterDifficulty.Heroic,high+1,out r),"admit next high tier");Register(p,r);
C(p.TryCompleteChapterNode(r)&&ChapterProgression.CompletedTier(p.Profile,ChapterNode.ForestCourt,ChapterDifficulty.Heroic)==high+1,"commit high tier without overflow");
Collect(p);C(p.LoadSlot(p.CurrentSlotId)&&ChapterProgression.CompletedTier(p.Profile,ChapterNode.ForestCourt,ChapterDifficulty.Heroic)==high+1,"preserve high record after save");
float health=ChapterProgression.TierHealthMultiplier(high),damage=ChapterProgression.TierDamageMultiplier(high);C(!float.IsInfinity(health)&&!float.IsNaN(health)&&!float.IsInfinity(damage)&&health>0&&damage>0,"finite high tier scaling");
}
C(ChapterProgression.TierHealthMultiplier(101)>ChapterProgression.TierHealthMultiplier(100)&&ChapterProgression.TierDamageMultiplier(1000)>ChapterProgression.TierDamageMultiplier(999),"enemy challenge keeps growing beyond old cap");
var atomic=New(args[0]);ChapterRunReceipt receipt;C(atomic.TryBeginChapterNode(ChapterNode.Redrock,ChapterDifficulty.Hard,1,out receipt),"atomic attempt");Register(atomic,receipt);int before=atomic.Profile.mechanicMaterials;Directory.CreateDirectory(atomic.SaveFilePath+".tmp");C(!atomic.TryCompleteChapterNode(receipt)&&ChapterProgression.AvailableTier(atomic.Profile,ChapterNode.Redrock,ChapterDifficulty.Hard)==1&&atomic.Profile.mechanicMaterials==before&&!atomic.Profile.pendingFashionChest,"failed clear does not unlock or reward");Directory.Delete(atomic.SaveFilePath+".tmp");C(atomic.TryCompleteChapterNode(receipt)&&ChapterProgression.AvailableTier(atomic.Profile,ChapterNode.Redrock,ChapterDifficulty.Hard)==1,"retry same receipt once");
var migrated=New(args[0]);var json=JsonNode.Parse(File.ReadAllText(migrated.SaveFilePath));var data=json["profile"];data["chapterTierRevision"]=0;data["chapterCompletedMask"]=1;data["chapterHighestDifficulties"]=new JsonArray(3,0,0);data["chapterBestTiers"]=new JsonArray(55,0,0);data["chapterDifficultyBestTiers"]=null;File.WriteAllText(migrated.SaveFilePath,json.ToJsonString());C(migrated.LoadSlot(migrated.CurrentSlotId)&&ChapterProgression.AvailableTier(migrated.Profile,ChapterNode.ForestCourt,ChapterDifficulty.Heroic)==56,"legacy highest difficulty record preserved");C(ChapterProgression.AvailableTier(migrated.Profile,ChapterNode.ForestCourt,ChapterDifficulty.Normal)==1,"legacy never invents uncleared lower difficulty tiers");C(migrated.LoadSlot(migrated.CurrentSlotId)&&ChapterProgression.AvailableTier(migrated.Profile,ChapterNode.ForestCourt,ChapterDifficulty.Heroic)==56,"migration idempotent");
var nextProfile=New(args[0]);ChapterRunReceipt nextReceipt;C(nextProfile.TryBeginChapterNode(ChapterNode.ForestCourt,ChapterDifficulty.Heroic,1,out nextReceipt),"next action start");Register(nextProfile,nextReceipt);C(nextProfile.TryCompleteChapterNode(nextReceipt),"next action settle");Collect(nextProfile);
var action=new NextTierSession{Progression=nextProfile,chapterReceipt=nextReceipt,ChapterRun=new NextRun(),chapterRetryTier=1};
C(action.CanChallengeNextTier&&action.ChallengeNextTier()&&action.StartedTier==2,"portal next tier enabled and dispatches chapter tier2");
action.ChapterRewardPending=true;C(!action.CanChallengeNextTier&&!action.ChallengeNextTier(),"pending chapter settlement blocks next");action.ChapterRewardPending=false;
action.ChapterRun.Failed=true;C(!action.CanChallengeNextTier,"failed chapter cannot advance");action.ChapterRun.Failed=false;
action.DungeonResultsReady=false;C(!action.CanChallengeNextTier,"uncollected reward blocks next");action.DungeonResultsReady=true;
action.chapterRetryTier=2;C(!action.CanChallengeNextTier,"cannot advance from uncleared tier");action.chapterRetryTier=int.MaxValue;C(!action.CanChallengeNextTier,"integer ceiling blocks overflow");action.chapterRetryTier=1;
action.chapterReceipt=null;C(!action.CanChallengeNextTier,"missing active receipt blocks next");action.chapterReceipt=nextReceipt;
action.Paused=true;C(!action.ChallengeNextTier(),"paused cannot restart");action.Paused=false;
action.RestartSucceeds=false;C(!action.ChallengeNextTier(),"chapter restart failure surfaced");action.RestartSucceeds=true;
action.ChapterRun.Difficulty=ChapterDifficulty.Normal;C(!action.CanChallengeNextTier,"normal has no next tier");action.ChapterRun.Difficulty=ChapterDifficulty.Hard;C(!action.CanChallengeNextTier,"hard has no next tier");action.ChapterActive=false;action.DungeonTier=1;action.MaximumDungeonTier=2;C(action.CanChallengeNextTier&&action.ChallengeNextTier()&&action.SelectedDungeonTier==2,"ordinary next tier unchanged");action.DungeonTier=2;C(!action.CanChallengeNextTier,"ordinary cap unchanged");
Console.WriteLine("PASS "+n+" chapter tier assertions");}}
'''
def member(path,signature):
 text=(root/path).read_text();start=text.index(signature);end=text.index('{',start)+1;depth=1
 while depth:
  depth+=(text[end]=='{')-(text[end]=='}');end+=1
 return text[start:end]
program+=r'''
class Mathf {public static int Min(int a,int b)=>Math.Min(a,b);}
class NextRun {public bool Failed;public ChapterNode Node=ChapterNode.ForestCourt;public ChapterDifficulty Difficulty=ChapterDifficulty.Heroic;}
class NextPause {public bool BackgroundPaused;}
class NextRooms {public bool Failed;}
class NextTierSession {
 public bool HasStarted=true,InDungeon=true,IsDead,DungeonResultsReady=true,changingZone,ModeRewardPending,DungeonRewardPending,ChapterActive=true,ChapterFinished=true,ChapterRewardPending,enteringChapter,Paused;
 public ProgressionService Progression;public ChapterRunReceipt chapterReceipt;public NextRun ChapterRun;public int chapterRetryTier;
 public int DungeonTier,MaximumDungeonTier,SelectedDungeonTier,StartedTier;public bool RestartSucceeds=true;public NextPause pauseState=new NextPause();public NextRooms RoomChainRun;public string ModeName;
 bool RestartChapterAttempt(int? tier=null){StartedTier=tier??chapterRetryTier;return RestartSucceeds;}
 bool ChangeZone(bool inside)=>true;void FailRoomGeneration(string message,Exception error){}void Notify(string message){}
 MEMBERS
}
'''
next_source='Assets/Scripts/Core/GameSession.NextChallenge.cs' if (root/'Assets/Scripts/Core/GameSession.NextChallenge.cs').exists() else 'Assets/Scripts/Core/GameSession.Expedition.cs'
program=program.replace('MEMBERS',member(next_source,'public bool CanChallengeNextTier')+'\n'+member(next_source,'public bool ChallengeNextTier()')+'\n'+member('Assets/Scripts/Core/GameSession.Chapter.cs','private bool CanAdvanceChapterTier')).replace('using System;using System.IO;','using UnityEngine;using System;using System.IO;')
with tempfile.TemporaryDirectory(prefix='chapter-tiers-') as tmp:
 p=Path(tmp);(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 names=['SkillRuntime','GameTypes','ProgressionService','ProgressionService.Attachments','ProgressionService.AutomaticGrowth','ProgressionService.Trading','ProgressionService.Smith','CombatBalance','SkillDamageBudgets','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState','ChapterProgression','ProgressionService.Chapter','ProgressionService.Reforge','ReforgeQuote','RoomTactics','CombatImpactBatch','SafeSaveFlow']
 sources=[root/('Assets/Scripts/Core/'+name+'.cs') for name in names]+[root/'Tests/ProgressionTests.cs']
 project=cv.write_project(p/'project',sources,program)
 subprocess.run([sys.argv[1] if len(sys.argv)>1 else '/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet','run','--project',str(project),'--',str(p/'saves')],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'))
