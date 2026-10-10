"""Exercise socket replacement, mainline HUD and retired preset compatibility."""
from pathlib import Path
import importlib.util, os, subprocess, tempfile
root=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
names=['SkillRuntime','GameTypes','ProgressionService','ProgressionService.Attachments','ProgressionService.AutomaticGrowth','ProgressionService.Trading','CombatBalance','SkillDamageBudgets','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState','ChapterProgression','ProgressionService.Chapter','ProgressionService.Reforge','ReforgeQuote','RoomTactics','CombatImpactBatch','SafeSaveFlow','ProgressionHudHint','ProgressionAttention']
sources=[root/('Assets/Scripts/Core/'+name+'.cs') for name in names]+[root/'Tests'/name for name in ['ProgressionTests.cs','TripleGemSocketTests.cs','MainlineGoalTests.cs']]
program='using System; class Program { static void Main(string[] args) { Console.WriteLine(TripleGemSocketTests.Run(args[0])); Console.WriteLine(MainlineGoalTests.Run(args[0])); } }'
with tempfile.TemporaryDirectory(prefix='progression-followup-') as tmp:
 p=Path(tmp);project=cv.write_project(p/'project',sources,program)
 sdk='/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet'
 subprocess.run([sdk,'run','--project',str(project),'--',str(p/'saves')],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'))
