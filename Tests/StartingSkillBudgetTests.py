"""Actual chapter economy services; compiled precise legacy behavior controls."""
from pathlib import Path
import importlib.util,tempfile,subprocess,sys,os
root=Path(__file__).resolve().parents[1];dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
core=['GameTypes','ProgressionService','ProgressionService.Chapter','ProgressionService.Attachments','ProgressionService.AutomaticGrowth','ProgressionService.Reforge','ReforgeQuote','ChapterProgression','RoomTactics','CombatBalance','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState']
with tempfile.TemporaryDirectory(prefix='chapter-xp-') as d:
 d=Path(d);sources=[]
 for path in [*[root/'Assets/Scripts/Core'/(n+'.cs') for n in core],root/'Tests/ProgressionTests.cs',root/'Tests/StartingSkillBudgetTests.cs']:
  dest=d/path.name;dest.write_text(path.read_text());sources.append(dest)
 config=d/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>');env=dict(os.environ,DOTNET_CLI_HOME=str(d/'cli'),DOTNET_NOLOGO='1');p=cv.write_project(d/'p',sources,program='System.Console.WriteLine(StartingSkillBudgetTests.Run(args[0]));')
 build=[dotnet,'build',str(p),'--configfile',str(config),'-v:q'];run=[dotnet,str(p.parent/'bin/Debug/net8.0/Validation.dll')]
 def execute(name):
  subprocess.run(build,env=env,check=True);return subprocess.run(run+[str(d/name)],env=env,capture_output=True,text=True)
 result=execute('current');print(result.stdout+result.stderr);result.check_returncode()
 mutations=[('GameTypes.cs','Math.Max(1, level - 1)','Math.Max(0, level - 1)','every class starts with usable first active rank one'),('GameTypes.cs','Math.Max(2, SkillRequiredLevels[skill])','SkillRequiredLevels[skill]','starting skill rank upgrades remain ten and twenty'),('ProgressionService.cs','if (profile.level == 1 && slot == 0) rank = 1;','','old unlearned level one migrated once')]
 for index,(file,before,after,expected) in enumerate(mutations):
  path=d/file;original=path.read_text();assert before in original;path.write_text(original.replace(before,after));result=execute('old'+str(index));assert result.returncode!=0 and expected in result.stdout+result.stderr,result.stdout+result.stderr;print('PASS compiled negative:',expected);path.write_text(original)
