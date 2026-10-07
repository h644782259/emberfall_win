from pathlib import Path
import sys,importlib.util,tempfile,subprocess,os
r=Path(__file__).resolve().parents[1];dotnet=sys.argv[1]
s=importlib.util.spec_from_file_location('cv',r/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(s);s.loader.exec_module(cv)
core=['GameTypes','ProgressionService','ProgressionService.Chapter','ChapterProgression','RoomTactics','ProgressionService.Attachments','ProgressionService.AutomaticGrowth','ProgressionService.Reforge','ReforgeQuote','CombatBalance','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState']
def member(source,key):
 a=source.index(key);b=source.index('{',a)+1;d=1
 while d:d+=(source[b]=='{')-(source[b]=='}');b+=1
 return source[a:b]
files=[r/'Assets/Scripts/Core'/f'{x}.cs' for x in core]+[r/'Tests'/f'{x}.cs' for x in ['ProgressionTests','ProgressionGrowthTests','ReturningCounterPersistenceTests']]
with tempfile.TemporaryDirectory(prefix='returning-save-') as t:
 p=Path(t);branch=member((r/'Assets/Scripts/UI/GameUI.Expedition.cs').read_text(),'else if(campTab==1)');desktop=p/'Desktop.cs';desktop.write_text((r/'Tests/ReturningCounterDesktopFixture.cs').read_text().replace('// GENERATED_BRANCH','void DrawMechanismWorkshop(){var p=session.Progression;var w=new Rect(0,0,1000,700);'+branch[branch.index('{')+1:-1]+'}'));files.append(desktop);project=cv.write_project(p/'p',files,program='using System;class Program{static void Main(string[] args){Console.WriteLine(Emberfall.GameUI.Verify(args[0]));Console.WriteLine(ReturningCounterPersistenceTests.Run(args[0]));Console.WriteLine(ProgressionGrowthTests.Run(args[0]));}}');c=p/'NuGet.Config';c.write_text('<configuration><packageSources><clear/></packageSources></configuration>');env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1');subprocess.run([dotnet,'build',str(project),'--configfile',str(c),'-v:q'],env=env,check=True);subprocess.run([dotnet,str(project.parent/'bin/Debug/net8.0/Validation.dll'),str(p/'save')],env=env,check=True)

 original=desktop.read_text();assert 'BuildCatalog.HasMechanicVariant(mechanic)&&Button' in original;desktop.write_text(original.replace('BuildCatalog.HasMechanicVariant(mechanic)&&Button','p.Profile.heroClass==HeroClass.Arcanist&&Button'))
 subprocess.run([dotnet,'build',str(project),'--configfile',str(c),'-v:q'],env=env,check=True)
 result=subprocess.run([dotnet,str(project.parent/'bin/Debug/net8.0/Validation.dll'),str(p/'mutant')],env=env,capture_output=True,text=True)
 assert result.returncode and 'desktop actual button unlocksB' in result.stdout+result.stderr,result.stdout+result.stderr
 print('PASS compiled desktop Arcanist-only regression rejected by actual unlock button')
