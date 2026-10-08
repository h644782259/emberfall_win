"""Actual goal UI/service plus extracted recap formatter calls; not Unity rendering."""
from pathlib import Path
import importlib.util,tempfile,os,subprocess,sys,re
r=Path(__file__).resolve().parents[1];dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
s=importlib.util.spec_from_file_location('cv',r/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(s);s.loader.exec_module(cv)
core=['GameTypes','ProgressionService','ProgressionService.Chapter','ChapterProgression','RoomTactics','ProgressionService.Attachments','ProgressionService.AutomaticGrowth','ProgressionService.Reforge','ReforgeQuote','CombatBalance','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState']
files=[r/'Assets/Scripts/Core'/f'{x}.cs' for x in core]+[r/'Assets/Scripts/UI'/f'{x}.cs' for x in ['GameUI.ProgressionGoal','GameUI.Reforge','MobilePanelLayout','ProgressionGoalLayout']]+[r/'Tests'/f'{x}.cs' for x in ['ProgressionTests','MilestoneGoalSurfaceTests','ProgressionGoalIdentityTests','EconomyGoalUiTests']]
recap=(r/'Assets/Scripts/UI/GameUI.RunRecap.cs').read_text()
calls=re.findall(r'CurrentProgressionGoalStatus\(data.Snapshot.RewardMaterials\)',recap)
assert len(calls)==2,'recap draw and height must both use shared actual-context formatter'
for file in ['GameUI.ProgressionGoal.cs']:
 assert 'CurrentProgressionGoalStatus()' in (r/'Assets/Scripts/UI'/file).read_text(),file
with tempfile.TemporaryDirectory(prefix='economy-goal-ui-') as t:
 o=Path(t);local=[]
 for source in files:
  p=o/source.name;p.write_text(source.read_text());local.append(p)
 # Replay the exact production formatter call expressions from recap draw and measurement.
 p=o/'RecapCalls.cs';p.write_text('namespace Emberfall {public sealed partial class GameUI {class SnapshotShell {public int RewardMaterials;} class RecapShell {public SnapshotShell Snapshot;}'+''.join('string '+name+'(int materials){var data=new RecapShell{Snapshot=new SnapshotShell{RewardMaterials=materials}};return '+call+';}' for name,call in zip(['ReplayRecapDraw','ReplayRecapMeasure'],calls))+'}}');local.append(p)
 desktop=next(line for line in (r/'Assets/Scripts/UI/GameUI.SkillIntegration.cs').read_text().splitlines() if '可用点数 ' in line and 'Text(new Rect' in line)
 mobile=next(line for line in (r/'Assets/Scripts/UI/GameUI.MobileWorkshop.cs').read_text().splitlines() if '精通与技能共用点数' in line)
 desktop_expression=re.search(r'Text\(new Rect\(.*?\),(.+),12,muted\);',desktop).group(1)
 mobile_expression=re.search(r'MobileWorkshopParagraph\(ref y, width, (.+), jade, draw\);',mobile).group(1)
 p=o/'MasteryTextCalls.cs';p.write_text('namespace Emberfall {public sealed partial class GameUI {'+''.join('string '+name+'(){var p=session.Progression;return '+expression+';}' for name,expression in [('ReplayDesktopMastery',desktop_expression),('ReplayMobileMastery',mobile_expression)])+'}}');local.append(p)
 p=cv.write_project(o/'p',local,program='using System;class Program{static void Main(string[] args){Console.WriteLine(Emberfall.GameUI.VerifyEconomyGoalUi(args[0]));Console.WriteLine(Emberfall.GameUI.VerifyMilestones(args[0]));Console.WriteLine(ProgressionGoalIdentityTests.Run(args[0]));}}')
 c=o/'NuGet.Config';c.write_text('<configuration><packageSources><clear /></packageSources></configuration>');env=dict(os.environ,DOTNET_CLI_HOME=str(o/'cli'),DOTNET_NOLOGO='1')
 build=[dotnet,'build',str(p),'--configfile',str(c),'-v:q'];run=[dotnet,str(p.parent/'bin/Debug/net8.0/Validation.dll')]
 subprocess.run(build,env=env,check=True);subprocess.run(run+[str(o/'saves')],env=env,check=True)
 mutations=[('MasteryTextCalls.cs','MasteryProgressionRules.TierSummary','"50/65/80/95级分段开放"','desktop mastery text includes every production tier and cap'),('GameUI.ProgressionGoal.cs','(kind!=ProgressionGoalKind.Reforge||session.Progression.Profile.progressionGoalLevel==session.Progression.Profile.level)','true','upgraded candidate must not claim fixed level20 selection'),('ProgressionService.cs','ProgressionGoalState goal=SelectedProgressionGoal(inCamp);','ProgressionGoalState goal=SelectedProgressionGoal(true);','field formatter must use actual camp context')]
 for index,(file,old,new,expected) in enumerate(mutations):
  source=o/file;original=source.read_text();assert old in original;source.write_text(original.replace(old,new))
  subprocess.run(build,env=env,check=True)
  result=subprocess.run(run+[str(o/f'mutation-{index}')],env=env,capture_output=True,text=True)
  assert result.returncode!=0 and expected in result.stdout+result.stderr,result.stdout+result.stderr
  source.write_text(original);print('PASS: compiled old behavior fails exact assertion: '+expected)
