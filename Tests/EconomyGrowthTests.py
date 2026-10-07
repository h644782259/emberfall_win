"""Real progression persistence and immutable-quote regression replay."""
from pathlib import Path
import importlib.util,tempfile,os,subprocess,sys
root=Path(__file__).resolve().parents[1];dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
s=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(s);s.loader.exec_module(cv)
core=['GameTypes','ProgressionService','ProgressionService.Chapter','ProgressionService.Attachments','ProgressionService.AutomaticGrowth','ProgressionService.Reforge','ChapterProgression','RoomTactics','CombatBalance','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState','ReforgeQuote','ExpeditionModeState']
tests=['EconomyGrowthTests','RebalanceProgressionTests','UpgradeProgressionTests','AdventureProgressionTests','ProgressionGoalIdentityTests','BuildPresetTests','ProgressionGrowthTests']
with tempfile.TemporaryDirectory(prefix='economy-growth-') as t:
 o=Path(t);sources=[root/'Assets/Scripts/Core'/f'{n}.cs' for n in core]+[root/'Assets/Scripts/UI/AdventureEntryPresentation.cs',root/'Tests/AdventureEntryPresentationTests.cs',root/'Tests/ProgressionTests.cs']+[root/'Tests'/f'{n}.cs' for n in tests]
 # Copy production sources so the mandatory negative control cannot mutate the repository.
 local=[]
 for source in sources:
  dest=o/'sources'/source.name;dest.parent.mkdir(exist_ok=True);dest.write_text(source.read_text());local.append(dest)
 program='using System;class Program{static void Main(string[] args){'+''.join('Console.WriteLine('+n+'.Run(args[0]));' for n in tests)+'Console.WriteLine(AdventureEntryPresentationTests.Run());}}'
 p=cv.write_project(o/'p',local,program=program)
 c=o/'NuGet.Config';c.write_text('<configuration><packageSources><clear /></packageSources></configuration>');env=dict(os.environ,DOTNET_CLI_HOME=str(o/'cli'),DOTNET_NOLOGO='1')
 build=[dotnet,'build',str(p),'--configfile',str(c),'-v:q'];run=[dotnet,str(p.parent/'bin/Debug/net8.0/Validation.dll')]
 subprocess.run(build,env=env,check=True);subprocess.run(run+[str(o/'saves')],env=env,check=True)
 source=o/'sources/ProgressionService.cs';original=source.read_text();needle='ReforgeMechanic(goal.ReforgeQuote,inCamp)';assert needle in original
 source.write_text(original.replace(needle,'ReforgeMechanic(goal.ItemId,inCamp)'))
 subprocess.run(build,env=env,check=True)
 failed=subprocess.run(run+[str(o/'mutation-saves')],env=env,capture_output=True,text=True)
 assert failed.returncode!=0 and 'goal execution retains captured target and quote at later level' in failed.stdout+failed.stderr,failed.stdout+failed.stderr
 print('PASS: compiled legacy current-level goal mutation fails the exact fixed-target production assertion')

 source.write_text(original)
 needle='if (profile.level >= 30)';assert needle in original
 source.write_text(original.replace(needle,'if (profile.level >= 30 && profile.masteryRevision >= 1)'))
 subprocess.run(build,env=env,check=True)
 failed=subprocess.run(run+[str(o/'legacy-mutation-saves')],env=env,capture_output=True,text=True)
 assert failed.returncode!=0 and 'legacy revision retains legal ranks' in failed.stdout+failed.stderr,failed.stdout+failed.stderr
 print('PASS: compiled legacy mastery clearing mutation fails the exact retained-investment assertion')
