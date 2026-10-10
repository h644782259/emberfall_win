"""Exercise level and dungeon milestone receipts against the actual save service."""
from pathlib import Path
import importlib.util,os,subprocess,tempfile
root=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
program=r'''using System;using Emberfall;
class Program{static int n;static void C(bool v,string m){n++;if(!v)throw new Exception(m);}static void Main(string[] args){
 foreach(ChapterNode node in Enum.GetValues(typeof(ChapterNode)))foreach(ChapterDifficulty difficulty in Enum.GetValues(typeof(ChapterDifficulty))){
 var run=new ChapterCombatRun(node,difficulty,17);while(!run.Finished){run.BindRoom(7+run.RoomIndex);int room=run.RoomIndex,epoch=run.Epoch;C(run.EnemyCount==(run.Objective==RoomObjective.Boss?7:10),"chapter room population increased");for(int i=0;i<run.EnemyCount;i++)C(run.Register(room,epoch,i),"all planned enemies registered");C(run.Defeat(room,epoch,0),"hunt target defeated");if(run.Objective==RoomObjective.Hunt)C(!run.DoorUnlocked,"target alone cannot skip encounter");for(int i=1;i<run.EnemyCount;i++)run.Defeat(room,epoch,i);
 if(run.Objective==RoomObjective.Purify)for(int i=0;i<30;i++){run.AdvanceSeal(0,.25f,true,true,false);run.AdvanceSeal(1,.25f,true,true,false);}if(run.Objective==RoomObjective.Escape)for(int i=0;i<20;i++)run.Advance(.25f,true,true,false);
 C(run.DoorUnlocked,"full encounter can finish objective");C(run.Exit(true,false),"exit advances chapter");}
 var budget=new ChapterExperienceBudget(node,50);int rooms=node==ChapterNode.StarPlatform?1:2,count=node==ChapterNode.StarPlatform?7:10;int earned=0;for(int room=0;room<rooms;room++)for(int i=0;i<count;i++){C(budget.Register(room,i,node==ChapterNode.StarPlatform&&i==0),"expanded experience identity admitted");int xp;C(budget.TryClaim(room,i,out xp),"expanded enemy pays once");earned+=xp;C(!budget.TryClaim(room,i,out xp),"duplicate enemy cannot pay twice");}C(budget.AllRegistered&&earned+budget.CompletionExperience==budget.TotalExperience,"completion and kills share exact total budget");
 }
 foreach(RoomBranch branch in Enum.GetValues(typeof(RoomBranch)))for(int i=0;i<5;i++){var room=new RoomChainPlan(i,77,branch);C(room.EnemyCount==(room.Boss?7:room.Branch==RoomBranch.Seal?8:10),"corridor density includes branching rooms");}
 Console.WriteLine("PASS "+n+" chapter density, completion and experience assertions");}}
'''
with tempfile.TemporaryDirectory(prefix='achievement-milestones-') as tmp:
 p=Path(tmp);names=['ChapterCombatRun','RoomChainState','SkillRuntime','GameTypes','ProgressionService','ProgressionService.Attachments','ProgressionService.AutomaticGrowth','ProgressionService.Trading','CombatBalance','SkillDamageBudgets','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState','ChapterProgression','ProgressionService.Chapter','ProgressionService.Reforge','ReforgeQuote','RoomTactics','CombatImpactBatch','SafeSaveFlow']
 sources=[root/('Assets/Scripts/Core/'+name+'.cs') for name in names]+[root/'Tests/ProgressionTests.cs'];project=cv.write_project(p/'project',sources,program)
 sdk='/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet'
 subprocess.run([sdk,'run','--project',str(project),'--',str(p/'saves')],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'))
