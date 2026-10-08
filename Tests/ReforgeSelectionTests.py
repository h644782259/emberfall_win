"""Actual service and UI selector replay. Managed GUI shells are not Unity rendering."""
from pathlib import Path
import importlib.util,tempfile,subprocess,os,sys
r=Path(__file__).resolve().parents[1];dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
s=importlib.util.spec_from_file_location('cv',r/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(s);s.loader.exec_module(cv)
core=['GameTypes','ProgressionService','ProgressionService.Chapter','ChapterProgression','RoomTactics','ProgressionService.Attachments','ProgressionService.AutomaticGrowth','ProgressionService.Reforge','ReforgeQuote','CombatBalance','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState']
files=[r/'Assets/Scripts/Core'/f'{x}.cs' for x in core]+[r/'Assets/Scripts/UI'/f'{x}.cs' for x in ['GameUI.ProgressionGoal','GameUI.Reforge','MobilePanelLayout','ProgressionGoalLayout']]+[r/'Tests'/f'{x}.cs' for x in ['ProgressionTests','MilestoneGoalSurfaceTests','ReforgeSelectionTests']]
with tempfile.TemporaryDirectory(prefix='reforge-selection-') as t:
 o=Path(t);sources=[]
 for f in files:
  p=o/f.name;p.write_text(f.read_text());sources.append(p)
 p=cv.write_project(o/'p',sources,program='using System;class Program{static void Main(string[] args){Console.WriteLine(Emberfall.GameUI.VerifyReforgeSelection(args[0]));}}')
 c=o/'NuGet.Config';c.write_text('<configuration><packageSources><clear /></packageSources></configuration>');env=dict(os.environ,DOTNET_CLI_HOME=str(o/'cli'),DOTNET_NOLOGO='1');build=[dotnet,'build',str(p),'--configfile',str(c),'-v:q'];run=[dotnet,str(p.parent/'bin/Debug/net8.0/Validation.dll')]
 subprocess.run(build,env=env,check=True);subprocess.run(run+[str(o/'save')],env=env,check=True)
 for index,(name,old,new,expected) in enumerate([
  ('ProgressionService.cs','(targetLevel==0?Profile.level:targetLevel)','Profile.level','actual track button freezes chosen30 not player50'),
  ('ProgressionService.Reforge.cs','target=low;','target=Profile.level;','three distinct actual choices'),
  ('ProgressionService.Reforge.cs','ApplyReforgeStats(Profile,preview,quote.TargetLevel);','preview.level=quote.TargetLevel;','UI commit matches actual preview and charges1220 once')]):
  f=o/name;original=f.read_text();assert old in original;f.write_text(original.replace(old,new));subprocess.run(build,env=env,check=True)
  v=subprocess.run(run+[str(o/f'mutant{index}')],env=env,capture_output=True,text=True)
  assert v.returncode!=0 and expected in v.stdout+v.stderr,v.stdout+v.stderr
  f.write_text(original);print('PASS compiled reforge regression rejected at: '+expected)
assert 'GoalOption(ref y,w,u,"重铸至 ' in (r/'Assets/Scripts/UI/GameUI.ProgressionGoal.cs').read_text()
assert 'case ProgressionGoalAction.Reforge:return ReforgeMechanic(goal.ReforgeQuote,inCamp);' in (r/'Assets/Scripts/Core/ProgressionService.cs').read_text()
assert 'if(CloseReforgeSurface())return true;' in (r/'Assets/Scripts/UI/GameUI.ProgressionGoal.cs').read_text()
print('PASS actual desktop/mobile entry and existing Back routing connect to the tested surface')
