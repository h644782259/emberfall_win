"""Actual UI partial and progression service; GUI shells are not device/render validation."""
from pathlib import Path
import importlib.util,tempfile,os,subprocess,sys
r=Path(__file__).resolve().parents[1];dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
s=importlib.util.spec_from_file_location('cv',r/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(s);s.loader.exec_module(cv)
core=['GameTypes','ProgressionService','ProgressionService.Chapter','ChapterProgression','RoomTactics','ProgressionService.Attachments','ProgressionService.AutomaticGrowth','ProgressionService.Reforge','ReforgeQuote','CombatBalance','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState']
files=[r/'Assets/Scripts/Core'/f'{x}.cs' for x in core]+[r/'Assets/Scripts/UI/GameUI.ProgressionGoal.cs',r/'Assets/Scripts/UI/GameUI.Reforge.cs',r/'Assets/Scripts/UI/MobilePanelLayout.cs',r/'Assets/Scripts/UI/ProgressionGoalLayout.cs',r/'Tests/ProgressionTests.cs',r/'Tests/MilestoneGoalSurfaceTests.cs',r/'Tests/ProgressionGoalIdentityTests.cs']
with tempfile.TemporaryDirectory(prefix='milestone-goals-') as t:
 o=Path(t);p=cv.write_project(o/'p',files,program='using System;class Program{static void Main(string[] args){Console.WriteLine(Emberfall.GameUI.VerifyMilestones(args[0]));Console.WriteLine(ProgressionGoalIdentityTests.Run(args[0]));}}')
 c=o/'NuGet.Config';c.write_text('<configuration><packageSources><clear /></packageSources></configuration>');env=dict(os.environ,DOTNET_CLI_HOME=str(o/'cli'),DOTNET_NOLOGO='1')
 subprocess.run([dotnet,'build',str(p),'--configfile',str(c),'-v:q'],env=env,check=True)
 subprocess.run([dotnet,str(p.parent/'bin/Debug/net8.0/Validation.dll'),str(o/'saves')],env=env,check=True)
