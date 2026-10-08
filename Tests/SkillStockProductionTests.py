from pathlib import Path
import importlib.util,tempfile,subprocess,os,sys
root=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
with tempfile.TemporaryDirectory(prefix='skill-stock-') as t:
 p=Path(t);(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 core=['SkillRuntime','GameTypes','ProgressionService','CombatBalance','SkillDamageBudgets','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState','ChapterProgression','ProgressionService.Chapter','ProgressionService.Reforge','ReforgeQuote','RoomTactics','CombatImpactBatch','SafeSaveFlow','CastFirstHitReceipt']
 sources=[root/('Assets/Scripts/Core/'+name+'.cs') for name in core]+[root/'Tests/ProgressionTests.cs',root/'Tests/SkillStockProductionTests.cs']
 project=cv.write_project(p/'project',sources,'System.Console.WriteLine(SkillStockProductionTests.Run(@"'+str(p/'saves')+'"));')
 subprocess.run([sys.argv[1] if len(sys.argv)>1 else 'dotnet','run','--project',str(project)],env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'),check=True)
